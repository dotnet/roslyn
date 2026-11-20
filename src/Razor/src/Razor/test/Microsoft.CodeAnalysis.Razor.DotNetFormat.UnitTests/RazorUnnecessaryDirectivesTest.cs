// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.DotNetFormat;

public class RazorUnnecessaryDirectivesTest
{
    private static readonly string s_componentFilePath = TestProjectData.SomeProjectComponentFile1.FilePath;
    private static readonly string s_importsFilePath = TestProjectData.SomeProjectComponentImportFile1.FilePath;

    private const string WidgetComponent = """
        namespace Other.Components;

        public class Widget : Microsoft.AspNetCore.Components.ComponentBase
        {
        }
        """;

    [Fact]
    public Task GetUnnecessaryUsingDirectiveSpansAsync_UnusedNamespace_IsReported()
        => VerifyReportedAsync(
            """
            @using System.Text
            <div></div>
            """,
            expected: ["@using System.Text"]);

    [Fact]
    public Task GetUnnecessaryUsingDirectiveSpansAsync_NamespaceUsedByCode_IsNotReported()
        => VerifyReportedAsync(
            """
            @using System.Text
            @code {
                StringBuilder Builder = new();
            }
            """,
            expected: []);

    [Fact]
    public Task GetUnnecessaryUsingDirectiveSpansAsync_NamespaceUsedByMarkup_IsNotReported()
        => VerifyReportedAsync(
            """
            @using System.Text
            <div>@(new StringBuilder().ToString())</div>
            """,
            expected: []);

    [Fact]
    public Task GetUnnecessaryUsingDirectiveSpansAsync_NamespaceNeededForComponent_IsNotReported()
        => VerifyReportedAsync(
            """
            @using Other.Components
            <Widget />
            """,
            expected: [],
            csharpDocument: WidgetComponent);

    [Fact]
    public Task GetUnnecessaryUsingDirectiveSpansAsync_NamespaceOfUnusedComponent_IsReported()
        => VerifyReportedAsync(
            """
            @using Other.Components
            <div></div>
            """,
            expected: ["@using Other.Components"],
            csharpDocument: WidgetComponent);

    [Fact]
    public Task GetUnnecessaryUsingDirectiveSpansAsync_RepeatOfImportsDirective_IsReportedWhenCodeUsesIt()
        => VerifyReportedAsync(
            """
            @using System.Text
            @code {
                StringBuilder Builder = new();
            }
            """,
            expected: ["@using System.Text"],
            importsText: "@using System.Text");

    [Fact]
    public Task GetUnnecessaryUsingDirectiveSpansAsync_RepeatOfImportsDirective_IsReportedWhenNeededForComponent()
        => VerifyReportedAsync(
            """
            @using Other.Components
            <Widget />
            """,
            expected: ["@using Other.Components"],
            importsText: "@using Other.Components",
            csharpDocument: WidgetComponent);

    [Fact]
    public Task GetUnnecessaryUsingDirectiveSpansAsync_DifferentImportsDirective_IsNotTreatedAsRepeat()
        => VerifyReportedAsync(
            """
            @using System.Text
            @code {
                StringBuilder Builder = new();
            }
            """,
            expected: [],
            importsText: "@using System.IO");

    [Fact]
    public async Task GetUnnecessaryUsingDirectiveSpansAsync_ImportsFile_ReportsNothing()
    {
        using var workspace = new AdhocWorkspace();
        var (project, _, importsId) = CreateProject(
            workspace,
            "<div></div>",
            importsText: "@using System.Text");

        var spans = await RazorUnnecessaryDirectives.GetUnnecessaryUsingDirectiveSpansAsync(project, importsId!, CancellationToken.None);

        Assert.Empty(spans);
    }

    [Fact]
    public async Task GetUnnecessaryUsingDirectiveSpansAsync_WithoutDocumentationComments_IsReported()
    {
        using var workspace = new AdhocWorkspace();
        var (project, documentId, _) = CreateProject(
            workspace,
            """
            @using System.Text
            <div></div>
            """);
        project = project.WithParseOptions(((CSharpParseOptions)project.ParseOptions!).WithDocumentationMode(DocumentationMode.None));

        var spans = await RazorUnnecessaryDirectives.GetUnnecessaryUsingDirectiveSpansAsync(project, documentId, CancellationToken.None);

        var text = await GetTextAsync(project, documentId);
        Assert.Equal(["@using System.Text"], spans.Select(text.ToString));
    }

    [Fact]
    public async Task GetUnnecessaryUsingDirectiveSpansAsync_NonComponentDocument_ReportsNothing()
    {
        using var workspace = new AdhocWorkspace();
        var (project, documentId, _) = CreateProject(
            workspace,
            "@using System.Text",
            documentFilePath: Path.ChangeExtension(s_componentFilePath, ".cshtml"));

        var spans = await RazorUnnecessaryDirectives.GetUnnecessaryUsingDirectiveSpansAsync(project, documentId, CancellationToken.None);

        Assert.Empty(spans);
    }

