// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.DotNetFormat;

public partial class RazorFormatterTest
{
    [Fact]
    public Task TryFormatAsync_FormatsRazorAndCSharpWithoutFormattingHtml()
        => VerifyFormattingAsync(
            input: """
                <div><span>HTML remains on one line</span></div>
                @code {
                class C
                {
                void M()
                {
                }
                }
                }
                """,
            expected: """
                <div><span>HTML remains on one line</span></div>
                @code {
                    class C
                    {
                        void M()
                        {
                        }
                    }
                }
                """);

    [Fact]
    public Task TryFormatAsync_PreservesScriptBlock()
        => VerifyFormattingAsync(
            input: """
                <script>
                function logValue( ){
                const value=1;
                  console.log( value );
                }
                </script>
                @code {
                int Value=1;
                }
                """,
            expected: """
                <script>
                function logValue( ){
                const value=1;
                  console.log( value );
                }
                </script>
                @code {
                    int Value = 1;
                }
                """);

    [Fact]
    public Task TryFormatAsync_PreservesStyleBlock()
        => VerifyFormattingAsync(
            input: """
                <style>
                .example{
                color:red;
                  padding:0  4px;
                }
                </style>
                @code {
                int Value=1;
                }
                """,
            expected: """
                <style>
                .example{
                color:red;
                  padding:0  4px;
                }
                </style>
                @code {
                    int Value = 1;
                }
                """);

    [Fact]
    public Task TryFormatAsync_IndentsNestedHtml()
        => VerifyFormattingAsync(
            input: """
                <div>
                <section>
                <span>Content</span>
                </section>
                </div>
                """,
            expected: """
                <div>
                    <section>
                        <span>Content</span>
                    </section>
                </div>
                """);

    [Fact]
    public Task TryFormatAsync_IndentsHtmlInsideRazorBlock()
        => VerifyFormattingAsync(
            input: """
                @if (true)
                {
                <div>
                <span>Content</span>
                </div>
                }
                """,
            expected: """
                @if (true)
                {
                    <div>
                        <span>Content</span>
                    </div>
                }
                """);

    [Fact]
    public Task TryFormatAsync_IndentsRazorBlockInsideHtml()
        => VerifyFormattingAsync(
            input: """
                <div>
                <section>
                @if (true)
                {
                <span>@DateTime.Now</span>
                }
                </section>
                </div>
                """,
            expected: """
                <div>
                    <section>
                        @if (true)
                        {
                            <span>@DateTime.Now</span>
                        }
                    </section>
                </div>
                """);

    [Fact]
    public Task TryFormatAsync_FormatsCshtml()
        => VerifyFormattingAsync(
            input: """
                @functions {
                class C
                {
                void M()
                {
                }
                }
                }
                """,
            expected: """
                @functions {
                    class C
                    {
                        void M()
                        {
                        }
                    }
                }
                """,
            documentFilePath: TestProjectData.SomeProjectCshtmlComponentFile5.FilePath);

    [Fact]
    public Task TryFormatAsync_UsesNestedEditorConfig()
    {
        var projectDirectory = Path.GetDirectoryName(TestProjectData.SomeProject.FilePath)!;
        var nestedDirectory = Path.Combine(projectDirectory, "Nested");

        return VerifyFormattingAsync(
            input: """
                @code {
                class C
                {
                void M()
                {
                    if (true)
                    {
                    }
                }
                }
                }
                """,
            expected: """
                @code {
                    class C {
                        void M() {
                            if(true) {
                            }
                        }
                    }
                }
                """,
            documentFilePath: Path.Combine(nestedDirectory, "File1.razor"),
            analyzerConfigs:
            [
                (Path.Combine(projectDirectory, ".editorconfig"), """
                    root = true

                    [*.razor]
                    csharp_new_line_before_open_brace = none
                    """),
                (Path.Combine(nestedDirectory, ".editorconfig"), """
                    [*.razor]
                    csharp_space_after_keywords_in_control_flow_statements = false
                    """),
            ]);
    }

    [Theory]
    [InlineData("\"\"\"", "\"\"\"")]
    [InlineData("\"\"\"", "\"\"\"u8")]
    [InlineData("$\"\"\"", "\"\"\"")]
    [InlineData("@\"", "\"")]
    [InlineData("$@\"", "\"")]
    public Task TryFormatAsync_PreservesMultilineStringLiteralContents(string prefix, string suffix)
        => VerifyFormattingAsync(
            input: """
                @code {
                void M()
                {
                var text = PREFIX
                    first
                        second
                    SUFFIX;
                }
                }
                """.Replace("PREFIX", prefix).Replace("SUFFIX", suffix),
            expected: """
                @code {
                    void M()
                    {
                        var text = PREFIX
                    first
                        second
                    SUFFIX;
                    }
                }
                """.Replace("PREFIX", prefix).Replace("SUFFIX", suffix));

    private static async Task VerifyFormattingAsync(
        string input,
        string expected,
        string? documentFilePath = null,
        (string FilePath, string Contents)[]? analyzerConfigs = null)
    {
        using var workspace = new AdhocWorkspace();
        var (project, documentId) = CreateProject(
            workspace,
            input,
            documentFilePath,
            analyzerConfigs: analyzerConfigs);

        var formatted = await RazorFormatter.TryFormatAsync(project, documentId, CancellationToken.None);

        Assert.NotNull(formatted);
        Assert.Equal(expected, formatted.ToString());
    }

    private static (Project Project, DocumentId DocumentId) CreateProject(
        AdhocWorkspace workspace,
        string text,
        string? documentFilePath = null,
        bool referenceRazorSourceGenerator = true,
        (string FilePath, string Contents)[]? analyzerConfigs = null)
    {
        var builder = new RazorProjectBuilder
        {
            ProjectFilePath = TestProjectData.SomeProject.FilePath,
            ReferenceRazorSourceGenerator = referenceRazorSourceGenerator,
        };

        builder.AddReferences(AspNet80.ReferenceInfos.All.Select(static referenceInfo => referenceInfo.Reference));
        var documentId = builder.AddAdditionalDocument(
            documentFilePath ?? TestProjectData.SomeProjectComponentFile1.FilePath,
            SourceText.From(text));

        if (analyzerConfigs is not null)
        {
            foreach (var (filePath, contents) in analyzerConfigs)
            {
                builder.AddAnalyzerConfigDocument(filePath, SourceText.From(contents));
            }
        }

        var solution = builder.Build(workspace.CurrentSolution);
        return (
            solution.GetProject(builder.Id)
                ?? throw new InvalidOperationException("The test solution did not contain the Razor project."),
            documentId);
    }
}
