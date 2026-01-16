// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Symbols;

public sealed partial class DocumentSymbolsTests
{
    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical(bool mutatingLspWorkspace)
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

                {|operator:static A operator {|operatorSelection:+|}(A a1, A a2) => a1;|}
            }|}|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
        Symbol(LSP.SymbolKind.Namespace, "Test", "Test", "namespace", "namespaceSelection", testLspServer,
            Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Constructor, "A", "A()", "constructor", "constructorSelection", testLspServer),
                Symbol(LSP.SymbolKind.Method, "M", "M()", "method", "methodSelection", testLspServer),
                Symbol(LSP.SymbolKind.Operator, "operator +", "operator +(A a1, A a2)", "operator", "operatorSelection", testLspServer)))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_ClassWithoutName(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|namespace:namespace {|namespaceSelection:NamespaceA|}
            {
                {|class:public class
            {|classSelection:|}|}}|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Namespace, "NamespaceA", "NamespaceA", "namespace", "namespaceSelection", testLspServer,
                Symbol(LSP.SymbolKind.Class, ".", "", "class", "classSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_NamespaceWithoutName(bool mutatingLspWorkspace)
    {
        var markup =
            """

            {|namespace:namespace
            {|namespaceSelection:|}{
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Namespace, ".", "", "namespace", "namespaceSelection", testLspServer)
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_DottedNamespace(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|namespace:namespace {|namespaceSelection:One.Two.Three|}
            {
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Namespace, "One.Two.Three", "One.Two.Three", "namespace", "namespaceSelection", testLspServer)
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_LocalFunction(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:A|}
            {
                {|method:void {|methodSelection:M|}()
                {
                    {|localFunction:int {|localFunctionSelection:LocalFunction|}(string input)
                    {
                        {|nestedLocal:void {|nestedLocalSelection:NestedLocal|}()
                        {
                        }|}
                    }|}
                }|}
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Method, "M() : void", "M() : void", "method", "methodSelection", testLspServer,
                    Symbol(LSP.SymbolKind.Method, "LocalFunction(string) : int", "LocalFunction(string) : int", "localFunction", "localFunctionSelection", testLspServer,
                        Symbol(LSP.SymbolKind.Method, "NestedLocal() : void", "NestedLocal() : void", "nestedLocal", "nestedLocalSelection", testLspServer))))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_NestedNamespace(bool mutatingLspWorkspace)
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

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Namespace, "Outer", "Outer", "outerNamespace", "outerNamespaceSelection", testLspServer,
                Symbol(LSP.SymbolKind.Namespace, "Inner", "Inner", "innerNamespace", "innerNamespaceSelection", testLspServer,
                    Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer)))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_ClassWithoutNamespace(bool mutatingLspWorkspace)
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

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Method, "M", "M()", "method", "methodSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_MultipleTopLevelTypes(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|classA:class {|classASelection:A|}
            {
                {|methodA:void {|methodASelection:M|}()
                {
                }|}
            }|}

            {|classB:class {|classBSelection:B|}
            {
                {|methodB:void {|methodBSelection:N|}()
                {
                }|}
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "A", "A", "classA", "classASelection", testLspServer,
                Symbol(LSP.SymbolKind.Method, "M() : void", "M() : void", "methodA", "methodASelection", testLspServer)),
            Symbol(LSP.SymbolKind.Class, "B", "B", "classB", "classBSelection", testLspServer,
                Symbol(LSP.SymbolKind.Method, "N() : void", "N() : void", "methodB", "methodBSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_NestedType(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:Outer|}
            {
                {|nestedEnum:enum {|nestedEnumSelection:Bar|}
                {
                    {|enumMember:{|enumMemberSelection:None|}|}
                }|}
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "Outer", "Outer", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Enum, "Bar", "Bar", "nestedEnum", "nestedEnumSelection", testLspServer,
                    Symbol(LSP.SymbolKind.EnumMember, "None", "None", "enumMember", "enumMemberSelection", testLspServer)))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_FileScopedNamespace(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|namespace:namespace {|namespaceSelection:Test|};

            {|class:class {|classSelection:A|}
            {
            }|}|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Namespace, "Test", "Test", "namespace", "namespaceSelection", testLspServer,
                Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_Struct(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|struct:struct {|structSelection:MyStruct|}
            {
                public int {|field:{|fieldSelection:Value|}|};
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Struct, "MyStruct", "MyStruct", "struct", "structSelection", testLspServer,
                Symbol(LSP.SymbolKind.Field, "Value : int", "Value : int", "field", "fieldSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_MultipleFieldDeclarations(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:A|}
            {
                public int {|fieldA:{|fieldASelection:a|}|}, {|fieldB:{|fieldBSelection:b|}|}, {|fieldC:{|fieldCSelection:c|}|};
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Field, "a : int", "a : int", "fieldA", "fieldASelection", testLspServer),
                Symbol(LSP.SymbolKind.Field, "b : int", "b : int", "fieldB", "fieldBSelection", testLspServer),
                Symbol(LSP.SymbolKind.Field, "c : int", "c : int", "fieldC", "fieldCSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_MultipleEventFieldDeclarations(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:A|}
            {
                public event System.EventHandler {|eventA:{|eventASelection:A|}|}, {|eventB:{|eventBSelection:B|}|};
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Event, "A : EventHandler", "A : EventHandler", "eventA", "eventASelection", testLspServer),
                Symbol(LSP.SymbolKind.Event, "B : EventHandler", "B : EventHandler", "eventB", "eventBSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_Record(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|record:record {|recordSelection:Person|}(string Name, int Age);|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "Person", "Person", "record", "recordSelection", testLspServer)
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_RecordStruct(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|record:record struct {|recordSelection:Point|}(int X, int Y);|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Struct, "Point", "Point", "record", "recordSelection", testLspServer)
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_Interface(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|interface:interface {|interfaceSelection:IMyInterface|}
            {
                {|method:void {|methodSelection:DoSomething|}();|}
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Interface, "IMyInterface", "IMyInterface", "interface", "interfaceSelection", testLspServer,
                Symbol(LSP.SymbolKind.Method, "DoSomething() : void", "DoSomething() : void", "method", "methodSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_Delegate(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|delegate:delegate void {|delegateSelection:MyDelegate|}(int x);|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Method, "MyDelegate(int) : void", "MyDelegate(int) : void", "delegate", "delegateSelection", testLspServer)
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_Destructor(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:A|}
            {
                {|destructor:~{|destructorSelection:A|}()
                {
                }|}
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Method, "~A()", "~A()", "destructor", "destructorSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_Property(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:A|}
            {
                {|property:public int {|propertySelection:Value|} { get; set; }|}
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Property, "Value : int", "Value : int", "property", "propertySelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_Indexer(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:A|}
            {
                {|indexer:public int {|indexerSelection:this|}[int index] => index;|}
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Property, "this[int] : int", "this[int] : int", "indexer", "indexerSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_Event(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:A|}
            {
                {|event:public event System.EventHandler {|eventSelection:MyEvent|}
                {
                    add { }
                    remove { }
                }|}
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Event, "MyEvent : EventHandler", "MyEvent : EventHandler", "event", "eventSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_EventField(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:A|}
            {
                public event System.EventHandler {|eventField:{|eventFieldSelection:MyEvent|}|};
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Event, "MyEvent : EventHandler", "MyEvent : EventHandler", "eventField", "eventFieldSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_ImplicitOperator(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:A|}
            {
                {|operator:public static implicit operator {|operatorSelection:int|}(A a) => 0;|}
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Operator, "implicit operator int(A)", "implicit operator int(A)", "operator", "operatorSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_ExplicitOperator(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:A|}
            {
                {|operator:public static explicit operator {|operatorSelection:int|}(A a) => 0;|}
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Operator, "explicit operator int(A)", "explicit operator int(A)", "operator", "operatorSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_ConstField(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:A|}
            {
                public const int {|const:{|constSelection:MaxValue|} = 100|};
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Constant, "MaxValue : int", "MaxValue : int", "const", "constSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_GenericClass(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:MyClass|}<T>
            {
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "MyClass<T>", "MyClass<T>", "class", "classSelection", testLspServer)
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_GenericClassMultipleTypeParameters(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:Dictionary|}<TKey, TValue>
            {
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "Dictionary<TKey, TValue>", "Dictionary<TKey, TValue>", "class", "classSelection", testLspServer)
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_GenericMethod(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|class:class {|classSelection:A|}
            {
                {|method:public T {|methodSelection:GetValue|}<T>(T input) => input;|}
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "A", "A", "class", "classSelection", testLspServer,
                Symbol(LSP.SymbolKind.Method, "GetValue<T>(T) : T", "GetValue<T>(T) : T", "method", "methodSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_GenericInterface(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|interface:interface {|interfaceSelection:IRepository|}<T>
            {
                {|method:T {|methodSelection:GetById|}(int id);|}
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Interface, "IRepository<T>", "IRepository<T>", "interface", "interfaceSelection", testLspServer,
                Symbol(LSP.SymbolKind.Method, "GetById(int) : T", "GetById(int) : T", "method", "methodSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_GenericStruct(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|struct:struct {|structSelection:Wrapper|}<T>
            {
                public T {|field:{|fieldSelection:Value|}|};
            }|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Struct, "Wrapper<T>", "Wrapper<T>", "struct", "structSelection", testLspServer,
                Symbol(LSP.SymbolKind.Field, "Value : T", "Value : T", "field", "fieldSelection", testLspServer))
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_GenericRecord(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|record:record {|recordSelection:Result|}<T>(T Value, bool Success);|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Class, "Result<T>", "Result<T>", "record", "recordSelection", testLspServer)
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/7985")]
    public async Task TestGetDocumentSymbolsAsync_Hierarchical_GenericDelegate(bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|delegate:delegate TResult {|delegateSelection:Func|}<T, TResult>(T arg);|}
            """;

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, HierarchicalDocumentSymbolCapabilities);

        LSP.DocumentSymbol[] expected = [
            Symbol(LSP.SymbolKind.Method, "Func<T, TResult>(T) : TResult", "Func<T, TResult>(T) : TResult", "delegate", "delegateSelection", testLspServer)
        ];

        var results = await RunGetDocumentSymbolsAsync<LSP.DocumentSymbol[]>(testLspServer);
        AssertDocumentSymbolsEqual(expected, results);
    }

    private static readonly LSP.ClientCapabilities HierarchicalDocumentSymbolCapabilities = new()
    {
        TextDocument = new LSP.TextDocumentClientCapabilities()
        {
            DocumentSymbol = new LSP.DocumentSymbolSetting()
            {
                HierarchicalDocumentSymbolSupport = true
            }
        }
    };

    private static void AssertDocumentSymbolsEqual(LSP.DocumentSymbol[] expected, LSP.DocumentSymbol[]? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Length, actual.Length);
        for (var i = 0; i < expected.Length; i++)
        {
            AssertDocumentSymbolEquals(expected[i], actual[i]);
        }
    }

    private static void AssertDocumentSymbolEquals(LSP.DocumentSymbol expected, LSP.DocumentSymbol actual)
    {
        Assert.Equal(expected.Kind, actual.Kind);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Detail, actual.Detail);
        Assert.Equal(expected.Range, actual.Range);
        Assert.Equal(expected.SelectionRange, actual.SelectionRange);

        // Verify selection range is contained within the range
        Assert.True(IsPositionBeforeOrEqual(actual.Range.Start, actual.SelectionRange.Start),
            $"SelectionRange start {actual.SelectionRange.Start} should be >= Range start {actual.Range.Start}");
        Assert.True(IsPositionBeforeOrEqual(actual.SelectionRange.End, actual.Range.End),
            $"SelectionRange end {actual.SelectionRange.End} should be <= Range end {actual.Range.End}");

        Assert.Equal(expected.Children?.Length, actual.Children?.Length);
        if (expected.Children is not null)
        {
            for (var i = 0; i < actual.Children!.Length; i++)
            {
                AssertDocumentSymbolEquals(expected.Children[i], actual.Children[i]);
            }
        }
    }

    private static bool IsPositionBeforeOrEqual(LSP.Position a, LSP.Position b)
    {
        return a.Line < b.Line || (a.Line == b.Line && a.Character <= b.Character);
    }

    /// <summary>
    /// Creates a document symbol with range and selection range from markup locations, with optional children.
    /// </summary>
    private static LSP.DocumentSymbol Symbol(
        LSP.SymbolKind kind,
        string name,
        string detail,
        string rangeLocationName,
        string selectionRangeLocationName,
        TestLspServer testLspServer,
        params LSP.DocumentSymbol[] children)
    {
        return new LSP.DocumentSymbol()
        {
            Kind = kind,
            Name = name,
            Detail = detail,
            Range = testLspServer.GetLocations(rangeLocationName).Single().Range,
            SelectionRange = testLspServer.GetLocations(selectionRangeLocationName).Single().Range,
            Children = children,
#pragma warning disable 618 // obsolete member
            Deprecated = false,
#pragma warning restore 618
        };
    }
}
