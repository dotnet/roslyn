// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// Holds the paths to all compiler output files for a single compilation.
    /// </summary>
    internal readonly struct CompilationOutputFiles
    {
        /// <summary>The absolute path of the main output assembly. Always set.</summary>
        public required string AssemblyPath { get; init; }

        /// <summary>The absolute path of the PDB file, or <see langword="null"/> if no standalone PDB file is emitted (including embedded PDBs).</summary>
        public string? PdbPath { get; init; }

        /// <summary>The absolute path of the reference assembly, or <see langword="null"/> if none is produced.</summary>
        public string? RefAssemblyPath { get; init; }

        /// <summary>The absolute path of the XML documentation file, or <see langword="null"/> if none is produced.</summary>
        public string? XmlDocPath { get; init; }
    }

    /// <summary>
    /// Provides a file-system based cache for compilation outputs.
    /// The cache is enabled by the <c>use-global-cache</c> feature flag on the compiler request.
    /// </summary>
    /// <remarks>
    /// Cache layout: <c>&lt;cache root&gt;/&lt;dll name&gt;/&lt;sha-256&gt;/</c>.
    /// Each entry directory contains the following files (when present):
    /// <list type="bullet">
    ///   <item><c>assembly</c> — the main output assembly (always present in a valid entry)</item>
    ///   <item><c>pdb</c> — the PDB debug symbols</item>
    ///   <item><c>refassembly</c> — the reference assembly</item>
    ///   <item><c>xmldoc</c> — the XML documentation file</item>
    ///   <item><c>&lt;dll name&gt;.key</c> — the full deterministic key JSON</item>
    /// </list>
    /// </remarks>
    internal sealed class CompilationCache
    {
        private const string DefaultCacheDirectoryName = "roslyn-cache";
        private const string AssemblyFileName = "assembly";
        private const string PdbFileName = "pdb";
        private const string RefAssemblyFileName = "refassembly";
        private const string XmlDocFileName = "xmldoc";
        private const string LastUsedFileName = "last-used";
        private const string CreatedFileName = "created";

        private readonly string _cachePath;

        private CompilationCache(string cachePath)
        {
            _cachePath = cachePath;
        }

        /// <summary>
        /// Creates a <see cref="CompilationCache"/> when the compiler request enables the
        /// <c>use-global-cache</c> experiment; otherwise returns <see langword="null"/>.
        /// </summary>
        internal static CompilationCache? TryCreate(CommandLineArguments arguments, ICompilerServerLogger logger)
        {
            var cachePath = GetCachePath(arguments.ParseOptions.Features, logger);
            if (cachePath is null)
            {
                return null;
            }

            logger.Log($"Compilation cache enabled at: {cachePath}");
            return new CompilationCache(cachePath);
        }

        /// <summary>
        /// Returns the cache root path, if set.
        /// </summary>
        internal string CachePath => _cachePath;

        /// <summary>
        /// Returns the default cache path used when no explicit path is configured.
        /// This is the same location that <see cref="TryCreate"/> would use as a fallback.
        /// Returns <see langword="null"/> when <c>LocalApplicationData</c> is unavailable.
        /// </summary>
        internal static string? GetDefaultCachePath()
            => PathUtilities.GetTempCachePath(DefaultCacheDirectoryName);

        private static string? GetCachePath(IReadOnlyDictionary<string, string> features, ICompilerServerLogger logger)
        {
            if (!features.TryGetValue(CompilerOptionParseUtilities.UseGlobalCacheFeatureFlag, out var featureValue) || featureValue is null)
            {
                return null;
            }

            if (featureValue.Length != 0 && !string.Equals(featureValue, bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                return featureValue;
            }

            var cachePath = PathUtilities.GetTempCachePath(DefaultCacheDirectoryName);
            if (cachePath is null)
            {
                logger.Log("Compilation cache disabled because LocalApplicationData is unavailable.");
            }

            return cachePath;
        }

        /// <summary>
        /// Computes the SHA-256 hash of the deterministic key and returns it as a lowercase hex string.
        /// </summary>
        internal static string ComputeHashKey(string deterministicKey)
        {
#if NET
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(deterministicKey));
            return Convert.ToHexString(bytes).ToLowerInvariant();
#else
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(deterministicKey));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
#endif
        }

        private string GetCacheEntryDirectory(string dllName, string hashKey)
            => Path.Combine(_cachePath, dllName, hashKey);

        internal string GetCacheEntryMutexName(string dllName, string hashKey)
            => BuildServerConnection.GetServerMutexName($"compilation-cache.{ComputeHashKey($"{_cachePath}|{dllName}|{hashKey}")}");

        /// <summary>
        /// Checks whether a cached result exists for the given DLL name and hash key.
        /// On a hit, all cached output files are copied to the paths specified by
        /// <paramref name="outputFiles"/>. Returns <see langword="false"/> if no cached
        /// entry exists or if any required file copy fails.
        /// </summary>
        internal bool TryRestoreCachedResult(
            string dllName,
            string hashKey,
            CompilationOutputFiles outputFiles,
            ICompilerServerLogger logger)
        {
            var entryDir = GetCacheEntryDirectory(dllName, hashKey);
            var cachedAssemblyPath = Path.Combine(entryDir, AssemblyFileName);

            try
            {
                if (!File.Exists(cachedAssemblyPath))
                {
                    using var entryMutex = TryAcquireEntryMutex(dllName, hashKey, createIfMissing: false);
                    if (entryMutex is null && !File.Exists(cachedAssemblyPath))
                    {
                        logger.Log($"Cache miss because entry is being populated: {dllName} [{hashKey}]");
                        return false;
                    }

                    if (!File.Exists(cachedAssemblyPath))
                    {
                        return false;
                    }
                }

                // Verify all required cached files exist before copying anything,
                // so we don't partially overwrite outputs on a cache miss.
                if (isMissingWhenRequired(entryDir, PdbFileName, outputFiles.PdbPath)
                    || isMissingWhenRequired(entryDir, RefAssemblyFileName, outputFiles.RefAssemblyPath)
                    || isMissingWhenRequired(entryDir, XmlDocFileName, outputFiles.XmlDocPath))
                {
                    logger.Log($"Cache miss because entry is missing required output files: {dllName} [{hashKey}]");
                    return false;
                }

                logger.Log($"Cache hit: {dllName} [{hashKey}]");
                TouchLastUsed(entryDir, logger);
                File.Copy(cachedAssemblyPath, outputFiles.AssemblyPath, overwrite: true);
                copyIfNeeded(entryDir, PdbFileName, outputFiles.PdbPath);
                copyIfNeeded(entryDir, RefAssemblyFileName, outputFiles.RefAssemblyPath);
                copyIfNeeded(entryDir, XmlDocFileName, outputFiles.XmlDocPath);
            }
            catch (Exception ex)
            {
                logger.Log($"Cache hit restore failed, falling through to compilation: {ex.Message}");
                return false;
            }

            return true;

            // Returns true if the cached file exists or is not needed (targetPath is null).
            static bool isMissingWhenRequired(string entryDir, string cachedFileName, string? targetPath)
            {
                if (targetPath is null)
                {
                    return false;
                }

                return !File.Exists(Path.Combine(entryDir, cachedFileName));
            }

            static void copyIfNeeded(string entryDir, string cachedFileName, string? targetPath)
            {
                if (targetPath is null)
                {
                    return;
                }

                File.Copy(Path.Combine(entryDir, cachedFileName), targetPath, overwrite: true);
            }
        }

        /// <summary>
        /// Logs a cache miss. When prior entries exist for the same DLL name, includes a diff
        /// of the current key against the most recent three entries to aid diagnosability.
        /// </summary>
        internal void LogCacheMiss(
            string dllName,
            string hashKey,
            string currentKey,
            ICompilerServerLogger logger)
        {
            if (!logger.IsLogging)
            {
                return;
            }

            logger.Log($"Cache miss: {dllName} [{hashKey}]");

            var dllCacheDir = Path.Combine(_cachePath, dllName);
            if (!Directory.Exists(dllCacheDir))
            {
                return;
            }

            // Find the most recent three entries for this DLL name (excluding the current hash).
            List<(string Path, string Name)> recentEntries;
            try
            {
                recentEntries = Directory.EnumerateDirectories(dllCacheDir)
                    .Select(d => (Path: d, Name: Path.GetFileName(d)))
                    .Where(e => e.Name != hashKey)
                    .Select(e => (e.Path, e.Name, Time: Directory.GetLastWriteTimeUtc(e.Path)))
                    .OrderByDescending(e => e.Time)
                    .Take(3)
                    .Select(e => (e.Path, e.Name))
                    .ToList();
            }
            catch (IOException)
            {
                return;
            }

            foreach (var (entryPath, entryName) in recentEntries)
            {
                var keyPath = Path.Combine(entryPath, dllName + ".key");
                if (!File.Exists(keyPath))
                {
                    continue;
                }

                try
                {
                    var oldKey = File.ReadAllText(keyPath, Encoding.UTF8);
                    var diff = ComputeDiff(currentKey, oldKey);
                    logger.Log($"Cache miss {dllName} [{hashKey}] diff vs entry [{entryName}]:{Environment.NewLine}{diff}");
                }
                catch (IOException)
                {
                    // Ignore errors reading cached key files.
                }
            }
        }

        /// <summary>
        /// Stores all compiled outputs and the deterministic key in the cache.
        /// Only files that actually exist at the given paths are stored.
        /// Failures are logged but do not propagate as exceptions.
        /// </summary>
        internal void TryStoreResult(
            string dllName,
            string hashKey,
            CompilationOutputFiles outputFiles,
            string deterministicKey,
            ICompilerServerLogger logger)
        {
            string? stagingDir = null;
            try
            {
                var dllCacheDir = Path.Combine(_cachePath, dllName);
                Directory.CreateDirectory(dllCacheDir);

                using var entryMutex = TryAcquireEntryMutex(dllName, hashKey);
                if (entryMutex is null)
                {
                    logger.Log($"Cache store skipped because another writer is populating: {dllName} [{hashKey}]");
                    return;
                }

                var cacheDir = GetCacheEntryDirectory(dllName, hashKey);
                if (Directory.Exists(cacheDir))
                {
                    // Another writer finished publishing this entry before we got here.
                    logger.Log($"Cache store skipped because entry already exists: {dllName} [{hashKey}]");
                    return;
                }

                // Populate a unique staging directory and publish it with a single rename so readers only
                // ever observe a fully populated cache entry.
                stagingDir = Path.Combine(dllCacheDir, hashKey + "." + Guid.NewGuid().ToString("N") + ".tmp");
                Directory.CreateDirectory(stagingDir);

                File.Copy(outputFiles.AssemblyPath, Path.Combine(stagingDir, AssemblyFileName), overwrite: false);

                tryCopyOptional(outputFiles.PdbPath, Path.Combine(stagingDir, PdbFileName));
                tryCopyOptional(outputFiles.RefAssemblyPath, Path.Combine(stagingDir, RefAssemblyFileName));
                tryCopyOptional(outputFiles.XmlDocPath, Path.Combine(stagingDir, XmlDocFileName));

                File.WriteAllText(Path.Combine(stagingDir, dllName + ".key"), deterministicKey, Encoding.UTF8);
                File.WriteAllText(Path.Combine(stagingDir, CreatedFileName), DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                Directory.Move(stagingDir, cacheDir);
                stagingDir = null;

                TouchLastUsed(cacheDir, logger);
                logger.Log($"Cache stored: {dllName} [{hashKey}]");
            }
            catch (Exception ex)
            {
                logger.Log($"Cache store failed for {dllName} [{hashKey}]: {ex.Message}");
            }
            finally
            {
                if (stagingDir is not null)
                {
                    try
                    {
                        if (Directory.Exists(stagingDir))
                        {
                            Directory.Delete(stagingDir, recursive: true);
                        }
                    }
                    catch (IOException ex)
                    {
                        logger.Log($"Cache cleanup failed for {dllName} [{hashKey}]: {ex.Message}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        logger.Log($"Cache cleanup failed for {dllName} [{hashKey}]: {ex.Message}");
                    }
                }
            }

            static void tryCopyOptional(string? sourcePath, string destPath)
            {
                if (sourcePath is not null && File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destPath, overwrite: false);
                }
            }
        }

        private IDisposable? TryAcquireEntryMutex(string dllName, string hashKey, bool createIfMissing = true)
        {
            var mutexName = GetCacheEntryMutexName(dllName, hashKey);
            if (!createIfMissing && !ServerNamedMutex.WasOpen(mutexName))
            {
                return null;
            }

            var mutex = new ServerNamedMutex(mutexName, out var createdNew);
            if (createdNew)
            {
                if (!createIfMissing)
                {
                    mutex.Dispose();
                    return null;
                }

                return mutex;
            }

            if (mutex.TryLock(timeoutMs: 0))
            {
                return mutex;
            }

            mutex.Dispose();
            return null;
        }

        /// <summary>
        /// Produces a simple line-based diff showing lines present only in <paramref name="oldKey"/>
        /// (prefixed with <c>-</c>) and lines present only in <paramref name="currentKey"/>
        /// (prefixed with <c>+</c>).
        /// </summary>
        private static string ComputeDiff(string currentKey, string oldKey)
        {
            var currentLineList = splitLines(currentKey);
            var oldLineList = splitLines(oldKey);
            var currentLines = new HashSet<string>(currentLineList);
            var oldLines = new HashSet<string>(oldLineList);

            var diff = new StringBuilder();

            foreach (var line in oldLineList)
            {
                if (!currentLines.Contains(line))
                {
                    diff.AppendLine($"- {line}");
                }
            }

            foreach (var line in currentLineList)
            {
                if (!oldLines.Contains(line))
                {
                    diff.AppendLine($"+ {line}");
                }
            }

            return diff.Length > 0 ? diff.ToString() : "(no differences)";
            static string[] splitLines(string text)
            {
                var lines = text.Split('\n');
                for (var i = 0; i < lines.Length; i++)
                {
                    lines[i] = lines[i].TrimEnd('\r');
                }

                return lines;
            }
        }

        /// <summary>
        /// Writes the current UTC timestamp into the <c>last-used</c> marker file in the
        /// given cache entry directory.  The timestamp is stored as round-trip text inside
        /// the file so that it survives copies/uploads that do not preserve file-system
        /// metadata.  Errors are logged and otherwise ignored — this is best-effort
        /// bookkeeping.
        /// </summary>
        private static void TouchLastUsed(string entryDir, ICompilerServerLogger logger)
        {
            try
            {
                var path = Path.Combine(entryDir, LastUsedFileName);
                File.WriteAllText(path, DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                // Best effort — don't fail a compilation because of bookkeeping.
                logger.LogException(ex, $"Failed to update last-used for {entryDir}");
            }
        }

        /// <summary>
        /// Deletes cache entries under <paramref name="cachePath"/> whose <c>last-used</c>
        /// marker is older than <paramref name="cutoff"/>. Entries without a marker file
        /// use the directory creation time instead.
        /// Returns a human-readable summary.
        /// </summary>
        internal static string PurgeEntries(string cachePath, DateTimeOffset cutoff, ICompilerServerLogger logger)
        {
            if (!Directory.Exists(cachePath))
            {
                return $"Cache directory does not exist: {cachePath}";
            }

            var totalDeleted = 0;
            var totalKept = 0;
            var totalErrors = 0;

            try
            {
                foreach (var dllDir in Directory.EnumerateDirectories(cachePath))
                {
                    var dllName = Path.GetFileName(dllDir);

                    foreach (var entryDir in Directory.EnumerateDirectories(dllDir))
                    {
                        var dirName = Path.GetFileName(entryDir);

                        // Skip staging directories (they end with .tmp)
                        if (dirName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var lastUsed = GetLastUsedTimeUtc(entryDir, logger);
                        if (lastUsed >= cutoff)
                        {
                            totalKept++;
                        }
                        else
                        {
                            try
                            {
                                Directory.Delete(entryDir, recursive: true);
                                totalDeleted++;
                                logger.Log($"Cache purge: deleted {dllName}/{dirName} (last used {lastUsed:u})");
                            }
                            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                            {
                                totalErrors++;
                                logger.Log($"Cache purge: failed to delete {dllName}/{dirName}: {ex.Message}");
                            }
                        }
                    }

                    // Remove the dllName directory if it's now empty
                    try
                    {
                        if (Directory.Exists(dllDir) && !Directory.EnumerateFileSystemEntries(dllDir).Any())
                        {
                            Directory.Delete(dllDir);
                            logger.Log($"Cache purge: removed empty directory {dllName}");
                        }
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        logger.Log($"Cache purge: failed to remove empty directory {dllName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.Log($"Cache purge: error enumerating {cachePath}: {ex.Message}");
            }

            var summary = $"Cache purge complete. Deleted: {totalDeleted}, Kept: {totalKept}, Errors: {totalErrors}";
            logger.Log(summary);
            return summary;
        }

        /// <summary>
        /// Returns the UTC time a cache entry was last used by reading the round-trip
        /// timestamp stored inside the <c>last-used</c> file.  Falls back to the
        /// directory creation time when the file is missing or unreadable.
        /// </summary>
        internal static DateTimeOffset GetLastUsedTimeUtc(string entryDir, ICompilerServerLogger logger)
        {
            var lastUsedPath = Path.Combine(entryDir, LastUsedFileName);
            try
            {
                if (File.Exists(lastUsedPath))
                {
                    var text = File.ReadAllText(lastUsedPath).Trim();
                    if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                    {
                        return parsed.ToUniversalTime();
                    }
                }
            }
            catch (Exception ex)
            {
                // Fall through to directory time.
                logger.Log($"Failed to read last-used for {entryDir}: {ex.Message}");
            }

            return new DateTimeOffset(Directory.GetCreationTimeUtc(entryDir), TimeSpan.Zero);
        }

        /// <summary>
        /// Returns the UTC time a cache entry was originally created by reading the
        /// <c>created</c> file.  Falls back to the directory creation time when the
        /// file is missing or unreadable.
        /// </summary>
        internal static DateTimeOffset GetCreatedTimeUtc(string entryDir, ICompilerServerLogger logger)
        {
            var createdPath = Path.Combine(entryDir, CreatedFileName);
            try
            {
                if (File.Exists(createdPath))
                {
                    var text = File.ReadAllText(createdPath).Trim();
                    if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                    {
                        return parsed.ToUniversalTime();
                    }
                }
            }
            catch (Exception ex)
            {
                // Fall through to directory time.
                logger.Log($"Failed to read created time for {entryDir}: {ex.Message}");
            }

            return new DateTimeOffset(Directory.GetCreationTimeUtc(entryDir), TimeSpan.Zero);
        }

        /// <summary>
        /// Computes cache hit/miss statistics for entries under <paramref name="cachePath"/>
        /// since the given <paramref name="since"/> cutoff.
        /// An entry is a <b>hit</b> if it was created before <paramref name="since"/>
        /// and used (last-used &gt;= since).  It is a <b>store</b> (miss) if created
        /// after <paramref name="since"/>.  Otherwise it is <b>untouched</b>.
        /// </summary>
        internal static CacheStats GetCacheStats(string cachePath, DateTimeOffset since, ICompilerServerLogger logger)
        {
            var stats = new CacheStats();
            if (!Directory.Exists(cachePath))
            {
                return stats;
            }

            try
            {
                foreach (var dllDir in Directory.EnumerateDirectories(cachePath))
                {
                    var dllName = Path.GetFileName(dllDir);

                    foreach (var entryDir in Directory.EnumerateDirectories(dllDir))
                    {
                        var dirName = Path.GetFileName(entryDir);
                        if (dirName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var created = GetCreatedTimeUtc(entryDir, logger);
                        var lastUsed = GetLastUsedTimeUtc(entryDir, logger);

                        if (created >= since)
                        {
                            stats.Stores++;
                            stats.StoreDetails.Add((dllName, dirName, created, lastUsed));
                        }
                        else if (lastUsed >= since)
                        {
                            stats.Hits++;
                            stats.HitDetails.Add((dllName, dirName, created, lastUsed));
                        }
                        else
                        {
                            stats.Untouched++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Best effort.
                logger.Log($"Failed to enumerate cache stats for {cachePath}: {ex.Message}");
            }

            return stats;
        }
    }

    /// <summary>
    /// Aggregated cache hit/miss statistics for a single cache root.
    /// </summary>
    internal sealed class CacheStats
    {
        public int Hits { get; set; }
        public int Stores { get; set; }
        public int Untouched { get; set; }
        public List<(string DllName, string HashKey, DateTimeOffset Created, DateTimeOffset LastUsed)> HitDetails { get; } = new();
        public List<(string DllName, string HashKey, DateTimeOffset Created, DateTimeOffset LastUsed)> StoreDetails { get; } = new();

        /// <summary>
        /// Formats a human-readable summary of the statistics.
        /// <paramref name="verbosity"/>: 0 = totals only, 1 = grouped by DLL, 2 = individual entries.
        /// </summary>
        public string FormatSummary(string cachePath, int verbosity)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Cache: {cachePath}");
            sb.AppendLine($"  Hits (reused):  {Hits}");
            sb.AppendLine($"  Stores (new):   {Stores}");
            sb.AppendLine($"  Untouched:      {Untouched}");

            if (verbosity >= 1)
            {
                FormatGrouped(sb, "Hit", HitDetails, verbosity);
                FormatGrouped(sb, "Store", StoreDetails, verbosity);
            }

            return sb.ToString().TrimEnd();
        }

        private static void FormatGrouped(StringBuilder sb, string label, List<(string DllName, string HashKey, DateTimeOffset Created, DateTimeOffset LastUsed)> details, int verbosity)
        {
            if (details.Count == 0)
            {
                return;
            }

            var byDll = details
                .GroupBy(d => d.DllName, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            sb.AppendLine($"  {label} details:");
            foreach (var group in byDll)
            {
                sb.AppendLine($"    {group.Key} ({group.Count()})");
                if (verbosity >= 2)
                {
                    foreach (var (_, hashKey, created, lastUsed) in group)
                    {
                        sb.AppendLine($"      {hashKey} (created: {created:u}, last used: {lastUsed:u})");
                    }
                }
            }
        }
    }
}
