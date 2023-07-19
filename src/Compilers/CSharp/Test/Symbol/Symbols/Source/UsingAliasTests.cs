// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Source
{
    public class UsingAliasTests : SemanticModelTestBase
    {
        [Fact]
        public void GetSemanticInfo()
        {
            var text =
@"using O = System.Object;

partial class A : O {}
partial class A : object {}
partial class A : System.Object {}
partial class A : Object {}
";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot();
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var a1 = root.Members[0] as TypeDeclarationSyntax;
            var a2 = root.Members[1] as TypeDeclarationSyntax;
            var a3 = root.Members[2] as TypeDeclarationSyntax;
            var a4 = root.Members[3] as TypeDeclarationSyntax;

            var base1 = a1.BaseList.Types[0].Type;
            var base2 = a2.BaseList.Types[0].Type;
            var base3 = a3.BaseList.Types[0].Type;
            var base4 = a4.BaseList.Types[0].Type;

            var model = comp.GetSemanticModel(tree);

            var info1 = model.GetSemanticInfoSummary(base1);
            Assert.NotNull(info1.Symbol);
            var alias1 = model.GetAliasInfo((IdentifierNameSyntax)base1);
            Assert.NotNull(alias1);
            Assert.Equal(SymbolKind.Alias, alias1.Kind);
            Assert.Equal("O", alias1.ToDisplayString());
            Assert.Equal("O=System.Object", alias1.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            Assert.Equal(info1.Symbol, alias1.Target);

            var info2 = model.GetSemanticInfoSummary(base2);
            Assert.NotNull(info2.Symbol);
            var b2 = info2.Symbol;
            Assert.Equal("System.Object", b2.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            Assert.Equal("System.Object", info2.Type.ToDisplayString(format: SymbolDisplayFormat.TestFormat));

            var info3 = model.GetSemanticInfoSummary(base3);
            Assert.NotNull(info3.Symbol);
            var b3 = info3.Symbol;
            Assert.Equal("System.Object", b3.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            Assert.Equal("System.Object", info3.Type.ToDisplayString(format: SymbolDisplayFormat.TestFormat));

            var info4 = model.GetSemanticInfoSummary(base4);
            Assert.Null(info4.Symbol); // no "using System;"
            Assert.Equal(0, info4.CandidateSymbols.Length);
            var alias4 = model.GetAliasInfo((IdentifierNameSyntax)base4);
            Assert.Null(alias4);
        }

        [Fact]
        public void GetSemanticInfo_PrimitiveType()
        {
            var text =
@"using O = object;

partial class A : O {}
partial class A : object {}
partial class A : System.Object {}
partial class A : Object {}
";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot();
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var a1 = root.Members[0] as TypeDeclarationSyntax;
            var a2 = root.Members[1] as TypeDeclarationSyntax;
            var a3 = root.Members[2] as TypeDeclarationSyntax;
            var a4 = root.Members[3] as TypeDeclarationSyntax;

            var base1 = a1.BaseList.Types[0].Type;
            var base2 = a2.BaseList.Types[0].Type;
            var base3 = a3.BaseList.Types[0].Type;
            var base4 = a4.BaseList.Types[0].Type;

            var model = comp.GetSemanticModel(tree);

            var usingAliasType = model.GetTypeInfo(usingAlias.NamespaceOrType).Type;
            Assert.Equal(SpecialType.System_Object, usingAliasType.SpecialType);

            var info1 = model.GetSemanticInfoSummary(base1);
            Assert.NotNull(info1.Symbol);
            var alias1 = model.GetAliasInfo((IdentifierNameSyntax)base1);
            Assert.NotNull(alias1);
            Assert.Equal(SymbolKind.Alias, alias1.Kind);
            Assert.Equal("O", alias1.ToDisplayString());
            Assert.Equal("O=System.Object", alias1.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            Assert.Equal(info1.Symbol, alias1.Target);

            var info2 = model.GetSemanticInfoSummary(base2);
            Assert.NotNull(info2.Symbol);
            var b2 = info2.Symbol;
            Assert.Equal("System.Object", b2.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            Assert.Equal("System.Object", info2.Type.ToDisplayString(format: SymbolDisplayFormat.TestFormat));

            var info3 = model.GetSemanticInfoSummary(base3);
            Assert.NotNull(info3.Symbol);
            var b3 = info3.Symbol;
            Assert.Equal("System.Object", b3.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            Assert.Equal("System.Object", info3.Type.ToDisplayString(format: SymbolDisplayFormat.TestFormat));

            var info4 = model.GetSemanticInfoSummary(base4);
            Assert.Null(info4.Symbol); // no "using System;"
            Assert.Equal(0, info4.CandidateSymbols.Length);
            var alias4 = model.GetAliasInfo((IdentifierNameSyntax)base4);
            Assert.Null(alias4);
        }

        [Fact]
        public void GetSymbolInfoInParent()
        {
            var text =
@"using O = System.Object;

partial class A : O {}
partial class A : object {}
partial class A : System.Object {}
partial class A : Object {}
";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot();
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var a1 = root.Members[0] as TypeDeclarationSyntax;
            var a2 = root.Members[1] as TypeDeclarationSyntax;
            var a3 = root.Members[2] as TypeDeclarationSyntax;
            var a4 = root.Members[3] as TypeDeclarationSyntax;

            var base1 = a1.BaseList.Types[0].Type;
            var base2 = a2.BaseList.Types[0].Type;
            var base3 = a3.BaseList.Types[0].Type;
            var base4 = a4.BaseList.Types[0].Type;

            var model = comp.GetSemanticModel(tree);

            var info1 = model.GetSemanticInfoSummary(base1);
            Assert.Equal("System.Object", info1.Type.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            var alias1 = model.GetAliasInfo((IdentifierNameSyntax)base1);
            Assert.NotNull(alias1);
            Assert.Equal(SymbolKind.Alias, alias1.Kind);
            Assert.Equal("O=System.Object", alias1.ToDisplayString(format: SymbolDisplayFormat.TestFormat));

            var info2 = model.GetSemanticInfoSummary(base2);
            Assert.NotNull(info2.Symbol);
            var b2 = info2.Symbol;
            Assert.Equal("System.Object", b2.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            Assert.Equal("System.Object", info2.Type.ToDisplayString(format: SymbolDisplayFormat.TestFormat));

            var info3 = model.GetSemanticInfoSummary(base3);
            Assert.NotNull(info3.Symbol);
            var b3 = info3.Symbol;
            Assert.Equal("System.Object", b3.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            Assert.Equal("System.Object", info3.Type.ToDisplayString(format: SymbolDisplayFormat.TestFormat));

            var info4 = model.GetSemanticInfoSummary(base4);
            Assert.Null(info4.Symbol); // no "using System;"
            Assert.Equal(0, info4.CandidateSymbols.Length);
            var alias4 = model.GetAliasInfo((IdentifierNameSyntax)base4);
            Assert.Null(alias4);
        }

        [Fact]
        public void GetSymbolInfoInParent_Primitive()
        {
            var text =
@"using O = object;

partial class A : O {}
partial class A : object {}
partial class A : System.Object {}
partial class A : Object {}
";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot();
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var a1 = root.Members[0] as TypeDeclarationSyntax;
            var a2 = root.Members[1] as TypeDeclarationSyntax;
            var a3 = root.Members[2] as TypeDeclarationSyntax;
            var a4 = root.Members[3] as TypeDeclarationSyntax;

            var base1 = a1.BaseList.Types[0].Type;
            var base2 = a2.BaseList.Types[0].Type;
            var base3 = a3.BaseList.Types[0].Type;
            var base4 = a4.BaseList.Types[0].Type;

            var model = comp.GetSemanticModel(tree);

            var usingAliasType = model.GetTypeInfo(usingAlias.NamespaceOrType).Type;
            Assert.Equal(SpecialType.System_Object, usingAliasType.SpecialType);

            var info1 = model.GetSemanticInfoSummary(base1);
            Assert.Equal("System.Object", info1.Type.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            var alias1 = model.GetAliasInfo((IdentifierNameSyntax)base1);
            Assert.NotNull(alias1);
            Assert.Equal(SymbolKind.Alias, alias1.Kind);
            Assert.Equal("O=System.Object", alias1.ToDisplayString(format: SymbolDisplayFormat.TestFormat));

            var info2 = model.GetSemanticInfoSummary(base2);
            Assert.NotNull(info2.Symbol);
            var b2 = info2.Symbol;
            Assert.Equal("System.Object", b2.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            Assert.Equal("System.Object", info2.Type.ToDisplayString(format: SymbolDisplayFormat.TestFormat));

            var info3 = model.GetSemanticInfoSummary(base3);
            Assert.NotNull(info3.Symbol);
            var b3 = info3.Symbol;
            Assert.Equal("System.Object", b3.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            Assert.Equal("System.Object", info3.Type.ToDisplayString(format: SymbolDisplayFormat.TestFormat));

            var info4 = model.GetSemanticInfoSummary(base4);
            Assert.Null(info4.Symbol); // no "using System;"
            Assert.Equal(0, info4.CandidateSymbols.Length);
            var alias4 = model.GetAliasInfo((IdentifierNameSyntax)base4);
            Assert.Null(alias4);
        }

        [Theory]
        [InlineData("(int a, int b)", "(System.Int32 a, System.Int32 b)")]
        [InlineData("int[]", "System.Int32[]")]
        [InlineData("int?", "System.Int32?")]
        [InlineData("int*", "System.Int32*")]
        [InlineData("delegate*<int,int>", "delegate*<System.Int32, System.Int32>")]
        [InlineData("dynamic", "dynamic")]
        [InlineData("nint", "nint")]
        public void GetAliasTypeInfo(string aliasType, string expected)
        {
            // Should get the same results in the semantic model regardless of whether the using has the 'unsafe'
            // keyword or not.
            getAliasTypeInfoHelper("");
            getAliasTypeInfoHelper("unsafe");

            void getAliasTypeInfoHelper(string unsafeString)
            {
                var text = $"using {unsafeString} O = {aliasType};";
                var tree = Parse(text);
                var root = tree.GetCompilationUnitRoot();
                var comp = CreateCompilation(tree);

                var usingAlias = root.Usings[0];

                var model = comp.GetSemanticModel(tree);

                var usingAliasType = model.GetTypeInfo(usingAlias.NamespaceOrType).Type;
                AssertEx.Equal(expected, usingAliasType.ToDisplayString(SymbolDisplayFormat.TestFormat));

                var alias = model.GetDeclaredSymbol(usingAlias);
                Assert.Equal(alias.Target, usingAliasType);
            }
        }

        [Fact]
        public void BindType()
        {
            var text =
@"using O = System.Object;

partial class A : O {}
partial class A : object {}
partial class A : System.Object {}
partial class A : Object {}
";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot();
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var a1 = root.Members[0] as TypeDeclarationSyntax;
            var a2 = root.Members[1] as TypeDeclarationSyntax;
            var a3 = root.Members[2] as TypeDeclarationSyntax;
            var a4 = root.Members[3] as TypeDeclarationSyntax;

            var base1 = a1.BaseList.Types[0].Type;
            var base2 = a2.BaseList.Types[0].Type;
            var base3 = a3.BaseList.Types[0].Type;
            var base4 = a4.BaseList.Types[0].Type;

            var model = comp.GetSemanticModel(tree);

            var symbolInfo = model.GetSpeculativeSymbolInfo(base2.SpanStart, base2, SpeculativeBindingOption.BindAsTypeOrNamespace);
            var info2 = symbolInfo.Symbol as ITypeSymbol;
            Assert.NotNull(info2);
            Assert.Equal("System.Object", info2.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            Assert.Equal("System.Object", info2.ToDisplayString(format: SymbolDisplayFormat.TestFormat));

            symbolInfo = model.GetSpeculativeSymbolInfo(base3.SpanStart, base3, SpeculativeBindingOption.BindAsTypeOrNamespace);
            var info3 = symbolInfo.Symbol as ITypeSymbol;
            Assert.NotNull(info3);
            Assert.Equal("System.Object", info3.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            Assert.Equal("System.Object", info3.ToDisplayString(format: SymbolDisplayFormat.TestFormat));

            symbolInfo = model.GetSpeculativeSymbolInfo(base4.SpanStart, base4, SpeculativeBindingOption.BindAsTypeOrNamespace);
            var info4 = symbolInfo.Symbol as ITypeSymbol;
            Assert.Null(info4); // no "using System;"
        }

        [Fact]
        public void BindType_Primitive()
        {
            var text =
@"using O = object;

partial class A : O {}
partial class A : object {}
partial class A : System.Object {}
partial class A : Object {}
";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot();
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var a1 = root.Members[0] as TypeDeclarationSyntax;
            var a2 = root.Members[1] as TypeDeclarationSyntax;
            var a3 = root.Members[2] as TypeDeclarationSyntax;
            var a4 = root.Members[3] as TypeDeclarationSyntax;

            var base1 = a1.BaseList.Types[0].Type;
            var base2 = a2.BaseList.Types[0].Type;
            var base3 = a3.BaseList.Types[0].Type;
            var base4 = a4.BaseList.Types[0].Type;

            var model = comp.GetSemanticModel(tree);

            var symbolInfo = model.GetSpeculativeSymbolInfo(base2.SpanStart, base2, SpeculativeBindingOption.BindAsTypeOrNamespace);
            var info2 = symbolInfo.Symbol as ITypeSymbol;
            Assert.NotNull(info2);
            Assert.Equal("System.Object", info2.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            Assert.Equal("System.Object", info2.ToDisplayString(format: SymbolDisplayFormat.TestFormat));

            symbolInfo = model.GetSpeculativeSymbolInfo(base3.SpanStart, base3, SpeculativeBindingOption.BindAsTypeOrNamespace);
            var info3 = symbolInfo.Symbol as ITypeSymbol;
            Assert.NotNull(info3);
            Assert.Equal("System.Object", info3.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            Assert.Equal("System.Object", info3.ToDisplayString(format: SymbolDisplayFormat.TestFormat));

            symbolInfo = model.GetSpeculativeSymbolInfo(base4.SpanStart, base4, SpeculativeBindingOption.BindAsTypeOrNamespace);
            var info4 = symbolInfo.Symbol as ITypeSymbol;
            Assert.Null(info4); // no "using System;"
        }

        [Fact]
        public void GetDeclaredSymbol01()
        {
            var text =
@"using O = System.Object;
";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot();
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var model = comp.GetSemanticModel(tree);

            var alias = model.GetDeclaredSymbol(usingAlias);
            Assert.Equal("O", alias.ToDisplayString());
            Assert.Equal("O=System.Object", alias.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            var global = (INamespaceSymbol)alias.ContainingSymbol;
            Assert.Equal(NamespaceKind.Module, global.NamespaceKind);
        }

        [Fact]
        public void GetDeclaredSymbol01_Primitive()
        {
            var text =
@"using O = object;
";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot();
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var model = comp.GetSemanticModel(tree);

            var alias = model.GetDeclaredSymbol(usingAlias);
            Assert.Equal("O", alias.ToDisplayString());
            Assert.Equal("O=System.Object", alias.ToDisplayString(format: SymbolDisplayFormat.TestFormat));
            var global = (INamespaceSymbol)alias.ContainingSymbol;
            Assert.Equal(NamespaceKind.Module, global.NamespaceKind);
        }

        [Fact]
        public void GetDeclaredSymbol02()
        {
            var text = "using System;";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot();
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var model = comp.GetSemanticModel(tree);

            var alias = model.GetDeclaredSymbol(usingAlias);
            Assert.Null(alias);
        }

        [Fact]
        public void LookupNames()
        {
            var text =
@"using O = System.Object;
class C {}
";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot();
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var model = comp.GetSemanticModel(tree);

            var names = model.LookupNames(root.Members[0].SpanStart);
            Assert.Contains("O", names);
        }

        [Fact]
        public void LookupNames_Primitive()
        {
            var text =
@"using O = object;
class C {}
";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot();
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var model = comp.GetSemanticModel(tree);

            var names = model.LookupNames(root.Members[0].SpanStart);
            Assert.Contains("O", names);
        }

        [Fact]
        public void LookupSymbols()
        {
            var text =
@"using O = System.Object;
class C {}
";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot();
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var model = comp.GetSemanticModel(tree);

            var symbols = model.LookupSymbols(root.Members[0].SpanStart, name: "O");
            Assert.Equal(1, symbols.Length);
            Assert.Equal(SymbolKind.Alias, symbols[0].Kind);
            Assert.Equal("O=System.Object", symbols[0].ToDisplayString(format: SymbolDisplayFormat.TestFormat));
        }

        [Fact]
        public void LookupSymbols_Primitive()
        {
            var text =
@"using O = object;
class C {}
";
            var tree = Parse(text);
            var root = tree.GetCompilationUnitRoot();
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var model = comp.GetSemanticModel(tree);

            var symbols = model.LookupSymbols(root.Members[0].SpanStart, name: "O");
            Assert.Equal(1, symbols.Length);
            Assert.Equal(SymbolKind.Alias, symbols[0].Kind);
            Assert.Equal("O=System.Object", symbols[0].ToDisplayString(format: SymbolDisplayFormat.TestFormat));
        }

        [WorkItem(537401, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537401")]
        [Fact]
        public void EventEscapedIdentifier()
        {
            var text = @"
using @for = @foreach;
namespace @foreach { }
";
            SyntaxTree syntaxTree = Parse(text);
            CSharpCompilation comp = CreateCompilation(syntaxTree);
            UsingDirectiveSyntax usingAlias = syntaxTree.GetCompilationUnitRoot().Usings.First();
            var alias = comp.GetSemanticModel(syntaxTree).GetDeclaredSymbol(usingAlias);
            Assert.Equal("for", alias.Name);
            Assert.Equal("@for", alias.ToString());
        }

        [WorkItem(541937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541937")]
        [Fact]
        public void LocalDeclaration()
        {
            var text = @"
using GIBBERISH = System.Int32;
class Program
{
    static void Main()
    {
        /*<bind>*/GIBBERISH/*</bind>*/ x;
    }
}";
            SyntaxTree syntaxTree = Parse(text);
            CSharpCompilation comp = CreateCompilation(syntaxTree);
            var model = comp.GetSemanticModel(syntaxTree);
            IdentifierNameSyntax exprSyntaxToBind = (IdentifierNameSyntax)GetExprSyntaxForBinding(GetExprSyntaxList(syntaxTree));
            Assert.Equal(SymbolKind.Alias, model.GetAliasInfo(exprSyntaxToBind).Kind);
        }

        [WorkItem(541937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541937")]
        [Fact]
        public void LocalDeclaration_Primitive()
        {
            var text = @"
using GIBBERISH = int;
class Program
{
    static void Main()
    {
        /*<bind>*/GIBBERISH/*</bind>*/ x;
    }
}";
            SyntaxTree syntaxTree = Parse(text);
            CSharpCompilation comp = CreateCompilation(syntaxTree);
            var model = comp.GetSemanticModel(syntaxTree);
            IdentifierNameSyntax exprSyntaxToBind = (IdentifierNameSyntax)GetExprSyntaxForBinding(GetExprSyntaxList(syntaxTree));
            Assert.Equal(SymbolKind.Alias, model.GetAliasInfo(exprSyntaxToBind).Kind);
        }

        [Fact, WorkItem(541937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541937")]
        public void LocalDeclaration_Array()
        {
            var text = @"
using GIBBERISH = int[];
class Program
{
    static void Main()
    {
        /*<bind>*/GIBBERISH/*</bind>*/ x;
    }
}";
            SyntaxTree syntaxTree = Parse(text);
            CSharpCompilation comp = CreateCompilation(syntaxTree);
            var model = comp.GetSemanticModel(syntaxTree);
            IdentifierNameSyntax exprSyntaxToBind = (IdentifierNameSyntax)GetExprSyntaxForBinding(GetExprSyntaxList(syntaxTree));
            Assert.Equal(SymbolKind.Alias, model.GetAliasInfo(exprSyntaxToBind).Kind);
            Assert.Equal("System.Int32[]", model.GetAliasInfo(exprSyntaxToBind).Target.ToTestDisplayString());
        }

        [Fact, WorkItem(541937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541937")]
        public void LocalDeclaration_Tuple()
        {
            var text = @"
using GIBBERISH = (int, int);
class Program
{
    static void Main()
    {
        /*<bind>*/GIBBERISH/*</bind>*/ x;
    }
}";
            SyntaxTree syntaxTree = Parse(text);
            CSharpCompilation comp = CreateCompilation(syntaxTree);
            var model = comp.GetSemanticModel(syntaxTree);
            IdentifierNameSyntax exprSyntaxToBind = (IdentifierNameSyntax)GetExprSyntaxForBinding(GetExprSyntaxList(syntaxTree));
            Assert.Equal(SymbolKind.Alias, model.GetAliasInfo(exprSyntaxToBind).Kind);
            Assert.Equal("(System.Int32, System.Int32)", model.GetAliasInfo(exprSyntaxToBind).Target.ToTestDisplayString());
        }

        [WorkItem(576809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576809")]
        [Fact]
        public void AsClause()
        {
            var text = @"
using N = System.Nullable<int>;
 
class Program
{
    static void Main()
    {
        object x = 1;
        var y = x as /*<bind>*/N/*</bind>*/ + 1;
    }
}
";
            SyntaxTree syntaxTree = Parse(text);
            CSharpCompilation comp = CreateCompilation(syntaxTree);
            var model = comp.GetSemanticModel(syntaxTree);
            IdentifierNameSyntax exprSyntaxToBind = (IdentifierNameSyntax)GetExprSyntaxForBinding(GetExprSyntaxList(syntaxTree));
            Assert.Equal("System.Int32?", model.GetAliasInfo(exprSyntaxToBind).Target.ToTestDisplayString());
        }

        [WorkItem(576809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576809")]
        [Fact]
        public void AsClause_Nullable()
        {
            var text = @"
using N = int?;
 
class Program
{
    static void Main()
    {
        object x = 1;
        var y = x as /*<bind>*/N/*</bind>*/ + 1;
    }
}
";
            SyntaxTree syntaxTree = Parse(text);
            CSharpCompilation comp = CreateCompilation(syntaxTree);
            var model = comp.GetSemanticModel(syntaxTree);
            IdentifierNameSyntax exprSyntaxToBind = (IdentifierNameSyntax)GetExprSyntaxForBinding(GetExprSyntaxList(syntaxTree));
            Assert.Equal("System.Int32?", model.GetAliasInfo(exprSyntaxToBind).Target.ToTestDisplayString());
        }

        [WorkItem(542552, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542552")]
        [Fact]
        public void IncompleteDuplicateAlias()
        {
            var text = @"namespace namespace1 { }
namespace namespace2 { }
namespace prog
{
    using ns = namespace1;
    using ns =";
            SyntaxTree syntaxTree = Parse(text);
            CSharpCompilation comp = CreateCompilation(syntaxTree);
            var discarded = comp.GetDiagnostics();
        }

        [ClrOnlyFact, WorkItem(2805, "https://github.com/dotnet/roslyn/issues/2805")]
        public void AliasWithAnError()
        {
            var text =
@"
namespace NS
{
    using Short = LongNamespace;
    class Test
    {
        public object Method1()
        {
            return (new Short.MyClass()).Prop;
        }
    }
}";

            var compilation = CreateCompilation(text);

            compilation.VerifyDiagnostics(
    // (4,19): error CS0246: The type or namespace name 'LongNamespace' could not be found (are you missing a using directive or an assembly reference?)
    //     using Short = LongNamespace;
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "LongNamespace").WithArguments("LongNamespace").WithLocation(4, 19)
                );

            var tree = compilation.SyntaxTrees.Single();

            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Short").Skip(1).Single();

            Assert.Equal("Short.MyClass", node.Parent.ToString());

            var model = compilation.GetSemanticModel(tree);

            var alias = model.GetAliasInfo(node);
            Assert.Equal("Short=LongNamespace", alias.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, alias.Target.Kind);
            Assert.Equal("LongNamespace", alias.Target.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(node);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [ClrOnlyFact, WorkItem(2805, "https://github.com/dotnet/roslyn/issues/2805")]
        public void AliasWithAnErrorFileScopedNamespace()
        {
            var text =
@"
namespace NS;
using Short = LongNamespace;
class Test
{
    public object Method1()
    {
        return (new Short.MyClass()).Prop;
    }
}
";

            var compilation = CreateCompilation(text, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));

            compilation.VerifyDiagnostics(
                // (3,15): error CS0246: The type or namespace name 'LongNamespace' could not be found (are you missing a using directive or an assembly reference?)
                // using Short = LongNamespace;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "LongNamespace").WithArguments("LongNamespace").WithLocation(3, 15));

            var tree = compilation.SyntaxTrees.Single();

            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "Short").Skip(1).Single();

            Assert.Equal("Short.MyClass", node.Parent.ToString());

            var model = compilation.GetSemanticModel(tree);

            var alias = model.GetAliasInfo(node);
            Assert.Equal("Short=LongNamespace", alias.ToTestDisplayString());
            Assert.Equal(SymbolKind.ErrorType, alias.Target.Kind);
            Assert.Equal("LongNamespace", alias.Target.ToTestDisplayString());

            var symbolInfo = model.GetSymbolInfo(node);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67281")]
        public void TestAliasToGenericWithPointer_NoElementAccess()
        {
            var text =
@"
// Legal in C# 11
using X = System.Collections.Generic.List<int*[]>;

class C
{
    void M(X x)
    {
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (7,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     void M(X x)
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(7, 12));

            CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (3,43): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // using X = System.Collections.Generic.List<int*[]>;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(3, 43),
                // (7,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     void M(X x)
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(7, 12));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67281")]
        public void TestAliasToGenericWithPointer_ElementAccess1()
        {
            var text =
@"
// Legal in C# 11
using X = System.Collections.Generic.List<int*[]>;

class C
{
    void M(X x)
    {
        var y = x[0];
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (7,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     void M(X x)
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(7, 12),
                // (9,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var y = x[0];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(9, 17),
                // (9,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var y = x[0];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[0]").WithLocation(9, 17));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67281")]
        public void TestAliasToGenericWithPointer_ElementAccess2()
        {
            var text = @"
// Legal in C# 11
using X = System.Collections.Generic.List<int*[]>;

class C
{
    void M(X x)
    {
        var y = x[0][0];
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (7,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     void M(X x)
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(7, 12),
                // (9,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var y = x[0][0];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(9, 17),
                // (9,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var y = x[0][0];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[0]").WithLocation(9, 17),
                // (9,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var y = x[0][0];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[0][0]").WithLocation(9, 17));

            CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (3,43): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // using X = System.Collections.Generic.List<int*[]>;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(3, 43),
                // (7,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     void M(X x)
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(7, 12),
                // (9,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var y = x[0][0];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x").WithLocation(9, 17),
                // (9,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var y = x[0][0];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[0]").WithLocation(9, 17),
                // (9,17): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         var y = x[0][0];
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "x[0][0]").WithLocation(9, 17));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67281")]
        public void TestAliasToGenericWithPointer()
        {
            var text = @"
using X = System.Collections.Generic.List<int*[]>;

class C
{
    void M(X x)
    {
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (6,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     void M(X x)
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(6, 12));

            var expected = new[]
            {
                // (2,43): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // using X = System.Collections.Generic.List<int*[]>;
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "int*").WithLocation(2, 43),
                // (6,12): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     void M(X x)
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "X").WithLocation(6, 12)
            };

            CreateCompilation(text, parseOptions: TestOptions.RegularNext).VerifyDiagnostics(expected);
            CreateCompilation(text).VerifyDiagnostics(expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67281")]
        public void TestAliasToGenericWithPointer_UnsafeAlias()
        {
            var text = @"
using unsafe X = System.Collections.Generic.List<int*[]>;

class C
{
    unsafe void M(X x)
    {
    }
}
";
            CreateCompilation(text).VerifyDiagnostics(
                // (2,7): error CS0227: Unsafe code may only appear if compiling with /unsafe
                // using unsafe X = System.Collections.Generic.List<int*[]>;
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "unsafe").WithLocation(2, 7),
                // (6,17): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //     unsafe void M(X x)
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "M").WithLocation(6, 17));

            CreateCompilation(text, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67989")]
        public void AliasToPointerReferencedInExpression()
        {
            CreateCompilation("""
                using unsafe Alias = int*;

                namespace N
                {
                    internal unsafe class C
                    {
                        const int Factor = 3;

                        void M()
                        {
                            _ = Alias * Factor;
                        }
                    }
                }
                """, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (11,17): error CS0119: 'int*' is a type, which is not valid in the given context
                //             _ = Alias * Factor;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Alias").WithArguments("int*", "type").WithLocation(11, 17));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67989")]
        public void AliasToTupleReferencedInExpression()
        {
            CreateCompilation("""
                using Alias = (int, int);

                internal class C
                {
                    void M()
                    {
                        Alias.Equals(null, null);
                    }
                }
                """).VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67989")]
        public void AliasToPointerReferencedInNameof()
        {
            CreateCompilation("""
                using Alias = (int, int);

                internal class C
                {
                    void M()
                    {
                        System.Console.WriteLine(nameof(Alias));
                    }
                }
                """).VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68546")]
        public void AliasToArrayReferencedInUsingStatic()
        {
            CreateCompilation("""
                using A = int[];

                namespace N
                {
                    using static A;
                }
                """).VerifyDiagnostics(
                    // (5,5): hidden CS8019: Unnecessary using directive.
                    //     using static A;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static A;").WithLocation(5, 5),
                    // (5,18): error CS9137: 'array' type is not valid for 'using static'. Only a class, struct, interface, enum, delegate or namespace can be used.
                    //     using static A;
                    Diagnostic(ErrorCode.ERR_BadUsingStaticType, "A").WithArguments("array").WithLocation(5, 18));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68546")]
        public void AliasToPointerReferencedInUsingStatic()
        {
            CreateCompilation("""
                using unsafe A = int*;

                namespace N
                {
                    using static A;
                }
                """, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                    // (5,5): hidden CS8019: Unnecessary using directive.
                    //     using static A;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static A;").WithLocation(5, 5),
                    // (5,18): error CS9137: 'pointer' type is not valid for 'using static'. Only a class, struct, interface, enum, delegate or namespace can be used.
                    //     using static A;
                    Diagnostic(ErrorCode.ERR_BadUsingStaticType, "A").WithArguments("pointer").WithLocation(5, 18));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68546")]
        public void AliasToFunctionPointerReferencedInUsingStatic()
        {
            CreateCompilation("""
                using unsafe A = delegate*<int,int>;

                namespace N
                {
                    using static A;
                }
                """, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                    // (5,5): hidden CS8019: Unnecessary using directive.
                    //     using static A;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static A;").WithLocation(5, 5),
                    // (5,18): error CS9137: 'function pointer' type is not valid for 'using static'. Only a class, struct, interface, enum, delegate or namespace can be used.
                    //     using static A;
                    Diagnostic(ErrorCode.ERR_BadUsingStaticType, "A").WithArguments("function pointer").WithLocation(5, 18));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68546")]
        public void AliasToDynamicReferencedInUsingStatic()
        {
            CreateCompilation("""
                using A = dynamic;

                namespace N
                {
                    using static A;
                }
                """).VerifyDiagnostics(
                    // (5,5): hidden CS8019: Unnecessary using directive.
                    //     using static A;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static A;").WithLocation(5, 5),
                    // (5,18): error CS9137: 'dynamic' type is not valid for 'using static'. Only a class, struct, interface, enum, delegate or namespace can be used.
                    //     using static A;
                    Diagnostic(ErrorCode.ERR_BadUsingStaticType, "A").WithArguments("dynamic").WithLocation(5, 18));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68546")]
        public void AliasToTupleReferencedInUsingStatic()
        {
            // Allowed, as it's the same as `using A = System.ValueTuple<int, int>;`
            CreateCompilation("""
                using A = (int, int);

                namespace N
                {
                    using static A;
                }
                """).VerifyDiagnostics(
                    // (5,5): hidden CS8019: Unnecessary using directive.
                    //     using static A;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static A;").WithLocation(5, 5));
        }
    }
}
