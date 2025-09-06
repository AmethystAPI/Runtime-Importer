using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing
{
    public class ASTBaseSpecifier
    {
        public ASTClass Class { get; set; } = null!;
        public bool IsVirtualBase { get; set; } = false;
    }
}
