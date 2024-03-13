// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.RemoteHost)]
    public class SolutionServiceTests
    {
        private static RemoteWorkspace CreateRemoteWorkspace()
            => new RemoteWorkspace(FeaturesTestCompositions.RemoteHost.GetHostServices());

        [Fact]
        public async Task TestCreation()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            using var remoteWorkspace = CreateRemoteWorkspace();

            var solution = workspace.CurrentSolution;
            var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution);

            var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
            var synched = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);

            Assert.Equal(solutionChecksum, await synched.CompilationState.GetChecksumAsync(CancellationToken.None));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestGetSolutionWithPrimaryFlag(bool updatePrimaryBranch)
        {
            var code1 = @"class Test1 { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code1);
            using var remoteWorkspace = CreateRemoteWorkspace();

            var solution = workspace.CurrentSolution;
            var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
            var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution);

            var synched = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch, solution.WorkspaceVersion, cancellationToken: CancellationToken.None);
            Assert.Equal(solutionChecksum, await synched.CompilationState.GetChecksumAsync(CancellationToken.None));

            Assert.Equal(WorkspaceKind.RemoteWorkspace, synched.WorkspaceKind);
        }

        [Fact]
        public async Task TestStrongNameProvider()
        {
            using var workspace = new AdhocWorkspace();
            using var remoteWorkspace = CreateRemoteWorkspace();

            var filePath = typeof(SolutionServiceTests).Assembly.Location;

            workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(), VersionStamp.Create(), "test", "test.dll", LanguageNames.CSharp,
                    filePath: filePath, outputFilePath: filePath));

            var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, workspace.CurrentSolution);

            var solutionChecksum = await workspace.CurrentSolution.CompilationState.GetChecksumAsync(CancellationToken.None);
            var solution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);

            var compilationOptions = solution.Projects.First().CompilationOptions;

            Assert.IsType<DesktopStrongNameProvider>(compilationOptions.StrongNameProvider);
            Assert.IsType<XmlFileResolver>(compilationOptions.XmlReferenceResolver);

            var dirName = PathUtilities.GetDirectoryName(filePath);
            var array = new[] { dirName, dirName };
            Assert.Equal(Hash.CombineValues(array, StringComparer.Ordinal), compilationOptions.StrongNameProvider.GetHashCode());
            Assert.Equal(((XmlFileResolver)compilationOptions.XmlReferenceResolver).BaseDirectory, dirName);
        }

        [Fact]
        public async Task TestStrongNameProviderEmpty()
        {
            using var workspace = new AdhocWorkspace();
            using var remoteWorkspace = CreateRemoteWorkspace();

            var filePath = "testLocation";

            workspace.AddProject(
                ProjectInfo.Create(
                    ProjectId.CreateNewId(), VersionStamp.Create(), "test", "test.dll", LanguageNames.CSharp,
                    filePath: filePath, outputFilePath: filePath));

            var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, workspace.CurrentSolution);

            var solutionChecksum = await workspace.CurrentSolution.CompilationState.GetChecksumAsync(CancellationToken.None);
            var solution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);

            var compilationOptions = solution.Projects.First().CompilationOptions;

            Assert.True(compilationOptions.StrongNameProvider is DesktopStrongNameProvider);
            Assert.True(compilationOptions.XmlReferenceResolver is XmlFileResolver);

            var array = new string[] { };
            Assert.Equal(Hash.CombineValues(array, StringComparer.Ordinal), compilationOptions.StrongNameProvider.GetHashCode());
            Assert.Null(((XmlFileResolver)compilationOptions.XmlReferenceResolver).BaseDirectory);
        }

        [Fact]
        public async Task TestCache()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            using var remoteWorkspace = CreateRemoteWorkspace();

            var solution = workspace.CurrentSolution;
            var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution);
            var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);

            var first = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            var second = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);

            // same instance from cache
            Assert.True(object.ReferenceEquals(first, second));
            Assert.Equal(WorkspaceKind.RemoteWorkspace, first.WorkspaceKind);
        }

        [Fact]
        public async Task TestUpdatePrimaryWorkspace()
        {
            var code = @"class Test { void Method() { } }";

            await VerifySolutionUpdate(code, s => s.WithDocumentText(s.Projects.First().DocumentIds.First(), SourceText.From(code + " ")));
        }

        [Fact]
        public async Task ProjectProperties()
        {
            using var workspace = TestWorkspace.CreateCSharp("");

            static Solution SetProjectProperties(Solution solution, int version)
            {
                var projectId = solution.ProjectIds.Single();
                return solution
                    .WithProjectName(projectId, "Name" + version)
                    .WithProjectAssemblyName(projectId, "AssemblyName" + version)
                    .WithProjectFilePath(projectId, "FilePath" + version)
                    .WithProjectOutputFilePath(projectId, "OutputFilePath" + version)
                    .WithProjectOutputRefFilePath(projectId, "OutputRefFilePath" + version)
                    .WithProjectCompilationOutputInfo(projectId, new CompilationOutputInfo("AssemblyPath" + version))
                    .WithProjectDefaultNamespace(projectId, "DefaultNamespace" + version)
                    .WithProjectChecksumAlgorithm(projectId, SourceHashAlgorithm.Sha1 + version)
                    .WithHasAllInformation(projectId, (version % 2) != 0)
                    .WithRunAnalyzers(projectId, (version % 2) != 0);
            }

            static void ValidateProperties(Solution solution, int version)
            {
                var project = solution.Projects.Single();
                Assert.Equal("Name" + version, project.Name);
                Assert.Equal("AssemblyName" + version, project.AssemblyName);
                Assert.Equal("FilePath" + version, project.FilePath);
                Assert.Equal("OutputFilePath" + version, project.OutputFilePath);
                Assert.Equal("OutputRefFilePath" + version, project.OutputRefFilePath);
                Assert.Equal("AssemblyPath" + version, project.CompilationOutputInfo.AssemblyPath);
                Assert.Equal("DefaultNamespace" + version, project.DefaultNamespace);
                Assert.Equal(SourceHashAlgorithm.Sha1 + version, project.State.ChecksumAlgorithm);
                Assert.Equal((version % 2) != 0, project.State.HasAllInformation);
                Assert.Equal((version % 2) != 0, project.State.RunAnalyzers);
            }

            Assert.True(workspace.SetCurrentSolution(s => SetProjectProperties(s, version: 0), WorkspaceChangeKind.SolutionChanged));

            await VerifySolutionUpdate(workspace,
                newSolutionGetter: s => SetProjectProperties(s, version: 1),
                oldSolutionValidator: s => ValidateProperties(s, version: 0),
                newSolutionValidator: s => ValidateProperties(s, version: 1)).ConfigureAwait(false);
        }

        [Fact]
        public async Task TestUpdateDocumentInfo()
        {
            var code = @"class Test { void Method() { } }";

            await VerifySolutionUpdate(code, s => s.WithDocumentFolders(s.Projects.First().Documents.First().Id, new[] { "test" }));
        }

        [Fact]
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

        [Fact]
        public async Task TestAdditionalDocument()
        {
            var code = @"class Test { void Method() { } }";
            using var workspace = TestWorkspace.CreateCSharp(code);

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

        [Fact]
        public async Task TestAnalyzerConfigDocument()
        {
            var configPath = Path.Combine(Path.GetTempPath(), ".editorconfig");
            var code = @"class Test { void Method() { } }";
            using var workspace = TestWorkspace.CreateCSharp(code);

            var projectId = workspace.CurrentSolution.ProjectIds.First();
            var analyzerConfigDocumentId = DocumentId.CreateNewId(projectId);
            var analyzerConfigDocumentInfo = DocumentInfo.Create(
                analyzerConfigDocumentId,
                name: ".editorconfig",
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From("root = true"), VersionStamp.Create(), filePath: configPath)),
                filePath: configPath);

            await VerifySolutionUpdate(workspace, s =>
            {
                return s.AddAnalyzerConfigDocuments(ImmutableArray.Create(analyzerConfigDocumentInfo));
            });

            workspace.OnAnalyzerConfigDocumentAdded(analyzerConfigDocumentInfo);

            await VerifySolutionUpdate(workspace, s =>
            {
                return s.WithAnalyzerConfigDocumentText(analyzerConfigDocumentId, SourceText.From("root = false"));
            });

            await VerifySolutionUpdate(workspace, s =>
            {
                return s.RemoveAnalyzerConfigDocument(analyzerConfigDocumentId);
            });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestDocument()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);

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

        [Fact]
        public async Task TestRemoteWorkspace()
        {
            var code = @"class Test { void Method() { } }";

            // create base solution
            using var workspace = TestWorkspace.CreateCSharp(code);
            using var remoteWorkspace = CreateRemoteWorkspace();

            // create solution service
            var solution1 = workspace.CurrentSolution;
            var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution1);

            var remoteSolution1 = await GetInitialOOPSolutionAsync(remoteWorkspace, assetProvider, solution1);

            await Verify(remoteWorkspace, solution1, remoteSolution1, expectRemoteSolutionToCurrent: true);
            var version = solution1.WorkspaceVersion;

            // update remote workspace
            var currentSolution = remoteSolution1.WithDocumentText(remoteSolution1.Projects.First().Documents.First().Id, SourceText.From(code + " class Test2 { }"));
            var (oopSolution2, _) = await remoteWorkspace.GetTestAccessor().TryUpdateWorkspaceCurrentSolutionAsync(currentSolution, ++version);

            await Verify(remoteWorkspace, currentSolution, oopSolution2, expectRemoteSolutionToCurrent: true);

            // move backward
            await Verify(remoteWorkspace, remoteSolution1, (await remoteWorkspace.GetTestAccessor().TryUpdateWorkspaceCurrentSolutionAsync(remoteSolution1, solution1.WorkspaceVersion)).solution, expectRemoteSolutionToCurrent: false);

            // move forward
            currentSolution = oopSolution2.WithDocumentText(oopSolution2.Projects.First().Documents.First().Id, SourceText.From(code + " class Test3 { }"));
            var remoteSolution3 = (await remoteWorkspace.GetTestAccessor().TryUpdateWorkspaceCurrentSolutionAsync(currentSolution, ++version)).solution;

            await Verify(remoteWorkspace, currentSolution, remoteSolution3, expectRemoteSolutionToCurrent: true);

            // move to new solution backward
            var solutionInfo2 = await assetProvider.CreateSolutionInfoAsync(await solution1.CompilationState.GetChecksumAsync(CancellationToken.None), CancellationToken.None);
            var solution2 = remoteWorkspace.GetTestAccessor().CreateSolutionFromInfo(solutionInfo2);
            Assert.False((await remoteWorkspace.GetTestAccessor().TryUpdateWorkspaceCurrentSolutionAsync(
                solution2, solution1.WorkspaceVersion)).updated);

            // move to new solution forward
            var (solution3, updated3) = await remoteWorkspace.GetTestAccessor().TryUpdateWorkspaceCurrentSolutionAsync(
                solution2, ++version);
            Assert.NotNull(solution3);
            Assert.True(updated3);
            await Verify(remoteWorkspace, solution1, solution3, expectRemoteSolutionToCurrent: true);

            static async Task<Solution> GetInitialOOPSolutionAsync(RemoteWorkspace remoteWorkspace, AssetProvider assetProvider, Solution solution)
            {
                // set up initial solution
                var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
                await remoteWorkspace.UpdatePrimaryBranchSolutionAsync(assetProvider, solutionChecksum, solution.WorkspaceVersion, CancellationToken.None);

                // get solution in remote host
                return await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            }

            static async Task Verify(RemoteWorkspace remoteWorkspace, Solution givenSolution, Solution remoteSolution, bool expectRemoteSolutionToCurrent)
            {
                // verify we got solution expected
                Assert.Equal(await givenSolution.CompilationState.GetChecksumAsync(CancellationToken.None), await remoteSolution.CompilationState.GetChecksumAsync(CancellationToken.None));

                // verify remote workspace got updated
                Assert.True(expectRemoteSolutionToCurrent == (remoteSolution == remoteWorkspace.CurrentSolution));
            }
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/48564")]
        public async Task TestAddingProjectsWithExplicitOptions(bool useDefaultOptionValue)
        {
            using var workspace = TestWorkspace.CreateCSharp(@"public class C { }");
            using var remoteWorkspace = CreateRemoteWorkspace();

            // Initial empty solution
            var solution = workspace.CurrentSolution;
            solution = solution.RemoveProject(solution.ProjectIds.Single());
            var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution);
            var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
            var synched = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, workspaceVersion: 0, CancellationToken.None);
            Assert.Equal(solutionChecksum, await synched.CompilationState.GetChecksumAsync(CancellationToken.None));

            // Add a C# project and a VB project, set some options, and check again
            var csharpDocument = new TestHostDocument("public class C { }");
            var csharpProject = new TestHostProject(workspace, csharpDocument, language: LanguageNames.CSharp, name: "project2");
            var csharpProjectInfo = csharpProject.ToProjectInfo();

            var vbDocument = new TestHostDocument("Public Class D \r\n  Inherits C\r\nEnd Class");
            var vbProject = new TestHostProject(workspace, vbDocument, language: LanguageNames.VisualBasic, name: "project3");
            var vbProjectInfo = vbProject.ToProjectInfo();

            solution = solution.AddProject(csharpProjectInfo).AddProject(vbProjectInfo);
            var newOptionValue = useDefaultOptionValue
                ? FormattingOptions2.NewLine.DefaultValue
                : FormattingOptions2.NewLine.DefaultValue + FormattingOptions2.NewLine.DefaultValue;
            solution = solution.WithOptions(solution.Options
                .WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, newOptionValue)
                .WithChangedOption(FormattingOptions.NewLine, LanguageNames.VisualBasic, newOptionValue));

            assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution);
            solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
            synched = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, workspaceVersion: 2, CancellationToken.None);
            Assert.Equal(solutionChecksum, await synched.CompilationState.GetChecksumAsync(CancellationToken.None));
        }

        [Fact]
        public async Task TestFrozenSourceGeneratedDocument()
        {
            using var workspace = TestWorkspace.CreateCSharp(@"");
            using var remoteWorkspace = CreateRemoteWorkspace();

            var solution = workspace.CurrentSolution
                .Projects.Single()
                .AddAnalyzerReference(new AnalyzerFileReference(typeof(Microsoft.CodeAnalysis.TestSourceGenerator.HelloWorldGenerator).Assembly.Location, new TestAnalyzerAssemblyLoader()))
                .Solution;

            // First sync the solution over that has a generator
            var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution);
            var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
            var synched = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, workspaceVersion: 0, CancellationToken.None);
            Assert.Equal(solutionChecksum, await synched.CompilationState.GetChecksumAsync(CancellationToken.None));

            // Now freeze with some content
            var documentIdentity = (await solution.Projects.Single().GetSourceGeneratedDocumentsAsync()).First().Identity;
            var frozenText1 = SourceText.From("// Hello, World!");
            var frozenSolution1 = solution.WithFrozenSourceGeneratedDocument(documentIdentity, frozenText1).Project.Solution;

            assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, frozenSolution1);
            solutionChecksum = await frozenSolution1.CompilationState.GetChecksumAsync(CancellationToken.None);
            synched = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, workspaceVersion: 1, CancellationToken.None);
            Assert.Equal(solutionChecksum, await synched.CompilationState.GetChecksumAsync(CancellationToken.None));

            // Try freezing with some different content from the original solution
            var frozenText2 = SourceText.From("// Hello, World! A second time!");
            var frozenSolution2 = solution.WithFrozenSourceGeneratedDocument(documentIdentity, frozenText2).Project.Solution;

            assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, frozenSolution2);
            solutionChecksum = await frozenSolution2.CompilationState.GetChecksumAsync(CancellationToken.None);
            synched = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, workspaceVersion: 2, CancellationToken.None);
            Assert.Equal(solutionChecksum, await synched.CompilationState.GetChecksumAsync(CancellationToken.None));
        }

        [Fact]
        public async Task TestPartialProjectSync_GetSolutionFirst()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            using var remoteWorkspace = CreateRemoteWorkspace();

            var solution = workspace.CurrentSolution;

            var project1 = solution.Projects.Single();
            var project2 = solution.AddProject("P2", "P2", LanguageNames.CSharp);

            solution = project2.Solution;

            var map = new Dictionary<Checksum, object>();
            var assetProvider = new AssetProvider(
                Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.GetService<ISerializerService>());

            // Do the initial full sync
            await solution.AppendAssetMapAsync(map, CancellationToken.None);

            var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
            var syncedFullSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, workspaceVersion: solution.WorkspaceVersion, CancellationToken.None);

            Assert.Equal(solutionChecksum, await syncedFullSolution.CompilationState.GetChecksumAsync(CancellationToken.None));
            Assert.Equal(2, syncedFullSolution.Projects.Count());

            // Syncing project1 should do nothing as syncing the solution already synced it over.
            var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
            await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
            var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(2, project1SyncedSolution.Projects.Count());

            // Syncing project2 should do nothing as syncing the solution already synced it over.
            var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
            await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
            var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(2, project2SyncedSolution.Projects.Count());
        }

        [Fact]
        public async Task TestPartialProjectSync_GetSolutionLast()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            using var remoteWorkspace = CreateRemoteWorkspace();

            var solution = workspace.CurrentSolution;

            var project1 = solution.Projects.Single();
            var project2 = solution.AddProject("P2", "P2", LanguageNames.CSharp);

            solution = project2.Solution;

            var map = new Dictionary<Checksum, object>();
            var assetProvider = new AssetProvider(
                Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.GetService<ISerializerService>());

            // Syncing project 1 should just since it over.
            await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
            var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
            var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(1, project1SyncedSolution.Projects.Count());
            Assert.Equal(project1.Name, project1SyncedSolution.Projects.Single().Name);

            // Syncing project 2 should end up with only p2 synced over.
            await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
            var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
            var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(1, project2SyncedSolution.Projects.Count());

            // then syncing the whole project should now copy both over.
            await solution.AppendAssetMapAsync(map, CancellationToken.None);
            var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
            var syncedFullSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, workspaceVersion: solution.WorkspaceVersion, CancellationToken.None);

            Assert.Equal(solutionChecksum, await syncedFullSolution.CompilationState.GetChecksumAsync(CancellationToken.None));
            Assert.Equal(2, syncedFullSolution.Projects.Count());
        }

        [Fact]
        public async Task TestPartialProjectSync_GetDependentProjects1()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            using var remoteWorkspace = CreateRemoteWorkspace();

            var solution = workspace.CurrentSolution;

            var project1 = solution.Projects.Single();
            var project2 = solution.AddProject("P2", "P2", LanguageNames.CSharp);
            var project3 = project2.Solution.AddProject("P3", "P3", LanguageNames.CSharp);

            solution = project3.Solution.AddProjectReference(project3.Id, new(project3.Solution.Projects.Single(p => p.Name == "P2").Id));

            var map = new Dictionary<Checksum, object>();
            var assetProvider = new AssetProvider(
                Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.GetService<ISerializerService>());

            await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
            var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
            var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(1, project2SyncedSolution.Projects.Count());
            Assert.Equal(project2.Name, project2SyncedSolution.Projects.Single().Name);

            // syncing project 3 should sync project 2 as well because of the p2p ref
            await solution.AppendAssetMapAsync(map, project3.Id, CancellationToken.None);
            var project3Checksum = await solution.CompilationState.GetChecksumAsync(project3.Id, CancellationToken.None);
            var project3SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project3Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(2, project3SyncedSolution.Projects.Count());
        }

        [Fact]
        public async Task TestPartialProjectSync_GetDependentProjects2()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            using var remoteWorkspace = CreateRemoteWorkspace();

            var solution = workspace.CurrentSolution;

            var project1 = solution.Projects.Single();
            var project2 = solution.AddProject("P2", "P2", LanguageNames.CSharp);
            var project3 = project2.Solution.AddProject("P3", "P3", LanguageNames.CSharp);

            solution = project3.Solution.AddProjectReference(project3.Id, new(project3.Solution.Projects.Single(p => p.Name == "P2").Id));

            var map = new Dictionary<Checksum, object>();
            var assetProvider = new AssetProvider(
                Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.GetService<ISerializerService>());

            // syncing P3 should since project P2 as well because of the p2p ref
            await solution.AppendAssetMapAsync(map, project3.Id, CancellationToken.None);
            var project3Checksum = await solution.CompilationState.GetChecksumAsync(project3.Id, CancellationToken.None);
            var project3SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project3Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(2, project3SyncedSolution.Projects.Count());

            // if we then sync just P2, we should still have only P2 in the synced cone
            await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
            var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
            var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(1, project2SyncedSolution.Projects.Count());
            AssertEx.Equal(project2.Name, project2SyncedSolution.Projects.Single().Name);

            // if we then sync just P1, we should only have it in its own cone.
            await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
            var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
            var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(1, project1SyncedSolution.Projects.Count());
            AssertEx.Equal(project1.Name, project1SyncedSolution.Projects.Single().Name);
        }

        [Fact]
        public async Task TestPartialProjectSync_GetDependentProjects3()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            using var remoteWorkspace = CreateRemoteWorkspace();

            var solution = workspace.CurrentSolution;

            var project1 = solution.Projects.Single();
            var project2 = solution.AddProject("P2", "P2", LanguageNames.CSharp);
            var project3 = project2.Solution.AddProject("P3", "P3", LanguageNames.CSharp);

            solution = project3.Solution.AddProjectReference(project3.Id, new(project2.Id))
                                        .AddProjectReference(project2.Id, new(project1.Id));

            var map = new Dictionary<Checksum, object>();
            var assetProvider = new AssetProvider(
                Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.GetService<ISerializerService>());

            // syncing project3 should since project2 and project1 as well because of the p2p ref
            await solution.AppendAssetMapAsync(map, project3.Id, CancellationToken.None);
            var project3Checksum = await solution.CompilationState.GetChecksumAsync(project3.Id, CancellationToken.None);
            var project3SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project3Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(3, project3SyncedSolution.Projects.Count());

            // syncing project2 should only have it and project 1.
            await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
            var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
            var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(2, project2SyncedSolution.Projects.Count());

            // syncing project1 should only be itself
            await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
            var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
            var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(1, project1SyncedSolution.Projects.Count());
        }

        [Fact]
        public async Task TestPartialProjectSync_GetDependentProjects4()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            using var remoteWorkspace = CreateRemoteWorkspace();

            var solution = workspace.CurrentSolution;

            var project1 = solution.Projects.Single();
            var project2 = solution.AddProject("P2", "P2", LanguageNames.CSharp);
            var project3 = project2.Solution.AddProject("P3", "P3", LanguageNames.CSharp);

            solution = project3.Solution.AddProjectReference(project3.Id, new(project2.Id))
                                        .AddProjectReference(project3.Id, new(project1.Id));

            var map = new Dictionary<Checksum, object>();
            var assetProvider = new AssetProvider(
                Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.GetService<ISerializerService>());

            // syncing project3 should since project2 and project1 as well because of the p2p ref
            await solution.AppendAssetMapAsync(map, project3.Id, CancellationToken.None);
            var project3Checksum = await solution.CompilationState.GetChecksumAsync(project3.Id, CancellationToken.None);
            var project3SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project3Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(3, project3SyncedSolution.Projects.Count());

            // Syncing project2 should only have a cone with itself.
            await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
            var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
            var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(1, project2SyncedSolution.Projects.Count());

            // Syncing project1 should only have a cone with itself.
            await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
            var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
            var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(1, project1SyncedSolution.Projects.Count());
        }

        [Fact]
        public async Task TestPartialProjectSync_Options1()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            using var remoteWorkspace = CreateRemoteWorkspace();

            var solution = workspace.CurrentSolution;

            var project1 = solution.Projects.Single();
            var project2 = solution.AddProject("P2", "P2", LanguageNames.VisualBasic);

            solution = project2.Solution;

            var map = new Dictionary<Checksum, object>();
            var assetProvider = new AssetProvider(
                Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.GetService<ISerializerService>());

            // Syncing over project1 should give us 1 set of options on the OOP side.
            await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
            var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
            var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(1, project1SyncedSolution.Projects.Count());
            Assert.Equal(project1.Name, project1SyncedSolution.Projects.Single().Name);

            // Syncing over project2 should also only be one set of options.
            await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
            var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
            var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            Assert.Equal(1, project2SyncedSolution.Projects.Count());
        }

        [Fact]
        public async Task TestPartialProjectSync_DoesNotSeeChangesOutsideOfCone()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            using var remoteWorkspace = CreateRemoteWorkspace();

            var solution = workspace.CurrentSolution;

            var project1 = solution.Projects.Single();
            var project2 = solution.AddProject("P2", "P2", LanguageNames.VisualBasic);

            solution = project2.Solution;

            var map = new Dictionary<Checksum, object>();
            var assetProvider = new AssetProvider(
                Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.GetService<ISerializerService>());

            // Do the initial full sync
            await solution.AppendAssetMapAsync(map, CancellationToken.None);
            var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
            var fullSyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, workspaceVersion: solution.WorkspaceVersion, CancellationToken.None);
            Assert.Equal(2, fullSyncedSolution.Projects.Count());

            // Mutate both projects to each have a document in it.
            solution = solution.GetProject(project1.Id).AddDocument("X.cs", SourceText.From("// X")).Project.Solution;
            solution = solution.GetProject(project2.Id).AddDocument("Y.vb", SourceText.From("' Y")).Project.Solution;

            // Now just sync project1's cone over.  We should not see the change to project2 on the remote side.
            // But we will still see project2.
            {
                await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
                var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
                var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
                Assert.Equal(2, project1SyncedSolution.Projects.Count());
                var csharpProject = project1SyncedSolution.Projects.Single(p => p.Language == LanguageNames.CSharp);
                var vbProject = project1SyncedSolution.Projects.Single(p => p.Language == LanguageNames.VisualBasic);
                Assert.True(csharpProject.DocumentIds.Count == 2);
                Assert.Empty(vbProject.DocumentIds);
            }

            // Similarly, if we sync just project2's cone over:
            {
                await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
                var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
                var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
                Assert.Equal(2, project2SyncedSolution.Projects.Count());
                var csharpProject = project2SyncedSolution.Projects.Single(p => p.Language == LanguageNames.CSharp);
                var vbProject = project2SyncedSolution.Projects.Single(p => p.Language == LanguageNames.VisualBasic);
                Assert.Single(csharpProject.DocumentIds);
                Assert.Single(vbProject.DocumentIds);
            }
        }

        [Fact]
        public async Task TestPartialProjectSync_AddP2PRef()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            using var remoteWorkspace = CreateRemoteWorkspace();

            var solution = workspace.CurrentSolution;

            var project1 = solution.Projects.Single();
            var project2 = solution.AddProject("P2", "P2", LanguageNames.CSharp);

            solution = project2.Solution;

            var map = new Dictionary<Checksum, object>();
            var assetProvider = new AssetProvider(
                Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.GetService<ISerializerService>());

            // Do the initial full sync
            await solution.AppendAssetMapAsync(map, CancellationToken.None);
            var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
            var fullSyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, workspaceVersion: solution.WorkspaceVersion, CancellationToken.None);
            Assert.Equal(2, fullSyncedSolution.Projects.Count());

            // Mutate both projects to have a document in it, and add a p2p ref from project1 to project2
            solution = solution.GetProject(project1.Id).AddDocument("X.cs", SourceText.From("// X")).Project.Solution;
            solution = solution.GetProject(project2.Id).AddDocument("Y.cs", SourceText.From("// Y")).Project.Solution;
            solution = solution.GetProject(project1.Id).AddProjectReference(new ProjectReference(project2.Id)).Solution;

            // Now just sync project1's cone over.  This will validate that the p2p ref doesn't try to add a new
            // project, but instead sees the existing one.
            {
                await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
                var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
                var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
                Assert.Equal(2, project1SyncedSolution.Projects.Count());
                var project1Synced = project1SyncedSolution.GetRequiredProject(project1.Id);
                var project2Synced = project1SyncedSolution.GetRequiredProject(project2.Id);

                Assert.True(project1Synced.DocumentIds.Count == 2);
                Assert.Single(project2Synced.DocumentIds);
                Assert.Single(project1Synced.ProjectReferences);
            }
        }

        [Fact]
        public async Task TestPartialProjectSync_ReferenceToNonExistentProject()
        {
            var code = @"class Test { void Method() { } }";

            using var workspace = TestWorkspace.CreateCSharp(code);
            using var remoteWorkspace = CreateRemoteWorkspace();

            var solution = workspace.CurrentSolution;

            var project1 = solution.Projects.Single();

            // This reference a project that doesn't exist.
            // Ensure that it's still fine to get the checksum for this project we have.
            project1 = project1.AddProjectReference(new ProjectReference(ProjectId.CreateNewId()));

            solution = project1.Solution;

            var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution);

            var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
        }

        private static async Task VerifySolutionUpdate(string code, Func<Solution, Solution> newSolutionGetter)
        {
            using var workspace = TestWorkspace.CreateCSharp(code);
            await VerifySolutionUpdate(workspace, newSolutionGetter);
        }

        private static async Task VerifySolutionUpdate(
            TestWorkspace workspace,
            Func<Solution, Solution> newSolutionGetter,
            Action<Solution> oldSolutionValidator = null,
            Action<Solution> newSolutionValidator = null)
        {
            var solution = workspace.CurrentSolution;
            oldSolutionValidator?.Invoke(solution);

            var map = new Dictionary<Checksum, object>();

            using var remoteWorkspace = CreateRemoteWorkspace();
            var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution, map);
            var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);

            // update primary workspace
            await remoteWorkspace.UpdatePrimaryBranchSolutionAsync(assetProvider, solutionChecksum, solution.WorkspaceVersion, CancellationToken.None);
            var recoveredSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);
            oldSolutionValidator?.Invoke(recoveredSolution);

            Assert.Equal(WorkspaceKind.RemoteWorkspace, recoveredSolution.WorkspaceKind);
            Assert.Equal(solutionChecksum, await recoveredSolution.CompilationState.GetChecksumAsync(CancellationToken.None));

            // get new solution
            var newSolution = newSolutionGetter(solution);
            var newSolutionChecksum = await newSolution.CompilationState.GetChecksumAsync(CancellationToken.None);
            await newSolution.AppendAssetMapAsync(map, CancellationToken.None);

            // get solution without updating primary workspace
            var recoveredNewSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, newSolutionChecksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);

            Assert.Equal(newSolutionChecksum, await recoveredNewSolution.CompilationState.GetChecksumAsync(CancellationToken.None));

            // do same once updating primary workspace
            await remoteWorkspace.UpdatePrimaryBranchSolutionAsync(assetProvider, newSolutionChecksum, solution.WorkspaceVersion + 1, CancellationToken.None);
            var third = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, newSolutionChecksum, updatePrimaryBranch: false, workspaceVersion: -1, CancellationToken.None);

            Assert.Equal(newSolutionChecksum, await third.CompilationState.GetChecksumAsync(CancellationToken.None));

            newSolutionValidator?.Invoke(recoveredNewSolution);
        }

        private static async Task<AssetProvider> GetAssetProviderAsync(Workspace workspace, RemoteWorkspace remoteWorkspace, Solution solution, Dictionary<Checksum, object> map = null)
        {
            // make sure checksum is calculated
            await solution.CompilationState.GetChecksumAsync(CancellationToken.None);

            map ??= [];
            await solution.AppendAssetMapAsync(map, CancellationToken.None);

            var sessionId = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
            var storage = new SolutionAssetCache();
            var assetSource = new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map);

            return new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.GetService<ISerializerService>());
        }
    }
}
