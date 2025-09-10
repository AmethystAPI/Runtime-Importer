using Amethyst.SymbolGenerator.Parsing.Annotations.Comments;
using System.Text.RegularExpressions;

namespace Amethyst.SymbolGenerator.Parsing.Annotations
{
    public static partial class CommentParser
    {
        public static IEnumerable<RawAnnotation> ParseAnnotations(string comment, ASTCursorLocation location)
        {
            using var sr = new StringReader(comment);
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();
                Match match = AnnotationRegex().Match(line);
                if (!match.Success)
                {
                    continue;
                }

                string name = match.Groups[1].Value;
                string? parameters = match.Groups[2].Success ? match.Groups[2].Value : null;
                IEnumerable<string> parts = (parameters?.Split(',') ?? []).Select(a => a.Trim().TrimStart('"').TrimEnd('"'));
                yield return new RawAnnotation(name, parts, location);
            }
        }

        [GeneratedRegex(@"@([^ ]+)(?:\s*{\s*([^}]*)\s*})?")]
        private static partial Regex AnnotationRegex();
    }
}
