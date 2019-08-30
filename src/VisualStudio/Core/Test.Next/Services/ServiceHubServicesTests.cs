// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttributes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Shared;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.TodoComments;
using Nerdbank;
using Roslyn.Test.Utilities.Remote;
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

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestRemoteHostCreation()
        {
            using (var remoteHostService = CreateService())
            {
                Assert.NotNull(remoteHostService);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void TestRemoteHostConnect()
        {
            using (var remoteHostService = CreateService())
            {
                var input = "Test";
                var output = remoteHostService.Connect(input, uiCultureLCID: 0, cultureLCID: 0, serializedSession: null, cancellationToken: CancellationToken.None);

                Assert.Equal(input, output);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestRemoteHostSynchronize()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                var client = (InProcRemoteHostClient)(await InProcRemoteHostClient.CreateAsync(workspace, runCacheCleanup: false));

                var solution = workspace.CurrentSolution;

                await UpdatePrimaryWorkspace(client, solution);
                await VerifyAssetStorageAsync(client, solution);

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
                var client = (InProcRemoteHostClient)(await InProcRemoteHostClient.CreateAsync(workspace, runCacheCleanup: false));

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
                await client.TryRunRemoteAsync(WellKnownRemoteHostServices.RemoteHostService, nameof(IRemoteHostService.SynchronizeTextAsync),
                    new object[] { oldDocument.Id, oldState.Text, newText.GetTextChanges(oldText) }, CancellationToken.None);

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
            var code = @"// TODO: Test";

            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                var client = (InProcRemoteHostClient)(await InProcRemoteHostClient.CreateAsync(workspace, runCacheCleanup: false));

                var solution = workspace.CurrentSolution;

                var comments = await client.TryRunCodeAnalysisRemoteAsync<IList<TodoComment>>(
                    solution,
                    nameof(IRemoteTodoCommentService.GetTodoCommentsAsync),
                    new object[] { solution.Projects.First().DocumentIds.First(), ImmutableArray.Create(new TodoCommentDescriptor("TODO", 0)) },
                    CancellationToken.None);

                Assert.Equal(comments.Count, 1);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestDesignerAttributes()
        {
            var code = @"[System.ComponentModel.DesignerCategory(""Form"")]
                class Test { }";

            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                var client = (InProcRemoteHostClient)(await InProcRemoteHostClient.CreateAsync(workspace, runCacheCleanup: false));

                var solution = workspace.CurrentSolution;

                var result = await client.TryRunCodeAnalysisRemoteAsync<DesignerAttributeResult>(
                    solution,
                    nameof(IRemoteDesignerAttributeService.ScanDesignerAttributesAsync),
                    solution.Projects.First().DocumentIds.First(),
                    CancellationToken.None);

                Assert.Equal(result.DesignerAttributeArgument, "Form");
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestRemoteHostSynchronizeGlobalAssets()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                var client = (InProcRemoteHostClient)(await InProcRemoteHostClient.CreateAsync(workspace, runCacheCleanup: false));

                await client.TryRunRemoteAsync(
                    WellKnownRemoteHostServices.RemoteHostService,
                    workspace.CurrentSolution,
                    nameof(IRemoteHostService.SynchronizeGlobalAssetsAsync),
                    new object[] { new Checksum[0] { } }, CancellationToken.None);

                var storage = client.AssetStorage;
                Assert.Equal(0, storage.GetGlobalAssetsOfType<object>(CancellationToken.None).Count());
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestUnknownProject()
        {
            var workspace = new AdhocWorkspace(TestHostServices.CreateHostServices());
            var solution = workspace.CurrentSolution.AddProject("unknown", "unknown", NoCompilationConstants.LanguageName).Solution;

            var client = (InProcRemoteHostClient)(await InProcRemoteHostClient.CreateAsync(workspace, runCacheCleanup: false));

            await UpdatePrimaryWorkspace(client, solution);
            await VerifyAssetStorageAsync(client, solution);

            Assert.Equal(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await RemoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestRemoteHostSynchronizeIncrementalUpdate()
        {
            using (var workspace = TestWorkspace.CreateCSharp(Array.Empty<string>(), metadataReferences: null))
            {
                var client = (InProcRemoteHostClient)(await InProcRemoteHostClient.CreateAsync(workspace, runCacheCleanup: false));

                var solution = Populate(workspace.CurrentSolution.RemoveProject(workspace.CurrentSolution.ProjectIds.First()));

                // verify initial setup
                await UpdatePrimaryWorkspace(client, solution);
                await VerifyAssetStorageAsync(client, solution);

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

                var remoteWorkspace = new RemoteWorkspace(workspaceKind: "test");

                // this shouldn't throw exception
                remoteWorkspace.TryAddSolutionIfPossible(solutionInfo, workspaceVersion: 1, out var solution);
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
            var (project, document) = GetProjectAndDocument(solution, projectName, documentName);

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
            await client.TryRunRemoteAsync(
                WellKnownRemoteHostServices.RemoteHostService, solution,
                nameof(IRemoteHostService.SynchronizePrimaryWorkspaceAsync),
                new object[] { await solution.State.GetChecksumAsync(CancellationToken.None), _solutionVersion++ },
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
