using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Parsing
{
    public record ASTCursorLocation(string File, uint Line, uint Column, uint Offset);
}
