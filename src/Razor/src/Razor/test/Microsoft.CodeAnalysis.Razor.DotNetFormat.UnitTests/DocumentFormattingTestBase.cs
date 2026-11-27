// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;

// The CLI formatter keeps per-call state in static helpers and expects sequential callers.
[Collection("Razor dotnet format")]
public abstract class DocumentFormattingTestBase(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "The shared tests pass options used only by the cohost runner.")]
    private protected async Task RunFormattingTestAsync(
        TestCode input,
        string htmlFormatted,
        string expected,
        RazorFileKind? fileKind = null,
        bool inGlobalNamespace = false,
        bool codeBlockBraceOnNextLine = false,
        AttributeIndentStyle attributeIndentStyle = AttributeIndentStyle.AlignWithFirst,
        bool ignoreIndentationInScriptOrStyleBlocksWithComplexRazor = true,
        bool insertSpaces = true,
        int tabSize = 4,
        bool allowDiagnostics = false,
        bool debugAssertsEnabled = true,
        bool validateHtmlFormattedMatchesWebTools = true,
        (string fileName, string contents)[]? additionalFiles = null,
        string? documentFilePath = null,
        string? dotNetFormatExpected = null)
    {
        // These control the cohost test harness, not the public CLI formatter.
        Assert.False(input.TryGetNamedSpans(string.Empty, out var spans) && spans.Any(static span => !span.IsEmpty),
            "Range formatting belongs in the cohost-only suite.");

        documentFilePath ??= fileKind == RazorFileKind.Legacy
            ? TestProjectData.SomeProjectFile1.FilePath
            : TestProjectData.SomeProjectComponentFile1.FilePath;

        // In the IDE these are user preferences. A low-priority global config gives the CLI the same defaults,
        // while allowing each test's .editorconfig to override them through the production options path.
        var attributeStyle = attributeIndentStyle switch
        {
            AttributeIndentStyle.AlignWithFirst => "align_with_first",
            AttributeIndentStyle.IndentByOne => "indent_by_one",
            AttributeIndentStyle.IndentByTwo => "indent_by_two",
            _ => throw new ArgumentOutOfRangeException(nameof(attributeIndentStyle)),
        };
        var defaultOptions = $"""
            is_global = true
            global_level = -100
            indent_style = {(insertSpaces ? "space" : "tab")}
            indent_size = {tabSize}
            tab_width = {tabSize}
            razor_code_block_brace_on_next_line = {codeBlockBraceOnNextLine.ToString().ToLowerInvariant()}
            razor_attribute_indent_style = {attributeStyle}
            razor_ignore_indentation_in_script_or_style_blocks_with_complex_razor = {ignoreIndentationInScriptOrStyleBlocksWithComplexRazor.ToString().ToLowerInvariant()}
            """;

        using var workspace = new AdhocWorkspace();
        var (project, documentId) = CreateProject(
            workspace,
            input.Text,
            documentFilePath,
            inGlobalNamespace: inGlobalNamespace,
            additionalFiles: additionalFiles,
            defaultOptions: defaultOptions);

        var formatted = await RazorFormatter.TryFormatAsync(project, documentId, DisposalToken);

        Assert.NotNull(formatted);
        AssertEx.EqualOrDiff(dotNetFormatExpected ?? expected, formatted.ToString());
    }

    protected static string FilePath(string projectRelativeFileName)
        => Path.GetFullPath(Path.Combine(TestProjectData.SomeProjectPath, projectRelativeFileName));

    private protected static (Project Project, DocumentId DocumentId) CreateProject(
        AdhocWorkspace workspace,
        string text,
        string? documentFilePath = null,
        bool referenceRazorSourceGenerator = true,
        bool inGlobalNamespace = false,
        (string fileName, string contents)[]? additionalFiles = null,
        string? defaultOptions = null)
    {
        var builder = new RazorProjectBuilder
        {
            ProjectFilePath = TestProjectData.SomeProject.FilePath,
            ReferenceRazorSourceGenerator = referenceRazorSourceGenerator,
            RootNamespace = inGlobalNamespace ? null : TestProjectData.SomeProject.RootNamespace,
        };

        builder.AddReferences(AspNet80.ReferenceInfos.All.Select(static referenceInfo => referenceInfo.Reference));
        var documentId = builder.AddAdditionalDocument(
            documentFilePath ?? TestProjectData.SomeProjectComponentFile1.FilePath,
            SourceText.From(text));

        // Match the imports used by the cohost project so component and tag helper discovery see the same inputs.
        builder.AddAdditionalDocument(
            TestProjectData.SomeProjectComponentImportFile1.FilePath,
            SourceText.From("""
                @using Microsoft.AspNetCore.Components
                @using Microsoft.AspNetCore.Components.Authorization
                @using Microsoft.AspNetCore.Components.Forms
                @using Microsoft.AspNetCore.Components.Routing
                @using Microsoft.AspNetCore.Components.Web
                """));
        builder.AddAdditionalDocument(
            TestProjectData.SomeProjectImportFile.FilePath,
            SourceText.From("""
                @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
                """));

        if (defaultOptions is not null)
        {
            builder.AddAnalyzerConfigDocument(FilePath("formatting-defaults.globalconfig"), SourceText.From(defaultOptions));
        }

        if (additionalFiles is not null)
        {
            foreach (var (fileName, contents) in additionalFiles)
            {
                var path = Path.IsPathRooted(fileName) ? fileName : FilePath(fileName);
                var sourceText = SourceText.From(contents);
                if (Path.GetFileName(path).Equals(".editorconfig", StringComparison.OrdinalIgnoreCase))
                {
                    builder.AddAnalyzerConfigDocument(path, sourceText);
                }
                else if (Path.GetExtension(path) == ".cs")
                {
                    builder.AddDocument(path, sourceText);
                }
                else
                {
                    builder.AddAdditionalDocument(path, sourceText);
                }
            }
        }

        var solution = builder.Build(workspace.CurrentSolution);
        return (
            solution.GetProject(builder.Id)
                ?? throw new InvalidOperationException("The test solution did not contain the Razor project."),
            documentId);
    }
}
