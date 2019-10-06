// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;
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

        //[Params("0k", "1k", "10k", "100k")]
        //public string DataSize { get; set; }

        //readonly byte[] _0k = new byte[0];
        //readonly byte[] _1k = new byte[1000];
        //readonly byte[] _10k = new byte[10000];
        //readonly byte[] _100k = new byte[100000];

        [Params(0, 25, 50, 75, 100)]
        public int ReadPercentage { get; set; }

        private TestWorkspace workspace;
        private SQLitePersistentStorageService storageService;
        private IChecksummedPersistentStorage storage;
        private Document document;
        private Random random;

        private class SolutionSizeTracker : ISolutionSizeTracker
        {
            public long GetSolutionSize(Workspace workspace, SolutionId solutionId)
            {
                return 0;
            }
        }

        private class LocationService : IPersistentStorageLocationService
        {
            public event EventHandler<PersistentStorageLocationChangingEventArgs> StorageLocationChanging;

            public bool IsSupported(Workspace workspace) => true;
            public string TryGetStorageLocation(SolutionId solutionId)
            {
                var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Console.WriteLine("Creating: " + tempDir);
                Directory.CreateDirectory(tempDir);
                return tempDir;
            }
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _useExportProviderAttribute.Before(null);

            if (workspace != null)
            {
                throw new InvalidOperationException();
            }

            workspace = TestWorkspace.CreateCSharp("");
            workspace.Options = workspace.Options.WithChangedOption(StorageOptions.SolutionSizeThreshold, -1);

            storageService = new SQLitePersistentStorageService(
                workspace.Services.GetService<IOptionService>(),
                new LocationService(),
                new SolutionSizeTracker());

            storage = storageService.GetStorageWorker(workspace.CurrentSolution);
            if (storage == NoOpPersistentStorage.Instance)
            {
                throw new InvalidOperationException("We didn't properly get the sqlite storage instance.");
            }

            Console.WriteLine("Storage type: " + storage.GetType());
            document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            random = new Random(0);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            if (workspace == null)
            {
                throw new InvalidOperationException();
            }

            document = null;
            storage.Dispose();
            storage = null;
            storageService = null;
            workspace.Dispose();
            workspace = null;

            _useExportProviderAttribute.After(null);
        }

        static readonly byte[] bytes = new byte[1000];

        [Benchmark(Baseline = true)]
        public Task Perf()
        {
            var tasks = new List<Task>();

            for (int i = 0; i < 1000; i++)
            {
                var name = random.Next(0, 4).ToString();
                if (random.Next(0, 100) < ReadPercentage)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        using (var stream = await storage.ReadStreamAsync(document, name))
                        {
                        }
                    }));
                }
                else
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        using (var stream = new MemoryStream(bytes))
                        {
                            await storage.WriteStreamAsync(document, name, stream);
                        }
                    }));
                }
            }

            return Task.WhenAll(tasks);
        }
    }
}
