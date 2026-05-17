using Amethyst.Common.Extensions;
using K4os.Hash.xxHash;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Reflection;

namespace Amethyst.Common.Tracking
{
    /// <summary>
    /// Tracks changes to files across one or more input directories.
    /// </summary>
    public class FileTracker
    {
        public static Version AssemblyVersion => Assembly.GetEntryAssembly()?.GetName()?.Version ?? new Version(1, 0, 0);
        public static ulong CurrentVersion
        {
            get
            {
                Version version = AssemblyVersion;
                return (ulong)((version.Major << 32) | (version.Minor << 16) | version.Build);
            }
        }

        public DirectoryInfo[] InputDirectories { get; private set; }
        public FileInfo ChecksumFile { get; private set; }
        public string[] SearchPatterns { get; private set; }
        public string[] Filters { get; private set; }

        public FileTracker(DirectoryInfo[] inputDirectories, FileInfo checksumFile, string[] searchPatterns, string[] filters)
        {
            ArgumentNullException.ThrowIfNull(inputDirectories);
            ArgumentNullException.ThrowIfNull(checksumFile);
            ArgumentNullException.ThrowIfNull(searchPatterns);
            if (inputDirectories.Length == 0)
                throw new ArgumentException("At least one input directory is required.", nameof(inputDirectories));
            foreach (var dir in inputDirectories)
            {
                if (dir.Exists is false)
                    throw new DirectoryNotFoundException($"Input directory '{dir.FullName}' does not exist.");
            }
            InputDirectories = inputDirectories;
            ChecksumFile = checksumFile;
            SearchPatterns = searchPatterns;
            Filters = filters;
        }

        /// <summary>
        /// Find which input directory a file belongs to and return the relative path.
        /// </summary>
        private string? GetRelativePathFromInputs(string filePath)
        {
            string normalized = filePath.Replace('\\', '/');
            foreach (var dir in InputDirectories)
            {
                string dirNorm = dir.FullName.Replace('\\', '/');
                if (!dirNorm.EndsWith('/')) dirNorm += '/';
                if (normalized.StartsWith(dirNorm, StringComparison.OrdinalIgnoreCase))
                    return Path.GetRelativePath(dir.FullName, filePath);
            }
            return null;
        }

        public (FileChange[] Changes, Dictionary<string, ulong> NewChecksums) TrackChanges()
            => TrackChanges(retainContent: false);

        /// <summary>
        /// Track changes and (optionally) keep each changed file's content on the FileChange
        /// so downstream consumers don't have to re-read it.
        /// </summary>
        public (FileChange[] Changes, Dictionary<string, ulong> NewChecksums) TrackChanges(bool retainContent)
        {
            // Load existing checksums
            Dictionary<string, ulong> lastChecksums = LoadChecksums();
            if (lastChecksums.TryGetValue("__version", out var version))
            {
                lastChecksums.Remove("__version");
                if (version != CurrentVersion)
                    lastChecksums = [];
            }
            else
            {
                lastChecksums = [];
            }

            // Collect all files matching the search patterns across all input directories
            FileInfo[] files = [.. InputDirectories
                .SelectMany(dir => SearchPatterns
                    .SelectMany(p => dir.EnumerateFiles(p, SearchOption.AllDirectories)))
                .Where(file =>
                {
                    if (Filters.Length == 0) return true;
                    string? rel = GetRelativePathFromInputs(file.FullName);
                    return rel is not null && Filters.Any(f => rel.StartsWith(f));
                })];

            ConcurrentBag<FileChange> changes = [];
            ConcurrentDictionary<string, ulong> newChecksums = new(StringComparer.OrdinalIgnoreCase);

#if DEBUG
            // In debug mode, treat all files as modified to simplify testing.
            foreach (var file in files)
            {
                string filePath = file.FullName.NormalizeSlashes();
                changes.Add(new FileChange(ChangeType.Modified, filePath));
            }
#else
            Parallel.ForEach(files, file =>
            {
                string filePath = file.FullName.NormalizeSlashes();
                byte[] bytes = File.ReadAllBytes(file.FullName);
                ulong hash = XXH64.DigestOf(bytes, 0, bytes.Length);
                newChecksums[filePath] = hash;

                if (!lastChecksums.TryGetValue(filePath, out var lh))
                {
                    var fc = new FileChange(ChangeType.Added, filePath);
                    if (retainContent) fc.Content = System.Text.Encoding.UTF8.GetString(bytes);
                    changes.Add(fc);
                }
                else if (lh != hash)
                {
                    var fc = new FileChange(ChangeType.Modified, filePath);
                    if (retainContent) fc.Content = System.Text.Encoding.UTF8.GetString(bytes);
                    changes.Add(fc);
                }
            });
#endif

            // Check for deleted files
            foreach (var lastFile in lastChecksums.Keys)
            {
                if (!newChecksums.ContainsKey(lastFile))
                    changes.Add(new FileChange(ChangeType.Deleted, lastFile));
            }

            return (changes.ToArray(), new Dictionary<string, ulong>(newChecksums, StringComparer.OrdinalIgnoreCase));
        }

        public Dictionary<string, ulong> LoadChecksums()
        {
            if (!ChecksumFile.Exists)
                return [];
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, ulong>>(File.ReadAllText(ChecksumFile.FullName)) ?? [];
            }
            catch
            {
                return [];
            }
        }

        public void SaveChecksums(Dictionary<string, ulong> checksums)
        {
            checksums["__version"] = CurrentVersion;
            var json = JsonConvert.SerializeObject(checksums, Formatting.None);
            if (ChecksumFile.DirectoryName is not string checksumDirectory)
                throw new Exception("Failed to get directory name for checksum file.");
            Directory.CreateDirectory(checksumDirectory);
            File.WriteAllText(ChecksumFile.FullName, json);
        }
    }
}
