using Newtonsoft.Json;

namespace Amethyst.Common.Models
{
    public class SymbolJSONModel
    {
        [JsonProperty("format_version")]
        public int FormatVersion { get; set; } = 1;

        [JsonProperty("functions")]
        public List<MethodSymbolJSONModel> Functions { get; set; } = [];

        [JsonProperty("variables")]
        public List<VariableSymbolJSONModel> Variables { get; set; } = [];

        [JsonProperty("vtables")]
        public List<VirtualTableSymbolJSONModel> VirtualTables { get; set; } = [];

        [JsonProperty("virtual_functions")]
        public List<VirtualIndexSymbolJSONModel> VirtualFunctions { get; set; } = [];
    }
}
