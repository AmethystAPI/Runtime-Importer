using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Extensions
{
    public static class StringExtensions
    {
        public static IEnumerable<string> SplitNonIntrusive(this string str)
        {
            List<string> strings = [];
            var matches = Regex.Matches(str, @"[\""].+?[\""]|\S+");
            foreach (Match match in matches)
            {
                strings.Add(match.Value);
            }
            return strings;
        }

        public static string NormalizeSlashes(this string str)
        {
            return str.Replace('\\', '/');
        }
    }
}
