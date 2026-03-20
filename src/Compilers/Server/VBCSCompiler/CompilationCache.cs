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

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// Provides a file-system based cache for compilation outputs.
    /// The cache is enabled by setting the <c>ROSLYN_CACHE_PATH</c> environment variable
    /// to the directory where cached outputs should be stored.
    /// </summary>
    /// <remarks>
    /// Cache layout: <c>$ROSLYN_CACHE_PATH/&lt;dll name&gt;/&lt;sha-256&gt;/&lt;dll name&gt;</c>
    /// Each cache entry also stores the full deterministic key as <c>&lt;dll name&gt;.key</c>.
    /// </remarks>
    internal sealed class CompilationCache
    {
        internal const string CachePathEnvironmentVariable = "ROSLYN_CACHE_PATH";

        private readonly string _cachePath;

        private CompilationCache(string cachePath)
        {
            _cachePath = cachePath;
        }

        /// <summary>
        /// Creates a <see cref="CompilationCache"/> if the <c>ROSLYN_CACHE_PATH</c>
        /// environment variable is set; otherwise returns <see langword="null"/>.
        /// </summary>
        internal static CompilationCache? TryCreate(ICompilerServerLogger logger)
        {
            var cachePath = Environment.GetEnvironmentVariable(CachePathEnvironmentVariable);
            if (string.IsNullOrEmpty(cachePath))
            {
                return null;
            }

            logger.Log($"Compilation cache enabled at: {cachePath}");
            return new CompilationCache(cachePath);
        }

        /// <summary>
        /// Computes the SHA-256 hash of the deterministic key and returns it as a lowercase hex string.
        /// </summary>
        internal static string ComputeHashKey(string deterministicKey)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(deterministicKey));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        private string GetCacheEntryDirectory(string dllName, string hashKey)
            => Path.Combine(_cachePath, dllName, hashKey);

        private string GetCachedDllPath(string dllName, string hashKey)
            => Path.Combine(GetCacheEntryDirectory(dllName, hashKey), dllName);

        private string GetCachedKeyPath(string dllName, string hashKey)
            => Path.Combine(GetCacheEntryDirectory(dllName, hashKey), dllName + ".key");

        /// <summary>
        /// Checks whether a cached result exists for the given DLL name and hash key.
        /// Logs a cache hit message if found.
        /// </summary>
        internal bool TryGetCachedResult(
            string dllName,
            string hashKey,
            ICompilerServerLogger logger,
            [NotNullWhen(true)] out string? cachedDllPath)
        {
            cachedDllPath = GetCachedDllPath(dllName, hashKey);
            if (File.Exists(cachedDllPath))
            {
                logger.Log($"Cache hit: {dllName} [{hashKey}]");
                return true;
            }

            cachedDllPath = null;
            return false;
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
                recentEntries = Directory.GetDirectories(dllCacheDir)
                    .Select(d => (Path: d, Name: System.IO.Path.GetFileName(d)))
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
                    logger.Log($"Cache miss diff vs entry {entryName}:{Environment.NewLine}{diff}");
                }
                catch (IOException)
                {
                    // Ignore errors reading cached key files.
                }
            }
        }

        /// <summary>
        /// Stores the compiled DLL and its deterministic key in the cache.
        /// Failures are logged but do not propagate as exceptions.
        /// </summary>
        internal void TryStoreResult(
            string dllName,
            string hashKey,
            string outputDllPath,
            string deterministicKey,
            ICompilerServerLogger logger)
        {
            try
            {
                var cacheDir = GetCacheEntryDirectory(dllName, hashKey);
                Directory.CreateDirectory(cacheDir);

                File.Copy(outputDllPath, GetCachedDllPath(dllName, hashKey), overwrite: true);
                File.WriteAllText(GetCachedKeyPath(dllName, hashKey), deterministicKey, Encoding.UTF8);

                logger.Log($"Cache stored: {dllName} [{hashKey}]");
            }
            catch (Exception ex)
            {
                logger.Log($"Cache store failed for {dllName} [{hashKey}]: {ex.Message}");
            }
        }

        /// <summary>
        /// Produces a simple line-based diff showing lines present only in <paramref name="oldKey"/>
        /// (prefixed with <c>-</c>) and lines present only in <paramref name="currentKey"/>
        /// (prefixed with <c>+</c>).
        /// </summary>
        private static string ComputeDiff(string currentKey, string oldKey)
        {
            static string[] SplitLines(string text)
            {
                var lines = text.Split('\n');
                for (var i = 0; i < lines.Length; i++)
                {
                    lines[i] = lines[i].TrimEnd('\r');
                }

                return lines;
            }

            var currentLines = new HashSet<string>(SplitLines(currentKey));
            var oldLines = new HashSet<string>(SplitLines(oldKey));

            var diff = new StringBuilder();

            foreach (var line in SplitLines(oldKey))
            {
                if (!currentLines.Contains(line))
                {
                    diff.AppendLine($"- {line}");
                }
            }

            foreach (var line in SplitLines(currentKey))
            {
                if (!oldLines.Contains(line))
                {
                    diff.AppendLine($"+ {line}");
                }
            }

            return diff.Length > 0 ? diff.ToString() : "(no differences)";
        }
    }
}
