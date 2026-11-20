// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators;

public sealed class RazorSourceGeneratorVirtualDocumentTests : RazorSourceGeneratorTestsBase
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ComponentUri_GeneratesValidComponent(bool encoded, bool targetPath)
    {
        const string uri = """git:/c:/repo/MainLayout.razor?{"path":"c:\\repo\\MainLayout.razor","ref":"~"}""";
        var filePath = encoded ? new Uri(uri).AbsoluteUri : uri;
        var (driver, compilation) = await CreateDriverAsync(filePath, """
            @using System
            @inject object Service

            <p>@Value</p>

            @code
            {
                private string Value => Guid.Empty.ToString();
            }
            """, targetPath);

        var result = RunGenerator(compilation, ref driver);
        var codeDocument = GetCodeDocument(result, filePath);
        Assert.Equal(RazorFileKind.Component, codeDocument.FileKind);
        Assert.EndsWith("MainLayout.razor", codeDocument.Source.RelativePath);
        Assert.Contains("partial class MainLayout", string.Join(Environment.NewLine, result.GeneratedSources.Select(source => source.SourceText.ToString())));
        Assert.Equal(filePath, codeDocument.Source.FilePath);
        var sourceMappings = codeDocument.GetRequiredCSharpDocument(declarationDocument: false).SourceMappingsSortedByGenerated;
        Assert.NotEmpty(sourceMappings);
        Assert.All(sourceMappings,
            mapping => Assert.Equal(filePath, mapping.OriginalSpan.FilePath));
        Assert.All(result.GeneratedSources, source => Assert.Empty(source.SyntaxTree.GetDiagnostics()));
        Assert.Contains(result.GeneratedSources, source => source.SourceText.ToString().StartsWith(
            $"#pragma checksum \"{filePath.Replace("\"", "%22")}\"", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("""custom:/repo/MainLayout.razor?{"ref":"~"}""")]
    [InlineData("""untitled:MainLayout.razor?{"ref":"~"}""")]
    [InlineData("git:/c:/repo/MainLayout.razor#revision")]
    [InlineData("git:/c:/repo/MainLayout%2Erazor?revision=HEAD")]
    public async Task OtherUris_GenerateValidComponents(string filePath)
    {
        var (driver, compilation) = await CreateDriverAsync(filePath, "<p>@(1 + 1)</p>");

        var result = RunGenerator(compilation, ref driver);
        var codeDocument = GetCodeDocument(result, filePath);
        Assert.Equal(RazorFileKind.Component, codeDocument.FileKind);
        Assert.EndsWith("MainLayout.razor", codeDocument.Source.RelativePath);
        Assert.Contains("partial class MainLayout", string.Join(Environment.NewLine, result.GeneratedSources.Select(source => source.SourceText.ToString())));
        Assert.Equal(filePath, codeDocument.Source.FilePath);
        Assert.All(result.GeneratedSources, source => Assert.Empty(source.SyntaxTree.GetDiagnostics()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ViewUri_GeneratesValidView(bool encoded)
    {
        const string uri = """git:/c:/repo/Index.cshtml?{"path":"c:\\repo\\Index.cshtml","ref":"~"}""";
        var filePath = encoded ? new Uri(uri).AbsoluteUri : uri;
        var (driver, compilation) = await CreateDriverAsync(filePath, """
            @using System
            <p>@DateTime.Now.Year</p>
            """);

        var result = RunGenerator(compilation, ref driver);
        var codeDocument = GetCodeDocument(result, filePath);
        Assert.Equal(RazorFileKind.Legacy, codeDocument.FileKind);
        Assert.Equal(filePath, codeDocument.Source.FilePath);
        Assert.All(result.GeneratedSources, source => Assert.Empty(source.SyntaxTree.GetDiagnostics()));
    }

    [Fact]
    public async Task ComponentUri_PreservesGenuineCSharpErrors()
    {
        const string uri = """git:/c:/repo/MainLayout.razor?{"ref":"~"}""";
        var (driver, compilation) = await CreateDriverAsync(uri, """
            @code
            {
                private string Value => "Hello"
            }
            """);

        RunGenerator(compilation, ref driver, out _, output =>
            Assert.Contains(output.GetDiagnostics(), diagnostic =>
                diagnostic.Id == "CS1002" &&
                new Uri(diagnostic.Location.GetMappedLineSpan().Path).AbsoluteUri == new Uri(uri).AbsoluteUri));
    }

    [Fact]
    public async Task DifferentRevisions_PreserveDistinctGeneratedSourceIdentities()
    {
        const string firstUri = """git:/c:/repo/MainLayout.razor?{"ref":"HEAD"}""";
        const string secondUri = """git:/c:/repo/MainLayout.razor?{"ref":"~"}""";
        var project = CreateTestProject(new()
        {
            [firstUri] = """
                @namespace FirstRevision
                <p>First</p>
                """,
            [secondUri] = """
                @namespace SecondRevision
                <p>Second</p>
                """,
        });
        var (driver, _, options) = await GetDriverWithAdditionalTextAndProviderAsync(project, hostOutputs: true);
        options.AdditionalTextOptions.Clear();
        driver = driver.WithUpdatedAnalyzerConfigOptions(options);
        var compilation = await project.GetCompilationAsync();
        Assert.NotNull(compilation);

        var result = RunGenerator(compilation, ref driver);
        var firstDocument = GetCodeDocument(result, firstUri);
        var secondDocument = GetCodeDocument(result, secondUri);

        Assert.NotSame(firstDocument, secondDocument);
        Assert.Equal(firstUri, firstDocument.Source.FilePath);
        Assert.Equal(secondUri, secondDocument.Source.FilePath);
        Assert.Equal(firstDocument.Source.RelativePath, secondDocument.Source.RelativePath);
        Assert.Equal(result.GeneratedSources.Length, result.GeneratedSources.Select(source => source.HintName).Distinct().Count());
    }

    private static async Task<(GeneratorDriver, Compilation)> CreateDriverAsync(string filePath, string content, bool targetPath = false)
    {
        var project = CreateTestProject(new() { [filePath] = content });
        var (driver, _, options) = await GetDriverWithAdditionalTextAndProviderAsync(project, hostOutputs: true);
        if (!targetPath)
        {
            options.AdditionalTextOptions.Clear();
        }

        options.TestGlobalOptions["build_property.GenerateRazorMetadataSourceChecksumAttributes"] = "true";
        driver = driver.WithUpdatedAnalyzerConfigOptions(options);
        var compilation = await project.GetCompilationAsync();
        Assert.NotNull(compilation);
        return (driver, compilation);
    }

    private static RazorCodeDocument GetCodeDocument(GeneratorRunResult result, string filePath)
    {
#pragma warning disable RSEXPERIMENTAL004 // Host outputs expose the source documents used by cohosting.
        var hostOutput = Assert.IsType<RazorGeneratorResult>(result.HostOutputs[nameof(RazorGeneratorResult)]);
#pragma warning restore RSEXPERIMENTAL004
        var codeDocument = hostOutput.GetCodeDocument(filePath);
        Assert.NotNull(codeDocument);
        return codeDocument;
    }
}
