// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.BuildTasks;

namespace MSBuildTaskBenchmarks
{
    /// <summary>
    /// Benchmarks comparing the performance of size/timestamp checking versus MVID extraction
    /// in the CopyRefAssembly MSBuild task.
    /// </summary>
    [MemoryDiagnoser]
    public class CopyRefAssemblyBenchmarks
    {
        private string _sourceFile = null!;
        private string _destFile = null!;
        private string _tempDir = null!;

        private static byte[] GetEmbeddedResource()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "MSBuildTaskBenchmarks.MVID1.dll";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new FileNotFoundException($"Could not find embedded resource: {resourceName}");
            }

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            // Create a temporary directory for test files
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDir);

            // Create source and destination files using embedded resource
            _sourceFile = Path.Combine(_tempDir, "source.dll");
            _destFile = Path.Combine(_tempDir, "dest.dll");

            // Write the embedded assembly to both files
            var assemblyBytes = GetEmbeddedResource();
            File.WriteAllBytes(_sourceFile, assemblyBytes);
            File.WriteAllBytes(_destFile, assemblyBytes);

            // Set the same timestamp to enable fast path optimization
            var timestamp = File.GetLastWriteTimeUtc(_sourceFile);
            File.SetLastWriteTimeUtc(_destFile, timestamp);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [Benchmark(Description = "Size and Timestamp Check (Fast Path)")]
        public bool SizeAndTimestampCheck()
        {
            var sourceInfo = new FileInfo(_sourceFile);
            var destInfo = new FileInfo(_destFile);

            return sourceInfo.Length == destInfo.Length &&
                   sourceInfo.LastWriteTimeUtc == destInfo.LastWriteTimeUtc;
        }

        [Benchmark(Description = "MVID Extraction (Slow Path)")]
        public bool MvidExtraction()
        {
            Guid sourceGuid;
            using (FileStream sourceStream = File.OpenRead(_sourceFile))
            {
                sourceGuid = MvidReader.ReadAssemblyMvidOrEmpty(sourceStream);
            }

            Guid destGuid;
            using (FileStream destStream = File.OpenRead(_destFile))
            {
                destGuid = MvidReader.ReadAssemblyMvidOrEmpty(destStream);
            }

            return sourceGuid.Equals(destGuid);
        }

        [Benchmark(Description = "Combined Check (Fast Path First)")]
        public bool CombinedCheck()
        {
            // First try the fast path
            var sourceInfo = new FileInfo(_sourceFile);
            var destInfo = new FileInfo(_destFile);

            if (sourceInfo.Length == destInfo.Length &&
                sourceInfo.LastWriteTimeUtc == destInfo.LastWriteTimeUtc)
            {
                return true;
            }

            // Fall back to MVID extraction if sizes or timestamps differ
            Guid sourceGuid;
            using (FileStream sourceStream = File.OpenRead(_sourceFile))
            {
                sourceGuid = MvidReader.ReadAssemblyMvidOrEmpty(sourceStream);
            }

            Guid destGuid;
            using (FileStream destStream = File.OpenRead(_destFile))
            {
                destGuid = MvidReader.ReadAssemblyMvidOrEmpty(destStream);
            }

            return sourceGuid.Equals(destGuid);
        }
    }
}
