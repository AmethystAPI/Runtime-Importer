using Amethyst.Common.Diagnostics;
using Amethyst.Common.Extensions;
using ClangSharp.Interop;
using System.Linq;

namespace Amethyst.SymbolGenerator.Parsing
{
    public class ASTVisitor
    {
        private readonly Dictionary<string, string> SpellingCache = [];
        private readonly Dictionary<string, string> MangleCache = [];
        private readonly Dictionary<string, ASTCursorLocation> LocationCache = [];
        private readonly Dictionary<string, string> RawCommentCache = [];
        private readonly Dictionary<string, bool> IsImportedCache = [];
        private readonly Dictionary<string, string> FullNamespaceCache = [];

        private readonly Dictionary<string, ASTClass> ClassCache = [];
        private readonly Dictionary<string, ASTMethod> MethodCache = [];
        private readonly Dictionary<string, ASTVariable> VariableCache = [];

        public CXIndex Index { get; private set; }
        public CXTranslationUnit TranslationUnit { get; private set; }
        public string InputDirectory { get; private set; }
        public HashSet<string> StrictHeaders { get; private set; } = [];

        public IReadOnlyCollection<ASTClass> Classes => ClassCache.Values;
        public IReadOnlyCollection<ASTMethod> Methods => MethodCache.Values;
        public IReadOnlyCollection<ASTVariable> Variables => VariableCache.Values;

        public ASTVisitor(string inputFile, string inputDirectory, IEnumerable<string> arguments, IEnumerable<string> strictHeaders)
        {
            Index = CXIndex.Create();
            var error = CXTranslationUnit.TryParse(
                Index,
                inputFile,
                arguments.ToArray(),
                [],
                CXTranslationUnit_Flags.CXTranslationUnit_SkipFunctionBodies |
                CXTranslationUnit_Flags.CXTranslationUnit_Incomplete | 
                CXTranslationUnit_Flags.CXTranslationUnit_IgnoreNonErrorsFromIncludedFiles,
                out var translationUnit);
            if (error != CXErrorCode.CXError_Success)
                throw new Exception($"Failed to parse translation unit. Error code: {error}");
            TranslationUnit = translationUnit;
            InputDirectory = inputDirectory.NormalizeSlashes();
            StrictHeaders = [.. strictHeaders.Select(h => Path.GetFullPath(h).NormalizeSlashes())];
        }

        #region Diagnostics
        public List<CXDiagnostic> GetDiagnostics()
        {
            var diagnostics = new List<CXDiagnostic>();
            var numDiagnostics = TranslationUnit.NumDiagnostics;
            for (var i = 0u; i < numDiagnostics; i++)
            {
                diagnostics.Add(TranslationUnit.GetDiagnostic(i));
            }
            return diagnostics;
        }

        public bool PrintErrors()
        {
            if (GetDiagnostics().All(d => d.Severity != CXDiagnosticSeverity.CXDiagnostic_Error && d.Severity != CXDiagnosticSeverity.CXDiagnostic_Fatal))
                return false;
            foreach (var diag in GetDiagnostics())
            {
                Action<string>? log = diag.Severity switch
                {
                    CXDiagnosticSeverity.CXDiagnostic_Error => Logger.Error,
                    CXDiagnosticSeverity.CXDiagnostic_Fatal => Logger.Error,
                    _ => null
                };
                if (log is null)
                    continue;
                var location = diag.Location;
                location.GetFileLocation(out var file, out var line, out var column, out var offset);
                string filePath = file.ToString() ?? "Unknown file";
                string message = diag.Format(CXDiagnostic.DefaultDisplayOptions).ToString() ?? "Unknown diagnostic message";
                log($"{filePath}({line},{column}): {message}");
            }
            return true;
        }
        #endregion

        public string GetUsr(CXCursor cursor)
        {
            return cursor.Usr.ToString() + cursor.IsDefinition;
        }

        public string GetSpelling(CXCursor cursor)
        {
            string usr = GetUsr(cursor);
            if (SpellingCache.TryGetValue(usr, out var cached))
                return cached;

            var spelling = cursor.Spelling.ToString();

            SpellingCache[usr] = spelling;
            return spelling;
        }

        public ASTCursorLocation? GetLocation(CXCursor cursor)
        {
            string usr = GetUsr(cursor);
            if (LocationCache.TryGetValue(usr, out var cached))
                return cached;

            cursor.Location.GetFileLocation(out var file, out var line, out var column, out var offset);

            string path = file.ToString();
            if (!string.IsNullOrEmpty(path))
                path = Path.GetFullPath(path.ToString()).NormalizeSlashes();

            var location = new ASTCursorLocation(path, line, column, offset);

            LocationCache[usr] = location;
            return location;
        }

