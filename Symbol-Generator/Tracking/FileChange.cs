using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Tracking
{
    public class FileChange(ChangeType type, string path)
    {
        public ChangeType ChangeType { get; set; } = type;
        public string FilePath { get; set; } = path;
    }
}
