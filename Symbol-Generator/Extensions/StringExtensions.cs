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

        public static string TrimSingle(this string s, char c)
        {
            if (s.Length >= 2 && s[0] == c && s[^1] == c)
                return s.Substring(1, s.Length - 2);
            return s;
        }
    }
}
