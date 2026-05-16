namespace Amethyst.Common.Tracking
{
    public class FileChange(ChangeType type, string path, string? content = null)
    {
        public ChangeType ChangeType { get; set; } = type;
        public string FilePath { get; set; } = path;
        // Populated by FileTracker so downstream consumers don't have to re-read the file.
        public string? Content { get; set; } = content;
    }
}
