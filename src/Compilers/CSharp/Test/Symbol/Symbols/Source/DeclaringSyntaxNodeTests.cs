// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Source
{
    public class DeclaringSyntaxNodeTests : CSharpTestBase
    {
        // Check that the given symbol has the expected number of declaring syntax nodes.
        // and that each declared node goes back to the given symbol.
        private ImmutableArray<SyntaxReference> CheckDeclaringSyntaxNodes(CSharpCompilation compilation,
                                               Symbol symbol,
                                               int expectedNumber)
        {
            var declaringReferences = symbol.DeclaringSyntaxReferences;
            Assert.Equal(expectedNumber, declaringReferences.Length);

            if (expectedNumber == 0)
            {
                Assert.True(!symbol.IsFromCompilation(compilation) || symbol.IsImplicitlyDeclared, "non-implicitly declares source symbol should have declaring location");
            }
            else
            {
                Assert.True(symbol.IsFromCompilation(compilation) || symbol is MergedNamespaceSymbol, "symbol with declaration should be in source, except for merged namespaces");
                Assert.False(symbol.IsImplicitlyDeclared);

                foreach (var node in declaringReferences.Select(d => d.GetSyntax()))
                {
                    // Make sure GetDeclaredSymbol gets back to the symbol for each node.

                    SyntaxTree tree = node.SyntaxTree;
                    SemanticModel model = compilation.GetSemanticModel(tree);
                    Assert.Equal(symbol.OriginalDefinition, model.GetDeclaredSymbol(node));
                }
            }

            return declaringReferences;
        }

        private ImmutableArray<SyntaxReference> CheckDeclaringSyntaxNodesIncludingParameters(CSharpCompilation compilation,
                                               Symbol symbol,
                                               int expectedNumber)
        {
            var nodes = CheckDeclaringSyntaxNodes(compilation, symbol, expectedNumber);

            MethodSymbol meth = symbol as MethodSymbol;
            if (meth != null)
            {
                foreach (ParameterSymbol p in meth.Parameters)
                    CheckDeclaringSyntaxNodes(compilation, p, meth.IsAccessor() ? 0 : expectedNumber);
            }

            PropertySymbol prop = symbol as PropertySymbol;
            if (prop != null)
            {
                foreach (ParameterSymbol p in prop.Parameters)
                    CheckDeclaringSyntaxNodes(compilation, p, expectedNumber);
            }

            return nodes;
        }

        // Check that the given symbol has the expected number of declaring syntax nodes.
        // and that the syntax has the expected kind. Does NOT test GetDeclaringSymbol
        private ImmutableArray<SyntaxReference> CheckDeclaringSyntaxNodesWithoutGetDeclaredSymbol(CSharpCompilation compilation,
                                               Symbol symbol,
                                               int expectedNumber,
                                               SyntaxKind expectedSyntaxKind)
        {
            var declaringReferences = symbol.DeclaringSyntaxReferences;
            Assert.Equal(expectedNumber, declaringReferences.Length);

            if (expectedNumber == 0)
            {
                Assert.True(!symbol.IsFromCompilation(compilation) || symbol.IsImplicitlyDeclared, "non-implicitly declares source symbol should have declaring location");
            }
            else
            {
                Assert.True(symbol.IsFromCompilation(compilation) || symbol is MergedNamespaceSymbol, "symbol with declaration should be in source, except for merged namespaces");

                if (symbol.Kind == SymbolKind.Namespace && ((NamespaceSymbol)symbol).IsGlobalNamespace)
                {
                    Assert.True(symbol.IsImplicitlyDeclared);
                }
                else
                {
                    Assert.False(symbol.IsImplicitlyDeclared);
                }

                foreach (var node in declaringReferences.Select(d => d.GetSyntax()))
                {
                    // Make sure each node is of the expected kind.
                    Assert.Equal(expectedSyntaxKind, node.Kind());
                }
            }

            return declaringReferences;
        }

        private void AssertDeclaringSyntaxNodes(Symbol symbol, CSharpCompilation compilation, params SyntaxNode[] expectedSyntaxNodes)
        {
            int expectedNumber = expectedSyntaxNodes.Length;
            var declaringReferences = symbol.DeclaringSyntaxReferences;
            Assert.Equal(expectedNumber, declaringReferences.Length);

            if (expectedNumber == 0)
            {
                Assert.True(!symbol.IsFromCompilation(compilation) || symbol.IsImplicitlyDeclared, "non-implicitly declares source symbol should have declaring location");
            }
            else
            {
                Assert.True(symbol.IsFromCompilation(compilation) || symbol is MergedNamespaceSymbol, "symbol with declaration should be in source, except for merged namespaces");
                Assert.False(symbol.IsImplicitlyDeclared);

                for (int i = 0; i < expectedNumber; i++)
                {
                    Assert.Same(expectedSyntaxNodes[i], declaringReferences[i].GetSyntax());
                }
            }
        }

        private void CheckDeclaringSyntax<TNode>(CSharpCompilation comp, SyntaxTree tree, string name, SymbolKind kind)
            where TNode : CSharpSyntaxNode
        {
            var model = comp.GetSemanticModel(tree);
            string code = tree.GetText().ToString();
            int position = code.IndexOf(name, StringComparison.Ordinal);
            var node = tree.GetRoot().FindToken(position).Parent.FirstAncestorOrSelf<TNode>();
            var sym = (Symbol)model.GetDeclaredSymbol(node);

            Assert.Equal(kind, sym.Kind);
            Assert.Equal(name, sym.Name);

            CheckDeclaringSyntaxNodes(comp, sym, 1);
        }

        private void CheckLambdaDeclaringSyntax<TNode>(CSharpCompilation comp, SyntaxTree tree, string textToSearchFor)
            where TNode : ExpressionSyntax
        {
            var model = comp.GetSemanticModel(tree);
            string code = tree.GetText().ToString();
            int position = code.IndexOf(textToSearchFor, StringComparison.Ordinal);
            var node = tree.GetCompilationUnitRoot().FindToken(position).Parent.FirstAncestorOrSelf<TNode>();
            MethodSymbol sym = model.GetSymbolInfo(node).Symbol as MethodSymbol;

            Assert.NotNull(sym);
            Assert.Equal(MethodKind.AnonymousFunction, sym.MethodKind);

            var nodes = CheckDeclaringSyntaxNodesWithoutGetDeclaredSymbol(comp, sym, 1, node.Kind());
            Assert.Equal(nodes[0].GetSyntax(), node);

            foreach (ParameterSymbol p in sym.Parameters)
            {
                CheckDeclaringSyntaxNodes(comp, p, 1);
            }
        }

        [Fact]
        public void SourceNamedTypeDeclaringSyntax()
        {
            var text =
@"
namespace N1 {
    class C1<T> {
        class Nested<U> {}
        delegate int NestedDel(string s);
    }
    public struct S1 {
        C1<int> f;
    }
    internal interface I1 {}
    enum E1 { Red }
    delegate void D1(int i);

}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var n1 = global.GetMembers("N1").Single() as NamespaceSymbol;

            Assert.False(n1.IsImplicitlyDeclared);
            Assert.True(comp.SourceModule.GlobalNamespace.IsImplicitlyDeclared);

            var types = n1.GetTypeMembers();
            foreach (Symbol s in types)
            {
                CheckDeclaringSyntaxNodes(comp, s, 1);
            }

            var c1 = n1.GetTypeMembers("C1").Single() as NamedTypeSymbol;
            var s1 = n1.GetTypeMembers("S1").Single() as NamedTypeSymbol;
            var f = s1.GetMembers("f").Single() as FieldSymbol;

            CheckDeclaringSyntaxNodes(comp, f.Type.TypeSymbol, 1);  // constructed type C1<int>.

            // Nested types.
            foreach (Symbol s in c1.GetTypeMembers())
            {
                CheckDeclaringSyntaxNodes(comp, s, 1);
            }
        }

        [Fact]
        public void NonSourceTypeDeclaringSyntax()
        {
            var text =
@"
namespace N1 {
    class C1 {
        object o;
        int i;
        System.Collections.Generic.List<string> lst;
        dynamic dyn;
        C1[] arr;
        C1[,] arr2d;
        ErrType err;
        ConsErrType<object> consErr;
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var n1 = global.GetMembers("N1").Single() as NamespaceSymbol;
            var c1 = n1.GetTypeMembers("C1").Single() as NamedTypeSymbol;

            // Check types of each field in C1; should not have declaring syntax node.
            foreach (FieldSymbol f in c1.GetMembers().OfType<FieldSymbol>())
            {
                CheckDeclaringSyntaxNodes(comp, f.Type.TypeSymbol, 0);
            }
        }

        [Fact]
        public void AnonTypeDeclaringSyntax()
        {
            var text =
@"
class C1 {
    void f()
    {
        var a1 = new { a = 5, b = ""hi"" };
        var a2 = new {};
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);
            var global = comp.GlobalNamespace;
            int posA1 = text.IndexOf("a1", StringComparison.Ordinal);

            var declaratorA1 = tree.GetCompilationUnitRoot().FindToken(posA1).Parent.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
            var localA1 = (LocalSymbol)model.GetDeclaredSymbol(declaratorA1);
            var localA1Type = localA1.Type.TypeSymbol;
            Assert.True(localA1Type.IsAnonymousType);

            // Anonymous types don't support GetDeclaredSymbol.
            CheckDeclaringSyntaxNodesWithoutGetDeclaredSymbol(comp, localA1Type, 1, SyntaxKind.AnonymousObjectCreationExpression);

            // Check members of the anonymous type.
            foreach (Symbol memb in localA1Type.GetMembers())
            {
                int expectedDeclaringNodes = 0;

                if (memb is PropertySymbol)
                {
                    expectedDeclaringNodes = 1;  // declared property 
                }

                CheckDeclaringSyntaxNodes(comp, memb, expectedDeclaringNodes);
            }
        }

        [WorkItem(543829, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543829")]
        [Fact]
        public void AnonymousTypeSymbolWithExplicitNew()
        {
            var text =
@"
class C1 {
    void f()
    {
        var q = new { y = 2 };
        var x = new { y = 5 };
        var z = x;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);
            var global = comp.GlobalNamespace;

            // check 'q'
            int posQ = text.IndexOf('q');
            var declaratorQ = tree.GetCompilationUnitRoot().FindToken(posQ).Parent.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
            CheckAnonymousType(model,
                (LocalSymbol)model.GetDeclaredSymbol(declaratorQ),
                (AnonymousObjectCreationExpressionSyntax)declaratorQ.Initializer.Value);

            // check 'x'
            int posX = text.IndexOf('x');
            var declaratorX = tree.GetCompilationUnitRoot().FindToken(posX).Parent.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
            CheckAnonymousType(model,
                (LocalSymbol)model.GetDeclaredSymbol(declaratorX),
                (AnonymousObjectCreationExpressionSyntax)declaratorX.Initializer.Value);

            // check 'z' --> 'x'
            int posZ = text.IndexOf('z');
            var declaratorZ = tree.GetCompilationUnitRoot().FindToken(posZ).Parent.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
            CheckAnonymousType(model,
                (LocalSymbol)model.GetDeclaredSymbol(declaratorZ),
                (AnonymousObjectCreationExpressionSyntax)declaratorX.Initializer.Value);
        }

        private void CheckAnonymousType(SemanticModel model, LocalSymbol local, AnonymousObjectCreationExpressionSyntax anonObjectCreation)
        {
            var localType = local.Type.TypeSymbol;
            Assert.True(localType.IsAnonymousType);

            // IsImplicitlyDeclared: Return false. The new { } clause 
            //                       serves as the declaration.
            Assert.False(localType.IsImplicitlyDeclared);

            // DeclaringSyntaxNodes: Return the AnonymousObjectCreationExpression from the particular 
            //                       anonymous type definition that flowed to this usage.
            AssertDeclaringSyntaxNodes(localType, (CSharpCompilation)model.Compilation, anonObjectCreation);

            // SemanticModel.GetDeclaredSymbol: Return this symbol when applied to the 
            //                                  AnonymousObjectCreationExpression in the new { } declaration.
            var symbol = model.GetDeclaredSymbol(anonObjectCreation);
            Assert.NotNull(symbol);
            Assert.Equal<ISymbol>(localType, symbol);
            Assert.Same(localType.DeclaringSyntaxReferences[0].GetSyntax(), symbol.DeclaringSyntaxReferences[0].GetSyntax());

            // Locations: Return the Span of that particular 
            //            AnonymousObjectCreationExpression's NewKeyword.
            Assert.Equal(1, localType.Locations.Length);
            Assert.Equal(localType.Locations[0], anonObjectCreation.NewKeyword.GetLocation());

            // Members check
            int propIndex = 0;
            foreach (var member in localType.GetMembers())
            {
                if (member.Kind == SymbolKind.Property)
                {
                    // Equals: Return true when comparing same-named members of 
                    //         structurally-equivalent anonymous type symbols.
                    var members = symbol.GetMembers(member.Name);
                    Assert.Equal(1, members.Length);
                    Assert.Equal(member, members[0]);

                    // IsImplicitlyDeclared: Return false. The foo = bar clause in 
                    //                       the new { } clause serves as the declaration.
                    Assert.False(member.IsImplicitlyDeclared);

                    // DeclaringSyntaxNodes: Return the AnonymousObjectMemberDeclarator from the 
                    //                       particular property definition that flowed to this usage.
                    var propertyInitializer = anonObjectCreation.Initializers[propIndex];
                    Assert.Equal(1, member.DeclaringSyntaxReferences.Length);
                    Assert.Same(propertyInitializer, member.DeclaringSyntaxReferences[0].GetSyntax());

                    // SemanticModel.GetDeclaredSymbol: Return this symbol when applied to its new { } 
                    //                                  declaration's AnonymousObjectMemberDeclarator.
                    var propSymbol = model.GetDeclaredSymbol(propertyInitializer);
                    Assert.Equal<ISymbol>(member, propSymbol);
                    Assert.Same(propertyInitializer, propSymbol.DeclaringSyntaxReferences[0].GetSyntax());

                    // Locations: Return the Span of that particular 
                    //            AnonymousObjectMemberDeclarator's IdentifierToken.
                    Assert.Equal(1, member.Locations.Length);
                    Assert.Equal(member.Locations[0], propertyInitializer.NameEquals.Name.GetLocation());

                    propIndex++;
                }
            }
        }

        [Fact]
        public void NamespaceDeclaringSyntax()
        {
            var text =
@"
namespace N1 {
    namespace N2 {
        namespace N3 {}
    }
}

namespace N1.N2 {
    namespace N3 {}
}

namespace System {}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var system = global.GetMembers("System").Single() as NamespaceSymbol;
            var n1 = global.GetMembers("N1").Single() as NamespaceSymbol;
            var n2 = n1.GetMembers("N2").Single() as NamespaceSymbol;
            var n3 = n2.GetMembers("N3").Single() as NamespaceSymbol;

            CheckDeclaringSyntaxNodes(comp, n2, 2);
            CheckDeclaringSyntaxNodes(comp, n3, 2);
            CheckDeclaringSyntaxNodes(comp, system, 1);

            // Can't use GetDeclaredSymbol for N1 or global.
            CheckDeclaringSyntaxNodesWithoutGetDeclaredSymbol(comp, n1, 2, SyntaxKind.NamespaceDeclaration);
            CheckDeclaringSyntaxNodesWithoutGetDeclaredSymbol(comp, global, 1, SyntaxKind.CompilationUnit);
        }

        [Fact]
        public void TypeParameterDeclaringSyntax()
        {
            var text =
@"
using System;
using System.Collections.Generic;

namespace N1 {
    class C1<T, U> {
        class C2<W> {
            public C1<int, string>.C2<W> f1;
            public void m<R, S>();
        }
        class C3<W> {
            IEnumerable<U> f2;
            Foo<Bar> f3;
        }
    }

    class M {
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var n1 = global.GetMembers("N1").Single() as NamespaceSymbol;
            var c1 = n1.GetTypeMembers("C1").Single() as NamedTypeSymbol;
            var c2 = c1.GetTypeMembers("C2").Single() as NamedTypeSymbol;
            var c3 = c1.GetTypeMembers("C3").Single() as NamedTypeSymbol;

            foreach (Symbol s in c1.TypeParameters)
            {
                CheckDeclaringSyntaxNodes(comp, s, 1);
            }

            foreach (FieldSymbol f in c2.GetMembers().OfType<FieldSymbol>())
            {
                foreach (TypeParameterSymbol tp in ((NamedTypeSymbol)f.Type.TypeSymbol).TypeParameters)
                {
                    CheckDeclaringSyntaxNodes(comp, tp, 1);
                }
            }

            foreach (MethodSymbol m in c2.GetMembers().OfType<MethodSymbol>())
            {
                foreach (TypeParameterSymbol tp in m.TypeParameters)
                {
                    CheckDeclaringSyntaxNodes(comp, tp, 1);
                }
            }

            foreach (FieldSymbol f in c3.GetMembers().OfType<FieldSymbol>())
            {
                foreach (TypeParameterSymbol tp in ((NamedTypeSymbol)f.Type.TypeSymbol).TypeParameters)
                {
                    CheckDeclaringSyntaxNodes(comp, tp, 0);
                }
            }
        }

        [Fact]
        public void MemberDeclaringSyntax()
        {
            var text =
@"
using System;
using System.Collections.Generic;

namespace N1 {
    enum E1 {Red, Blue = 5, Green };
    class C1<T> {
        C1<int> t, w, x;
        const int q = 4, r = 7;
        C1(int i) {}
        static C1() {}
        int m(T t, int y = 3) { return 3; }
        int P {get { return 0; } set {}}
        abstract int Prop2 {get; set; }
        int Prop3 {get; set; }
        string this[int i] {get { return ""; } set {}}
        abstract string this[int i, int j] {get; set;}
        event EventHandler ev1;
        event EventHandler ev2 { add {} remove {} }
    }
    class C2<U>
    {
         static int x = 7;
    }
}
";
            var comp = CreateCompilationWithMscorlib(text);
            var global = comp.GlobalNamespace;
            var n1 = global.GetMembers("N1").Single() as NamespaceSymbol;
            var c1 = n1.GetTypeMembers("C1").Single() as NamedTypeSymbol;
            var c2 = n1.GetTypeMembers("C2").Single() as NamedTypeSymbol;
            var e1 = n1.GetTypeMembers("E1").Single() as NamedTypeSymbol;

            foreach (Symbol memb in e1.GetMembers())
            {
                if (memb.Kind == SymbolKind.Method && ((MethodSymbol)memb).MethodKind == MethodKind.Constructor)
                    CheckDeclaringSyntaxNodesIncludingParameters(comp, memb, 0);  // implicit constructor
                else
                    CheckDeclaringSyntaxNodesIncludingParameters(comp, memb, 1);
            }

            var ev1 = c1.GetMembers("ev1").Single() as EventSymbol;
            var prop3 = c1.GetMembers("Prop3").Single() as PropertySymbol;

            foreach (Symbol memb in c1.GetMembers())
            {
                int expectedDeclaringNodes = 1;

                if (memb is MethodSymbol)
                {
                    MethodSymbol meth = (MethodSymbol)memb;
                    if (meth.AssociatedSymbol != null && meth.AssociatedSymbol.OriginalDefinition.Equals(ev1))
                        expectedDeclaringNodes = 0;  // implicit accessor.
                }
                if (memb is FieldSymbol)
                {
                    FieldSymbol fld = (FieldSymbol)memb;
                    if (fld.AssociatedSymbol != null && fld.AssociatedSymbol.OriginalDefinition.Equals(prop3))
                        expectedDeclaringNodes = 0;  // auto-prop backing field.
                }

                CheckDeclaringSyntaxNodesIncludingParameters(comp, memb, expectedDeclaringNodes);
            }

            var fieldT = c1.GetMembers("t").Single() as FieldSymbol;
            var constructedC1 = fieldT.Type.TypeSymbol;

            foreach (Symbol memb in constructedC1.GetMembers())
            {
                int expectedDeclaringNodes = 1;

                if (memb is MethodSymbol)
                {
                    MethodSymbol meth = (MethodSymbol)memb;
                    if (meth.AssociatedSymbol != null && meth.AssociatedSymbol.OriginalDefinition.Equals(ev1))
                        expectedDeclaringNodes = 0;  // implicit accessor.
                }
                if (memb is FieldSymbol)
                {
                    FieldSymbol fld = (FieldSymbol)memb;
                    if (fld.AssociatedSymbol != null && fld.AssociatedSymbol.OriginalDefinition.Equals(prop3))
                        expectedDeclaringNodes = 0;  // auto-prop backing field.
                }

                CheckDeclaringSyntaxNodesIncludingParameters(comp, memb, expectedDeclaringNodes);
            }

            foreach (Symbol memb in c2.GetMembers())
            {
                if (memb.Kind == SymbolKind.Method)
                    CheckDeclaringSyntaxNodesIncludingParameters(comp, memb, 0);
            }
        }

        [Fact]
        public void LocalDeclaringSyntax()
        {
            var text =
@"
using System;
using System.Collections.Generic;

class C1
{
    void m()
    {
        int loc1, loc2 = 4, loc3;
        const int loc4 = 6, loc5 = 7;
        using (IDisposable loc6 = foo()) {}
        for (int loc7 = 0; loc7 < 10; ++loc7) {}
        foreach (int loc8 in new int[] {1,3,4}) {}
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            CheckDeclaringSyntax<VariableDeclaratorSyntax>(comp, tree, "loc1", SymbolKind.Local);
            CheckDeclaringSyntax<VariableDeclaratorSyntax>(comp, tree, "loc2", SymbolKind.Local);
            CheckDeclaringSyntax<VariableDeclaratorSyntax>(comp, tree, "loc3", SymbolKind.Local);
            CheckDeclaringSyntax<VariableDeclaratorSyntax>(comp, tree, "loc4", SymbolKind.Local);
            CheckDeclaringSyntax<VariableDeclaratorSyntax>(comp, tree, "loc5", SymbolKind.Local);
            CheckDeclaringSyntax<VariableDeclaratorSyntax>(comp, tree, "loc6", SymbolKind.Local);
            CheckDeclaringSyntax<VariableDeclaratorSyntax>(comp, tree, "loc7", SymbolKind.Local);
            CheckDeclaringSyntax<ForEachStatementSyntax>(comp, tree, "loc8", SymbolKind.Local);
        }

        [Fact]
        public void LabelDeclaringSyntax()
        {
            var text =
@"
using System;
using System.Collections.Generic;

class C1
{
    void m(int i)
    {
        lab1: ;
        lab2: lab3: Console.WriteLine();
        switch (i) {
            case 4: case 3:
               break;
            default:
               break;
        }
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            CheckDeclaringSyntax<LabeledStatementSyntax>(comp, tree, "lab1", SymbolKind.Label);
            CheckDeclaringSyntax<LabeledStatementSyntax>(comp, tree, "lab2", SymbolKind.Label);
            CheckDeclaringSyntax<LabeledStatementSyntax>(comp, tree, "lab3", SymbolKind.Label);
            CheckDeclaringSyntax<SwitchLabelSyntax>(comp, tree, "case 4:", SymbolKind.Label);
            CheckDeclaringSyntax<SwitchLabelSyntax>(comp, tree, "case 3:", SymbolKind.Label);
            CheckDeclaringSyntax<SwitchLabelSyntax>(comp, tree, "default:", SymbolKind.Label);
        }


        [Fact]
        public void AliasDeclaringSyntax()
        {
            var text =
@"
using System;
using System.Collections.Generic;
using ConsoleAlias=System.Console;
using ListOfIntAlias=System.Collections.Generic.List<int>;

namespace N1
{
    using FooAlias=Con;
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            CheckDeclaringSyntax<UsingDirectiveSyntax>(comp, tree, "ConsoleAlias", SymbolKind.Alias);
            CheckDeclaringSyntax<UsingDirectiveSyntax>(comp, tree, "ListOfIntAlias", SymbolKind.Alias);
            CheckDeclaringSyntax<UsingDirectiveSyntax>(comp, tree, "FooAlias", SymbolKind.Alias);
        }

        [Fact]
        public void RangeVariableDeclaringSyntax()
        {
            var text =
@"
using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    void f()
    {
        IEnumerable<int> a = null;
        var y1 = from range1 in a let range2 = a.ToString() select range1 into range3 select range3 + 1;
        var y2 = from range4 in a join range5 in a on range4.ToString() equals range5.ToString() into range6 select range6;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            CheckDeclaringSyntax<QueryClauseSyntax>(comp, tree, "range1", SymbolKind.RangeVariable);
            CheckDeclaringSyntax<QueryClauseSyntax>(comp, tree, "range2", SymbolKind.RangeVariable);
            CheckDeclaringSyntax<QueryContinuationSyntax>(comp, tree, "range3", SymbolKind.RangeVariable);
            CheckDeclaringSyntax<QueryClauseSyntax>(comp, tree, "range4", SymbolKind.RangeVariable);
            CheckDeclaringSyntax<QueryClauseSyntax>(comp, tree, "range5", SymbolKind.RangeVariable);
            CheckDeclaringSyntax<JoinIntoClauseSyntax>(comp, tree, "range6", SymbolKind.RangeVariable);
        }

        [Fact]
        public void LambdaDeclaringSyntax()
        {
            var text =
@"
using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    void f()
    {
        Func<int, int, int> f1 = (a,b) => /*1*/ a + b;
        Func<int, int, int> f1 = a => /*2*/ a + 1;
        Func<int, int, int> f1 = delegate(int a, int b) /*3*/ { return a + b; };
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            CheckLambdaDeclaringSyntax<ParenthesizedLambdaExpressionSyntax>(comp, tree, "/*1*/");
            CheckLambdaDeclaringSyntax<SimpleLambdaExpressionSyntax>(comp, tree, "/*2*/");
            CheckLambdaDeclaringSyntax<AnonymousMethodExpressionSyntax>(comp, tree, "/*3*/");
        }
    }
}
