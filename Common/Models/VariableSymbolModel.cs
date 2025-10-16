using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.Common.Models
{
    public class VariableSymbolModel
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("address")]
        public string? Address { get; set; } = null;

        [JsonProperty("is_vaddress")]
        public bool IsVirtualTableAddress { get; set; } = false;

        [JsonProperty("signature")]
        public string? Signature { get; set; } = null;
    }
}
