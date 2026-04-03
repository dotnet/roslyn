// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
                Directory.Move(stagingDir, cacheDir);
                stagingDir = null;

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
    }
}
