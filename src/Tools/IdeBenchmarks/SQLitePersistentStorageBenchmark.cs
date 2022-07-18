// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SQLite.v2;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace IdeBenchmarks
{
    public class SQLitePersistentStorageBenchmarks
    {
        private readonly UseExportProviderAttribute _useExportProviderAttribute = new UseExportProviderAttribute();

        // Run the test with different ratios of reads/writes.
        [Params(0, 25, 50, 75, 100)]
        public int ReadPercentage { get; set; }

        private TestWorkspace _workspace;
        private SQLitePersistentStorageService _storageService;
        private IChecksummedPersistentStorage _storage;
        private Document _document;
        private Random _random;

        public SQLitePersistentStorageBenchmarks()
        {
            _document = null!;
            _storage = null!;
            _storageService = null!;
            _workspace = null!;
            _random = null!;
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _useExportProviderAttribute.Before(null);

            if (_workspace != null)
            {
                throw new InvalidOperationException();
            }

            _workspace = TestWorkspace.Create(
@"<Workspace>
    <Project Language=""NoCompilation"" CommonReferences=""false"">
        <Document>
            // a no-compilation document
        </Document>
    </Project>
</Workspace>");

            var connectionPoolService = _workspace.ExportProvider.GetExportedValue<SQLiteConnectionPoolService>();
            var asyncListener = _workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>().GetListener(FeatureAttribute.PersistentStorage);

            _storageService = new SQLitePersistentStorageService(connectionPoolService, new StorageConfiguration(), asyncListener);

            var solution = _workspace.CurrentSolution;
            _storage = _storageService.GetStorageWorkerAsync(SolutionKey.ToSolutionKey(solution), CancellationToken.None).AsTask().GetAwaiter().GetResult();

            Console.WriteLine("Storage type: " + _storage.GetType());
            _document = _workspace.CurrentSolution.Projects.Single().Documents.Single();
            _random = new Random(0);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            if (_workspace == null)
            {
                throw new InvalidOperationException();
            }

            _document = null!;
            _storage.Dispose();
            _storage = null!;
            _storageService = null!;
            _workspace.Dispose();
            _workspace = null!;

            _useExportProviderAttribute.After(null);
        }

        private static readonly byte[] s_bytes = new byte[1000];

        [Benchmark(Baseline = true)]
        public Task PerfAsync()
        {
            const int capacity = 1000;
            var tasks = new List<Task>(capacity);

            // Create a lot of overlapping reads and writes to the DB to several different keys. The
            // percentage of reads and writes is parameterized above, allowing us to validate
            // performance with several different usage patterns.
            for (var i = 0; i < capacity; i++)
            {
                var name = _random.Next(0, 4).ToString();
                if (_random.Next(0, 100) < ReadPercentage)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        using var stream = await _storage.ReadStreamAsync(_document, name);
                    }));
                }
                else
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        using var stream = new MemoryStream(s_bytes);
                        await _storage.WriteStreamAsync(_document, name, stream);
                    }));
                }
            }

            return Task.WhenAll(tasks);
        }

        private class StorageConfiguration : IPersistentStorageConfiguration
        {
            public bool ThrowOnFailure => true;

            public string TryGetStorageLocation(SolutionKey _)
            {
                // Store the db in a different random temp dir.
                var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Console.WriteLine("Creating: " + tempDir);
                Directory.CreateDirectory(tempDir);
                return tempDir;
            }
        }
    }
}
