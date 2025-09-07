namespace Amethyst.Common.Tracking
{
    public class FileChange(ChangeType type, string path)
    {
        public ChangeType ChangeType { get; set; } = type;
        public string FilePath { get; set; } = path;
    }
}
