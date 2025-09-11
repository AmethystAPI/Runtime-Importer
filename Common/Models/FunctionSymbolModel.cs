using Newtonsoft.Json;

namespace Amethyst.Common.Models
{
    public class FunctionSymbolModel
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("address")]
        public string? Address { get; set; }

        [JsonProperty("signature")]
        public string? Signature { get; set; }
    }
}
