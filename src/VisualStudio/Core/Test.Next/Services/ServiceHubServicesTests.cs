﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Threading;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;
using Xunit.Sdk;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.RemoteHost)]
    public class ServiceHubServicesTests
    {
        private static TestWorkspace CreateWorkspace(Type[] additionalParts = null)
             => new(composition: FeaturesTestCompositions.Features.WithTestHostParts(TestHost.OutOfProcess).AddParts(additionalParts));

        [Fact]
        public async Task TestRemoteHostSynchronize()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = CreateWorkspace();
            workspace.InitializeDocuments(LanguageNames.CSharp, files: [code], openDocuments: false);

            using var client = await InProcRemoteHostClient.GetTestClientAsync(workspace).ConfigureAwait(false);

            var solution = workspace.CurrentSolution;

            await UpdatePrimaryWorkspace(client, solution);
            await VerifyAssetStorageAsync(client, solution);

            var remoteWorkpace = client.GetRemoteWorkspace();

            Assert.Equal(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await remoteWorkpace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));
        }

        [Fact]
        public async Task TestRemoteHostTextSynchronize()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = CreateWorkspace();
            workspace.InitializeDocuments(LanguageNames.CSharp, files: [code], openDocuments: false);

            var client = await InProcRemoteHostClient.GetTestClientAsync(workspace).ConfigureAwait(false);

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
            await client.TryInvokeAsync<IRemoteAssetSynchronizationService>(
                (service, cancellationToken) => service.SynchronizeTextAsync(oldDocument.Id, oldState.Text, newText.GetTextChanges(oldText), cancellationToken),
                CancellationToken.None);

            // apply change to solution
            var newDocument = oldDocument.WithText(newText);
            var newState = await newDocument.State.GetStateChecksumsAsync(CancellationToken.None);

            // check that text already exist in remote side
            Assert.True(client.TestData.WorkspaceManager.SolutionAssetCache.TryGetAsset<SerializableSourceText>(newState.Text, out var serializableRemoteText));
            Assert.Equal(newText.ToString(), (await serializableRemoteText.GetTextAsync(CancellationToken.None)).ToString());
        }

        private static async Task<AssetProvider> GetAssetProviderAsync(Workspace workspace, Workspace remoteWorkspace, Solution solution, Dictionary<Checksum, object> map = null)
        {
            // make sure checksum is calculated
            await solution.State.GetChecksumAsync(CancellationToken.None);

            map ??= new Dictionary<Checksum, object>();
            await solution.AppendAssetMapAsync(map, CancellationToken.None);

            var sessionId = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            var storage = new SolutionAssetCache();
            var assetSource = new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map);

            return new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.GetService<ISerializerService>());
        }

        [Fact]
        public async Task TestDesignerAttributes()
        {
            var source = @"[System.ComponentModel.DesignerCategory(""Form"")] class Test { }";

            using var workspace = CreateWorkspace();
            workspace.InitializeDocuments(LanguageNames.CSharp, files: [source], openDocuments: false);

            using var client = await InProcRemoteHostClient.GetTestClientAsync(workspace).ConfigureAwait(false);
            var remoteWorkspace = client.GetRemoteWorkspace();

            // Start solution crawler in the remote workspace:
            await client.TryInvokeAsync<IRemoteDiagnosticAnalyzerService>(
                (service, cancellationToken) => service.StartSolutionCrawlerAsync(cancellationToken),
                CancellationToken.None).ConfigureAwait(false);

            var cancellationTokenSource = new CancellationTokenSource();
            var solution = workspace.CurrentSolution;

            // Ensure remote workspace is in sync with normal workspace.
            var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution);
            var solutionChecksum = await solution.State.GetChecksumAsync(CancellationToken.None);
            await remoteWorkspace.UpdatePrimaryBranchSolutionAsync(assetProvider, solutionChecksum, solution.WorkspaceVersion, CancellationToken.None);

            var callback = new DesignerAttributeComputerCallback();

            using var connection = client.CreateConnection<IRemoteDesignerAttributeDiscoveryService>(callback);

            var invokeTask = connection.TryInvokeAsync(
                solution,
                (service, checksum, callbackId, cancellationToken) => service.DiscoverDesignerAttributesAsync(
                    callbackId, checksum, priorityDocument: null, useFrozenSnapshots: true, cancellationToken),
                cancellationTokenSource.Token);

            var infos = await callback.Infos;
            Assert.Equal(1, infos.Length);

            var info = infos[0];
            Assert.Equal("Form", info.Category);
            Assert.Equal(solution.Projects.Single().Documents.Single().Id, info.DocumentId);

            cancellationTokenSource.Cancel();

            Assert.True(await invokeTask);
        }

        private class DesignerAttributeComputerCallback : IDesignerAttributeDiscoveryService.ICallback
        {
            private readonly TaskCompletionSource<ImmutableArray<DesignerAttributeData>> _infosSource = new();

            public Task<ImmutableArray<DesignerAttributeData>> Infos => _infosSource.Task;

            public ValueTask ReportDesignerAttributeDataAsync(ImmutableArray<DesignerAttributeData> infos, CancellationToken cancellationToken)
            {
                _infosSource.SetResult(infos);
                return ValueTaskFactory.CompletedTask;
            }
        }

        [Fact]
        public async Task TestUnknownProject()
        {
            var workspace = CreateWorkspace([typeof(NoCompilationLanguageService)]);
            var solution = workspace.CurrentSolution.AddProject("unknown", "unknown", NoCompilationConstants.LanguageName).Solution;

            using var client = await InProcRemoteHostClient.GetTestClientAsync(workspace).ConfigureAwait(false);
            var remoteWorkspace = client.GetRemoteWorkspace();

            await UpdatePrimaryWorkspace(client, solution);
            await VerifyAssetStorageAsync(client, solution);

            // Only C# and VB projects are supported in Remote workspace.
            // See "RemoteSupportedLanguages.IsSupported"
            Assert.Empty(remoteWorkspace.CurrentSolution.Projects);

            // No serializable remote options affect options checksum, so the checksums should match.
            Assert.Equal(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await remoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));

            solution = solution.RemoveProject(solution.ProjectIds.Single());

            Assert.Equal(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await remoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1365014")]
        public async Task TestRemoteHostSynchronizeIncrementalUpdate(bool applyInBatch)
        {
            using var workspace = CreateWorkspace();

            using var client = await InProcRemoteHostClient.GetTestClientAsync(workspace).ConfigureAwait(false);
            var remoteWorkspace = client.GetRemoteWorkspace();

            var solution = Populate(workspace.CurrentSolution);

            // verify initial setup
            await workspace.ChangeSolutionAsync(solution);
            solution = workspace.CurrentSolution;
            await UpdatePrimaryWorkspace(client, solution);
            await VerifyAssetStorageAsync(client, solution);

            Assert.Equal(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await remoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));

            // incrementally update
            solution = await VerifyIncrementalUpdatesAsync(
                workspace, remoteWorkspace, client, solution, applyInBatch, csAddition: " ", vbAddition: " ");

            Assert.Equal(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await remoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));

            // incrementally update
            solution = await VerifyIncrementalUpdatesAsync(
                workspace, remoteWorkspace, client, solution, applyInBatch, csAddition: "\r\nclass Addition { }", vbAddition: "\r\nClass VB\r\nEnd Class");

            Assert.Equal(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await remoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52578")]
        public async Task TestIncrementalUpdateHandlesReferenceReversal()
        {
            using var workspace = CreateWorkspace();

            using var client = await InProcRemoteHostClient.GetTestClientAsync(workspace).ConfigureAwait(false);
            var remoteWorkspace = client.GetRemoteWorkspace();

            var solution = workspace.CurrentSolution;
            solution = AddProject(solution, LanguageNames.CSharp, documents: Array.Empty<string>(), additionalDocuments: Array.Empty<string>(), p2pReferences: Array.Empty<ProjectId>());
            var projectId1 = solution.ProjectIds.Single();
            solution = AddProject(solution, LanguageNames.CSharp, documents: Array.Empty<string>(), additionalDocuments: Array.Empty<string>(), p2pReferences: Array.Empty<ProjectId>());
            var projectId2 = solution.ProjectIds.Where(id => id != projectId1).Single();

            var project1ToProject2 = new ProjectReference(projectId2);
            var project2ToProject1 = new ProjectReference(projectId1);

            // Start with projectId1 -> projectId2
            solution = solution.AddProjectReference(projectId1, project1ToProject2);

            // verify initial setup
            await UpdatePrimaryWorkspace(client, solution);
            await VerifyAssetStorageAsync(client, solution);

            Assert.Equal(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await remoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));

            // reverse project references and incrementally update
            solution = solution.RemoveProjectReference(projectId1, project1ToProject2);
            solution = solution.AddProjectReference(projectId2, project2ToProject1);
            await workspace.ChangeSolutionAsync(solution);
            solution = workspace.CurrentSolution;
            await UpdatePrimaryWorkspace(client, solution);

            Assert.Equal(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await remoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));

            // reverse project references again and incrementally update
            solution = solution.RemoveProjectReference(projectId2, project2ToProject1);
            solution = solution.AddProjectReference(projectId1, project1ToProject2);
            await workspace.ChangeSolutionAsync(solution);
            solution = workspace.CurrentSolution;
            await UpdatePrimaryWorkspace(client, solution);

            Assert.Equal(
                await solution.State.GetChecksumAsync(CancellationToken.None),
                await remoteWorkspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None));
        }

        [Fact]
        public async Task TestRemoteWorkspaceCircularReferences()
        {
            using var tempRoot = new TempRoot();

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

            using var remoteWorkspace = new RemoteWorkspace(FeaturesTestCompositions.RemoteHost.GetHostServices());

            // this shouldn't throw exception
            var (solution, updated) = await remoteWorkspace.GetTestAccessor().TryUpdateWorkspaceCurrentSolutionAsync(
                remoteWorkspace.GetTestAccessor().CreateSolutionFromInfo(solutionInfo), workspaceVersion: 1);
            Assert.True(updated);
            Assert.NotNull(solution);
        }

        private static ImmutableArray<ImmutableArray<T>> Permute<T>(T[] values)
        {
            using var _ = ArrayBuilder<ImmutableArray<T>>.GetInstance(out var result);
            DoPermute(0, values.Length - 1);
            return result.ToImmutable();

            void DoPermute(int start, int end)
            {
                if (start == end)
                {
                    // We have one of our possible n! solutions,
                    // add it to the list.
                    result.Add(values.ToImmutableArray());
                }
                else
                {
                    for (var i = start; i <= end; i++)
                    {
                        (values[start], values[i]) = (values[i], values[start]);
                        DoPermute(start + 1, end);
                        (values[start], values[i]) = (values[i], values[start]);
                    }
                }
            }
        }

        private static async Task TestInProcAndRemoteWorkspace(
            bool syncWithRemoteServer, params ImmutableArray<(string hintName, SourceText text)>[] values)
        {
            // Try every permutation of these values.
            foreach (var permutation in Permute(values))
            {
                var gotException = false;
                try
                {
                    await TestInProcAndRemoteWorkspaceWorker(syncWithRemoteServer, permutation);
                }
                catch (XunitException)
                {
                    gotException = true;
                }

                // If we're syncing to the remove server, we should get no exceptions, since the data should be matched
                // between both.  If we're not syncing, we should see a failure since the two processes will disagree on
                // the contents.
                Assert.Equal(syncWithRemoteServer, !gotException);
            }
        }

        [ExportWorkspaceService(typeof(IWorkspaceConfigurationService), ServiceLayer.Test), SharedAttribute, PartNotDiscoverable]
        private sealed class NoSyncWorkspaceConfigurationService : IWorkspaceConfigurationService
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public NoSyncWorkspaceConfigurationService()
            {
            }

            public WorkspaceConfigurationOptions Options => WorkspaceConfigurationOptions.Default with { RunSourceGeneratorsInSameProcessOnly = true };
        }

        private static async Task TestInProcAndRemoteWorkspaceWorker(
            bool syncWithRemoteServer, ImmutableArray<ImmutableArray<(string hintName, SourceText text)>> values)
        {
            var throwIfCalled = false;
            ImmutableArray<(string hintName, SourceText text)> sourceTexts = default;
            var generator = new CallbackGenerator(
                onInit: _ => { },
                onExecute: _ => { },
                computeSourceTexts: () =>
                {
                    Contract.ThrowIfTrue(throwIfCalled);
                    return sourceTexts;
                });

            using var localWorkspace = CreateWorkspace(syncWithRemoteServer ? Array.Empty<Type>() : [typeof(NoSyncWorkspaceConfigurationService)]);

            DocumentId tempDocId;

            // Keep this all in a nested scope so we don't accidentally access this data inside the loop below.  We only
            // want to access the true workspace solution (which will be a fork of the solution we're producing here).
            {
                var projectId = ProjectId.CreateNewId();
                var analyzerReference = new TestGeneratorReference(generator);
                var project = localWorkspace.CurrentSolution
                    .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, name: "Test", assemblyName: "Test", language: LanguageNames.CSharp))
                    .GetRequiredProject(projectId)
                    .AddAnalyzerReference(analyzerReference);
                var tempDoc = project.AddDocument("X.cs", SourceText.From("// "));
                tempDocId = tempDoc.Id;

                Assert.True(localWorkspace.SetCurrentSolution(_ => tempDoc.Project.Solution, WorkspaceChangeKind.SolutionChanged));
            }

            using var client = await InProcRemoteHostClient.GetTestClientAsync(localWorkspace);
            var remoteWorkspace = client.GetRemoteWorkspace();

            for (var i = 0; i < values.Length; i++)
            {
                sourceTexts = values[i];

                // make a change to the project to force a change between the local and oop solutions.

                Assert.True(localWorkspace.SetCurrentSolution(s => s.WithDocumentText(tempDocId, SourceText.From("// " + i)), WorkspaceChangeKind.SolutionChanged));
                await UpdatePrimaryWorkspace(client, localWorkspace.CurrentSolution);

                var localProject = localWorkspace.CurrentSolution.Projects.Single();
                var remoteProject = remoteWorkspace.CurrentSolution.Projects.Single();

                // Run generators locally
                throwIfCalled = false;
                var localCompilation = await localProject.GetCompilationAsync();

                // Now run them remotely.  This must not actually call into the generator since nothing has changed.
                throwIfCalled = true;
                var remoteCompilation = await remoteProject.GetCompilationAsync();

                await AssertSourceGeneratedDocumentsAreSame(localProject, remoteProject, expectedCount: sourceTexts.Length);
            }

            static async Task AssertSourceGeneratedDocumentsAreSame(Project localProject, Project remoteProject, int expectedCount)
            {
                // The docs on both sides must be in the exact same order, and with identical contents (including
                // source-text encoding/hash-algorithm).

                var localGeneratedDocs = (await localProject.GetSourceGeneratedDocumentsAsync()).ToImmutableArray();
                var remoteGeneratedDocs = (await remoteProject.GetSourceGeneratedDocumentsAsync()).ToImmutableArray();

                Assert.Equal(localGeneratedDocs.Length, remoteGeneratedDocs.Length);
                Assert.Equal(expectedCount, localGeneratedDocs.Length);

                for (var i = 0; i < expectedCount; i++)
                {
                    var localDoc = localGeneratedDocs[i];
                    var remoteDoc = remoteGeneratedDocs[i];

                    Assert.Equal(localDoc.HintName, remoteDoc.HintName);
                    Assert.Equal(localDoc.DocumentState.Id, remoteDoc.DocumentState.Id);

                    var localText = await localDoc.GetTextAsync();
                    var remoteText = await localDoc.GetTextAsync();
                    Assert.Equal(localText.ToString(), remoteText.ToString());
                    Assert.Equal(localText.Encoding, remoteText.Encoding);
                    Assert.Equal(localText.ChecksumAlgorithm, remoteText.ChecksumAlgorithm);
                }
            }
        }

        private static SourceText CreateText(string content, Encoding encoding = null, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
            => SourceText.From(content, encoding ?? Encoding.UTF8, checksumAlgorithm);

        private static SourceText CreateStreamText(string content, bool useBOM, bool useMemoryStream)
        {
            var encoding = new UTF8Encoding(useBOM);
            var bytes = encoding.GetBytes(content);
            if (useMemoryStream)
            {
                using var stream = new MemoryStream(bytes);
                return SourceText.From(stream, encoding, SourceHashAlgorithm.Sha1, throwIfBinaryDetected: true);
            }
            else
            {
                return SourceText.From(bytes, bytes.Length, encoding, SourceHashAlgorithm.Sha1, throwIfBinaryDetected: true);
            }
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree1(bool syncWithRemoteServer)
        {
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateText(Guid.NewGuid().ToString()))));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree2(bool syncWithRemoteServer)
        {
            var sourceText = CreateText(Guid.NewGuid().ToString());
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", sourceText)),
                ImmutableArray.Create(("SG.cs", sourceText)));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree3(bool syncWithRemoteServer)
        {
            var sourceText = Guid.NewGuid().ToString();
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateText(sourceText))),
                ImmutableArray.Create(("SG.cs", CreateText(sourceText))));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree4(bool syncWithRemoteServer)
        {
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateText(Guid.NewGuid().ToString()))),
                ImmutableArray.Create(("SG.cs", CreateText(Guid.NewGuid().ToString()))));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree5(bool syncWithRemoteServer)
        {
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateText(Guid.NewGuid().ToString()))),
                ImmutableArray.Create(("NewName.cs", CreateText(Guid.NewGuid().ToString()))));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree6(bool syncWithRemoteServer)
        {
            var sourceText = CreateText(Guid.NewGuid().ToString());
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", sourceText)),
                ImmutableArray.Create(("NewName.cs", sourceText)));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree7(bool syncWithRemoteServer)
        {
            var sourceText = Guid.NewGuid().ToString();
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateText(sourceText))),
                ImmutableArray.Create(("NewName.cs", CreateText(sourceText))));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree8(bool syncWithRemoteServer)
        {
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateText(Guid.NewGuid().ToString()))),
                ImmutableArray.Create(("NewName.cs", CreateText(Guid.NewGuid().ToString()))));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree9(bool syncWithRemoteServer)
        {
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateText("X", Encoding.ASCII))),
                ImmutableArray.Create(("SG.cs", CreateText("X", Encoding.UTF8))));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree10(bool syncWithRemoteServer)
        {
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateText("X", Encoding.UTF8, checksumAlgorithm: SourceHashAlgorithm.Sha1))),
                ImmutableArray.Create(("SG.cs", CreateText("X", Encoding.UTF8, checksumAlgorithm: SourceHashAlgorithm.Sha256))));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree11(bool syncWithRemoteServer)
        {
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateText(Guid.NewGuid().ToString()))),
                ImmutableArray<(string, SourceText)>.Empty);
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree12(bool syncWithRemoteServer)
        {
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray<(string, SourceText)>.Empty,
                ImmutableArray.Create(("SG.cs", CreateText(Guid.NewGuid().ToString()))));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree13(bool syncWithRemoteServer)
        {
            var contents = Guid.NewGuid().ToString();
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateText(contents))),
                ImmutableArray.Create(("SG.cs", CreateText(contents)), ("SG1.cs", CreateText(contents))));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree14(bool syncWithRemoteServer)
        {
            var contents = Guid.NewGuid().ToString();
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateText(contents))),
                ImmutableArray.Create(("SG.cs", CreateText(contents)), ("SG1.cs", CreateText("Other"))));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree15(bool syncWithRemoteServer)
        {
            var contents = Guid.NewGuid().ToString();
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateText(contents))),
                ImmutableArray.Create(("SG1.cs", CreateText(contents)), ("SG.cs", CreateText("Other"))));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree16(bool syncWithRemoteServer)
        {
            var contents = Guid.NewGuid().ToString();
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateText(contents))),
                ImmutableArray.Create(("SG1.cs", CreateText("Other")), ("SG.cs", CreateText(contents))));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree17(bool syncWithRemoteServer)
        {
            var contents = Guid.NewGuid().ToString();
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateText(contents))),
                ImmutableArray.Create(("SG1.cs", CreateText("Other")), ("SG.cs", CreateText(contents))),
                ImmutableArray<(string, SourceText)>.Empty);
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree18(bool syncWithRemoteServer)
        {
            var contents = CreateText(Guid.NewGuid().ToString());
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG1.cs", contents), ("SG2.cs", contents)));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree19(bool syncWithRemoteServer)
        {
            var contents = CreateText(Guid.NewGuid().ToString());
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG1.cs", contents), ("SG2.cs", contents)),
                ImmutableArray.Create(("SG2.cs", contents), ("SG1.cs", contents)));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree20(bool syncWithRemoteServer)
        {
            var contents = Guid.NewGuid().ToString();
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG1.cs", CreateText(contents)), ("SG2.cs", CreateText(contents))),
                ImmutableArray.Create(("SG2.cs", CreateText(contents)), ("SG1.cs", CreateText(contents))));
        }

        [Theory, CombinatorialData]
        public async Task InProcAndRemoteWorkspaceAgree21(
            bool syncWithRemoteServer, bool useBOM1, bool useMemoryStream1, bool useBOM2, bool useMemoryStream2)
        {
            var contents = Guid.NewGuid().ToString();
            await TestInProcAndRemoteWorkspace(
                syncWithRemoteServer,
                ImmutableArray.Create(("SG.cs", CreateStreamText(contents, useBOM: useBOM1, useMemoryStream: useMemoryStream1))),
                ImmutableArray.Create(("SG.cs", CreateStreamText(contents, useBOM: useBOM2, useMemoryStream: useMemoryStream2))));
        }

        private static async Task<Solution> VerifyIncrementalUpdatesAsync(
            TestWorkspace localWorkspace,
            Workspace remoteWorkspace,
            RemoteHostClient client,
            Solution solution,
            bool applyInBatch,
            string csAddition,
            string vbAddition)
        {
            var remoteSolution = remoteWorkspace.CurrentSolution;
            var projectIds = solution.ProjectIds;

            for (var i = 0; i < projectIds.Count; i++)
            {
                var projectName = $"Project{i}";
                var project = solution.GetProject(projectIds[i]);
                var changedDocuments = new List<string>();

                var documentIds = project.DocumentIds;
                for (var j = 0; j < documentIds.Count; j++)
                {
                    var documentName = $"Document{j}";

                    var currentSolution = UpdateSolution(solution, projectName, documentName, csAddition, vbAddition);
                    changedDocuments.Add(documentName);

                    solution = currentSolution;

                    if (!applyInBatch)
                    {
                        await UpdateAndVerifyAsync();
                    }
                }

                if (applyInBatch)
                {
                    await UpdateAndVerifyAsync();
                }

                async Task UpdateAndVerifyAsync()
                {
                    var documentNames = changedDocuments.ToImmutableArray();
                    changedDocuments.Clear();

                    await localWorkspace.ChangeSolutionAsync(solution);
                    solution = localWorkspace.CurrentSolution;
                    await UpdatePrimaryWorkspace(client, solution);

                    var currentRemoteSolution = remoteWorkspace.CurrentSolution;
                    VerifyStates(remoteSolution, currentRemoteSolution, projectName, documentNames);

                    remoteSolution = currentRemoteSolution;

                    Assert.Equal(
                        await solution.State.GetChecksumAsync(CancellationToken.None),
                        await remoteSolution.State.GetChecksumAsync(CancellationToken.None));
                }
            }

            return solution;
        }

        private static void VerifyStates(Solution solution1, Solution solution2, string projectName, ImmutableArray<string> documentNames)
        {
            Assert.Equal(WorkspaceKind.RemoteWorkspace, solution1.WorkspaceKind);
            Assert.Equal(WorkspaceKind.RemoteWorkspace, solution2.WorkspaceKind);

            SetEqual(solution1.ProjectIds, solution2.ProjectIds);

            var (project, documents) = GetProjectAndDocuments(solution1, projectName, documentNames);

            var projectId = project.Id;
            var documentIds = documents.SelectAsArray(document => document.Id);

            var projectIds = solution1.ProjectIds;
            for (var i = 0; i < projectIds.Count; i++)
            {
                var currentProjectId = projectIds[i];

                var projectStateShouldSame = projectId != currentProjectId;
                Assert.Equal(projectStateShouldSame, object.ReferenceEquals(solution1.GetProject(currentProjectId).State, solution2.GetProject(currentProjectId).State));

                if (!projectStateShouldSame)
                {
                    SetEqual(solution1.GetProject(currentProjectId).DocumentIds, solution2.GetProject(currentProjectId).DocumentIds);

                    var documentIdsInProject = solution1.GetProject(currentProjectId).DocumentIds;
                    for (var j = 0; j < documentIdsInProject.Count; j++)
                    {
                        var currentDocumentId = documentIdsInProject[j];

                        var documentStateShouldSame = !documentIds.Contains(currentDocumentId);
                        Assert.Equal(documentStateShouldSame, object.ReferenceEquals(solution1.GetDocument(currentDocumentId).State, solution2.GetDocument(currentDocumentId).State));
                    }
                }
            }
        }

        private static async Task VerifyAssetStorageAsync(InProcRemoteHostClient client, Solution solution)
        {
            var map = await solution.GetAssetMapAsync(CancellationToken.None);

            var storage = client.TestData.WorkspaceManager.SolutionAssetCache;

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

        private static (Project project, Document document) GetProjectAndDocument(Solution solution, string projectName, string documentName)
        {
            var project = solution.Projects.First(p => string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));
            var document = project.Documents.First(d => string.Equals(d.Name, documentName, StringComparison.OrdinalIgnoreCase));

            return (project, document);
        }

        private static (Project project, ImmutableArray<Document> documents) GetProjectAndDocuments(Solution solution, string projectName, ImmutableArray<string> documentNames)
        {
            var project = solution.Projects.First(p => string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));
            var documents = documentNames.SelectAsArray(
                documentName => project.Documents.First(d => string.Equals(d.Name, documentName, StringComparison.OrdinalIgnoreCase)));

            return (project, documents);
        }

        private static async Task UpdatePrimaryWorkspace(RemoteHostClient client, Solution solution)
        {
            var workspaceVersion = solution.WorkspaceVersion;
            await client.TryInvokeAsync<IRemoteAssetSynchronizationService>(
                solution,
                async (service, solutionInfo, cancellationToken) => await service.SynchronizePrimaryWorkspaceAsync(solutionInfo, workspaceVersion, cancellationToken),
                CancellationToken.None);
        }

        private static Solution Populate(Solution solution)
        {
            solution = AddProject(solution, LanguageNames.CSharp,
            [
                "class CS { }",
                "class CS2 { }"
            ],
            [
                "cs additional file content"
            ], Array.Empty<ProjectId>());

            solution = AddProject(solution, LanguageNames.VisualBasic,
            [
                "Class VB\r\nEnd Class",
                "Class VB2\r\nEnd Class"
            ],
            [
                "vb additional file content"
            ], [solution.ProjectIds.First()]);

            solution = AddProject(solution, LanguageNames.CSharp,
            [
                "class Top { }"
            ],
            [
                "cs additional file content"
            ], solution.ProjectIds.ToArray());

            solution = AddProject(solution, LanguageNames.CSharp,
            [
                "class OrphanCS { }",
                "class OrphanCS2 { }"
            ],
            [
                "cs additional file content",
                "cs additional file content2"
            ], Array.Empty<ProjectId>());

            solution = AddProject(solution, LanguageNames.CSharp,
            [
                "class CS { }",
                "class CS2 { }",
                "class CS3 { }",
                "class CS4 { }",
                "class CS5 { }",
            ],
            [
                "cs additional file content"
            ], Array.Empty<ProjectId>());

            solution = AddProject(solution, LanguageNames.VisualBasic,
            [
                "Class VB\r\nEnd Class",
                "Class VB2\r\nEnd Class",
                "Class VB3\r\nEnd Class",
                "Class VB4\r\nEnd Class",
                "Class VB5\r\nEnd Class",
            ],
            [
                "vb additional file content"
            ], Array.Empty<ProjectId>());

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
