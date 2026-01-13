// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Symbols;

public sealed class DocumentSymbolsTests : AbstractLanguageServerProtocolTests
{
    public DocumentSymbolsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestGetDocumentSymbolsAsync(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|namespace:namespace {|namespaceSelection:Test|};

            {|class:class {|classSelection:A|}
            {
                {|constructor:public {|constructorSelection:A|}()
                {
                }|}

                {|method:void {|methodSelection:M|}()
                {
                }|}

                {|operator:static A {|operatorSelection:operator +|}(A a1, A a2) => a1;|}
            }|}|}
            """;
        var clientCapabilities = new LSP.ClientCapabilities()
        {
            TextDocument = new LSP.TextDocumentClientCapabilities()
            {
                DocumentSymbol = new LSP.DocumentSymbolSetting()
                {
                    HierarchicalDocumentSymbolSupport = true
                }
            }
        };
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
        var namespaceSymbol = CreateDocumentSymbol(LSP.SymbolKind.Namespace, "Test", "Test", testLspServer.GetLocations("namespace").Single(), testLspServer.GetLocations("namespaceSelection").Single());
        var classSymbol = CreateDocumentSymbol(LSP.SymbolKind.Class, "A", "A", testLspServer.GetLocations("class").Single(), testLspServer.GetLocations("classSelection").Single(), namespaceSymbol);
        var constructorSymbol = CreateDocumentSymbol(LSP.SymbolKind.Constructor, "A", "A()", testLspServer.GetLocations("constructor").Single(), testLspServer.GetLocations("constructorSelection").Single(), classSymbol);
        var methodSymbol = CreateDocumentSymbol(LSP.SymbolKind.Method, "M", "M()", testLspServer.GetLocations("method").Single(), testLspServer.GetLocations("methodSelection").Single(), classSymbol);
        var operatorSymbol = CreateDocumentSymbol(LSP.SymbolKind.Operator, "operator +", "operator +(A a1, A a2)", testLspServer.GetLocations("operator").Single(), testLspServer.GetLocations("operatorSelection").Single(), classSymbol);

        LSP.DocumentSymbol[] expected = [namespaceSymbol];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        Assert.NotNull(results);
        Assert.Equal(expected.Length, results.Length);
        for (var i = 0; i < results.Length; i++)
        {
            AssertDocumentSymbolEquals(expected[i], results[i]);
        }
    }

    [Theory, CombinatorialData]
    public async Task TestGetDocumentSymbolsAsync_WithoutHierarchicalSupport(bool mutatingLspWorkspace)
    {
        var markup =
            """
            namespace Test;

            class {|class:A|}
            {
                public {|constructor:A|}()
                {
                }

                void {|method:M|}()
                {
                }

                static A operator {|operator:+|}(A a1, A a2) => a1;
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        LSP.SymbolInformation[] expected = [
            CreateSymbolInformation(LSP.SymbolKind.Class, "Test.A", testLspServer.GetLocations("class").Single(), Glyph.ClassInternal),
            CreateSymbolInformation(LSP.SymbolKind.Method, "A()", testLspServer.GetLocations("constructor").Single(), Glyph.MethodPublic, "Test.A"),
            CreateSymbolInformation(LSP.SymbolKind.Method, "M()", testLspServer.GetLocations("method").Single(), Glyph.MethodPrivate, "Test.A"),
            CreateSymbolInformation(LSP.SymbolKind.Operator, "operator +(A a1, A a2)", testLspServer.GetLocations("operator").Single(), Glyph.OperatorPrivate, "Test.A"),
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.SymbolInformation[]>(testLspServer);
        AssertJsonEquals(expected, results);
    }

