// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.DotNetFormat;

public partial class RazorFormatterTest
{
    private const string UnformattedCodeBlock = """
        @code {
        class C
        {
        void M()
        {
        }
        }
        }
        """;

    private const string FormattedCodeBlock = """
        @code {
            class C
            {
                void M()
                {
                }
            }
        }
        """;

    [Fact]
    public async Task TryFormatAsync_AfterAdditionalDocumentChanged_UsesUpdatedGeneratorOutput()
    {
        var updatedText = UnformattedCodeBlock.Replace("class C", "class Updated", StringComparison.Ordinal);

        using var workspace = new AdhocWorkspace();
        var (project, documentId) = CreateProject(workspace, UnformattedCodeBlock);

        var initialFormatted = await RazorFormatter.TryFormatAsync(project, documentId, CancellationToken.None);
        var updatedProject = project.Solution
            .WithAdditionalDocumentText(documentId, SourceText.From(updatedText))
            .GetProject(project.Id)
            ?? throw new InvalidOperationException("The updated solution did not contain the test project.");
        var updatedFormatted = await RazorFormatter.TryFormatAsync(updatedProject, documentId, CancellationToken.None);

        Assert.NotNull(initialFormatted);
        Assert.NotNull(updatedFormatted);
        Assert.Equal(FormattedCodeBlock, initialFormatted.ToString());
        Assert.Equal(
            FormattedCodeBlock.Replace("class C", "class Updated", StringComparison.Ordinal),
            updatedFormatted.ToString());
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public async Task TryFormatAsync_PreservesLineEndings(string newLine)
    {
        var input = WithLineEndings(UnformattedCodeBlock, newLine);
        var expected = WithLineEndings(FormattedCodeBlock, newLine);

        using var workspace = new AdhocWorkspace();
        var (project, documentId) = CreateProject(workspace, input);

        var formatted = await RazorFormatter.TryFormatAsync(project, documentId, CancellationToken.None);

        Assert.NotNull(formatted);
        Assert.Equal(expected, formatted.ToString());
    }

    [Fact]
    public async Task TryFormatAsync_ReturnsNullForNonRazorDocument()
    {
        using var workspace = new AdhocWorkspace();
        var filePath = Path.ChangeExtension(TestProjectData.SomeProjectComponentFile1.FilePath, ".txt");
        var (project, documentId) = CreateProject(workspace, UnformattedCodeBlock, filePath);

        var formatted = await RazorFormatter.TryFormatAsync(project, documentId, CancellationToken.None);

        Assert.Null(formatted);
    }

    [Fact]
    public async Task TryFormatAsync_ReturnsNullWhenProjectDoesNotReferenceRazorGenerator()
    {
        using var workspace = new AdhocWorkspace();
        var (project, documentId) = CreateProject(
            workspace,
            UnformattedCodeBlock,
            referenceRazorSourceGenerator: false);

        var formatted = await RazorFormatter.TryFormatAsync(project, documentId, CancellationToken.None);

        Assert.Null(formatted);
    }

    [Fact]
    public async Task TryFormatAsync_ThrowsForIncompatibleRazorGeneratorAssembly()
    {
        using var workspace = new AdhocWorkspace();
        var (project, documentId) = CreateProject(
            workspace,
            UnformattedCodeBlock,
            referenceRazorSourceGenerator: false);
        project = project.Solution
            .WithProjectAnalyzerReferences(project.Id, [new IncompatibleRazorGeneratorReference()])
            .GetProject(project.Id)
            ?? throw new InvalidOperationException("The updated solution did not contain the test project.");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => RazorFormatter.TryFormatAsync(project, documentId, CancellationToken.None));

        Assert.Contains("incompatible Razor source generator", exception.Message, StringComparison.Ordinal);
        Assert.Contains("informational version", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryFormatAsync_DoesNotThrowForMalformedRazor()
    {
        const string input = """
            @foreach (var item in Items)
            {
                <div><div>
            }
            """;

        using var workspace = new AdhocWorkspace();
        var (project, documentId) = CreateProject(workspace, input);

        var formatted = await RazorFormatter.TryFormatAsync(project, documentId, CancellationToken.None);

        Assert.NotNull(formatted);
    }

    [Fact]
    public async Task TryFormatAsync_RejectsNonAdditionalDocument()
    {
        using var workspace = new AdhocWorkspace();
        var (project, _) = CreateProject(workspace, UnformattedCodeBlock);
        var documentId = DocumentId.CreateNewId(project.Id);

        await Assert.ThrowsAsync<ArgumentException>(
            () => RazorFormatter.TryFormatAsync(project, documentId, CancellationToken.None));
    }

    private static string WithLineEndings(string text, string newLine)
        => text.ReplaceLineEndings("\n").Replace("\n", newLine, StringComparison.Ordinal);

    private sealed class IncompatibleRazorGeneratorReference : AnalyzerReference
    {
        private readonly ISourceGenerator _generator = CreateGenerator();

        public override string? FullPath => null;
        public override string Display => nameof(IncompatibleRazorGeneratorReference);
        public override object Id => this;

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
            => [];

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
            => [];

        public override ImmutableArray<ISourceGenerator> GetGeneratorsForAllLanguages()
            => [_generator];

        public override ImmutableArray<ISourceGenerator> GetGenerators(string language)
            => language == LanguageNames.CSharp ? [_generator] : [];

        private static ISourceGenerator CreateGenerator()
        {
            const string typeName = "Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator";
            var type = typeof(RazorFormatterTest).Assembly.GetType(typeName, throwOnError: true)!;
            return (ISourceGenerator)Activator.CreateInstance(type)!;
        }
    }
}
