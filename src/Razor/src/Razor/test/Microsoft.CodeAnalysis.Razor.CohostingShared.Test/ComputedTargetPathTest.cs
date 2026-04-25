// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudioCode.RazorExtension.Test.Endpoints.Shared;

public class ComputedTargetPathTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    // What the source generator would produce for TestProjectData.SomeProjectPath
    private static readonly string s_hintNamePrefix = PlatformInformation.IsWindows
        ? "c_/users/example/src/SomeProject"
        : "_home/example/SomeProject";

    [Fact]
    public async Task DoubleSlashInUri()
    {
        // Directly from a real VS Code agent session: https://github.com/dotnet/razor/issues/12595
        var uriString = "chat-editing-snapshot-text-model:/d:/HashR/src/HashR/Pages/LibraryUsage.razor?%7B%22session%22:%7B%22$mid%22:1,%22external%22:%22vscode-chat-session://local/MzczZWU3N2YtZTBmOS00ZWRhLThmYTktZWI0MjM3ZDE2NTIw%22,%22path%22:%22/MzczZWU3N2YtZTBmOS00ZWRhLThmYTktZWI0MjM3ZDE2NTIw%22,%22scheme%22:%22vscode-chat-session%22,%22authority%22:%22local%22%7D,%22requestId%22:%22request_8e668a63-5ac8-43fe-ad18-65e38621cce4%22,%22undoStop%22:%22__epoch_9007199254740991%22%7D.";

        var uri = new Uri(uriString);
        // This calls the same method that Roslyn calls when creating a misc file for unknown documents
        var documentFilePath = RazorUri.GetDocumentFilePathFromUri(uri);

        var builder = new RazorProjectBuilder
        {
            ProjectFilePath = null,
            GenerateGlobalConfigFile = false,
            GenerateAdditionalDocumentMetadata = false,
            GenerateMSBuildProjectDirectory = false
        };

        var id = builder.AddAdditionalDocument(documentFilePath, SourceText.From("<div></div>"));

        var solution = LocalWorkspace.CurrentSolution;
        solution = builder.Build(solution);

        var document = solution.GetAdditionalDocument(id).AssumeNotNull();

        _ = await document.Project.GetCompilationAsync(DisposalToken);

        var generatedDocument = await document.Project.TryGetSourceGeneratedDocumentForRazorDocumentAsync(document, DisposalToken);
        Assert.NotNull(generatedDocument);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task SingleDocument(bool projectPath, bool generateConfigFile)
    {
        var builder = new RazorProjectBuilder
        {
            ProjectFilePath = projectPath ? TestProjectData.SomeProject.FilePath : null,
            GenerateGlobalConfigFile = generateConfigFile,
            GenerateAdditionalDocumentMetadata = false,
            GenerateMSBuildProjectDirectory = false
        };

        var id = builder.AddAdditionalDocument(FilePath("File1.razor"), SourceText.From(""));

        var solution = LocalWorkspace.CurrentSolution;
        solution = builder.Build(solution);

        var document = solution.GetAdditionalDocument(id).AssumeNotNull();

        _ = await document.Project.GetCompilationAsync(DisposalToken);

        var generatedDocument = await document.Project.TryGetSourceGeneratedDocumentForRazorDocumentAsync(document, DisposalToken);
        Assert.NotNull(generatedDocument);
        Assert.Equal($"{s_hintNamePrefix}/File1_razor.g.cs", generatedDocument.HintName);
    }

    [Theory]
    [CombinatorialData]
    public async Task TwoDocumentsWithTheSameBaseFileName(bool generateTargetPath)
    {
        // This test just proves the "correct" behaviour, with the Razor SDK
        var builder = new RazorProjectBuilder
        {
            ProjectFilePath = TestProjectData.SomeProject.FilePath,
            GenerateAdditionalDocumentMetadata = generateTargetPath
        };

        var doc1Id = builder.AddAdditionalDocument(FilePath(@"Pages\Index.razor"), SourceText.From(""));
        var doc2Id = builder.AddAdditionalDocument(FilePath(@"Components\Index.razor"), SourceText.From(""));

        var solution = LocalWorkspace.CurrentSolution;
        solution = builder.Build(solution);

        var doc1 = solution.GetAdditionalDocument(doc1Id).AssumeNotNull();
        var doc2 = solution.GetAdditionalDocument(doc2Id).AssumeNotNull();

        var generatedDocument = await doc1.Project.TryGetSourceGeneratedDocumentForRazorDocumentAsync(doc1, DisposalToken);
        Assert.NotNull(generatedDocument);
        Assert.Equal($"Pages/Index_razor.g.cs", generatedDocument.HintName);

        generatedDocument = await doc2.Project.TryGetSourceGeneratedDocumentForRazorDocumentAsync(doc2, DisposalToken);
        Assert.NotNull(generatedDocument);
        Assert.Equal($"Components/Index_razor.g.cs", generatedDocument.HintName);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task TwoDocumentsWithTheSameBaseFileName_FullPathHintName(bool projectPath, bool generateConfigFile)
    {
        var builder = new RazorProjectBuilder
        {
            ProjectFilePath = projectPath ? TestProjectData.SomeProject.FilePath : null,
            GenerateGlobalConfigFile = generateConfigFile,
            GenerateAdditionalDocumentMetadata = false,
            GenerateMSBuildProjectDirectory = false
        };

        var doc1Id = builder.AddAdditionalDocument(FilePath(@"Pages\Index.razor"), SourceText.From(""));
        var doc2Id = builder.AddAdditionalDocument(FilePath(@"Components\Index.razor"), SourceText.From(""));

        var solution = LocalWorkspace.CurrentSolution;
        solution = builder.Build(solution);

        var doc1 = solution.GetAdditionalDocument(doc1Id).AssumeNotNull();
        var doc2 = solution.GetAdditionalDocument(doc2Id).AssumeNotNull();

        var generatedDocument = await doc1.Project.TryGetSourceGeneratedDocumentForRazorDocumentAsync(doc1, DisposalToken);
        Assert.NotNull(generatedDocument);
        Assert.Equal($"{s_hintNamePrefix}/Pages/Index_razor.g.cs", generatedDocument.HintName);

        generatedDocument = await doc2.Project.TryGetSourceGeneratedDocumentForRazorDocumentAsync(doc2, DisposalToken);
        Assert.NotNull(generatedDocument);
        Assert.Equal($"{s_hintNamePrefix}/Components/Index_razor.g.cs", generatedDocument.HintName);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task TwoDocumentsWithTheSameBaseHintName(bool projectPath, bool generateConfigFile)
    {
        var builder = new RazorProjectBuilder
        {
            ProjectFilePath = projectPath ? TestProjectData.SomeProject.FilePath : null,
            GenerateGlobalConfigFile = generateConfigFile,
            GenerateAdditionalDocumentMetadata = false,
            GenerateMSBuildProjectDirectory = false
        };

        var doc1Id = builder.AddAdditionalDocument(FilePath(@"Pages\Index.razor"), SourceText.From(""));
        var doc2Id = builder.AddAdditionalDocument(FilePath(@"Pages_Index.razor"), SourceText.From(""));

        var solution = LocalWorkspace.CurrentSolution;
        solution = builder.Build(solution);

        var doc1 = solution.GetAdditionalDocument(doc1Id).AssumeNotNull();
        var doc2 = solution.GetAdditionalDocument(doc2Id).AssumeNotNull();

        var generatedDocument = await doc1.Project.TryGetSourceGeneratedDocumentForRazorDocumentAsync(doc1, DisposalToken);
        Assert.NotNull(generatedDocument);
        Assert.Equal($"{s_hintNamePrefix}/Pages/Index_razor.g.cs", generatedDocument.HintName);

        generatedDocument = await doc2.Project.TryGetSourceGeneratedDocumentForRazorDocumentAsync(doc2, DisposalToken);
        Assert.NotNull(generatedDocument);
        Assert.Equal($"{s_hintNamePrefix}/Pages_Index_razor.g.cs", generatedDocument.HintName);
    }
}