    [Theory(Skip = "GetDocumentSymbolsAsync does not yet support locals."), CombinatorialData]
    // TODO - Remove skip & modify once GetDocumentSymbolsAsync is updated to support more than 2 levels.
    // https://github.com/dotnet/roslyn/projects/45#card-20033869
    public async Task TestGetDocumentSymbolsAsync__WithLocals(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void Method()
                {
                    int i = 1;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var results = await RunGetDocumentSymbolsAsync<LSP.SymbolInformation[]>(testLspServer).ConfigureAwait(false);
        Assert.NotNull(results);
        Assert.Equal(3, results.Length);
    }

    [Theory, CombinatorialData]
    public async Task TestGetDocumentSymbolsAsync_EmptyName(bool mutatingLspWorkspace)
    {
        var markup =
            """
            namepsace NamespaceA
            {
                public class
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var results = await RunGetDocumentSymbolsAsync<LSP.SymbolInformation[]>(testLspServer).ConfigureAwait(false);
        Assert.NotNull(results);
#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Equal(".", results.First().Name);
#pragma warning restore CS0618
    }

    [Theory, CombinatorialData]
    public async Task TestGetDocumentSymbolsAsync_EmptyNameWithHierarchicalSupport(bool mutatingLspWorkspace)
    {
        var markup =
            """
            namespace NamespaceA
            {
                public class
            }
            """;
        var clientCapabilities = new LSP.ClientCapabilities()
        {
            TextDocument = new LSP.TextDocumentClientCapabilities()
            {
                DocumentSymbol = new LSP.DocumentSymbolSetting()
                {
                    HierarchicalDocumentSymbolSupport = true
                }
            }
        };

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer).ConfigureAwait(false);
        Assert.NotNull(results);
        // The namespace "NamespaceA" is the top-level symbol with the empty-named class as a child
        Assert.Single(results);
        Assert.Equal("NamespaceA", results[0].Name);
        Assert.NotNull(results[0].Children);
        Assert.Single(results[0].Children!);
        Assert.Equal(".", results[0].Children![0].Name);
    }

    [Theory, CombinatorialData]
    public async Task TestGetDocumentSymbolsAsync__NoSymbols(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);

        var results = await RunGetDocumentSymbolsAsync<LSP.SymbolInformation[]>(testLspServer);
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGetDocumentSymbolsAsync_LocalFunction(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|namespace:namespace {|namespaceSelection:Test|};
            {|class:class {|classSelection:A|}
            {
                {|method:void {|methodSelection:M|}()
                {
                    {|localFunction:void {|localFunctionSelection:LocalFunction|}()
                    {
                    }|}
                }|}
            }|}|}
            """;
        var clientCapabilities = new LSP.ClientCapabilities()
        {
            TextDocument = new LSP.TextDocumentClientCapabilities()
            {
                DocumentSymbol = new LSP.DocumentSymbolSetting()
                {
                    HierarchicalDocumentSymbolSupport = true
                }
            }
        };

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
        var namespaceSymbol = CreateDocumentSymbol(LSP.SymbolKind.Namespace, "Test", "Test", testLspServer.GetLocations("namespace").Single(), testLspServer.GetLocations("namespaceSelection").Single());
        var classSymbol = CreateDocumentSymbol(LSP.SymbolKind.Class, "A", "A", testLspServer.GetLocations("class").Single(), testLspServer.GetLocations("classSelection").Single(), namespaceSymbol);
        var methodSymbol = CreateDocumentSymbol(LSP.SymbolKind.Method, "M", "M()", testLspServer.GetLocations("method").Single(), testLspServer.GetLocations("methodSelection").Single(), classSymbol);
        var localFunctionSymbol = CreateDocumentSymbol(LSP.SymbolKind.Function, "LocalFunction", "LocalFunction()", testLspServer.GetLocations("localFunction").Single(), testLspServer.GetLocations("localFunctionSelection").Single(), methodSymbol);

        LSP.DocumentSymbol[] expected = [namespaceSymbol];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        Assert.NotNull(results);
        Assert.Equal(expected.Length, results.Length);
        for (var i = 0; i < results.Length; i++)
        {
            AssertDocumentSymbolEquals(expected[i], results[i]);
        }
    }

    private static async Task<TReturn?> RunGetDocumentSymbolsAsync<TReturn>(TestLspServer testLspServer)
    {
        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
        var request = new LSP.DocumentSymbolParams
        {
            TextDocument = CreateTextDocumentIdentifier(document.GetURI())
        };

        return await testLspServer.ExecuteRequestAsync<LSP.DocumentSymbolParams, TReturn>(LSP.Methods.TextDocumentDocumentSymbolName,
            request, CancellationToken.None);
    }

    private static void AssertDocumentSymbolEquals(LSP.DocumentSymbol expected, LSP.DocumentSymbol actual)
    {
        Assert.Equal(expected.Kind, actual.Kind);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Detail, actual.Detail);
        Assert.Equal(expected.Range, actual.Range);
        Assert.Equal(expected.Children?.Length, actual.Children?.Length);
        if (expected.Children is not null)
        {
            for (var i = 0; i < actual.Children!.Length; i++)
            {
                AssertDocumentSymbolEquals(expected.Children[i], actual.Children[i]);
            }
        }
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_NestedNamespace(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|outerNamespace:namespace {|outerNamespaceSelection:Outer|}
            {
                {|innerNamespace:namespace {|innerNamespaceSelection:Inner|}
                {
                    {|class:class {|classSelection:A|}
                    {
                    }|}
                }|}
            }|}
            """;
        var clientCapabilities = new LSP.ClientCapabilities()
        {
            TextDocument = new LSP.TextDocumentClientCapabilities()
            {
                DocumentSymbol = new LSP.DocumentSymbolSetting()
                {
                    HierarchicalDocumentSymbolSupport = true
                }
            }
        };

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
        var outerNamespaceSymbol = CreateDocumentSymbol(LSP.SymbolKind.Namespace, "Outer", "Outer", testLspServer.GetLocations("outerNamespace").Single(), testLspServer.GetLocations("outerNamespaceSelection").Single());
        var innerNamespaceSymbol = CreateDocumentSymbol(LSP.SymbolKind.Namespace, "Inner", "Inner", testLspServer.GetLocations("innerNamespace").Single(), testLspServer.GetLocations("innerNamespaceSelection").Single(), outerNamespaceSymbol);
        var classSymbol = CreateDocumentSymbol(LSP.SymbolKind.Class, "A", "A", testLspServer.GetLocations("class").Single(), testLspServer.GetLocations("classSelection").Single(), innerNamespaceSymbol);

        LSP.DocumentSymbol[] expected = [outerNamespaceSymbol];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        Assert.NotNull(results);
        Assert.Equal(expected.Length, results.Length);
        for (var i = 0; i < results.Length; i++)
        {
            AssertDocumentSymbolEquals(expected[i], results[i]);
        }
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_ClassWithoutNamespace(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:A|}
            {
                {|method:void {|methodSelection:M|}()
                {
                }|}
            }|}
            """;
        var clientCapabilities = new LSP.ClientCapabilities()
        {
            TextDocument = new LSP.TextDocumentClientCapabilities()
            {
                DocumentSymbol = new LSP.DocumentSymbolSetting()
                {
                    HierarchicalDocumentSymbolSupport = true
                }
            }
        };

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
        var classSymbol = CreateDocumentSymbol(LSP.SymbolKind.Class, "A", "A", testLspServer.GetLocations("class").Single(), testLspServer.GetLocations("classSelection").Single());
        var methodSymbol = CreateDocumentSymbol(LSP.SymbolKind.Method, "M", "M()", testLspServer.GetLocations("method").Single(), testLspServer.GetLocations("methodSelection").Single(), classSymbol);

        LSP.DocumentSymbol[] expected = [classSymbol];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        Assert.NotNull(results);
        Assert.Equal(expected.Length, results.Length);
        for (var i = 0; i < results.Length; i++)
        {
            AssertDocumentSymbolEquals(expected[i], results[i]);
        }
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_NestedType(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|namespace:namespace {|namespaceSelection:Test|};

            {|class:class {|classSelection:Outer|}
            {
                {|nestedEnum:enum {|nestedEnumSelection:Bar|}
                {
                    {|enumMember:{|enumMemberSelection:None|}|}
                }|}
            }|}|}
            """;
        var clientCapabilities = new LSP.ClientCapabilities()
        {
            TextDocument = new LSP.TextDocumentClientCapabilities()
            {
                DocumentSymbol = new LSP.DocumentSymbolSetting()
                {
                    HierarchicalDocumentSymbolSupport = true
                }
            }
        };

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
        var namespaceSymbol = CreateDocumentSymbol(LSP.SymbolKind.Namespace, "Test", "Test", testLspServer.GetLocations("namespace").Single(), testLspServer.GetLocations("namespaceSelection").Single());
        var classSymbol = CreateDocumentSymbol(LSP.SymbolKind.Class, "Outer", "Outer", testLspServer.GetLocations("class").Single(), testLspServer.GetLocations("classSelection").Single(), namespaceSymbol);
        var nestedEnumSymbol = CreateDocumentSymbol(LSP.SymbolKind.Enum, "Bar", "Bar", testLspServer.GetLocations("nestedEnum").Single(), testLspServer.GetLocations("nestedEnumSelection").Single(), classSymbol);
        var enumMemberSymbol = CreateDocumentSymbol(LSP.SymbolKind.EnumMember, "None", "None", testLspServer.GetLocations("enumMember").Single(), testLspServer.GetLocations("enumMemberSelection").Single(), nestedEnumSymbol);

        LSP.DocumentSymbol[] expected = [namespaceSymbol];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        Assert.NotNull(results);
        Assert.Equal(expected.Length, results.Length);
        for (var i = 0; i < results.Length; i++)
        {
            AssertDocumentSymbolEquals(expected[i], results[i]);
        }
    }

    private static LSP.DocumentSymbol CreateDocumentSymbol(LSP.SymbolKind kind, string name, string detail,
        LSP.Location location, LSP.Location selection, LSP.DocumentSymbol? parent = null)
    {
        var documentSymbol = new LSP.DocumentSymbol()
        {
            Kind = kind,
            Name = name,
            Range = location.Range,
            Children = [],
            Detail = detail,
#pragma warning disable 618 // obsolete member
            Deprecated = false,
#pragma warning restore 618
            SelectionRange = selection.Range
        };

        if (parent != null)
        {
            var children = parent.Children?.ToList() ?? [];
            children.Add(documentSymbol);
            parent.Children = [.. children];
        }

        return documentSymbol;
    }
}
