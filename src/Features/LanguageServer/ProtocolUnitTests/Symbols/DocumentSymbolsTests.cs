// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Symbols
{
    public class DocumentSymbolsTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestGetDocumentSymbolsAsync()
        {
            var markup =
@"{|class:class {|classSelection:A|}
{
    {|method:void {|methodSelection:M|}()
    {
    }|}
}|}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = new LSP.SumType<LSP.DocumentSymbol, LSP.SymbolInformation>[]
            {
                CreateDocumentSymbol(LSP.SymbolKind.Class, "A", "A", locations["class"].Single(), locations["classSelection"].Single())
            };
            CreateDocumentSymbol(LSP.SymbolKind.Method, "M", "M()", locations["method"].Single(), locations["methodSelection"].Single(), expected.First().First);

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__WithoutHierarchicalSupport()
        {
            var markup =
@"class {|class:A|}
{
    void {|method:M|}()
    {
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = new LSP.SumType<LSP.DocumentSymbol, LSP.SymbolInformation>[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Class, "A", locations["class"].Single()),
                CreateSymbolInformation(LSP.SymbolKind.Method, "M()", locations["method"].Single(), "A")
            };

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__WithLocals()
        {
            var markup =
@"{|class:class {|classSelection:A|}
{
    {|method:void {|methodSelection:Method|}()
    {
        {|local:int {|localSelection:i|} = 1;|}
    }|}
}|}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = new LSP.SumType<LSP.DocumentSymbol, LSP.SymbolInformation>[]
            {
                CreateDocumentSymbol(LSP.SymbolKind.Class, "A", "A", locations["class"].Single(), locations["classSelection"].Single())
            };
            var method = CreateDocumentSymbol(LSP.SymbolKind.Method, "Method", "Method()", locations["method"].Single(), locations["methodSelection"].Single(), expected.First().First);
            CreateDocumentSymbol(LSP.SymbolKind.Variable, "i", "i", locations["local"].Single(), locations["localSelection"].Single(), method);

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true).ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__NamespaceSymbolsNotReturned()
        {
            var markup =
@"namespace N
{
    {|class:class {|classSelection:A|}
    {
    }|}
}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = new LSP.SumType<LSP.DocumentSymbol, LSP.SymbolInformation>[]
            {
                CreateDocumentSymbol(LSP.SymbolKind.Class, "A", "N.A", locations["class"].Single(), locations["classSelection"].Single())
            };

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true).ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__NoSymbols()
        {
            using var workspace = CreateTestWorkspace(string.Empty, out var _);

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true).ConfigureAwait(false);
            Assert.Empty(results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__NestedTypes_HierarchicalSupport()
        {
            var markup =
@"{|outer:class {|outerSelection:Outer|}
{
    {|inner1:class {|inner1Selection:Inner1|}
    {
        {|inner2:class {|inner2Selection:Inner2|}
        {
        }|}
    }|}
}|}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var outer = CreateDocumentSymbol(LSP.SymbolKind.Class, "Outer", "Outer", locations["outer"].Single(), locations["outerSelection"].Single());
            var inner1 = CreateDocumentSymbol(LSP.SymbolKind.Class, "Inner1", "Outer.Inner1", locations["inner1"].Single(), locations["inner1Selection"].Single(), outer);
            var inner2 = CreateDocumentSymbol(LSP.SymbolKind.Class, "Inner2", "Outer.Inner1.Inner2", locations["inner2"].Single(), locations["inner2Selection"].Single(), inner1);

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true).ConfigureAwait(false);
            AssertJsonEquals(outer, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__NestedTypes_NoHierarchicalSupport()
        {
            var markup =
@"class {|outer:Outer|}
{
    class {|inner1:Inner1|}
    {
        class {|inner2:Inner2|}
        {
        }
    }
}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = new LSP.SumType<LSP.DocumentSymbol, LSP.SymbolInformation>[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Class, "Outer", locations["outer"].Single()),
                CreateSymbolInformation(LSP.SymbolKind.Class, "Outer.Inner1", locations["inner1"].Single()),
                CreateSymbolInformation(LSP.SymbolKind.Class, "Outer.Inner1.Inner2", locations["inner2"].Single())
            };

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, false).ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__LocalFunctions()
        {
            var markup =
@"{|class:class {|classSelection:A|}
{
    {|method:void {|methodSelection:M|}()
    {
        {|localFunction:void {|localFunctionSelection:LocalFunction|}()
        {
        }|}
    }|}
}|}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = new LSP.SumType<LSP.DocumentSymbol, LSP.SymbolInformation>[]
            {
                CreateDocumentSymbol(LSP.SymbolKind.Class, "A", "A", locations["class"].Single(), locations["classSelection"].Single())
            };
            var m = CreateDocumentSymbol(LSP.SymbolKind.Method, "M", "M()", locations["method"].Single(), locations["methodSelection"].Single(), expected.First().First);
            CreateDocumentSymbol(LSP.SymbolKind.Method, "LocalFunction", "LocalFunction()", locations["localFunction"].Single(), locations["localFunctionSelection"].Single(), m);

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true).ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__Fields_HierarchicalSupport()
        {
            var markup =
@"{|a:class {|aSelection:A|}
{
    {|i1:private int {|i1Selection:i1|} = 1;|}
    {|b:class {|bSelection:B|}
    {
        {|i2:private int {|i2Selection:i2|};|}
    }|}

    {|i4:private int {|i4Selection:i4|}|}, {|i5:{|i5Selection:i5|};|}
}|}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var a = CreateDocumentSymbol(LSP.SymbolKind.Class, "A", "A", locations["a"].Single(), locations["aSelection"].Single());
            CreateDocumentSymbol(LSP.SymbolKind.Field, "i1", "i1", locations["i1"].Single(), locations["i1Selection"].Single(), a);
            var b = CreateDocumentSymbol(LSP.SymbolKind.Class, "B", "A.B", locations["b"].Single(), locations["bSelection"].Single(), a);
            CreateDocumentSymbol(LSP.SymbolKind.Field, "i2", "i2", locations["i2"].Single(), locations["i2Selection"].Single(), b);
            CreateDocumentSymbol(LSP.SymbolKind.Field, "i4", "i4", locations["i4"].Single(), locations["i4Selection"].Single(), a);
            CreateDocumentSymbol(LSP.SymbolKind.Field, "i5", "i5", locations["i5"].Single(), locations["i5Selection"].Single(), a);

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true);
            AssertJsonEquals(a, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__Fields_NoHierarchicalSupport()
        {
            var markup =
@"class {|a:A|}
{
    private int {|i1:i1|} = 1;
    class {|b:B|}
    {
        private int {|i2:i2|};
    }
}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = new LSP.SumType<LSP.DocumentSymbol, LSP.SymbolInformation>[]
            {
                CreateSymbolInformation(LSP.SymbolKind.Class, "A", locations["a"].Single()),
                CreateSymbolInformation(LSP.SymbolKind.Field, "i1", locations["i1"].Single(), "A"),
                CreateSymbolInformation(LSP.SymbolKind.Class, "A.B", locations["b"].Single()),
                CreateSymbolInformation(LSP.SymbolKind.Field, "i2", locations["i2"].Single(), "A.B"),
            };

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__Enums_HierarchicalSupport()
        {
            var markup =
@"{|e:enum {|eSelection:E|}
{
    {|e1:{|e1Selection:Element1|},|} {|e2:{|e2Selection:Element2|},|}
    {|e3:{|e3Selection:Element3|} = 3|}
}|}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var e = CreateDocumentSymbol(LSP.SymbolKind.Enum, "E", "E", locations["e"].Single(), locations["eSelection"].Single());
            CreateDocumentSymbol(LSP.SymbolKind.EnumMember, "Element1", "Element1", locations["e1"].Single(), locations["e1Selection"].Single(), e);
            CreateDocumentSymbol(LSP.SymbolKind.EnumMember, "Element2", "Element2", locations["e2"].Single(), locations["e2Selection"].Single(), e);
            CreateDocumentSymbol(LSP.SymbolKind.EnumMember, "Element3", "Element3", locations["e3"].Single(), locations["e3Selection"].Single(), e);

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true);
            AssertJsonEquals(e, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__TopLevelStatements()
        {
            var markup =
@"{|a:var {|aSelection:a|} = 1;|}
{|m:static void {|mSelection:M|}()
{
    {|b:int {|bSelection:b|};|}
}|}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var a = CreateDocumentSymbol(LSP.SymbolKind.Variable, "a", "a", locations["a"].Single(), locations["aSelection"].Single());
            var m = CreateDocumentSymbol(LSP.SymbolKind.Method, "M", "M()", locations["m"].Single(), locations["mSelection"].Single());
            CreateDocumentSymbol(LSP.SymbolKind.Variable, "b", "b", locations["b"].Single(), locations["bSelection"].Single(), m);

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true);
            AssertJsonEquals(new LSP.SumType<LSP.DocumentSymbol, LSP.SymbolInformation>[] { a, m }, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__Constructor()
        {
            var markup =
@"{|c:class {|cSelection:C|}
{
    {|constructor:public {|constructorSelection:C|}()
    {
    }|}
}|}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var c = CreateDocumentSymbol(LSP.SymbolKind.Class, "C", "C", locations["c"].Single(), locations["cSelection"].Single());
            CreateDocumentSymbol(LSP.SymbolKind.Constructor, ".ctor", "C()", locations["constructor"].Single(), locations["constructorSelection"].Single(), c);

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true);
            AssertJsonEquals(c, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__InterfaceType()
        {
            var markup =
@"{|i:interface {|iSelection:I|}
{
}|}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var i = CreateDocumentSymbol(LSP.SymbolKind.Interface, "I", "I", locations["i"].Single(), locations["iSelection"].Single());

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true);
            AssertJsonEquals(i, results);
        }

        [Fact]
        public async Task TetGetDocumentSymbolsAsync__TypeParameters()
        {
            var markup =
@"{|c:class {|cSelection:C|}<{|t1:T1|}>
{
    {|m:void {|mSelection:M|}<{|t2:T2|}>()
    {
    }|}
}|}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var c = CreateDocumentSymbol(LSP.SymbolKind.Class, "C", "C<T1>", locations["c"].Single(), locations["cSelection"].Single());
            CreateDocumentSymbol(LSP.SymbolKind.TypeParameter, "T1", "T1", locations["t1"].Single(), locations["t1"].Single(), c);
            var m = CreateDocumentSymbol(LSP.SymbolKind.Method, "M", "M<T2>()", locations["m"].Single(), locations["mSelection"].Single(), c);
            CreateDocumentSymbol(LSP.SymbolKind.TypeParameter, "T2", "T2", locations["t2"].Single(), locations["t2"].Single(), m);

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true);
            AssertJsonEquals(c, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__StructType()
        {
            var markup =
@"{|s:struct {|sSelection:S|}
{
}|}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var s = CreateDocumentSymbol(LSP.SymbolKind.Struct, "S", "S", locations["s"].Single(), locations["sSelection"].Single());

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true);
            AssertJsonEquals(s, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__Properties()
        {
            var markup =
@"{|c:class {|cSelection:C|}
{
    {|prop1:public int {|prop1Selection:Prop1|} => 1;|}
    {|prop2:public int {|prop2Selection:Prop2|} { get; set; } = 2;|}
    {|prop3:public int {|prop3Selection:Prop3|}
    {
        get
        {
            return 3;
        }
        set
        {
            _ = value;
        }
    }|}
}|}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var c = CreateDocumentSymbol(LSP.SymbolKind.Class, "C", "C", locations["c"].Single(), locations["cSelection"].Single());
            CreateDocumentSymbol(LSP.SymbolKind.Property, "Prop1", "Prop1", locations["prop1"].Single(), locations["prop1Selection"].Single(), c);
            CreateDocumentSymbol(LSP.SymbolKind.Property, "Prop2", "Prop2", locations["prop2"].Single(), locations["prop2Selection"].Single(), c);
            CreateDocumentSymbol(LSP.SymbolKind.Property, "Prop3", "Prop3", locations["prop3"].Single(), locations["prop3Selection"].Single(), c);

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true);
            AssertJsonEquals(c, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__Operators()
        {
            var markup =
@"{|c:class {|cSelection:C|}
{
    {|operator:public static C operator{|operatorSelection:+|}(C c1, C c2) => null;|}
}|}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var c = CreateDocumentSymbol(LSP.SymbolKind.Class, "C", "C", locations["c"].Single(), locations["cSelection"].Single());
            CreateDocumentSymbol(LSP.SymbolKind.Operator, "op_Addition", "operator +(C c1, C c2)", locations["operator"].Single(), locations["operatorSelection"].Single(), c);

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true);
            AssertJsonEquals(c, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync_Constants()
        {
            var markup =
@"{|c:class {|cSelection:C|}
{
    {|c1:const int {|c1Selection:C1|} = 1;|}

    {|m:void {|mSelection:M|}()
    {
        {|c2:const int {|c2Selection:C2|} = 2;|}
    }|}
}|}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var c = CreateDocumentSymbol(LSP.SymbolKind.Class, "C", "C", locations["c"].Single(), locations["cSelection"].Single());
            CreateDocumentSymbol(LSP.SymbolKind.Constant, "C1", "C1", locations["c1"].Single(), locations["c1Selection"].Single(), c);
            var m = CreateDocumentSymbol(LSP.SymbolKind.Method, "M", "M()", locations["m"].Single(), locations["mSelection"].Single(), c);
            CreateDocumentSymbol(LSP.SymbolKind.Constant, "C2", "C2", locations["c2"].Single(), locations["c2Selection"].Single(), m);

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true);
            AssertJsonEquals(c, results);
        }

        [Fact]
        public async Task TestGetDocumentSymbolsAsync__PartialMethods()
        {
            var markup =
@"{|c1P1:partial class {|c1P1Selection:C1|}
{
    {|m1P1:partial void {|m1P1Selection:M1|}();|}
}|}
{|c1P2:partial class {|c1P2Selection:C1|}
{
    {|m1P2:partial void {|m1P2Selection:M1|}() {}|}
}|}";

            using var workspace = CreateTestWorkspace(markup, out var locations);
            var c1P1 = CreateDocumentSymbol(LSP.SymbolKind.Class, "C1", "C1", locations["c1P1"].Single(), locations["c1P1Selection"].Single());
            CreateDocumentSymbol(LSP.SymbolKind.Method, "M1", "M1()", locations["m1P1"].Single(), locations["m1P1Selection"].Single(), c1P1);

            // TODO: Decide how to handle this scenario. Should we deduplicate the results in the features result, or in the symbol handler?
            var c1P2 = CreateDocumentSymbol(LSP.SymbolKind.Class, "C1", "C1", locations["c1P1"].Single(), locations["c1P1Selection"].Single());
            CreateDocumentSymbol(LSP.SymbolKind.Method, "M1", "M1()", locations["m1P2"].Single(), locations["m1P2Selection"].Single(), c1P2);

            var results = await RunGetDocumentSymbolsAsync(workspace.CurrentSolution, true);
            AssertJsonEquals(new LSP.SumType<LSP.DocumentSymbol, LSP.SymbolInformation>[] { c1P1, c1P2 }, results);
        }

        private static async Task<LSP.SumType<LSP.DocumentSymbol, LSP.SymbolInformation>[]> RunGetDocumentSymbolsAsync(Solution solution, bool hierarchicalSupport)
        {
            var document = solution.Projects.First().Documents.First();
            var request = new LSP.DocumentSymbolParams
            {
                TextDocument = CreateTextDocumentIdentifier(new Uri(document.FilePath))
            };

            var clientCapabilities = new LSP.ClientCapabilities()
            {
                TextDocument = new LSP.TextDocumentClientCapabilities()
                {
                    DocumentSymbol = new LSP.DocumentSymbolSetting()
                    {
                        HierarchicalDocumentSymbolSupport = hierarchicalSupport
                    }
                }
            };

            var queue = CreateRequestQueue(solution);
            return await GetLanguageServer(solution).ExecuteRequestAsync<LSP.DocumentSymbolParams, LSP.SumType<LSP.DocumentSymbol, LSP.SymbolInformation>[]>(queue, LSP.Methods.TextDocumentDocumentSymbolName,
                request, clientCapabilities, null, CancellationToken.None);
        }

        private static LSP.DocumentSymbol CreateDocumentSymbol(LSP.SymbolKind kind, string name, string detail,
            LSP.Location location, LSP.Location selection, LSP.DocumentSymbol parent = null)
        {
            var documentSymbol = new LSP.DocumentSymbol()
            {
                Kind = kind,
                Name = name,
                Range = location.Range,
                Children = new LSP.DocumentSymbol[0],
                Detail = detail,
                Deprecated = false,
                SelectionRange = selection.Range
            };

            if (parent != null)
            {
                var children = parent.Children.ToList();
                children.Add(documentSymbol);
                parent.Children = children.ToArray();
            }

            return documentSymbol;
        }

        private static void AssertJsonEquals(LSP.DocumentSymbol expected, LSP.SumType<LSP.DocumentSymbol, LSP.SymbolInformation>[] actual)
            => AssertJsonEquals(new LSP.SumType<LSP.DocumentSymbol, LSP.SymbolInformation>[] { expected }, actual);
    }
}
