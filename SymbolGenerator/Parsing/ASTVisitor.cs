using Amethyst.Common.Diagnostics;
using Amethyst.Common.Extensions;
using ClangSharp.Interop;

namespace Amethyst.SymbolGenerator.Parsing
{
    public class ASTVisitor
    {
        private Dictionary<string, string> SpellingCache = [];
        private Dictionary<string, string> MangleCache = [];
        private Dictionary<string, ASTCursorLocation> LocationCache = [];
        private Dictionary<string, string> RawCommentCache = [];
        private Dictionary<string, bool> IsImportedCache = [];

        private Dictionary<string, ASTClass> ClassCache = [];
        private Dictionary<string, ASTMethod> MethodCache = [];
        private Dictionary<string, ASTVariable> VariableCache = [];

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
                CXTranslationUnit_Flags.CXTranslationUnit_DetailedPreprocessingRecord,
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

        public string GetSpelling(CXCursor cursor)
        {
            string usr = cursor.Usr.ToString();
            if (SpellingCache.TryGetValue(usr, out var cached))
                return cached;
            var spelling = cursor.Spelling.ToString();
            SpellingCache[usr] = spelling;
            return spelling;
        }

        public ASTCursorLocation? GetLocation(CXCursor cursor)
        {
            string usr = cursor.Usr.ToString();
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

            cursor = GetDefinition(cursor);

            switch (cursor.Kind)
            {
                case CXCursorKind.CXCursor_FunctionDecl:
                case CXCursorKind.CXCursor_CXXMethod:
                case CXCursorKind.CXCursor_Constructor:
                case CXCursorKind.CXCursor_Destructor:
                case CXCursorKind.CXCursor_VarDecl:
                case CXCursorKind.CXCursor_ConversionFunction:
                case CXCursorKind.CXCursor_FirstDecl:
                    break;
                default:
                    return "Unknown";
            }

            string usr = cursor.Usr.ToString();
            if (MangleCache.TryGetValue(usr, out var cached))
                return cached;

            string mangled = cursor.Mangling.ToString();
            
            MangleCache[usr] = mangled;
            return mangled;
        }

