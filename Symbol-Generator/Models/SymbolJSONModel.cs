using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Models
{
    public class SymbolJSONModel
    {
        [JsonProperty("format_version")]
        public int FormatVersion { get; set; } = 1;

        [JsonProperty("functions")]
        public List<MethodSymbolJSONModel> Functions { get; set; } = [];
    }
}
