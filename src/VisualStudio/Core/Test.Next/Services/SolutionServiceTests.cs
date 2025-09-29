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
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.RemoteHost)]
public sealed class SolutionServiceTests
{
    private static readonly TestComposition s_composition = FeaturesTestCompositions.Features.WithTestHostParts(TestHost.OutOfProcess);
    private static readonly TestComposition s_compositionWithFirstDocumentIsActiveAndVisible =
        s_composition.AddParts(typeof(FirstDocumentIsActiveAndVisibleDocumentTrackingService.Factory));

    private static RemoteWorkspace CreateRemoteWorkspace()
        => new(FeaturesTestCompositions.RemoteHost.GetHostServices());

    [Fact]
    public async Task TestCreation()
    {
        var code = @"class Test { void Method() { } }";

        using var workspace = TestWorkspace.CreateCSharp(code);
        using var remoteWorkspace = CreateRemoteWorkspace();

        var solution = workspace.CurrentSolution;
        var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution);

        var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
        var synched = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, CancellationToken.None);

        Assert.Equal(solutionChecksum, await synched.CompilationState.GetChecksumAsync(CancellationToken.None));
    }

    [Theory, CombinatorialData]
    public async Task TestGetSolutionWithPrimaryFlag(bool updatePrimaryBranch)
    {
        var code1 = @"class Test1 { void Method() { } }";

        using var workspace = TestWorkspace.CreateCSharp(code1);
        using var remoteWorkspace = CreateRemoteWorkspace();

        var solution = workspace.CurrentSolution;
        var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
        var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution);

        var synched = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch, cancellationToken: CancellationToken.None);
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
        var solution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, CancellationToken.None);

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
        var solution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, CancellationToken.None);

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

        var first = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, CancellationToken.None);
        var second = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, CancellationToken.None);

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
    public async Task FallbackAnalyzerOptions()
    {
        using var workspace = TestWorkspace.CreateCSharp("");

        static Solution SetSolutionProperties(Solution solution, int version)
        {
            return solution.WithFallbackAnalyzerOptions(ImmutableDictionary<string, StructuredAnalyzerConfigOptions>.Empty
                .Add(LanguageNames.CSharp, StructuredAnalyzerConfigOptions.Create(new DictionaryAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty
                    .Add("cs_optionA", $"csA{version}")
                    .Add("cs_optionB", $"csB{version}"))))
                .Add(LanguageNames.VisualBasic, StructuredAnalyzerConfigOptions.Create(new DictionaryAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty
                    .Add("vb_optionA", $"vbA{version}")
                    .Add("vb_optionB", $"vbB{version}"))))
                .Add(LanguageNames.FSharp, StructuredAnalyzerConfigOptions.Create(new DictionaryAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty
                    .Add("fs_optionA", $"fsA{version}")
                    .Add("fs_optionB", $"fsB{version}")))));
        }

        static void ValidateProperties(Solution solution, int version, bool isRecovered)
        {
            // F# options are serialized because F# projects are not available OOP.

            AssertEx.SetEqual(isRecovered
                ? [
                    $"C#: [cs_optionA = csA{version}, cs_optionB = csB{version}]",
                    $"Visual Basic: [vb_optionA = vbA{version}, vb_optionB = vbB{version}]",
                ]
                :
                [
                    $"F#: [fs_optionA = fsA{version}, fs_optionB = fsB{version}]",
                    $"C#: [cs_optionA = csA{version}, cs_optionB = csB{version}]",
                    $"Visual Basic: [vb_optionA = vbA{version}, vb_optionB = vbB{version}]",
                ],
                solution.FallbackAnalyzerOptions.Select(languageOptions =>
                    $"{languageOptions.Key}: [" +
                    $"{string.Join(", ", languageOptions.Value.Keys.Order().Select(k => $"{k} = {(languageOptions.Value.TryGetValue(k, out var v) ? v : null)}"))}]"));
        }

        Assert.True(workspace.SetCurrentSolution(s => SetSolutionProperties(s, version: 0), WorkspaceChangeKind.SolutionChanged));

        await VerifySolutionUpdate(workspace,
            newSolutionGetter: s => SetSolutionProperties(s, version: 1),
            oldSolutionValidator: s => ValidateProperties(s, version: 0, isRecovered: false),
            oldRecoveredSolutionValidator: s => ValidateProperties(s, version: 0, isRecovered: true),
            newRecoveredSolutionValidator: s => ValidateProperties(s, version: 1, isRecovered: true)).ConfigureAwait(false);
    }

    [Fact]
    public async Task ProjectCountsByLanguage()
    {
        using var workspace = TestWorkspace.CreateCSharp("");

        static Solution SetSolutionProperties(Solution solution, int version)
        {
            foreach (var projectId in solution.ProjectIds)
                solution = solution.RemoveProject(projectId);

            for (var i = 0; i < version + 2; i++)
                solution = solution.AddProject("CS" + i, "CS" + i, LanguageNames.CSharp).Solution;

            return solution
                .AddProject("VB1", "VB1", LanguageNames.VisualBasic).Solution;
        }

        static void ValidateProperties(Solution solution, int version)
        {
            AssertEx.SetEqual(
            [
                (LanguageNames.CSharp, version + 2),
                (LanguageNames.VisualBasic, 1),
            ], solution.SolutionState.ProjectCountByLanguage.Select(e => (e.Key, e.Value)));
        }

        Assert.True(workspace.SetCurrentSolution(s => SetSolutionProperties(s, version: 0), WorkspaceChangeKind.SolutionChanged));

        await VerifySolutionUpdate(workspace,
            newSolutionGetter: s => SetSolutionProperties(s, version: 1),
            oldSolutionValidator: s => ValidateProperties(s, version: 0),
            newSolutionValidator: s => ValidateProperties(s, version: 1)).ConfigureAwait(false);
    }

    [Fact]
    public async Task ProjectProperties()
    {
        var dir = Path.GetDirectoryName(typeof(SolutionServiceTests).Assembly.Location);
        using var workspace = TestWorkspace.CreateCSharp("");

        Solution SetProjectProperties(Solution solution, int version)
        {
            var projectId = solution.ProjectIds.Single();
            return solution
                .WithProjectName(projectId, "Name" + version)
                .WithProjectAssemblyName(projectId, "AssemblyName" + version)
                .WithProjectFilePath(projectId, "FilePath" + version)
                .WithProjectOutputFilePath(projectId, "OutputFilePath" + version)
                .WithProjectOutputRefFilePath(projectId, "OutputRefFilePath" + version)
                .WithProjectCompilationOutputInfo(projectId, new CompilationOutputInfo("AssemblyPath" + version, dir + version))
                .WithProjectDefaultNamespace(projectId, "DefaultNamespace" + version)
                .WithProjectChecksumAlgorithm(projectId, SourceHashAlgorithm.Sha1 + version)
                .WithHasAllInformation(projectId, (version % 2) != 0)
                .WithRunAnalyzers(projectId, (version % 2) != 0);
        }

        void ValidateProperties(Solution solution, int version)
        {
            var project = solution.Projects.Single();
            Assert.Equal("Name" + version, project.Name);
            Assert.Equal("AssemblyName" + version, project.AssemblyName);
            Assert.Equal("FilePath" + version, project.FilePath);
            Assert.Equal("OutputFilePath" + version, project.OutputFilePath);
            Assert.Equal("OutputRefFilePath" + version, project.OutputRefFilePath);
            Assert.Equal(dir + version, project.CompilationOutputInfo.GeneratedFilesOutputDirectory);
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
    public Task TestUpdateDocumentInfo()
        => VerifySolutionUpdate(@"class Test { void Method() { } }", s => s.WithDocumentFolders(s.Projects.First().Documents.First().Id, ["test"]));

    [Fact]
    public Task TestAddUpdateRemoveProjects()
        => VerifySolutionUpdate(@"class Test { void Method() { } }", s =>
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
            return s.AddAnalyzerConfigDocuments([analyzerConfigDocumentInfo]);
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

        await Verify(remoteWorkspace, solution1, remoteSolution1);

        // update remote workspace
        var currentSolution = remoteSolution1.WithDocumentText(remoteSolution1.Projects.First().Documents.First().Id, SourceText.From(code + " class Test2 { }"));
        var oopSolution2 = await remoteWorkspace.GetTestAccessor().UpdateWorkspaceCurrentSolutionAsync(currentSolution);

        await Verify(remoteWorkspace, currentSolution, oopSolution2);

        // move backward
        await Verify(remoteWorkspace, remoteSolution1, await remoteWorkspace.GetTestAccessor().UpdateWorkspaceCurrentSolutionAsync(remoteSolution1));

        // move forward
        currentSolution = oopSolution2.WithDocumentText(oopSolution2.Projects.First().Documents.First().Id, SourceText.From(code + " class Test3 { }"));
        var remoteSolution3 = await remoteWorkspace.GetTestAccessor().UpdateWorkspaceCurrentSolutionAsync(currentSolution);

        await Verify(remoteWorkspace, currentSolution, remoteSolution3);

        // move to new solution backward
        var solutionInfo2 = await assetProvider.CreateSolutionInfoAsync(
            await solution1.CompilationState.GetChecksumAsync(CancellationToken.None),
            remoteWorkspace.Services.SolutionServices,
            CancellationToken.None);
        var solution2 = remoteWorkspace.GetTestAccessor().CreateSolutionFromInfo(solutionInfo2);

        // move to new solution forward
        var solution3 = await remoteWorkspace.GetTestAccessor().UpdateWorkspaceCurrentSolutionAsync(solution2);
        Assert.NotNull(solution3);
        await Verify(remoteWorkspace, solution1, solution3);

        static async Task<Solution> GetInitialOOPSolutionAsync(RemoteWorkspace remoteWorkspace, AssetProvider assetProvider, Solution solution)
        {
            // set up initial solution
            var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
            await remoteWorkspace.UpdatePrimaryBranchSolutionAsync(assetProvider, solutionChecksum, CancellationToken.None);

            // get solution in remote host
            return await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, CancellationToken.None);
        }

        static async Task Verify(RemoteWorkspace remoteWorkspace, Solution givenSolution, Solution remoteSolution)
        {
            // verify we got solution expected
            Assert.Equal(await givenSolution.CompilationState.GetChecksumAsync(CancellationToken.None), await remoteSolution.CompilationState.GetChecksumAsync(CancellationToken.None));

            // verify remote workspace got updated
            Assert.Equal(remoteSolution, remoteWorkspace.CurrentSolution);
        }
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/48564")]
    public async Task TestAddingProjectsWithExplicitOptions(bool useDefaultOptionValue)
    {
        using var workspace = TestWorkspace.CreateCSharp(@"public class C { }");

        var solution = workspace.CurrentSolution;
        var solutionChecksum1 = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);

        // legacy options are not serialized and have no effect on checksum:
        var newOptionValue = useDefaultOptionValue
            ? FormattingOptions2.NewLine.DefaultValue
            : FormattingOptions2.NewLine.DefaultValue + FormattingOptions2.NewLine.DefaultValue;
        solution = solution.WithOptions(solution.Options
            .WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, newOptionValue)
            .WithChangedOption(FormattingOptions.NewLine, LanguageNames.VisualBasic, newOptionValue));

        var solutionChecksum2 = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
        Assert.Equal(solutionChecksum1, solutionChecksum2);
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
        var synched = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, CancellationToken.None);
        Assert.Equal(solutionChecksum, await synched.CompilationState.GetChecksumAsync(CancellationToken.None));

        // Now freeze with some content
        var documentIdentity = (await solution.Projects.Single().GetSourceGeneratedDocumentsAsync()).First().Identity;
        var frozenText1 = SourceText.From("// Hello, World!");
        var frozenSolution1 = solution.WithFrozenSourceGeneratedDocument(documentIdentity, DateTime.Now, frozenText1).Project.Solution;

        assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, frozenSolution1);
        solutionChecksum = await frozenSolution1.CompilationState.GetChecksumAsync(CancellationToken.None);
        synched = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, CancellationToken.None);
        Assert.Equal(solutionChecksum, await synched.CompilationState.GetChecksumAsync(CancellationToken.None));

        // Try freezing with some different content from the original solution
        var frozenText2 = SourceText.From("// Hello, World! A second time!");
        var frozenSolution2 = solution.WithFrozenSourceGeneratedDocument(documentIdentity, DateTime.Now, frozenText2).Project.Solution;

        assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, frozenSolution2);
        solutionChecksum = await frozenSolution2.CompilationState.GetChecksumAsync(CancellationToken.None);
        synched = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, CancellationToken.None);
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
            Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.SolutionServices);

        // Do the initial full sync
        await solution.AppendAssetMapAsync(map, CancellationToken.None);

        var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
        var syncedFullSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, CancellationToken.None);

        Assert.Equal(solutionChecksum, await syncedFullSolution.CompilationState.GetChecksumAsync(CancellationToken.None));
        Assert.Equal(2, syncedFullSolution.Projects.Count());

        // Syncing project1 should do nothing as syncing the solution already synced it over.
        var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
        await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
        var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, CancellationToken.None);
        Assert.Equal(2, project1SyncedSolution.Projects.Count());

        // Syncing project2 should do nothing as syncing the solution already synced it over.
        var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
        await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
        var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, CancellationToken.None);
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
            Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.SolutionServices);

        // Syncing project 1 should just since it over.
        await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
        var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
        var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, CancellationToken.None);
        Assert.Equal(1, project1SyncedSolution.Projects.Count());
        Assert.Equal(project1.Name, project1SyncedSolution.Projects.Single().Name);

        // Syncing project 2 should end up with only p2 synced over.
        await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
        var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
        var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, CancellationToken.None);
        Assert.Equal(1, project2SyncedSolution.Projects.Count());

        // then syncing the whole project should now copy both over.
        await solution.AppendAssetMapAsync(map, CancellationToken.None);
        var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
        var syncedFullSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, CancellationToken.None);

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
            Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.SolutionServices);

        await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
        var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
        var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, CancellationToken.None);
        Assert.Equal(1, project2SyncedSolution.Projects.Count());
        Assert.Equal(project2.Name, project2SyncedSolution.Projects.Single().Name);

        // syncing project 3 should sync project 2 as well because of the p2p ref
        await solution.AppendAssetMapAsync(map, project3.Id, CancellationToken.None);
        var project3Checksum = await solution.CompilationState.GetChecksumAsync(project3.Id, CancellationToken.None);
        var project3SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project3Checksum, updatePrimaryBranch: false, CancellationToken.None);
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
            Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.SolutionServices);

        // syncing P3 should since project P2 as well because of the p2p ref
        await solution.AppendAssetMapAsync(map, project3.Id, CancellationToken.None);
        var project3Checksum = await solution.CompilationState.GetChecksumAsync(project3.Id, CancellationToken.None);
        var project3SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project3Checksum, updatePrimaryBranch: false, CancellationToken.None);
        Assert.Equal(2, project3SyncedSolution.Projects.Count());

        // if we then sync just P2, we should still have only P2 in the synced cone
        await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
        var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
        var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, CancellationToken.None);
        Assert.Equal(1, project2SyncedSolution.Projects.Count());
        AssertEx.Equal(project2.Name, project2SyncedSolution.Projects.Single().Name);

        // if we then sync just P1, we should only have it in its own cone.
        await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
        var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
        var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, CancellationToken.None);
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
            Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.SolutionServices);

        // syncing project3 should since project2 and project1 as well because of the p2p ref
        await solution.AppendAssetMapAsync(map, project3.Id, CancellationToken.None);
        var project3Checksum = await solution.CompilationState.GetChecksumAsync(project3.Id, CancellationToken.None);
        var project3SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project3Checksum, updatePrimaryBranch: false, CancellationToken.None);
        Assert.Equal(3, project3SyncedSolution.Projects.Count());

        // syncing project2 should only have it and project 1.
        await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
        var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
        var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, CancellationToken.None);
        Assert.Equal(2, project2SyncedSolution.Projects.Count());

        // syncing project1 should only be itself
        await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
        var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
        var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, CancellationToken.None);
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
            Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.SolutionServices);

        // syncing project3 should since project2 and project1 as well because of the p2p ref
        await solution.AppendAssetMapAsync(map, project3.Id, CancellationToken.None);
        var project3Checksum = await solution.CompilationState.GetChecksumAsync(project3.Id, CancellationToken.None);
        var project3SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project3Checksum, updatePrimaryBranch: false, CancellationToken.None);
        Assert.Equal(3, project3SyncedSolution.Projects.Count());

        // Syncing project2 should only have a cone with itself.
        await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
        var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
        var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, CancellationToken.None);
        Assert.Equal(1, project2SyncedSolution.Projects.Count());

        // Syncing project1 should only have a cone with itself.
        await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
        var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
        var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, CancellationToken.None);
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
            Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.SolutionServices);

        // Syncing over project1 should give us 1 set of options on the OOP side.
        await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
        var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
        var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, CancellationToken.None);
        Assert.Equal(1, project1SyncedSolution.Projects.Count());
        Assert.Equal(project1.Name, project1SyncedSolution.Projects.Single().Name);

        // Syncing over project2 should also only be one set of options.
        await solution.AppendAssetMapAsync(map, project2.Id, CancellationToken.None);
        var project2Checksum = await solution.CompilationState.GetChecksumAsync(project2.Id, CancellationToken.None);
        var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, CancellationToken.None);
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
            Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.SolutionServices);

        // Do the initial full sync
        await solution.AppendAssetMapAsync(map, CancellationToken.None);
        var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
        var fullSyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, CancellationToken.None);
        Assert.Equal(2, fullSyncedSolution.Projects.Count());

        // Mutate both projects to each have a document in it.
        solution = solution.GetProject(project1.Id).AddDocument("X.cs", SourceText.From("// X")).Project.Solution;
        solution = solution.GetProject(project2.Id).AddDocument("Y.vb", SourceText.From("' Y")).Project.Solution;

        // Now just sync project1's cone over.  We should not see the change to project2 on the remote side.
        // But we will still see project2.
        {
            await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
            var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
            var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, CancellationToken.None);
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
            var project2SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project2Checksum, updatePrimaryBranch: false, CancellationToken.None);
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
            Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.SolutionServices);

        // Do the initial full sync
        await solution.AppendAssetMapAsync(map, CancellationToken.None);
        var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
        var fullSyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, CancellationToken.None);
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
            var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, CancellationToken.None);
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

    [Fact]
    public async Task TestPartialProjectSync_SourceGeneratorExecutionVersion_1()
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
            Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray())), new SolutionAssetCache(), new SimpleAssetSource(workspace.Services.GetService<ISerializerService>(), map), remoteWorkspace.Services.SolutionServices);

        // Do the initial full sync
        await solution.AppendAssetMapAsync(map, CancellationToken.None);
        var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
        var fullSyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: true, CancellationToken.None);
        Assert.Equal(2, fullSyncedSolution.Projects.Count());

        // Update the source generator versions for all projects for the local workspace.
        workspace.EnqueueUpdateSourceGeneratorVersion(projectId: null, forceRegeneration: true);
        await GetWorkspaceWaiter(workspace).ExpeditedWaitAsync();
        solution = workspace.CurrentSolution;

        // Now just sync project1's cone over.  This will validate that that we get the right checksums, even with a
        // partial cone sync.
        {
            await solution.AppendAssetMapAsync(map, project1.Id, CancellationToken.None);
            var project1Checksum = await solution.CompilationState.GetChecksumAsync(project1.Id, CancellationToken.None);
            var project1SyncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, project1Checksum, updatePrimaryBranch: false, CancellationToken.None);
        }
    }

    private static IAsynchronousOperationWaiter GetWorkspaceWaiter(TestWorkspace workspace)
    {
        var operations = workspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        return operations.GetWaiter(FeatureAttribute.Workspace);
    }

    [Fact]
    public void TestNoActiveDocumentSemanticModelNotCached()
    {
        var code = @"class Test { void Method() { } }";

        using var workspace = TestWorkspace.CreateCSharp(code);
        using var remoteWorkspace = CreateRemoteWorkspace();

        var solution = workspace.CurrentSolution;

        var project1 = solution.Projects.Single();
        var document1 = project1.Documents.Single();

        // Without anything holding onto the semantic model, it should get releases.
        var objectReference = ObjectReference.CreateFromFactory(() => document1.GetSemanticModelAsync().GetAwaiter().GetResult());

        objectReference.AssertReleased();
    }

    [Fact]
    public void TestActiveDocumentSemanticModelCached()
    {
        var code = @"class Test { void Method() { } }";

        using var workspace = TestWorkspace.CreateCSharp(code, composition: s_compositionWithFirstDocumentIsActiveAndVisible);
        using var remoteWorkspace = CreateRemoteWorkspace();

        var solution = workspace.CurrentSolution;

        var project1 = solution.Projects.Single();
        var document1 = project1.Documents.Single();

        // Since this is the active document, we should hold onto it.
        var objectReference = ObjectReference.CreateFromFactory(() => document1.GetSemanticModelAsync().GetAwaiter().GetResult());

        objectReference.AssertHeld();
    }

    [Fact]
    public void TestOnlyActiveDocumentSemanticModelCached()
    {
        using var workspace = TestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        class Program1
                        {
                        }
                    </Document>
                    <Document FilePath="File2.cs">
                        class Program2
                        {
                        }
                    </Document>
                </Project>
            </Workspace>
            """, composition: s_compositionWithFirstDocumentIsActiveAndVisible);
        using var remoteWorkspace = CreateRemoteWorkspace();

        var solution = workspace.CurrentSolution;

        var project1 = solution.Projects.Single();
        var document1 = project1.Documents.First();
        var document2 = project1.Documents.Last();

        // Only the semantic model for the active document should be cached.
        var objectReference1 = ObjectReference.CreateFromFactory(() => document1.GetSemanticModelAsync().GetAwaiter().GetResult());
        var objectReference2 = ObjectReference.CreateFromFactory(() => document2.GetSemanticModelAsync().GetAwaiter().GetResult());

        objectReference1.AssertHeld();
        objectReference2.AssertReleased();
    }

    [Fact]
    public void TestActiveAndRelatedDocumentSemanticModelCached()
    {
        using var workspace = TestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        class Program1
                        {
                        }
                    </Document>
                    <Document FilePath="File2.cs">
                        class Program2
                        {
                        }
                    </Document>
                </Project>
                <Project Language="C#" AssemblyName="Assembly2" CommonReferences="true">
                    <Document IsLinkFile="true" LinkAssemblyName="Assembly1" LinkFilePath="File1.cs" />
                </Project>
            </Workspace>
            """, composition: s_compositionWithFirstDocumentIsActiveAndVisible);
        using var remoteWorkspace = CreateRemoteWorkspace();

        var solution = workspace.CurrentSolution;

        var project1 = solution.Projects.Single(p => p.AssemblyName == "Assembly1");
        var project2 = solution.Projects.Single(p => p.AssemblyName == "Assembly2");
        var document1 = project1.Documents.First();
        var document2 = project1.Documents.Last();
        var document3 = project2.Documents.Single();

        // Only the semantic model for the active document should be cached.
        var objectReference1 = ObjectReference.CreateFromFactory(() => document1.GetSemanticModelAsync().GetAwaiter().GetResult());
        var objectReference2 = ObjectReference.CreateFromFactory(() => document2.GetSemanticModelAsync().GetAwaiter().GetResult());
        var objectReference3 = ObjectReference.CreateFromFactory(() => document3.GetSemanticModelAsync().GetAwaiter().GetResult());

        objectReference1.AssertHeld();
        objectReference2.AssertReleased();
        objectReference3.AssertHeld();
    }

    [Fact]
    public async Task TestRemoteWorkspaceCachesNothingIfActiveDocumentNotSynced()
    {
        var code = @"class Test { void Method() { } }";

        using var workspace = TestWorkspace.CreateCSharp(code, composition: s_compositionWithFirstDocumentIsActiveAndVisible);
        using var remoteWorkspace = CreateRemoteWorkspace();

        var solution = workspace.CurrentSolution;

        var project1 = solution.Projects.Single();
        var document1 = project1.Documents.Single();

        // Locally the semantic model will be held
        var objectReference1 = ObjectReference.CreateFromFactory(() => document1.GetSemanticModelAsync().GetAwaiter().GetResult());
        objectReference1.AssertHeld();

        var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution);

        var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
        var syncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, CancellationToken.None);

        // The remote semantic model will not be held as it doesn't know what the active document is yet.
        var objectReference2 = ObjectReference.CreateFromFactory(() => syncedSolution.GetRequiredDocument(document1.Id).GetSemanticModelAsync().GetAwaiter().GetResult());
        objectReference2.AssertReleased();
    }

    [Theory, CombinatorialData]
    public async Task TestRemoteWorkspaceCachesPropertyIfActiveDocumentIsSynced(bool updatePrimaryBranch)
    {
        var code = @"class Test { void Method() { } }";

        using var workspace = TestWorkspace.CreateCSharp(code, composition: s_compositionWithFirstDocumentIsActiveAndVisible);
        using var remoteWorkspace = CreateRemoteWorkspace();

        var solution = workspace.CurrentSolution;

        var project1 = solution.Projects.Single();
        var document1 = project1.Documents.Single();

        // Locally the semantic model will be held
        var objectReference1 = ObjectReference.CreateFromFactory(() => document1.GetSemanticModelAsync().GetAwaiter().GetResult());
        objectReference1.AssertHeld();

        var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution);
        var remoteDocumentTrackingService = (RemoteDocumentTrackingService)remoteWorkspace.Services.GetRequiredService<IDocumentTrackingService>();
        remoteDocumentTrackingService.SetActiveDocument(document1.Id);

        var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
        var syncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch, CancellationToken.None);

        // The remote semantic model will be held as it refers to the active document.
        var objectReference2 = ObjectReference.CreateFromFactory(() => syncedSolution.GetRequiredDocument(document1.Id).GetSemanticModelAsync().GetAwaiter().GetResult());
        objectReference2.AssertHeld();
    }

    [Theory, CombinatorialData]
    public async Task ValidateUpdaterInformsRemoteWorkspaceOfActiveDocument(bool updatePrimaryBranch)
    {
        var code = @"class Test { void Method() { } }";

        using var workspace = TestWorkspace.CreateCSharp(code, composition: s_compositionWithFirstDocumentIsActiveAndVisible);
        using var remoteWorkspace = CreateRemoteWorkspace();

        var solution = workspace.CurrentSolution;

        var project1 = solution.Projects.Single();
        var document1 = project1.Documents.Single();

        // Locally the semantic model will be held
        var objectReference1 = ObjectReference.CreateFromFactory(() => document1.GetSemanticModelAsync().GetAwaiter().GetResult());
        objectReference1.AssertHeld();

        // By creating a checksum updater, we should notify the remote workspace of the active document.
        var listenerProvider = workspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        var checksumUpdater = new SolutionChecksumUpdater(workspace, listenerProvider, CancellationToken.None);

        var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution);

        var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
        var syncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch, CancellationToken.None);

        var waiter = listenerProvider.GetWaiter(FeatureAttribute.SolutionChecksumUpdater);
        await waiter.ExpeditedWaitAsync();

        // The remote semantic model will be held as it refers to the active document.
        var objectReference2 = ObjectReference.CreateFromFactory(() => syncedSolution.GetRequiredDocument(document1.Id).GetSemanticModelAsync().GetAwaiter().GetResult());
        objectReference2.AssertHeld();
    }

    [Theory, CombinatorialData]
    public async Task ValidateUpdaterInformsRemoteWorkspaceOfActiveDocument_EvenAcrossActiveDocumentChanges(bool updatePrimaryBranch)
    {
        using var workspace = TestWorkspace.Create("""
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="File1.cs">
                        class Program1
                        {
                        }
                    </Document>
                    <Document FilePath="File2.cs">
                        class Program2
                        {
                        }
                    </Document>
                </Project>
            </Workspace>
            """, composition: s_composition.AddParts(typeof(TestDocumentTrackingService)));
        using var remoteWorkspace = CreateRemoteWorkspace();

        var solution = workspace.CurrentSolution;

        var project1 = solution.Projects.Single();
        var document1 = project1.Documents.First();
        var document2 = project1.Documents.Last();

        // By creating a checksum updater, we should notify the remote workspace of the active document. Have it
        // initially be set to the first document.
        var documentTrackingService = (TestDocumentTrackingService)workspace.Services.GetRequiredService<IDocumentTrackingService>();
        documentTrackingService.SetActiveDocument(document1.Id);

        // Locally the semantic model for the first document will be held, but the second will not.
        var objectReference1_step1 = ObjectReference.CreateFromFactory(() => document1.GetSemanticModelAsync().GetAwaiter().GetResult());
        var objectReference2_step1 = ObjectReference.CreateFromFactory(() => document2.GetSemanticModelAsync().GetAwaiter().GetResult());
        objectReference1_step1.AssertHeld();
        objectReference2_step1.AssertReleased();

        var listenerProvider = workspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        var checksumUpdater = new SolutionChecksumUpdater(workspace, listenerProvider, CancellationToken.None);

        var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution);

        var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);
        var syncedSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch, CancellationToken.None);

        var waiter = listenerProvider.GetWaiter(FeatureAttribute.SolutionChecksumUpdater);
        await waiter.ExpeditedWaitAsync();

        // The remote semantic model should match the local behavior once it has been notified that the first document is active.
        var oopDocumentReference1_step1 = ObjectReference.CreateFromFactory(() => syncedSolution.GetRequiredDocument(document1.Id).GetSemanticModelAsync().GetAwaiter().GetResult());
        var oopDocumentReference2_step1 = ObjectReference.CreateFromFactory(() => syncedSolution.GetRequiredDocument(document2.Id).GetSemanticModelAsync().GetAwaiter().GetResult());
        oopDocumentReference1_step1.AssertHeld();
        oopDocumentReference2_step1.AssertReleased();

        // Now, change the active document to the second document.
        documentTrackingService.SetActiveDocument(document2.Id);

        // And get the semantic models again.  The second document should now be held, and the first released.
        var objectReference1_step2 = ObjectReference.CreateFromFactory(() => document1.GetSemanticModelAsync().GetAwaiter().GetResult());
        var objectReference2_step2 = ObjectReference.CreateFromFactory(() => document2.GetSemanticModelAsync().GetAwaiter().GetResult());

        // The second document should be held.
        objectReference2_step2.AssertHeld();

        // Ensure that the active doc change is sync'ed to oop.
        await waiter.ExpeditedWaitAsync();

        // And get the semantic models again on the oop side.  The second document should now be held, and the first released.
        var oopDocumentReference1_step2 = ObjectReference.CreateFromFactory(() => syncedSolution.GetRequiredDocument(document1.Id).GetSemanticModelAsync().GetAwaiter().GetResult());
        var oopDocumentReference2_step2 = ObjectReference.CreateFromFactory(() => syncedSolution.GetRequiredDocument(document2.Id).GetSemanticModelAsync().GetAwaiter().GetResult());

        // The second document on oop should now be held.
        oopDocumentReference2_step2.AssertHeld();
    }

    private static async Task VerifySolutionUpdate(string code, Func<Solution, Solution> newSolutionGetter)
    {
        using var workspace = TestWorkspace.CreateCSharp(code);
        await VerifySolutionUpdate(workspace, newSolutionGetter);
    }

