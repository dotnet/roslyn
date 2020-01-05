// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public enum Size
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

        private AbstractPersistentStorageService _storageService;
        private readonly string _persistentFolder;

        private const int LargeSize = (int)(SQLitePersistentStorage.MaxPooledByteArrayLength * 2);
        private const int MediumSize = (int)(SQLitePersistentStorage.MaxPooledByteArrayLength / 2);

        private const string SmallData1 = "Hello ESENT";
        private const string SmallData2 = "Goodbye ESENT";

        private static readonly string MediumData1 = string.Join(",", Enumerable.Repeat(SmallData1, MediumSize / SmallData1.Length));
        private static readonly string MediumData2 = string.Join(",", Enumerable.Repeat(SmallData2, MediumSize / SmallData2.Length));

        private static readonly string LargeData1 = string.Join(",", Enumerable.Repeat(SmallData1, LargeSize / SmallData1.Length));
        private static readonly string LargeData2 = string.Join(",", Enumerable.Repeat(SmallData2, LargeSize / SmallData2.Length));

        private static readonly Checksum s_checksum1 = Checksum.Create("1");
        private static readonly Checksum s_checksum2 = Checksum.Create("2");

        static AbstractPersistentStorageTests()
        {
            Assert.NotEqual(s_checksum1, s_checksum2);

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
            _storageService?.GetTestAccessor().Shutdown();

            if (Directory.Exists(_persistentFolder))
            {
                Directory.Delete(_persistentFolder, true);
            }
        }

        private string GetData1(Size size)
            => size == Size.Small ? SmallData1 : size == Size.Medium ? MediumData1 : LargeData1;

        private string GetData2(Size size)
            => size == Size.Small ? SmallData2 : size == Size.Medium ? MediumData2 : LargeData2;

        private Checksum GetChecksum1(bool withChecksum)
            => withChecksum ? s_checksum1 : null;

        private Checksum GetChecksum2(bool withChecksum)
            => withChecksum ? s_checksum2 : null;

        [Fact]
        public async Task TestNullFilePaths()
        {
            var solution = CreateOrOpenSolution(nullPaths: true);

            var streamName = "stream";

            using var storage = GetStorage(solution);
            var project = solution.Projects.First();
            var document = project.Documents.First();
            Assert.False(await storage.WriteStreamAsync(project, streamName, EncodeString("")));
            Assert.False(await storage.WriteStreamAsync(document, streamName, EncodeString("")));

            Assert.Null(await storage.ReadStreamAsync(project, streamName));
            Assert.Null(await storage.ReadStreamAsync(document, streamName));
        }

        [Theory]
        [CombinatorialData]
        public async Task PersistentService_Solution_WriteReadDifferentInstances(Size size, bool withChecksum)
        {
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Solution_WriteReadDifferentInstances1";
            var streamName2 = "PersistentService_Solution_WriteReadDifferentInstances2";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
                Assert.True(await storage.WriteStreamAsync(streamName2, EncodeString(GetData2(size)), GetChecksum2(withChecksum)));
            }

            using (var storage = GetStorage(solution))
            {
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName1, GetChecksum1(withChecksum))));
                Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName2, GetChecksum2(withChecksum))));
            }
        }

        [Theory]
        [CombinatorialData]
        public async Task PersistentService_Solution_WriteReadReopenSolution(Size size, bool withChecksum)
        {
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Solution_WriteReadReopenSolution1";
            var streamName2 = "PersistentService_Solution_WriteReadReopenSolution2";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
                Assert.True(await storage.WriteStreamAsync(streamName2, EncodeString(GetData2(size)), GetChecksum2(withChecksum)));
            }

            solution = CreateOrOpenSolution();

            using (var storage = GetStorage(solution))
            {
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName1, GetChecksum1(withChecksum))));
                Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName2, GetChecksum2(withChecksum))));
            }
        }

        [Theory]
        [CombinatorialData]
        private async Task PersistentService_Solution_WriteReadSameInstance(Size size, bool withChecksum)
        {
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Solution_WriteReadSameInstance1";
            var streamName2 = "PersistentService_Solution_WriteReadSameInstance2";

            using var storage = GetStorage(solution);
            Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
            Assert.True(await storage.WriteStreamAsync(streamName2, EncodeString(GetData2(size)), GetChecksum2(withChecksum)));

            Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName1, GetChecksum1(withChecksum))));
            Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName2, GetChecksum2(withChecksum))));
        }

        [Theory(Skip = "https://github.com/dotnet/roslyn/issues/22437")]
        [CombinatorialData]
        public async Task PersistentService_Project_WriteReadSameInstance(Size size, bool withChecksum)
        {
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Project_WriteReadSameInstance1";
            var streamName2 = "PersistentService_Project_WriteReadSameInstance2";

            using var storage = GetStorage(solution);
            var project = solution.Projects.Single();

            Assert.True(await storage.WriteStreamAsync(project, streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
            Assert.True(await storage.WriteStreamAsync(project, streamName2, EncodeString(GetData2(size)), GetChecksum2(withChecksum)));

            Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(project, streamName1, GetChecksum1(withChecksum))));
            Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(project, streamName2, GetChecksum2(withChecksum))));
        }

        [Theory]
        [CombinatorialData]
        public async Task PersistentService_Document_WriteReadSameInstance(Size size, bool withChecksum)
        {
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Document_WriteReadSameInstance1";
            var streamName2 = "PersistentService_Document_WriteReadSameInstance2";

            using var storage = GetStorage(solution);
            var document = solution.Projects.Single().Documents.Single();

            Assert.True(await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
            Assert.True(await storage.WriteStreamAsync(document, streamName2, EncodeString(GetData2(size)), GetChecksum2(withChecksum)));

            Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1, GetChecksum1(withChecksum))));
            Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName2, GetChecksum2(withChecksum))));
        }

        [Fact]
        public async Task PersistentService_Solution_SimultaneousWrites()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Solution_SimultaneousWrites1";

            using var storage = GetStorage(solution);
            DoSimultaneousWrites(s => storage.WriteStreamAsync(streamName1, EncodeString(s)));
            var value = int.Parse(ReadStringToEnd(await storage.ReadStreamAsync(streamName1)));
            Assert.True(value >= 0);
            Assert.True(value < NumThreads);
        }

        [Fact]
        public async Task PersistentService_Project_SimultaneousWrites()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Project_SimultaneousWrites1";

            using var storage = GetStorage(solution);
            DoSimultaneousWrites(s => storage.WriteStreamAsync(solution.Projects.Single(), streamName1, EncodeString(s)));
            var value = int.Parse(ReadStringToEnd(await storage.ReadStreamAsync(solution.Projects.Single(), streamName1)));
            Assert.True(value >= 0);
            Assert.True(value < NumThreads);
        }

        [Fact]
        public async Task PersistentService_Document_SimultaneousWrites()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Document_SimultaneousWrites1";

            using var storage = GetStorage(solution);
            DoSimultaneousWrites(s => storage.WriteStreamAsync(solution.Projects.Single().Documents.Single(), streamName1, EncodeString(s)));
            var value = int.Parse(ReadStringToEnd(await storage.ReadStreamAsync(solution.Projects.Single().Documents.Single(), streamName1)));
            Assert.True(value >= 0);
            Assert.True(value < NumThreads);
        }

        private void DoSimultaneousWrites(Func<string, Task> write)
        {
            var barrier = new Barrier(NumThreads);
            var countdown = new CountdownEvent(NumThreads);
            for (var i = 0; i < NumThreads; i++)
            {
                ThreadPool.QueueUserWorkItem(s =>
                {
                    var id = (int)s;
                    barrier.SignalAndWait();
                    write(id + "").Wait();
                    countdown.Signal();
                }, i);
            }

            countdown.Wait();
        }

        [Theory]
        [CombinatorialData]
        public async Task PersistentService_Solution_SimultaneousReads(Size size, bool withChecksum)
        {
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Solution_SimultaneousReads1";

            using var storage = GetStorage(solution);
            Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
            DoSimultaneousReads(async () => ReadStringToEnd(await storage.ReadStreamAsync(streamName1, GetChecksum1(withChecksum))), GetData1(size));
        }

        [Theory]
        [CombinatorialData]
        public async Task PersistentService_Project_SimultaneousReads(Size size, bool withChecksum)
        {
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Project_SimultaneousReads1";

            using var storage = GetStorage(solution);
            Assert.True(await storage.WriteStreamAsync(solution.Projects.Single(), streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
            DoSimultaneousReads(async () => ReadStringToEnd(await storage.ReadStreamAsync(solution.Projects.Single(), streamName1, GetChecksum1(withChecksum))), GetData1(size));
        }

        [Theory]
        [CombinatorialData]
        public async Task PersistentService_Document_SimultaneousReads(Size size, bool withChecksum)
        {
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Document_SimultaneousReads1";

            using var storage = GetStorage(solution);
            Assert.True(await storage.WriteStreamAsync(solution.Projects.Single().Documents.Single(), streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
            DoSimultaneousReads(async () => ReadStringToEnd(await storage.ReadStreamAsync(solution.Projects.Single().Documents.Single(), streamName1, GetChecksum1(withChecksum))), GetData1(size));
        }

        [Fact]
        public async Task TestReadChecksumReturnsNullWhenNeverWritten()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestReadChecksumReturnsNullWhenNeverWritten";

            using var storage = GetStorage(solution);
            Assert.Null(await storage.ReadChecksumAsync(streamName1));
        }

        [Fact]
        public async Task TestCanReadWithNullChecksumSomethingWrittenWithNonNullChecksum()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestCanReadWithNullChecksumSomethingWrittenWithNonNullChecksum";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(Size.Small)), s_checksum1));
            }

            using (var storage = GetStorage(solution))
            {
                Assert.Equal(GetData1(Size.Small), ReadStringToEnd(await storage.ReadStreamAsync(streamName1, checksum: null)));
            }
        }

        [Fact]
        public async Task TestCannotReadWithMismatchedChecksums()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestCannotReadWithMismatchedChecksums";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(Size.Small)), s_checksum1));
            }

            using (var storage = GetStorage(solution))
            {
                Assert.Null(await storage.ReadStreamAsync(streamName1, s_checksum2));
            }
        }

        [Fact]
        public async Task TestCannotReadChecksumIfWriteDidNotIncludeChecksum()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestCannotReadChecksumIfWriteDidNotIncludeChecksum";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(Size.Small)), checksum: null));
            }

            using (var storage = GetStorage(solution))
            {
                Assert.Null(await storage.ReadChecksumAsync(streamName1));
            }
        }

        [Fact]
        public async Task TestReadChecksumProducesWrittenChecksum()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestReadChecksumProducesWrittenChecksum";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(Size.Small)), checksum: s_checksum1));
            }

            using (var storage = GetStorage(solution))
            {
                Assert.Equal(s_checksum1, await storage.ReadChecksumAsync(streamName1));
            }
        }

        [Fact]
        public async Task TestReadChecksumProducesLastWrittenChecksum1()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestReadChecksumProducesLastWrittenChecksum1";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(Size.Small)), checksum: s_checksum1));
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(Size.Small)), checksum: null));
            }

            using (var storage = GetStorage(solution))
            {
                Assert.Null(await storage.ReadChecksumAsync(streamName1));
            }
        }

        [Fact]
        public async Task TestReadChecksumProducesLastWrittenChecksum2()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestReadChecksumProducesLastWrittenChecksum2";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(Size.Small)), checksum: null));
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(Size.Small)), checksum: s_checksum1));
            }

            using (var storage = GetStorage(solution))
            {
                Assert.Equal(s_checksum1, await storage.ReadChecksumAsync(streamName1));
            }
        }

        [Fact]
        public async Task TestReadChecksumProducesLastWrittenChecksum3()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestReadChecksumProducesLastWrittenChecksum3";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(Size.Small)), checksum: s_checksum1));
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(Size.Small)), checksum: s_checksum2));
            }

            using (var storage = GetStorage(solution))
            {
                Assert.Equal(s_checksum2, await storage.ReadChecksumAsync(streamName1));
            }
        }

        private void DoSimultaneousReads(Func<Task<string>> read, string expectedValue)
        {
            var barrier = new Barrier(NumThreads);
            var countdown = new CountdownEvent(NumThreads);

            var exceptions = new List<Exception>();
            for (var i = 0; i < NumThreads; i++)
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

        internal IChecksummedPersistentStorage GetStorage(
            Solution solution, IPersistentStorageFaultInjector faultInjectorOpt = null)
        {
            // For the sake of tests, all solutions are bigger than our threshold, and thus deserve to get storage for them
            var solutionSizeTrackerMock = new Mock<ISolutionSizeTracker>();
            solutionSizeTrackerMock.Setup(m => m.GetSolutionSize(solution.Workspace, solution.Id))
                                   .Returns(solution.Workspace.Options.GetOption(StorageOptions.SolutionSizeThreshold) + 1);

            // If we handed out one for a previous test, we need to shut that down first
            _storageService?.GetTestAccessor().Shutdown();
            var locationService = new MockPersistentStorageLocationService(solution.Id, _persistentFolder);

            _storageService = GetStorageService(locationService, solutionSizeTrackerMock.Object, faultInjectorOpt);
            var storage = _storageService.GetStorage(solution, checkBranchId: true);

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

            public MockPersistentStorageLocationService(SolutionId solutionId, string storageLocation)
            {
                _solutionId = solutionId;
                _storageLocation = storageLocation;
            }

            public bool IsSupported(Workspace workspace) => true;

            public string TryGetStorageLocation(Solution solution)
                => solution.Id == _solutionId ? _storageLocation : null;
        }

        internal abstract AbstractPersistentStorageService GetStorageService(IPersistentStorageLocationService locationService, ISolutionSizeTracker solutionSizeTracker, IPersistentStorageFaultInjector faultInjector);

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
                var count = 0;
                while (count < stream.Length)
                {
                    count = stream.Read(bytes, count, (int)stream.Length - count);
                }

                return _encoding.GetString(bytes);
            }
        }
    }
}
