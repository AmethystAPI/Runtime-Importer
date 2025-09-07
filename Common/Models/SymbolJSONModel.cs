using Newtonsoft.Json;

namespace Amethyst.Common.Models
{
    public class SymbolJSONModel
    {
        [JsonProperty("format_version")]
        public int FormatVersion { get; set; } = 1;

        [JsonProperty("functions")]
        public List<MethodSymbolJSONModel> Functions { get; set; } = [];
    }
}