#nullable enable
    private static Task VerifySolutionUpdate(
        TestWorkspace workspace,
        Func<Solution, Solution> newSolutionGetter,
        Action<Solution>? oldSolutionValidator = null,
        Action<Solution>? newSolutionValidator = null)
        => VerifySolutionUpdate(workspace, newSolutionGetter, oldSolutionValidator, oldSolutionValidator, newSolutionValidator);

    private static async Task VerifySolutionUpdate(
        TestWorkspace workspace,
        Func<Solution, Solution> newSolutionGetter,
        Action<Solution>? oldSolutionValidator,
        Action<Solution>? oldRecoveredSolutionValidator,
        Action<Solution>? newRecoveredSolutionValidator)
    {
        var solution = workspace.CurrentSolution;
        oldSolutionValidator?.Invoke(solution);

        var map = new Dictionary<Checksum, object>();

        using var remoteWorkspace = CreateRemoteWorkspace();
        var assetProvider = await GetAssetProviderAsync(workspace, remoteWorkspace, solution, map);
        var solutionChecksum = await solution.CompilationState.GetChecksumAsync(CancellationToken.None);

        // update primary workspace
        await remoteWorkspace.UpdatePrimaryBranchSolutionAsync(assetProvider, solutionChecksum, CancellationToken.None);
        var recoveredSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, solutionChecksum, updatePrimaryBranch: false, CancellationToken.None);
        oldRecoveredSolutionValidator?.Invoke(recoveredSolution);

        Assert.Equal(WorkspaceKind.RemoteWorkspace, recoveredSolution.WorkspaceKind);
        Assert.Equal(solutionChecksum, await recoveredSolution.CompilationState.GetChecksumAsync(CancellationToken.None));

        // get new solution
        var newSolution = newSolutionGetter(solution);
        var newSolutionChecksum = await newSolution.CompilationState.GetChecksumAsync(CancellationToken.None);
        await newSolution.AppendAssetMapAsync(map, CancellationToken.None);

        // get solution without updating primary workspace
        var recoveredNewSolution = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, newSolutionChecksum, updatePrimaryBranch: false, CancellationToken.None);

        Assert.Equal(newSolutionChecksum, await recoveredNewSolution.CompilationState.GetChecksumAsync(CancellationToken.None));

        // do same once updating primary workspace
        await remoteWorkspace.UpdatePrimaryBranchSolutionAsync(assetProvider, newSolutionChecksum, CancellationToken.None);
        var third = await remoteWorkspace.GetTestAccessor().GetSolutionAsync(assetProvider, newSolutionChecksum, updatePrimaryBranch: false, CancellationToken.None);

        Assert.Equal(newSolutionChecksum, await third.CompilationState.GetChecksumAsync(CancellationToken.None));

        newRecoveredSolutionValidator?.Invoke(recoveredNewSolution);
    }

    private static async Task<AssetProvider> GetAssetProviderAsync(Workspace workspace, RemoteWorkspace remoteWorkspace, Solution solution, Dictionary<Checksum, object>? map = null)
    {
        // make sure checksum is calculated
        await solution.CompilationState.GetChecksumAsync(CancellationToken.None);

        map ??= [];
        await solution.AppendAssetMapAsync(map, CancellationToken.None);

        var sessionId = Checksum.Create(ImmutableArray.CreateRange(Guid.NewGuid().ToByteArray()));
        var storage = new SolutionAssetCache();
        var assetSource = new SimpleAssetSource(workspace.Services.GetRequiredService<ISerializerService>(), map);

        return new AssetProvider(sessionId, storage, assetSource, remoteWorkspace.Services.SolutionServices);
    }
}
