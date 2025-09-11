using Newtonsoft.Json;

namespace Amethyst.Common.Models
{
    public class SymbolJSONModel
    {
        [JsonProperty("format_version")]
        public int FormatVersion { get; set; } = 1;

        [JsonProperty("functions")]
        public List<FunctionSymbolModel> Functions { get; set; } = [];

        [JsonProperty("variables")]
        public List<VariableSymbolModel> Variables { get; set; } = [];

        [JsonProperty("vtables")]
        public List<VirtualTableSymbolModel> VirtualTables { get; set; } = [];

        [JsonProperty("virtual_functions")]
        public List<VirtualFunctionSymbolModel> VirtualFunctions { get; set; } = [];
    }
}
