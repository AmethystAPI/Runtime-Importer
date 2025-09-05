using Amethyst.SymbolGenerator.Diagnostics;
using Amethyst.SymbolGenerator.Extensions;
using ClangSharp;
using ClangSharp.Interop;
using CliFx.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Amethyst.SymbolGenerator.Parsing
{
    public class ASTVisitor
    {
        private Dictionary<string, string> SpellingCache = [];
        private Dictionary<string, ASTCursorLocation> LocationCache = [];

        private Dictionary<string, ASTRawClass> ClassCache = [];

        public CXIndex Index { get; private set; }
        public CXTranslationUnit TranslationUnit { get; private set; }
        public string InputDirectory { get; private set; }

        public ASTVisitor(string inputFile, string inputDirectory, IEnumerable<string> arguments)
        {
            Index = CXIndex.Create();
            var error = CXTranslationUnit.TryParse(
                Index,
                inputFile,
                arguments.ToArray(),
                [],
                CXTranslationUnit_Flags.CXTranslationUnit_DetailedPreprocessingRecord |
                CXTranslationUnit_Flags.CXTranslationUnit_SkipFunctionBodies,
                out var translationUnit);
            if (error != CXErrorCode.CXError_Success)
                throw new Exception($"Failed to parse translation unit. Error code: {error}");
            TranslationUnit = translationUnit;
            InputDirectory = inputDirectory.NormalizeSlashes();
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

        public ASTCursorLocation GetLocation(CXCursor cursor)
        {
            if (!cursor.IsDefinition)
                cursor = cursor.Definition;
            string usr = cursor.Usr.ToString();
            if (LocationCache.TryGetValue(usr, out var cached))
                return cached;
            cursor.Location.GetFileLocation(out var file, out var line, out var column, out var offset);
            var location = new ASTCursorLocation(file.ToString().NormalizeSlashes(), line, column, offset);
            LocationCache[usr] = location;
            return location;
        }

        public CXCursor[] GetBaseClasses(CXCursor cursor)
        {
            if (cursor.Kind != CXCursorKind.CXCursor_ClassDecl && cursor.Kind != CXCursorKind.CXCursor_StructDecl)
                throw new ArgumentException("Cursor must be a class or struct declaration.", nameof(cursor));

            var bases = new List<CXCursor>();
            unsafe
            {
                // Visit children to find base specifiers
                cursor.VisitChildren((c, parent, data) =>
                {
                    if (c.Kind == CXCursorKind.CXCursor_CXXBaseSpecifier)
                    {
                        var type = c.Type;
                        var declaration = type.Declaration;
                        if (declaration.Kind == CXCursorKind.CXCursor_ClassDecl || declaration.Kind == CXCursorKind.CXCursor_StructDecl)
                        {
                            bases.Add(declaration);
                        }
                    }
                    return CXChildVisitResult.CXChildVisit_Continue;
                }, new CXClientData(nint.Zero));
            }
            return [.. bases];
        }

        public ASTRawClass[] GetClasses()
        {
            ClassCache.Clear();
            var classes = new List<ASTRawClass>();
            unsafe
            {
                // Traverse the AST
                TranslationUnit.Cursor.VisitChildren((cursor, parent, data) =>
                {
                    // Get class/struct declarations
                    if (cursor.Kind == CXCursorKind.CXCursor_ClassDecl || cursor.Kind == CXCursorKind.CXCursor_StructDecl)
                    {
                        if (!cursor.IsDefinition)
                            cursor = cursor.Definition;

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

        private (CXChildVisitResult result, ASTRawClass? rawClass) VisitClass(CXCursor cursor, CXCursor parent, string usr)
        {
            // Get from cache if available
            if (ClassCache.TryGetValue(usr, out var cached))
                return (CXChildVisitResult.CXChildVisit_Continue, cached);

            ASTCursorLocation location = GetLocation(cursor);

            // Ensure that the class is in the input directory
            if (!location.File.StartsWith(InputDirectory, StringComparison.Ordinal))
                return (CXChildVisitResult.CXChildVisit_Continue, null);

            string name = GetSpelling(cursor);

            // Find base classes
            ASTRawClass[] baseClasses = [.. GetBaseClasses(cursor)
                .Select(c =>
                {
                    return VisitClass(c, parent, c.Usr.ToString()).rawClass;
                }
            ).Where(t => t is not null)!];

            var rawClass = new ASTRawClass
            {
                Name = name,
                Namespace = parent.Kind == CXCursorKind.CXCursor_Namespace ? GetSpelling(parent) : null,
                Location = location,
                DirectBaseClasses = baseClasses
            };

            ClassCache[usr] = rawClass;
            return (CXChildVisitResult.CXChildVisit_Recurse, rawClass);
        }
    }
}