        public string GetMangledName(CXCursor cursor)
        {
            if (cursor.IsNull || cursor.IsInvalid)
                return "Unknown";

            string usr = GetUsr(cursor);
            if (MangleCache.TryGetValue(usr, out var cached))
                return cached;

            switch (cursor.Kind)
            {
                case CXCursorKind.CXCursor_FunctionDecl:
                case CXCursorKind.CXCursor_CXXMethod:
                case CXCursorKind.CXCursor_Constructor:
                case CXCursorKind.CXCursor_Destructor:
                case CXCursorKind.CXCursor_VarDecl:
                    break;
                default:
                    return "Unknown";
            }

            string? mangled = null;
            unsafe
            {
                if (cursor.CXXManglings is not null)
                    mangled = cursor.CXXManglings->FirstOrDefault().ToString();
                else 
                    mangled = cursor.Mangling.ToString();
            }
            
            if (string.IsNullOrEmpty(mangled))
                mangled = "Unknown";

            MangleCache[usr] = mangled;
            return mangled;
        }

        public string? GetRawComment(CXCursor cursor)
        {
            string usr = GetUsr(cursor);

            if (RawCommentCache.TryGetValue(usr, out var cached))
                return cached;

            var rawComment = cursor.RawCommentText.ToString();
            if (string.IsNullOrEmpty(rawComment))
                return null;

            RawCommentCache[usr] = rawComment;
            return rawComment;
        }

        public bool IsImported(CXCursor cursor)
        {
            string usr = GetUsr(cursor);
            if (IsImportedCache.TryGetValue(usr, out var cached))
                return cached;

            bool isImported = false;
            unsafe
            {
                cursor.VisitChildren((c, parent, data) =>
                {
                    if (c.Kind == CXCursorKind.CXCursor_DLLImport)
                    {
                        isImported = true;
                        return CXChildVisitResult.CXChildVisit_Break;
                    }
                    return CXChildVisitResult.CXChildVisit_Continue;
                }, new CXClientData(nint.Zero));
            }

            IsImportedCache[usr] = isImported;
            return isImported;
        }

        public string? GetFullNamespace(CXCursor cursor)
        {
            if (cursor.Kind != CXCursorKind.CXCursor_Namespace)
                return null;
            string usr = GetUsr(cursor);
            if (FullNamespaceCache.TryGetValue(usr, out var cached))
                return cached;

            var names = new List<string>();
            var current = cursor;
            while (!current.IsNull && current.Kind == CXCursorKind.CXCursor_Namespace)
            {
                names.Add(current.Spelling.ToString());
                current = current.SemanticParent;
            }
            names.Reverse();
            string namespaceStr = string.Join("::", names);
            FullNamespaceCache[usr] = namespaceStr;
            return namespaceStr;
        }

        private bool ShouldProcessFile(string? file)
        {
            if (string.IsNullOrEmpty(file)) 
                return false;
            file = Path.GetFullPath(file).NormalizeSlashes();
            return file.StartsWith(InputDirectory, StringComparison.Ordinal) && StrictHeaders.Contains(file);
        }

        private (List<CXCursor> methods, List<CXCursor> vars, List<CXCursor> bases) CollectClassMembers(CXCursor cursor)
        {
            var methods = new List<CXCursor>();
            var vars = new List<CXCursor>();
            var bases = new List<CXCursor>();

            unsafe
            {
                cursor.VisitChildren((c, parent, data) =>
                {
                    switch (c.Kind)
                    {
                        case CXCursorKind.CXCursor_CXXMethod:
                        case CXCursorKind.CXCursor_Constructor:
                        case CXCursorKind.CXCursor_Destructor:
                            methods.Add(c);
                            break;
                        case CXCursorKind.CXCursor_FieldDecl:
                        case CXCursorKind.CXCursor_VarDecl:
                            if (cursor.Linkage == CXLinkageKind.CXLinkage_External && cursor.Language == CXLanguageKind.CXLanguage_C)
                                return CXChildVisitResult.CXChildVisit_Continue;
                            vars.Add(c);
                            break;
                        case CXCursorKind.CXCursor_CXXBaseSpecifier:
                            bases.Add(c);
                            break;
                    }
                    return CXChildVisitResult.CXChildVisit_Continue;
                }, new CXClientData(nint.Zero));
            }

            return (methods, vars, bases);
        }

