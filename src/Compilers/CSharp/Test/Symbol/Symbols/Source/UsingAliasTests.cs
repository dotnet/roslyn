// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using System.Linq;
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
            var root = tree.GetCompilationUnitRoot() as CompilationUnitSyntax;
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var a1 = root.Members[0] as TypeDeclarationSyntax;
            var a2 = root.Members[1] as TypeDeclarationSyntax;
            var a3 = root.Members[2] as TypeDeclarationSyntax;
            var a4 = root.Members[3] as TypeDeclarationSyntax;

            var base1 = a1.BaseList.Types[0].Type as TypeSyntax;
            var base2 = a2.BaseList.Types[0].Type as TypeSyntax;
            var base3 = a3.BaseList.Types[0].Type as TypeSyntax;
            var base4 = a4.BaseList.Types[0].Type as TypeSyntax;

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
            var root = tree.GetCompilationUnitRoot() as CompilationUnitSyntax;
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var a1 = root.Members[0] as TypeDeclarationSyntax;
            var a2 = root.Members[1] as TypeDeclarationSyntax;
            var a3 = root.Members[2] as TypeDeclarationSyntax;
            var a4 = root.Members[3] as TypeDeclarationSyntax;

            var base1 = a1.BaseList.Types[0].Type as TypeSyntax;
            var base2 = a2.BaseList.Types[0].Type as TypeSyntax;
            var base3 = a3.BaseList.Types[0].Type as TypeSyntax;
            var base4 = a4.BaseList.Types[0].Type as TypeSyntax;

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
            var root = tree.GetCompilationUnitRoot() as CompilationUnitSyntax;
            var comp = CreateCompilation(tree);

            var usingAlias = root.Usings[0];

            var a1 = root.Members[0] as TypeDeclarationSyntax;
            var a2 = root.Members[1] as TypeDeclarationSyntax;
            var a3 = root.Members[2] as TypeDeclarationSyntax;
            var a4 = root.Members[3] as TypeDeclarationSyntax;

            var base1 = a1.BaseList.Types[0].Type as TypeSyntax;
            var base2 = a2.BaseList.Types[0].Type as TypeSyntax;
            var base3 = a3.BaseList.Types[0].Type as TypeSyntax;
            var base4 = a4.BaseList.Types[0].Type as TypeSyntax;

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
            var root = tree.GetCompilationUnitRoot() as CompilationUnitSyntax;
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
            var root = tree.GetCompilationUnitRoot() as CompilationUnitSyntax;
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
            var root = tree.GetCompilationUnitRoot() as CompilationUnitSyntax;
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
            var root = tree.GetCompilationUnitRoot() as CompilationUnitSyntax;
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
            UsingDirectiveSyntax usingAlias = (syntaxTree.GetCompilationUnitRoot() as CompilationUnitSyntax).Usings.First();
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
    }
}