    [Fact]
    public async Task GetUnnecessaryUsingDirectiveSpansAsync_WithoutRazorGenerator_ReportsNothing()
    {
        using var workspace = new AdhocWorkspace();
        var (project, documentId, _) = CreateProject(
            workspace,
            "@using System.Text",
            referenceRazorSourceGenerator: false);

        var spans = await RazorUnnecessaryDirectives.GetUnnecessaryUsingDirectiveSpansAsync(project, documentId, CancellationToken.None);

        Assert.Empty(spans);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public async Task RemoveUnnecessaryUsingDirectivesAsync_RemovesLinesAndKeepsLineEndings(string newLine)
    {
        var input = WithLineEndings(
            """
            @using System.Text
            @using System.Collections.Generic
            @using System.IO
            <div></div>
            @code {
                List<int> Values = new();
            }

            """,
            newLine);
        var expected = WithLineEndings(
            """
            @using System.Collections.Generic
            <div></div>
            @code {
                List<int> Values = new();
            }

            """,
            newLine);

        using var workspace = new AdhocWorkspace();
        var (project, documentId, _) = CreateProject(workspace, input);

        var result = await RazorUnnecessaryDirectives.RemoveUnnecessaryUsingDirectivesAsync(project, documentId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expected, result.ToString());
    }

    [Fact]
    public async Task RemoveUnnecessaryUsingDirectivesAsync_NothingToRemove_ReturnsNull()
    {
        using var workspace = new AdhocWorkspace();
        var (project, documentId, _) = CreateProject(
            workspace,
            """
            @using System.Text
            @code {
                StringBuilder Builder = new();
            }
            """);

        var result = await RazorUnnecessaryDirectives.RemoveUnnecessaryUsingDirectivesAsync(project, documentId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUnnecessaryUsingDirectiveSpansAsync_RejectsNonAdditionalDocument()
    {
        using var workspace = new AdhocWorkspace();
        var (project, _, _) = CreateProject(workspace, "<div></div>");
        var documentId = DocumentId.CreateNewId(project.Id);

        await Assert.ThrowsAsync<ArgumentException>(
            () => RazorUnnecessaryDirectives.GetUnnecessaryUsingDirectiveSpansAsync(project, documentId, CancellationToken.None));
    }

    private static async Task VerifyReportedAsync(
        string componentText,
        string[] expected,
        string? importsText = null,
        string? csharpDocument = null)
    {
        using var workspace = new AdhocWorkspace();
        var (project, documentId, _) = CreateProject(workspace, componentText, importsText: importsText, csharpDocument: csharpDocument);

        var spans = await RazorUnnecessaryDirectives.GetUnnecessaryUsingDirectiveSpansAsync(project, documentId, CancellationToken.None);

        var text = await GetTextAsync(project, documentId);
        Assert.Equal(expected, spans.Select(text.ToString));
    }

    private static async Task<SourceText> GetTextAsync(Project project, DocumentId documentId)
    {
        var document = project.GetAdditionalDocument(documentId)
            ?? throw new InvalidOperationException("The test project did not contain the Razor document.");
        return await document.GetTextAsync(CancellationToken.None);
    }

    private static string WithLineEndings(string text, string newLine)
        => text.ReplaceLineEndings("\n").Replace("\n", newLine, StringComparison.Ordinal);

    private static (Project Project, DocumentId DocumentId, DocumentId? ImportsId) CreateProject(
        AdhocWorkspace workspace,
        string text,
        string? documentFilePath = null,
        string? importsText = null,
        string? csharpDocument = null,
        bool referenceRazorSourceGenerator = true)
    {
        var builder = new RazorProjectBuilder
        {
            ProjectFilePath = TestProjectData.SomeProject.FilePath,
            ReferenceRazorSourceGenerator = referenceRazorSourceGenerator,
        };

        builder.AddReferences(AspNet80.ReferenceInfos.All.Select(static referenceInfo => referenceInfo.Reference));
        var documentId = builder.AddAdditionalDocument(documentFilePath ?? s_componentFilePath, SourceText.From(text));
        var importsId = importsText is null
            ? null
            : builder.AddAdditionalDocument(s_importsFilePath, SourceText.From(importsText));

        if (csharpDocument is not null)
        {
            builder.AddDocument(Path.Combine(TestProjectData.SomeProjectPath, "Widget.cs"), SourceText.From(csharpDocument));
        }

        var solution = builder.Build(workspace.CurrentSolution);
        return (
            solution.GetProject(builder.Id)
                ?? throw new InvalidOperationException("The test solution did not contain the Razor project."),
            documentId,
            importsId);
    }
}
