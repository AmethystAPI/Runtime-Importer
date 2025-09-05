using ClangSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing
{
    public class ASTVisitor
    {
        public CXIndex Index { get; private set; }
        public CXTranslationUnit TranslationUnit { get; private set; }

        public ASTVisitor(string input, IEnumerable<string> arguments)
        {
            Index = CXIndex.Create();
            var error = CXTranslationUnit.TryParse(
                Index,
                input,
                arguments.ToArray(),
                [],
                CXTranslationUnit_Flags.CXTranslationUnit_DetailedPreprocessingRecord |
                CXTranslationUnit_Flags.CXTranslationUnit_SkipFunctionBodies,
                out var translationUnit);
            if (error != CXErrorCode.CXError_Success)
                throw new Exception($"Failed to parse translation unit. Error code: {error}");
            TranslationUnit = translationUnit;
        }

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
    }
}
