// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SemanticTests : CSharpTestBase
    {
        [Fact]
        public void LocalSymbolsAreEquivalentAcrossSemanticModelsFromTheSameCompilation()
        {
            var text = @"public class C { public void M() { int x = 10; } }";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);

            var model1 = comp.GetSemanticModel(tree);
            var model2 = comp.GetSemanticModel(tree);
            Assert.NotEqual(model1, model2);

            var vardecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol1 = model1.GetDeclaredSymbol(vardecl);
            var symbol2 = model2.GetDeclaredSymbol(vardecl);

            Assert.NotSame(symbol1, symbol2);
            Assert.Equal(symbol1, symbol2);
        }

        [Fact]
        public void LocalSymbolsAreDifferentArossSemanticModelsFromDifferentCompilations()
        {
            var text = @"public class C { public void M() { int x = 10; } }";
            var tree = Parse(text);
            var comp1 = CreateCompilationWithMscorlib(tree);
            var comp2 = CreateCompilationWithMscorlib(tree);

            var model1 = comp1.GetSemanticModel(tree);
            var model2 = comp2.GetSemanticModel(tree);
            Assert.NotEqual(model1, model2);

            var vardecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            var symbol1 = model1.GetDeclaredSymbol(vardecl);
            var symbol2 = model2.GetDeclaredSymbol(vardecl);

            Assert.NotSame(symbol1, symbol2);
            Assert.NotEqual(symbol1, symbol2);
        }

        [Fact]
        public void RangeVariableSymbolsAreEquivalentAcrossSemanticModelsFromTheSameCompilation()
        {
            var text = @"using System.Linq; public class C { public void M() { var q = from c in string.Empty select c; } }";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlibAndSystemCore(new[] { tree });

            var model1 = comp.GetSemanticModel(tree);
            var model2 = comp.GetSemanticModel(tree);
            Assert.NotEqual(model1, model2);

            var vardecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<QueryClauseSyntax>().First();
            var symbol1 = model1.GetDeclaredSymbol(vardecl);
            var symbol2 = model2.GetDeclaredSymbol(vardecl);

            Assert.NotSame(symbol1, symbol2);
            Assert.Equal(symbol1, symbol2);
        }

        [Fact]
        public void RangeVariableSymbolsAreDifferentArossSemanticModelsFromDifferentCompilations()
        {
            var text = @"using System.Linq; public class C { public void M() { var q = from c in string.Empty select c; } }";
            var tree = Parse(text);
            var comp1 = CreateCompilationWithMscorlibAndSystemCore(new[] { tree });
            var comp2 = CreateCompilationWithMscorlibAndSystemCore(new[] { tree });

            var model1 = comp1.GetSemanticModel(tree);
            var model2 = comp2.GetSemanticModel(tree);
            Assert.NotEqual(model1, model2);

            var vardecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<QueryClauseSyntax>().First();
            var symbol1 = model1.GetDeclaredSymbol(vardecl);
            var symbol2 = model2.GetDeclaredSymbol(vardecl);

            Assert.NotSame(symbol1, symbol2);
            Assert.NotEqual(symbol1, symbol2);
        }

        [Fact]
        public void LabelSymbolsAreEquivalentAcrossSemanticModelsFromSameCompilation()
        {
            var text = @"public class C { public void M() { label: goto label; } }";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);

            var model1 = comp.GetSemanticModel(tree);
            var model2 = comp.GetSemanticModel(tree);
            Assert.NotEqual(model1, model2);

            var statement = tree.GetCompilationUnitRoot().DescendantNodes().OfType<GotoStatementSyntax>().First();
            var symbol1 = model1.GetSymbolInfo(statement.Expression).Symbol;
            var symbol2 = model2.GetSymbolInfo(statement.Expression).Symbol;

            Assert.Equal(false, ReferenceEquals(symbol1, symbol2));
            Assert.Equal(symbol1, symbol2);
        }

        [Fact]
        public void LambdaParameterSymbolsAreEquivalentAcrossSemanticModelsFromSameCompilation()
        {
            var text = @"using System; public class C { public void M() { Func<int,int> f = (p) => p; } }";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);

            var model1 = comp.GetSemanticModel(tree);
            var model2 = comp.GetSemanticModel(tree);
            Assert.NotEqual(model1, model2);

            var paramdecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().First();
            var symbol1 = model1.GetDeclaredSymbol(paramdecl);
            var symbol2 = model2.GetDeclaredSymbol(paramdecl);

            Assert.NotSame(symbol1, symbol2);
            Assert.Equal(symbol1.ContainingSymbol, symbol2.ContainingSymbol);
            Assert.Equal(symbol1, symbol2);
        }

        [Fact]
        public void LambdaParameterSymbolsAreDifferentAcrossSemanticModelsFromDifferentCompilations()
        {
            var text = @"using System; public class C { public void M() { Func<int,int> f = (p) => p; } }";
            var tree1 = Parse(text);
            var tree2 = Parse(text);
            var comp1 = CreateCompilationWithMscorlib(tree1);
            var comp2 = CreateCompilationWithMscorlib(tree2);

            var model1 = comp1.GetSemanticModel(tree1);
            var model2 = comp2.GetSemanticModel(tree2);
            Assert.NotEqual(model1, model2);

            var paramdecl1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().First();
            var symbol1 = model1.GetDeclaredSymbol(paramdecl1);
            var paramdecl2 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().First();
            var symbol2 = model2.GetDeclaredSymbol(paramdecl2);

            Assert.NotSame(symbol1, symbol2);
            Assert.NotEqual(symbol1.ContainingSymbol, symbol2.ContainingSymbol);
            Assert.NotEqual(symbol1, symbol2);
        }

        [WorkItem(539740, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539740")]
        [Fact]
        public void NamespaceWithoutName()
        {
            var text = "namespace";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);
            var errors = comp.GetDiagnostics().ToArray();
            Assert.Equal(3, errors.Length);

            var nsArray = tree.GetCompilationUnitRoot().DescendantNodes().Where(node => node.IsKind(SyntaxKind.NamespaceDeclaration)).ToArray();
            Assert.Equal(1, nsArray.Length);

            var nsSyntax = nsArray[0] as NamespaceDeclarationSyntax;
            var symbol = model.GetDeclaredSymbol(nsSyntax);
            Assert.Equal(string.Empty, symbol.Name);
        }

        [Fact]
        public void LazyBoundUsings1()
        {
            var text =
@"
// Peter Golde[7/19/2010]: I managed to construct the following interesting example today,
// which Dev10 does compile. Interestingly, the resolution of one ""using"" can depend
// on the resolution of another ""using"" later in the same namespace.
using K = A.Q;
using L = B.R;
 
class B
{
    public class R
    {
        public class Q
        {
            public class S : K { }
        }
    }
}
 
class A : L
{
    public K.S v = null;
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var a = global.GetTypeMembers("A", 0).Single();
            var abase = a.BaseType;
            Assert.Equal("B.R", abase.ToTestDisplayString());

            var b = global.GetTypeMembers("B", 0).Single();
            var r = b.GetTypeMembers("R", 0).Single();
            var q = r.GetTypeMembers("Q", 0).Single();
            var v = a.GetMembers("v").Single() as FieldSymbol;
            var s = v.Type;
            Assert.Equal("B.R.Q.S", s.ToTestDisplayString());
            var sbase = s.BaseType;
            Assert.Equal("B.R.Q", sbase.ToTestDisplayString());
        }

        [Fact]
        public void Diagnostics1()
        {
            var text =
@"
class A : A {}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);

            var errs = comp.GetSemanticModel(tree).GetDeclarationDiagnostics();
            Assert.Equal(1, errs.Count());
        }

        [Fact]
        public void DiagnosticsInOneTree()
        {
            var partial1 =
@"
partial class A 
{ 
    void foo() { int x = y; }
}

class C : B {}
";

            var partial2 =
@"
partial class A 
{ 
    int q;      //an unused field in a partial type
    void bar() { int x = z; }
}

class B : NonExistent {}
";

            var partial1Tree = Parse(partial1);
            var partial2Tree = Parse(partial2);
            var comp = CreateCompilationWithMscorlib(new SyntaxTree[] { partial1Tree, partial2Tree });

            var errs = comp.GetSemanticModel(partial1Tree).GetDiagnostics();
            Assert.Equal(1, errs.Count());
        }

        [Fact]
        public void Bindings1()
        {
            var text =
@"
class B : A {}
class A {}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);

            var bdecl = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var bbase = bdecl.BaseList.Types[0].Type as TypeSyntax;
            var model = comp.GetSemanticModel(tree);

            var info = model.GetSymbolInfo(bbase);
            Assert.NotNull(info.Symbol);
            var a = comp.GlobalNamespace.GetTypeMembers("A", 0).Single();
            Assert.Equal(a, info.Symbol);
            Assert.Equal(a, model.GetTypeInfo(bbase).Type);
        }

        [Fact]
        public void BaseScope1()
        {
            // ensure the base clause is not bound in the scope of the class
            var text =
@"
public class C : B {}
public class A {
    public class B {}
}
public class B : A {}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);

            var cdecl = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var cbase = cdecl.BaseList.Types[0].Type as TypeSyntax;
            var model = comp.GetSemanticModel(tree);

            var info = model.GetSymbolInfo(cbase);
            Assert.NotNull(info.Symbol);
            var b = comp.GlobalNamespace.GetTypeMembers("B", 0).Single();
            Assert.Equal(b, info.Symbol);
            Assert.Equal(b, model.GetTypeInfo(cbase).Type);
        }

        [Fact]
        public void BaseScope2()
        {
            // ensure type parameters are in scope in the base clause
            var text =
@"
public class C<T> : A<T> { }
public class A<T> : B { }
public class B {
    public class T { }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);

            var cdecl = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var cbase = cdecl.BaseList.Types[0].Type as TypeSyntax;
            var model = comp.GetSemanticModel(tree);

            var info = model.GetSymbolInfo(cbase);
            Assert.NotNull(info.Symbol);
            var cbasetype = info.Symbol as NamedTypeSymbol;

            var c = comp.GlobalNamespace.GetTypeMembers("C", 1).Single();
            Assert.Equal(c.BaseType, cbasetype);
        }

        [Fact]
        public void Bindings2()
        {
            var text =
@"
class B<T> : A<T> {}
class A<T> {}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);

            var bdecl = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var bbase = bdecl.BaseList.Types[0].Type as TypeSyntax; // A<T>
            var model = comp.GetSemanticModel(tree);

            var info = model.GetSymbolInfo(bbase);
            Assert.NotNull(info.Symbol);
            var at2 = info.Symbol as NamedTypeSymbol;
            Assert.Equal(at2, model.GetTypeInfo(bbase).Type);

            var a = comp.GlobalNamespace.GetTypeMembers("A", 1).Single();
            var at = a.TypeParameters.First();
            var b = comp.GlobalNamespace.GetTypeMembers("B", 1).Single();
            var bt = b.TypeParameters.First();

            Assert.Equal(a.OriginalDefinition, at2.OriginalDefinition);
            Assert.Equal(b.TypeParameters.First(), at2.TypeArguments.First());
        }

        [Fact]
        public void Bindings3()
        {
            var text =
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static Program Field;
}";
            var tree1 = Parse(text);
            var compilation = CreateCompilationWithMscorlib(tree1);

            var tree2 = Parse(text);
            var classProgram = tree2.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var staticProgramField = classProgram.Members[0] as FieldDeclarationSyntax;
            var program = staticProgramField.Declaration.Type;
            var model = compilation.GetSemanticModel(tree1);

            Assert.Throws<ArgumentException>(() =>
            {
                // tree2 not in the compilation
                var lookup = model.GetSymbolInfo(program);
            });
        }

        [Fact]
        public void Bindings4()
        {
            var text =
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    Program p;
    static void Main(string[] args)
    {
    }
}";

            var tree1 = Parse(text);
            var compilation = CreateCompilationWithMscorlib(tree1);

            var decl = tree1.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var field = decl.Members[0] as FieldDeclarationSyntax;
            var type = field.Declaration.Type;
            var model = compilation.GetSemanticModel(tree1);

            var info = model.GetSymbolInfo(type);
            Assert.Equal(compilation.GlobalNamespace.GetTypeMembers("Program", 0).Single(), info.Symbol);
        }

        [Fact]
        public void Bindings5()
        {
            var text =
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
    }
}";

            var tree1 = Parse(text);
            var tree2 = Parse(text);
            var compilation = CreateCompilationWithMscorlib(new SyntaxTree[] { tree1, tree2 });

            var decl = tree1.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var method = decl.Members[0] as MethodDeclarationSyntax;
            var type = method.ParameterList.Parameters[0].Type;

            var model = compilation.GetSemanticModel(tree1);

            var info = model.GetSymbolInfo(type);
            Assert.Equal<Symbol>(compilation.GetSpecialType(SpecialType.System_String), (info.Symbol as ArrayTypeSymbol).ElementType);
        }

        [Fact]
        public void Speculative1()
        {
            var text =
@"
class B {
    object x;
}
class A {}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);

            var bdecl = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var xdecl = bdecl.Members[0] as FieldDeclarationSyntax;

            var model = comp.GetSemanticModel(tree);

            TypeSyntax speculate = SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("A"));
            var symbolInfo = model.GetSpeculativeSymbolInfo(xdecl.SpanStart, speculate, SpeculativeBindingOption.BindAsTypeOrNamespace);
            var lookup = symbolInfo.Symbol as TypeSymbol;


            Assert.NotNull(lookup);
            var a = comp.GlobalNamespace.GetTypeMembers("A", 0).Single();
            Assert.Equal(a, lookup);
        }

        [Fact]
        public void GetType1()
        {
            var text =
@"
class A {
    class B {}
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);

            var adecl = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var bdecl = adecl.Members[0] as TypeDeclarationSyntax;

            var model = comp.GetSemanticModel(tree);
            var a1 = model.GetDeclaredSymbol(adecl);
            var b1 = model.GetDeclaredSymbol(bdecl);

            var global = comp.GlobalNamespace;
            var a2 = global.GetTypeMembers("A", 0).Single();
            var b2 = a2.GetTypeMembers("B", 0).Single();

            Assert.Equal(a2, a1);
            Assert.Equal(b2, b1);
        }

        [Fact]
        public void DottedName()
        {
            var text =
@"
class Main {
  A.B x; // this refers to the B within A.
}
class A {
    public class B {}
}
class B {}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);

            var root = tree.GetCompilationUnitRoot();
            var mainDecl = root.Members[0] as TypeDeclarationSyntax;
            var mainType = model.GetDeclaredSymbol(mainDecl);

            var aDecl = root.Members[1] as TypeDeclarationSyntax;
            var aType = model.GetDeclaredSymbol(aDecl);

            var abDecl = aDecl.Members[0] as TypeDeclarationSyntax;
            var abType = model.GetDeclaredSymbol(abDecl);

            var bDecl = root.Members[2] as TypeDeclarationSyntax;
            var bType = model.GetDeclaredSymbol(bDecl);

            var xDecl = mainDecl.Members[0] as FieldDeclarationSyntax;
            var xSym = mainType.GetMembers("x").Single() as FieldSymbol;
            Assert.Equal<ISymbol>(abType, xSym.Type);
            var info = model.GetSymbolInfo((xDecl.Declaration.Type as QualifiedNameSyntax).Right);
            Assert.Equal(abType, info.Symbol);
        }

        [Fact]
        public void AliasQualifiedName()
        {
            var text =
@"
class B {}
namespace N {
  class C : global::B {}
  class B {}
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);

            var root = tree.GetCompilationUnitRoot();
            var nDecl = root.Members[0] as NamespaceDeclarationSyntax;
            var n2Decl = root.Members[1] as NamespaceDeclarationSyntax;
            var cDecl = n2Decl.Members[0] as TypeDeclarationSyntax;
            var cBase = (cDecl.BaseList.Types[0].Type as AliasQualifiedNameSyntax).Name;

            var cBaseType = model.GetSymbolInfo(cBase).Symbol;
            var bOuter = comp.GlobalNamespace.GetTypeMembers("B", 0).Single();
            var bInner = (comp.GlobalNamespace.GetMembers("N").Single() as NamespaceSymbol).GetTypeMembers("B", 0).Single();
            Assert.Equal(bOuter, cBaseType);
        }

        [Fact, WorkItem(528655, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528655")]
        public void ErrorSymbolForInvalidCode()
        {
            var text = @"
public class A 
{
	int foo	{	void foo() {}	} // Error
	static int Main() {	return 1;    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var mems = comp.SourceModule.GlobalNamespace.GetMembers();

            var typeA = mems.Where(s => s.Name == "A").Select(s => s);
            Assert.Equal(1, typeA.Count());
            var invalid = mems.Where(s => s.Name == "<invalid-global-code>").Select(s => s);
            Assert.Equal(1, invalid.Count());
        }

        [Fact, WorkItem(543225, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543225"), WorkItem(529057, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529057")]
        public void MergePartialMethodAndParameterSymbols()
        {
            var text = @"
using System;

partial class PC
{
    partial void PM(int pp);
}

partial class PC
{
    partial void PM(int pp) {}
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var pTypeSym = comp.SourceModule.GlobalNamespace.GetTypeMembers("PC").Single();
            var pMethodSym = pTypeSym.GetMembers("PM").Single();

            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            var pType01 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            var pType02 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Last();
            Assert.NotEqual(pType01, pType02);
            var ptSym01 = model.GetDeclaredSymbol(pType01);
            var ptSym02 = model.GetDeclaredSymbol(pType02);
            // same partial type symbol
            Assert.Same(ptSym01, ptSym02);
            Assert.Equal(2, ptSym01.Locations.Length);

            var pMethod01 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var pMethod02 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Last();
            Assert.NotEqual(pMethod01, pMethod02);

            var pmSym01 = model.GetDeclaredSymbol(pMethod01);
            var pmSym02 = model.GetDeclaredSymbol(pMethod02);
            // different partial method symbols:(
            Assert.NotSame(pmSym01, pmSym02);
            // the declaration one is what one can get from GetMembers()
            Assert.Same(pMethodSym, pmSym01);

            // with decl|impl point to each other
            Assert.Null(pmSym01.PartialDefinitionPart);
            Assert.Same(pmSym02, pmSym01.PartialImplementationPart);

            Assert.Same(pmSym01, pmSym02.PartialDefinitionPart);
            Assert.Null(pmSym02.PartialImplementationPart);

            var pParam01 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().First();
            var pParam02 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().Last();
            Assert.NotEqual(pParam01, pParam02);

            var ppSym01 = model.GetDeclaredSymbol(pParam01);
            var ppSym02 = model.GetDeclaredSymbol(pParam02);
            Assert.NotSame(ppSym01, ppSym02);
            Assert.Equal(1, ppSym01.Locations.Length);
            Assert.Equal(1, ppSym02.Locations.Length);
        }

        [Fact, WorkItem(544221, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544221")]
        public void GetTypeInfoForOptionalParameterDefaultValueInDelegate()
        {
            var text = @"
using System;

class Test
{
    public delegate void DFoo(byte i = 1);
    protected internal void MFoo(sbyte j = 2) { }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();
            var exprs = root.DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(2, exprs.Length);

            var type1 = model.GetTypeInfo(exprs[0]);
            var type2 = model.GetTypeInfo(exprs[1]);

            Assert.NotNull(type1.Type);
            Assert.Equal("System.Int32", type1.Type.ToTestDisplayString());
            Assert.NotNull(type2.Type);
            Assert.Equal("System.Int32", type2.Type.ToTestDisplayString());
        }

        [Fact, WorkItem(544231, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544231")]
        public void GetDeclSymbolForParameterOfPartialMethod()
        {
            var text1 = @"
using System;

partial class Partial001
{
    static partial void Foo(ulong x);
}
";

            var text2 = @"
using System;

partial class Partial001
{
    static partial void Foo(ulong x)  {    }
    static int Main()  {    return 1;    }
}
";
            var tree1 = Parse(text1);
            var tree2 = Parse(text2);
            var comp = CreateCompilationWithMscorlib(new List<SyntaxTree> { tree1, tree2 });

            var model1 = comp.GetSemanticModel(tree1);
            var model2 = comp.GetSemanticModel(tree2);
            var root1 = tree1.GetCompilationUnitRoot();
            var root2 = tree1.GetCompilationUnitRoot();
            var para1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().First();
            var para2 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().First();
            var sym1 = model1.GetDeclaredSymbol(para1);
            var sym2 = model2.GetDeclaredSymbol(para2);

            Assert.NotNull(sym1);
            Assert.NotNull(sym2);
            Assert.Equal("System.UInt64 x", sym1.ToTestDisplayString());
            Assert.Equal("System.UInt64 x", sym2.ToTestDisplayString());
            Assert.NotEqual(sym1.Locations[0], sym2.Locations[0]);
        }

        [Fact, WorkItem(544473, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544473")]
        public void GetDeclSymbolForTypeParameterOfPartialMethod()
        {
            var text1 = @"
using System;

partial class Partial001
{
    static partial void Foo<T>(T x);
}
";

            var text2 = @"
using System;

partial class Partial001
{
    static partial void Foo<T>(T x)  {    }
    static int Main()  {    return 1;    }
}
";
            var tree1 = Parse(text1);
            var tree2 = Parse(text2);
            var comp = CreateCompilationWithMscorlib(new List<SyntaxTree> { tree1, tree2 });

            var model1 = comp.GetSemanticModel(tree1);
            var model2 = comp.GetSemanticModel(tree2);
            var root1 = tree1.GetCompilationUnitRoot();
            var root2 = tree1.GetCompilationUnitRoot();
            var para1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType<TypeParameterSyntax>().First();
            var para2 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType<TypeParameterSyntax>().First();
            var sym1 = model1.GetDeclaredSymbol(para1);
            var sym2 = model2.GetDeclaredSymbol(para2);

            Assert.NotNull(sym1);
            Assert.NotNull(sym2);
            Assert.Equal("T", sym1.ToTestDisplayString());
            Assert.Equal("T", sym2.ToTestDisplayString());
            Assert.NotEqual(sym1.Locations[0], sym2.Locations[0]);
        }

        [Fact]
        public void GetDeclaredSymbolForAnonymousTypeProperty01()
        {
            var text = @"
using System;

struct AnonTypeTest
{
    static long Prop
    {
        get
        {
            short @short = -1;
            var anonType = new { id = 123, @do = ""QC"", @short, Prop };
            return anonType.id + anonType.@short;
        }
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);
            var anonProps = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AnonymousObjectMemberDeclaratorSyntax>();
            Assert.Equal(4, anonProps.Count());
            var symList = from ap in anonProps
                          let apsym = model.GetDeclaredSymbol(ap)
                          orderby apsym.Name
                          select apsym.Name;

            var results = string.Join(", ", symList);
            Assert.Equal("do, id, Prop, short", results);
        }

        [Fact]
        public void GetDeclaredSymbolForAnonymousTypeProperty02()
        {
            var text = @"
using System;

class AnonTypeTest
{
    long field = 111;
    void M(byte p1, ref sbyte p2, out string p3, params string[] ary)
    {
        ulong local = 12345;
        var anonType = new { local, this.field, p1, p2, ary };
        p3 = anonType.ary.Length > 0 ? anonType.ary[0] : """";
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);
            var anonProps = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AnonymousObjectMemberDeclaratorSyntax>();
            Assert.Equal(5, anonProps.Count());
            var symList = from ap in anonProps
                          let apsym = model.GetDeclaredSymbol(ap)
                          orderby apsym.Name
                          select apsym.Name;

            var results = string.Join(", ", symList);
            Assert.Equal("ary, field, local, p1, p2", results);
        }

        [Fact]
        public void GetDeclaredSymbolForAnonymousTypeProperty03()
        {
            var text = @"
using System;

enum E { a, b, c }
class Base
{
    protected E baseField = E.b;
    protected virtual Base BaseProp { get { return this; } }
    public Func<string, char> deleField;
}

class AnonTypeTest : Base
{
    protected override Base BaseProp { get { return null; } }
    char this[string @string]
    {
        get
        {
            var anonType = new { id = deleField, base.BaseProp, base.baseField, ret = @string };
            return anonType.id(anonType.ret);
        }
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);
            var anonProps = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AnonymousObjectMemberDeclaratorSyntax>();
            Assert.Equal(4, anonProps.Count());
            var symList = from ap in anonProps
                          let apsym = model.GetDeclaredSymbol(ap)
                          orderby apsym.Name
                          select apsym.Name;

            var results = string.Join(", ", symList);
            Assert.Equal("baseField, BaseProp, id, ret", results);
        }

        [Fact]
        public void GetDeclaredSymbolForAnonymousTypeProperty04()
        {
            var text = @"
using System;

enum E { a, b, c }
struct S
{
    public static E sField;
    public interface IFoo {  }
    public IFoo GetFoo { get; set; }
    public IFoo GetFoo2() { return null; }
}

class AnonTypeTest
{
    event Action<ushort> Eve
    {
        add
        {
            var anonType = new { a1 = new { S.sField, ifoo = new { new S().GetFoo } } };
        }
        remove 
        {
            var anonType = new { a1 = new { a2 = new { a2 = S.sField, a3 = new { a3 = new S().GetFoo2() } } } };
        }
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);
            var anonProps = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AnonymousObjectMemberDeclaratorSyntax>();
            Assert.Equal(9, anonProps.Count());
            var symList = from ap in anonProps
                          let apsym = model.GetDeclaredSymbol(ap)
                          orderby apsym.Name
                          select apsym.Name;

            var results = string.Join(", ", symList);
            Assert.Equal("a1, a1, a2, a2, a3, a3, GetFoo, ifoo, sField", results);
        }

        [Fact(), WorkItem(542861, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542861"), WorkItem(529673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529673")]
        public void GetSymbolInfoForAccessorParameters()
        {
            var text = @"
using System;

public class Test
{
    object[] _Items = new object[3];
    public object this[int index]
    {
        get
        {
            return _Items[index];
        }
        set
        {
            _Items[index] = value;
        }
    } 
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);
            var descendants = tree.GetCompilationUnitRoot().DescendantNodes();

            var paras = descendants.OfType<ParameterSyntax>();
            Assert.Equal(1, paras.Count());
            var parasym = model.GetDeclaredSymbol(paras.First());
            var ploc = parasym.Locations[0];

            var args = descendants.OfType<ArgumentSyntax>().Where(s => s.ToString() == "index").Select(s => s);
            Assert.Equal(2, args.Count());
            var argsym1 = model.GetSymbolInfo(args.First().Expression).Symbol;
            var argsym2 = model.GetSymbolInfo(args.Last().Expression).Symbol;
            Assert.NotNull(argsym1);
            Assert.NotNull(argsym2);

            Assert.Equal(ploc, argsym1.Locations[0]);
            Assert.Equal(ploc, argsym2.Locations[0]);

            Assert.Equal(parasym.Kind, argsym1.Kind);
            Assert.Equal(parasym.Kind, argsym2.Kind);

            // SourceSimpleParameterSymbol vs. SourceClonedComplexParameterSymbol
            Assert.NotEqual(parasym, argsym1);
            Assert.NotEqual(parasym, argsym2);
        }

        [WorkItem(545648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545648")]
        [Fact]
        public void AliasDeclaredSymbolWithConflict()
        {
            var source = @"
using X = System;
 
class X { }
";

            var comp = CreateCompilationWithMscorlib(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var aliasSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single();

            var symbol = model.GetDeclaredSymbol(aliasSyntax);
            Assert.Equal(symbol.Target, comp.GlobalNamespace.GetMember<NamespaceSymbol>("System"));

            comp.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using X = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = System;"));
        }

        [WorkItem(529751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529751")]
        [Fact]
        public void ExternAlias()
        {
            var source = @"
extern alias X;

class Test
{
    static void Main()
    {
        X::C c = null;
    }
}
";
            var comp1 = CreateCompilationWithMscorlib("public class C { }");
            var ref1 = comp1.EmitToImageReference(aliases: ImmutableArray.Create("X"));

            var comp2 = CreateCompilationWithMscorlib(source, new[] { ref1 });
            var tree = comp2.SyntaxTrees.Single();
            var model = comp2.GetSemanticModel(tree);

            var aliasSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().Single();

            // Compilation.GetExternAliasTarget defines this behavior: the target is a merged namespace
            // with the same name as the alias, contained in the global namespace of the compilation.
            var aliasSymbol = model.GetDeclaredSymbol(aliasSyntax);
            var aliasTarget = (NamespaceSymbol)aliasSymbol.Target;
            Assert.Equal(NamespaceKind.Module, aliasTarget.Extent.Kind);
            Assert.Equal("", aliasTarget.Name);
            Assert.True(aliasTarget.IsGlobalNamespace);
            Assert.Null(aliasTarget.ContainingNamespace);

            Assert.Equal(0, comp2.GlobalNamespace.GetMembers("X").Length); //Doesn't contain the alias target namespace as a child.

            var aliasQualifiedSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AliasQualifiedNameSyntax>().Single();
            Assert.Equal(aliasSymbol, model.GetAliasInfo(aliasQualifiedSyntax.Alias));

            comp2.VerifyDiagnostics(
                // (8,14): warning CS0219: The variable 'c' is assigned but its value is never used
                //         X::C c = null;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "c").WithArguments("c"));
        }

        [Fact, WorkItem(546687, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546687"), WorkItem(529751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529751")]
        public void ExternAliasWithoutTarget()
        {
            var source = @"
extern alias X;

class Test
{
    static void Main()
    {
        X::C c = null;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var aliasSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().Single();

            var aliasSymbol = model.GetDeclaredSymbol(aliasSyntax);
            Assert.IsType<MissingNamespaceSymbol>(aliasSymbol.Target);

            var aliasQualifiedSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AliasQualifiedNameSyntax>().Single();
            Assert.Equal(aliasSymbol, model.GetAliasInfo(aliasQualifiedSyntax.Alias));

            comp.VerifyDiagnostics(
                // (2,14): error CS0430: The extern alias 'X' was not specified in a /reference option
                // extern alias X;
                Diagnostic(ErrorCode.ERR_BadExternAlias, "X").WithArguments("X"),
                // (8,12): error CS0234: The type or namespace name 'C' does not exist in the namespace 'X' (are you missing an assembly reference?)
                //         X::C c = null;
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "C").WithArguments("C", "X"));
        }

        [WorkItem(545648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545648")]
        [Fact]
        public void UsingDirectiveAliasSemanticInfo()
        {
            var source = "using X = System;";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (1,1): info CS8019: Unnecessary using directive.
                // using X = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = System;"));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var aliasSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<NameEqualsSyntax>().Single().Name;
            Assert.Equal(SymbolInfo.None, model.GetSymbolInfo(aliasSyntax));

            var usingSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single();
            Assert.Equal(SymbolKind.Alias, model.GetDeclaredSymbol(usingSyntax).Kind);
        }

        [WorkItem(545882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545882")]
        [Fact]
        public void SpeculativelyBindConstructorInitializerInPlaceOfActual()
        {
            var source = @"class C
{
    C(int x) { }
    C() : this((int) 1) { }
}";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var oldSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().Single();

            var newSyntax = SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer);

            var info = model.GetSpeculativeSymbolInfo(oldSyntax.SpanStart, newSyntax);
            var symbol = info.Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C"), symbol.ContainingType);
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            var method = (MethodSymbol)symbol;
            Assert.Equal(MethodKind.Constructor, method.MethodKind);
            Assert.Equal(0, method.ParameterCount);
        }

        [WorkItem(545882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545882")]
        [Fact]
        public void SpeculativelyBindConstructorInitializerInNewLocation()
        {
            var source = @"class C
{
    C() { }
}";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var oldSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            var newSyntax = SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer);

            var info = model.GetSpeculativeSymbolInfo(oldSyntax.ParameterList.Span.End, newSyntax);
            Assert.Equal(SymbolInfo.None, info);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelInFieldInitializer()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  object y = 1;
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var fieldDecl = (FieldDeclarationSyntax)typeDecl.Members[0];
            var varDecl = fieldDecl.Declaration.Variables.First();

            var model = compilation.GetSemanticModel(tree);
            Assert.False(model.IsSpeculativeSemanticModel);
            Assert.Null(model.ParentModel);
            Assert.Equal(0, model.OriginalPositionForSpeculation);

            // Speculate on the equals value syntax (initializer)
            // Conversion info available, ConvertedType: Object.
            var equalsValue = SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(@"(string)""Hello"""));
            var expr = equalsValue.Value;
            int position = varDecl.Initializer.SpanStart;

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(position, equalsValue, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);
            Assert.True(speculativeModel.IsSpeculativeSemanticModel);
            Assert.Equal(model, speculativeModel.ParentModel);
            Assert.Equal(position, speculativeModel.OriginalPositionForSpeculation);

            var typeInfo = speculativeModel.GetTypeInfo(expr);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("String", typeInfo.Type.Name);
            Assert.Equal("Object", typeInfo.ConvertedType.Name);

            var constantInfo = speculativeModel.GetConstantValue(expr);
            Assert.True(constantInfo.HasValue, "must be a constant");
            Assert.Equal("Hello", constantInfo.Value);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelInEnumMember()
        {
            var compilation = CreateCompilationWithMscorlib(@"
enum C 
{
  y = 1
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (EnumDeclarationSyntax)root.Members[0];
            var enumMemberDecl = (EnumMemberDeclarationSyntax)typeDecl.Members[0];
            var equalsValue = enumMemberDecl.EqualsValue;
            var initializer = equalsValue.Value;

            var model = compilation.GetSemanticModel(tree);

            // Speculate on the equals value syntax (initializer)
            // Conversion info available, ConvertedType: Int32.
            var newEqualsValue = SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("(short)0"));
            var expr = newEqualsValue.Value;
            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(equalsValue.SpanStart, newEqualsValue, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var typeInfo = speculativeModel.GetTypeInfo(expr);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("Int16", typeInfo.Type.Name);
            Assert.Equal("Int32", typeInfo.ConvertedType.Name);

            var constantInfo = speculativeModel.GetConstantValue(expr);
            Assert.True(constantInfo.HasValue, "must be a constant");
            Assert.Equal((short)0, constantInfo.Value);
        }

        [Fact]
        [WorkItem(648305, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/648305")]
        public void TestGetSpeculativeSemanticModelInDefaultValueArgument()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  void M(int x = 1)
  {
    string y = ""Hello"";     
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var equalsValue = methodDecl.ParameterList.Parameters[0].Default;
            var paramDefaultArg = equalsValue.Value;

            var model = compilation.GetSemanticModel(tree);

            var ti = model.GetTypeInfo(paramDefaultArg);

            // Speculate on the equals value syntax (initializer)
            // Conversion info available, ConvertedType: Int32.
            var newEqualsValue = SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("(short)0"));
            var expr = newEqualsValue.Value;

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(equalsValue.SpanStart, newEqualsValue, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var typeInfo = speculativeModel.GetTypeInfo(expr);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("Int16", typeInfo.Type.Name);
            Assert.Equal("Int32", typeInfo.ConvertedType.Name);

            var constantInfo = speculativeModel.GetConstantValue(expr);
            Assert.True(constantInfo.HasValue, "must be a constant");
            Assert.Equal((short)0, constantInfo.Value);
        }

        [Fact]
        [WorkItem(746002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/746002")]
        public void TestGetSpeculativeSemanticModelInDefaultValueArgument2()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System;

enum E
{
   A = 1,
   B = 2  
}

interface I
{
    void M1(E e = E.A);
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var interfaceDecl = (TypeDeclarationSyntax)root.Members[1];
            var methodDecl = (MethodDeclarationSyntax)interfaceDecl.Members[0];
            var param = methodDecl.ParameterList.Parameters[0];
            var equalsValue = param.Default;
            var paramDefaultArg = equalsValue.Value;

            var model = compilation.GetSemanticModel(tree);

            // Speculate on the equals value syntax (initializer) with a non-null parent
            var newEqualsValue = SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("E.B | E.A"));
            newEqualsValue = param.ReplaceNode(equalsValue, newEqualsValue).Default;
            var binaryExpr = newEqualsValue.Value;

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(equalsValue.SpanStart, newEqualsValue, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var typeInfo = speculativeModel.GetTypeInfo(binaryExpr);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("E", typeInfo.Type.Name);
            Assert.Equal("E", typeInfo.ConvertedType.Name);
        }

        [Fact]
        [WorkItem(657701, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/657701")]
        public void TestGetSpeculativeSemanticModelInConstructorDefaultValueArgument()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  C(int x = 1)
  {
    string y = ""Hello"";     
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var constructorDecl = (ConstructorDeclarationSyntax)typeDecl.Members[0];
            var equalsValue = constructorDecl.ParameterList.Parameters[0].Default;
            var paramDefaultArg = equalsValue.Value;

            var model = compilation.GetSemanticModel(tree);

            var ti = model.GetTypeInfo(paramDefaultArg);

            // Speculate on the equals value syntax (initializer)
            // Conversion info available, ConvertedType: Int32.
            var newEqualsValue = SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("(short)0"));
            var expr = newEqualsValue.Value;

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(equalsValue.SpanStart, newEqualsValue, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var typeInfo = speculativeModel.GetTypeInfo(expr);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("Int16", typeInfo.Type.Name);
            Assert.Equal("Int32", typeInfo.ConvertedType.Name);

            var constantInfo = speculativeModel.GetConstantValue(expr);
            Assert.True(constantInfo.HasValue, "must be a constant");
            Assert.Equal((short)0, constantInfo.Value);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelInExpressionBody_Property()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  public object X => 0;
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var propertyDecl = (PropertyDeclarationSyntax)typeDecl.Members[0];
            var expressionBody = propertyDecl.ExpressionBody;

            TestExpressionBodySpeculation(compilation, tree, expressionBody);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelInExpressionBody_Method()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  public object X() => 0;
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var expressionBody = methodDecl.ExpressionBody;

            TestExpressionBodySpeculation(compilation, tree, expressionBody);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelInExpressionBody_Indexer()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  public object this[int x] => 0;
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var indexerDecl = (IndexerDeclarationSyntax)typeDecl.Members[0];
            var expressionBody = indexerDecl.ExpressionBody;

            TestExpressionBodySpeculation(compilation, tree, expressionBody);
        }

        private static void TestExpressionBodySpeculation(Compilation compilation, SyntaxTree tree, ArrowExpressionClauseSyntax expressionBody)
        {
            var model = compilation.GetSemanticModel(tree);
            Assert.False(model.IsSpeculativeSemanticModel);
            Assert.Null(model.ParentModel);
            Assert.Equal(0, model.OriginalPositionForSpeculation);

            // Speculate on the expression body syntax.
            // Conversion info available, ConvertedType: Object.
            var newExpressionBody = SyntaxFactory.ArrowExpressionClause(SyntaxFactory.ParseExpression(@"(string)""Hello"""));
            var expr = newExpressionBody.Expression;
            int position = expressionBody.SpanStart;

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(position, newExpressionBody, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);
            Assert.True(speculativeModel.IsSpeculativeSemanticModel);
            Assert.Equal(model, speculativeModel.ParentModel);
            Assert.Equal(position, speculativeModel.OriginalPositionForSpeculation);

            var typeInfo = speculativeModel.GetTypeInfo(expr);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("String", typeInfo.Type.Name);
            Assert.Equal("Object", typeInfo.ConvertedType.Name);

            var constantInfo = speculativeModel.GetConstantValue(expr);
            Assert.True(constantInfo.HasValue, "must be a constant");
            Assert.Equal("Hello", constantInfo.Value);
        }

        [WorkItem(529893, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529893")]
        [Fact]
        public void AliasCalledVar()
        {
            var source = @"
using var = Q;

class Q
{
    var q;
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics(
                // (6,9): warning CS0169: The field 'Q.q' is never used
                //     var q;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "q").WithArguments("Q.q"));

            var classQ = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Q");
            var fieldQ = classQ.GetMember<FieldSymbol>("q");

            Assert.Equal(classQ, fieldQ.Type);

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var aliasDecl = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single();

            var aliasSymbol = model.GetDeclaredSymbol(aliasDecl);
            Assert.Equal(SymbolKind.Alias, aliasSymbol.Kind);
            Assert.Equal(classQ, ((AliasSymbol)aliasSymbol).Target);
            Assert.Equal("var", aliasSymbol.Name);

            var aliasDeclInfo = model.GetSymbolInfo(aliasDecl.Alias.Name);
            Assert.Null(aliasDeclInfo.Symbol);
            Assert.Equal(CandidateReason.None, aliasDeclInfo.CandidateReason);

            var fieldDecl = tree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().Single();

            var fieldSymbol = model.GetDeclaredSymbol(fieldDecl.Declaration.Variables.Single());
            Assert.Equal(fieldQ, fieldSymbol);

            var typeSyntax = (IdentifierNameSyntax)fieldDecl.Declaration.Type;

            var fieldTypeInfo = model.GetSymbolInfo(typeSyntax);
            Assert.Equal(classQ, fieldTypeInfo.Symbol);

            var fieldTypeAliasInfo = model.GetAliasInfo(typeSyntax);
            Assert.Equal(aliasSymbol, fieldTypeAliasInfo);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForStatement()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  void M(int x)
  {
    int y = 1000;     
  }
}
");

            var statement = (BlockSyntax)SyntaxFactory.ParseStatement(@"
{ 
   int z = 0; 
   M(z);  
   M(y);
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].SpanStart, statement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var localDecl = (LocalDeclarationStatementSyntax)statement.Statements[0];
            var declarator = localDecl.Declaration.Variables.First();
            var local = speculativeModel.GetDeclaredSymbol(declarator);
            Assert.NotNull(local);
            Assert.Equal("z", local.Name);
            Assert.Equal(SymbolKind.Local, local.Kind);
            Assert.Equal("Int32", ((LocalSymbol)local).Type.Name);

            var typeInfo = speculativeModel.GetTypeInfo(localDecl.Declaration.Type);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("Int32", typeInfo.Type.Name);

            var call = (InvocationExpressionSyntax)((ExpressionStatementSyntax)statement.Statements[1]).Expression;
            var arg = call.ArgumentList.Arguments[0].Expression;
            var info = speculativeModel.GetSymbolInfo(arg);
            Assert.NotNull(info.Symbol);
            Assert.Equal("z", info.Symbol.Name);
            Assert.Equal(SymbolKind.Local, info.Symbol.Kind);

            var call2 = (InvocationExpressionSyntax)((ExpressionStatementSyntax)((BlockSyntax)statement).Statements[2]).Expression;
            var arg2 = call2.ArgumentList.Arguments[0].Expression;
            var info2 = speculativeModel.GetSymbolInfo(arg2);
            Assert.NotNull(info2.Symbol);
            Assert.Equal("y", info2.Symbol.Name);
            Assert.Equal(SymbolKind.Local, info2.Symbol.Kind);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8778")]
        public void TestGetSpeculativeSemanticModelForStatement_DeclaredLocal()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  void M(int x)
  {
    int y = 1000;     
  }
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);

            // different name local
            var statement = SyntaxFactory.ParseStatement(@"int z = 0;");

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].SpanStart, statement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var declarator = ((LocalDeclarationStatementSyntax)statement).Declaration.Variables.First();
            var local = speculativeModel.GetDeclaredSymbol(declarator);
            Assert.NotNull(local);
            Assert.Equal("z", local.Name);
            Assert.Equal(SymbolKind.Local, local.Kind);
            Assert.Equal("Int32", ((LocalSymbol)local).Type.Name);

            // same name local
            statement = SyntaxFactory.ParseStatement(@"string y = null;");
            success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].SpanStart, statement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            declarator = ((LocalDeclarationStatementSyntax)statement).Declaration.Variables.First();
            local = speculativeModel.GetDeclaredSymbol(declarator);
            Assert.NotNull(local);
            Assert.Equal("y", local.Name);
            Assert.Equal(SymbolKind.Local, local.Kind);
            Assert.Equal("String", ((LocalSymbol)local).Type.Name);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForStatement_GetDeclaredLabelSymbol()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  void M(int x)
  {
    int y = 1000;     
  }
}
");

            var labeledStatement = SyntaxFactory.ParseStatement(@"label: y++;");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);

            SemanticModel statModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].SpanStart, labeledStatement, out statModel);
            Assert.True(success);
            Assert.NotNull(statModel);

            var label = statModel.GetDeclaredSymbol(labeledStatement);
            Assert.NotNull(label);
            Assert.Equal("label", label.Name);
            Assert.Equal(SymbolKind.Label, label.Kind);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForStatement_GetDeclaredSwitchLabelSymbol()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  void M(int x)
  {
    int y = 0;
  }
}
");

            var switchStatement = (SwitchStatementSyntax)SyntaxFactory.ParseStatement(@"
switch (y)
{
  case 0:
    y++;
    break;
}");
            var switchLabel = switchStatement.Sections[0].Labels[0];

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);
            SemanticModel statModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].Span.End, switchStatement, out statModel);
            Assert.True(success);
            Assert.NotNull(statModel);

            var symbol = statModel.GetDeclaredSymbol(switchLabel);
            Assert.NotNull(symbol);
            Assert.IsType<SourceLabelSymbol>(symbol);

            var labelSymbol = (SourceLabelSymbol)symbol;
            Assert.Equal(ConstantValue.Default(SpecialType.System_Int32), labelSymbol.SwitchCaseLabelConstant);
            Assert.Equal(switchLabel, labelSymbol.IdentifierNodeOrToken.AsNode());
            Assert.Equal("case 0:", labelSymbol.Name);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForStatement_GetDeclaredLambdaParameterSymbol()
        {
            var compilation = CreateCompilationWithMscorlibAndSystemCore(@"
using System.Linq;

class C 
{
  void M(int x)
  {
    int y = 0;
  }
}
");

            var speculatedStatement = (LocalDeclarationStatementSyntax)SyntaxFactory.ParseStatement(@"Func<int, int> var = (z) => x + z;");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].Span.End, speculatedStatement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var lambdaExpression = speculatedStatement.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().FirstOrDefault();
            var lambdaParam = lambdaExpression.ParameterList.Parameters[0];
            var parameterSymbol = speculativeModel.GetDeclaredSymbol(lambdaParam);
            Assert.NotNull(parameterSymbol);
            Assert.Equal("z", parameterSymbol.Name);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForStatement_ForEach()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  void M(int x)
  {
    var a = new [] {1, 2, 3};     
  }
}
");

            var statement = (ForEachStatementSyntax)SyntaxFactory.ParseStatement(@"
foreach(short ele in a)
{
} 
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].SpanStart, statement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            ForEachStatementInfo info = speculativeModel.GetForEachStatementInfo(statement);
            Assert.NotNull(info);
            Assert.Equal("System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()", info.GetEnumeratorMethod.ToTestDisplayString());
            Assert.Equal("System.Object System.Collections.IEnumerator.Current.get", info.CurrentProperty.GetMethod.ToTestDisplayString());
            Assert.Equal("System.Boolean System.Collections.IEnumerator.MoveNext()", info.MoveNextMethod.ToTestDisplayString());
            Assert.Equal("void System.IDisposable.Dispose()", info.DisposeMethod.ToTestDisplayString());
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelInAutoPropInitializer1()
        {
            var source = @"class C
{
    int y = 0;
    int X { get; } = 1;
}";

            var comp = CreateCompilationWithMscorlib(source, parseOptions: TestOptions.ExperimentalParseOptions);
            var tree = comp.SyntaxTrees.Single();

            var model = comp.GetSemanticModel(tree);
            Assert.False(model.IsSpeculativeSemanticModel);
            Assert.Null(model.ParentModel);
            Assert.Equal(0, model.OriginalPositionForSpeculation);

            // Speculate on the initializer
            var root = tree.GetCompilationUnitRoot();
            var oldSyntax = root.DescendantNodes()
                .OfType<EqualsValueClauseSyntax>().ElementAt(1);
            var position = oldSyntax.SpanStart;
            var newSyntax = SyntaxFactory.EqualsValueClause(
                SyntaxFactory.ParseExpression("this.y"));
            var expr = newSyntax.Value;

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(oldSyntax.SpanStart,
                newSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);
            Assert.True(speculativeModel.IsSpeculativeSemanticModel);
            Assert.Equal(model, speculativeModel.ParentModel);
            Assert.Equal(position, speculativeModel.OriginalPositionForSpeculation);

            var typeInfo = speculativeModel.GetTypeInfo(expr);
            Assert.NotNull(typeInfo);
            Assert.Equal("Int32", typeInfo.Type.Name);

            var thisSyntax = expr.DescendantNodes().OfType<ThisExpressionSyntax>().Single();
            var symbolInfo = speculativeModel.GetSpeculativeSymbolInfo(
                thisSyntax.SpanStart,
                thisSyntax, SpeculativeBindingOption.BindAsExpression);
            Assert.NotNull(symbolInfo);
            var candidates = symbolInfo.CandidateSymbols;
            Assert.Equal(1, candidates.Length);
            Assert.IsType<ThisParameterSymbol>(candidates[0]);
            Assert.Equal(CandidateReason.NotReferencable, symbolInfo.CandidateReason);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForConstructorInitializer()
        {
            var source = @"class C
{
    C(int x) { }
    C() : this((int) 1) { }
}";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var parentModel = comp.GetSemanticModel(tree);

            var oldSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().Single();

            var newSyntax = SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer);

            SemanticModel speculativeModel;
            bool success = parentModel.TryGetSpeculativeSemanticModel(oldSyntax.SpanStart, newSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var info = speculativeModel.GetSymbolInfo(newSyntax);
            var symbol = info.Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C"), symbol.ContainingType);
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            var method = (MethodSymbol)symbol;
            Assert.Equal(MethodKind.Constructor, method.MethodKind);
            Assert.Equal(0, method.ParameterCount);

            // test unnecessary cast removal
            var newArgument = SyntaxFactory.ParseExpression("1");
            newSyntax = oldSyntax.ReplaceNode(oldSyntax.DescendantNodes().OfType<CastExpressionSyntax>().Single(), newArgument);

            success = parentModel.TryGetSpeculativeSemanticModel(oldSyntax.SpanStart, newSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            info = speculativeModel.GetSymbolInfo(newSyntax);
            symbol = info.Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C"), symbol.ContainingType);
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            method = (MethodSymbol)symbol;
            Assert.Equal(MethodKind.Constructor, method.MethodKind);
            Assert.Equal(1, method.ParameterCount);

            // test incorrect cast replacement
            newArgument = SyntaxFactory.ParseExpression("(string) 1");
            newSyntax = oldSyntax.ReplaceNode(oldSyntax.DescendantNodes().OfType<CastExpressionSyntax>().Single(), newArgument);
            success = parentModel.TryGetSpeculativeSemanticModel(oldSyntax.SpanStart, newSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            info = speculativeModel.GetSymbolInfo(newSyntax);
            symbol = info.Symbol;
            Assert.Null(symbol);
            Assert.Equal(CandidateReason.OverloadResolutionFailure, info.CandidateReason);
            Assert.Equal(2, info.CandidateSymbols.Length);
            var sortedCandidates = info.CandidateSymbols.OrderBy(s => s.ToTestDisplayString()).ToArray();
            Assert.Equal("C..ctor()", sortedCandidates[0].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[0].Kind);
            Assert.Equal("C..ctor(System.Int32 x)", sortedCandidates[1].ToTestDisplayString());
            Assert.Equal(SymbolKind.Method, sortedCandidates[1].Kind);
        }

        [WorkItem(545882, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545882")]
        [Fact]
        public void TestGetSpeculativeSemanticModelForConstructorInitializer_UnsupportedLocation()
        {
            var source = @"class C
{
    C() { }
}";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var parentModel = comp.GetSemanticModel(tree);

            var oldSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            var newSyntax = SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer);

            SemanticModel speculativeModel;
            bool success = parentModel.TryGetSpeculativeSemanticModel(oldSyntax.SpanStart, newSyntax, out speculativeModel);
            Assert.False(success);
            Assert.Null(speculativeModel);
        }

        [Fact]
        public void TestArgumentsToGetSpeculativeSemanticModelAPI()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  public C(): this(0) {}
  public C(int i) {}

  [System.Obsolete]
  void M(int x)
  {
    string y = ""Hello"";     
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var ctor1 = (ConstructorDeclarationSyntax)typeDecl.Members[0];
            var ctor2 = (ConstructorDeclarationSyntax)typeDecl.Members[1];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[2];

            var model = compilation.GetSemanticModel(tree);
            var statement = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            var initializer = statement.Declaration.Variables[0].Initializer;
            var ctorInitializer = ctor1.Initializer;
            var attribute = methodDecl.AttributeLists[0].Attributes[0];

            SemanticModel speculativeModel;
            Assert.Throws<ArgumentNullException>(() => model.TryGetSpeculativeSemanticModel(statement.SpanStart, statement: null, speculativeModel: out speculativeModel));
            Assert.Throws<ArgumentNullException>(() => model.TryGetSpeculativeSemanticModel(statement.SpanStart, constructorInitializer: null, speculativeModel: out speculativeModel));
            Assert.Throws<ArgumentNullException>(() => model.TryGetSpeculativeSemanticModel(statement.SpanStart, attribute: null, speculativeModel: out speculativeModel));

            // Speculate on a node from the same syntax tree.
            Assert.Throws<ArgumentException>(() => model.TryGetSpeculativeSemanticModel(statement.SpanStart, statement: statement, speculativeModel: out speculativeModel));
            Assert.Throws<ArgumentException>(() => model.TryGetSpeculativeSemanticModel(ctorInitializer.SpanStart, constructorInitializer: ctorInitializer, speculativeModel: out speculativeModel));
            Assert.Throws<ArgumentException>(() => model.TryGetSpeculativeSemanticModel(attribute.SpanStart, attribute: attribute, speculativeModel: out speculativeModel));

            // Chaining speculative semantic model is not supported.
            var speculatedStatement = statement.ReplaceNode(initializer.Value, SyntaxFactory.ParseExpression("0"));
            model.TryGetSpeculativeSemanticModel(statement.SpanStart, speculatedStatement, speculativeModel: out speculativeModel);
            Assert.Throws<InvalidOperationException>(() => speculativeModel.TryGetSpeculativeSemanticModel(speculatedStatement.SpanStart, speculatedStatement, speculativeModel: out speculativeModel));
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelOnSpeculativeSemanticModel()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  public C(): this(0) {}
  public C(int i) {}

  [System.Obsolete]
  void M(int x)
  {
    string y = ""Hello"";     
  }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var ctor1 = (ConstructorDeclarationSyntax)typeDecl.Members[0];
            var ctor2 = (ConstructorDeclarationSyntax)typeDecl.Members[1];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[2];

            var model = compilation.GetSemanticModel(tree);
            var statement = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            var expression = statement.Declaration.Variables[0].Initializer.Value;
            var ctorInitializer = ctor1.Initializer;
            var attribute = methodDecl.AttributeLists[0].Attributes[0];

            var speculatedStatement = statement.ReplaceNode(expression, SyntaxFactory.ParseExpression("0"));
            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(statement.SpanStart, speculatedStatement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            // Chaining speculative semantic model is not supported.
            // (a) Expression
            var newSpeculatedStatement = statement.ReplaceNode(expression, SyntaxFactory.ParseExpression("1.1"));
            SemanticModel newModel;
            Assert.Throws<InvalidOperationException>(() => speculativeModel.TryGetSpeculativeSemanticModel(speculatedStatement.SpanStart, newSpeculatedStatement, out newModel));

            // (b) Statement
            newSpeculatedStatement = (LocalDeclarationStatementSyntax)SyntaxFactory.ParseStatement(@"int z = 0;");
            Assert.Throws<InvalidOperationException>(() => speculativeModel.TryGetSpeculativeSemanticModel(speculatedStatement.SpanStart, newSpeculatedStatement, out newModel));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/8778")]
        public void TestGetSpeculativeSemanticModelInsideUnsafeCode()
        {
            var compilation = CreateCompilationWithMscorlib(@"
unsafe class C
{
    void M()
    {
        int x;
    }
}
");
            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];

            var model = compilation.GetSemanticModel(tree);

            var unsafeStatement = (LocalDeclarationStatementSyntax)SyntaxFactory.ParseStatement("int *p = &x;");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].Span.End, unsafeStatement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var declarator = unsafeStatement.Declaration.Variables.First();
            var initializer = declarator.Initializer.Value;

            var binder = ((CSharpSemanticModel)speculativeModel).GetEnclosingBinder(initializer.SpanStart);
            Assert.True(binder.InUnsafeRegion, "must be in unsafe code");
            Assert.True(binder.IsSemanticModelBinder, "must be speculative");

            var typeInfo = speculativeModel.GetTypeInfo(initializer);
            Assert.Equal("System.Int32*", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Pointer, typeInfo.Type.TypeKind);
            Assert.Equal("System.Int32*", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Pointer, typeInfo.ConvertedType.TypeKind);

            var conv = speculativeModel.GetConversion(initializer);
            Assert.Equal(ConversionKind.Identity, conv.Kind);

            var symbol = speculativeModel.GetDeclaredSymbol(declarator);
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.Local, symbol.Kind);
            Assert.Equal("p", symbol.Name);
        }

        [WorkItem(663704, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/663704")]
        [Fact]
        public void TestGetSpeculativeSemanticModelInsideUnknownAccessor()
        {
            var source = @"
class C
{
    C P
    {
        foo
        {
            return null;
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var accessorSyntax = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();
            Assert.Equal(SyntaxKind.UnknownAccessorDeclaration, accessorSyntax.Kind());

            var statementSyntax = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single();
            var memberModel = ((CSharpSemanticModel)model).GetMemberModel(statementSyntax);
            Assert.Null(memberModel); // No member model since no symbol.

            var speculativeSyntax = SyntaxFactory.ParseStatement("return default(C);");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(statementSyntax.SpanStart, speculativeSyntax, out speculativeModel);
            Assert.False(success);
            Assert.Null(speculativeModel);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForMethodBody()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  void M(int x)
  {
    int y = 1000;     
  }
}
");

            var blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"
{ 
   int z = 0; 
   M(z);  
   M(y);    // Should generate error here as we are replacing the method body.
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);

            var speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].SpanStart, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            VerifySpeculativeSemanticModelForMethodBody(blockStatement, speculativeModel);
        }

        private static void VerifySpeculativeSemanticModelForMethodBody(BlockSyntax blockStatement, SemanticModel speculativeModel)
        {
            var localDecl = (LocalDeclarationStatementSyntax)blockStatement.Statements[0];
            var declarator = localDecl.Declaration.Variables.First();
            var local = speculativeModel.GetDeclaredSymbol(declarator);
            Assert.NotNull(local);
            Assert.Equal("z", local.Name);
            Assert.Equal(SymbolKind.Local, local.Kind);
            Assert.Equal("Int32", ((LocalSymbol)local).Type.Name);

            var typeInfo = speculativeModel.GetTypeInfo(localDecl.Declaration.Type);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("Int32", typeInfo.Type.Name);

            var call = (InvocationExpressionSyntax)((ExpressionStatementSyntax)blockStatement.Statements[1]).Expression;
            var arg = call.ArgumentList.Arguments[0].Expression;
            var info = speculativeModel.GetSymbolInfo(arg);
            Assert.NotNull(info.Symbol);
            Assert.Equal("z", info.Symbol.Name);
            Assert.Equal(SymbolKind.Local, info.Symbol.Kind);

            // Shouldn't bind to local y in the original method as we are replacing the method body.
            var call2 = (InvocationExpressionSyntax)((ExpressionStatementSyntax)((BlockSyntax)blockStatement).Statements[2]).Expression;
            var arg2 = call2.ArgumentList.Arguments[0].Expression;
            var info2 = speculativeModel.GetSymbolInfo(arg2);
            Assert.Null(info2.Symbol);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForIndexerAccessorBody()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
    private int this[int x]
    {
        set
        {
            int y = 1000;
        }
    }
}
");

            var blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"
{ 
   int z = 0; 
   M(z);  
   M(y);    // Should generate error here as we are replacing the method body.
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var indexerDecl = (IndexerDeclarationSyntax)typeDecl.Members[0];
            var methodDecl = indexerDecl.AccessorList.Accessors[0];
            var model = compilation.GetSemanticModel(tree);

            var speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].SpanStart, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            VerifySpeculativeSemanticModelForMethodBody(blockStatement, speculativeModel);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForPropertyAccessorBody()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
    private int M
    {
        set
        {
            int y = 1000;
        }
    }
}
");

            var blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"
{ 
   int z = 0; 
   M(z);  
   M(y);    // Should generate error here as we are replacing the method body.
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var propertyDecl = (PropertyDeclarationSyntax)typeDecl.Members[0];
            var methodDecl = propertyDecl.AccessorList.Accessors[0];
            var model = compilation.GetSemanticModel(tree);

            var speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].SpanStart, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            VerifySpeculativeSemanticModelForMethodBody(blockStatement, speculativeModel);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForEventAccessorBody()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
    private event System.Action E
    {
        add
        {
            int y = 1000;
        }
        remove
        {
        }
    }
}
");

            var blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"
{ 
   int z = 0; 
   M(z);  
   M(y);    // Should generate error here as we are replacing the method body.
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var eventDecl = (EventDeclarationSyntax)typeDecl.Members[0];
            var methodDecl = eventDecl.AccessorList.Accessors[0];
            var model = compilation.GetSemanticModel(tree);

            var speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].SpanStart, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            VerifySpeculativeSemanticModelForMethodBody(blockStatement, speculativeModel);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForMethodBody_DeclaredLocal()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  void M(int x)
  {
    int y = 1000;     
  }
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);

            // different name local
            var blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"{ int z = 0; }");
            var speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].SpanStart, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var declarator = ((LocalDeclarationStatementSyntax)blockStatement.Statements[0]).Declaration.Variables.First();
            var local = speculativeModel.GetDeclaredSymbol(declarator);
            Assert.NotNull(local);
            Assert.Equal("z", local.Name);
            Assert.Equal(SymbolKind.Local, local.Kind);
            Assert.Equal("Int32", ((LocalSymbol)local).Type.Name);

            // same name local
            blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"{ string y = null; }");
            speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;
            success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].SpanStart, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);
            declarator = ((LocalDeclarationStatementSyntax)blockStatement.Statements[0]).Declaration.Variables.First();
            local = speculativeModel.GetDeclaredSymbol(declarator);
            Assert.NotNull(local);
            Assert.Equal("y", local.Name);
            Assert.Equal(SymbolKind.Local, local.Kind);
            Assert.Equal("String", ((LocalSymbol)local).Type.Name);

            // parameter symbol
            blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"{ var y = x; }");
            speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;
            success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].SpanStart, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);
            declarator = ((LocalDeclarationStatementSyntax)blockStatement.Statements[0]).Declaration.Variables.First();
            local = speculativeModel.GetDeclaredSymbol(declarator);
            Assert.NotNull(local);
            Assert.Equal("y", local.Name);
            Assert.Equal(SymbolKind.Local, local.Kind);
            Assert.Equal("Int32", ((LocalSymbol)local).Type.Name);

            var param = speculativeModel.GetSymbolInfo(declarator.Initializer.Value).Symbol;
            Assert.NotNull(param);
            Assert.Equal(SymbolKind.Parameter, param.Kind);
            var paramSymbol = (ParameterSymbol)param;
            Assert.Equal("x", paramSymbol.Name);
            Assert.Equal("Int32", paramSymbol.Type.Name);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForMethodBody_GetDeclaredLabelSymbol()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class C 
{
  void M(int x)
  {
    int y = 1000;     
  }
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);

            var blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"{ label: y++; }");
            var speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;
            var labeledStatement = blockStatement.Statements[0];

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].SpanStart, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var label = speculativeModel.GetDeclaredSymbol(labeledStatement);
            Assert.NotNull(label);
            Assert.Equal("label", label.Name);
            Assert.Equal(SymbolKind.Label, label.Kind);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForMethodBody_GetDeclaredLambdaParameterSymbol()
        {
            var compilation = CreateCompilationWithMscorlibAndSystemCore(@"
using System.Linq;

class C 
{
  void M(int x)
  {
    int z = 0;
  }
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);

            var blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"{ Func<int, int> var = (z) => x + z; }");
            var speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].Span.End, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var lambdaExpression = blockStatement.Statements[0].DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().FirstOrDefault();
            var lambdaParam = lambdaExpression.ParameterList.Parameters[0];
            var parameterSymbol = speculativeModel.GetDeclaredSymbol(lambdaParam);
            Assert.NotNull(parameterSymbol);
            Assert.Equal("z", parameterSymbol.Name);
        }

        private static void TestGetSpeculativeSemanticModelForTypeSyntax_Common(
            SemanticModel model,
            int position,
            TypeSyntax speculatedTypeSyntax,
            SpeculativeBindingOption bindingOption,
            SymbolKind expectedSymbolKind,
            string expectedTypeDisplayString)
        {
            Assert.False(model.IsSpeculativeSemanticModel);
            Assert.Null(model.ParentModel);
            Assert.Equal(0, model.OriginalPositionForSpeculation);

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(position, speculatedTypeSyntax, out speculativeModel, bindingOption);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            Assert.True(speculativeModel.IsSpeculativeSemanticModel);
            Assert.Equal(model, speculativeModel.ParentModel);
            Assert.NotNull(speculativeModel);
            Assert.Equal(position, speculativeModel.OriginalPositionForSpeculation);

            var symbol = speculativeModel.GetSymbolInfo(speculatedTypeSyntax).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(expectedSymbolKind, symbol.Kind);
            Assert.Equal(expectedTypeDisplayString, symbol.ToDisplayString());

            var typeSymbol = speculativeModel.GetTypeInfo(speculatedTypeSyntax).Type;
            Assert.NotNull(symbol);
            Assert.Equal(expectedSymbolKind, symbol.Kind);
            Assert.Equal(expectedTypeDisplayString, symbol.ToDisplayString());

            if (speculatedTypeSyntax.Kind() == SyntaxKind.QualifiedName)
            {
                var right = ((QualifiedNameSyntax)speculatedTypeSyntax).Right;

                symbol = speculativeModel.GetSymbolInfo(right).Symbol;
                Assert.NotNull(symbol);
                Assert.Equal(expectedSymbolKind, symbol.Kind);
                Assert.Equal(expectedTypeDisplayString, symbol.ToDisplayString());

                typeSymbol = speculativeModel.GetTypeInfo(right).Type;
                Assert.NotNull(symbol);
                Assert.Equal(expectedSymbolKind, symbol.Kind);
                Assert.Equal(expectedTypeDisplayString, symbol.ToDisplayString());
            }
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForMethodBody_SwitchStatement()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System;

class C 
{
  void M(int x)
  {
    switch(x)
    {
        case 0:
            Console.WriteLine(x);
            break;
    }
  }
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);

            var blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"{
    switch(x)
    {
        case 1:
            Console.WriteLine(x);
            break;
    }
}");

            var speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;
            var switchStatement = (SwitchStatementSyntax)blockStatement.Statements[0];

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].SpanStart, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var switchLabel = switchStatement.Sections[0].Labels[0] as CaseSwitchLabelSyntax;
            Assert.NotNull(switchLabel);
            var constantVal = speculativeModel.GetConstantValue(switchLabel.Value);
            Assert.True(constantVal.HasValue);
            Assert.Equal(1, constantVal.Value);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForTypeSyntax_InGlobalUsing()
        {
            var compilation = CreateCompilationWithMscorlib(@"using System.Runtime;");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var usingStatement = root.Usings[0];
            var model = compilation.GetSemanticModel(tree);

            var speculatedUsingExpression = SyntaxFactory.ParseName("System.Collections");
            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, usingStatement.Name.Position,
                speculatedUsingExpression, SpeculativeBindingOption.BindAsTypeOrNamespace, SymbolKind.Namespace, "System.Collections");
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForTypeSyntax_InGlobalAlias()
        {
            var compilation = CreateCompilationWithMscorlib(@"using A = System.Exception;");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var usingStatement = root.Usings[0];
            var model = compilation.GetSemanticModel(tree);

            var speculatedUsingExpression = SyntaxFactory.ParseName("System.ArgumentException");
            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, usingStatement.Name.Position,
                speculatedUsingExpression, SpeculativeBindingOption.BindAsExpression, SymbolKind.NamedType, "System.ArgumentException");
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForTypeSyntax_InBaseList()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class MyException : System.Exception
{
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var baseList = typeDecl.BaseList;
            var model = compilation.GetSemanticModel(tree);

            var speculatedTypeExpression = SyntaxFactory.ParseName("System.ArgumentException");
            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, baseList.SpanStart,
                speculatedTypeExpression, SpeculativeBindingOption.BindAsTypeOrNamespace, SymbolKind.NamedType, "System.ArgumentException");
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForTypeSyntax_InMemberDeclaration()
        {
            var compilation = CreateCompilationWithMscorlib(@"
class Program
{
    System.Exception field = null;
    System.Exception Method(System.Exception param)
    {
        return field;
    }
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var fieldDecl = (FieldDeclarationSyntax)typeDecl.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[1];
            var model = compilation.GetSemanticModel(tree);

            var speculatedTypeExpression = SyntaxFactory.ParseName("System.ArgumentException");
            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, fieldDecl.SpanStart,
                speculatedTypeExpression, SpeculativeBindingOption.BindAsTypeOrNamespace, SymbolKind.NamedType, "System.ArgumentException");

            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, methodDecl.ReturnType.SpanStart,
                speculatedTypeExpression, SpeculativeBindingOption.BindAsTypeOrNamespace, SymbolKind.NamedType, "System.ArgumentException");

            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, methodDecl.ParameterList.Parameters.First().SpanStart,
                speculatedTypeExpression, SpeculativeBindingOption.BindAsTypeOrNamespace, SymbolKind.NamedType, "System.ArgumentException");
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForTypeSyntax_AliasName()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using A = System.ArgumentException;

class Program
{
    System.Exception field = null;
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var fieldDecl = (FieldDeclarationSyntax)typeDecl.Members[0];
            var model = compilation.GetSemanticModel(tree);

            var speculatedAliasName = SyntaxFactory.ParseName("A");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(fieldDecl.SpanStart, speculatedAliasName, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var symbol = (AliasSymbol)speculativeModel.GetAliasInfo(speculatedAliasName);
            Assert.NotNull(symbol);
            Assert.Equal("A", symbol.ToDisplayString());
            Assert.Equal("System.ArgumentException", symbol.Target.ToDisplayString());
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForTypeCrefSyntax()
        {
            var compilation = CreateCompilationWithMscorlibAndDocumentationComments(@"
/// <summary>
/// <see cref=""int""/>
/// </summary>
class A { }
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var cref = typeDecl.DescendantNodes(descendIntoTrivia: true).OfType<TypeCrefSyntax>().Single();

            var model = (CSharpSemanticModel)compilation.GetSemanticModel(tree);
            var symbol = model.GetSymbolInfo(cref.Type).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal("int", symbol.ToDisplayString());

            var speculatedCref = (TypeCrefSyntax)SyntaxFactory.ParseCref("object");

            // TryGetSpeculativeSemanticModel
            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(cref.SpanStart, speculatedCref, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbol = speculativeModel.GetSymbolInfo(speculatedCref.Type).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.NamedType, symbol.Kind);
            Assert.Equal("object", symbol.ToDisplayString());

            // GetSpeculativeSymbolInfo
            symbol = model.GetSpeculativeSymbolInfo(cref.SpanStart, speculatedCref).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.NamedType, symbol.Kind);
            Assert.Equal("object", symbol.ToDisplayString());
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForNameMemberCrefSyntax()
        {
            var compilation = CreateCompilationWithMscorlibAndDocumentationComments(@"
/// <summary>
/// <see cref=""int""/>
/// </summary>
class A { }
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var cref = typeDecl.DescendantNodes(descendIntoTrivia: true).OfType<TypeCrefSyntax>().Single();

            var model = (CSharpSemanticModel)compilation.GetSemanticModel(tree);
            var symbol = model.GetSymbolInfo(cref.Type).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal("int", symbol.ToDisplayString());

            var speculatedCref = (NameMemberCrefSyntax)SyntaxFactory.ParseCref("A");

            // TryGetSpeculativeSemanticModel
            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(cref.SpanStart, speculatedCref, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbol = speculativeModel.GetSymbolInfo(speculatedCref.Name).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.NamedType, symbol.Kind);
            Assert.Equal("A", symbol.ToDisplayString());

            // GetSpeculativeSymbolInfo
            symbol = model.GetSpeculativeSymbolInfo(cref.SpanStart, speculatedCref).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.NamedType, symbol.Kind);
            Assert.Equal("A", symbol.ToDisplayString());
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForQualifiedCrefSyntax()
        {
            var compilation = CreateCompilationWithMscorlibAndDocumentationComments(@"
/// <summary>
/// <see cref=""int""/>
/// </summary>
class A
{
    class B { }

    static void M() { }
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var cref = typeDecl.DescendantNodes(descendIntoTrivia: true).OfType<TypeCrefSyntax>().Single();

            var model = (CSharpSemanticModel)compilation.GetSemanticModel(tree);
            var symbol = model.GetSymbolInfo(cref.Type).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal("int", symbol.ToDisplayString());

            var speculatedCref = (QualifiedCrefSyntax)SyntaxFactory.ParseCref("A.B");

            // Type member: TryGetSpeculativeSemanticModel
            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(cref.SpanStart, speculatedCref, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbol = speculativeModel.GetSymbolInfo(speculatedCref).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.NamedType, symbol.Kind);
            Assert.Equal("A.B", symbol.ToDisplayString());

            symbol = speculativeModel.GetSymbolInfo(speculatedCref.Member).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.NamedType, symbol.Kind);
            Assert.Equal("A.B", symbol.ToDisplayString());

            symbol = speculativeModel.GetTypeInfo(speculatedCref.Container).Type;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.NamedType, symbol.Kind);
            Assert.Equal("A", symbol.ToDisplayString());

            // Type member: GetSpeculativeSymbolInfo
            symbol = model.GetSpeculativeSymbolInfo(cref.SpanStart, speculatedCref).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.NamedType, symbol.Kind);
            Assert.Equal("A.B", symbol.ToDisplayString());

            // Method member: TryGetSpeculativeSemanticModel
            speculatedCref = (QualifiedCrefSyntax)SyntaxFactory.ParseCref("A.M");

            success = model.TryGetSpeculativeSemanticModel(cref.SpanStart, speculatedCref, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            symbol = speculativeModel.GetSymbolInfo(speculatedCref).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            Assert.Equal("A.M()", symbol.ToDisplayString());

            symbol = speculativeModel.GetSymbolInfo(speculatedCref.Member).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            Assert.Equal("A.M()", symbol.ToDisplayString());

            symbol = speculativeModel.GetTypeInfo(speculatedCref.Container).Type;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.NamedType, symbol.Kind);
            Assert.Equal("A", symbol.ToDisplayString());

            // Method member: GetSpeculativeSymbolInfo
            symbol = model.GetSpeculativeSymbolInfo(cref.SpanStart, speculatedCref).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            Assert.Equal("A.M()", symbol.ToDisplayString());
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForCrefSyntax_InvalidPosition()
        {
            var compilation = CreateCompilationWithMscorlibAndDocumentationComments(@"
/// <summary>
/// <see cref=""int""/>
/// </summary>
class A
{
    static void M() { }
}
");

            var tree = compilation.SyntaxTrees[0];
            var root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            var model = (CSharpSemanticModel)compilation.GetSemanticModel(tree);

            var speculatedCref = (TypeCrefSyntax)SyntaxFactory.ParseCref("object");

            // TryGetSpeculativeSemanticModel
            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(typeDecl.SpanStart, speculatedCref, out speculativeModel);
            Assert.False(success);

            success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.SpanStart, speculatedCref, out speculativeModel);
            Assert.False(success);

            // GetSpeculativeSymbolInfo
            var symbolInfo = model.GetSpeculativeSymbolInfo(methodDecl.Body.SpanStart, speculatedCref);
            Assert.Null(symbolInfo.Symbol);
        }

        [WorkItem(731108, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/731108")]
        [Fact]
        public void Repro731108()
        {
            var source = @"
public class C
{
    static void Main()
    {
        M(async x => x);
    }

    static void M(C c)
    {
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var syntax = tree.GetRoot().DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().Single().
                Body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Single();
            Assert.Equal("x", syntax.Identifier.ValueText);

            var symbol = model.GetSymbolInfo(syntax).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
            Assert.Equal("x", symbol.Name);
            Assert.Equal(TypeKind.Error, ((ParameterSymbol)symbol).Type.TypeKind);
        }

        [WorkItem(783566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/783566")]
        [Fact]
        public void SpeculateAboutYieldStatement1()
        {
            var source = @"
class C
{
    void M() // No way to infer iterator element type.
    {
        return;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var position = source.IndexOf("return", StringComparison.Ordinal);
            var yieldStatement = (YieldStatementSyntax)SyntaxFactory.ParseStatement("yield return 1;");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(position, yieldStatement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var info = speculativeModel.GetTypeInfo(yieldStatement.Expression);
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
            Assert.Equal(TypeKind.Error, info.ConvertedType.TypeKind);
        }

        [WorkItem(783566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/783566")]
        [Fact]
        public void SpeculateAboutYieldStatement2()
        {
            var source = @"
using System.Collections.Generic;

class C
{
    IEnumerable<long> M() // Can infer iterator element type.
    {
        return null;
    }
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var position = source.IndexOf("return", StringComparison.Ordinal);
            var yieldStatement = (YieldStatementSyntax)SyntaxFactory.ParseStatement("yield return 1;");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(position, yieldStatement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var info = speculativeModel.GetTypeInfo(yieldStatement.Expression);
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
            Assert.Equal(SpecialType.System_Int64, info.ConvertedType.SpecialType);
        }

        [WorkItem(791794, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/791794")]
        [Fact]
        public void SpeculateAboutOmittedArraySizeInCref()
        {
            var source = @"
/// <see cref=""int""/>
class C
{
}
";

            var comp = CreateCompilationWithMscorlibAndDocumentationComments(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var position = source.IndexOf("int", StringComparison.Ordinal);
            var typeSyntax = SyntaxFactory.ParseTypeName("System.Collections.Generic.IEnumerable<C[]>");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(position, typeSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var omittedArraySize = typeSyntax.DescendantNodes().OfType<OmittedArraySizeExpressionSyntax>().Single();
            var info = speculativeModel.GetSymbolInfo(omittedArraySize); // Used to throw NRE.
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
            Assert.Equal(0, info.CandidateSymbols.Length);
        }

        [WorkItem(784255, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/784255")]
        [Fact]
        public void Repro784255()
        {
            var source = @"
using System;

public class CategoryAttribute : Attribute
{
    public CategoryAttribute(string s) { }
    private CategoryAttribute() { }
}

struct S
{
}
";

            var comp = CreateCompilationWithMscorlib(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var position = source.IndexOf("struct", StringComparison.Ordinal);
            var attributeSyntax = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Category"));

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(position, attributeSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var info = speculativeModel.GetSymbolInfo(attributeSyntax.Name);
        }

        [WorkItem(1015557, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015557")]
        [Fact(Skip = "1015557")]
        public void GetSpeculativeSymbolInfoForGenericNameInCref()
        {
            var tree = CSharpSyntaxTree.ParseText(@"using System.Collections.Generic;
class Program
{
    /// <summary>
    /// <see cref=""System.Collections.Generic.List{T}.Contains(T)""/>
    /// </summary>
    static void Main()
    {
    }
}", CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose));
            var compilation = CreateCompilationWithMscorlib(tree);
            var root = tree.GetCompilationUnitRoot();
            var crefSyntax = root.DescendantNodes(descendIntoTrivia: true).OfType<QualifiedCrefSyntax>().Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var symbolInfo = semanticModel.GetSymbolInfo(crefSyntax.FindNode(new TextSpan(91, 34)));
            var oldSymbol = symbolInfo.Symbol;
            Assert.NotNull(oldSymbol);
            Assert.Equal(SymbolKind.NamedType, oldSymbol.Kind);
            Assert.Equal("System.Collections.Generic.List<T>", oldSymbol.ToTestDisplayString());

            var speculatedName = (GenericNameSyntax)SyntaxFactory.GenericName("List{T}");
            var speculativeSymbolInfo = semanticModel.GetSpeculativeSymbolInfo(crefSyntax.SpanStart, speculatedName, SpeculativeBindingOption.BindAsExpression);
            var newSymbol = speculativeSymbolInfo.Symbol;
            Assert.NotNull(newSymbol);
            Assert.Equal(SymbolKind.NamedType, newSymbol.Kind);
            Assert.Equal("System.Collections.Generic.List<T>", newSymbol.ToTestDisplayString());

            Assert.False(((NamedTypeSymbol)newSymbol).TypeArguments.Single().IsErrorType());
            Assert.True(newSymbol.Equals(oldSymbol));
        }

        [WorkItem(823791, "DevDiv")]
        [Fact]
        public void LambdaArgumentInBadCall_Constructor()
        {
            var source = @"
using System;

class Test
{
    void M()
    {
        new Test(s => s.
    }

    Test(Action<string> a, int i) { }
}
";
            CheckLambdaArgumentInBadCall(source);
        }

        [WorkItem(823791, "DevDiv")]
        [Fact]
        public void LambdaArgumentInBadCall_Method()
        {
            var source = @"
using System;

class Test
{
    void M()
    {
        Method(s => s.
    }

    void Method(Action<string> a, int i) { }
}
";
            CheckLambdaArgumentInBadCall(source);
        }

        [WorkItem(823791, "DevDiv")]
        [Fact]
        public void LambdaArgumentInBadCall_Indexer()
        {
            var source = @"
using System;

class Test
{
    void M()
    {
        var t = this[s => s.
    }

    int this[Action<string> a, int i] { get { return 0; } }
}
";
            CheckLambdaArgumentInBadCall(source);
        }

        [WorkItem(823791, "DevDiv")]
        [Fact]
        public void LambdaArgumentInBadCall_Delegate()
        {
            var source = @"
using System;

class Test
{
    void M()
    {
        d(s => s.
    }

    Action<Action<string>, int> d = null;
}
";
            CheckLambdaArgumentInBadCall(source);
        }

        [WorkItem(823791, "DevDiv")]
        [Fact]
        public void LambdaArgumentInBadCall_ConstructorInitializer()
        {
            var source = @"
using System;

class Test
{
    protected Test(Action<string> a, int i) { }
}

class Derived : Test
{
    Derived()
        : base(s => s.
    {
    }
}
";
            CheckLambdaArgumentInBadCall(source);
        }

        private static void CheckLambdaArgumentInBadCall(string source)
        {
            var comp = CreateCompilationWithMscorlib(source);

            var tree = comp.SyntaxTrees.Single();
            Assert.NotEmpty(tree.GetDiagnostics());

            var memberAccess = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
            var identifier = (IdentifierNameSyntax)memberAccess.Expression;
            Assert.Equal("s", identifier.Identifier.ValueText);

            var stringType = comp.GetSpecialType(SpecialType.System_String);
            var actionType = comp.GetWellKnownType(WellKnownType.System_Action_T).Construct(stringType);

            var model = comp.GetSemanticModel(tree);

            // Can't walk up to ArgumentListSyntax because indexers use BracketedArgumentListSyntax.
            var expr = identifier.FirstAncestorOrSelf<ArgumentSyntax>().Parent.Parent;

            var exprInfo = model.GetSymbolInfo(expr);
            var firstParamType = ((Symbol)exprInfo.CandidateSymbols.Single()).GetParameterTypes().First();
            Assert.Equal(actionType, firstParamType);

            var identifierInfo = model.GetTypeInfo(identifier);
            Assert.Equal(stringType, identifierInfo.Type);
            Assert.Equal(stringType, identifierInfo.ConvertedType);
        }

        [WorkItem(850907, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850907")]
        [Fact]
        public void ExtensionMethodViability()
        {
            var source = @"
static class Extensions
{
    private static void ToString(this object o, int x)
    {
        o.ToString(1);
    }
}
";

            var comp = CreateCompilationWithMscorlibAndSystemCore(source);
            comp.VerifyDiagnostics();

            var extensionMethod = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Extensions").GetMember<MethodSymbol>("ToString");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var callSyntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            var memberGroup = model.GetMemberGroup(callSyntax.Expression);
            Assert.Contains(extensionMethod.ReduceExtensionMethod(), memberGroup);
        }

        [WorkItem(849698, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/849698")]
        [Fact]
        public void LookupExternAlias()
        {
            var source = @"
extern alias Alias;

class C
{
    static void Main()
    {
        Alias::C.M(); 
    }

";

            var libRef = CreateCompilationWithMscorlib("", assemblyName: "lib").EmitToImageReference(aliases: ImmutableArray.Create("Alias"));
            var comp = CreateCompilationWithMscorlib(source, new[] { libRef });
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var syntax = tree.GetRoot().DescendantNodes().OfType<AliasQualifiedNameSyntax>().Single();

            var symbol = model.LookupSymbols(syntax.SpanStart, name: "Alias").Single();
            Assert.Equal("Alias", symbol.Name);
            Assert.Equal(SymbolKind.Alias, symbol.Kind);

            var target = (NamespaceSymbol)((AliasSymbol)symbol).Target;
            Assert.True(target.IsGlobalNamespace);
            Assert.Equal("lib", target.ContainingAssembly.Name);
        }

        [WorkItem(1019366, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1019366")]
        [WorkItem(273, "CodePlex")]
        [ClrOnlyFact]
        public void Bug1019366()
        {
            var source = @"
using System;

static class Program
{
    static void Main()
    {
        short case1 = unchecked((short)65535.17567);
        Console.WriteLine(case1);
        int? case2 = (int?)5.5;
        Console.WriteLine(case2);
        int? case3 = (int?)5;
        Console.WriteLine(case3);
    }
}
";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            var init0 = method.Body.Statements[0].DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer.Value;
            var value0 = model.GetConstantValue(init0);
            var typeInfo0 = model.GetTypeInfo(init0);
            Assert.True(value0.HasValue);
            Assert.Equal(-1, (short)value0.Value);
            Assert.True(typeInfo0.Type != null && typeInfo0.Type.SpecialType == SpecialType.System_Int16);

            // The CodePlex bug indicates this should return a constant value of 5.  While 'case2' should 
            // have that value it is not constant because of the nullable cast
            var init1 = method.Body.Statements[2].DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer.Value;
            var value1 = model.GetConstantValue(init1);
            var typeInfo1 = model.GetTypeInfo(init1);
            var type1 = comp.GetSpecialType(SpecialType.System_Nullable_T).Construct(comp.GetSpecialType(SpecialType.System_Int32));
            Assert.False(value1.HasValue);
            Assert.True(typeInfo1.Type != null && typeInfo1.Type.Equals(type1));

            var init2 = method.Body.Statements[4].DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer.Value;
            var value2 = model.GetConstantValue(init2);
            var typeInfo2 = model.GetTypeInfo(init2);
            var type2 = comp.GetSpecialType(SpecialType.System_Nullable_T).Construct(comp.GetSpecialType(SpecialType.System_Int32));
            Assert.False(value2.HasValue);
            Assert.True(typeInfo2.Type != null && typeInfo2.Type.Equals(type2));

            var output = @"
-1
5
5";
            CompileAndVerify(compilation: comp, expectedOutput: output);
        }

        [Fact]
        public void Regression01()
        {
            Regression(@"
using System;

class MyClass {

	public protected int intI = 1;

	public static int Main() {
		return 1;
	}
}"
                );
        }

        [Fact]
        public void Regression02()
        {
            Regression(@"
using System;

class Convert
{
	public static void Main()
	{
		S s = new S();
	}
}
"
                );
        }

        [Fact]
        public void Regression03()
        {
            Regression(@"
using System;

namespace Microsoft.Conformance.Expressions
{
    using basic097LevelOne.basic097LevelTwo;

    public class basic097I<A1>
    {
        public static int Method()
        {
            return 0;
        }
    }

    namespace basic097LevelOne
    {
        namespace basic097LevelTwo
        {
            public class basic097I<A1>
            {
            }
        }
    } 
}"
                );
        }

        [Fact, WorkItem(1504, "https://github.com/dotnet/roslyn/issues/1504")]
        public void ContainingSymbol02()
        {
            var source =
@"using System;
class C
{
    static bool G<T>(Func<T> f) => true;
    static void F()
    {
        Exception x1 = null;
        try
        {
            G(() => x1);
        }
        catch (Exception x2) when (G(() => x2))
        {
        }
    }
}";

            var compilation = CreateCompilationWithMscorlibAndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            for (var match = System.Text.RegularExpressions.Regex.Match(source, " => x"); match.Success; match = match.NextMatch())
            {
                var discarded = model.GetEnclosingSymbol(match.Index);
            }
        }

        [Fact, WorkItem(1504, "https://github.com/dotnet/roslyn/issues/1504")]
        public void ContainingSymbol03()
        {
            var source =
@"using System;
class C
{
    static bool G<T>(Func<T> f) => true;
    static void F()
    {
        Exception x1 = null, x2 = null;
        do
        {
            G(() => x1);
        }
        while (G(() => x2));
    }
}";

            var compilation = CreateCompilationWithMscorlibAndDocumentationComments(source);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            for (var match = System.Text.RegularExpressions.Regex.Match(source, " => x"); match.Success; match = match.NextMatch())
            {
                var x = tree.GetRoot().FindToken(match.Index + 4).Parent;
                var discarded = model.GetEnclosingSymbol(match.Index);
                var disc = model.GetSymbolInfo(x);
            }
        }

        [Fact, WorkItem(1504, "https://github.com/dotnet/roslyn/issues/1504")]
        public void ContainingSymbol04()
        {
            var source =
@"using System;
class C
{
    static bool G<T>(Func<T> f) => true;
    static void F()
    {
        Exception x1 = null, x2 = null;
        if (G(() => x1));
        {
            G(() => x2);
        }
    }
}";

            var compilation = CreateCompilationWithMscorlibAndDocumentationComments(source);
            var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            var discarded1 = model.GetEnclosingSymbol(source.LastIndexOf(" => x"));
            var discarded2 = model.GetEnclosingSymbol(source.IndexOf(" => x"));
        }

        [WorkItem(976, "https://github.com/dotnet/roslyn/issues/976")]
        [Fact]
        public void ConstantValueOfInterpolatedString()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($""Hello, world!"");
        Console.WriteLine($""{DateTime.Now.ToString()}.{args[0]}"");
    }
}";

            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            foreach (var interp in tree.GetRoot().DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
            {
                Assert.False(model.GetConstantValue(interp).HasValue);
            }
        }

        [WorkItem(814, "https://github.com/dotnet/roslyn/issues/814")]
        [Fact]
        public void TypeOfDynamic()
        {
            var source = @"
using System;
using System.Dynamic;
    class Program
    {
        static void Main(string[] args)
        {
            dynamic a = 5;
        }   
     }
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var typeSyntax = SyntaxFactory.ParseTypeName("dynamic");
            int spanStart = source.IndexOf("dynamic a = 5;");
            var dynamicType = model.GetSpeculativeTypeInfo(spanStart, typeSyntax, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Equal(TypeKind.Dynamic, dynamicType.Type.TypeKind);
        }

        #region "regression helper"
        private void Regression(string text)
        {
            var tree = Parse(text);
            var compilation = CreateCompilationWithMscorlib(tree);
            var model = compilation.GetSemanticModel(tree);
            var exprSynList = new List<ExpressionSyntax>();
            GetExpressionSyntax(tree.GetCompilationUnitRoot(), exprSynList);

            // Console.WriteLine("Roslyn Symbol Info: ");
            foreach (var exprSyn in exprSynList)
            {
                var expr = exprSyn.ToString();
                // Console.WriteLine("Expression: " + expr);
                var type = model.GetTypeInfo(exprSyn).Type;
                // Console.WriteLine("Type: " + type);
                // Console.WriteLine();
            }
        }
        private static void GetExpressionSyntax(SyntaxNode node, List<ExpressionSyntax> exprSynList)
        {
            if (node is ExpressionSyntax)
                exprSynList.Add(node as ExpressionSyntax);

            foreach (var child in node.ChildNodesAndTokens())
                if (child.IsNode)
                    GetExpressionSyntax(child.AsNode(), exprSynList);
        }
        #endregion

    }
}
