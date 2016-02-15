// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class DocumentationCommentIDTests : CSharpTestBase
    {
        [Fact]
        public void AliasSymbol()
        {
            var source = @"
using A = System.String;

class C { }
";
            var comp = CreateCompilationWithMscorlib(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Single();
            var symbol = model.GetDeclaredSymbol(syntax);
            Assert.Equal(SymbolKind.Alias, symbol.Kind);
            Assert.Null(symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void ArrayTypeSymbol()
        {
            var source = @"
class C
{
    C[] f;
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var field = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<FieldSymbol>("f");
            var symbol = field.Type.TypeSymbol;
            Assert.Equal(SymbolKind.ArrayType, symbol.Kind);
            Assert.Null(symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void AssemblySymbol()
        {
            var source = @"
class C
{
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var symbol = comp.SourceAssembly;
            Assert.Equal(SymbolKind.Assembly, symbol.Kind);
            Assert.Null(symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void DynamicTypeSymbol()
        {
            var source = @"
class C
{
    dynamic f;
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var field = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<FieldSymbol>("f");
            var symbol = field.Type.TypeSymbol;
            Assert.Equal(SymbolKind.DynamicType, symbol.Kind);
            Assert.Null(symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void ErrorTypeSymbol()
        {
            var source = @"
class C : M.N.O
{
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var symbol = type.BaseType;
            Assert.Equal(SymbolKind.ErrorType, symbol.Kind);
            Assert.Equal("!:M.N.O", symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void EventSymbol()
        {
            var source = @"
class C
{
    event System.Action E;
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var symbol = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<EventSymbol>("E");
            Assert.Equal(SymbolKind.Event, symbol.Kind);
            Assert.Equal("E:C.E", symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void FieldSymbol()
        {
            var source = @"
class C
{
    int f;
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var symbol = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<FieldSymbol>("f");
            Assert.Equal(SymbolKind.Field, symbol.Kind);
            Assert.Equal("F:C.f", symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void LabelSymbol()
        {
            var source = @"
class C
{
    void M()
    {
      LABEL:
        goto LABEL;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<GotoStatementSyntax>().Single().Expression;
            var symbol = model.GetSymbolInfo(syntax).Symbol;
            Assert.Equal(SymbolKind.Label, symbol.Kind);
            Assert.Null(symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void LocalSymbol()
        {
            var source = @"
class C
{
    void M()
    {
        int x;
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var symbol = model.GetDeclaredSymbol(syntax);
            Assert.Equal(SymbolKind.Local, symbol.Kind);
            Assert.Null(symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void MethodSymbol()
        {
            var source = @"
class C
{
    void M()
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var symbol = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M");
            Assert.Equal(SymbolKind.Method, symbol.Kind);
            Assert.Equal("M:C.M", symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void NetModuleSymbol()
        {
            var source = @"
class C
{
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var symbol = comp.SourceModule;
            Assert.Equal(SymbolKind.NetModule, symbol.Kind);
            Assert.Null(symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void NamedTypeSymbol()
        {
            var source = @"
class C
{
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var symbol = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            Assert.Equal(SymbolKind.NamedType, symbol.Kind);
            Assert.Equal("T:C", symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void NamespaceSymbol()
        {
            var source = @"
class C
{
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var symbol = comp.GlobalNamespace.GetMember<NamespaceSymbol>("System");
            Assert.Equal(SymbolKind.Namespace, symbol.Kind);
            Assert.Equal("N:System", symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void GlobalNamespaceSymbol()
        {
            var source = @"
class C
{
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var symbol = comp.GlobalNamespace;
            Assert.Equal(SymbolKind.Namespace, symbol.Kind);
            Assert.Null(symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void ParameterSymbol()
        {
            var source = @"
class C
{
    void M(int x)
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var symbol = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M").Parameters.Single();
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
            Assert.Null(symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void PointerTypeSymbol()
        {
            var source = @"
class C
{
    unsafe int* f;
}
";
            var comp = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll);
            var field = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<FieldSymbol>("f");
            var symbol = field.Type.TypeSymbol;
            Assert.Equal(SymbolKind.PointerType, symbol.Kind);
            Assert.Null(symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void PropertySymbol()
        {
            var source = @"
class C
{
    int P { get; set; }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var symbol = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<PropertySymbol>("P");
            Assert.Equal(SymbolKind.Property, symbol.Kind);
            Assert.Equal("P:C.P", symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void RangeVariableSymbol()
        {
            var source = @"
using System.Linq;

class C
{
    void M()
    {
        var q = from x in p select x;
    }
}
";
            var comp = CreateCompilationWithMscorlibAndSystemCore(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var syntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<FromClauseSyntax>().Single();
            var symbol = model.GetDeclaredSymbol(syntax);
            Assert.Equal(SymbolKind.RangeVariable, symbol.Kind);
            Assert.Null(symbol.GetDocumentationCommentId());
        }

        [Fact]
        public void TypeParameterSymbol()
        {
            var source = @"
class C<T>
{
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var symbol = type.TypeParameters.Single();
            Assert.Equal(SymbolKind.TypeParameter, symbol.Kind);
            Assert.Equal("!:T", symbol.GetDocumentationCommentId());
        }

        [WorkItem(531409, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531409")]
        [Fact]
        public void ExplicitInterfaceImplementation()
        {
            var source = @"
interface I<T>
{
    void M();
    int P { get; set; }
    event System.Action E;
}

class C<T> : I<T>
{
    void I<T>.M() { }
    int I<T>.P { get { return 0; } set { } }
    event System.Action I<T>.E { add { } remove { } }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var method = type.GetMembersUnordered().OfType<MethodSymbol>().Single(m => m.MethodKind == MethodKind.ExplicitInterfaceImplementation);
            var property = type.GetMembersUnordered().OfType<PropertySymbol>().Single();
            var @event = type.GetMembersUnordered().OfType<EventSymbol>().Single();
            Assert.Equal("M:C`1.I{T}#M", method.GetDocumentationCommentId());
            Assert.Equal("P:C`1.I{T}#P", property.GetDocumentationCommentId());
            Assert.Equal("E:C`1.I{T}#E", @event.GetDocumentationCommentId());
        }

        [Fact]
        public void ArgList()
        {
            var source = @"
class C
{
    void M1(__arglist) { }
    void M2(int x, __arglist) { }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var method1 = type.GetMember<MethodSymbol>("M1");
            var method2 = type.GetMember<MethodSymbol>("M2");
            Assert.Equal("M:C.M1()", method1.GetDocumentationCommentId());
            Assert.Equal("M:C.M2(System.Int32,)", method2.GetDocumentationCommentId());
        }

        [WorkItem(547163, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547163")]
        [Fact]
        public void NestedGenericTypes()
        {
            var source = @"
class A<TA1, TA2>
{
    class B<TB1, TB2>
    {
        class C<TC1, TC2>
        {
            void M<TM1, TM2>(TA1 a1, TA2 a2, TB1 b1, TB2 b2, TC1 c1, TC2 c2, TM1 m1, TM2 m2)
            {
            }
        }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var method = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<NamedTypeSymbol>("B").GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M");
            Assert.Equal("M:A`2.B`2.C`2.M``2(`0,`1,`2,`3,`4,`5,``0,``1)", method.GetDocumentationCommentId());
        }
    }
}
