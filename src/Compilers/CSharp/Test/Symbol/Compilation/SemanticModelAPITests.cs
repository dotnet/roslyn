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
        public void RefReassignSymbolInfo()
        {
            CSharpCompilation comp = CreateCompilation(@"
class C
{
    int f = 0; 
    void M(ref int rx)
    {
        (rx = ref f) = 0;
    }
}");
            comp.VerifyDiagnostics();
            SyntaxTree tree = comp.SyntaxTrees.Single();
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            ParenthesizedExpressionSyntax assignment = root.DescendantNodes().OfType<ParenthesizedExpressionSyntax>().Single();
            SemanticModel model = comp.GetSemanticModel(tree);
            Assert.Null(model.GetDeclaredSymbol(assignment));
            SymbolInfo assignmentInfo = model.GetSymbolInfo(assignment);
            Assert.Null(assignmentInfo.Symbol);
        }

        [Fact]
        public void RefForSymbolInfo()
        {
            CSharpCompilation comp = CreateCompilation(@"
class C
{
    void M(int x)
    {
        for (ref readonly int rx = ref x;;)
        {
            if (rx == 0)
            {
                return;
            }
        }
    }
}");
            comp.VerifyDiagnostics();
            SyntaxTree tree = comp.SyntaxTrees.Single();
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            IdentifierNameSyntax rx = root.DescendantNodes().OfType<IdentifierNameSyntax>().Last();
            SemanticModel model = comp.GetSemanticModel(tree);
            SymbolInfo rxInfo = model.GetSymbolInfo(rx);
            Assert.NotNull(rxInfo.Symbol);
            ILocalSymbol rxSymbol = Assert.IsAssignableFrom<ILocalSymbol>(rxInfo.Symbol);
            Assert.True(rxSymbol.IsRef);
            Assert.Equal(RefKind.RefReadOnly, rxSymbol.RefKind);
            VariableDeclarationSyntax rxDecl = root.DescendantNodes().OfType<ForStatementSyntax>().Single().Declaration;
            Assert.Same(model.GetDeclaredSymbol(rxDecl.Variables.Single()), rxSymbol);
        }

        [Fact]
        public void RefForEachSymbolInfo()
        {
            CSharpCompilation comp = CreateCompilationWithMscorlibAndSpan(@"
using System;
class C
{
    void M(Span<int> span)
    {
        foreach (ref readonly int rx in span)
        {
            if (rx == 0)
            {
                return;
            }
        }
    }
}");
            comp.VerifyDiagnostics();
            SyntaxTree tree = comp.SyntaxTrees.Single();
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            IdentifierNameSyntax rx = root.DescendantNodes().OfType<IdentifierNameSyntax>().Last();
            SemanticModel model = comp.GetSemanticModel(tree);
            SymbolInfo rxInfo = model.GetSymbolInfo(rx);
            Assert.NotNull(rxInfo.Symbol);
            ILocalSymbol rxSymbol = Assert.IsAssignableFrom<ILocalSymbol>(rxInfo.Symbol);
            Assert.True(rxSymbol.IsRef);
            Assert.Equal(RefKind.RefReadOnly, rxSymbol.RefKind);
            ForEachStatementSyntax rxDecl = root.DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            Assert.Same(model.GetDeclaredSymbol(rxDecl), rxSymbol);
        }

        [Fact]
        public void LocalSymbolsAreEquivalentAcrossSemanticModelsFromTheSameCompilation()
        {
            var text = @"public class C { public void M() { int x = 10; } }";
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);

            SemanticModel model1 = comp.GetSemanticModel(tree);
            SemanticModel model2 = comp.GetSemanticModel(tree);
            Assert.NotEqual(model1, model2);

            VariableDeclaratorSyntax vardecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            ISymbol symbol1 = model1.GetDeclaredSymbol(vardecl);
            ISymbol symbol2 = model2.GetDeclaredSymbol(vardecl);

            Assert.NotSame(symbol1, symbol2);
            Assert.Equal(symbol1, symbol2);
        }

        [Fact]
        public void LocalSymbolsAreDifferentArossSemanticModelsFromDifferentCompilations()
        {
            var text = @"public class C { public void M() { int x = 10; } }";
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp1 = CreateCompilation(tree);
            CSharpCompilation comp2 = CreateCompilation(tree);

            SemanticModel model1 = comp1.GetSemanticModel(tree);
            SemanticModel model2 = comp2.GetSemanticModel(tree);
            Assert.NotEqual(model1, model2);

            VariableDeclaratorSyntax vardecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
            ISymbol symbol1 = model1.GetDeclaredSymbol(vardecl);
            ISymbol symbol2 = model2.GetDeclaredSymbol(vardecl);

            Assert.NotSame(symbol1, symbol2);
            Assert.NotEqual(symbol1, symbol2);
        }

        [Fact]
        public void RangeVariableSymbolsAreEquivalentAcrossSemanticModelsFromTheSameCompilation()
        {
            var text = @"using System.Linq; public class C { public void M() { var q = from c in string.Empty select c; } }";
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { tree });

            SemanticModel model1 = comp.GetSemanticModel(tree);
            SemanticModel model2 = comp.GetSemanticModel(tree);
            Assert.NotEqual(model1, model2);

            QueryClauseSyntax vardecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<QueryClauseSyntax>().First();
            IRangeVariableSymbol symbol1 = model1.GetDeclaredSymbol(vardecl);
            IRangeVariableSymbol symbol2 = model2.GetDeclaredSymbol(vardecl);

            Assert.NotSame(symbol1, symbol2);
            Assert.Equal(symbol1, symbol2);
        }

        [Fact]
        public void RangeVariableSymbolsAreDifferentAcrossSemanticModelsFromDifferentCompilations()
        {
            var text = @"using System.Linq; public class C { public void M() { var q = from c in string.Empty select c; } }";
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp1 = CreateCompilationWithMscorlib40AndSystemCore(new[] { tree });
            CSharpCompilation comp2 = CreateCompilationWithMscorlib40AndSystemCore(new[] { tree });

            SemanticModel model1 = comp1.GetSemanticModel(tree);
            SemanticModel model2 = comp2.GetSemanticModel(tree);
            Assert.NotEqual(model1, model2);

            QueryClauseSyntax vardecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<QueryClauseSyntax>().First();
            IRangeVariableSymbol symbol1 = model1.GetDeclaredSymbol(vardecl);
            IRangeVariableSymbol symbol2 = model2.GetDeclaredSymbol(vardecl);

            Assert.NotSame(symbol1, symbol2);
            Assert.NotEqual(symbol1, symbol2);
        }

        [Fact]
        public void LabelSymbolsAreEquivalentAcrossSemanticModelsFromSameCompilation()
        {
            var text = @"public class C { public void M() { label: goto label; } }";
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);

            SemanticModel model1 = comp.GetSemanticModel(tree);
            SemanticModel model2 = comp.GetSemanticModel(tree);
            Assert.NotEqual(model1, model2);

            GotoStatementSyntax statement = tree.GetCompilationUnitRoot().DescendantNodes().OfType<GotoStatementSyntax>().First();
            ISymbol symbol1 = model1.GetSymbolInfo(statement.Expression).Symbol;
            ISymbol symbol2 = model2.GetSymbolInfo(statement.Expression).Symbol;

            Assert.Equal(false, ReferenceEquals(symbol1, symbol2));
            Assert.Equal(symbol1, symbol2);
        }

        [Fact]
        public void LambdaParameterSymbolsAreEquivalentAcrossSemanticModelsFromSameCompilation()
        {
            var text = @"using System; public class C { public void M() { Func<int,int> f = (p) => p; } }";
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);

            SemanticModel model1 = comp.GetSemanticModel(tree);
            SemanticModel model2 = comp.GetSemanticModel(tree);
            Assert.NotEqual(model1, model2);

            ParameterSyntax paramdecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().First();
            IParameterSymbol symbol1 = model1.GetDeclaredSymbol(paramdecl);
            IParameterSymbol symbol2 = model2.GetDeclaredSymbol(paramdecl);

            Assert.NotSame(symbol1, symbol2);
            Assert.Equal(symbol1.ContainingSymbol, symbol2.ContainingSymbol);
            Assert.Equal(symbol1, symbol2);
        }

        [Fact]
        public void LambdaParameterSymbolsAreDifferentAcrossSemanticModelsFromDifferentCompilations()
        {
            var text = @"using System; public class C { public void M() { Func<int,int> f = (p) => p; } }";
            SyntaxTree tree1 = Parse(text);
            SyntaxTree tree2 = Parse(text);
            CSharpCompilation comp1 = CreateCompilation(tree1);
            CSharpCompilation comp2 = CreateCompilation(tree2);

            SemanticModel model1 = comp1.GetSemanticModel(tree1);
            SemanticModel model2 = comp2.GetSemanticModel(tree2);
            Assert.NotEqual(model1, model2);

            ParameterSyntax paramdecl1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().First();
            IParameterSymbol symbol1 = model1.GetDeclaredSymbol(paramdecl1);
            ParameterSyntax paramdecl2 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().First();
            IParameterSymbol symbol2 = model2.GetDeclaredSymbol(paramdecl2);

            Assert.NotSame(symbol1, symbol2);
            Assert.NotEqual(symbol1.ContainingSymbol, symbol2.ContainingSymbol);
            Assert.NotEqual(symbol1, symbol2);
        }

        [WorkItem(539740, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539740")]
        [Fact]
        public void NamespaceWithoutName()
        {
            var text = "namespace";
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);
            SemanticModel model = comp.GetSemanticModel(tree);
            Diagnostic[] errors = comp.GetDiagnostics().ToArray();
            Assert.Equal(3, errors.Length);

            SyntaxNode[] nsArray = tree.GetCompilationUnitRoot().DescendantNodes().Where(node => node.IsKind(SyntaxKind.NamespaceDeclaration)).ToArray();
            Assert.Equal(1, nsArray.Length);

            var nsSyntax = nsArray[0] as NamespaceDeclarationSyntax;
            INamespaceSymbol symbol = model.GetDeclaredSymbol(nsSyntax);
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
            CSharpCompilation comp = CreateCompilation(text);
            NamespaceSymbol global = comp.GlobalNamespace;
            NamedTypeSymbol a = global.GetTypeMembers("A", 0).Single();
            NamedTypeSymbol abase = a.BaseType();
            Assert.Equal("B.R", abase.ToTestDisplayString());

            NamedTypeSymbol b = global.GetTypeMembers("B", 0).Single();
            NamedTypeSymbol r = b.GetTypeMembers("R", 0).Single();
            NamedTypeSymbol q = r.GetTypeMembers("Q", 0).Single();
            var v = a.GetMembers("v").Single() as FieldSymbol;
            TypeSymbol s = v.Type;
            Assert.Equal("B.R.Q.S", s.ToTestDisplayString());
            NamedTypeSymbol sbase = s.BaseType();
            Assert.Equal("B.R.Q", sbase.ToTestDisplayString());
        }

        [Fact]
        public void Diagnostics1()
        {
            var text =
@"
class A : A {}
";
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);

            ImmutableArray<Diagnostic> errs = comp.GetSemanticModel(tree).GetDeclarationDiagnostics();
            Assert.Equal(1, errs.Count());
        }

        [Fact]
        public void DiagnosticsInOneTree()
        {
            var partial1 =
@"
partial class A 
{ 
    void goo() { int x = y; }
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

            SyntaxTree partial1Tree = Parse(partial1);
            SyntaxTree partial2Tree = Parse(partial2);
            CSharpCompilation comp = CreateCompilation(new SyntaxTree[] { partial1Tree, partial2Tree });

            ImmutableArray<Diagnostic> errs = comp.GetSemanticModel(partial1Tree).GetDiagnostics();
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
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);

            var bdecl = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var bbase = bdecl.BaseList.Types[0].Type as TypeSyntax;
            SemanticModel model = comp.GetSemanticModel(tree);

            SymbolInfo info = model.GetSymbolInfo(bbase);
            Assert.NotNull(info.Symbol);
            NamedTypeSymbol a = comp.GlobalNamespace.GetTypeMembers("A", 0).Single();
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
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);

            var cdecl = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var cbase = cdecl.BaseList.Types[0].Type as TypeSyntax;
            SemanticModel model = comp.GetSemanticModel(tree);

            SymbolInfo info = model.GetSymbolInfo(cbase);
            Assert.NotNull(info.Symbol);
            NamedTypeSymbol b = comp.GlobalNamespace.GetTypeMembers("B", 0).Single();
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
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);

            var cdecl = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var cbase = cdecl.BaseList.Types[0].Type as TypeSyntax;
            SemanticModel model = comp.GetSemanticModel(tree);

            SymbolInfo info = model.GetSymbolInfo(cbase);
            Assert.NotNull(info.Symbol);
            var cbasetype = info.Symbol as NamedTypeSymbol;

            NamedTypeSymbol c = comp.GlobalNamespace.GetTypeMembers("C", 1).Single();
            Assert.Equal(c.BaseType(), cbasetype);
        }

        [Fact]
        public void Bindings2()
        {
            var text =
@"
class B<T> : A<T> {}
class A<T> {}
";
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);

            var bdecl = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var bbase = bdecl.BaseList.Types[0].Type as TypeSyntax; // A<T>
            SemanticModel model = comp.GetSemanticModel(tree);

            SymbolInfo info = model.GetSymbolInfo(bbase);
            Assert.NotNull(info.Symbol);
            var at2 = info.Symbol as NamedTypeSymbol;
            Assert.Equal(at2, model.GetTypeInfo(bbase).Type);

            NamedTypeSymbol a = comp.GlobalNamespace.GetTypeMembers("A", 1).Single();
            TypeParameterSymbol at = a.TypeParameters.First();
            NamedTypeSymbol b = comp.GlobalNamespace.GetTypeMembers("B", 1).Single();
            TypeParameterSymbol bt = b.TypeParameters.First();

            Assert.Equal(a.OriginalDefinition, at2.OriginalDefinition);
            Assert.Equal(b.TypeParameters.First(), at2.TypeArguments().First());
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
            SyntaxTree tree1 = Parse(text);
            CSharpCompilation compilation = CreateCompilation(tree1);

            SyntaxTree tree2 = Parse(text);
            var classProgram = tree2.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var staticProgramField = classProgram.Members[0] as FieldDeclarationSyntax;
            TypeSyntax program = staticProgramField.Declaration.Type;
            SemanticModel model = compilation.GetSemanticModel(tree1);

            Assert.Throws<ArgumentException>(() =>
            {
                // tree2 not in the compilation
                SymbolInfo lookup = model.GetSymbolInfo(program);
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

            SyntaxTree tree1 = Parse(text);
            CSharpCompilation compilation = CreateCompilation(tree1);

            var decl = tree1.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var field = decl.Members[0] as FieldDeclarationSyntax;
            TypeSyntax type = field.Declaration.Type;
            SemanticModel model = compilation.GetSemanticModel(tree1);

            SymbolInfo info = model.GetSymbolInfo(type);
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

            SyntaxTree tree1 = Parse(text);
            SyntaxTree tree2 = Parse(text);
            CSharpCompilation compilation = CreateCompilation(new SyntaxTree[] { tree1, tree2 });

            var decl = tree1.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var method = decl.Members[0] as MethodDeclarationSyntax;
            TypeSyntax type = method.ParameterList.Parameters[0].Type;

            SemanticModel model = compilation.GetSemanticModel(tree1);

            SymbolInfo info = model.GetSymbolInfo(type);
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
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);

            var bdecl = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var xdecl = bdecl.Members[0] as FieldDeclarationSyntax;

            SemanticModel model = comp.GetSemanticModel(tree);

            TypeSyntax speculate = SyntaxFactory.IdentifierName(SyntaxFactory.Identifier("A"));
            SymbolInfo symbolInfo = model.GetSpeculativeSymbolInfo(xdecl.SpanStart, speculate, SpeculativeBindingOption.BindAsTypeOrNamespace);
            var lookup = symbolInfo.Symbol as TypeSymbol;


            Assert.NotNull(lookup);
            NamedTypeSymbol a = comp.GlobalNamespace.GetTypeMembers("A", 0).Single();
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
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);

            var adecl = tree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var bdecl = adecl.Members[0] as TypeDeclarationSyntax;

            SemanticModel model = comp.GetSemanticModel(tree);
            INamedTypeSymbol a1 = model.GetDeclaredSymbol(adecl);
            INamedTypeSymbol b1 = model.GetDeclaredSymbol(bdecl);

            NamespaceSymbol global = comp.GlobalNamespace;
            NamedTypeSymbol a2 = global.GetTypeMembers("A", 0).Single();
            NamedTypeSymbol b2 = a2.GetTypeMembers("B", 0).Single();

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
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var mainDecl = root.Members[0] as TypeDeclarationSyntax;
            INamedTypeSymbol mainType = model.GetDeclaredSymbol(mainDecl);

            var aDecl = root.Members[1] as TypeDeclarationSyntax;
            INamedTypeSymbol aType = model.GetDeclaredSymbol(aDecl);

            var abDecl = aDecl.Members[0] as TypeDeclarationSyntax;
            INamedTypeSymbol abType = model.GetDeclaredSymbol(abDecl);

            var bDecl = root.Members[2] as TypeDeclarationSyntax;
            INamedTypeSymbol bType = model.GetDeclaredSymbol(bDecl);

            var xDecl = mainDecl.Members[0] as FieldDeclarationSyntax;
            var xSym = mainType.GetMembers("x").Single() as FieldSymbol;
            Assert.Equal<ISymbol>(abType, xSym.Type);
            SymbolInfo info = model.GetSymbolInfo((xDecl.Declaration.Type as QualifiedNameSyntax).Right);
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
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);
            SemanticModel model = comp.GetSemanticModel(tree);

            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var nDecl = root.Members[0] as NamespaceDeclarationSyntax;
            var n2Decl = root.Members[1] as NamespaceDeclarationSyntax;
            var cDecl = n2Decl.Members[0] as TypeDeclarationSyntax;
            SimpleNameSyntax cBase = (cDecl.BaseList.Types[0].Type as AliasQualifiedNameSyntax).Name;

            ISymbol cBaseType = model.GetSymbolInfo(cBase).Symbol;
            NamedTypeSymbol bOuter = comp.GlobalNamespace.GetTypeMembers("B", 0).Single();
            NamedTypeSymbol bInner = (comp.GlobalNamespace.GetMembers("N").Single() as NamespaceSymbol).GetTypeMembers("B", 0).Single();
            Assert.Equal(bOuter, cBaseType);
        }

        [Fact, WorkItem(528655, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528655")]
        public void ErrorSymbolForInvalidCode()
        {
            var text = @"
public class A 
{
	int goo	{	void goo() {}	} // Error
	static int Main() {	return 1;    }
}
";
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);
            ImmutableArray<Symbol> mems = comp.SourceModule.GlobalNamespace.GetMembers();

            IEnumerable<Symbol> typeA = mems.Where(s => s.Name == "A").Select(s => s);
            Assert.Equal(1, typeA.Count());
            IEnumerable<Symbol> invalid = mems.Where(s => s.Name == "<invalid-global-code>").Select(s => s);
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
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);
            NamedTypeSymbol pTypeSym = comp.SourceModule.GlobalNamespace.GetTypeMembers("PC").Single();
            Symbol pMethodSym = pTypeSym.GetMembers("PM").Single();

            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            ClassDeclarationSyntax pType01 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
            ClassDeclarationSyntax pType02 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Last();
            Assert.NotEqual(pType01, pType02);
            INamedTypeSymbol ptSym01 = model.GetDeclaredSymbol(pType01);
            INamedTypeSymbol ptSym02 = model.GetDeclaredSymbol(pType02);
            // same partial type symbol
            Assert.Same(ptSym01, ptSym02);
            Assert.Equal(2, ptSym01.Locations.Length);

            MethodDeclarationSyntax pMethod01 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            MethodDeclarationSyntax pMethod02 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Last();
            Assert.NotEqual(pMethod01, pMethod02);

            IMethodSymbol pmSym01 = model.GetDeclaredSymbol(pMethod01);
            IMethodSymbol pmSym02 = model.GetDeclaredSymbol(pMethod02);
            // different partial method symbols:(
            Assert.NotSame(pmSym01, pmSym02);
            // the declaration one is what one can get from GetMembers()
            Assert.Same(pMethodSym, pmSym01);

            // with decl|impl point to each other
            Assert.Null(pmSym01.PartialDefinitionPart);
            Assert.Same(pmSym02, pmSym01.PartialImplementationPart);

            Assert.Same(pmSym01, pmSym02.PartialDefinitionPart);
            Assert.Null(pmSym02.PartialImplementationPart);

            ParameterSyntax pParam01 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().First();
            ParameterSyntax pParam02 = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().Last();
            Assert.NotEqual(pParam01, pParam02);

            IParameterSymbol ppSym01 = model.GetDeclaredSymbol(pParam01);
            IParameterSymbol ppSym02 = model.GetDeclaredSymbol(pParam02);
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
    public delegate void DGoo(byte i = 1);
    protected internal void MGoo(sbyte j = 2) { }
}
";
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);
            SemanticModel model = comp.GetSemanticModel(tree);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            LiteralExpressionSyntax[] exprs = root.DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(2, exprs.Length);

            TypeInfo type1 = model.GetTypeInfo(exprs[0]);
            TypeInfo type2 = model.GetTypeInfo(exprs[1]);

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
    static partial void Goo(ulong x);
}
";

            var text2 = @"
