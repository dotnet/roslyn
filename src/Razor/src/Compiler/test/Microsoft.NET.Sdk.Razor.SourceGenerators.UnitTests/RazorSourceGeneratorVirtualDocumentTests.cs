// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
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

    [Fact]
    public async Task HostOutput_UpdatesAliasBindingWhenGeneratedSourceIsUnchanged()
    {
        var project = CreateTestProject(new()
        {
            ["Index.razor"] = """
                @namespace MyApp
                @using UI = Library
                <div />
                """,
            ["Other.razor"] = """
                @namespace OtherApp
                @using UI = Library
                <div />
                """,
        }, new()
        {
            ["Components.cs"] = """
                namespace Library { public class Button : Microsoft.AspNetCore.Components.ComponentBase { } }
                """,
        });
        var compilation = await project.GetCompilationAsync();
        Assert.NotNull(compilation);
        var (driver, _, _) = await GetDriverWithAdditionalTextAndProviderAsync(
            project,
            configureGlobalOptions: options => options.TestGlobalOptions["build_property.RazorLangVersion"] = "12.0",
            hostOutputs: true,
            trackSteps: true);

        var original = RunGenerator(compilation, ref driver);
        var originalDocument = GetCodeDocument(original, "Index.razor");
        var originalOtherDocument = GetCodeDocument(original, "Other.razor");
        Assert.NotNull(originalDocument.GetRequiredTagHelperContext().GetBinder().GetBinding("UI.Button", [], parentTagName: null, parentIsTagHelper: false));

        // The new namespace changes alias lookup but not the generated C# for either document.
        var shadow = CSharpSyntaxTree.ParseText(SourceText.From("namespace MyApp.Library { public class Helper { } }"), (CSharpParseOptions)project.ParseOptions!);
        var updatedCompilation = compilation.AddSyntaxTrees(shadow);
        var updated = RunGenerator(updatedCompilation, ref driver);
        var (freshDriver, _, _) = await GetDriverWithAdditionalTextAndProviderAsync(
            project,
            configureGlobalOptions: options => options.TestGlobalOptions["build_property.RazorLangVersion"] = "12.0",
            hostOutputs: true);
        var fresh = RunGenerator(updatedCompilation, ref freshDriver);

        Assert.Equal(
            original.GeneratedSources.Select(source => source.SourceText.ToString()),
            updated.GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.Equal(
            fresh.GeneratedSources.Select(source => source.SourceText.ToString()),
            updated.GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.Null(GetCodeDocument(fresh, "Index.razor").GetRequiredTagHelperContext().GetBinder().GetBinding("UI.Button", [], parentTagName: null, parentIsTagHelper: false));
        Assert.Null(GetCodeDocument(updated, "Index.razor").GetRequiredTagHelperContext().GetBinder().GetBinding("UI.Button", [], parentTagName: null, parentIsTagHelper: false));
        Assert.Same(originalOtherDocument, GetCodeDocument(updated, "Other.razor"));
        Assert.NotNull(GetCodeDocument(updated, "Other.razor").GetRequiredTagHelperContext().GetBinder().GetBinding("UI.Button", [], parentTagName: null, parentIsTagHelper: false));

        var reasons = updated.TrackedSteps["CSharpDocuments"].SelectMany(step => step.Outputs).Select(output => output.Reason).ToArray();
        Assert.Equal(1, reasons.Count(reason => reason == IncrementalStepRunReason.Unchanged));
        Assert.Equal(1, reasons.Count(reason => reason == IncrementalStepRunReason.Cached));

        var restored = RunGenerator(compilation, ref driver);
        Assert.NotNull(GetCodeDocument(restored, "Index.razor").GetRequiredTagHelperContext().GetBinder().GetBinding("UI.Button", [], parentTagName: null, parentIsTagHelper: false));
        Assert.Same(originalOtherDocument, GetCodeDocument(restored, "Other.razor"));
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