        public ASTClass[] GetClasses()
        {
            var classes = new List<ASTClass>();
            unsafe
            {
                // Traverse the AST
                TranslationUnit.Cursor.VisitChildren((cursor, parent, data) =>
                {
                    var loc = GetLocation(cursor);
                    if (loc is null || !ShouldProcessFile(loc.File))
                        return CXChildVisitResult.CXChildVisit_Continue;

                    // Get class/struct declarations
                    if (cursor.Kind == CXCursorKind.CXCursor_ClassDecl || cursor.Kind == CXCursorKind.CXCursor_StructDecl)
                    {
                        if (!cursor.IsDefinition)
                            cursor = cursor.Definition;
                        if (!cursor.IsDefinition)
                            return CXChildVisitResult.CXChildVisit_Continue;

                        var usr = GetUsr(cursor);
                        if (ClassCache.TryGetValue(usr, out var cached))
                        {
                            classes.Add(cached);
                            return CXChildVisitResult.CXChildVisit_Continue;
                        }

                        var (result, rawClass) = VisitClass(cursor, parent, usr);
                        if (rawClass is not null)
                        {
                            classes.Add(rawClass);
                            return result;
                        }
                    }

                    // Recurse into namespaces
                    if (cursor.Kind == CXCursorKind.CXCursor_Namespace)
                    {
                        return CXChildVisitResult.CXChildVisit_Recurse;
                    }

                    return CXChildVisitResult.CXChildVisit_Continue;
                }, new CXClientData(nint.Zero));
            }
            return [.. classes];
        }

        public ASTMethod[] GetMethods()
        {
            var methods = new List<ASTMethod>();
            unsafe
            {
                // Traverse the AST
                TranslationUnit.Cursor.VisitChildren((cursor, parent, data) =>
                {
                    var loc = GetLocation(cursor);
                    if (loc is null || !ShouldProcessFile(loc.File))
                        return CXChildVisitResult.CXChildVisit_Continue;

                    // Get methods/free functions
                    if (cursor.Kind == CXCursorKind.CXCursor_FunctionDecl)
                    {
                        var usr = GetUsr(cursor);
                        if (MethodCache.TryGetValue(usr, out var cached))
                        {
                            methods.Add(cached);
                            return CXChildVisitResult.CXChildVisit_Continue;
                        }

                        var (result, method) = VisitMethod(cursor, parent, usr);
                        if (method is not null)
                        {
                            methods.Add(method);
                            return result;
                        }
                    }
                    return CXChildVisitResult.CXChildVisit_Continue;
                }, new CXClientData(nint.Zero));
            }
            return [.. methods];
        }

        public ASTVariable[] GetVariables()
        {
            var variables = new List<ASTVariable>();
            unsafe
            {
                // Traverse the AST
                TranslationUnit.Cursor.VisitChildren((cursor, parent, data) =>
                {
                    var loc = GetLocation(cursor);
                    if (loc is null || !ShouldProcessFile(loc.File))
                        return CXChildVisitResult.CXChildVisit_Continue;

                    // Get variable declarations
                    if (cursor.Kind == CXCursorKind.CXCursor_VarDecl)
                    {
                        var usr = GetUsr(cursor);
                        if (VariableCache.TryGetValue(usr, out var cached))
                        {
                            variables.Add(cached);
                            return CXChildVisitResult.CXChildVisit_Continue;
                        }

                        if (cursor.Linkage == CXLinkageKind.CXLinkage_External && cursor.Language == CXLanguageKind.CXLanguage_C)
                            return CXChildVisitResult.CXChildVisit_Continue;

                        var (result, variable) = VisitVariable(cursor, parent, usr);
                        if (variable is not null)
                        {
                            variables.Add(variable);
                            return result;
                        }
                    }
                    return CXChildVisitResult.CXChildVisit_Continue;
                }, new CXClientData(nint.Zero));
            }
            return [.. variables];
        }