using System;

partial class Partial001
{
    static partial void Goo(ulong x)  {    }
    static int Main()  {    return 1;    }
}
";
            SyntaxTree tree1 = Parse(text1);
            SyntaxTree tree2 = Parse(text2);
            CSharpCompilation comp = CreateCompilation(new[] { tree1, tree2 });

            SemanticModel model1 = comp.GetSemanticModel(tree1);
            SemanticModel model2 = comp.GetSemanticModel(tree2);
            CompilationUnitSyntax root1 = tree1.GetCompilationUnitRoot();
            CompilationUnitSyntax root2 = tree1.GetCompilationUnitRoot();
            ParameterSyntax para1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().First();
            ParameterSyntax para2 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().First();
            IParameterSymbol sym1 = model1.GetDeclaredSymbol(para1);
            IParameterSymbol sym2 = model2.GetDeclaredSymbol(para2);

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
    static partial void Goo<T>(T x);
}
";

            var text2 = @"
using System;

partial class Partial001
{
    static partial void Goo<T>(T x)  {    }
    static int Main()  {    return 1;    }
}
";
            SyntaxTree tree1 = Parse(text1);
            SyntaxTree tree2 = Parse(text2);
            CSharpCompilation comp = CreateCompilation(new List<SyntaxTree> { tree1, tree2 });

            SemanticModel model1 = comp.GetSemanticModel(tree1);
            SemanticModel model2 = comp.GetSemanticModel(tree2);
            CompilationUnitSyntax root1 = tree1.GetCompilationUnitRoot();
            CompilationUnitSyntax root2 = tree1.GetCompilationUnitRoot();
            TypeParameterSyntax para1 = tree1.GetCompilationUnitRoot().DescendantNodes().OfType<TypeParameterSyntax>().First();
            TypeParameterSyntax para2 = tree2.GetCompilationUnitRoot().DescendantNodes().OfType<TypeParameterSyntax>().First();
            ITypeParameterSymbol sym1 = model1.GetDeclaredSymbol(para1);
            ITypeParameterSymbol sym2 = model2.GetDeclaredSymbol(para2);

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
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);
            SemanticModel model = comp.GetSemanticModel(tree);
            IEnumerable<AnonymousObjectMemberDeclaratorSyntax> anonProps = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AnonymousObjectMemberDeclaratorSyntax>();
            Assert.Equal(4, anonProps.Count());
            IEnumerable<string> symList = from ap in anonProps
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
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);
            SemanticModel model = comp.GetSemanticModel(tree);
            IEnumerable<AnonymousObjectMemberDeclaratorSyntax> anonProps = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AnonymousObjectMemberDeclaratorSyntax>();
            Assert.Equal(5, anonProps.Count());
            IEnumerable<string> symList = from ap in anonProps
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
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);
            SemanticModel model = comp.GetSemanticModel(tree);
            IEnumerable<AnonymousObjectMemberDeclaratorSyntax> anonProps = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AnonymousObjectMemberDeclaratorSyntax>();
            Assert.Equal(4, anonProps.Count());
            IEnumerable<string> symList = from ap in anonProps
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
    public interface IGoo {  }
    public IGoo GetGoo { get; set; }
    public IGoo GetGoo2() { return null; }
}

