// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostDocumentSymbolEndpointTest(ITestOutputHelper testOutput) : CohostEndpointTestBase(testOutput)
{
    [Fact]
    public Task DocumentSymbols_CSharpClassWithMethods()
        => VerifySymbolInformationsAsync(
            """
            {|BuildRenderTree():|}@code {
                class {|SomeProject.File1.C:C|}
                {
                    private void {|HandleString(string s):HandleString|}(string s)
                    {
                        s += "Hello";
                    }

                    private void {|M(int i):M|}(int i)
                    {
                        i++;
                    }
            
                    private string {|ObjToString(object o):ObjToString|}(object o)
                    {
                        return o.ToString();
                    }
                }
            }
            
            """);

    [Fact]
    public async Task DocumentSymbols_CSharpClassWithMethods_Hierarchical()
    {
        TestCode input = """
            @code {
                class {|C:C|}
                {
                    private void {|HandleString:HandleString|}(string s)
                    {
                        s += "Hello";
                    }

                    private void {|M:M|}(int i)
                    {
                        i++;
                    }
            
                    private string {|ObjToString:ObjToString|}(object o)
                    {
                        return o.ToString();
                    }
                }
            }
            
            """;

        var documentSymbols = await GetDocumentSymbolsAsync(input);
        var sourceText = SourceText.From(input.Text);

        // Expect: 1 class C containing HandleString, M, ObjToString methods
        var classC = Assert.Single(documentSymbols);
        Assert.Equal("C", classC.Name);
        Assert.Equal(SymbolKind.Class, classC.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["C"])), classC.SelectionRange);
        Assert.NotNull(classC.Children);
        Assert.Equal(3, classC.Children!.Length);

        var handleString = classC.Children[0];
        Assert.Equal("HandleString(string) : void", handleString.Name);
        Assert.Equal(SymbolKind.Method, handleString.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["HandleString"])), handleString.SelectionRange);

        var m = classC.Children[1];
        Assert.Equal("M(int) : void", m.Name);
        Assert.Equal(SymbolKind.Method, m.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["M"])), m.SelectionRange);

        var objToString = classC.Children[2];
        Assert.Equal("ObjToString(object) : string", objToString.Name);
        Assert.Equal(SymbolKind.Method, objToString.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["ObjToString"])), objToString.SelectionRange);
    }

    [Fact]
    public Task DocumentSymbols_CSharpClassWithMethods_MiscFile()
    {
        // What the source generator would product for TestProjectData.SomeProjectPath
        var generatedNamespace = PlatformInformation.IsWindows
            ? "c_.users.example.src.SomeProject"
            : "home.example.SomeProject";
        return VerifySymbolInformationsAsync(
            $$"""
            {|BuildRenderTree():|}@code {
                class {|ASP.{{generatedNamespace}}.File1.C:C|}
                {
                    private void {|HandleString(string s):HandleString|}(string s)
                    {
                        s += "Hello";
                    }

                    private void {|M(int i):M|}(int i)
                    {
                        i++;
                    }
            
                    private string {|ObjToString(object o):ObjToString|}(object o)
                    {
                        return o.ToString();
                    }
                }
            }
            
            """,
            miscellaneousFile: true);
    }

    [Fact]
    public async Task DocumentSymbols_CSharpClassWithMethods_MiscFile_Hierarchical()
    {
        TestCode input = """
            @code {
                class {|C:C|}
                {
                    private void {|HandleString:HandleString|}(string s)
                    {
                        s += "Hello";
                    }

                    private void {|M:M|}(int i)
                    {
                        i++;
                    }
            
                    private string {|ObjToString:ObjToString|}(object o)
                    {
                        return o.ToString();
                    }
                }
            }
            
            """;

        var documentSymbols = await GetDocumentSymbolsAsync(input, miscellaneousFile: true);
        var sourceText = SourceText.From(input.Text);

        // Expect: 1 class C containing HandleString, M, ObjToString methods
        var classC = Assert.Single(documentSymbols);
        Assert.Equal("C", classC.Name);
        Assert.Equal(SymbolKind.Class, classC.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["C"])), classC.SelectionRange);
        Assert.NotNull(classC.Children);
        Assert.Equal(3, classC.Children!.Length);

        var handleString = classC.Children[0];
        Assert.Equal("HandleString(string) : void", handleString.Name);
        Assert.Equal(SymbolKind.Method, handleString.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["HandleString"])), handleString.SelectionRange);

        var m = classC.Children[1];
        Assert.Equal("M(int) : void", m.Name);
        Assert.Equal(SymbolKind.Method, m.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["M"])), m.SelectionRange);

        var objToString = classC.Children[2];
        Assert.Equal("ObjToString(object) : string", objToString.Name);
        Assert.Equal(SymbolKind.Method, objToString.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["ObjToString"])), objToString.SelectionRange);
    }

    [Fact]
    public Task DocumentSymbols_CSharpMethods()
        => VerifySymbolInformationsAsync(
            """
            {|BuildRenderTree():|}@code {
                private void {|HandleString(string s):HandleString|}(string s)
                {
                    s += "Hello";
                }

                private void {|M(int i):M|}(int i)
                {
                    i++;
                }

                private string {|ObjToString(object o):ObjToString|}(object o)
                {
                    return o.ToString();
                }
            }
            
            """);

    [Fact]
    public async Task DocumentSymbols_CSharpMethods_Hierarchical()
    {
        TestCode input = """
            @code {
                private void {|HandleString:HandleString|}(string s)
                {
                    s += "Hello";
                }

                private void {|M:M|}(int i)
                {
                    i++;
                }

                private string {|ObjToString:ObjToString|}(object o)
                {
                    return o.ToString();
                }
            }
            
            """;

        var documentSymbols = await GetDocumentSymbolsAsync(input);
        var sourceText = SourceText.From(input.Text);

        // Expect: HandleString, M, ObjToString methods at top level
        Assert.Equal(3, documentSymbols.Length);

        var handleString = documentSymbols[0];
        Assert.Equal("HandleString(string) : void", handleString.Name);
        Assert.Equal(SymbolKind.Method, handleString.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["HandleString"])), handleString.SelectionRange);

        var m = documentSymbols[1];
        Assert.Equal("M(int) : void", m.Name);
        Assert.Equal(SymbolKind.Method, m.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["M"])), m.SelectionRange);

        var objToString = documentSymbols[2];
        Assert.Equal("ObjToString(object) : string", objToString.Name);
        Assert.Equal(SymbolKind.Method, objToString.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["ObjToString"])), objToString.SelectionRange);
    }

    [Fact]
    public Task DocumentSymbols_CSharpMethods_Legacy()
        => VerifySymbolInformationsAsync(
            """
                {|ExecuteAsync():|}@functions {
                    private void {|HandleString(string s):HandleString|}(string s)
                    {
                        s += "Hello";
                    }

                    private void {|M(int i):M|}(int i)
                    {
                        i++;
                    }

                    private string {|ObjToString(object o):ObjToString|}(object o)
                    {
                        return o.ToString();
                    }
                }
            
                """,
            fileKind: RazorFileKind.Legacy);

    [Fact]
    public async Task DocumentSymbols_CSharpMethods_Legacy_Hierarchical()
    {
        TestCode input = """
            @functions {
                private void {|HandleString:HandleString|}(string s)
                {
                    s += "Hello";
                }

                private void {|M:M|}(int i)
                {
                    i++;
                }

                private string {|ObjToString:ObjToString|}(object o)
                {
                    return o.ToString();
                }
            }
            
            """;

        var documentSymbols = await GetDocumentSymbolsAsync(input, fileKind: RazorFileKind.Legacy);
        var sourceText = SourceText.From(input.Text);

        // Expect: HandleString, M, ObjToString methods at top level
        Assert.Equal(3, documentSymbols.Length);

        var handleString = documentSymbols[0];
        Assert.Equal("HandleString(string) : void", handleString.Name);
        Assert.Equal(SymbolKind.Method, handleString.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["HandleString"])), handleString.SelectionRange);

        var m = documentSymbols[1];
        Assert.Equal("M(int) : void", m.Name);
        Assert.Equal(SymbolKind.Method, m.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["M"])), m.SelectionRange);

        var objToString = documentSymbols[2];
        Assert.Equal("ObjToString(object) : string", objToString.Name);
        Assert.Equal(SymbolKind.Method, objToString.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["ObjToString"])), objToString.SelectionRange);
    }

    private async Task VerifySymbolInformationsAsync(string input, bool miscellaneousFile = false, RazorFileKind? fileKind = null)
    {
        fileKind ??= RazorFileKind.Component;

        TestFileMarkupParser.GetSpans(input, out input, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spansDict);
        var document = CreateProjectAndRazorDocument(input, fileKind, miscellaneousFile: miscellaneousFile);

        var endpoint = new CohostDocumentSymbolEndpoint(IncompatibleProjectService, RemoteServiceInvoker);

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(document, useHierarchicalSymbols: false, DisposalToken);

        // Roslyn's DocumentSymbol type has an annoying property that makes it hard to serialize
        Assert.NotNull(JsonSerializer.SerializeToDocument(result, JsonHelpers.JsonSerializerOptions));

        var sourceText = SourceText.From(input);

        Assumes.NotNull(result);
        var symbolsInformations = result.Value.Second;
        Assert.Equal(spansDict.Values.Count(), symbolsInformations.Length);

#pragma warning disable CS0618 // Type or member is obsolete
        // SymbolInformation is obsolete, but things still return it so we have to handle it
        foreach (var symbolInformation in symbolsInformations)
        {
            Assert.True(spansDict.TryGetValue(symbolInformation.Name, out var spans), $"Expected {symbolInformation.Name} to be in test provided markers");
            var expectedRange = sourceText.GetRange(Assert.Single(spans));
            Assert.Equal(expectedRange, symbolInformation.Location.Range);
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }

    private async Task<DocumentSymbol[]> GetDocumentSymbolsAsync(TestCode input, bool miscellaneousFile = false, RazorFileKind? fileKind = null)
    {
        fileKind ??= RazorFileKind.Component;

        var document = CreateProjectAndRazorDocument(input.Text, fileKind, miscellaneousFile: miscellaneousFile);

        var endpoint = new CohostDocumentSymbolEndpoint(IncompatibleProjectService, RemoteServiceInvoker);

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(document, useHierarchicalSymbols: true, DisposalToken);

        // Roslyn's DocumentSymbol type has an annoying property that makes it hard to serialize
        Assert.NotNull(JsonSerializer.SerializeToDocument(result, JsonHelpers.JsonSerializerOptions));

        Assumes.NotNull(result);
        Assert.True(result.Value.TryGetFirst(out var documentSymbols));
        return documentSymbols;
    }
}
