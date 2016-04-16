// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices
{
    public class PersistentStorageTests : IDisposable
    {
        private const int NumThreads = 10;
        private const string PersistentFolderPrefix = "PersistentStorageTests_";

        private readonly Encoding _encoding = Encoding.UTF8;
        private readonly IOptionService _persistentEnabledOptionService = new OptionServiceMock(new Dictionary<IOption, object>
        {
            { PersistentStorageOptions.Enabled, true },
            { InternalFeatureOnOffOptions.EsentPerformanceMonitor, false }
        });

        private readonly string _persistentFolder;

        private const string Data1 = "Hello ESENT";
        private const string Data2 = "Goodbye ESENT";

        public PersistentStorageTests()
        {
            _persistentFolder = Path.Combine(Path.GetTempPath(), PersistentFolderPrefix + Guid.NewGuid());
            Directory.CreateDirectory(_persistentFolder);
            int workerThreads, completionPortThreads;
            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            ThreadPool.SetMinThreads(Math.Max(workerThreads, NumThreads), completionPortThreads);
        }

        public void Dispose()
        {
            if (Directory.Exists(_persistentFolder))
            {
                Directory.Delete(_persistentFolder, true);
            }
        }

        private void CleanUpPersistentFolder()
        {
        }

        [Fact]
        public async Task PersistentService_Solution_WriteReadDifferentInstances()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Solution_WriteReadDifferentInstances1";
            var streamName2 = "PersistentService_Solution_WriteReadDifferentInstances2";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(Data1)));
                Assert.True(await storage.WriteStreamAsync(streamName2, EncodeString(Data2)));
            }

            using (var storage = GetStorage(solution))
            {
                Assert.Equal(Data1, ReadStringToEnd(await storage.ReadStreamAsync(streamName1)));
                Assert.Equal(Data2, ReadStringToEnd(await storage.ReadStreamAsync(streamName2)));
            }
        }

        [Fact]
        public async Task PersistentService_Solution_WriteReadReopenSolution()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Solution_WriteReadReopenSolution1";
            var streamName2 = "PersistentService_Solution_WriteReadReopenSolution2";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(Data1)));
                Assert.True(await storage.WriteStreamAsync(streamName2, EncodeString(Data2)));
            }

            solution = CreateOrOpenSolution();

            using (var storage = GetStorage(solution))
            {
                Assert.Equal(Data1, ReadStringToEnd(await storage.ReadStreamAsync(streamName1)));
                Assert.Equal(Data2, ReadStringToEnd(await storage.ReadStreamAsync(streamName2)));
            }
        }

        [Fact]
        public async Task PersistentService_Solution_WriteReadSameInstance()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Solution_WriteReadSameInstance1";
            var streamName2 = "PersistentService_Solution_WriteReadSameInstance2";

            using (var storage = GetStorage(solution))
            {
                Assert.True(await storage.WriteStreamAsync(streamName1, EncodeString(Data1)));
                Assert.True(await storage.WriteStreamAsync(streamName2, EncodeString(Data2)));

                Assert.Equal(Data1, ReadStringToEnd(await storage.ReadStreamAsync(streamName1)));
                Assert.Equal(Data2, ReadStringToEnd(await storage.ReadStreamAsync(streamName2)));
            }
        }

        [Fact]
        public async Task PersistentService_Project_WriteReadSameInstance()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Project_WriteReadSameInstance1";
            var streamName2 = "PersistentService_Project_WriteReadSameInstance2";

            using (var storage = GetStorage(solution))
            {
                var project = solution.Projects.Single();

                Assert.True(await storage.WriteStreamAsync(project, streamName1, EncodeString(Data1)));
                Assert.True(await storage.WriteStreamAsync(project, streamName2, EncodeString(Data2)));

                Assert.Equal(Data1, ReadStringToEnd(await storage.ReadStreamAsync(project, streamName1)));
                Assert.Equal(Data2, ReadStringToEnd(await storage.ReadStreamAsync(project, streamName2)));
            }
        }

        [Fact]
        public async Task PersistentService_Document_WriteReadSameInstance()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Document_WriteReadSameInstance1";
            var streamName2 = "PersistentService_Document_WriteReadSameInstance2";

            using (var storage = GetStorage(solution))
            {
                var document = solution.Projects.Single().Documents.Single();

                Assert.True(await storage.WriteStreamAsync(document, streamName1, EncodeString(Data1)));
                Assert.True(await storage.WriteStreamAsync(document, streamName2, EncodeString(Data2)));

                Assert.Equal(Data1, ReadStringToEnd(await storage.ReadStreamAsync(document, streamName1)));
                Assert.Equal(Data2, ReadStringToEnd(await storage.ReadStreamAsync(document, streamName2)));
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

            var streamName1 = "PersistentService_Solution_SimultaneousReads1";

            using (var storage = GetStorage(solution))
            {
                await storage.WriteStreamAsync(streamName1, EncodeString(Data1));
                DoSimultaneousReads(async () => ReadStringToEnd(await storage.ReadStreamAsync(streamName1)), Data1);
            }
        }

        [Fact]
        public async Task PersistentService_Project_SimultaneousReads()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Project_SimultaneousReads1";

            using (var storage = GetStorage(solution))
            {
                await storage.WriteStreamAsync(solution.Projects.Single(), streamName1, EncodeString(Data1));
                DoSimultaneousReads(async () => ReadStringToEnd(await storage.ReadStreamAsync(solution.Projects.Single(), streamName1)), Data1);
            }
        }

        [Fact]
        public async Task PersistentService_Document_SimultaneousReads()
        {
            var solution = CreateOrOpenSolution();

            var streamName1 = "PersistentService_Document_SimultaneousReads1";

            using (var storage = GetStorage(solution))
            {
                await storage.WriteStreamAsync(solution.Projects.Single().Documents.Single(), streamName1, EncodeString(Data1));
                DoSimultaneousReads(async () => ReadStringToEnd(await storage.ReadStreamAsync(solution.Projects.Single().Documents.Single(), streamName1)), Data1);
            }
        }

        private void DoSimultaneousReads(Func<Task<string>> read, string expectedValue)
        {
            var barrier = new Barrier(NumThreads);
            var countdown = new CountdownEvent(NumThreads);

            for (int i = 0; i < NumThreads; i++)
            {
                Task.Run(async () =>
                {
                    barrier.SignalAndWait();
                    Assert.Equal(expectedValue, await read());
                    countdown.Signal();
                });
            }

            countdown.Wait();
        }

        [Fact]
        public async Task PersistentService_IdentifierSet()
        {
            var solution = CreateOrOpenSolution();

            var newId = DocumentId.CreateNewId(solution.ProjectIds[0]);

            string documentFile = Path.Combine(Path.GetDirectoryName(solution.FilePath), "IdentifierSet.cs");

            File.WriteAllText(documentFile, @"
class A
{
    public int Test(int i, A a)
    {
        return a;
    }
}");

            var newSolution = solution.AddDocument(DocumentInfo.Create(newId, "IdentifierSet", loader: new FileTextLoader(documentFile, Encoding.UTF8), filePath: documentFile));

            using (var storage = GetStorage(newSolution))
            {
                var syntaxTreeStorage = storage as ISyntaxTreeInfoPersistentStorage;
                Assert.NotNull(syntaxTreeStorage);

                var document = newSolution.GetDocument(newId);
                var version = await document.GetSyntaxVersionAsync();
                var root = await document.GetSyntaxRootAsync();

                Assert.True(syntaxTreeStorage.WriteIdentifierLocations(document, version, root, CancellationToken.None));

                Assert.Equal(version, syntaxTreeStorage.GetIdentifierSetVersion(document));

                List<int> positions = new List<int>();
                Assert.True(syntaxTreeStorage.ReadIdentifierPositions(document, version, "Test", positions, CancellationToken.None));

                Assert.Equal(1, positions.Count);
                Assert.Equal(29, positions[0]);
            }
        }

        private Solution CreateOrOpenSolution()
        {
            string solutionFile = Path.Combine(_persistentFolder, "Solution1.sln");
            bool newSolution;
            if (newSolution = !File.Exists(solutionFile))
            {
                File.WriteAllText(solutionFile, "");
            }

            var info = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(), solutionFile);

            var workspace = new AdhocWorkspace();
            workspace.AddSolution(info);

            var solution = workspace.CurrentSolution;

            if (newSolution)
            {
                string projectFile = Path.Combine(Path.GetDirectoryName(solutionFile), "Project1.csproj");
                File.WriteAllText(projectFile, "");
                solution = solution.AddProject(ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "Project1", "Project1", LanguageNames.CSharp, projectFile));
                var project = solution.Projects.Single();

                string documentFile = Path.Combine(Path.GetDirectoryName(projectFile), "Document1.cs");
                File.WriteAllText(documentFile, "");
                solution = solution.AddDocument(DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "Document1", filePath: documentFile));
            }

            return solution;
        }

        private IPersistentStorage GetStorage(Solution solution)
        {
            var storage = new PersistentStorageService(_persistentEnabledOptionService, testing: true).GetStorage(solution);

            Assert.NotEqual(PersistentStorageService.NoOpPersistentStorageInstance, storage);
            return storage;
        }

        private Stream EncodeString(string text)
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
