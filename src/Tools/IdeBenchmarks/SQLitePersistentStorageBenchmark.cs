// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BenchmarkDotNet.Attributes;
using ICSharpCode.Decompiler.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.CodeAnalysis.SQLite;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace IdeBenchmarks
{
    public class SQLitePersistentStorageBenchmarks
    {
        private readonly UseExportProviderAttribute _useExportProviderAttribute = new UseExportProviderAttribute();

        [Params(0, 1000, 10000, 100000)]
        public int DataLength { get; set; }

        private TestWorkspace workspace;
        private SQLitePersistentStorageService storageService;
        private IChecksummedPersistentStorage storage;
        private Document document;

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

            document = workspace.CurrentSolution.Projects.Single().Documents.Single();
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

        [Benchmark(Baseline = true, Description = "Heavy Writes")]
        public void AllWrites()
        {
            var bytes = new byte[DataLength];
            storage.WriteStreamAsync(document, "", new MemoryStream(bytes)).GetAwaiter().GetResult();
        }
    }
}
