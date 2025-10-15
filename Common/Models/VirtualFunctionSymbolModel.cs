using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.Common.Models
{
    public class VirtualFunctionSymbolModel
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("vtable")]
        public string VirtualTable { get; set; } = string.Empty;

        [JsonIgnore]
        public bool Inherit { get; set; } = false;

        [JsonIgnore]
        public string? Overrides { get; set; }

        [JsonProperty("index")]
        public uint Index { get; set; } = 0;

        [JsonProperty("is_vdtor")]
        public bool IsVirtualDestructor { get; set; } = false;
    }
}
