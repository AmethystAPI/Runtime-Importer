using AsmResolver.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.ModuleTweaker.Utility {
    public static class Utils {
        public static BinaryReader ToReader(this BinaryStreamReader reader) {
            var ms = new MemoryStream(reader.ReadToEnd());
            return new BinaryReader(ms, Encoding.UTF8, false);
        }

        public static BinaryWriter ToWriter(this BinaryStreamWriter writer) {
            return new BinaryWriter(writer.BaseStream, Encoding.UTF8, false);
        }
    }
}
