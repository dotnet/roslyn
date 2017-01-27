// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Roslyn.VisualStudio.Next.UnitTests.Mocks;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    public class SolutionServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestCreation()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = await TestWorkspace.CreateCSharpAsync(code))
            {
                var solution = workspace.CurrentSolution;
                var service = await GetSolutionServiceAsync(solution);

                var solutionChecksum = await solution.State.GetChecksumAsync(CancellationToken.None);
                var synched = await service.GetSolutionAsync(solutionChecksum, CancellationToken.None);

                Assert.Equal(solutionChecksum, await synched.State.GetChecksumAsync(CancellationToken.None));
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
        public async Task TestCreationWithOption()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = await TestWorkspace.CreateCSharpAsync(code))
            {
                var options = new TestOptionSet().WithChangedOption(RemoteHostOptions.RemoteHostTest, true);

                var solution = workspace.CurrentSolution;
                var service = await GetSolutionServiceAsync(solution);

                var solutionChecksum = await solution.State.GetChecksumAsync(CancellationToken.None);
                var synched = await service.GetSolutionAsync(solutionChecksum, options, CancellationToken.None);

                Assert.Equal(solutionChecksum, await synched.State.GetChecksumAsync(CancellationToken.None));
                Assert.Empty(options.GetChangedOptions(synched.Workspace.Options));

                Assert.True(options.GetOption(RemoteHostOptions.RemoteHostTest));
                Assert.True(synched.Workspace.Options.GetOption(RemoteHostOptions.RemoteHostTest));
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestOptionIsolation()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = await TestWorkspace.CreateCSharpAsync(code))
            {
                var solution = workspace.CurrentSolution;
                var service = await GetSolutionServiceAsync(solution);

                var solutionChecksum = await solution.State.GetChecksumAsync(CancellationToken.None);

                var options = new TestOptionSet().WithChangedOption(RemoteHostOptions.RemoteHostTest, true);
                var first = await service.GetSolutionAsync(solutionChecksum, options, CancellationToken.None);
                var second = await service.GetSolutionAsync(solutionChecksum, options.WithChangedOption(RemoteHostOptions.RemoteHostTest, false), CancellationToken.None);

                Assert.Equal(await first.State.GetChecksumAsync(CancellationToken.None), await second.State.GetChecksumAsync(CancellationToken.None));

                // option change shouldn't affect other workspace
                Assert.True(first.Workspace.Options.GetOption(RemoteHostOptions.RemoteHostTest));
                Assert.False(second.Workspace.Options.GetOption(RemoteHostOptions.RemoteHostTest));
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestCache()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = await TestWorkspace.CreateCSharpAsync(code))
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
        public async Task TestCacheWithOption()
        {
            var code = @"class Test { void Method() { } }";

            using (var workspace = await TestWorkspace.CreateCSharpAsync(code))
            {
                var options = new TestOptionSet().WithChangedOption(RemoteHostOptions.RemoteHostTest, true);

                var solution = workspace.CurrentSolution;
                var service = await GetSolutionServiceAsync(solution);

                var solutionChecksum = await solution.State.GetChecksumAsync(CancellationToken.None);

                var first = await service.GetSolutionAsync(solutionChecksum, options, CancellationToken.None);
                var second = await service.GetSolutionAsync(solutionChecksum, options, CancellationToken.None);

                // new solutions if option is involved for isolation
                Assert.False(object.ReferenceEquals(first, second));

                // but semantic of both solution should be same
                Assert.Equal(await first.State.GetChecksumAsync(CancellationToken.None), await second.State.GetChecksumAsync(CancellationToken.None));

                // also any sub nodes such as projects should be same
                Assert.True(object.ReferenceEquals(first.Projects.First().State, second.Projects.First().State));

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
            using (var workspace = await TestWorkspace.CreateCSharpAsync(code))
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

            using (var workspace = await TestWorkspace.CreateCSharpAsync(code))
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

        private static async Task VerifySolutionUpdate(string code, Func<Solution, Solution> newSolutionGetter)
        {
            using (var workspace = await TestWorkspace.CreateCSharpAsync(code))
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
            await service.UpdatePrimaryWorkspaceAsync(solutionChecksum, CancellationToken.None);
            var first = await service.GetSolutionAsync(solutionChecksum, CancellationToken.None);

            Assert.Equal(solutionChecksum, await first.State.GetChecksumAsync(CancellationToken.None));
            Assert.True(object.ReferenceEquals(PrimaryWorkspace.Workspace.PrimaryBranchId, first.BranchId));

            // get new solution
            var newSolution = newSolutionGetter(solution);
            var newSolutionChecksum = await newSolution.State.GetChecksumAsync(CancellationToken.None);
            newSolution.AppendAssetMap(map);

            // get solution without updating primary workspace
            var second = await service.GetSolutionAsync(newSolutionChecksum, CancellationToken.None);

            Assert.Equal(newSolutionChecksum, await second.State.GetChecksumAsync(CancellationToken.None));
            Assert.False(object.ReferenceEquals(PrimaryWorkspace.Workspace.PrimaryBranchId, second.BranchId));

            // do same once updating primary workspace
            await service.UpdatePrimaryWorkspaceAsync(newSolutionChecksum, CancellationToken.None);
            var third = await service.GetSolutionAsync(newSolutionChecksum, CancellationToken.None);

            Assert.Equal(newSolutionChecksum, await third.State.GetChecksumAsync(CancellationToken.None));
            Assert.True(object.ReferenceEquals(PrimaryWorkspace.Workspace.PrimaryBranchId, third.BranchId));
        }

        private static async Task<SolutionService> GetSolutionServiceAsync(Solution solution, Dictionary<Checksum, object> map = null)
        {
            // make sure checksum is calculated
            await solution.State.GetChecksumAsync(CancellationToken.None);

            map = map ?? new Dictionary<Checksum, object>();
            solution.AppendAssetMap(map);

            var sessionId = 0;
            var storage = new AssetStorage();
            var source = new TestAssetSource(storage, sessionId, map);
            var service = new SolutionService(new AssetService(sessionId, storage));

            return service;
        }
    }
}
