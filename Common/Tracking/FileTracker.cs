using Amethyst.Common.Extensions;
using K4os.Hash.xxHash;
using Newtonsoft.Json;
using System.Text;

namespace Amethyst.Common.Tracking
{
    /// <summary>
    /// Tracks changes to files.
    /// </summary>
    public class FileTracker
    {
        public const int CurrentVersion = 1;

        public DirectoryInfo InputDirectory { get; private set; }
        public FileInfo ChecksumFile { get; private set; }
        public string[] SearchPatterns { get; private set; }
        public string[] Filters { get; private set; }

        public FileTracker(DirectoryInfo inputDirectory, FileInfo checksumFile, string[] searchPatterns, string[] filters)
        {
            ArgumentNullException.ThrowIfNull(inputDirectory);
            ArgumentNullException.ThrowIfNull(checksumFile);
            ArgumentNullException.ThrowIfNull(searchPatterns);
            if (inputDirectory.Exists is false)
                throw new DirectoryNotFoundException($"Input directory '{inputDirectory.FullName}' does not exist.");
            InputDirectory = inputDirectory;
            ChecksumFile = checksumFile;
            SearchPatterns = searchPatterns;
            Filters = filters;
        }

        public (FileChange[] Changes, Dictionary<string, ulong> NewChecksums) TrackChanges()
        {
            List<FileChange> changes = [];
            // Load existing checksums
            Dictionary<string, ulong> lastChecksums = LoadChecksums();
            Dictionary<string, ulong> newChecksums = [];
            if (lastChecksums.TryGetValue("__version", out var version))
            {
                // Version found, check if it matches current version
                lastChecksums.Remove("__version");
                if (version != CurrentVersion)
                {
                    // Version mismatch, reset checksums
                    lastChecksums = [];
                }
            }
            else
            {
                // No version found, assume old format and reset
                lastChecksums = [];
            }

            // Collect all files matching the search patterns
            IEnumerable<FileInfo> files = SearchPatterns
                .SelectMany(p => InputDirectory.EnumerateFiles(p, SearchOption.AllDirectories))
                .Select(f => f);

            // Check each file for changes
            foreach (var file in files)
            {
                if (Filters.Any() && !Filters.Any(f => Path.GetRelativePath(InputDirectory.FullName, file.FullName).StartsWith(f)))
                    continue;
                string filePath = file.FullName.NormalizeSlashes();
#if !DEBUG
                string content = File.ReadAllText(file.FullName);
                ulong hash = XXH64.DigestOf(Encoding.UTF8.GetBytes(content));

                newChecksums[filePath] = hash;
                if (!lastChecksums.TryGetValue(filePath, out var lastHash))
                {
                    changes.Add(new FileChange(ChangeType.Added, filePath));
                }
                else if (lastHash != hash)
                {
                    changes.Add(new FileChange(ChangeType.Modified, filePath));
                }
#else
                // In debug mode, treat all files as modified to simplify testing
                changes.Add(new FileChange(ChangeType.Modified, filePath));
#endif
            }

            // Check for deleted files
            foreach (var lastFile in lastChecksums.Keys)
            {
                if (!newChecksums.ContainsKey(lastFile))
                {
                    changes.Add(new FileChange(ChangeType.Deleted, lastFile));
                }
            }

            return (changes.ToArray(), newChecksums);
        }

        public Dictionary<string, ulong> LoadChecksums()
        {
            if (!ChecksumFile.Exists)
            {
                return [];
            }
            string json = File.ReadAllText(ChecksumFile.FullName);
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, ulong>>(json) ?? [];
            }
            catch
            {
                return [];
            }
        }

        public void SaveChecksums(Dictionary<string, ulong> checksums)
        {
            // Always set the current version
            checksums["__version"] = CurrentVersion;
            var json = JsonConvert.SerializeObject(checksums, Formatting.Indented);
            if (ChecksumFile.DirectoryName is not string checksumDirectory)
                throw new Exception("Failed to get directory name for checksum file.");
            Directory.CreateDirectory(checksumDirectory);
            File.WriteAllText(ChecksumFile.FullName, json);
        }
    }
}
