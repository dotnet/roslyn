// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class CompilationCacheTests : TestBase
    {
        private readonly ICompilerServerLogger _logger;

        public CompilationCacheTests(ITestOutputHelper testOutputHelper)
        {
            _logger = new XunitCompilerServerLogger(testOutputHelper);
        }

        [Fact]
        public void TryCreate_ReturnsNull_WhenEnvironmentVariableNotSet()
        {
            Environment.SetEnvironmentVariable(CompilationCache.CachePathEnvironmentVariable, null);
            var cache = CompilationCache.TryCreate(_logger);
            Assert.Null(cache);
        }

        [Fact]
        public void TryCreate_ReturnsCache_WhenEnvironmentVariableSet()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            Environment.SetEnvironmentVariable(CompilationCache.CachePathEnvironmentVariable, cacheDir);
            try
            {
                var cache = CompilationCache.TryCreate(_logger);
                Assert.NotNull(cache);
            }
            finally
            {
                Environment.SetEnvironmentVariable(CompilationCache.CachePathEnvironmentVariable, null);
            }
        }

        [Fact]
        public void TryCreate_ReturnsNull_WhenEnvironmentVariableEmpty()
        {
            Environment.SetEnvironmentVariable(CompilationCache.CachePathEnvironmentVariable, "");
            try
            {
                var cache = CompilationCache.TryCreate(_logger);
                Assert.Null(cache);
            }
            finally
            {
                Environment.SetEnvironmentVariable(CompilationCache.CachePathEnvironmentVariable, null);
            }
        }

        [Fact]
        public void ComputeHashKey_IsDeterministic()
        {
            var key = "some deterministic key content";
            var hash1 = CompilationCache.ComputeHashKey(key);
            var hash2 = CompilationCache.ComputeHashKey(key);

            Assert.Equal(hash1, hash2);
            Assert.Equal(64, hash1.Length); // SHA-256 produces 32 bytes = 64 hex chars
            Assert.Equal(hash1, hash1.ToLowerInvariant()); // lowercase
        }

        [Fact]
        public void ComputeHashKey_DifferentInputs_ProduceDifferentHashes()
        {
            var hash1 = CompilationCache.ComputeHashKey("input A");
            var hash2 = CompilationCache.ComputeHashKey("input B");
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void TryGetCachedResult_ReturnsFalse_WhenEntryMissing()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            var cache = CreateCache(cacheDir);

            var result = cache.TryGetCachedResult("Util.dll", "abc123", _logger, out var path);

            Assert.False(result);
            Assert.Null(path);
        }

        [Fact]
        public void TryGetCachedResult_ReturnsTrue_WhenEntryExists()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            var cache = CreateCache(cacheDir);
            var dllName = "Util.dll";
            var hashKey = "abc123def456";

            // Create the cache entry manually.
            var entryDir = Path.Combine(cacheDir, dllName, hashKey);
            Directory.CreateDirectory(entryDir);
            var cachedDll = Path.Combine(entryDir, dllName);
            File.WriteAllBytes(cachedDll, [1, 2, 3]);

            var result = cache.TryGetCachedResult(dllName, hashKey, _logger, out var path);

            Assert.True(result);
            Assert.Equal(cachedDll, path);
        }

        [Fact]
        public void TryStoreResult_WritesFilesCorrectly()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            var cache = CreateCache(cacheDir);
            var dllName = "MyLib.dll";
            var hashKey = CompilationCache.ComputeHashKey("some key");
            var deterministicKey = """{ "compilation": "data" }""";

            // Create a fake output DLL.
            var outputDir = Temp.CreateDirectory().Path;
            var outputDll = Path.Combine(outputDir, dllName);
            File.WriteAllBytes(outputDll, [10, 20, 30]);

            cache.TryStoreResult(dllName, hashKey, outputDll, deterministicKey, _logger);

            // Check the cached DLL exists.
            var expectedDllPath = Path.Combine(cacheDir, dllName, hashKey, dllName);
            Assert.True(File.Exists(expectedDllPath));
            Assert.Equal([10, 20, 30], File.ReadAllBytes(expectedDllPath));

            // Check the cached key file exists.
            var expectedKeyPath = Path.Combine(cacheDir, dllName, hashKey, dllName + ".key");
            Assert.True(File.Exists(expectedKeyPath));
            Assert.Equal(deterministicKey, File.ReadAllText(expectedKeyPath, Encoding.UTF8));
        }

        [Fact]
        public void TryStoreResult_DoesNotThrow_WhenOutputFileNotFound()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            var cache = CreateCache(cacheDir);

            // No exception should escape even if the source file doesn't exist.
            cache.TryStoreResult("Util.dll", "hash", "/nonexistent/path/Util.dll", "key", _logger);
        }

        [Fact]
        public void RoundTrip_StoreAndRetrieve()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            var cache = CreateCache(cacheDir);
            var dllName = "Round.dll";
            var deterministicKey = "the key";
            var hashKey = CompilationCache.ComputeHashKey(deterministicKey);

            var outputDir = Temp.CreateDirectory().Path;
            var outputDll = Path.Combine(outputDir, dllName);
            File.WriteAllBytes(outputDll, [0xAB, 0xCD]);

            // Initially no cache hit.
            Assert.False(cache.TryGetCachedResult(dllName, hashKey, _logger, out _));

            // Store the result.
            cache.TryStoreResult(dllName, hashKey, outputDll, deterministicKey, _logger);

            // Now there should be a cache hit.
            Assert.True(cache.TryGetCachedResult(dllName, hashKey, _logger, out var cachedPath));
            Assert.NotNull(cachedPath);
            Assert.Equal([0xAB, 0xCD], File.ReadAllBytes(cachedPath));
        }

        [Fact]
        public void LogCacheMiss_LogsNothing_WhenNoPriorEntries()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            var cache = CreateCache(cacheDir);
            var logMessages = new List<string>();
            var logger = new CollectingLogger(logMessages);

            // Should complete without error and log only the miss line.
            cache.LogCacheMiss("NewLib.dll", "newhash", "current key content", logger);

            Assert.Single(logMessages);
            Assert.Contains("Cache miss:", logMessages[0]);
        }

        [Fact]
        public void LogCacheMiss_IncludesDiff_WhenPriorEntriesExist()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            var cache = CreateCache(cacheDir);

            var dllName = "Util.dll";
            var oldHashKey = "oldhash";
            var oldKeyContent = """{ "version": "1", "source": "old" }""";

            // Create an old cache entry.
            var oldEntryDir = Path.Combine(cacheDir, dllName, oldHashKey);
            Directory.CreateDirectory(oldEntryDir);
            File.WriteAllText(Path.Combine(oldEntryDir, dllName + ".key"), oldKeyContent, Encoding.UTF8);
            // Also place a fake DLL so the directory exists.
            File.WriteAllBytes(Path.Combine(oldEntryDir, dllName), [1]);

            var logMessages = new List<string>();
            var logger = new CollectingLogger(logMessages);

            var newKeyContent = """{ "version": "1", "source": "new" }""";
            cache.LogCacheMiss(dllName, "newhash", newKeyContent, logger);

            // There should be a miss line plus at least one diff line.
            Assert.True(logMessages.Count >= 2);
            Assert.Contains(logMessages, m => m.Contains("Cache miss:"));
            Assert.Contains(logMessages, m => m.Contains("diff vs entry"));
        }

        private static CompilationCache CreateCache(string cachePath)
        {
            Environment.SetEnvironmentVariable(CompilationCache.CachePathEnvironmentVariable, cachePath);
            try
            {
                return CompilationCache.TryCreate(EmptyCompilerServerLogger.Instance)
                    ?? throw new InvalidOperationException("Failed to create cache");
            }
            finally
            {
                Environment.SetEnvironmentVariable(CompilationCache.CachePathEnvironmentVariable, null);
            }
        }

        private sealed class CollectingLogger : ICompilerServerLogger
        {
            private readonly List<string> _messages;

            public CollectingLogger(List<string> messages) => _messages = messages;

            public bool IsLogging => true;

            public void Log(string message) => _messages.Add(message);
        }
    }
}
