// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.CodeAnalysis.SQLite;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace IdeBenchmarks
{
    public class SQLitePersistentStorageBenchmarks
    {
        private readonly UseExportProviderAttribute _useExportProviderAttribute = new UseExportProviderAttribute();

        // Run the test with different ratios of reads/writes.
        [Params(0, 25, 50, 75, 100)]
        // [Params(25)]
        public int ReadPercentage { get; set; }

        private TestWorkspace _workspace;
        private SQLitePersistentStorageService _storageService;
        private IChecksummedPersistentStorage _storage;
        private Document _document;
        private Random _random;

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

            // Ensure we always use the storage service, no matter what the size of the solution.
            _workspace.Options = _workspace.Options.WithChangedOption(StorageOptions.SolutionSizeThreshold, -1);

            _storageService = new SQLitePersistentStorageService(
                _workspace.Services.GetService<IOptionService>(),
                new LocationService(),
                new SolutionSizeTracker());

            _storage = _storageService.GetStorageWorker(_workspace.CurrentSolution);
            if (_storage == NoOpPersistentStorage.Instance)
            {
                throw new InvalidOperationException("We didn't properly get the sqlite storage instance.");
            }

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

            _document = null;
            _storage.Dispose();
            _storage = null;
            _storageService = null;
            _workspace.Dispose();
            _workspace = null;

            _useExportProviderAttribute.After(null);
        }

        private static readonly byte[] s_bytes = new byte[1000];

        [Benchmark(Baseline = true)]
        public Task Perf()
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

        private class SolutionSizeTracker : ISolutionSizeTracker
        {
            public long GetSolutionSize(Workspace workspace, SolutionId solutionId) => 0;
        }

        private class LocationService : IPersistentStorageLocationService
        {
            public event EventHandler<PersistentStorageLocationChangingEventArgs> StorageLocationChanging { add { } remove { } }

            public bool IsSupported(Workspace workspace) => true;
            public string TryGetStorageLocation(SolutionId solutionId)
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
