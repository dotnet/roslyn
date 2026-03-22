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
        public void TryRestoreCachedResult_ReturnsFalse_WhenEntryMissing()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            var cache = CreateCache(cacheDir);
            var outputDir = Temp.CreateDirectory().Path;

            var outputFiles = new CompilationOutputFiles
            {
                AssemblyPath = Path.Combine(outputDir, "Util.dll"),
            };

            var result = cache.TryRestoreCachedResult("Util.dll", "abc123", outputFiles, _logger);

            Assert.False(result);
            Assert.False(File.Exists(outputFiles.AssemblyPath));
        }

        [Fact]
        public void TryRestoreCachedResult_ReturnsTrue_WhenEntryExists()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            var cache = CreateCache(cacheDir);
            var dllName = "Util.dll";
            var hashKey = "abc123def456";
            var outputDir = Temp.CreateDirectory().Path;

            // Create the cache entry manually.
            var entryDir = Path.Combine(cacheDir, dllName, hashKey);
            Directory.CreateDirectory(entryDir);
            File.WriteAllBytes(Path.Combine(entryDir, "assembly"), [1, 2, 3]);

            var outputFiles = new CompilationOutputFiles
            {
                AssemblyPath = Path.Combine(outputDir, dllName),
            };

            var result = cache.TryRestoreCachedResult(dllName, hashKey, outputFiles, _logger);

            Assert.True(result);
            Assert.True(File.Exists(outputFiles.AssemblyPath));
            Assert.Equal([1, 2, 3], File.ReadAllBytes(outputFiles.AssemblyPath));
        }

        [Fact]
        public void TryRestoreCachedResult_RestoresAllOutputFiles()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            var cache = CreateCache(cacheDir);
            var dllName = "MyLib.dll";
            var hashKey = "fullhash";
            var outputDir = Temp.CreateDirectory().Path;

            // Create cache entry with all output files.
            var entryDir = Path.Combine(cacheDir, dllName, hashKey);
            Directory.CreateDirectory(entryDir);
            File.WriteAllBytes(Path.Combine(entryDir, "assembly"), [1]);
            File.WriteAllBytes(Path.Combine(entryDir, "pdb"), [2]);
            File.WriteAllBytes(Path.Combine(entryDir, "refassembly"), [3]);
            File.WriteAllBytes(Path.Combine(entryDir, "xmldoc"), [4]);

            var outputFiles = new CompilationOutputFiles
            {
                AssemblyPath = Path.Combine(outputDir, dllName),
                PdbPath = Path.Combine(outputDir, "MyLib.pdb"),
                RefAssemblyPath = Path.Combine(outputDir, "ref", dllName),
                XmlDocPath = Path.Combine(outputDir, "MyLib.xml"),
            };

            Directory.CreateDirectory(Path.Combine(outputDir, "ref"));

            var result = cache.TryRestoreCachedResult(dllName, hashKey, outputFiles, _logger);

            Assert.True(result);
            Assert.Equal([1], File.ReadAllBytes(outputFiles.AssemblyPath));
            Assert.Equal([2], File.ReadAllBytes(outputFiles.PdbPath!));
            Assert.Equal([3], File.ReadAllBytes(outputFiles.RefAssemblyPath!));
            Assert.Equal([4], File.ReadAllBytes(outputFiles.XmlDocPath!));
        }

        [Fact]
        public void TryRestoreCachedResult_SkipsOptionalFiles_WhenNotCached()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            var cache = CreateCache(cacheDir);
            var dllName = "MyLib.dll";
            var hashKey = "assemblonly";
            var outputDir = Temp.CreateDirectory().Path;

            // Cache entry with only the assembly.
            var entryDir = Path.Combine(cacheDir, dllName, hashKey);
            Directory.CreateDirectory(entryDir);
            File.WriteAllBytes(Path.Combine(entryDir, "assembly"), [10]);

            var outputFiles = new CompilationOutputFiles
            {
                AssemblyPath = Path.Combine(outputDir, dllName),
                PdbPath = Path.Combine(outputDir, "MyLib.pdb"),
            };

            var result = cache.TryRestoreCachedResult(dllName, hashKey, outputFiles, _logger);

            Assert.True(result);
            Assert.True(File.Exists(outputFiles.AssemblyPath));
            // PDB was requested but not cached — should not be written.
            Assert.False(File.Exists(outputFiles.PdbPath!));
        }

        [Fact]
        public void TryStoreResult_WritesAllOutputFiles()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            var cache = CreateCache(cacheDir);
            var dllName = "MyLib.dll";
            var hashKey = CompilationCache.ComputeHashKey("some key");
            var deterministicKey = """{ "compilation": "data" }""";

            // Create fake output files.
            var outputDir = Temp.CreateDirectory().Path;
            var assemblyPath = Path.Combine(outputDir, dllName);
            var pdbPath = Path.Combine(outputDir, "MyLib.pdb");
            var refPath = Path.Combine(outputDir, "ref", dllName);
            var xmlPath = Path.Combine(outputDir, "MyLib.xml");
            Directory.CreateDirectory(Path.Combine(outputDir, "ref"));
            File.WriteAllBytes(assemblyPath, [10, 20, 30]);
            File.WriteAllBytes(pdbPath, [40, 50]);
            File.WriteAllBytes(refPath, [60]);
            File.WriteAllBytes(xmlPath, [70, 80, 90]);

            var outputFiles = new CompilationOutputFiles
            {
                AssemblyPath = assemblyPath,
                PdbPath = pdbPath,
                RefAssemblyPath = refPath,
                XmlDocPath = xmlPath,
            };

            cache.TryStoreResult(dllName, hashKey, outputFiles, deterministicKey, _logger);

            var entryDir = Path.Combine(cacheDir, dllName, hashKey);
            Assert.Equal([10, 20, 30], File.ReadAllBytes(Path.Combine(entryDir, "assembly")));
            Assert.Equal([40, 50], File.ReadAllBytes(Path.Combine(entryDir, "pdb")));
            Assert.Equal([60], File.ReadAllBytes(Path.Combine(entryDir, "refassembly")));
            Assert.Equal([70, 80, 90], File.ReadAllBytes(Path.Combine(entryDir, "xmldoc")));
            Assert.Equal(deterministicKey, File.ReadAllText(Path.Combine(entryDir, dllName + ".key"), Encoding.UTF8));
        }

        [Fact]
        public void TryStoreResult_SkipsOptionalFiles_WhenNotPresent()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            var cache = CreateCache(cacheDir);
            var dllName = "MyLib.dll";
            var hashKey = CompilationCache.ComputeHashKey("key");

            // Only the assembly exists.
            var outputDir = Temp.CreateDirectory().Path;
            var assemblyPath = Path.Combine(outputDir, dllName);
            File.WriteAllBytes(assemblyPath, [1, 2, 3]);

            var outputFiles = new CompilationOutputFiles
            {
                AssemblyPath = assemblyPath,
                PdbPath = null,
                RefAssemblyPath = null,
                XmlDocPath = null,
            };

            cache.TryStoreResult(dllName, hashKey, outputFiles, "key", _logger);

            var entryDir = Path.Combine(cacheDir, dllName, hashKey);
            Assert.True(File.Exists(Path.Combine(entryDir, "assembly")));
            Assert.False(File.Exists(Path.Combine(entryDir, "pdb")));
            Assert.False(File.Exists(Path.Combine(entryDir, "refassembly")));
            Assert.False(File.Exists(Path.Combine(entryDir, "xmldoc")));
        }

        [Fact]
        public void TryStoreResult_DoesNotThrow_WhenAssemblyFileNotFound()
        {
            var cacheDir = Temp.CreateDirectory().Path;
            var cache = CreateCache(cacheDir);

            var outputFiles = new CompilationOutputFiles
            {
                AssemblyPath = "/nonexistent/path/Util.dll",
            };

            // No exception should escape even if the source file doesn't exist.
            cache.TryStoreResult("Util.dll", "hash", outputFiles, "key", _logger);
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
            var assemblyPath = Path.Combine(outputDir, dllName);
            File.WriteAllBytes(assemblyPath, [0xAB, 0xCD]);

            var restoreDir = Temp.CreateDirectory().Path;
            var restoreFiles = new CompilationOutputFiles
            {
                AssemblyPath = Path.Combine(restoreDir, dllName),
            };

            // Initially no cache hit.
            Assert.False(cache.TryRestoreCachedResult(dllName, hashKey, restoreFiles, _logger));

            // Store the result.
            var storeFiles = new CompilationOutputFiles { AssemblyPath = assemblyPath };
            cache.TryStoreResult(dllName, hashKey, storeFiles, deterministicKey, _logger);

            // Now there should be a cache hit.
            Assert.True(cache.TryRestoreCachedResult(dllName, hashKey, restoreFiles, _logger));
            Assert.Equal([0xAB, 0xCD], File.ReadAllBytes(restoreFiles.AssemblyPath));
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
            // Also place a fake assembly so the directory looks like a valid entry.
            File.WriteAllBytes(Path.Combine(oldEntryDir, "assembly"), [1]);

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
