using Newtonsoft.Json;

namespace Amethyst.Common.Models
{
    /// <summary>
    /// All extracted symbols, keyed by absolute header path. One file = one entry; the entry
    /// is fully replaced on re-parse. Persisted as a single file (replaces the legacy
    /// per-header *.symbols.json files).
    /// </summary>
    public class SymbolCache
    {
        [JsonProperty("entries")]
        public Dictionary<string, SymbolJSONModel> Entries { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public static SymbolCache Load(FileInfo file)
        {
            if (!file.Exists) return new SymbolCache();
            try
            {
                return JsonConvert.DeserializeObject<SymbolCache>(File.ReadAllText(file.FullName)) ?? new SymbolCache();
            }
            catch
            {
                return new SymbolCache();
            }
        }

        public void Save(FileInfo file)
        {
            Directory.CreateDirectory(file.DirectoryName!);
            File.WriteAllText(file.FullName, JsonConvert.SerializeObject(this, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            }));
        }
    }
}
