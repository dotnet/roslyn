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
    public Task DocumentSymbols_NoCodeBlock()
        => VerifySymbolInformationsAsync(
            """
            {|BuildRenderTree():|}<p>Hello</p>

            """);

    [Fact]
    public async Task DocumentSymbols_NoCodeBlock_Hierarchical()
    {
        TestCode input = """
            {|BuildRenderTree():|}<p>Hello</p>

            """;

        var documentSymbols = await GetDocumentSymbolsAsync(input);
        var sourceText = SourceText.From(input.Text);

        var buildRenderTree = Assert.Single(documentSymbols);
        Assert.Equal("BuildRenderTree()", buildRenderTree.Name);
        Assert.Equal(SymbolKind.Method, buildRenderTree.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["BuildRenderTree()"])), buildRenderTree.SelectionRange);
    }

    // Flat-symbol clients still use the obsolete SymbolInformation API.
#pragma warning disable CS0618 // Type or member is obsolete
    [Fact]
    public async Task DocumentSymbols_LayoutNull_DoesNotExposeGeneratedLayout()
    {
        var symbols = await VerifySymbolInformationsAsync(
            input: """
                {|BuildRenderTree():|}@namespace Custom.Components
                @layout null
                <p>Hello</p>
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var renderMethod = Assert.Single(symbols, symbol => symbol.Name == "BuildRenderTree()");
        Assert.Equal("Custom.Components.File1", renderMethod.ContainerName);
    }

    [Fact]
    public async Task DocumentSymbols_LayoutNull_WithUserMember()
    {
        var symbols = await VerifySymbolInformationsAsync(
            input: """
                {|BuildRenderTree():|}@namespace Custom.Components
                @layout null
                <p>Hello</p>
                @code {
                    private void {|UserMethod():UserMethod|}() { }
                }
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var renderMethod = Assert.Single(symbols, symbol => symbol.Name == "BuildRenderTree()");
        Assert.Equal("Custom.Components.File1", renderMethod.ContainerName);
    }

    [Fact]
    public async Task DocumentSymbols_LayoutNull_Generic()
    {
        var symbols = await VerifySymbolInformationsAsync(
            input: """
                {|BuildRenderTree():|}@namespace Custom.Components
                @layout null
                @typeparam TItem
                <p>Hello</p>
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var renderMethod = Assert.Single(symbols, symbol => symbol.Name == "BuildRenderTree()");
        Assert.Equal("Custom.Components.File1<TItem>", renderMethod.ContainerName);
    }

    [Fact]
    public async Task DocumentSymbols_LayoutNull_Generic_WithUserMember()
    {
        var symbols = await VerifySymbolInformationsAsync(
            input: """
                {|BuildRenderTree():|}@namespace Custom.Components
                @layout null
                @typeparam TItem
                <p>Hello</p>
                @code {
                    private void {|UserMethod():UserMethod|}() { }
                }
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var renderMethod = Assert.Single(symbols, symbol => symbol.Name == "BuildRenderTree()");
        Assert.Equal("Custom.Components.File1<TItem>", renderMethod.ContainerName);
    }

    // A preprocessor directive forces unsplit output, so the helper shares the component's generated file.
    [Fact]
    public async Task DocumentSymbols_LayoutNull_Unsplit()
    {
        var symbols = await VerifySymbolInformationsAsync(
            input: """
                {|BuildRenderTree():|}@namespace Custom.Components
                @layout null
                <p>Hello</p>
                @code {
                    #if DEBUG
                    #endif
                }
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var renderMethod = Assert.Single(symbols, symbol => symbol.Name == "BuildRenderTree()");
        Assert.Equal("Custom.Components.File1", renderMethod.ContainerName);
    }

    [Fact]
    public async Task DocumentSymbols_LayoutNull_Unsplit_WithUserMember()
    {
        var symbols = await VerifySymbolInformationsAsync(
            input: """
                {|BuildRenderTree():|}@namespace Custom.Components
                @layout null
                <p>Hello</p>
                @code {
                    private void {|UserMethod():UserMethod|}() { }
                    #if DEBUG
                    #endif
                }
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var renderMethod = Assert.Single(symbols, symbol => symbol.Name == "BuildRenderTree()");
        Assert.Equal("Custom.Components.File1", renderMethod.ContainerName);
    }

    [Fact]
    public async Task DocumentSymbols_LayoutNull_Generic_Unsplit()
    {
        var symbols = await VerifySymbolInformationsAsync(
            input: """
                {|BuildRenderTree():|}@namespace Custom.Components
                @layout null
                @typeparam TItem
                <p>Hello</p>
                @code {
                    #if DEBUG
                    #endif
                }
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var renderMethod = Assert.Single(symbols, symbol => symbol.Name == "BuildRenderTree()");
        Assert.Equal("Custom.Components.File1<TItem>", renderMethod.ContainerName);
    }

    [Fact]
    public async Task DocumentSymbols_LayoutNull_Generic_Unsplit_WithUserMember()
    {
        var symbols = await VerifySymbolInformationsAsync(
            input: """
                {|BuildRenderTree():|}@namespace Custom.Components
                @layout null
                @typeparam TItem
                <p>Hello</p>
                @code {
                    private void {|UserMethod():UserMethod|}() { }
                    #if DEBUG
                    #endif
                }
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var renderMethod = Assert.Single(symbols, symbol => symbol.Name == "BuildRenderTree()");
        Assert.Equal("Custom.Components.File1<TItem>", renderMethod.ContainerName);
    }
#pragma warning restore CS0618 // Type or member is obsolete

    [Fact]
    public async Task DocumentSymbols_LayoutNull_DoesNotExposeGeneratedLayout_Hierarchical()
    {
        var symbols = await GetDocumentSymbolsAsync(
            input: """
                @namespace Custom.Components
                @layout null
                <p>Hello</p>
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var symbol = Assert.Single(symbols);
        Assert.Equal("BuildRenderTree()", symbol.Name);
        Assert.True(symbol.Children is null or []);
    }

    [Fact]
    public async Task DocumentSymbols_LayoutNull_WithUserMember_Hierarchical()
    {
        var symbols = await GetDocumentSymbolsAsync(
            input: """
                @namespace Custom.Components
                @layout null
                <p>Hello</p>
                @code {
                    private void UserMethod() { }
                }
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var symbol = Assert.Single(symbols);
        Assert.Equal("UserMethod() : void", symbol.Name);
        Assert.True(symbol.Children is null or []);
    }

    [Fact]
    public async Task DocumentSymbols_LayoutNull_Generic_Hierarchical()
    {
        var symbols = await GetDocumentSymbolsAsync(
            input: """
                @namespace Custom.Components
                @layout null
                @typeparam TItem
                <p>Hello</p>
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var symbol = Assert.Single(symbols);
        Assert.Equal("BuildRenderTree()", symbol.Name);
        Assert.True(symbol.Children is null or []);
    }

    [Fact]
    public async Task DocumentSymbols_LayoutNull_Generic_WithUserMember_Hierarchical()
    {
        var symbols = await GetDocumentSymbolsAsync(
            input: """
                @namespace Custom.Components
                @layout null
                @typeparam TItem
                <p>Hello</p>
                @code {
                    private void UserMethod() { }
                }
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var symbol = Assert.Single(symbols);
        Assert.Equal("UserMethod() : void", symbol.Name);
        Assert.True(symbol.Children is null or []);
    }

    [Fact]
    public async Task DocumentSymbols_LayoutNull_Unsplit_Hierarchical()
    {
        var symbols = await GetDocumentSymbolsAsync(
            input: """
                @namespace Custom.Components
                @layout null
                <p>Hello</p>
                @code {
                    #if DEBUG
                    #endif
                }
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var symbol = Assert.Single(symbols);
        Assert.Equal("BuildRenderTree()", symbol.Name);
        Assert.True(symbol.Children is null or []);
    }

    [Fact]
    public async Task DocumentSymbols_LayoutNull_Unsplit_WithUserMember_Hierarchical()
    {
        var symbols = await GetDocumentSymbolsAsync(
            input: """
                @namespace Custom.Components
                @layout null
                <p>Hello</p>
                @code {
                    private void UserMethod() { }
                    #if DEBUG
                    #endif
                }
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var symbol = Assert.Single(symbols);
        Assert.Equal("UserMethod() : void", symbol.Name);
        Assert.True(symbol.Children is null or []);
    }

    [Fact]
    public async Task DocumentSymbols_LayoutNull_Generic_Unsplit_Hierarchical()
    {
        var symbols = await GetDocumentSymbolsAsync(
            input: """
                @namespace Custom.Components
                @layout null
                @typeparam TItem
                <p>Hello</p>
                @code {
                    #if DEBUG
                    #endif
                }
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var symbol = Assert.Single(symbols);
        Assert.Equal("BuildRenderTree()", symbol.Name);
        Assert.True(symbol.Children is null or []);
    }

    [Fact]
    public async Task DocumentSymbols_LayoutNull_Generic_Unsplit_WithUserMember_Hierarchical()
    {
        var symbols = await GetDocumentSymbolsAsync(
            input: """
                @namespace Custom.Components
                @layout null
                @typeparam TItem
                <p>Hello</p>
                @code {
                    private void UserMethod() { }
                    #if DEBUG
                    #endif
                }
                """);

        Assert.DoesNotContain("__EmptyLayout", JsonSerializer.Serialize(symbols, JsonHelpers.JsonSerializerOptions));
        var symbol = Assert.Single(symbols);
        Assert.Equal("UserMethod() : void", symbol.Name);
        Assert.True(symbol.Children is null or []);
    }

    [Fact]
    public Task DocumentSymbols_UserRenderMethod()
        => VerifySymbolInformationsAsync(
            input: """
                {|BuildRenderTree():|}@using Microsoft.AspNetCore.Components.Rendering
                @code {
                    class {|SomeProject.File1.C:C|}
                    {
                        private void {|BuildRenderTree(RenderTreeBuilder __builder):BuildRenderTree|}(RenderTreeBuilder __builder)
                        {
                        }
                    }
                }
                """);

    [Fact]
    public Task DocumentSymbols_EmptyCodeBlock()
        => VerifySymbolInformationsAsync(
            """
            {|BuildRenderTree():|}@code {
            }

            """);

    [Fact]
    public async Task DocumentSymbols_EmptyCodeBlock_Hierarchical()
    {
        TestCode input = """
            {|BuildRenderTree():|}@code {
            }

            """;

        var documentSymbols = await GetDocumentSymbolsAsync(input);
        var sourceText = SourceText.From(input.Text);

        var buildRenderTree = Assert.Single(documentSymbols);
        Assert.Equal("BuildRenderTree()", buildRenderTree.Name);
        Assert.Equal(SymbolKind.Method, buildRenderTree.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["BuildRenderTree()"])), buildRenderTree.SelectionRange);
    }

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
    public async Task DocumentSymbols_CSharpPropertyWithMarkup()
    {
        TestCode input = """
            @code {
                public RenderFragment {|Property:Property|} => @<div></div>;
            }
            """;

        var documentSymbols = await GetDocumentSymbolsAsync(input);
        var sourceText = SourceText.From(input.Text);

        var property = Assert.Single(documentSymbols);
        Assert.Equal("Property : RenderFragment", property.Name);
        Assert.Equal(SymbolKind.Property, property.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["Property"])), property.SelectionRange);
    }

    [Fact]
    public async Task DocumentSymbols_CSharpMethodWithMarkup()
    {
        TestCode input = """
            @code {
                public RenderFragment {|Method:Method|}() => @<div></div>;
            }
            """;

        var documentSymbols = await GetDocumentSymbolsAsync(input);
        var sourceText = SourceText.From(input.Text);

        var method = Assert.Single(documentSymbols);
        Assert.Equal("Method() : RenderFragment", method.Name);
        Assert.Equal(SymbolKind.Method, method.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["Method"])), method.SelectionRange);
    }

    [Fact]
    public Task DocumentSymbols_CSharpClassWithMethods_Legacy()
        => VerifySymbolInformationsAsync(
            """
            {|ExecuteAsync():|}@functions {
                class {|AspNetCoreGeneratedDocument.File1.C:C|}
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
            fileKind: RazorFileKind.Legacy);

    [Fact]
    public async Task DocumentSymbols_CSharpClassWithMethods_Legacy_Hierarchical()
    {
        TestCode input = """
            @functions {
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

        var documentSymbols = await GetDocumentSymbolsAsync(input, fileKind: RazorFileKind.Legacy);
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
    public Task DocumentSymbols_CSharpClassWithMethods_Legacy_MiscFile()
    {
        // What the source generator would product for TestProjectData.SomeProjectPath
        var generatedClassName = PlatformInformation.IsWindows
            ? "c__users_example_src_SomeProject_File1"
            : "_home_example_SomeProject_File1";
        return VerifySymbolInformationsAsync(
            $$"""
            {|ExecuteAsync():|}@functions {
                class {|AspNetCoreGeneratedDocument.{{generatedClassName}}.C:C|}
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
            miscellaneousFile: true,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task DocumentSymbols_CSharpClassWithMethods_Legacy_MiscFile_Hierarchical()
    {
        TestCode input = """
            @functions {
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

        var documentSymbols = await GetDocumentSymbolsAsync(input, miscellaneousFile: true, fileKind: RazorFileKind.Legacy);
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
    public Task DocumentSymbols_NoCodeBlock_Legacy()
        => VerifySymbolInformationsAsync(
            """
            {|ExecuteAsync():|}<p>Hello</p>

            """,
            fileKind: RazorFileKind.Legacy);

    [Fact]
    public async Task DocumentSymbols_NoCodeBlock_Legacy_Hierarchical()
    {
        TestCode input = """
            {|ExecuteAsync():|}<p>Hello</p>

            """;

        var documentSymbols = await GetDocumentSymbolsAsync(input, fileKind: RazorFileKind.Legacy);
        var sourceText = SourceText.From(input.Text);

        var executeAsync = Assert.Single(documentSymbols);
        Assert.Equal("ExecuteAsync()", executeAsync.Name);
        Assert.Equal(SymbolKind.Method, executeAsync.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["ExecuteAsync()"])), executeAsync.SelectionRange);
    }

    [Fact]
    public Task DocumentSymbols_EmptyCodeBlock_Legacy()
        => VerifySymbolInformationsAsync(
            """
            {|ExecuteAsync():|}@functions {
            }

            """,
            fileKind: RazorFileKind.Legacy);

    [Fact]
    public async Task DocumentSymbols_EmptyCodeBlock_Legacy_Hierarchical()
    {
        TestCode input = """
            {|ExecuteAsync():|}@functions {
            }

            """;

        var documentSymbols = await GetDocumentSymbolsAsync(input, fileKind: RazorFileKind.Legacy);
        var sourceText = SourceText.From(input.Text);

        var executeAsync = Assert.Single(documentSymbols);
        Assert.Equal("ExecuteAsync()", executeAsync.Name);
        Assert.Equal(SymbolKind.Method, executeAsync.Kind);
        Assert.Equal(sourceText.GetRange(Assert.Single(input.NamedSpans["ExecuteAsync()"])), executeAsync.SelectionRange);
    }

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

    // SymbolInformation is obsolete, but things still return it so we have to handle it.
#pragma warning disable CS0618 // Type or member is obsolete
    private async Task<SymbolInformation[]> VerifySymbolInformationsAsync(string input, bool miscellaneousFile = false, RazorFileKind? fileKind = null)
    {
        fileKind ??= RazorFileKind.Component;

        TestFileMarkupParser.GetSpans(input, out input, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spansDict);
        var document = CreateProjectAndRazorDocument(input, fileKind, miscellaneousFile: miscellaneousFile);

        var endpoint = new CohostDocumentSymbolEndpoint(IncompatibleProjectService, RemoteServiceInvoker);

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(document, useHierarchicalSymbols: false, DisposalToken);

        // Roslyn's DocumentSymbol type has an annoying property that makes it hard to serialize
        Assert.NotNull(JsonSerializer.SerializeToDocument(result, JsonHelpers.JsonSerializerOptions));

        var sourceText = SourceText.From(input);

        Microsoft.CodeAnalysis.Contract.ThrowIfNull(result);
        var symbolsInformations = result.Value.Second;
        Assert.Equal(spansDict.Values.Count(), symbolsInformations.Length);

        foreach (var symbolInformation in symbolsInformations)
        {
            Assert.True(spansDict.TryGetValue(symbolInformation.Name, out var spans), $"Expected {symbolInformation.Name} to be in test provided markers");
            var expectedRange = sourceText.GetRange(Assert.Single(spans));
            Assert.Equal(expectedRange, symbolInformation.Location.Range);
        }

        return symbolsInformations;
    }
#pragma warning restore CS0618 // Type or member is obsolete

    private async Task<DocumentSymbol[]> GetDocumentSymbolsAsync(TestCode input, bool miscellaneousFile = false, RazorFileKind? fileKind = null)
    {
        fileKind ??= RazorFileKind.Component;

        var document = CreateProjectAndRazorDocument(input.Text, fileKind, miscellaneousFile: miscellaneousFile);

        var endpoint = new CohostDocumentSymbolEndpoint(IncompatibleProjectService, RemoteServiceInvoker);

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(document, useHierarchicalSymbols: true, DisposalToken);

        // Roslyn's DocumentSymbol type has an annoying property that makes it hard to serialize
        Assert.NotNull(JsonSerializer.SerializeToDocument(result, JsonHelpers.JsonSerializerOptions));

        Microsoft.CodeAnalysis.Contract.ThrowIfNull(result);
        Assert.True(result.Value.TryGetFirst(out var documentSymbols));
        return documentSymbols;
    }
}
