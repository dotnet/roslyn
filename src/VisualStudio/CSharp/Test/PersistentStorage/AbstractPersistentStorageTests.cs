// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.UnitTests;
using Roslyn.Test.Utilities;
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
            Large,
            ExtraLarge,
        }

        private const int Iterations = 20;

        private const int NumThreads = 10;
        private const string PersistentFolderPrefix = "PersistentStorageTests_";

        private readonly Encoding _encoding = Encoding.UTF8;

        private AbstractPersistentStorageService? _storageService;
        private readonly DisposableDirectory _persistentFolderRoot;
        private readonly TempDirectory _persistentFolder;

        // 256k (larger than the 100k that CloudCache uses to decide when to dump to an external file).
        // See https://dev.azure.com/devdiv/DevDiv/_git/VS.CloudCache?path=%2Fsrc%2FMicrosoft.VisualStudio.Cache%2FCacheService.cs&version=GBmain&line=35&lineEnd=36&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
        private const int ExtraLargeSize = 256 * 1024;
        private const int LargeSize = (int)(SQLite.v2.SQLitePersistentStorage.MaxPooledByteArrayLength * 2);
        private const int MediumSize = (int)(SQLite.v2.SQLitePersistentStorage.MaxPooledByteArrayLength / 2);

        private const string SmallData1 = "Hello ESENT";
        private const string SmallData2 = "Goodbye ESENT";

        private static readonly string MediumData1 = string.Join(",", Enumerable.Repeat(SmallData1, MediumSize / SmallData1.Length));
        private static readonly string MediumData2 = string.Join(",", Enumerable.Repeat(SmallData2, MediumSize / SmallData2.Length));

        private static readonly string LargeData1 = string.Join(",", Enumerable.Repeat(SmallData1, LargeSize / SmallData1.Length));
        private static readonly string LargeData2 = string.Join(",", Enumerable.Repeat(SmallData2, LargeSize / SmallData2.Length));

        private static readonly string ExtraLargeData1 = string.Join(",", Enumerable.Repeat(SmallData1, ExtraLargeSize / SmallData1.Length));
        private static readonly string ExtraLargeData2 = string.Join(",", Enumerable.Repeat(SmallData2, ExtraLargeSize / SmallData2.Length));

        private static readonly Checksum s_checksum1 = Checksum.Create("1");
        private static readonly Checksum s_checksum2 = Checksum.Create("2");

        static AbstractPersistentStorageTests()
        {
            Assert.NotEqual(s_checksum1, s_checksum2);

            Assert.True(MediumData1.Length < SQLite.v2.SQLitePersistentStorage.MaxPooledByteArrayLength);
            Assert.True(MediumData2.Length < SQLite.v2.SQLitePersistentStorage.MaxPooledByteArrayLength);

            Assert.True(LargeData1.Length > SQLite.v2.SQLitePersistentStorage.MaxPooledByteArrayLength);
            Assert.True(LargeData2.Length > SQLite.v2.SQLitePersistentStorage.MaxPooledByteArrayLength);
        }

        protected AbstractPersistentStorageTests()
        {
            _persistentFolderRoot = new DisposableDirectory(new TempRoot());
            _persistentFolder = _persistentFolderRoot.CreateDirectory(PersistentFolderPrefix + Guid.NewGuid());

            ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
            ThreadPool.SetMinThreads(Math.Max(workerThreads, NumThreads), completionPortThreads);
        }

        internal abstract AbstractPersistentStorageService GetStorageService(
            IMefHostExportProvider exportProvider,
            IPersistentStorageConfiguration configuration,
            IPersistentStorageFaultInjector? faultInjector,
            string rootFolder);

        public void Dispose()
        {
            // This should cause the service to release the cached connection it maintains for the primary workspace
            _storageService?.GetTestAccessor().Shutdown();
            _persistentFolderRoot.Dispose();
        }

        private static string GetData1(Size size)
            => size == Size.Small ? SmallData1 :
               size == Size.Medium ? MediumData1 :
               size == Size.Large ? LargeData1 : ExtraLargeData1;

        private static string GetData2(Size size)
            => size == Size.Small ? SmallData2 :
               size == Size.Medium ? MediumData2 :
               size == Size.Large ? LargeData2 : ExtraLargeData2;

        private static Checksum? GetChecksum1(bool withChecksum)
            => withChecksum ? s_checksum1 : null;

        private static Checksum? GetChecksum2(bool withChecksum)
            => withChecksum ? s_checksum2 : null;

        [Fact]
        public async Task TestNullFilePaths()
        {
            var solution = CreateOrOpenSolution(nullPaths: true);

            var streamName = "stream";

            await using var storage = await GetStorageAsync(solution);
            var project = solution.Projects.First();
            var document = project.Documents.First();
            Assert.False(await storage.WriteStreamAsync(project, streamName, EncodeString("")));
            Assert.False(await storage.WriteStreamAsync(document, streamName, EncodeString("")));

            Assert.Null(await storage.ReadStreamAsync(project, streamName));
            Assert.Null(await storage.ReadStreamAsync(document, streamName));
        }

        [Theory, CombinatorialData, WorkItem(1436188, "https://devdiv.visualstudio.com/DevDiv/_queries/edit/1436188")]
        public async Task CacheDirectoryInPathWithSingleQuote(Size size, bool withChecksum, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            using var folderRoot = new DisposableDirectory(new TempRoot());
            var folder = folderRoot.CreateDirectory(PersistentFolderPrefix + "'" + Guid.NewGuid());
            var solution = CreateOrOpenSolution(folder);

            var streamName1 = "PersistentService_Solution_WriteReadDifferentInstances1";
            var streamName2 = "PersistentService_Solution_WriteReadDifferentInstances2";

            await using (var storage = await GetStorageAsync(solution, folder))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
                Assert.True(await storage.WriteStreamAsync(streamName2, EncodeString(GetData2(size)), GetChecksum2(withChecksum)));
            }

            await using (var storage = await GetStorageAsync(solution, folder))
            {
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName1, GetChecksum1(withChecksum))));
                Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName2, GetChecksum2(withChecksum))));
            }
        }

        [Theory, CombinatorialData]
        public async Task PersistentService_Solution_WriteReadDifferentInstances(Size size, bool withChecksum, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Solution_WriteReadDifferentInstances1";
            var streamName2 = "PersistentService_Solution_WriteReadDifferentInstances2";

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
                Assert.True(await storage.WriteStreamAsync(streamName2, EncodeString(GetData2(size)), GetChecksum2(withChecksum)));
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName1, GetChecksum1(withChecksum))));
                Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName2, GetChecksum2(withChecksum))));
            }
        }

        [Theory, CombinatorialData]
        public async Task PersistentService_Solution_WriteReadReopenSolution(Size size, bool withChecksum, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Solution_WriteReadReopenSolution1";
            var streamName2 = "PersistentService_Solution_WriteReadReopenSolution2";

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
                Assert.True(await storage.WriteStreamAsync(streamName2, EncodeString(GetData2(size)), GetChecksum2(withChecksum)));
            }

            solution = CreateOrOpenSolution();

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName1, GetChecksum1(withChecksum))));
                Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName2, GetChecksum2(withChecksum))));
            }
        }

        [Theory, CombinatorialData]
        public async Task PersistentService_Solution_WriteReadSameInstance(Size size, bool withChecksum, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Solution_WriteReadSameInstance1";
            var streamName2 = "PersistentService_Solution_WriteReadSameInstance2";

            await using var storage = await GetStorageAsync(solution);
            Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
            Assert.True(await storage.WriteStreamAsync(streamName2, EncodeString(GetData2(size)), GetChecksum2(withChecksum)));

            Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName1, GetChecksum1(withChecksum))));
            Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName2, GetChecksum2(withChecksum))));
        }

        [Theory, CombinatorialData]
        public async Task PersistentService_Project_WriteReadSameInstance(Size size, bool withChecksum, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Project_WriteReadSameInstance1";
            var streamName2 = "PersistentService_Project_WriteReadSameInstance2";

            await using var storage = await GetStorageAsync(solution);
            var project = solution.Projects.Single();

            Assert.True(await storage.WriteStreamAsync(project, streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
            Assert.True(await storage.WriteStreamAsync(project, streamName2, EncodeString(GetData2(size)), GetChecksum2(withChecksum)));

            Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(project, streamName1, GetChecksum1(withChecksum))));
            Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(project, streamName2, GetChecksum2(withChecksum))));
        }

        [Theory, CombinatorialData]
        public async Task PersistentService_Document_WriteReadSameInstance(Size size, bool withChecksum, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Document_WriteReadSameInstance1";
            var streamName2 = "PersistentService_Document_WriteReadSameInstance2";

            await using var storage = await GetStorageAsync(solution);
            var document = solution.Projects.Single().Documents.Single();

            Assert.True(await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
            Assert.True(await storage.WriteStreamAsync(document, streamName2, EncodeString(GetData2(size)), GetChecksum2(withChecksum)));

            Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1, GetChecksum1(withChecksum))));
            Assert.Equal(GetData2(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName2, GetChecksum2(withChecksum))));
        }

        [Theory, CombinatorialData]
        public async Task PersistentService_Solution_SimultaneousWrites([CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Solution_SimultaneousWrites1";

            await using var storage = await GetStorageAsync(solution);
            DoSimultaneousWrites(s => storage.WriteStreamAsync(streamName1, EncodeString(s)));
            var value = int.Parse(ReadStringToEnd(await storage.ReadStreamAsync(streamName1)));
            Assert.True(value >= 0);
            Assert.True(value < NumThreads);
        }

        [Theory, CombinatorialData]
        public async Task PersistentService_Project_SimultaneousWrites([CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Project_SimultaneousWrites1";

            await using var storage = await GetStorageAsync(solution);
            DoSimultaneousWrites(s => storage.WriteStreamAsync(solution.Projects.Single(), streamName1, EncodeString(s)));
            var value = int.Parse(ReadStringToEnd(await storage.ReadStreamAsync(solution.Projects.Single(), streamName1)));
            Assert.True(value >= 0);
            Assert.True(value < NumThreads);
        }

        [Theory, CombinatorialData]
        public async Task PersistentService_Document_SimultaneousWrites([CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Document_SimultaneousWrites1";

            await using var storage = await GetStorageAsync(solution);
            DoSimultaneousWrites(s => storage.WriteStreamAsync(solution.Projects.Single().Documents.Single(), streamName1, EncodeString(s)));
            var value = int.Parse(ReadStringToEnd(await storage.ReadStreamAsync(solution.Projects.Single().Documents.Single(), streamName1)));
            Assert.True(value >= 0);
            Assert.True(value < NumThreads);
        }

        [Theory, CombinatorialData]
        public async Task PersistentService_Solution_SimultaneousReads(Size size, bool withChecksum, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Solution_SimultaneousReads1";

            await using var storage = await GetStorageAsync(solution);
            Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
            DoSimultaneousReads(async () => ReadStringToEnd(await storage.ReadStreamAsync(streamName1, GetChecksum1(withChecksum))), GetData1(size));
        }

        [Theory, CombinatorialData]
        public async Task PersistentService_Project_SimultaneousReads(Size size, bool withChecksum, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Project_SimultaneousReads1";

            await using var storage = await GetStorageAsync(solution);
            Assert.True(await storage.WriteStreamAsync(solution.Projects.Single(), streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
            DoSimultaneousReads(async () => ReadStringToEnd(await storage.ReadStreamAsync(solution.Projects.Single(), streamName1, GetChecksum1(withChecksum))), GetData1(size));
        }

        [Theory, CombinatorialData]
        public async Task PersistentService_Document_SimultaneousReads(Size size, bool withChecksum, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;

            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_Document_SimultaneousReads1";

            await using var storage = await GetStorageAsync(solution);
            Assert.True(await storage.WriteStreamAsync(solution.Projects.Single().Documents.Single(), streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
            DoSimultaneousReads(async () => ReadStringToEnd(await storage.ReadStreamAsync(solution.Projects.Single().Documents.Single(), streamName1, GetChecksum1(withChecksum))), GetData1(size));
        }

        [Theory, CombinatorialData]
        public async Task TestReadChecksumReturnsNullWhenNeverWritten([CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestReadChecksumReturnsNullWhenNeverWritten";

            await using var storage = await GetStorageAsync(solution);
            Assert.False(await storage.ChecksumMatchesAsync(streamName1, s_checksum1));
        }

        [Theory, CombinatorialData]
        public async Task TestCanReadWithNullChecksumSomethingWrittenWithNonNullChecksum(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestCanReadWithNullChecksumSomethingWrittenWithNonNullChecksum";

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), s_checksum1));
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(streamName1, checksum: null)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestCannotReadWithMismatchedChecksums(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestCannotReadWithMismatchedChecksums";

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), s_checksum1));
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.Null(await storage.ReadStreamAsync(streamName1, s_checksum2));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestCannotReadChecksumIfWriteDidNotIncludeChecksum(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestCannotReadChecksumIfWriteDidNotIncludeChecksum";

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), checksum: null));
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.False(await storage.ChecksumMatchesAsync(streamName1, s_checksum1));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestReadChecksumProducesWrittenChecksum(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestReadChecksumProducesWrittenChecksum";

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), checksum: s_checksum1));
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.ChecksumMatchesAsync(streamName1, s_checksum1));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestReadChecksumProducesLastWrittenChecksum1(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestReadChecksumProducesLastWrittenChecksum1";

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), checksum: s_checksum1));
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), checksum: null));
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.False(await storage.ChecksumMatchesAsync(streamName1, s_checksum1));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestReadChecksumProducesLastWrittenChecksum2(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestReadChecksumProducesLastWrittenChecksum2";

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), checksum: null));
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), checksum: s_checksum1));
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.ChecksumMatchesAsync(streamName1, s_checksum1));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestReadChecksumProducesLastWrittenChecksum3(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();

            var streamName1 = "TestReadChecksumProducesLastWrittenChecksum3";

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), checksum: s_checksum1));
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), checksum: s_checksum2));
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.ChecksumMatchesAsync(streamName1, s_checksum2));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionKeyReadWithDocumentKey(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageAsync(solution))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                Assert.True(await storage.ChecksumMatchesAsync(DocumentKey.ToDocumentKey(document), streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(DocumentKey.ToDocumentKey(document), streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionKeyReadWithDocument(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageAsync(solution))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                Assert.True(await storage.ChecksumMatchesAsync(document, streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionReadWithDocumentKey(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageAsync(solution))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.ChecksumMatchesAsync(DocumentKey.ToDocumentKey(document), streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(DocumentKey.ToDocumentKey(document), streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionReadWithDocument(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageAsync(solution))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.ChecksumMatchesAsync(document, streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionReadWithDocumentKeyAndDocument1(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageAsync(solution))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.ChecksumMatchesAsync(DocumentKey.ToDocumentKey(document), streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(DocumentKey.ToDocumentKey(document), streamName1)));

                Assert.True(await storage.ChecksumMatchesAsync(document, streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionReadWithDocumentKeyAndDocument2(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageAsync(solution))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.ChecksumMatchesAsync(document, streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1)));

                Assert.True(await storage.ChecksumMatchesAsync(DocumentKey.ToDocumentKey(document), streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(DocumentKey.ToDocumentKey(document), streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionKeyReadWithDocumentKeyAndDocument1(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageAsync(solution))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                Assert.True(await storage.ChecksumMatchesAsync(DocumentKey.ToDocumentKey(document), streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(DocumentKey.ToDocumentKey(document), streamName1)));

                Assert.True(await storage.ChecksumMatchesAsync(document, streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionKeyReadWithDocumentKeyAndDocument2(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageAsync(solution))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                Assert.True(await storage.ChecksumMatchesAsync(document, streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1)));

                Assert.True(await storage.ChecksumMatchesAsync(DocumentKey.ToDocumentKey(document), streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(DocumentKey.ToDocumentKey(document), streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionKeyReadWithDocumentKey_WriteWithSolutionKey(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                Assert.True(await storage.ChecksumMatchesAsync(DocumentKey.ToDocumentKey(document), streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(DocumentKey.ToDocumentKey(document), streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionKeyReadWithDocument_WriteWithSolutionKey(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                Assert.True(await storage.ChecksumMatchesAsync(document, streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionReadWithDocumentKey_WriteWithSolutionKey(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.ChecksumMatchesAsync(DocumentKey.ToDocumentKey(document), streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(DocumentKey.ToDocumentKey(document), streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionReadWithDocument_WriteWithSolutionKey(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.ChecksumMatchesAsync(document, streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionReadWithDocumentKeyAndDocument1_WriteWithSolutionKey(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.ChecksumMatchesAsync(DocumentKey.ToDocumentKey(document), streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(DocumentKey.ToDocumentKey(document), streamName1)));

                Assert.True(await storage.ChecksumMatchesAsync(document, streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionReadWithDocumentKeyAndDocument2_WriteWithSolutionKey(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.ChecksumMatchesAsync(document, streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1)));

                Assert.True(await storage.ChecksumMatchesAsync(DocumentKey.ToDocumentKey(document), streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(DocumentKey.ToDocumentKey(document), streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionKeyReadWithDocumentKeyAndDocument1_WriteWithSolutionKey(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                Assert.True(await storage.ChecksumMatchesAsync(DocumentKey.ToDocumentKey(document), streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(DocumentKey.ToDocumentKey(document), streamName1)));

                Assert.True(await storage.ChecksumMatchesAsync(document, streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1)));
            }
        }

        [Theory, CombinatorialData]
        public async Task TestOpenWithSolutionKeyReadWithDocumentKeyAndDocument2_WriteWithSolutionKey(Size size, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var document = solution.Projects.Single().Documents.Single();

            var streamName1 = "stream";

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                await storage.WriteStreamAsync(document, streamName1, EncodeString(GetData1(size)), checksum: s_checksum1);
            }

            await using (var storage = await GetStorageFromKeyAsync(solution.Workspace, SolutionKey.ToSolutionKey(solution)))
            {
                Assert.True(await storage.ChecksumMatchesAsync(document, streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1)));

                Assert.True(await storage.ChecksumMatchesAsync(DocumentKey.ToDocumentKey(document), streamName1, s_checksum1));
                Assert.Equal(GetData1(size), ReadStringToEnd(await storage.ReadStreamAsync(DocumentKey.ToDocumentKey(document), streamName1)));
            }
        }

        [Fact, WorkItem(1174219, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1174219")]
        public void CacheDirectoryShouldNotBeAtRoot()
        {
            var workspace = new AdhocWorkspace(FeaturesTestCompositions.Features.GetHostServices());
            workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), new VersionStamp(), @"D:\git\PCLCrypto\PCLCrypto.sln"));

            var configuration = workspace.Services.GetRequiredService<IPersistentStorageConfiguration>();
            var location = configuration.TryGetStorageLocation(SolutionKey.ToSolutionKey(workspace.CurrentSolution));
            Assert.False(location?.StartsWith("/") ?? false);
        }

        [Theory, CombinatorialData]
        public async Task PersistentService_ReadByteTwice(Size size, bool withChecksum, [CombinatorialRange(0, Iterations)] int iteration)
        {
            _ = iteration;
            var solution = CreateOrOpenSolution();
            var streamName1 = "PersistentService_ReadByteTwice";

            await using (var storage = await GetStorageAsync(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(GetData1(size)), GetChecksum1(withChecksum)));
            }

            await using (var storage = await GetStorageAsync(solution))
            {
                using var stream = await storage.ReadStreamAsync(streamName1, GetChecksum1(withChecksum));
                stream.ReadByte();
                stream.ReadByte();
            }
        }

        private static void DoSimultaneousReads(Func<Task<string>> read, string expectedValue)
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

        private static void DoSimultaneousWrites(Func<string, Task> write)
        {
            var barrier = new Barrier(NumThreads);
            var countdown = new CountdownEvent(NumThreads);

            var exceptions = new List<Exception>();
            for (var i = 0; i < NumThreads; i++)
            {
                ThreadPool.QueueUserWorkItem(s =>
                {
                    var id = (int)s;
                    barrier.SignalAndWait();
                    try
                    {
                        write(id + "").Wait();
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }

                    countdown.Signal();
                }, i);
            }

            countdown.Wait();

            Assert.Empty(exceptions);
        }

        protected Solution CreateOrOpenSolution(TempDirectory? persistentFolder = null, bool nullPaths = false)
        {
            persistentFolder ??= _persistentFolder;
            var solutionFile = persistentFolder.CreateOrOpenFile("Solution1.sln").WriteAllText("");

            var info = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(), solutionFile.Path);

            var workspace = new AdhocWorkspace(VisualStudioTestCompositions.LanguageServices.GetHostServices());
            workspace.AddSolution(info);

            var solution = workspace.CurrentSolution;

            var projectFile = persistentFolder.CreateOrOpenFile("Project1.csproj").WriteAllText("");
            solution = solution.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "Project1", "Project1", LanguageNames.CSharp,
                filePath: nullPaths ? null : projectFile.Path));
            var project = solution.Projects.Single();

            var documentFile = persistentFolder.CreateOrOpenFile("Document1.cs").WriteAllText("");
            solution = solution.AddDocument(DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "Document1",
                filePath: nullPaths ? null : documentFile.Path));

            // Apply this to the workspace so our Solution is the primary branch ID, which matches our usual behavior
            workspace.TryApplyChanges(solution);

            return workspace.CurrentSolution;
        }

        internal async Task<IChecksummedPersistentStorage> GetStorageAsync(
            Solution solution,
            TempDirectory? persistentFolder = null,
            IPersistentStorageFaultInjector? faultInjector = null,
            bool throwOnFailure = true)
        {
            // If we handed out one for a previous test, we need to shut that down first
            persistentFolder ??= _persistentFolder;
            _storageService?.GetTestAccessor().Shutdown();
            var configuration = new MockPersistentStorageConfiguration(solution.Id, persistentFolder.Path, throwOnFailure);

            _storageService = GetStorageService((IMefHostExportProvider)solution.Workspace.Services.HostServices, configuration, faultInjector, _persistentFolder.Path);
            var storage = await _storageService.GetStorageAsync(SolutionKey.ToSolutionKey(solution), CancellationToken.None);

            // If we're injecting faults, we expect things to be strange
            if (faultInjector == null)
            {
                Assert.NotEqual(NoOpPersistentStorage.TestAccessor.StorageInstance, storage);
            }

            return storage;
        }

        internal async Task<IChecksummedPersistentStorage> GetStorageFromKeyAsync(
            Workspace workspace, SolutionKey solutionKey, IPersistentStorageFaultInjector? faultInjector = null)
        {
            // If we handed out one for a previous test, we need to shut that down first
            _storageService?.GetTestAccessor().Shutdown();
            var configuration = new MockPersistentStorageConfiguration(solutionKey.Id, _persistentFolder.Path, throwOnFailure: true);

            _storageService = GetStorageService((IMefHostExportProvider)workspace.Services.HostServices, configuration, faultInjector, _persistentFolder.Path);
            var storage = await _storageService.GetStorageAsync(solutionKey, CancellationToken.None);

            // If we're injecting faults, we expect things to be strange
            if (faultInjector == null)
            {
                Assert.NotEqual(NoOpPersistentStorage.TestAccessor.StorageInstance, storage);
            }

            return storage;
        }

        private class MockPersistentStorageConfiguration : IPersistentStorageConfiguration
        {
            private readonly SolutionId _solutionId;
            private readonly string _storageLocation;
            private readonly bool _throwOnFailure;

            public MockPersistentStorageConfiguration(SolutionId solutionId, string storageLocation, bool throwOnFailure)
            {
                _solutionId = solutionId;
                _storageLocation = storageLocation;
                _throwOnFailure = throwOnFailure;
            }

            public bool ThrowOnFailure => _throwOnFailure;

            public string? TryGetStorageLocation(SolutionKey solutionKey)
                => solutionKey.Id == _solutionId ? _storageLocation : null;
        }

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
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                return _encoding.GetString(memoryStream.ToArray());
            }
        }
    }
}
