// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.CodeAnalysis.UnitTests;
using Nerdbank;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Roslyn.VisualStudio.Next.UnitTests.Mocks;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    [UseExportProvider]
    public class ServiceHubServicesTests
    {
        private static RemoteWorkspace RemoteWorkspace
        {
            get
            {
                var primaryWorkspace = ((IMefHostExportProvider)RoslynServices.HostServices).GetExports<PrimaryWorkspace>().Single().Value;
                return (RemoteWorkspace)primaryWorkspace.Workspace;
            }
        }

        private static Solution WithChangedOptionsFromRemoteWorkspace(Solution solution)
            => solution.WithChangedOptionsFrom(RemoteWorkspace.Options);

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestRemoteHostCreation()
        {
            using (var remoteHostService = CreateService())
            {
                Assert.NotNull(remoteHostService);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestRemoteHostSynchronize()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                var client = (InProcRemoteHostClient)await InProcRemoteHostClient.CreateAsync(workspace.Services, runCacheCleanup: false);

                var solution = workspace.CurrentSolution;

                await UpdatePrimaryWorkspace(client, solution);
                await VerifyAssetStorageAsync(client, solution);

                solution = WithChangedOptionsFromRemoteWorkspace(solution);

                Assert.Equal(
                    await solution.State.GetChecksumAsync(CancellationToken.None),
                    await RemoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestRemoteHostTextSynchronize()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                var client = (InProcRemoteHostClient)await InProcRemoteHostClient.CreateAsync(workspace.Services, runCacheCleanup: false);

                var solution = workspace.CurrentSolution;

                // sync base solution
                await UpdatePrimaryWorkspace(client, solution);
                await VerifyAssetStorageAsync(client, solution);

                // get basic info
                var oldDocument = solution.Projects.First().Documents.First();
                var oldState = await oldDocument.State.GetStateChecksumsAsync(CancellationToken.None);
                var oldText = await oldDocument.GetTextAsync();

                // update text
                var newText = oldText.WithChanges(new TextChange(TextSpan.FromBounds(0, 0), "/* test */"));

                // sync
                await client.RunRemoteAsync(
                    WellKnownServiceHubService.RemoteHost,
                    nameof(IRemoteHostService.SynchronizeTextAsync),
                    solution: null,
                    new object[] { oldDocument.Id, oldState.Text, newText.GetTextChanges(oldText) },
                    callbackTarget: null,
                    CancellationToken.None);

                // apply change to solution
                var newDocument = oldDocument.WithText(newText);
                var newState = await newDocument.State.GetStateChecksumsAsync(CancellationToken.None);

                // check that text already exist in remote side
                Assert.True(client.AssetStorage.TryGetAsset<SourceText>(newState.Text, out var remoteText));
                Assert.Equal(newText.ToString(), remoteText.ToString());
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestTodoComments()
        {
            using var workspace = TestWorkspace.CreateCSharp(@"

// TODO: Test");

            var cancellationTokenSource = new CancellationTokenSource();

            var solution = workspace.CurrentSolution;

            // Ensure remote workspace is in sync with normal workspace.
            var solutionService = await GetSolutionServiceAsync(solution);
            var solutionChecksum = await solution.State.GetChecksumAsync(CancellationToken.None);
            await solutionService.UpdatePrimaryWorkspaceAsync(solutionChecksum, solution.WorkspaceVersion, CancellationToken.None);

            var callback = new TodoCommentsListener();

            using var client = await InProcRemoteHostClient.CreateAsync(workspace.Services, runCacheCleanup: false);
            using var connection = await client.CreateConnectionAsync(
                WellKnownServiceHubService.RemoteTodoCommentsService,
                callback,
                cancellationTokenSource.Token);

            var invokeTask = connection.RunRemoteAsync(
                nameof(IRemoteTodoCommentsService.ComputeTodoCommentsAsync),
                solution: null,
                arguments: Array.Empty<object>(),
                cancellationTokenSource.Token);

            var data = await callback.Data;
            Assert.Equal(solution.Projects.Single().Documents.Single().Id, data.Item1);
            Assert.Equal(1, data.Item2.Length);

            var commentInfo = data.Item2[0];
            Assert.Equal(new TodoCommentData
            {
                DocumentId = solution.Projects.Single().Documents.Single().Id,
                Priority = 1,
                Message = "TODO: Test",
                MappedFilePath = null,
                OriginalFilePath = "test1.cs",
                OriginalLine = 2,
                MappedLine = 2,
                OriginalColumn = 3,
                MappedColumn = 3,
            }, commentInfo);

            cancellationTokenSource.Cancel();

            await invokeTask;
        }

        private class TodoCommentsListener : ITodoCommentsListener
        {
            private readonly TaskCompletionSource<(DocumentId, ImmutableArray<TodoCommentData>)> _dataSource
                = new TaskCompletionSource<(DocumentId, ImmutableArray<TodoCommentData>)>();
            public Task<(DocumentId, ImmutableArray<TodoCommentData>)> Data => _dataSource.Task;

            public Task ReportTodoCommentDataAsync(DocumentId documentId, ImmutableArray<TodoCommentData> data, CancellationToken cancellationToken)
            {
                _dataSource.SetResult((documentId, data));
                return Task.CompletedTask;
            }
        }

        private static async Task<SolutionService> GetSolutionServiceAsync(Solution solution, Dictionary<Checksum, object> map = null)
        {
            // make sure checksum is calculated
            await solution.State.GetChecksumAsync(CancellationToken.None);

            map ??= new Dictionary<Checksum, object>();
            await solution.AppendAssetMapAsync(map, CancellationToken.None);

            var sessionId = 0;
            var storage = new AssetStorage();
            storage.Initialize(new SimpleAssetSource(map));
            var remoteWorkspace = new RemoteWorkspace(applyStartupOptions: false);

            return new SolutionService(new AssetProvider(sessionId, storage, remoteWorkspace.Services.GetService<ISerializerService>()));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestDesignerAttributes()
        {
            using var workspace = TestWorkspace.CreateCSharp(
@"[System.ComponentModel.DesignerCategory(""Form"")]
class Test { }");

            var cancellationTokenSource = new CancellationTokenSource();

            var solution = workspace.CurrentSolution;

            // Ensure remote workspace is in sync with normal workspace.
            var solutionService = await GetSolutionServiceAsync(solution);
            var solutionChecksum = await solution.State.GetChecksumAsync(CancellationToken.None);
            await solutionService.UpdatePrimaryWorkspaceAsync(solutionChecksum, solution.WorkspaceVersion, CancellationToken.None);

            var callback = new DesignerAttributeListener();

            using var client = await InProcRemoteHostClient.CreateAsync(workspace.Services, runCacheCleanup: false);
            using var connection = await client.CreateConnectionAsync(
                WellKnownServiceHubService.RemoteDesignerAttributeService,
                callback,
                cancellationTokenSource.Token);

            var invokeTask = connection.RunRemoteAsync(
                nameof(IRemoteDesignerAttributeService.StartScanningForDesignerAttributesAsync),
                solution: null,
                arguments: Array.Empty<object>(),
                cancellationTokenSource.Token);

            var infos = await callback.Infos;
            Assert.Equal(1, infos.Length);

            var info = infos[0];
            Assert.Equal("Form", info.Category);
            Assert.Equal(solution.Projects.Single().Documents.Single().Id, info.DocumentId);

            cancellationTokenSource.Cancel();

            await invokeTask;
        }

        private class DesignerAttributeListener : IDesignerAttributeListener
        {
            private readonly TaskCompletionSource<ImmutableArray<DesignerAttributeData>> _infosSource
                = new TaskCompletionSource<ImmutableArray<DesignerAttributeData>>();
            public Task<ImmutableArray<DesignerAttributeData>> Infos => _infosSource.Task;

            public Task OnProjectRemovedAsync(ProjectId projectId, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task ReportDesignerAttributeDataAsync(ImmutableArray<DesignerAttributeData> infos, CancellationToken cancellationToken)
            {
                _infosSource.SetResult(infos);
                return Task.CompletedTask;
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestUnknownProject()
        {
            var workspace = new AdhocWorkspace(TestHostServices.CreateHostServices());
            var solution = workspace.CurrentSolution.AddProject("unknown", "unknown", NoCompilationConstants.LanguageName).Solution;

            var client = (InProcRemoteHostClient)await InProcRemoteHostClient.CreateAsync(workspace.Services, runCacheCleanup: false);

            await UpdatePrimaryWorkspace(client, solution);
            await VerifyAssetStorageAsync(client, solution);

            // Only C# and VB projects are supported in Remote workspace.
            // See "RemoteSupportedLanguages.IsSupported"
            Assert.Empty(RemoteWorkspace.CurrentSolution.Projects);

            solution = WithChangedOptionsFromRemoteWorkspace(solution);

            Assert.NotEqual(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await RemoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));

            solution = solution.RemoveProject(solution.ProjectIds.Single());
            solution = WithChangedOptionsFromRemoteWorkspace(solution);

            Assert.Equal(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await RemoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestRemoteHostSynchronizeIncrementalUpdate()
        {
            using var workspace = new TestWorkspace();

            var client = (InProcRemoteHostClient)await InProcRemoteHostClient.CreateAsync(workspace.Services, runCacheCleanup: false);

            var solution = Populate(workspace.CurrentSolution);

            // verify initial setup
            await UpdatePrimaryWorkspace(client, solution);
            await VerifyAssetStorageAsync(client, solution);

            solution = WithChangedOptionsFromRemoteWorkspace(solution);

            Assert.Equal(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await RemoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));

            // incrementally update
            solution = await VerifyIncrementalUpdatesAsync(client, solution, csAddition: " ", vbAddition: " ");

            Assert.Equal(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await RemoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));

            // incrementally update
            solution = await VerifyIncrementalUpdatesAsync(client, solution, csAddition: "\r\nclass Addition { }", vbAddition: "\r\nClass VB\r\nEnd Class");

            Assert.Equal(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await RemoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestRemoteWorkspaceCircularReferences()
        {
            using (var tempRoot = new Microsoft.CodeAnalysis.Test.Utilities.TempRoot())
            {
                var file = tempRoot.CreateDirectory().CreateFile("p1.dll");
                file.CopyContentFrom(typeof(object).Assembly.Location);

                var p1 = ProjectId.CreateNewId();
                var p2 = ProjectId.CreateNewId();

                var solutionInfo = SolutionInfo.Create(
                    SolutionId.CreateNewId(), VersionStamp.Create(), "",
                    new[]
                    {
                        ProjectInfo.Create(
                            p1, VersionStamp.Create(), "p1", "p1", LanguageNames.CSharp, outputFilePath: file.Path,
                            projectReferences: new [] { new ProjectReference(p2) }),
                        ProjectInfo.Create(
                            p2, VersionStamp.Create(), "p2", "p2", LanguageNames.CSharp,
                            metadataReferences: new [] { MetadataReference.CreateFromFile(file.Path) })
                    });

                var languages = ImmutableHashSet.Create(LanguageNames.CSharp);
                var remoteWorkspace = new RemoteWorkspace(workspaceKind: "test");
                var optionService = remoteWorkspace.Services.GetRequiredService<IOptionService>();
                var options = new SerializableOptionSet(languages, optionService, ImmutableHashSet<IOption>.Empty, ImmutableDictionary<OptionKey, object>.Empty, ImmutableHashSet<OptionKey>.Empty);

                // this shouldn't throw exception
                remoteWorkspace.TryAddSolutionIfPossible(solutionInfo, workspaceVersion: 1, options, out var solution);
                Assert.NotNull(solution);
            }
        }

        private async Task<Solution> VerifyIncrementalUpdatesAsync(InProcRemoteHostClient client, Solution solution, string csAddition, string vbAddition)
        {
            var remoteSolution = RemoteWorkspace.CurrentSolution;
            var projectIds = solution.ProjectIds;

            for (var i = 0; i < projectIds.Count; i++)
            {
                var projectName = $"Project{i}";
                var project = solution.GetProject(projectIds[i]);

                var documentIds = project.DocumentIds;
                for (var j = 0; j < documentIds.Count; j++)
                {
                    var documentName = $"Document{j}";

                    var currentSolution = UpdateSolution(solution, projectName, documentName, csAddition, vbAddition);
                    await UpdatePrimaryWorkspace(client, currentSolution);

                    var currentRemoteSolution = RemoteWorkspace.CurrentSolution;
                    VerifyStates(remoteSolution, currentRemoteSolution, projectName, documentName);

                    solution = currentSolution;
                    remoteSolution = currentRemoteSolution;

                    Assert.Equal(
                        await solution.State.GetChecksumAsync(CancellationToken.None),
                        await remoteSolution.State.GetChecksumAsync(CancellationToken.None));
                }
            }

            return solution;
        }

        private static void VerifyStates(Solution solution1, Solution solution2, string projectName, string documentName)
        {
            Assert.True(solution1.Workspace is RemoteWorkspace);
            Assert.True(solution2.Workspace is RemoteWorkspace);

            SetEqual(solution1.ProjectIds, solution2.ProjectIds);

            var (project, document) = GetProjectAndDocument(solution1, projectName, documentName);

            var projectId = project.Id;
            var documentId = document.Id;

            var projectIds = solution1.ProjectIds;
            for (var i = 0; i < projectIds.Count; i++)
            {
                var currentProjectId = projectIds[i];

                var projectStateShouldSame = projectId != currentProjectId;
                Assert.Equal(projectStateShouldSame, object.ReferenceEquals(solution1.GetProject(currentProjectId).State, solution2.GetProject(currentProjectId).State));

                if (!projectStateShouldSame)
                {
                    SetEqual(solution1.GetProject(currentProjectId).DocumentIds, solution2.GetProject(currentProjectId).DocumentIds);

                    var documentIds = solution1.GetProject(currentProjectId).DocumentIds;
                    for (var j = 0; j < documentIds.Count; j++)
                    {
                        var currentDocumentId = documentIds[j];

                        var documentStateShouldSame = documentId != currentDocumentId;
                        Assert.Equal(documentStateShouldSame, object.ReferenceEquals(solution1.GetDocument(currentDocumentId).State, solution2.GetDocument(currentDocumentId).State));
                    }
                }
            }
        }

        private static async Task VerifyAssetStorageAsync(InProcRemoteHostClient client, Solution solution)
        {
            var map = await solution.GetAssetMapAsync(CancellationToken.None);

            var storage = client.AssetStorage;

            TestUtils.VerifyAssetStorage(map, storage);
        }

        private static Solution UpdateSolution(Solution solution, string projectName, string documentName, string csAddition, string vbAddition)
        {
            var (_, document) = GetProjectAndDocument(solution, projectName, documentName);

            return document.WithText(GetNewText(document, csAddition, vbAddition)).Project.Solution;
        }

        private static SourceText GetNewText(Document document, string csAddition, string vbAddition)
        {
            if (document.Project.Language == LanguageNames.CSharp)
            {
                return SourceText.From(document.State.GetTextSynchronously(CancellationToken.None).ToString() + csAddition);
            }

            return SourceText.From(document.State.GetTextSynchronously(CancellationToken.None).ToString() + vbAddition);
        }

        private static (Project, Document) GetProjectAndDocument(Solution solution, string projectName, string documentName)
        {
            var project = solution.Projects.First(p => string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));
            var document = project.Documents.First(d => string.Equals(d.Name, documentName, StringComparison.OrdinalIgnoreCase));

            return (project, document);
        }

        // make sure we always move remote workspace forward
        private int _solutionVersion = 0;

        private async Task UpdatePrimaryWorkspace(InProcRemoteHostClient client, Solution solution)
        {
            await client.RunRemoteAsync(
                WellKnownServiceHubService.RemoteHost,
                nameof(IRemoteHostService.SynchronizePrimaryWorkspaceAsync),
                solution,
                new object[] { await solution.State.GetChecksumAsync(CancellationToken.None), _solutionVersion++ },
                callbackTarget: null,
                CancellationToken.None);
        }

        private static Solution Populate(Solution solution)
        {
            solution = AddProject(solution, LanguageNames.CSharp, new[]
            {
                "class CS { }",
                "class CS2 { }"
            }, new[]
            {
                "cs additional file content"
            }, Array.Empty<ProjectId>());

            solution = AddProject(solution, LanguageNames.VisualBasic, new[]
            {
                "Class VB\r\nEnd Class",
                "Class VB2\r\nEnd Class"
            }, new[]
            {
                "vb additional file content"
            }, new ProjectId[] { solution.ProjectIds.First() });

            solution = AddProject(solution, LanguageNames.CSharp, new[]
            {
                "class Top { }"
            }, new[]
            {
                "cs additional file content"
            }, solution.ProjectIds.ToArray());

            solution = AddProject(solution, LanguageNames.CSharp, new[]
            {
                "class OrphanCS { }",
                "class OrphanCS2 { }"
            }, new[]
            {
                "cs additional file content",
                "cs additional file content2"
            }, Array.Empty<ProjectId>());

            return solution;
        }

        private static Solution AddProject(Solution solution, string language, string[] documents, string[] additionalDocuments, ProjectId[] p2pReferences)
        {
            var projectName = $"Project{solution.ProjectIds.Count}";
            var project = solution.AddProject(projectName, $"{projectName}.dll", language)
                                  .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                                  .AddAnalyzerReference(new AnalyzerFileReference(typeof(object).Assembly.Location, new TestAnalyzerAssemblyLoader()));

            var projectId = project.Id;
            solution = project.Solution;

            for (var i = 0; i < documents.Length; i++)
            {
                var current = solution.GetProject(projectId);
                solution = current.AddDocument($"Document{i}", SourceText.From(documents[i])).Project.Solution;
            }

            for (var i = 0; i < additionalDocuments.Length; i++)
            {
                var current = solution.GetProject(projectId);
                solution = current.AddAdditionalDocument($"AdditionalDocument{i}", SourceText.From(additionalDocuments[i])).Project.Solution;
            }

            for (var i = 0; i < p2pReferences.Length; i++)
            {
                var current = solution.GetProject(projectId);
                solution = current.AddProjectReference(new ProjectReference(p2pReferences[i])).Solution;
            }

            return solution;
        }

        private static RemoteHostService CreateService()
        {
            var tuple = FullDuplexStream.CreateStreams();
            return new RemoteHostService(tuple.Item1, new InProcRemoteHostClient.ServiceProvider(runCacheCleanup: false));
        }

        private static void SetEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            var expectedSet = new HashSet<T>(expected);
            var result = expected.Count() == actual.Count() && expectedSet.SetEquals(actual);
            if (!result)
            {
                Assert.True(result);
            }
        }
    }
}
