using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.Common.Models
{
    public class VirtualIndexSymbolJSONModel
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("vftable")]
        public string VirtualTable { get; set; } = string.Empty;

        [JsonIgnore]
        public bool Inherit { get; set; } = false;

        [JsonIgnore]
        public string? Overrides { get; set; }

        [JsonProperty("index")]
        public uint Index { get; set; } = 0;
    }
}
