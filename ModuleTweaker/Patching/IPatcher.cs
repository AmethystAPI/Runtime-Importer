using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Patching {
    public interface IPatcher {
        bool IsPatched();
        bool RemoveSection(string name);
        bool Patch();
        bool Unpatch();
        bool IsCustomSection(string name);
    }
}