class AnonTypeTest
{
    event Action<ushort> Eve
    {
        add
        {
            var anonType = new { a1 = new { S.sField, igoo = new { new S().GetGoo } } };
        }
        remove 
        {
            var anonType = new { a1 = new { a2 = new { a2 = S.sField, a3 = new { a3 = new S().GetGoo2() } } } };
        }
    }
}
";
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);
            SemanticModel model = comp.GetSemanticModel(tree);
            IEnumerable<AnonymousObjectMemberDeclaratorSyntax> anonProps = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AnonymousObjectMemberDeclaratorSyntax>();
            Assert.Equal(9, anonProps.Count());
            IEnumerable<string> symList = from ap in anonProps
                          let apsym = model.GetDeclaredSymbol(ap)
                          orderby apsym.Name
                          select apsym.Name;

            var results = string.Join(", ", symList);
            Assert.Equal("a1, a1, a2, a2, a3, a3, GetGoo, igoo, sField", results);
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
            SyntaxTree tree = Parse(text);
            CSharpCompilation comp = CreateCompilation(tree);
            SemanticModel model = comp.GetSemanticModel(tree);
            IEnumerable<SyntaxNode> descendants = tree.GetCompilationUnitRoot().DescendantNodes();

            IEnumerable<ParameterSyntax> paras = descendants.OfType<ParameterSyntax>();
            Assert.Equal(1, paras.Count());
            IParameterSymbol parasym = model.GetDeclaredSymbol(paras.First());
            Location ploc = parasym.Locations[0];

            IEnumerable<ArgumentSyntax> args = descendants.OfType<ArgumentSyntax>().Where(s => s.ToString() == "index").Select(s => s);
            Assert.Equal(2, args.Count());
            ISymbol argsym1 = model.GetSymbolInfo(args.First().Expression).Symbol;
            ISymbol argsym2 = model.GetSymbolInfo(args.Last().Expression).Symbol;
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

            CSharpCompilation comp = CreateCompilation(source);
            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            UsingDirectiveSyntax aliasSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single();

            IAliasSymbol symbol = model.GetDeclaredSymbol(aliasSyntax);
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
            CSharpCompilation comp1 = CreateCompilation("public class C { }");
            MetadataReference ref1 = comp1.EmitToImageReference(aliases: ImmutableArray.Create("X"));

            CSharpCompilation comp2 = CreateCompilation(source, new[] { ref1 });
            SyntaxTree tree = comp2.SyntaxTrees.Single();
            SemanticModel model = comp2.GetSemanticModel(tree);

            ExternAliasDirectiveSyntax aliasSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().Single();

            // Compilation.GetExternAliasTarget defines this behavior: the target is a merged namespace
            // with the same name as the alias, contained in the global namespace of the compilation.
            IAliasSymbol aliasSymbol = model.GetDeclaredSymbol(aliasSyntax);
            var aliasTarget = (NamespaceSymbol)aliasSymbol.Target;
            Assert.Equal(NamespaceKind.Module, aliasTarget.Extent.Kind);
            Assert.Equal("", aliasTarget.Name);
            Assert.True(aliasTarget.IsGlobalNamespace);
            Assert.Null(aliasTarget.ContainingNamespace);

            Assert.Equal(0, comp2.GlobalNamespace.GetMembers("X").Length); //Doesn't contain the alias target namespace as a child.

            AliasQualifiedNameSyntax aliasQualifiedSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AliasQualifiedNameSyntax>().Single();
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

            CSharpCompilation comp = CreateCompilation(source);
            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            ExternAliasDirectiveSyntax aliasSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ExternAliasDirectiveSyntax>().Single();

            IAliasSymbol aliasSymbol = model.GetDeclaredSymbol(aliasSyntax);
            Assert.IsType<MissingNamespaceSymbol>(aliasSymbol.Target);

            AliasQualifiedNameSyntax aliasQualifiedSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AliasQualifiedNameSyntax>().Single();
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

            CSharpCompilation comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (1,1): info CS8019: Unnecessary using directive.
                // using X = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = System;"));

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            IdentifierNameSyntax aliasSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<NameEqualsSyntax>().Single().Name;
            Assert.Equal(SymbolInfo.None, model.GetSymbolInfo(aliasSyntax));

            UsingDirectiveSyntax usingSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single();
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

            CSharpCompilation comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            ConstructorInitializerSyntax oldSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().Single();

            ConstructorInitializerSyntax newSyntax = SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer);

            SymbolInfo info = model.GetSpeculativeSymbolInfo(oldSyntax.SpanStart, newSyntax);
            ISymbol symbol = info.Symbol;
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

            CSharpCompilation comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            ConstructorDeclarationSyntax oldSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            ConstructorInitializerSyntax newSyntax = SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer);

            SymbolInfo info = model.GetSpeculativeSymbolInfo(oldSyntax.ParameterList.Span.End, newSyntax);
            Assert.Equal(SymbolInfo.None, info);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelInFieldInitializer()
        {
            CSharpCompilation compilation = CreateCompilation(@"
class C 
{
  object y = 1;
}
");
            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var fieldDecl = (FieldDeclarationSyntax)typeDecl.Members[0];
            VariableDeclaratorSyntax varDecl = fieldDecl.Declaration.Variables.First();

            SemanticModel model = compilation.GetSemanticModel(tree);
            Assert.False(model.IsSpeculativeSemanticModel);
            Assert.Null(model.ParentModel);
            Assert.Equal(0, model.OriginalPositionForSpeculation);

            // Speculate on the equals value syntax (initializer)
            // Conversion info available, ConvertedType: Object.
            EqualsValueClauseSyntax equalsValue = SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(@"(string)""Hello"""));
            ExpressionSyntax expr = equalsValue.Value;
            int position = varDecl.Initializer.SpanStart;

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(position, equalsValue, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);
            Assert.True(speculativeModel.IsSpeculativeSemanticModel);
            Assert.Equal(model, speculativeModel.ParentModel);
            Assert.Equal(position, speculativeModel.OriginalPositionForSpeculation);

            TypeInfo typeInfo = speculativeModel.GetTypeInfo(expr);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("String", typeInfo.Type.Name);
            Assert.Equal("Object", typeInfo.ConvertedType.Name);

            Optional<object> constantInfo = speculativeModel.GetConstantValue(expr);
            Assert.True(constantInfo.HasValue, "must be a constant");
            Assert.Equal("Hello", constantInfo.Value);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelInEnumMember()
        {
            CSharpCompilation compilation = CreateCompilation(@"
enum C 
{
  y = 1
}
");
            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (EnumDeclarationSyntax)root.Members[0];
            var enumMemberDecl = (EnumMemberDeclarationSyntax)typeDecl.Members[0];
            EqualsValueClauseSyntax equalsValue = enumMemberDecl.EqualsValue;
            ExpressionSyntax initializer = equalsValue.Value;

            SemanticModel model = compilation.GetSemanticModel(tree);

            // Speculate on the equals value syntax (initializer)
            // Conversion info available, ConvertedType: Int32.
            EqualsValueClauseSyntax newEqualsValue = SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("(short)0"));
            ExpressionSyntax expr = newEqualsValue.Value;
            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(equalsValue.SpanStart, newEqualsValue, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            TypeInfo typeInfo = speculativeModel.GetTypeInfo(expr);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("Int16", typeInfo.Type.Name);
            Assert.Equal("Int32", typeInfo.ConvertedType.Name);

            Optional<object> constantInfo = speculativeModel.GetConstantValue(expr);
            Assert.True(constantInfo.HasValue, "must be a constant");
            Assert.Equal((short)0, constantInfo.Value);
        }

        [Fact]
        [WorkItem(648305, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/648305")]
        public void TestGetSpeculativeSemanticModelInDefaultValueArgument()
        {
            CSharpCompilation compilation = CreateCompilation(@"
class C 
{
  void M(int x = 1)
  {
    string y = ""Hello"";     
  }
}
");
            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            EqualsValueClauseSyntax equalsValue = methodDecl.ParameterList.Parameters[0].Default;
            ExpressionSyntax paramDefaultArg = equalsValue.Value;

            SemanticModel model = compilation.GetSemanticModel(tree);

            TypeInfo ti = model.GetTypeInfo(paramDefaultArg);

            // Speculate on the equals value syntax (initializer)
            // Conversion info available, ConvertedType: Int32.
            EqualsValueClauseSyntax newEqualsValue = SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("(short)0"));
            ExpressionSyntax expr = newEqualsValue.Value;

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(equalsValue.SpanStart, newEqualsValue, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            TypeInfo typeInfo = speculativeModel.GetTypeInfo(expr);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("Int16", typeInfo.Type.Name);
            Assert.Equal("Int32", typeInfo.ConvertedType.Name);

            Optional<object> constantInfo = speculativeModel.GetConstantValue(expr);
            Assert.True(constantInfo.HasValue, "must be a constant");
            Assert.Equal((short)0, constantInfo.Value);
        }

        [Fact]
        [WorkItem(746002, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/746002")]
        public void TestGetSpeculativeSemanticModelInDefaultValueArgument2()
        {
            CSharpCompilation compilation = CreateCompilation(@"
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
            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var interfaceDecl = (TypeDeclarationSyntax)root.Members[1];
            var methodDecl = (MethodDeclarationSyntax)interfaceDecl.Members[0];
            ParameterSyntax param = methodDecl.ParameterList.Parameters[0];
            EqualsValueClauseSyntax equalsValue = param.Default;
            ExpressionSyntax paramDefaultArg = equalsValue.Value;

            SemanticModel model = compilation.GetSemanticModel(tree);

            // Speculate on the equals value syntax (initializer) with a non-null parent
            EqualsValueClauseSyntax newEqualsValue = SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("E.B | E.A"));
            newEqualsValue = param.ReplaceNode(equalsValue, newEqualsValue).Default;
            ExpressionSyntax binaryExpr = newEqualsValue.Value;

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(equalsValue.SpanStart, newEqualsValue, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            TypeInfo typeInfo = speculativeModel.GetTypeInfo(binaryExpr);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("E", typeInfo.Type.Name);
            Assert.Equal("E", typeInfo.ConvertedType.Name);
        }

        [Fact]
        [WorkItem(657701, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/657701")]
        public void TestGetSpeculativeSemanticModelInConstructorDefaultValueArgument()
        {
            CSharpCompilation compilation = CreateCompilation(@"
class C 
{
  C(int x = 1)
  {
    string y = ""Hello"";     
  }
}
");
            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var constructorDecl = (ConstructorDeclarationSyntax)typeDecl.Members[0];
            EqualsValueClauseSyntax equalsValue = constructorDecl.ParameterList.Parameters[0].Default;
            ExpressionSyntax paramDefaultArg = equalsValue.Value;

            SemanticModel model = compilation.GetSemanticModel(tree);

            TypeInfo ti = model.GetTypeInfo(paramDefaultArg);

            // Speculate on the equals value syntax (initializer)
            // Conversion info available, ConvertedType: Int32.
            EqualsValueClauseSyntax newEqualsValue = SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression("(short)0"));
            ExpressionSyntax expr = newEqualsValue.Value;

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(equalsValue.SpanStart, newEqualsValue, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            TypeInfo typeInfo = speculativeModel.GetTypeInfo(expr);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("Int16", typeInfo.Type.Name);
            Assert.Equal("Int32", typeInfo.ConvertedType.Name);

            Optional<object> constantInfo = speculativeModel.GetConstantValue(expr);
            Assert.True(constantInfo.HasValue, "must be a constant");
            Assert.Equal((short)0, constantInfo.Value);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelInExpressionBody_Property()
        {
            CSharpCompilation compilation = CreateCompilation(@"
class C 
{
  public object X => 0;
}
");
            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var propertyDecl = (PropertyDeclarationSyntax)typeDecl.Members[0];
            ArrowExpressionClauseSyntax expressionBody = propertyDecl.ExpressionBody;

            TestExpressionBodySpeculation(compilation, tree, expressionBody);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelInExpressionBody_Method()
        {
            CSharpCompilation compilation = CreateCompilation(@"
class C 
{
  public object X() => 0;
}
");
            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            ArrowExpressionClauseSyntax expressionBody = methodDecl.ExpressionBody;

            TestExpressionBodySpeculation(compilation, tree, expressionBody);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelInExpressionBody_Indexer()
        {
            CSharpCompilation compilation = CreateCompilation(@"
class C 
{
  public object this[int x] => 0;
}
");
            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var indexerDecl = (IndexerDeclarationSyntax)typeDecl.Members[0];
            ArrowExpressionClauseSyntax expressionBody = indexerDecl.ExpressionBody;

            TestExpressionBodySpeculation(compilation, tree, expressionBody);
        }

        private static void TestExpressionBodySpeculation(Compilation compilation, SyntaxTree tree, ArrowExpressionClauseSyntax expressionBody)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            Assert.False(model.IsSpeculativeSemanticModel);
            Assert.Null(model.ParentModel);
            Assert.Equal(0, model.OriginalPositionForSpeculation);

            // Speculate on the expression body syntax.
            // Conversion info available, ConvertedType: Object.
            ArrowExpressionClauseSyntax newExpressionBody = SyntaxFactory.ArrowExpressionClause(SyntaxFactory.ParseExpression(@"(string)""Hello"""));
            ExpressionSyntax expr = newExpressionBody.Expression;
            int position = expressionBody.SpanStart;

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(position, newExpressionBody, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);
            Assert.True(speculativeModel.IsSpeculativeSemanticModel);
            Assert.Equal(model, speculativeModel.ParentModel);
            Assert.Equal(position, speculativeModel.OriginalPositionForSpeculation);

            TypeInfo typeInfo = speculativeModel.GetTypeInfo(expr);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("String", typeInfo.Type.Name);
            Assert.Equal("Object", typeInfo.ConvertedType.Name);

            Optional<object> constantInfo = speculativeModel.GetConstantValue(expr);
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

            CSharpCompilation comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,9): warning CS0169: The field 'Q.q' is never used
                //     var q;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "q").WithArguments("Q.q"));

            NamedTypeSymbol classQ = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Q");
            FieldSymbol fieldQ = classQ.GetMember<FieldSymbol>("q");

            Assert.Equal(classQ, fieldQ.Type);

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            UsingDirectiveSyntax aliasDecl = tree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single();

            IAliasSymbol aliasSymbol = model.GetDeclaredSymbol(aliasDecl);
            Assert.Equal(SymbolKind.Alias, aliasSymbol.Kind);
            Assert.Equal(classQ, ((AliasSymbol)aliasSymbol).Target);
            Assert.Equal("var", aliasSymbol.Name);

            SymbolInfo aliasDeclInfo = model.GetSymbolInfo(aliasDecl.Alias.Name);
            Assert.Null(aliasDeclInfo.Symbol);
            Assert.Equal(CandidateReason.None, aliasDeclInfo.CandidateReason);

            FieldDeclarationSyntax fieldDecl = tree.GetRoot().DescendantNodes().OfType<FieldDeclarationSyntax>().Single();

            ISymbol fieldSymbol = model.GetDeclaredSymbol(fieldDecl.Declaration.Variables.Single());
            Assert.Equal(fieldQ, fieldSymbol);

            var typeSyntax = (IdentifierNameSyntax)fieldDecl.Declaration.Type;

            SymbolInfo fieldTypeInfo = model.GetSymbolInfo(typeSyntax);
            Assert.Equal(classQ, fieldTypeInfo.Symbol);

            IAliasSymbol fieldTypeAliasInfo = model.GetAliasInfo(typeSyntax);
            Assert.Equal(aliasSymbol, fieldTypeAliasInfo);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForStatement()
        {
            CSharpCompilation compilation = CreateCompilation(@"
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

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].SpanStart, statement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var localDecl = (LocalDeclarationStatementSyntax)statement.Statements[0];
            VariableDeclaratorSyntax declarator = localDecl.Declaration.Variables.First();
            ISymbol local = speculativeModel.GetDeclaredSymbol(declarator);
            Assert.NotNull(local);
            Assert.Equal("z", local.Name);
            Assert.Equal(SymbolKind.Local, local.Kind);
            Assert.Equal("Int32", ((LocalSymbol)local).Type.Name);

            TypeInfo typeInfo = speculativeModel.GetTypeInfo(localDecl.Declaration.Type);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("Int32", typeInfo.Type.Name);

            var call = (InvocationExpressionSyntax)((ExpressionStatementSyntax)statement.Statements[1]).Expression;
            ExpressionSyntax arg = call.ArgumentList.Arguments[0].Expression;
            SymbolInfo info = speculativeModel.GetSymbolInfo(arg);
            Assert.NotNull(info.Symbol);
            Assert.Equal("z", info.Symbol.Name);
            Assert.Equal(SymbolKind.Local, info.Symbol.Kind);

            var call2 = (InvocationExpressionSyntax)((ExpressionStatementSyntax)((BlockSyntax)statement).Statements[2]).Expression;
            ExpressionSyntax arg2 = call2.ArgumentList.Arguments[0].Expression;
            SymbolInfo info2 = speculativeModel.GetSymbolInfo(arg2);
            Assert.NotNull(info2.Symbol);
            Assert.Equal("y", info2.Symbol.Name);
            Assert.Equal(SymbolKind.Local, info2.Symbol.Kind);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForStatement_DeclaredLocal()
        {
            CSharpCompilation compilation = CreateCompilation(@"
class C 
{
  void M(int x)
  {
    int y = 1000;
  }
}
");

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            // different name local
            StatementSyntax statement = SyntaxFactory.ParseStatement(@"int z = 0;");

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].SpanStart, statement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            VariableDeclaratorSyntax declarator = ((LocalDeclarationStatementSyntax)statement).Declaration.Variables.First();
            ISymbol local = speculativeModel.GetDeclaredSymbol(declarator);
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
            CSharpCompilation compilation = CreateCompilation(@"
class C 
{
  void M(int x)
  {
    int y = 1000;     
  }
}
");

            StatementSyntax labeledStatement = SyntaxFactory.ParseStatement(@"label: y++;");

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            SemanticModel statModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].SpanStart, labeledStatement, out statModel);
            Assert.True(success);
            Assert.NotNull(statModel);

            ISymbol label = statModel.GetDeclaredSymbol(labeledStatement);
            Assert.NotNull(label);
            Assert.Equal("label", label.Name);
            Assert.Equal(SymbolKind.Label, label.Kind);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForStatement_GetDeclaredSwitchLabelSymbol()
        {
            CSharpCompilation compilation = CreateCompilation(@"
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
            SwitchLabelSyntax switchLabel = switchStatement.Sections[0].Labels[0];

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            SemanticModel model = compilation.GetSemanticModel(tree);
            SemanticModel statModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].Span.End, switchStatement, out statModel);
            Assert.True(success);
            Assert.NotNull(statModel);

            ILabelSymbol symbol = statModel.GetDeclaredSymbol(switchLabel);
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
            CSharpCompilation compilation = CreateCompilationWithMscorlib40AndSystemCore(@"
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

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].Span.End, speculatedStatement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            ParenthesizedLambdaExpressionSyntax lambdaExpression = speculatedStatement.DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().FirstOrDefault();
            ParameterSyntax lambdaParam = lambdaExpression.ParameterList.Parameters[0];
            IParameterSymbol parameterSymbol = speculativeModel.GetDeclaredSymbol(lambdaParam);
            Assert.NotNull(parameterSymbol);
            Assert.Equal("z", parameterSymbol.Name);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForStatement_ForEach()
        {
            CSharpCompilation compilation = CreateCompilation(@"
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

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

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

            CSharpCompilation comp = CreateCompilation(source, parseOptions: TestOptions.Regular);
            SyntaxTree tree = comp.SyntaxTrees.Single();

            SemanticModel model = comp.GetSemanticModel(tree);
            Assert.False(model.IsSpeculativeSemanticModel);
            Assert.Null(model.ParentModel);
            Assert.Equal(0, model.OriginalPositionForSpeculation);

            // Speculate on the initializer
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            EqualsValueClauseSyntax oldSyntax = root.DescendantNodes()
                .OfType<EqualsValueClauseSyntax>().ElementAt(1);
            var position = oldSyntax.SpanStart;
            EqualsValueClauseSyntax newSyntax = SyntaxFactory.EqualsValueClause(
                SyntaxFactory.ParseExpression("this.y"));
            ExpressionSyntax expr = newSyntax.Value;

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(oldSyntax.SpanStart,
                newSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);
            Assert.True(speculativeModel.IsSpeculativeSemanticModel);
            Assert.Equal(model, speculativeModel.ParentModel);
            Assert.Equal(position, speculativeModel.OriginalPositionForSpeculation);

            TypeInfo typeInfo = speculativeModel.GetTypeInfo(expr);
            Assert.NotNull(typeInfo);
            Assert.Equal("Int32", typeInfo.Type.Name);

            ThisExpressionSyntax thisSyntax = expr.DescendantNodes().OfType<ThisExpressionSyntax>().Single();
            SymbolInfo symbolInfo = speculativeModel.GetSpeculativeSymbolInfo(
                thisSyntax.SpanStart,
                thisSyntax, SpeculativeBindingOption.BindAsExpression);
            Assert.NotNull(symbolInfo);
            ImmutableArray<ISymbol> candidates = symbolInfo.CandidateSymbols;
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

            CSharpCompilation comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel parentModel = comp.GetSemanticModel(tree);

            ConstructorInitializerSyntax oldSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().Single();

            ConstructorInitializerSyntax newSyntax = SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer);

            SemanticModel speculativeModel;
            bool success = parentModel.TryGetSpeculativeSemanticModel(oldSyntax.SpanStart, newSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            SymbolInfo info = speculativeModel.GetSymbolInfo(newSyntax);
            ISymbol symbol = info.Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C"), symbol.ContainingType);
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            var method = (MethodSymbol)symbol;
            Assert.Equal(MethodKind.Constructor, method.MethodKind);
            Assert.Equal(0, method.ParameterCount);

            // test unnecessary cast removal
            ExpressionSyntax newArgument = SyntaxFactory.ParseExpression("1");
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
            ISymbol[] sortedCandidates = info.CandidateSymbols.OrderBy(s => s.ToTestDisplayString()).ToArray();
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

            CSharpCompilation comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel parentModel = comp.GetSemanticModel(tree);

            ConstructorDeclarationSyntax oldSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            ConstructorInitializerSyntax newSyntax = SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer);

            SemanticModel speculativeModel;
            bool success = parentModel.TryGetSpeculativeSemanticModel(oldSyntax.SpanStart, newSyntax, out speculativeModel);
            Assert.False(success);
            Assert.Null(speculativeModel);
        }

        [Fact]
        public void TestArgumentsToGetSpeculativeSemanticModelAPI()
        {
            CSharpCompilation compilation = CreateCompilation(@"
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
            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var ctor1 = (ConstructorDeclarationSyntax)typeDecl.Members[0];
            var ctor2 = (ConstructorDeclarationSyntax)typeDecl.Members[1];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[2];

            SemanticModel model = compilation.GetSemanticModel(tree);
            var statement = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            EqualsValueClauseSyntax initializer = statement.Declaration.Variables[0].Initializer;
            ConstructorInitializerSyntax ctorInitializer = ctor1.Initializer;
            AttributeSyntax attribute = methodDecl.AttributeLists[0].Attributes[0];

            SemanticModel speculativeModel;
            Assert.Throws<ArgumentNullException>(() => model.TryGetSpeculativeSemanticModel(statement.SpanStart, statement: null, speculativeModel: out speculativeModel));
            Assert.Throws<ArgumentNullException>(() => model.TryGetSpeculativeSemanticModel(statement.SpanStart, constructorInitializer: null, speculativeModel: out speculativeModel));
            Assert.Throws<ArgumentNullException>(() => model.TryGetSpeculativeSemanticModel(statement.SpanStart, attribute: null, speculativeModel: out speculativeModel));

            // Speculate on a node from the same syntax tree.
            Assert.Throws<ArgumentException>(() => model.TryGetSpeculativeSemanticModel(statement.SpanStart, statement: statement, speculativeModel: out speculativeModel));
            Assert.Throws<ArgumentException>(() => model.TryGetSpeculativeSemanticModel(ctorInitializer.SpanStart, constructorInitializer: ctorInitializer, speculativeModel: out speculativeModel));
            Assert.Throws<ArgumentException>(() => model.TryGetSpeculativeSemanticModel(attribute.SpanStart, attribute: attribute, speculativeModel: out speculativeModel));

            // Chaining speculative semantic model is not supported.
            LocalDeclarationStatementSyntax speculatedStatement = statement.ReplaceNode(initializer.Value, SyntaxFactory.ParseExpression("0"));
            model.TryGetSpeculativeSemanticModel(statement.SpanStart, speculatedStatement, speculativeModel: out speculativeModel);
            Assert.Throws<InvalidOperationException>(() => speculativeModel.TryGetSpeculativeSemanticModel(speculatedStatement.SpanStart, speculatedStatement, speculativeModel: out speculativeModel));
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelOnSpeculativeSemanticModel()
        {
            CSharpCompilation compilation = CreateCompilation(@"
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
            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var ctor1 = (ConstructorDeclarationSyntax)typeDecl.Members[0];
            var ctor2 = (ConstructorDeclarationSyntax)typeDecl.Members[1];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[2];

            SemanticModel model = compilation.GetSemanticModel(tree);
            var statement = (LocalDeclarationStatementSyntax)methodDecl.Body.Statements[0];
            ExpressionSyntax expression = statement.Declaration.Variables[0].Initializer.Value;
            ConstructorInitializerSyntax ctorInitializer = ctor1.Initializer;
            AttributeSyntax attribute = methodDecl.AttributeLists[0].Attributes[0];

            LocalDeclarationStatementSyntax speculatedStatement = statement.ReplaceNode(expression, SyntaxFactory.ParseExpression("0"));
            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(statement.SpanStart, speculatedStatement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            // Chaining speculative semantic model is not supported.
            // (a) Expression
            LocalDeclarationStatementSyntax newSpeculatedStatement = statement.ReplaceNode(expression, SyntaxFactory.ParseExpression("1.1"));
            SemanticModel newModel;
            Assert.Throws<InvalidOperationException>(() => speculativeModel.TryGetSpeculativeSemanticModel(speculatedStatement.SpanStart, newSpeculatedStatement, out newModel));

            // (b) Statement
            newSpeculatedStatement = (LocalDeclarationStatementSyntax)SyntaxFactory.ParseStatement(@"int z = 0;");
            Assert.Throws<InvalidOperationException>(() => speculativeModel.TryGetSpeculativeSemanticModel(speculatedStatement.SpanStart, newSpeculatedStatement, out newModel));
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelInsideUnsafeCode()
        {
            CSharpCompilation compilation = CreateCompilation(@"
unsafe class C
{
    void M()
    {
        int x;
    }
}
");
            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];

            SemanticModel model = compilation.GetSemanticModel(tree);

            var unsafeStatement = (LocalDeclarationStatementSyntax)SyntaxFactory.ParseStatement("int *p = &x;");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(methodDecl.Body.Statements[0].Span.End, unsafeStatement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            VariableDeclaratorSyntax declarator = unsafeStatement.Declaration.Variables.First();
            ExpressionSyntax initializer = declarator.Initializer.Value;

            Binder binder = ((CSharpSemanticModel)speculativeModel).GetEnclosingBinder(initializer.SpanStart);
            Assert.True(binder.InUnsafeRegion, "must be in unsafe code");
            Assert.True(binder.IsSemanticModelBinder, "must be speculative");

            TypeInfo typeInfo = speculativeModel.GetTypeInfo(initializer);
            Assert.Equal("System.Int32*", typeInfo.Type.ToTestDisplayString());
            Assert.Equal(TypeKind.Pointer, typeInfo.Type.TypeKind);
            Assert.Equal("System.Int32*", typeInfo.ConvertedType.ToTestDisplayString());
            Assert.Equal(TypeKind.Pointer, typeInfo.ConvertedType.TypeKind);

            Conversion conv = speculativeModel.GetConversion(initializer);
            Assert.Equal(ConversionKind.Identity, conv.Kind);

            ISymbol symbol = speculativeModel.GetDeclaredSymbol(declarator);
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
        goo
        {
            return null;
        }
    }
}
";
            CSharpCompilation comp = CreateCompilation(source);
            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            AccessorDeclarationSyntax accessorSyntax = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();
            Assert.Equal(SyntaxKind.UnknownAccessorDeclaration, accessorSyntax.Kind());

            ReturnStatementSyntax statementSyntax = tree.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>().Single();
            MemberSemanticModel memberModel = ((CSharpSemanticModel)model).GetMemberModel(statementSyntax);
            Assert.Null(memberModel); // No member model since no symbol.

            StatementSyntax speculativeSyntax = SyntaxFactory.ParseStatement("return default(C);");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(statementSyntax.SpanStart, speculativeSyntax, out speculativeModel);
            Assert.False(success);
            Assert.Null(speculativeModel);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForMethodBody()
        {
            CSharpCompilation compilation = CreateCompilation(@"
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

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            MethodDeclarationSyntax speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].SpanStart, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            VerifySpeculativeSemanticModelForMethodBody(blockStatement, speculativeModel);
        }

        [Fact()]
        [WorkItem(10211, "https://github.com/dotnet/roslyn/issues/10211")]
        public void GetDependenceChainRegression_10211_working()
        {
            CSharpCompilation compilation = CreateEmptyCompilation(@"
class Parent {}
class Child : Parent {}
");
            SemanticModel semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
            // Ensure that we don't crash here.
            semanticModel.GetMethodBodyDiagnostics();
        }

        [Fact()]
        [WorkItem(10211, "https://github.com/dotnet/roslyn/issues/10211")]
        public void GetDependenceChainRegression_10211()
        {
            CSharpCompilation compilation = CreateEmptyCompilation(@"
class Child : Parent {}
class Parent {}
");
            SemanticModel semanticModel = compilation.GetSemanticModel(compilation.SyntaxTrees[0]);
            // Ensure that we don't crash here.
            semanticModel.GetMethodBodyDiagnostics();
        }

        private static void VerifySpeculativeSemanticModelForMethodBody(BlockSyntax blockStatement, SemanticModel speculativeModel)
        {
            var localDecl = (LocalDeclarationStatementSyntax)blockStatement.Statements[0];
            VariableDeclaratorSyntax declarator = localDecl.Declaration.Variables.First();
            ISymbol local = speculativeModel.GetDeclaredSymbol(declarator);
            Assert.NotNull(local);
            Assert.Equal("z", local.Name);
            Assert.Equal(SymbolKind.Local, local.Kind);
            Assert.Equal("Int32", ((LocalSymbol)local).Type.Name);

            TypeInfo typeInfo = speculativeModel.GetTypeInfo(localDecl.Declaration.Type);
            Assert.NotNull(typeInfo.Type);
            Assert.Equal("Int32", typeInfo.Type.Name);

            var call = (InvocationExpressionSyntax)((ExpressionStatementSyntax)blockStatement.Statements[1]).Expression;
            ExpressionSyntax arg = call.ArgumentList.Arguments[0].Expression;
            SymbolInfo info = speculativeModel.GetSymbolInfo(arg);
            Assert.NotNull(info.Symbol);
            Assert.Equal("z", info.Symbol.Name);
            Assert.Equal(SymbolKind.Local, info.Symbol.Kind);

            // Shouldn't bind to local y in the original method as we are replacing the method body.
            var call2 = (InvocationExpressionSyntax)((ExpressionStatementSyntax)((BlockSyntax)blockStatement).Statements[2]).Expression;
            ExpressionSyntax arg2 = call2.ArgumentList.Arguments[0].Expression;
            SymbolInfo info2 = speculativeModel.GetSymbolInfo(arg2);
            Assert.Null(info2.Symbol);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForIndexerAccessorBody()
        {
            CSharpCompilation compilation = CreateCompilation(@"
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

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var indexerDecl = (IndexerDeclarationSyntax)typeDecl.Members[0];
            AccessorDeclarationSyntax methodDecl = indexerDecl.AccessorList.Accessors[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            AccessorDeclarationSyntax speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
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
            CSharpCompilation compilation = CreateCompilation(@"
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

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var propertyDecl = (PropertyDeclarationSyntax)typeDecl.Members[0];
            AccessorDeclarationSyntax methodDecl = propertyDecl.AccessorList.Accessors[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            AccessorDeclarationSyntax speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
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
            CSharpCompilation compilation = CreateCompilation(@"
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

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var eventDecl = (EventDeclarationSyntax)typeDecl.Members[0];
            AccessorDeclarationSyntax methodDecl = eventDecl.AccessorList.Accessors[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            AccessorDeclarationSyntax speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
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
            CSharpCompilation compilation = CreateCompilation(@"
class C 
{
  void M(int x)
  {
    int y = 1000;     
  }
}
");

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            // different name local
            var blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"{ int z = 0; }");
            MethodDeclarationSyntax speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].SpanStart, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            VariableDeclaratorSyntax declarator = ((LocalDeclarationStatementSyntax)blockStatement.Statements[0]).Declaration.Variables.First();
            ISymbol local = speculativeModel.GetDeclaredSymbol(declarator);
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

            ISymbol param = speculativeModel.GetSymbolInfo(declarator.Initializer.Value).Symbol;
            Assert.NotNull(param);
            Assert.Equal(SymbolKind.Parameter, param.Kind);
            var paramSymbol = (ParameterSymbol)param;
            Assert.Equal("x", paramSymbol.Name);
            Assert.Equal("Int32", paramSymbol.Type.Name);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForMethodBody_GetDeclaredLabelSymbol()
        {
            CSharpCompilation compilation = CreateCompilation(@"
class C 
{
  void M(int x)
  {
    int y = 1000;     
  }
}
");

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            var blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"{ label: y++; }");
            MethodDeclarationSyntax speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;
            StatementSyntax labeledStatement = blockStatement.Statements[0];

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].SpanStart, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            ISymbol label = speculativeModel.GetDeclaredSymbol(labeledStatement);
            Assert.NotNull(label);
            Assert.Equal("label", label.Name);
            Assert.Equal(SymbolKind.Label, label.Kind);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForMethodBody_GetDeclaredLambdaParameterSymbol()
        {
            CSharpCompilation compilation = CreateCompilationWithMscorlib40AndSystemCore(@"
using System.Linq;

class C 
{
  void M(int x)
  {
    int z = 0;
  }
}
");

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            var blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"{ Func<int, int> var = (z) => x + z; }");
            MethodDeclarationSyntax speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].Span.End, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            ParenthesizedLambdaExpressionSyntax lambdaExpression = blockStatement.Statements[0].DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().FirstOrDefault();
            ParameterSyntax lambdaParam = lambdaExpression.ParameterList.Parameters[0];
            IParameterSymbol parameterSymbol = speculativeModel.GetDeclaredSymbol(lambdaParam);
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

            ISymbol symbol = speculativeModel.GetSymbolInfo(speculatedTypeSyntax).Symbol;
            Assert.NotNull(symbol);
            Assert.Equal(expectedSymbolKind, symbol.Kind);
            Assert.Equal(expectedTypeDisplayString, symbol.ToDisplayString());

            ITypeSymbol typeSymbol = speculativeModel.GetTypeInfo(speculatedTypeSyntax).Type;
            Assert.NotNull(symbol);
            Assert.Equal(expectedSymbolKind, symbol.Kind);
            Assert.Equal(expectedTypeDisplayString, symbol.ToDisplayString());

            if (speculatedTypeSyntax.Kind() == SyntaxKind.QualifiedName)
            {
                SimpleNameSyntax right = ((QualifiedNameSyntax)speculatedTypeSyntax).Right;

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
            CSharpCompilation compilation = CreateCompilation(@"
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

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            var blockStatement = (BlockSyntax)SyntaxFactory.ParseStatement(@"{
    switch(x)
    {
        case 1:
            Console.WriteLine(x);
            break;
    }
}");

            MethodDeclarationSyntax speculatedMethod = methodDecl.ReplaceNode(methodDecl.Body, blockStatement);
            blockStatement = speculatedMethod.Body;
            var switchStatement = (SwitchStatementSyntax)blockStatement.Statements[0];

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModelForMethodBody(methodDecl.Body.Statements[0].SpanStart, speculatedMethod, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            var switchLabel = switchStatement.Sections[0].Labels[0] as CaseSwitchLabelSyntax;
            Assert.NotNull(switchLabel);
            Optional<object> constantVal = speculativeModel.GetConstantValue(switchLabel.Value);
            Assert.True(constantVal.HasValue);
            Assert.Equal(1, constantVal.Value);
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForTypeSyntax_InGlobalUsing()
        {
            CSharpCompilation compilation = CreateCompilation(@"using System.Runtime;");

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            UsingDirectiveSyntax usingStatement = root.Usings[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            NameSyntax speculatedUsingExpression = SyntaxFactory.ParseName("System.Collections");
            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, usingStatement.Name.Position,
                speculatedUsingExpression, SpeculativeBindingOption.BindAsTypeOrNamespace, SymbolKind.Namespace, "System.Collections");
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForTypeSyntax_InGlobalAlias()
        {
            CSharpCompilation compilation = CreateCompilation(@"using A = System.Exception;");

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            UsingDirectiveSyntax usingStatement = root.Usings[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            NameSyntax speculatedUsingExpression = SyntaxFactory.ParseName("System.ArgumentException");
            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, usingStatement.Name.Position,
                speculatedUsingExpression, SpeculativeBindingOption.BindAsExpression, SymbolKind.NamedType, "System.ArgumentException");
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForTypeSyntax_InBaseList()
        {
            CSharpCompilation compilation = CreateCompilation(@"
class MyException : System.Exception
{
}
");

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            BaseListSyntax baseList = typeDecl.BaseList;
            SemanticModel model = compilation.GetSemanticModel(tree);

            NameSyntax speculatedTypeExpression = SyntaxFactory.ParseName("System.ArgumentException");
            TestGetSpeculativeSemanticModelForTypeSyntax_Common(model, baseList.SpanStart,
                speculatedTypeExpression, SpeculativeBindingOption.BindAsTypeOrNamespace, SymbolKind.NamedType, "System.ArgumentException");
        }

        [Fact]
        public void TestGetSpeculativeSemanticModelForTypeSyntax_InMemberDeclaration()
        {
            CSharpCompilation compilation = CreateCompilation(@"
class Program
{
    System.Exception field = null;
    System.Exception Method(System.Exception param)
    {
        return field;
    }
}
");

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var fieldDecl = (FieldDeclarationSyntax)typeDecl.Members[0];
            var methodDecl = (MethodDeclarationSyntax)typeDecl.Members[1];
            SemanticModel model = compilation.GetSemanticModel(tree);

            NameSyntax speculatedTypeExpression = SyntaxFactory.ParseName("System.ArgumentException");
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
            CSharpCompilation compilation = CreateCompilation(@"
using A = System.ArgumentException;

class Program
{
    System.Exception field = null;
}
");

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            var fieldDecl = (FieldDeclarationSyntax)typeDecl.Members[0];
            SemanticModel model = compilation.GetSemanticModel(tree);

            NameSyntax speculatedAliasName = SyntaxFactory.ParseName("A");

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
            CSharpCompilation compilation = CreateCompilationWithMscorlib40AndDocumentationComments(@"
/// <summary>
/// <see cref=""int""/>
/// </summary>
class A { }
");

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            TypeCrefSyntax cref = typeDecl.DescendantNodes(descendIntoTrivia: true).OfType<TypeCrefSyntax>().Single();

            var model = (CSharpSemanticModel)compilation.GetSemanticModel(tree);
            ISymbol symbol = model.GetSymbolInfo(cref.Type).Symbol;
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
            CSharpCompilation compilation = CreateCompilationWithMscorlib40AndDocumentationComments(@"
/// <summary>
/// <see cref=""int""/>
/// </summary>
class A { }
");

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            TypeCrefSyntax cref = typeDecl.DescendantNodes(descendIntoTrivia: true).OfType<TypeCrefSyntax>().Single();

            var model = (CSharpSemanticModel)compilation.GetSemanticModel(tree);
            ISymbol symbol = model.GetSymbolInfo(cref.Type).Symbol;
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
            CSharpCompilation compilation = CreateCompilationWithMscorlib40AndDocumentationComments(@"
/// <summary>
/// <see cref=""int""/>
/// </summary>
class A
{
    class B { }

    static void M() { }
}
");

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var typeDecl = (TypeDeclarationSyntax)root.Members[0];
            TypeCrefSyntax cref = typeDecl.DescendantNodes(descendIntoTrivia: true).OfType<TypeCrefSyntax>().Single();

            var model = (CSharpSemanticModel)compilation.GetSemanticModel(tree);
            ISymbol symbol = model.GetSymbolInfo(cref.Type).Symbol;
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
            CSharpCompilation compilation = CreateCompilationWithMscorlib40AndDocumentationComments(@"
/// <summary>
/// <see cref=""int""/>
/// </summary>
class A
{
    static void M() { }
}
");

            SyntaxTree tree = compilation.SyntaxTrees[0];
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
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
            SymbolInfo symbolInfo = model.GetSpeculativeSymbolInfo(methodDecl.Body.SpanStart, speculatedCref);
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

            CSharpCompilation comp = CreateCompilation(source);
            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            IdentifierNameSyntax syntax = tree.GetRoot().DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().Single().
                Body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Single();
            Assert.Equal("x", syntax.Identifier.ValueText);

            ISymbol symbol = model.GetSymbolInfo(syntax).Symbol;
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

            CSharpCompilation comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            var position = source.IndexOf("return", StringComparison.Ordinal);
            var yieldStatement = (YieldStatementSyntax)SyntaxFactory.ParseStatement("yield return 1;");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(position, yieldStatement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            TypeInfo info = speculativeModel.GetTypeInfo(yieldStatement.Expression);
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

            CSharpCompilation comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            var position = source.IndexOf("return", StringComparison.Ordinal);
            var yieldStatement = (YieldStatementSyntax)SyntaxFactory.ParseStatement("yield return 1;");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(position, yieldStatement, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            TypeInfo info = speculativeModel.GetTypeInfo(yieldStatement.Expression);
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

            CSharpCompilation comp = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            var position = source.IndexOf("int", StringComparison.Ordinal);
            TypeSyntax typeSyntax = SyntaxFactory.ParseTypeName("System.Collections.Generic.IEnumerable<C[]>");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(position, typeSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            OmittedArraySizeExpressionSyntax omittedArraySize = typeSyntax.DescendantNodes().OfType<OmittedArraySizeExpressionSyntax>().Single();
            SymbolInfo info = speculativeModel.GetSymbolInfo(omittedArraySize); // Used to throw NRE.
            Assert.Null(info.Symbol);
            Assert.Equal(CandidateReason.None, info.CandidateReason);
            Assert.Equal(0, info.CandidateSymbols.Length);
        }

        [WorkItem(14384, "https://github.com/dotnet/roslyn/issues/14384")]
        [Fact]
        public void SpeculateWithExpressionVariables_out()
        {
            var source = @"
class C
{
    void M()
    {
        int x;
        TakesOut(out x);
    }
    void TakesOut(out int x) { x = 0; }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            ExpressionSyntax method1 = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
            SymbolInfo info1 = model.GetSymbolInfo(method1);
            Assert.NotNull(info1.Symbol);

            var position = source.IndexOf("TakesOut", StringComparison.Ordinal);
            StatementSyntax statementSyntax = SyntaxFactory.ParseStatement("TakesOut(out int x);");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(position, statementSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            ExpressionSyntax method2 = statementSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
            SymbolInfo info2 = speculativeModel.GetSymbolInfo(method2);
            Assert.NotNull(info2.Symbol);

            Assert.Equal(info1.Symbol, info2.Symbol);
        }

        [WorkItem(14384, "https://github.com/dotnet/roslyn/issues/14384")]
        [Fact]
        public void SpeculateWithExpressionVariables_out2()
        {
            var source = @"
class C
{
    void M()
    {
        int y;
        TakesOut(out y);
    }
    void TakesOut(out int y) { y = 0; }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            ExpressionSyntax method1 = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
            SymbolInfo info1 = model.GetSymbolInfo(method1);
            Assert.NotNull(info1.Symbol);

            var position = source.IndexOf("TakesOut", StringComparison.Ordinal);
            StatementSyntax statementSyntax = SyntaxFactory.ParseStatement("TakesOut(out int x);");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(position, statementSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            ExpressionSyntax method2 = statementSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
            SymbolInfo info2 = speculativeModel.GetSymbolInfo(method2);
            Assert.NotNull(info2.Symbol);

            Assert.Equal(info1.Symbol, info2.Symbol);
        }

        [WorkItem(14384, "https://github.com/dotnet/roslyn/issues/14384")]
        [Fact]
        public void SpeculateWithExpressionVariables_out3()
        {
            var source = @"
class C
{
    void M()
    {
        int x;
        TakesOut(out x);
    }
    void TakesOut(out int x) { x = 0; }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            ExpressionSyntax method1 = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
            SymbolInfo info1 = model.GetSymbolInfo(method1);
            Assert.NotNull(info1.Symbol);

            var position = source.IndexOf("TakesOut", StringComparison.Ordinal);
            ExpressionSyntax statementSyntax = SyntaxFactory.ParseExpression("TakesOut(out int x)");
            SymbolInfo info2 = model.GetSpeculativeSymbolInfo(position, statementSyntax, SpeculativeBindingOption.BindAsExpression);
            Assert.NotNull(info2.Symbol);

            Assert.Equal(info1.Symbol, info2.Symbol);
        }

        [WorkItem(14384, "https://github.com/dotnet/roslyn/issues/14384")]
        [Fact]
        public void SpeculateWithExpressionVariables_out4()
        {
            var source = @"
class C
{
    void M()
    {
        int y;
        TakesOut(out y);
    }
    void TakesOut(out int y) { y = 0; }
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            ExpressionSyntax method1 = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
            SymbolInfo info1 = model.GetSymbolInfo(method1);
            Assert.NotNull(info1.Symbol);

            var position = source.IndexOf("TakesOut", StringComparison.Ordinal);
            StatementSyntax statementSyntax = SyntaxFactory.ParseStatement("{ TakesOut(out int x); }");
            statementSyntax = ((BlockSyntax)statementSyntax).Statements[0];

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(position, statementSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            ExpressionSyntax method2 = statementSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
            SymbolInfo info2 = speculativeModel.GetSymbolInfo(method2);
            Assert.NotNull(info2.Symbol);

            Assert.Equal(info1.Symbol, info2.Symbol);
        }

        [WorkItem(14384, "https://github.com/dotnet/roslyn/issues/14384")]
        [Fact]
        public void SpeculateWithExpressionVariables_pattern()
        {
            var source = @"
class C
{
    void M(object o)
    {
        Method(o is string);
        string s = o as string;
    }
    bool Method(bool b) => b;
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            ExpressionSyntax method1 = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
            SymbolInfo info1 = model.GetSymbolInfo(method1);
            Assert.NotNull(info1.Symbol);

            var position = source.IndexOf("Method", StringComparison.Ordinal);
            StatementSyntax statementSyntax = SyntaxFactory.ParseStatement("Method(o is string s);");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(position, statementSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            ExpressionSyntax method2 = statementSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
            SymbolInfo info2 = speculativeModel.GetSymbolInfo(method2);
            Assert.NotNull(info2.Symbol);

            Assert.Equal(info1.Symbol, info2.Symbol);
        }

        [WorkItem(14384, "https://github.com/dotnet/roslyn/issues/14384")]
        [Fact]
        public void SpeculateWithExpressionVariables_pattern2()
        {
            var source = @"
class C
{
    void M(object o)
    {
        Method(o is string);
        string s2 = o as string;
    }
    bool Method(bool b) => b;
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            ExpressionSyntax method1 = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
            SymbolInfo info1 = model.GetSymbolInfo(method1);
            Assert.NotNull(info1.Symbol);

            var position = source.IndexOf("Method", StringComparison.Ordinal);
            StatementSyntax statementSyntax = SyntaxFactory.ParseStatement("Method(o is string s);");

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(position, statementSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            ExpressionSyntax method2 = statementSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
            SymbolInfo info2 = speculativeModel.GetSymbolInfo(method2);
            Assert.NotNull(info2.Symbol);

            Assert.Equal(info1.Symbol, info2.Symbol);
        }

        [WorkItem(14384, "https://github.com/dotnet/roslyn/issues/14384")]
        [Fact]
        public void SpeculateWithExpressionVariables_pattern3()
        {
            var source = @"
class C
{
    void M(object o)
    {
        Method(o is string);
        string s2 = o as string;
    }
    bool Method(bool b) => b;
}
";

            CSharpCompilation comp = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            ExpressionSyntax method1 = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single().Expression;
            SymbolInfo info1 = model.GetSymbolInfo(method1);
            Assert.NotNull(info1.Symbol);

            var position = source.IndexOf("Method", StringComparison.Ordinal);
            ExpressionSyntax statementSyntax = SyntaxFactory.ParseExpression("Method(o is string s)");
            SymbolInfo info2 = model.GetSpeculativeSymbolInfo(position, statementSyntax, SpeculativeBindingOption.BindAsExpression);
            Assert.NotNull(info2.Symbol);

            Assert.Equal(info1.Symbol, info2.Symbol);
        }

        [Fact]
        public void BindSpeculativeAttributeWithExpressionVariable_1()
        {
            new ObsoleteAttribute("goo");

            var source =
@"using System;
class C { }";
            CSharpCompilation compilation = CreateCompilation(source);

            SyntaxTree tree = compilation.SyntaxTrees.Single();
            SemanticModel model = compilation.GetSemanticModel(tree);

            var position = tree.GetText().ToString().IndexOf("class C {", StringComparison.Ordinal);
            AttributeSyntax attr2 = ParseAttributeSyntax("[ObsoleteAttribute(string.Empty is string s ? s : string.Empty)]");

            SymbolInfo symbolInfo = model.GetSpeculativeSymbolInfo(position, attr2);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.Symbol.ToTestDisplayString());
        }

        [Fact]
        public void BindSpeculativeAttributeWithExpressionVariable_2()
        {
            new ObsoleteAttribute("goo");

            var source =
@"using System;
class C { }";
            CSharpCompilation compilation = CreateCompilation(source);

            SyntaxTree tree = compilation.SyntaxTrees.Single();
            SemanticModel model = compilation.GetSemanticModel(tree);

            var position = tree.GetText().ToString().IndexOf("class C {", StringComparison.Ordinal);
            AttributeSyntax attr2 = ParseAttributeSyntax("[ObsoleteAttribute(string.Empty is string s ? s : string.Empty)]");
            SemanticModel model2;
            model.TryGetSpeculativeSemanticModel(position, attr2, out model2);

            SymbolInfo symbolInfo = model2.GetSymbolInfo(attr2);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Equal("System.ObsoleteAttribute..ctor(System.String message)", symbolInfo.Symbol.ToTestDisplayString());
        }

        private AttributeSyntax ParseAttributeSyntax(string source)
        {
            return SyntaxFactory.ParseCompilationUnit(source + " class X {}").Members.First().AsTypeDeclarationSyntax().AttributeLists.First().Attributes.First();
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

            CSharpCompilation comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            var position = source.IndexOf("struct", StringComparison.Ordinal);
            AttributeSyntax attributeSyntax = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Category"));

            SemanticModel speculativeModel;
            var success = model.TryGetSpeculativeSemanticModel(position, attributeSyntax, out speculativeModel);
            Assert.True(success);
            Assert.NotNull(speculativeModel);

            SymbolInfo info = speculativeModel.GetSymbolInfo(attributeSyntax.Name);
        }

        [WorkItem(1015557, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015557")]
        [Fact(Skip = "1015557")]
        public void GetSpeculativeSymbolInfoForGenericNameInCref()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(@"using System.Collections.Generic;
class Program
{
    /// <summary>
    /// <see cref=""System.Collections.Generic.List{T}.Contains(T)""/>
    /// </summary>
    static void Main()
    {
    }
}", CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose));
            CSharpCompilation compilation = CreateCompilation(tree);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            QualifiedCrefSyntax crefSyntax = root.DescendantNodes(descendIntoTrivia: true).OfType<QualifiedCrefSyntax>().Single();
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(crefSyntax.FindNode(new TextSpan(91, 34)));
            ISymbol oldSymbol = symbolInfo.Symbol;
            Assert.NotNull(oldSymbol);
            Assert.Equal(SymbolKind.NamedType, oldSymbol.Kind);
            Assert.Equal("System.Collections.Generic.List<T>", oldSymbol.ToTestDisplayString());

            var speculatedName = (GenericNameSyntax)SyntaxFactory.GenericName("List{T}");
            SymbolInfo speculativeSymbolInfo = semanticModel.GetSpeculativeSymbolInfo(crefSyntax.SpanStart, speculatedName, SpeculativeBindingOption.BindAsExpression);
            ISymbol newSymbol = speculativeSymbolInfo.Symbol;
            Assert.NotNull(newSymbol);
            Assert.Equal(SymbolKind.NamedType, newSymbol.Kind);
            Assert.Equal("System.Collections.Generic.List<T>", newSymbol.ToTestDisplayString());

            Assert.False(((NamedTypeSymbol)newSymbol).TypeArguments().Single().IsErrorType());
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
            CSharpCompilation comp = CreateCompilation(source);

            SyntaxTree tree = comp.SyntaxTrees.Single();
            Assert.NotEmpty(tree.GetDiagnostics());

            MemberAccessExpressionSyntax memberAccess = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
            var identifier = (IdentifierNameSyntax)memberAccess.Expression;
            Assert.Equal("s", identifier.Identifier.ValueText);

            NamedTypeSymbol stringType = comp.GetSpecialType(SpecialType.System_String);
            NamedTypeSymbol actionType = comp.GetWellKnownType(WellKnownType.System_Action_T).Construct(stringType);

            SemanticModel model = comp.GetSemanticModel(tree);

            // Can't walk up to ArgumentListSyntax because indexers use BracketedArgumentListSyntax.
            CSharpSyntaxNode expr = identifier.FirstAncestorOrSelf<ArgumentSyntax>().Parent.Parent;

            SymbolInfo exprInfo = model.GetSymbolInfo(expr);
            TypeSymbol firstParamType = ((Symbol)exprInfo.CandidateSymbols.Single()).GetParameterTypes().First();
            Assert.Equal(actionType, firstParamType);

            TypeInfo identifierInfo = model.GetTypeInfo(identifier);
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

            CSharpCompilation comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();

            MethodSymbol extensionMethod = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Extensions").GetMember<MethodSymbol>("ToString");

            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            InvocationExpressionSyntax callSyntax = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            ImmutableArray<ISymbol> memberGroup = model.GetMemberGroup(callSyntax.Expression);
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

            MetadataReference libRef = CreateCompilation("", assemblyName: "lib").EmitToImageReference(aliases: ImmutableArray.Create("Alias"));
            CSharpCompilation comp = CreateCompilation(source, new[] { libRef });
            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            AliasQualifiedNameSyntax syntax = tree.GetRoot().DescendantNodes().OfType<AliasQualifiedNameSyntax>().Single();

            ISymbol symbol = model.LookupSymbols(syntax.SpanStart, name: "Alias").Single();
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

            CSharpCompilation comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);

            MethodDeclarationSyntax method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            ExpressionSyntax init0 = method.Body.Statements[0].DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer.Value;
            Optional<object> value0 = model.GetConstantValue(init0);
            TypeInfo typeInfo0 = model.GetTypeInfo(init0);
            Assert.True(value0.HasValue);
            Assert.Equal(-1, (short)value0.Value);
            Assert.True(typeInfo0.Type != null && typeInfo0.Type.SpecialType == SpecialType.System_Int16);

            // The CodePlex bug indicates this should return a constant value of 5.  While 'case2' should 
            // have that value it is not constant because of the nullable cast
            ExpressionSyntax init1 = method.Body.Statements[2].DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer.Value;
            Optional<object> value1 = model.GetConstantValue(init1);
            TypeInfo typeInfo1 = model.GetTypeInfo(init1);
            NamedTypeSymbol type1 = comp.GetSpecialType(SpecialType.System_Nullable_T).Construct(comp.GetSpecialType(SpecialType.System_Int32));
            Assert.False(value1.HasValue);
            Assert.True(typeInfo1.Type != null && typeInfo1.Type.Equals(type1));

            ExpressionSyntax init2 = method.Body.Statements[4].DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer.Value;
            Optional<object> value2 = model.GetConstantValue(init2);
            TypeInfo typeInfo2 = model.GetTypeInfo(init2);
            NamedTypeSymbol type2 = comp.GetSpecialType(SpecialType.System_Nullable_T).Construct(comp.GetSpecialType(SpecialType.System_Int32));
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

            CSharpCompilation compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            SemanticModel model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            for (System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(source, " => x"); match.Success; match = match.NextMatch())
            {
                ISymbol discarded = model.GetEnclosingSymbol(match.Index);
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

            CSharpCompilation compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            SyntaxTree tree = compilation.SyntaxTrees.Single();
            SemanticModel model = compilation.GetSemanticModel(tree);
            for (System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(source, " => x"); match.Success; match = match.NextMatch())
            {
                SyntaxNode x = tree.GetRoot().FindToken(match.Index + 4).Parent;
                ISymbol discarded = model.GetEnclosingSymbol(match.Index);
                SymbolInfo disc = model.GetSymbolInfo(x);
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

            CSharpCompilation compilation = CreateCompilationWithMscorlib40AndDocumentationComments(source);
            SemanticModel model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());
            ISymbol discarded1 = model.GetEnclosingSymbol(source.LastIndexOf(" => x"));
            ISymbol discarded2 = model.GetEnclosingSymbol(source.IndexOf(" => x"));
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

            CSharpCompilation comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);
            foreach (InterpolatedStringExpressionSyntax interp in tree.GetRoot().DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
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
            CSharpCompilation comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            SyntaxTree tree = comp.SyntaxTrees.Single();
            SemanticModel model = comp.GetSemanticModel(tree);
            TypeSyntax typeSyntax = SyntaxFactory.ParseTypeName("dynamic");
            int spanStart = source.IndexOf("dynamic a = 5;");
            TypeInfo dynamicType = model.GetSpeculativeTypeInfo(spanStart, typeSyntax, SpeculativeBindingOption.BindAsTypeOrNamespace);
            Assert.Equal(TypeKind.Dynamic, dynamicType.Type.TypeKind);
        }

        [Fact]
        public void IsAccessible()
        {
            var source =
@"using System;
class A
{
    public int X;
    protected int Y;
    private protected int Z;
}
class B : A
{
    void Goo()
    {
        // in B.Goo
    }
    // in B class level
    int field;
}
class C
{
    void Goo()
    {
        // in C.Goo
    }
}
namespace N
{
    // in N
}";
            CSharpCompilation compilation = CreateCompilation(source, parseOptions: TestOptions.Regular7_2);
            compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Verify();

            SyntaxTree tree = compilation.SyntaxTrees[0];
            var text = tree.GetText().ToString();
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);
            int positionInB = text.IndexOf("in B class level");
            int positionInBGoo = text.IndexOf("in B.Goo");
            int positionInCGoo = text.IndexOf("in C.Goo");
            int positionInN = text.IndexOf("in N");

            NamespaceSymbol globalNs = compilation.GlobalNamespace;
            var classA = (NamedTypeSymbol)globalNs.GetMembers("A").Single();
            var fieldX = (FieldSymbol)classA.GetMembers("X").Single();
            var fieldY = (FieldSymbol)classA.GetMembers("Y").Single();
            var fieldZ = (FieldSymbol)classA.GetMembers("Z").Single();

            Assert.True(semanticModel.IsAccessible(positionInN, fieldX));
            Assert.False(semanticModel.IsAccessible(positionInN, fieldY));
            Assert.False(semanticModel.IsAccessible(positionInN, fieldZ));
            Assert.True(semanticModel.IsAccessible(positionInB, fieldX));
            Assert.True(semanticModel.IsAccessible(positionInB, fieldY));
            Assert.True(semanticModel.IsAccessible(positionInB, fieldZ));
            Assert.True(semanticModel.IsAccessible(positionInBGoo, fieldX));
            Assert.True(semanticModel.IsAccessible(positionInBGoo, fieldY));
            Assert.True(semanticModel.IsAccessible(positionInBGoo, fieldZ));
            Assert.True(semanticModel.IsAccessible(positionInCGoo, fieldX));
            Assert.False(semanticModel.IsAccessible(positionInCGoo, fieldY));
            Assert.False(semanticModel.IsAccessible(positionInCGoo, fieldZ));
        }

        #region "regression helper"
        private void Regression(string text)
        {
            SyntaxTree tree = Parse(text);
            CSharpCompilation compilation = CreateCompilation(tree);
            SemanticModel model = compilation.GetSemanticModel(tree);
            var exprSynList = new List<ExpressionSyntax>();
            GetExpressionSyntax(tree.GetCompilationUnitRoot(), exprSynList);

            // Console.WriteLine("Roslyn Symbol Info: ");
            foreach (ExpressionSyntax exprSyn in exprSynList)
            {
                var expr = exprSyn.ToString();
                // Console.WriteLine("Expression: " + expr);
                ITypeSymbol type = model.GetTypeInfo(exprSyn).Type;
                // Console.WriteLine("Type: " + type);
                // Console.WriteLine();
            }
        }
        private static void GetExpressionSyntax(SyntaxNode node, List<ExpressionSyntax> exprSynList)
        {
            if (node is ExpressionSyntax)
                exprSynList.Add(node as ExpressionSyntax);

            foreach (SyntaxNodeOrToken child in node.ChildNodesAndTokens())
                if (child.IsNode)
                    GetExpressionSyntax(child.AsNode(), exprSynList);
        }
        #endregion

    }
}
