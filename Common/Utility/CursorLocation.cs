using Amethyst.Common.Extensions;

namespace Amethyst.Common.Utility {
    public class CursorLocation
    {
        public string File { get; set; }
        public uint Line { get; set; }
        public uint Column { get; set; }

        public CursorLocation(string file, uint line, uint column)
        {
            if (string.IsNullOrEmpty(file) || !System.IO.File.Exists(file))
                File = "<unknown>";
            else
                File = Path.GetFullPath(file).NormalizeSlashes();
            Line = line;
            Column = column;
        }

        override public string ToString()
        {
            if (File == "<unknown>")
                return "";
            return $"{File}({Line},{Column})";
        }
    }
}