        public string? GetRawComment(CXCursor cursor)
        {
            string usr = cursor.Usr.ToString();
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
            string usr = cursor.Usr.ToString();
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

        public bool HasBody(CXCursor cursor)
        {
            if (cursor.Kind == CXCursorKind.CXCursor_VarDecl)
            {
                return cursor.IsDefinition;
            }

            if (cursor.Kind != CXCursorKind.CXCursor_FunctionDecl &&
                cursor.Kind != CXCursorKind.CXCursor_CXXMethod &&
                cursor.Kind != CXCursorKind.CXCursor_Constructor &&
                cursor.Kind != CXCursorKind.CXCursor_Destructor)
                return false;
            bool hasBody = cursor.HasBody;
            if (hasBody)
                return true;
            unsafe
            {
                cursor.VisitChildren((cursor, parent, data) =>
                {
                    if (cursor.Kind == CXCursorKind.CXCursor_CompoundStmt)
                    {
                        hasBody = true;
                        return CXChildVisitResult.CXChildVisit_Break;
                    }
                    return CXChildVisitResult.CXChildVisit_Continue;
                }, new CXClientData(nint.Zero));
            }
            return hasBody;
        }

        public string? GetFullNamespace(CXCursor cursor)
        {
            if (cursor.Kind != CXCursorKind.CXCursor_Namespace)
                return null;

            var names = new List<string>();
            var current = cursor;
            while (!current.IsNull && current.Kind == CXCursorKind.CXCursor_Namespace)
            {
                names.Add(current.Spelling.ToString());
                current = current.SemanticParent;
            }
            names.Reverse();
            return string.Join("::", names);
        }

        public CXCursor GetDefinition(CXCursor cursor)
        {
            if (cursor.IsNull || cursor.IsInvalid || cursor.IsDefinition)
                return cursor;
            CXCursor old = cursor;
            if (!cursor.IsDefinition && cursor.IsDeclaration)
            {
                old = cursor;
                cursor = cursor.CanonicalCursor;
            }
            if (cursor.IsNull || cursor.IsInvalid)
                cursor = old;
            return cursor;
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
                    ASTCursorLocation? location = GetLocation(cursor);

                    // Ensure that the class is in the input directory
                    string file = location?.File ?? string.Empty;
                    if (file.StartsWith(InputDirectory, StringComparison.Ordinal) == false || !StrictHeaders.Contains(file))
                        return CXChildVisitResult.CXChildVisit_Continue;

                    // Get class/struct declarations
                    if (cursor.Kind == CXCursorKind.CXCursor_ClassDecl || cursor.Kind == CXCursorKind.CXCursor_StructDecl)
                    {
                        var usr = cursor.Usr.ToString();
                        var (result, rawClass) = VisitClass(cursor, parent, usr);
                        if (rawClass is not null)
                        {
                            classes.Add(rawClass);
                            return result;
                        }
                    }
                    return CXChildVisitResult.CXChildVisit_Recurse;
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
                    ASTCursorLocation? location = GetLocation(cursor);

                    // Ensure that the class is in the input directory
                    string file = location?.File ?? string.Empty;
                    if (file.StartsWith(InputDirectory, StringComparison.Ordinal) == false || !StrictHeaders.Contains(file))
                        return CXChildVisitResult.CXChildVisit_Continue;

                    // Get methods/free functions
                    if (cursor.Kind == CXCursorKind.CXCursor_FunctionDecl)
                    {
                        var usr = cursor.Usr.ToString();
                        var (result, method) = VisitMethod(cursor, parent, usr);
                        if (method is not null)
                        {
                            methods.Add(method);
                            return result;
                        }
                    }
                    return CXChildVisitResult.CXChildVisit_Recurse;
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
                    ASTCursorLocation? location = GetLocation(cursor);

                    // Ensure that the class is in the input directory
                    string file = location?.File ?? string.Empty;
                    if (file.StartsWith(InputDirectory, StringComparison.Ordinal) == false || !StrictHeaders.Contains(file))
                        return CXChildVisitResult.CXChildVisit_Continue;

                    // Get variable declarations
                    if (cursor.Kind == CXCursorKind.CXCursor_VarDecl)
                    {
                        if (cursor.Linkage == CXLinkageKind.CXLinkage_External && cursor.Language == CXLanguageKind.CXLanguage_C)
                            return CXChildVisitResult.CXChildVisit_Continue;

                        var usr = cursor.Usr.ToString();
                        var (result, variable) = VisitVariable(cursor, parent, usr);
                        if (variable is not null)
                        {
                            variables.Add(variable);
                            return result;
                        }
                    }
                    return CXChildVisitResult.CXChildVisit_Recurse;
                }, new CXClientData(nint.Zero));
            }
            return [.. variables];
        }

        private (CXChildVisitResult result, ASTClass? rawClass) VisitClass(CXCursor cursor, CXCursor parent, string usr)
        {
            cursor = GetDefinition(cursor);

            // Get from cache if available
            if (ClassCache.TryGetValue(usr, out var cached))
                return (CXChildVisitResult.CXChildVisit_Continue, cached);

            ASTCursorLocation? location = GetLocation(cursor);

            // Ensure that the class is in the input directory
            string file = location?.File ?? string.Empty;
            if (file.StartsWith(InputDirectory, StringComparison.Ordinal) == false || !StrictHeaders.Contains(file))
                return (CXChildVisitResult.CXChildVisit_Continue, null);

            // Collect members
            var (methodsCursors, variableCursors, baseCursors) = CollectClassMembers(cursor);

            // Find base classes
            ASTBaseSpecifier[] baseClasses = [.. baseCursors
                .Select(c =>
                {
                    bool isVirtualBase = c.IsVirtualBase;
                    CXType type = c.Type;
                    CXCursor decl = type.Declaration;
                    ASTClass? classInfo = VisitClass(decl, decl.SemanticParent, decl.Usr.ToString()).rawClass;
                    if (classInfo is null) {
                        return null;
                    }
                    return new ASTBaseSpecifier
                    {
                        Class = classInfo,
                        IsVirtualBase = isVirtualBase
                    };
                }
            ).Where(t => t is not null)!];

            // Find methods
            ASTMethod[] methods = [.. methodsCursors
                .Select(c =>
                {
                    var (result, method) = VisitMethod(c, cursor, c.Usr.ToString());
                    return method;
                }
            ).Where(t => t is not null)!];

            // Find variables
            ASTVariable[] variables = [.. variableCursors
                .Select(c =>
                {
                    var (result, variable) = VisitVariable(c, cursor, c.Usr.ToString());
                    return variable;
                }
            ).Where(t => t is not null)!];

            string name = GetSpelling(cursor);
            ASTClass rawClass = new()
            {
                Name = name,
                Namespace = parent.Kind == CXCursorKind.CXCursor_Namespace ? GetFullNamespace(parent) : null,
                Location = location,
                DirectBaseClasses = baseClasses,
                Methods = methods
            };

            foreach (var method in rawClass.Methods)
            {
                method.DeclaringClass = rawClass;
            }

            foreach (var variable in variables)
            {
                variable.DeclaringClass = rawClass;
            }

            ClassCache[usr] = rawClass;
            return (CXChildVisitResult.CXChildVisit_Recurse, rawClass);
        }

        private (CXChildVisitResult result, ASTMethod? method) VisitMethod(CXCursor cursor, CXCursor parent, string usr)
        {
            cursor = GetDefinition(cursor);

            // Get from cache if available
            if (MethodCache.TryGetValue(usr, out var cached))
                return (CXChildVisitResult.CXChildVisit_Continue, cached);

            ASTCursorLocation? location = GetLocation(cursor);

            // Ensure that the function is in the input directory
            string file = location?.File ?? string.Empty;
            if (file.StartsWith(InputDirectory, StringComparison.Ordinal) == false || !StrictHeaders.Contains(file))
                return (CXChildVisitResult.CXChildVisit_Continue, null);

            string name = GetSpelling(cursor);
            string mangledName = GetMangledName(cursor);
            string? rawComment = GetRawComment(cursor);
            bool isImported = IsImported(cursor);
            bool isDestructor = cursor.Kind == CXCursorKind.CXCursor_Destructor;
            string? namespaceName = null;
            if (parent.Kind == CXCursorKind.CXCursor_Namespace)
            {
                namespaceName = GetFullNamespace(parent);
            }

            // Try to find the declaring class
            string? declaringClassUsr = null;
            if (parent.Kind == CXCursorKind.CXCursor_ClassDecl || parent.Kind == CXCursorKind.CXCursor_StructDecl)
            {
                declaringClassUsr = parent.Usr.ToString();
            }

            ASTMethod method = new()
            {
                Name = name,
                MangledName = mangledName,
                Location = location,
                DeclaringClass = null,
                IsVirtual = cursor.CXXMethod_IsVirtual,
                RawComment = rawComment,
                IsImported = isImported,
                HasBody = HasBody(cursor),
                Namespace = namespaceName,
                IsDestructor = isDestructor
            };

            MethodCache[usr] = method;
            return (CXChildVisitResult.CXChildVisit_Continue, method);
        }

        public (CXChildVisitResult result, ASTVariable? variable) VisitVariable(CXCursor cursor, CXCursor parent, string usr)
        {
            cursor = GetDefinition(cursor);

            // Get from cache if available
            if (VariableCache.TryGetValue(usr, out var cached))
            {
                if (!cached.HasDefinition && cursor.IsDefinition)
                {
                    cached.HasDefinition = true;
                }
                return (CXChildVisitResult.CXChildVisit_Continue, cached);
            }

            if (!cursor.IsDefinition)
                cursor = cursor.CanonicalCursor;

            ASTCursorLocation? location = GetLocation(cursor);
            // Ensure that the variable is in the input directory
            string file = location?.File ?? string.Empty;
            if (file.StartsWith(InputDirectory, StringComparison.Ordinal) == false || !StrictHeaders.Contains(file))
                return (CXChildVisitResult.CXChildVisit_Continue, null);

            string name = GetSpelling(cursor);
            string mangledName = GetMangledName(cursor);
            string? rawComment = GetRawComment(cursor);
            bool isImported = IsImported(cursor);
            string? namespaceName = null;

            if (parent.Kind == CXCursorKind.CXCursor_Namespace)
            {
                namespaceName = GetFullNamespace(parent);
            }
            
            ASTVariable variable = new()
            {
                Name = name,
                MangledName = mangledName,
                Location = location,
                DeclaringClass = null,
                RawComment = rawComment,
                IsImported = isImported,
                HasDefinition = false,
                Namespace = namespaceName,
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
