﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.DebugUtil;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Roslyn.VisualStudio.Next.UnitTests.Mocks;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    [UseExportProvider]
    public class SolutionServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestCreation()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                var solution = workspace.CurrentSolution;
                var service = await GetSolutionServiceAsync(solution);

                var solutionChecksum = await solution.State.GetChecksumAsync(CancellationToken.None);
                var synched = await service.GetSolutionAsync(solutionChecksum, CancellationToken.None);

                Assert.Equal(solutionChecksum, await synched.State.GetChecksumAsync(CancellationToken.None));
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestGetSolutionWithPrimaryFlag()
        {
            var code1 = @"class Test1 { void Method() { } }";

            using (var workspace = TestWorkspace.CreateCSharp(code1))
            {
                var solution = workspace.CurrentSolution;
                var solutionChecksum = await solution.State.GetChecksumAsync(CancellationToken.None);

                var service = await GetSolutionServiceAsync(solution);
                var synched = await service.GetSolutionAsync(solutionChecksum, CancellationToken.None);
                Assert.Equal(solutionChecksum, await synched.State.GetChecksumAsync(CancellationToken.None));
                Assert.True(synched.Workspace is TemporaryWorkspace);
            }

            var code2 = @"class Test2 { void Method() { } }";

            using (var workspace = TestWorkspace.CreateCSharp(code2))
            {
                var solution = workspace.CurrentSolution;
                var solutionChecksum = await solution.State.GetChecksumAsync(CancellationToken.None);

                var service = (ISolutionController)await GetSolutionServiceAsync(solution);
                var synched = await service.GetSolutionAsync(solutionChecksum, fromPrimaryBranch: true, cancellationToken: CancellationToken.None);
                Assert.Equal(solutionChecksum, await synched.State.GetChecksumAsync(CancellationToken.None));
                Assert.True(synched.Workspace is RemoteWorkspace);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestStrongNameProvider()
        {
            var workspace = new AdhocWorkspace();

            var filePath = typeof(SolutionServiceTests).Assembly.Location;

            workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(), VersionStamp.Create(), "test", "test.dll", LanguageNames.CSharp,
                    filePath: filePath, outputFilePath: filePath));

            var service = await GetSolutionServiceAsync(workspace.CurrentSolution);

            var solutionChecksum = await workspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None);
            var solution = await service.GetSolutionAsync(solutionChecksum, CancellationToken.None);

            var compilationOptions = solution.Projects.First().CompilationOptions;

            Assert.True(compilationOptions.StrongNameProvider is DesktopStrongNameProvider);
            Assert.True(compilationOptions.XmlReferenceResolver is XmlFileResolver);

            var dirName = PathUtilities.GetDirectoryName(filePath);
            var array = new[] { dirName, dirName };
            Assert.Equal(Hash.CombineValues(array, StringComparer.Ordinal), compilationOptions.StrongNameProvider.GetHashCode());
            Assert.Equal(((XmlFileResolver)compilationOptions.XmlReferenceResolver).BaseDirectory, dirName);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestStrongNameProviderEmpty()
        {
            var workspace = new AdhocWorkspace();

            var filePath = "testLocation";

            workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(), VersionStamp.Create(), "test", "test.dll", LanguageNames.CSharp,
                    filePath: filePath, outputFilePath: filePath));

            var service = await GetSolutionServiceAsync(workspace.CurrentSolution);

            var solutionChecksum = await workspace.CurrentSolution.State.GetChecksumAsync(CancellationToken.None);
            var solution = await service.GetSolutionAsync(solutionChecksum, CancellationToken.None);

            var compilationOptions = solution.Projects.First().CompilationOptions;

            Assert.True(compilationOptions.StrongNameProvider is DesktopStrongNameProvider);
            Assert.True(compilationOptions.XmlReferenceResolver is XmlFileResolver);

            var array = new string[] { };
            Assert.Equal(Hash.CombineValues(array, StringComparer.Ordinal), compilationOptions.StrongNameProvider.GetHashCode());
            Assert.Equal(((XmlFileResolver)compilationOptions.XmlReferenceResolver).BaseDirectory, null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestCache()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                var solution = workspace.CurrentSolution;
                var service = await GetSolutionServiceAsync(solution);

                var solutionChecksum = await solution.State.GetChecksumAsync(CancellationToken.None);

                var first = await service.GetSolutionAsync(solutionChecksum, CancellationToken.None);
                var second = await service.GetSolutionAsync(solutionChecksum, CancellationToken.None);

                // same instance from cache
                Assert.True(object.ReferenceEquals(first, second));
                Assert.True(first.Workspace is TemporaryWorkspace);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestUpdatePrimaryWorkspace()
        {
            var code = @"class Test { void Method() { } }";

            await VerifySolutionUpdate(code, s => s.WithDocumentText(s.Projects.First().DocumentIds.First(), SourceText.From(code + " ")));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestUpdateProjectInfo()
        {
            var code = @"class Test { void Method() { } }";

            await VerifySolutionUpdate(code, s => s.Projects.First().WithAssemblyName("test2").Solution);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestUpdateOutputFilePath()
        {
            var code = @"class Test { void Method() { } }";

            await VerifySolutionUpdate(code, s => s.WithProjectOutputFilePath(s.ProjectIds[0], "test.dll"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestUpdateOutputRefFilePath()
        {
            var code = @"class Test { void Method() { } }";

            await VerifySolutionUpdate(code, s => s.WithProjectOutputRefFilePath(s.ProjectIds[0], "test.dll"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestUpdateDocumentInfo()
        {
            var code = @"class Test { void Method() { } }";

            await VerifySolutionUpdate(code, s => s.WithDocumentFolders(s.Projects.First().Documents.First().Id, new[] { "test" }));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestHasAllInformation()
        {
            var code = @"class Test { void Method() { } }";

            await VerifySolutionUpdate(code, s => s.WithHasAllInformation(s.ProjectIds.First(), false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestAddUpdateRemoveProjects()
        {
            var code = @"class Test { void Method() { } }";

            await VerifySolutionUpdate(code, s =>
            {
                var existingProjectId = s.ProjectIds.First();

                s = s.AddProject("newProject", "newProject", LanguageNames.CSharp).Solution;

                var project = s.GetProject(existingProjectId);
                project = project.WithCompilationOptions(project.CompilationOptions.WithModuleName("modified"));

                var existingDocumentId = project.DocumentIds.First();

                project = project.AddDocument("newDocument", SourceText.From("// new text")).Project;

                var document = project.GetDocument(existingDocumentId);

                document = document.WithSourceCodeKind(SourceCodeKind.Script);

                return document.Project.Solution;
            });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestAdditionalDocument()
        {
            var code = @"class Test { void Method() { } }";
            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                var projectId = workspace.CurrentSolution.ProjectIds.First();
                var additionalDocumentId = DocumentId.CreateNewId(projectId);
                var additionalDocumentInfo = DocumentInfo.Create(
                    additionalDocumentId, "additionalFile",
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From("test"), VersionStamp.Create())));

                await VerifySolutionUpdate(workspace, s =>
                {
                    return s.AddAdditionalDocument(additionalDocumentInfo);
                });

                workspace.OnAdditionalDocumentAdded(additionalDocumentInfo);

                await VerifySolutionUpdate(workspace, s =>
                {
                    return s.WithAdditionalDocumentText(additionalDocumentId, SourceText.From("changed"));
                });

                await VerifySolutionUpdate(workspace, s =>
                {
                    return s.RemoveAdditionalDocument(additionalDocumentId);
                });
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestDocument()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                var projectId = workspace.CurrentSolution.ProjectIds.First();
                var documentId = DocumentId.CreateNewId(projectId);
                var documentInfo = DocumentInfo.Create(
                    documentId, "sourceFile",
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From("class A { }"), VersionStamp.Create())));

                await VerifySolutionUpdate(workspace, s =>
                {
                    return s.AddDocument(documentInfo);
                });

                workspace.OnDocumentAdded(documentInfo);

                await VerifySolutionUpdate(workspace, s =>
                {
                    return s.WithDocumentText(documentId, SourceText.From("class Changed { }"));
                });

                await VerifySolutionUpdate(workspace, s =>
                {
                    return s.RemoveDocument(documentId);
                });
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestRemoteWorkspaceSolutionCrawler()
        {
            var code = @"class Test { void Method() { } }";

            // create base solution
            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                // create solution service
                var solution = workspace.CurrentSolution;
                var service = await GetSolutionServiceAsync(solution);

                // update primary workspace
                var solutionChecksum = await solution.State.GetChecksumAsync(CancellationToken.None);
                await ((ISolutionController)service).UpdatePrimaryWorkspaceAsync(solutionChecksum, CancellationToken.None);

                // get solution in remote host
                var oopSolution = await service.GetSolutionAsync(solutionChecksum, CancellationToken.None);
                Assert.True(oopSolution.Workspace is RemoteWorkspace);

                // get solution cralwer in remote host
                var solutionCrawlerService = oopSolution.Workspace.Services.GetService<ISolutionCrawlerRegistrationService>() as SolutionCrawlerRegistrationService;
                Assert.NotNull(solutionCrawlerService);

                // check remote workspace has enabled solution crawler in remote host
                var testAnalyzerProvider = new TestAnalyzerProvider();
                solutionCrawlerService.AddAnalyzerProvider(
                    testAnalyzerProvider,
                    new IncrementalAnalyzerProviderMetadata("Test", highPriorityForActiveFile: false, workspaceKinds: WorkspaceKind.RemoteWorkspace));

                // check our solution crawler has ran
                Assert.True(await testAnalyzerProvider.Analyzer.Called);

                testAnalyzerProvider.Analyzer.Reset();

                // update remote workspace
                var remoteWorkspace = (RemoteWorkspace)oopSolution.Workspace;
                remoteWorkspace.UpdateSolution(oopSolution.WithDocumentText(oopSolution.Projects.First().Documents.First().Id, SourceText.From(code + " class Test2 { }")));

                // check solution update correctly ran solution crawler
                Assert.True(await testAnalyzerProvider.Analyzer.Called);
            }
        }

        private static async Task VerifySolutionUpdate(string code, Func<Solution, Solution> newSolutionGetter)
        {
            using (var workspace = TestWorkspace.CreateCSharp(code))
            {
                await VerifySolutionUpdate(workspace, newSolutionGetter);
            }
        }

        private static async Task VerifySolutionUpdate(TestWorkspace workspace, Func<Solution, Solution> newSolutionGetter)
        {
            var map = new Dictionary<Checksum, object>();

            var solution = workspace.CurrentSolution;
            var service = await GetSolutionServiceAsync(solution, map);

            var solutionChecksum = await solution.State.GetChecksumAsync(CancellationToken.None);

            // update primary workspace
            await ((ISolutionController)service).UpdatePrimaryWorkspaceAsync(solutionChecksum, CancellationToken.None);
            var first = await service.GetSolutionAsync(solutionChecksum, CancellationToken.None);

            Assert.IsAssignableFrom<RemoteWorkspace>(first.Workspace);
            var primaryWorkspace = first.Workspace;
            Assert.Equal(solutionChecksum, await first.State.GetChecksumAsync(CancellationToken.None));
            Assert.True(object.ReferenceEquals(primaryWorkspace.PrimaryBranchId, first.BranchId));

            // get new solution
            var newSolution = newSolutionGetter(solution);
            var newSolutionChecksum = await newSolution.State.GetChecksumAsync(CancellationToken.None);
            newSolution.AppendAssetMap(map);

            // get solution without updating primary workspace
            var second = await service.GetSolutionAsync(newSolutionChecksum, CancellationToken.None);

            Assert.Equal(newSolutionChecksum, await second.State.GetChecksumAsync(CancellationToken.None));
            Assert.False(object.ReferenceEquals(primaryWorkspace.PrimaryBranchId, second.BranchId));

            // do same once updating primary workspace
            await ((ISolutionController)service).UpdatePrimaryWorkspaceAsync(newSolutionChecksum, CancellationToken.None);
            var third = await service.GetSolutionAsync(newSolutionChecksum, CancellationToken.None);

            Assert.Equal(newSolutionChecksum, await third.State.GetChecksumAsync(CancellationToken.None));
            Assert.True(object.ReferenceEquals(primaryWorkspace.PrimaryBranchId, third.BranchId));
        }

        private static async Task<SolutionService> GetSolutionServiceAsync(Solution solution, Dictionary<Checksum, object> map = null)
        {
            // make sure checksum is calculated
            await solution.State.GetChecksumAsync(CancellationToken.None);

            map = map ?? new Dictionary<Checksum, object>();
            solution.AppendAssetMap(map);

            var sessionId = 0;
            var storage = new AssetStorage();
            var source = new TestAssetSource(storage, map);
            var remoteWorkspace = new RemoteWorkspace();
            var service = new SolutionService(new AssetService(sessionId, storage, remoteWorkspace));

            return service;
        }

        private class TestAnalyzerProvider : IIncrementalAnalyzerProvider
        {
            public readonly TestAnalyzer Analyzer = new TestAnalyzer();

            public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
            {
                return Analyzer;
            }

            public class TestAnalyzer : IncrementalAnalyzerBase
            {
                private TaskCompletionSource<bool> _source = new TaskCompletionSource<bool>();

                public override Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
                {
                    _source.SetResult(true);
                    return SpecializedTasks.EmptyTask;
                }

                public Task<bool> Called => _source.Task;

                public void Reset()
                {
                    _source = new TaskCompletionSource<bool>();
                }
            }
        }
    }
}
