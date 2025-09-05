using Amethyst.SymbolGenerator.Extensions;
using K4os.Hash.xxHash;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amethyst.SymbolGenerator.Tracking
{
    /// <summary>
    /// Tracks changes to files.
    /// </summary>
    public class FileTracker
    {
        public DirectoryInfo InputDirectory { get; private set; }
        public FileInfo ChecksumFile { get; private set; }
        public string[] SearchPatterns { get; private set; }

        public FileTracker(DirectoryInfo inputDirectory, FileInfo checksumFile, string[] searchPatterns)
        {
            ArgumentNullException.ThrowIfNull(inputDirectory);
            ArgumentNullException.ThrowIfNull(checksumFile);
            ArgumentNullException.ThrowIfNull(searchPatterns);
            if (inputDirectory.Exists is false)
                throw new DirectoryNotFoundException($"Input directory '{inputDirectory.FullName}' does not exist.");
            InputDirectory = inputDirectory;
            ChecksumFile = checksumFile;
            SearchPatterns = searchPatterns;
        }

        public IEnumerable<FileChange> TrackChanges()
        {
            // Load existing checksums
            Dictionary<string, ulong> lastChecksums = LoadChecksums();
            Dictionary<string, ulong> newChecksums = [];

            // Collect all files matching the search patterns
            IEnumerable<FileInfo> files = SearchPatterns
                .SelectMany(p => InputDirectory.EnumerateFiles(p, SearchOption.AllDirectories))
                .Select(f => f);

            // Check each file for changes
            foreach (var file in files)
            {
                string filePath = file.FullName.NormalizeSlashes();
                string content = File.ReadAllText(file.FullName);
                ulong hash = XXH64.DigestOf(Encoding.UTF8.GetBytes(content));

                newChecksums[filePath] = hash;
                if (!lastChecksums.TryGetValue(filePath, out var lastHash))
                {
                    yield return new FileChange(ChangeType.Added, filePath);
                }
                else if (lastHash != hash)
                {
                    yield return new FileChange(ChangeType.Modified, filePath);
                }
            }

            // Check for deleted files
            foreach (var lastFile in lastChecksums.Keys)
            {
                if (!newChecksums.ContainsKey(lastFile))
                {
                    yield return new FileChange(ChangeType.Deleted, lastFile);
                }
            }

            // Save the new checksums
            SaveChecksums(newChecksums);
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
            var json = JsonConvert.SerializeObject(checksums, Formatting.Indented);
            if (ChecksumFile.DirectoryName is not string checksumDirectory)
                throw new Exception("Failed to get directory name for checksum file.");
            Directory.CreateDirectory(checksumDirectory);
            File.WriteAllText(ChecksumFile.FullName, json);
        }
    }
}
