using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.Common.Utility {
    public enum PlatformType {
        WinClient,
        WinServer,
        WinAny
    }

    public static class PlatformUtility {
        public static bool TryParse(string str, out PlatformType type) {
            type = PlatformType.WinClient;
            switch (str) {
                case "win-client" or "wc":
                    type = PlatformType.WinClient;
                    return true;
                case "win-server" or "ws":
                    type = PlatformType.WinServer;
                    return true;
                case "win-any" or "wa":
                    type = PlatformType.WinAny;
                    return true;
                default:
                    return false;
            }
        }
    }
}
