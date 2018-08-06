﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionSize;
using Microsoft.CodeAnalysis.SQLite;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Test.Utilities;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    [UseExportProvider]
    public abstract class AbstractPersistentStorageTests : IDisposable
    {
        private enum Size
        {
            Small,
            Medium,
            Large
        }

        private const int NumThreads = 10;
        private const string PersistentFolderPrefix = "PersistentStorageTests_";

        private readonly Encoding _encoding = Encoding.UTF8;
        internal readonly IOptionService _persistentEnabledOptionService = new OptionServiceMock(new Dictionary<IOption, object>
        {
            { PersistentStorageOptions.Enabled, true },
            { StorageOptions.SolutionSizeThreshold, 100 }
        });

        private MockPersistentStorageLocationService _persistentLocationService;
        private readonly string _persistentFolder;

        private const int LargeSize = (int)(SQLitePersistentStorage.MaxPooledByteArrayLength * 2);
        private const int MediumSize = (int)(SQLitePersistentStorage.MaxPooledByteArrayLength / 2);

        private const string SmallData1 = "Hello ESENT";
        private const string SmallData2 = "Goodbye ESENT";

        private static string MediumData1 = string.Join(",", Enumerable.Repeat(SmallData1, MediumSize / SmallData1.Length));
        private static string MediumData2 = string.Join(",", Enumerable.Repeat(SmallData2, MediumSize / SmallData2.Length));

        private static string LargeData1 = string.Join(",", Enumerable.Repeat(SmallData1, LargeSize / SmallData1.Length));
        private static string LargeData2 = string.Join(",", Enumerable.Repeat(SmallData2, LargeSize / SmallData2.Length));

        static AbstractPersistentStorageTests()
        {
            Assert.True(MediumData1.Length < SQLitePersistentStorage.MaxPooledByteArrayLength);
            Assert.True(MediumData2.Length < SQLitePersistentStorage.MaxPooledByteArrayLength);

            Assert.True(LargeData1.Length > SQLitePersistentStorage.MaxPooledByteArrayLength);
            Assert.True(LargeData2.Length > SQLitePersistentStorage.MaxPooledByteArrayLength);
        }

        protected AbstractPersistentStorageTests()
        {
            _persistentFolder = Path.Combine(Path.GetTempPath(), PersistentFolderPrefix + Guid.NewGuid());
            Directory.CreateDirectory(_persistentFolder);

            ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
            ThreadPool.SetMinThreads(Math.Max(workerThreads, NumThreads), completionPortThreads);
        }

        public void Dispose()
        {
            // This should cause the service to release the cached connection it maintains for the primary workspace
            _persistentLocationService?.RaiseShutdown();

            if (Directory.Exists(_persistentFolder))
            {
                Directory.Delete(_persistentFolder, true);
            }
        }

        private string GetData1(Size size)
            => size == Size.Small ? SmallData1 : size == Size.Medium ? MediumData1 : LargeData1;

        private string GetData2(Size size)
            => size == Size.Small ? SmallData2 : size == Size.Medium ? MediumData2 : LargeData2;

        [Fact]
        public async Task TestNullFilePaths()
        {
            var solution = CreateOrOpenSolution(nullPaths: true);

            var streamName = "stream";

            using (var storage = GetStorage(solution))
            {
                var project = solution.Projects.First();
                var document = project.Documents.First();
                Assert.False(await storage.WriteStreamAsync(project, streamName, EncodeString("")));
                Assert.False(await storage.WriteStreamAsync(document, streamName, EncodeString("")));

                Assert.Null(await storage.ReadStreamAsync(project, streamName));
                Assert.Null(await storage.ReadStreamAsync(document, streamName));
            }
        }

        [Fact]
        public async Task PersistentService_Solution_WriteReadDifferentInstances()
        {
            var solution = CreateOrOpenSolution();
            await PersistentService_Solution_WriteReadDifferentInstances(solution, Size.Small);
            await PersistentService_Solution_WriteReadDifferentInstances(solution, Size.Medium);
            await PersistentService_Solution_WriteReadDifferentInstances(solution, Size.Large);
        }

        private async Task PersistentService_Solution_WriteReadDifferentInstances(Solution solution, Size size)
        {
            var streamName1 = "PersistentService_Solution_WriteReadDifferentInstances1";
            var streamName2 = "PersistentService_Solution_WriteReadDifferentInstances2";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size))));
                Assert.True(await storage.WriteStreamAsync(streamName2, EncodeString(GetData2(size))));
            }

            using (var storage = GetStorage(solution))
            {
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName1)));
                Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName2)));
            }
        }

        [Fact]
        public async Task PersistentService_Solution_WriteReadReopenSolution()
        {
            var solution = CreateOrOpenSolution();
            await PersistentService_Solution_WriteReadReopenSolution(solution, Size.Small);
            await PersistentService_Solution_WriteReadReopenSolution(solution, Size.Medium);
            await PersistentService_Solution_WriteReadReopenSolution(solution, Size.Large);
        }

        private async Task PersistentService_Solution_WriteReadReopenSolution(Solution solution, Size size)
        {
            var streamName1 = "PersistentService_Solution_WriteReadReopenSolution1";
            var streamName2 = "PersistentService_Solution_WriteReadReopenSolution2";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size))));
                Assert.True(await storage.WriteStreamAsync(streamName2, EncodeString(GetData2(size))));
            }

            solution = CreateOrOpenSolution();

            using (var storage = GetStorage(solution))
            {
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName1)));
                Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName2)));
            }
        }

        [Fact]
        public async Task PersistentService_Solution_WriteReadSameInstance()
        {
            var solution = CreateOrOpenSolution();
            await PersistentService_Solution_WriteReadSameInstance(solution, Size.Small);
            await PersistentService_Solution_WriteReadSameInstance(solution, Size.Medium);
            await PersistentService_Solution_WriteReadSameInstance(solution, Size.Large);
        }

        private async Task PersistentService_Solution_WriteReadSameInstance(Solution solution, Size size)
        {
            var streamName1 = "PersistentService_Solution_WriteReadSameInstance1";
            var streamName2 = "PersistentService_Solution_WriteReadSameInstance2";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size))));
                Assert.True(await storage.WriteStreamAsync(streamName2, EncodeString(GetData2(size))));

                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName1)));
                Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName2)));
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/22437")]
        public async Task PersistentService_Project_WriteReadSameInstance()
        {
            var solution = CreateOrOpenSolution();
            await PersistentService_Project_WriteReadSameInstance(solution, Size.Small);
            await PersistentService_Project_WriteReadSameInstance(solution, Size.Medium);
            await PersistentService_Project_WriteReadSameInstance(solution, Size.Large);
        }

        private async Task PersistentService_Project_WriteReadSameInstance(Solution solution, Size size)
        {
            var streamName1 = "PersistentService_Project_WriteReadSameInstance1";
            var streamName2 = "PersistentService_Project_WriteReadSameInstance2";

            using (var storage = GetStorage(solution))
            {
                var project = solution.Projects.Single();

                Assert.True(await storage.WriteStreamAsync(project, streamName1, EncodeString(GetData1(size))));
                Assert.True(await storage.WriteStreamAsync(project, streamName2, EncodeString(GetData2(size))));

                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(project, streamName1)));
                Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(project, streamName2)));
            }
        }

        [Fact]
        public async Task PersistentService_Document_WriteReadSameInstance()
        {
            var solution = CreateOrOpenSolution();
            await PersistentService_Document_WriteReadSameInstance(solution, Size.Small);
            await PersistentService_Document_WriteReadSameInstance(solution, Size.Medium);
            await PersistentService_Document_WriteReadSameInstance(solution, Size.Large);
        }

        private async Task PersistentService_Document_WriteReadSameInstance(Solution solution, Size size)
        {
            var streamName1 = "PersistentService_Document_WriteReadSameInstance1";
            var streamName2 = "PersistentService_Document_WriteReadSameInstance2";

            using (var storage = GetStorage(solution))
            {
                var document = solution.Projects.Single().Documents.Single();

                Assert.True(await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size))));
                Assert.True(await storage.WriteStreamAsync(document, streamName2, EncodeString(GetData2(size))));

                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1)));
                Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName2)));
            }
        }

        [Fact]
        public async Task PersistentService_Solution_SimultaneousWrites()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Solution_SimultaneousWrites1";

            using (var storage = GetStorage(solution))
            {
                DoSimultaneousWrites(s => storage.WriteStreamAsync(streamName1, EncodeString(s)));
                int value = int.Parse(ReadStringToEnd(await storage.ReadStreamAsync(streamName1)));
                Assert.True(value >= 0);
                Assert.True(value < NumThreads);
            }
        }

        [Fact]
        public async Task PersistentService_Project_SimultaneousWrites()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Project_SimultaneousWrites1";

            using (var storage = GetStorage(solution))
            {
                DoSimultaneousWrites(s => storage.WriteStreamAsync(solution.Projects.Single(), streamName1, EncodeString(s)));
                int value = int.Parse(ReadStringToEnd(await storage.ReadStreamAsync(solution.Projects.Single(), streamName1)));
                Assert.True(value >= 0);
                Assert.True(value < NumThreads);
            }
        }

        [Fact]
        public async Task PersistentService_Document_SimultaneousWrites()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Document_SimultaneousWrites1";

            using (var storage = GetStorage(solution))
            {
                DoSimultaneousWrites(s => storage.WriteStreamAsync(solution.Projects.Single().Documents.Single(), streamName1, EncodeString(s)));
                int value = int.Parse(ReadStringToEnd(await storage.ReadStreamAsync(solution.Projects.Single().Documents.Single(), streamName1)));
                Assert.True(value >= 0);
                Assert.True(value < NumThreads);
            }
        }

        private void DoSimultaneousWrites(Func<string, Task> write)
        {
            var barrier = new Barrier(NumThreads);
            var countdown = new CountdownEvent(NumThreads);
            for (int i = 0; i < NumThreads; i++)
            {
                ThreadPool.QueueUserWorkItem(s =>
                {
                    int id = (int)s;
                    barrier.SignalAndWait();
                    write(id + "").Wait();
                    countdown.Signal();
                }, i);
            }

            countdown.Wait();
        }

        [Fact]
        public async Task PersistentService_Solution_SimultaneousReads()
        {
            var solution = CreateOrOpenSolution();
            await PersistentService_Solution_SimultaneousReads(solution, Size.Small);
            await PersistentService_Solution_SimultaneousReads(solution, Size.Medium);
            await PersistentService_Solution_SimultaneousReads(solution, Size.Large);
        }

        private async Task PersistentService_Solution_SimultaneousReads(Solution solution, Size size)
        {
            var streamName1 = "PersistentService_Solution_SimultaneousReads1";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size))));
                DoSimultaneousReads(async () => ReadStringToEnd(await storage.ReadStreamAsync(streamName1)), GetData1(size));
            }
        }

        [Fact]
        public async Task PersistentService_Project_SimultaneousReads()
        {
            var solution = CreateOrOpenSolution();
            await PersistentService_Project_SimultaneousReads(solution, Size.Small);
            await PersistentService_Project_SimultaneousReads(solution, Size.Medium);
            await PersistentService_Project_SimultaneousReads(solution, Size.Large);
        }

        private async Task PersistentService_Project_SimultaneousReads(Solution solution, Size size)
        {
            var streamName1 = "PersistentService_Project_SimultaneousReads1";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(solution.Projects.Single(), streamName1, EncodeString(GetData1(size))));
                DoSimultaneousReads(async () => ReadStringToEnd(await storage.ReadStreamAsync(solution.Projects.Single(), streamName1)), GetData1(size));
            }
        }

        [Fact]
        public async Task PersistentService_Document_SimultaneousReads()
        {
            var solution = CreateOrOpenSolution();
            await PersistentService_Document_SimultaneousReads(solution, Size.Small);
            await PersistentService_Document_SimultaneousReads(solution, Size.Medium);
            await PersistentService_Document_SimultaneousReads(solution, Size.Large);
        }

        private async Task PersistentService_Document_SimultaneousReads(Solution solution, Size size)
        {
            var streamName1 = "PersistentService_Document_SimultaneousReads1";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(solution.Projects.Single().Documents.Single(), streamName1, EncodeString(GetData1(size))));
                DoSimultaneousReads(async () => ReadStringToEnd(await storage.ReadStreamAsync(solution.Projects.Single().Documents.Single(), streamName1)), GetData1(size));
            }
        }

        private void DoSimultaneousReads(Func<Task<string>> read, string expectedValue)
        {
            var barrier = new Barrier(NumThreads);
            var countdown = new CountdownEvent(NumThreads);

            var exceptions = new List<Exception>();
            for (int i = 0; i < NumThreads; i++)
            {
                Task.Run(async () =>
                {
                    barrier.SignalAndWait();
                    try
                    {
                        Assert.Equal(expectedValue, await read());
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                    countdown.Signal();
                });
            }

            countdown.Wait();

            Assert.Equal(new List<Exception>(), exceptions);
        }

        protected Solution CreateOrOpenSolution(bool nullPaths = false)
        {
            var solutionFile = Path.Combine(_persistentFolder, "Solution1.sln");
            File.WriteAllText(solutionFile, "");

            var info = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(), solutionFile);

            var workspace = new AdhocWorkspace();
            workspace.AddSolution(info);

            var solution = workspace.CurrentSolution;

            var projectFile = Path.Combine(Path.GetDirectoryName(solutionFile), "Project1.csproj");
            File.WriteAllText(projectFile, "");
            solution = solution.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "Project1", "Project1", LanguageNames.CSharp,
                filePath: nullPaths ? null : projectFile));
            var project = solution.Projects.Single();

            var documentFile = Path.Combine(Path.GetDirectoryName(projectFile), "Document1.cs");
            File.WriteAllText(documentFile, "");
            solution = solution.AddDocument(DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "Document1",
                filePath: nullPaths ? null : documentFile));

            // Apply this to the workspace so our Solution is the primary branch ID, which matches our usual behavior
            workspace.TryApplyChanges(solution);

            return workspace.CurrentSolution;
        }

        internal IPersistentStorage GetStorage(
            Solution solution, IPersistentStorageFaultInjector faultInjectorOpt = null)
        {
            // For the sake of tests, all solutions are bigger than our threshold, and thus deserve to get storage for them
            var solutionSizeTrackerMock = new Mock<ISolutionSizeTracker>();
            solutionSizeTrackerMock.Setup(m => m.GetSolutionSize(solution.Workspace, solution.Id))
                                   .Returns(solution.Workspace.Options.GetOption(StorageOptions.SolutionSizeThreshold) + 1);

            // If we handed out one for a previous test, we need to shut that down first
            _persistentLocationService?.RaiseShutdown();
            _persistentLocationService = new MockPersistentStorageLocationService(solution.Id, _persistentFolder);

            var storage = GetStorageService(_persistentLocationService, solutionSizeTrackerMock.Object, faultInjectorOpt).GetStorage(solution);

            // If we're injecting faults, we expect things to be strange
            if (faultInjectorOpt == null)
            {
                Assert.NotEqual(NoOpPersistentStorage.Instance, storage);
            }

            return storage;
        }


        private class MockPersistentStorageLocationService : IPersistentStorageLocationService
        {
            private readonly SolutionId _solutionId;
            private readonly string _storageLocation;

#pragma warning disable CS0067
            public event EventHandler<PersistentStorageLocationChangingEventArgs> StorageLocationChanging;
#pragma warning restore CS0067

            public MockPersistentStorageLocationService(SolutionId solutionId, string storageLocation)
            {
                _solutionId = solutionId;
                _storageLocation = storageLocation;
            }

            public bool IsSupported(Workspace workspace)
            {
                return true;
            }

            public string TryGetStorageLocation(SolutionId solutionId)
            {
                return solutionId == _solutionId ? _storageLocation : null;
            }

            public void RaiseShutdown()
            {
                StorageLocationChanging?.Invoke(this, new PersistentStorageLocationChangingEventArgs(_solutionId, null, mustUseNewStorageLocationImmediately: true));
            }
        }

        internal abstract IPersistentStorageService GetStorageService(IPersistentStorageLocationService locationService, ISolutionSizeTracker solutionSizeTracker, IPersistentStorageFaultInjector faultInjector);

        protected Stream EncodeString(string text)
        {
            var bytes = _encoding.GetBytes(text);
            var stream = new MemoryStream(bytes);
            return stream;
        }

        private string ReadStringToEnd(Stream stream)
        {
            using (stream)
            {
                var bytes = new byte[stream.Length];
                int count = 0;
                while (count < stream.Length)
                {
                    count = stream.Read(bytes, count, (int)stream.Length - count);
                }

                return _encoding.GetString(bytes);
            }
        }
    }
}