        private (CXChildVisitResult result, ASTClass? rawClass) VisitClass(CXCursor cursor, CXCursor parent, string usr)
        {
            // Get from cache if available
            if (ClassCache.TryGetValue(usr, out var cached))
                return (CXChildVisitResult.CXChildVisit_Continue, cached);

            ASTCursorLocation? location = GetLocation(cursor);

            // Collect members
            var (methodsCursors, variableCursors, baseCursors) = CollectClassMembers(cursor);

            // Find base classes
            List<ASTBaseSpecifier> baseClasses = [.. baseCursors
                .Select(c =>
                {
                    bool isVirtualBase = c.IsVirtualBase;
                    CXType type = c.Type;
                    CXCursor decl = type.Declaration;
                    ASTClass? classInfo = VisitClass(decl, decl.SemanticParent, decl.Usr.ToString()).rawClass;
                    return classInfo is not null ? new ASTBaseSpecifier()
                    {
                        Class = classInfo,
                        IsVirtualBase = isVirtualBase
                    } : null;
                }
            ).Where(t => t is not null)!];

            // Find methods
            List<ASTMethod> methods = [.. methodsCursors
                .Select(c => VisitMethod(c, cursor, c.Usr.ToString()).method
            ).Where(t => t is not null)!];

            // Find variables
            List<ASTVariable> variables = [.. variableCursors
                .Select(c => VisitVariable(c, cursor, c.Usr.ToString()).variable
            ).Where(t => t is not null)!];

            ASTClass rawClass = new()
            {
                Name = GetSpelling(cursor),
                RawComment = GetRawComment(cursor),
                Namespace = parent.Kind == CXCursorKind.CXCursor_Namespace ? GetFullNamespace(parent) : null,
                Location = location,
                DirectBaseClasses = [.. baseClasses],
                Methods = [.. methods],
                Variables = [.. variables]
            };

            rawClass.Methods.ToList().ForEach(m => m.DeclaringClass = rawClass);
            rawClass.Variables.ToList().ForEach(v => v.DeclaringClass = rawClass);

            ClassCache[usr] = rawClass;
            return (CXChildVisitResult.CXChildVisit_Recurse, rawClass);
        }

        private (CXChildVisitResult result, ASTMethod? method) VisitMethod(CXCursor cursor, CXCursor parent, string usr)
        {
            // Get from cache if available
            if (MethodCache.TryGetValue(usr, out var cached))
                return (CXChildVisitResult.CXChildVisit_Continue, cached);

            ASTMethod? overrideOf = null;
            if (cursor.CXXMethod_IsVirtual)
            {
                var overriden = cursor.OverriddenCursors;
                if (overriden.Length > 0)
                {
                    var first = overriden[0];
                    var (_, overrideOfMethod) = VisitMethod(first, first.SemanticParent, first.Usr.ToString());
                    overrideOf = overrideOfMethod;
                }
            }

            ASTMethod method = new()
            {
                Name = GetSpelling(cursor),
                MangledName = GetMangledName(cursor),
                Location = GetLocation(cursor),
                DeclaringClass = null,
                IsVirtual = cursor.CXXMethod_IsVirtual,
                RawComment = GetRawComment(cursor),
                IsImported = IsImported(cursor),
                Namespace = parent.Kind == CXCursorKind.CXCursor_Namespace ? GetFullNamespace(parent) : null,
                IsDestructor = cursor.Kind == CXCursorKind.CXCursor_Destructor,
                IsConstructor = cursor.Kind == CXCursorKind.CXCursor_Constructor,
                OverrideOf = overrideOf
            };

            MethodCache[usr] = method;
            return (CXChildVisitResult.CXChildVisit_Continue, method);
        }

        public (CXChildVisitResult result, ASTVariable? variable) VisitVariable(CXCursor cursor, CXCursor parent, string usr)
        {
            // Get from cache if available
            if (VariableCache.TryGetValue(usr, out var cached))
                return (CXChildVisitResult.CXChildVisit_Continue, cached);

            ASTVariable variable = new()
            {
                Name = GetSpelling(cursor),
                MangledName = GetMangledName(cursor),
                Location = GetLocation(cursor),
                DeclaringClass = null,
                RawComment = GetRawComment(cursor),
                IsImported = IsImported(cursor),
                Namespace = parent.Kind == CXCursorKind.CXCursor_Namespace ? GetFullNamespace(parent) : null,
                IsStatic = cursor.StorageClass == CX_StorageClass.CX_SC_Static
            };

            VariableCache[usr] = variable;
            return (CXChildVisitResult.CXChildVisit_Continue, variable);
        }

        public void Reset()
        {
            SpellingCache.Clear();
            LocationCache.Clear();
            ClassCache.Clear();
            VariableCache.Clear();
            MethodCache.Clear();
            MangleCache.Clear();
            RawCommentCache.Clear();
            IsImportedCache.Clear();
        }
    }
}
