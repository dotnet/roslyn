' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class BindingsTests
        Private Function GetBinderFromNode(compilation As VisualBasicCompilation,
                                             semanticModel As SemanticModel,
                                             treeName As String,
                                             textToFind As String) As Binder
            Dim tree As SyntaxTree = CompilationUtils.GetTree(compilation, treeName)
            Dim position As Integer = CompilationUtils.FindPositionFromText(tree, textToFind)

            Dim binder = DirectCast(semanticModel, VBSemanticModel).GetEnclosingBinder(position)
            Assert.True(binder.IsSemanticModelBinder)
            Return If(TypeOf binder Is SemanticModelBinder, binder.ContainingBinder, binder) ' Tests are expecting specific runtime types, so strip off SemanticModelBinder.
        End Function

        ' Go up the tree until a TypeSyntax node is found. Find the root of that type syntax.
        Private Function FindTypeSyntax(token As SyntaxToken) As TypeSyntax
            Dim node = token.Parent

            While Not (TypeOf node Is TypeSyntax)
                node = node.Parent
            End While

            While (TypeOf node.Parent Is TypeSyntax)
                node = node.Parent
            End While

            Return DirectCast(node, TypeSyntax)
        End Function

        <Fact(), WorkItem(546400, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546400")>
        Public Sub TestGetEnclosingBinder()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
      <compilation name="Compilation">
          <file name="a.vb">
        ' top of file
        Option Strict On

        Imports System.Collections

        Namespace N1
            Class C1
                Private field1 as Integer
                Private Function meth1(Of TParam1, TParam2)(x as TParam1) As Generic.IEnumerable(Of TParam2)
                End Function
                Custom Event e As System.EventHandler
                    AddHandler(e as System.EventHandler)
                        dim a as System.DateTime
                    End AddHandler
                End Event
            End Class

            Namespace N2
                Partial Class C2
                    Private field2 as String
                End Class

                public class Q
                    private field4 as integer
                public class Q

            End Namespace

            ' outside class

        End Namespace

    </file>
          <file name="b.vb">
        Option Strict Off

        Namespace Global.N3
            Namespace N4
                ' inside N4
            End Namespace

            ' inside N3
        End Namespace

        Namespace N1.N2
            Partial Class C2
                Private field3 as Integer
            End Class
            Public Interface Q
                sub method1() as integer
            end interface
        End Namespace

        Namespace Global
            Namespace N7
                'inside N7
            End Namespace
            'inside Global
        End Namespace
    </file>
      </compilation>, options:=TestOptions.ReleaseExe.WithRootNamespace("Foo.Bar"))

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            Dim treeB = CompilationUtils.GetTree(compilation, "b.vb")
            Dim bindingsB = compilation.GetSemanticModel(treeB)

            Dim context As Binder, typeContext As NamedTypeBinder, nsContext As NamespaceBinder

            context = GetBinderFromNode(compilation, bindingsA, "a.vb", "field1")
            Assert.IsType(GetType(NamedTypeBinder), context)
            typeContext = DirectCast(context, NamedTypeBinder)
            Assert.Equal("Foo.Bar.N1.C1", typeContext.ContainingNamespaceOrType.ToTestDisplayString())

            context = GetBinderFromNode(compilation, bindingsA, "a.vb", "field1")
            Assert.IsType(GetType(NamedTypeBinder), context)
            typeContext = DirectCast(context, NamedTypeBinder)
            Assert.Equal("Foo.Bar.N1.C1", typeContext.ContainingNamespaceOrType.ToTestDisplayString())

            context = GetBinderFromNode(compilation, bindingsA, "a.vb", "outside class")
            Assert.IsType(GetType(NamespaceBinder), context)
            nsContext = DirectCast(context, NamespaceBinder)
            Assert.Equal("Foo.Bar.N1", nsContext.ContainingNamespaceOrType.ToTestDisplayString())

            context = GetBinderFromNode(compilation, bindingsA, "a.vb", "top of file")
            Assert.IsType(GetType(NamespaceBinder), context)
            nsContext = DirectCast(context, NamespaceBinder)
            Assert.Equal("Foo.Bar", nsContext.ContainingNamespaceOrType.ToTestDisplayString())

            context = GetBinderFromNode(compilation, bindingsB, "b.vb", "field3")
            Assert.IsType(GetType(NamedTypeBinder), context)
            typeContext = DirectCast(context, NamedTypeBinder)
            Assert.Equal("Foo.Bar.N1.N2.C2", typeContext.ContainingNamespaceOrType.ToTestDisplayString())
            Assert.Equal(OptionStrict.Off, context.OptionStrict)

            context = GetBinderFromNode(compilation, bindingsB, "b.vb", "inside N3")
            Assert.IsType(GetType(NamespaceBinder), context)
            nsContext = DirectCast(context, NamespaceBinder)
            Assert.Equal("N3", nsContext.ContainingNamespaceOrType.ToTestDisplayString())
            Assert.Equal(OptionStrict.Off, context.OptionStrict)

            context = GetBinderFromNode(compilation, bindingsB, "b.vb", "inside N4")
            Assert.IsType(GetType(NamespaceBinder), context)
            nsContext = DirectCast(context, NamespaceBinder)
            Assert.Equal("N3.N4", nsContext.ContainingNamespaceOrType.ToTestDisplayString())
            Assert.Equal(OptionStrict.Off, context.OptionStrict)

            context = GetBinderFromNode(compilation, bindingsB, "b.vb", "inside N7")
            Assert.IsType(GetType(NamespaceBinder), context)
            nsContext = DirectCast(context, NamespaceBinder)
            Assert.Equal("N7", nsContext.ContainingNamespaceOrType.ToTestDisplayString())
            Assert.Equal(OptionStrict.Off, context.OptionStrict)

            context = GetBinderFromNode(compilation, bindingsB, "b.vb", "inside Global")
            Assert.IsType(GetType(NamespaceBinder), context)
            nsContext = DirectCast(context, NamespaceBinder)
            Assert.Equal("Global", nsContext.ContainingNamespaceOrType.ToTestDisplayString())
            Assert.Equal(OptionStrict.Off, context.OptionStrict)

            context = GetBinderFromNode(compilation, bindingsA, "a.vb", "field2")
            Assert.IsType(GetType(NamedTypeBinder), context)
            typeContext = DirectCast(context, NamedTypeBinder)
            Assert.Equal("Foo.Bar.N1.N2.C2", typeContext.ContainingNamespaceOrType.ToTestDisplayString())
            Assert.Equal(OptionStrict.On, context.OptionStrict)

            context = GetBinderFromNode(compilation, bindingsA, "a.vb", "field4")
            Assert.IsType(GetType(NamedTypeBinder), context)
            typeContext = DirectCast(context, NamedTypeBinder)
            Assert.Equal("Foo.Bar.N1.N2.Q", typeContext.ContainingNamespaceOrType.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, DirectCast(typeContext.ContainingNamespaceOrType, NamedTypeSymbol).TypeKind)
            Assert.Equal(OptionStrict.On, context.OptionStrict)

            context = GetBinderFromNode(compilation, bindingsB, "b.vb", "method1")
            Assert.IsType(GetType(NamedTypeBinder), context)
            typeContext = DirectCast(context, NamedTypeBinder)
            Assert.Equal("Foo.Bar.N1.N2.Q", typeContext.ContainingNamespaceOrType.ToTestDisplayString())
            Assert.Equal(TypeKind.Interface, DirectCast(typeContext.ContainingNamespaceOrType, NamedTypeSymbol).TypeKind)
            Assert.Equal(OptionStrict.Off, context.OptionStrict)

            context = GetBinderFromNode(compilation, bindingsA, "a.vb", "System.Collections")
            Assert.IsType(GetType(LocationSpecificBinder), context)
            Assert.IsType(GetType(IgnoreBaseClassesBinder), context.ContainingBinder)
            Assert.Equal(OptionStrict.On, context.OptionStrict)

            context = GetBinderFromNode(compilation, bindingsA, "a.vb", "System.DateTime")
            Assert.IsType(GetType(StatementListBinder), context)
            Dim stListContext = DirectCast(context, StatementListBinder)
            Assert.Equal("Foo.Bar.N1.C1", stListContext.ContainingNamespaceOrType.ToTestDisplayString())

            context = GetBinderFromNode(compilation, bindingsA, "a.vb", "Generic.IEnumerable(Of TParam2)")
            Assert.IsType(GetType(MethodTypeParametersBinder), context)
        End Sub

        ' Test case where method isn't enclosed in a class.
        <Fact>
        Public Sub GetEnclosingBinderForMembersInsideNamespace()
            Dim options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace("Foo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="Compilation">
    <file name="a.vb">
        ' top of file
        Option Strict Off

        Imports System.Collections

        Namespace N1
            Function MyMethod() As Integer
                Console.WriteLine("hello")
            End Function

            Dim MyField As String = "MyFieldInitializer"

            Property MyProperty As String = "MyPropertyInitializer"

            WithEvents MyWithEvent As String = "MyWithEventInitializer"
        End Namespace
    </file>
</compilation>, options)

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            Dim context As Binder

            context = GetBinderFromNode(compilation, bindingsA, "a.vb", "MyMethod")
            Assert.IsType(GetType(NamedTypeBinder), context)
            Dim implicitTypeContext = DirectCast(context, NamedTypeBinder)
            Assert.True(DirectCast(implicitTypeContext.ContainingNamespaceOrType, NamedTypeSymbol).IsImplicitClass)
            Assert.Equal("Foo.Bar.N1.<invalid-global-code>", implicitTypeContext.ContainingNamespaceOrType.ToTestDisplayString())
            Assert.Equal(OptionStrict.Off, context.OptionStrict)

            context = GetBinderFromNode(compilation, bindingsA, "a.vb", "WriteLine")
            Assert.IsType(GetType(StatementListBinder), context)
            Dim statementBinder = DirectCast(context, StatementListBinder)
            Assert.Equal("Foo.Bar.N1.<invalid-global-code>", statementBinder.ContainingNamespaceOrType.ToTestDisplayString())
            Assert.Equal(OptionStrict.Off, context.OptionStrict)

            context = GetBinderFromNode(compilation, bindingsA, "a.vb", "MyFieldInitializer")
            Assert.IsType(GetType(DeclarationInitializerBinder), context)
            Dim fInitBinder = DirectCast(context, DeclarationInitializerBinder)
            Assert.Equal("Foo.Bar.N1.<invalid-global-code>", fInitBinder.ContainingNamespaceOrType.ToTestDisplayString())
            Assert.Equal(OptionStrict.Off, context.OptionStrict)
            Assert.Same(implicitTypeContext, context.ContainingBinder)

            context = GetBinderFromNode(compilation, bindingsA, "a.vb", "MyPropertyInitializer")
            Assert.IsType(GetType(DeclarationInitializerBinder), context)
            Dim pInitBinder = DirectCast(context, DeclarationInitializerBinder)
            Assert.Equal("Foo.Bar.N1.<invalid-global-code>", pInitBinder.ContainingNamespaceOrType.ToTestDisplayString())
            Assert.Equal(OptionStrict.Off, context.OptionStrict)
            Assert.Same(implicitTypeContext, context.ContainingBinder)

            context = GetBinderFromNode(compilation, bindingsA, "a.vb", "MyWithEventInitializer")
            Assert.IsType(GetType(DeclarationInitializerBinder), context)
            Dim weInitBinder = DirectCast(context, DeclarationInitializerBinder)
            Assert.Equal("Foo.Bar.N1.<invalid-global-code>", weInitBinder.ContainingNamespaceOrType.ToTestDisplayString())
            Assert.Equal(OptionStrict.Off, context.OptionStrict)
            Assert.Same(implicitTypeContext, context.ContainingBinder)
        End Sub

        <Fact>
        Public Sub TestGetTypeFromDeclaration()
            Dim options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace("Foo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
      <compilation name="Compilation">
          <file name="a.vb">
        ' top of file
        Option Strict On

        Imports System.Collections

        Namespace N1
            Class C1
            End Class

            Namespace N2
                Partial Class C2
                End Class

                public partial class Q'first
                end class
                public class Q'second
                end class
                public structure Q'third
                end structure
                public class Q(Of T)
                end class
 
            End Namespace
        End Namespace

    </file>
          <file name="b.vb">
        Option Strict Off

        Namespace N1.N2
            Partial Class C2
            End Class
            Public Interface Q
            end interface
        End Namespace

        Namespace Global.N1.N2
            Public Class RRR
            End Class
        End Namespace
    </file>

      </compilation>, options)

            Dim expectedErrors = <errors>
BC30179: class 'Q' and structure 'Q' conflict in namespace 'Foo.Bar.N1.N2'.
                public partial class Q'first
                                     ~
BC30179: class 'Q' and structure 'Q' conflict in namespace 'Foo.Bar.N1.N2'.
                public class Q'second
                             ~
BC30179: structure 'Q' and class 'Q' conflict in namespace 'Foo.Bar.N1.N2'.
                public structure Q'third
                                 ~
BC30179: interface 'Q' and class 'Q' conflict in namespace 'Foo.Bar.N1.N2'.
            Public Interface Q
                             ~   
    </errors>

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            Dim treeB = CompilationUtils.GetTree(compilation, "b.vb")
            Dim bindingsB = compilation.GetSemanticModel(treeB)

            Dim typeSymbol, typeSymbol2, typeSymbol3, typeSymbol4, typeSymbol5, typeSymbol6 As INamedTypeSymbol

            typeSymbol = CompilationUtils.GetTypeSymbol(compilation, bindingsA, "a.vb", "C1")
            Assert.NotNull(typeSymbol)
            Assert.Equal("Foo.Bar.N1.C1", typeSymbol.ToTestDisplayString())

            typeSymbol = CompilationUtils.GetTypeSymbol(compilation, bindingsA, "a.vb", "C2")
            Assert.NotNull(typeSymbol)
            Assert.Equal("Foo.Bar.N1.N2.C2", typeSymbol.ToTestDisplayString())

            typeSymbol2 = CompilationUtils.GetTypeSymbol(compilation, bindingsB, "b.vb", "C2")
            Assert.NotNull(typeSymbol2)
            Assert.Equal("Foo.Bar.N1.N2.C2", typeSymbol2.ToTestDisplayString())
            Assert.Equal(typeSymbol, typeSymbol2)

            typeSymbol = CompilationUtils.GetTypeSymbol(compilation, bindingsA, "a.vb", "Q'first")
            Assert.NotNull(typeSymbol)
            Assert.Equal("Foo.Bar.N1.N2.Q", typeSymbol.ToTestDisplayString())
            Assert.Equal(0, typeSymbol.Arity)
            Assert.Equal(TypeKind.Class, typeSymbol.TypeKind)

            typeSymbol2 = CompilationUtils.GetTypeSymbol(compilation, bindingsA, "a.vb", "Q'second")
            Assert.NotNull(typeSymbol2)
            Assert.Equal("Foo.Bar.N1.N2.Q", typeSymbol2.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, typeSymbol2.TypeKind)
            Assert.Equal(0, typeSymbol2.Arity)
            Assert.Equal(typeSymbol, typeSymbol2)

            typeSymbol3 = CompilationUtils.GetTypeSymbol(compilation, bindingsA, "a.vb", "Q'third")
            Assert.NotNull(typeSymbol3)
            Assert.Equal("Foo.Bar.N1.N2.Q", typeSymbol3.ToTestDisplayString())
            Assert.Equal(TypeKind.Structure, typeSymbol3.TypeKind)
            Assert.Equal(0, typeSymbol3.Arity)
            Assert.NotEqual(typeSymbol, typeSymbol3)
            Assert.NotEqual(typeSymbol2, typeSymbol3)

            typeSymbol4 = CompilationUtils.GetTypeSymbol(compilation, bindingsB, "b.vb", "Q")
            Assert.NotNull(typeSymbol4)
            Assert.Equal("Foo.Bar.N1.N2.Q", typeSymbol4.ToTestDisplayString())
            Assert.Equal(TypeKind.Interface, typeSymbol4.TypeKind)
            Assert.Equal(0, typeSymbol4.Arity)
            Assert.NotEqual(typeSymbol4, typeSymbol3)
            Assert.NotEqual(typeSymbol4, typeSymbol2)
            Assert.NotEqual(typeSymbol4, typeSymbol)

            typeSymbol5 = CompilationUtils.GetTypeSymbol(compilation, bindingsA, "a.vb", "Q(Of T)")
            Assert.NotNull(typeSymbol5)
            Assert.Equal("Foo.Bar.N1.N2.Q(Of T)", typeSymbol5.ToTestDisplayString())
            Assert.Equal(TypeKind.Class, typeSymbol5.TypeKind)
            Assert.Equal(1, typeSymbol5.Arity)
            Assert.NotEqual(typeSymbol5, typeSymbol4)
            Assert.NotEqual(typeSymbol5, typeSymbol3)
            Assert.NotEqual(typeSymbol5, typeSymbol2)
            Assert.NotEqual(typeSymbol5, typeSymbol)

            typeSymbol6 = CompilationUtils.GetTypeSymbol(compilation, bindingsB, "b.vb", "RRR")
            Assert.NotNull(typeSymbol6)
            Assert.Equal("N1.N2.RRR", typeSymbol6.ToTestDisplayString())
            Assert.Equal(0, typeSymbol6.Arity)
            Assert.Equal(TypeKind.Class, typeSymbol6.TypeKind)

            CompilationUtils.AssertTheseDeclarationDiagnostics(compilation, expectedErrors)
        End Sub


        <Fact>
        Public Sub TestTypeBinding()
            Dim options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace("Foo.Bar")

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
      <compilation name="Compilation">
          <file name="a.vb">
        ' top of file
        Option Strict On

        Imports System.Collections
        Imports Foo.bar.N1.N2.Orange
        Imports foo.    Bar.  n1. n2.  yellow%(Of Integer)

        Namespace N1
            Class Class1
                Function blah(arg1 as N2.IAmbig) As String
                    dim a as N2.Yellow
                    return DirectCast("", String)
                End Function

                Private Function meth1(Of TParam1, TParam2)(x as TParam1) As Generic.IEnumerable(Of TParam2)
                End Function

                Public k As Elvis
        End Namespace

    </file>
          <file name="b.vb">
        Option Strict Off

        Namespace N1.N2
            Class Orange
            End Class
            Class Yellow(Of T)
            End Class

            Module M1
                Interface IAmbig
                End Interface
            End Module

            Module M2
                Interface IAmbig
                End Interface
            End Module
        End Namespace
    </file>
      </compilation>, options)

            Dim symbols As ImmutableArray(Of ISymbol)
            Dim symbol As Symbol
            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            Dim treeB = CompilationUtils.GetTree(compilation, "b.vb")
            Dim bindingsB = compilation.GetSemanticModel(treeB)

            ' Bind "System.Collections" in "Imports System.Collections". It binds to a namespace,
            ' not a type.
            Dim importSystemCollectionsTypeSyntax = FindTypeSyntax(CompilationUtils.FindTokenFromText(treeA, "System.Collections"))

            Dim sysCollectionsSymInfo = bindingsA.GetSpeculativeSymbolInfo(importSystemCollectionsTypeSyntax.SpanStart, importSystemCollectionsTypeSyntax, SpeculativeBindingOption.BindAsTypeOrNamespace)
            Dim sysCollectionsType = TryCast(sysCollectionsSymInfo.Symbol, TypeSymbol)
            If sysCollectionsType IsNot Nothing Then
                Assert.Equal(TypeKind.Error, sysCollectionsType.TypeKind)
            End If

            ' Bind "Foo.Bar.N1.N2.Orange" in "Imports Foo.Bar.N1.N2.Orange". It binds fine.
            Dim importsOrangeTypeSyntax = FindTypeSyntax(CompilationUtils.FindTokenFromText(treeA, "N2.Orange"))
            Dim importsOrangeSymInfo = bindingsA.GetSemanticInfoSummary(importsOrangeTypeSyntax)
            Assert.Equal(TypeKind.Class, importsOrangeSymInfo.Type.TypeKind)
            symbol = importsOrangeSymInfo.Symbol
            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.NamedType, symbol.Kind)
            Assert.Same(importsOrangeSymInfo.Type, symbol)
            Assert.Equal("Foo.Bar.N1.N2.Orange", symbol.ToTestDisplayString())

            ' Bind "foo.    Bar.  n1. n2.  yellow%" in "Imports foo.    Bar.  n1. n2.  yellow%". It binds fine but has an 
            ' error.
            Dim importsYellowTypeSyntax = FindTypeSyntax(CompilationUtils.FindTokenFromText(treeA, "yellow%"))
            Dim importsYellowSymInfo = bindingsA.GetSemanticInfoSummary(importsYellowTypeSyntax)
            Assert.Equal(TypeKind.Class, importsYellowSymInfo.Type.TypeKind)
            symbol = importsYellowSymInfo.Symbol
            Assert.NotNull(symbol)
            Assert.Equal(SymbolKind.NamedType, symbol.Kind)
            Assert.Equal(Of ISymbol)(importsYellowSymInfo.Type, symbol)
            Assert.Equal("Foo.Bar.N1.N2.Yellow(Of System.Int32)", symbol.ToDisplayString(SymbolDisplayFormat.TestFormat))

            ' Bind "N2.IAmbig" in "arg1 as N2.IAmbig". It is ambiguous.
            Dim interfaceIAmbigTypeSyntax = FindTypeSyntax(CompilationUtils.FindTokenFromText(treeA, "N2.IAmbig"))
            Dim interfaceIAmbigSymInfo = bindingsA.GetSemanticInfoSummary(interfaceIAmbigTypeSyntax)
            If interfaceIAmbigSymInfo.Type IsNot Nothing Then
                ' semantic info has an error type in it.
                Assert.Equal(TypeKind.Error, interfaceIAmbigSymInfo.Type.TypeKind)
            End If
            Dim sortedSymbols(interfaceIAmbigSymInfo.CandidateSymbols.Length - 1) As symbol
            interfaceIAmbigSymInfo.CandidateSymbols.CopyTo(sortedSymbols)
            Array.Sort(sortedSymbols, Function(sym1, sym2) sym1.ToTestDisplayString().CompareTo(sym2.ToTestDisplayString()))
            Assert.Equal(2, sortedSymbols.Count)
            Assert.Equal(CandidateReason.Ambiguous, interfaceIAmbigSymInfo.CandidateReason)
            Assert.Equal(SymbolKind.NamedType, sortedSymbols(0).Kind)
            Assert.Equal(TypeKind.Interface, DirectCast(sortedSymbols(0), NamedTypeSymbol).TypeKind)
            Assert.Equal("Foo.Bar.N1.N2.M1.IAmbig", sortedSymbols(0).ToTestDisplayString())
            Assert.Equal(SymbolKind.NamedType, sortedSymbols(1).Kind)
            Assert.Equal(TypeKind.Interface, DirectCast(sortedSymbols(1), NamedTypeSymbol).TypeKind)
            Assert.Equal("Foo.Bar.N1.N2.M2.IAmbig", sortedSymbols(1).ToTestDisplayString())

            ' Bind "N2.Yellow" in "Dim a As N2.Yellow". It has the wrong arity.
            Dim classYellowTypeSyntax = FindTypeSyntax(CompilationUtils.FindTokenFromText(treeA, "Yellow"))
            Dim classYellowSymInfo = bindingsA.GetSemanticInfoSummary(classYellowTypeSyntax)
            If classYellowSymInfo.Type IsNot Nothing Then
                Assert.Equal(TypeKind.Class, classYellowSymInfo.Type.TypeKind)
            End If
            symbols = classYellowSymInfo.CandidateSymbols
            Assert.Equal(1, symbols.Length)
            Assert.Equal(CandidateReason.WrongArity, classYellowSymInfo.CandidateReason)
            Assert.Equal(SymbolKind.NamedType, symbols(0).Kind)
            Assert.Equal(TypeKind.Class, DirectCast(symbols(0), NamedTypeSymbol).TypeKind)
            Assert.Equal("Foo.Bar.N1.N2.Yellow(Of T)", symbols(0).ToTestDisplayString())

            ' Bind "Elvis" in "Public k as Elvis". It doesn't exist at all.
            Dim elvisTypeSyntax = FindTypeSyntax(CompilationUtils.FindTokenFromText(treeA, "Elvis"))
            Dim elvisSymInfo = bindingsA.GetSemanticInfoSummary(elvisTypeSyntax)

            Assert.NotNull(elvisSymInfo.Type)
            Assert.Equal(TypeKind.Error, elvisSymInfo.Type.TypeKind)
            Assert.Null(elvisSymInfo.Symbol)
            Assert.Equal(0, elvisSymInfo.CandidateSymbols.Length)

            ' Bind Generic.IEnumerable(Of TParam2) in meth1.
            Dim iEnumSyntax = FindTypeSyntax(CompilationUtils.FindTokenFromText(treeA, "Generic.IEnumerable(Of TParam2)"))
            Dim iEnumSymInfo = bindingsA.GetSemanticInfoSummary(iEnumSyntax)
            Assert.Equal(TypeKind.Interface, iEnumSymInfo.Type.TypeKind)
            Assert.Equal("System.Collections.Generic.IEnumerable(Of TParam2)", iEnumSymInfo.Type.ToTestDisplayString())
        End Sub

        <Fact>
        Public Sub TestTypeBinding2()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
      <compilation name="Compilation">
          <file name="a.vb">
        ' top of file
    Public Class C
        Inherits B
    End Class

    Public Class A
        Public Class B
        End Class
    End Class

    Public Class B
        Inherits A
    End Class
    </file>
      </compilation>)

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            ' Find "Class C".
            Dim typeC = CompilationUtils.GetTypeSymbol(compilation, bindingsA, "a.vb", "C")
            Dim cBase = typeC.BaseType

            ' Bind "B" in "Inherits B".  It binds to the top-level type.
            Dim bSyntax = FindTypeSyntax(CompilationUtils.FindTokenFromText(treeA, "B"))
            Dim bLookup = bindingsA.GetSemanticInfoSummary(bSyntax)
            Dim typeB = bLookup.Type
            Assert.Equal(cBase, typeB) ' check that the Bindings API returned the actual base type.
        End Sub

        <Fact>
        Public Sub TestTypeBinding3()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
      <compilation name="Compilation">
          <file name="a.vb">
        ' top of file
Public Class C(Of T)
    Inherits A(Of T)
End Class

Public Class A(Of T)
    Inherits B
End Class

Public Class B
    Public Class T
    End Class
End Class
    </file>
      </compilation>)

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim bindingsA = compilation.GetSemanticModel(treeA)

            ' Find "Class C".
            Dim typeC = CompilationUtils.GetTypeSymbol(compilation, bindingsA, "a.vb", "C")
            Dim cBase1 = typeC.BaseType

            ' Bind "A(Of T)" in "Inherits A(Of T)".  It binds to the base clause.
            Dim baseSyntax = FindTypeSyntax(CompilationUtils.FindTokenFromText(treeA, "A(Of T)"))
            Dim cBaseLookup = bindingsA.GetSemanticInfoSummary(baseSyntax)
            Dim cBase2 = cBaseLookup.Type
            Assert.Equal(cBase1, cBase2) ' check that the Bindings API returned the actual base type.
        End Sub

        <WorkItem(538878, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538878")>
        <Fact>
        Public Sub TestTypeBinding4()
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
      <compilation name="Compilation">
          <file name="a.vb">
Class A
    Class B
    End Class
End Class

Class D
    Inherits A
    Protected Class B
    End Class
End Class

Class E
    Inherits D
    Protected Class F
        Inherits B
    End Class
End Class
    </file>
      </compilation>)

            Dim ef = compilation.GetTypeByMetadataName("E+F")
            Dim db = compilation.GetTypeByMetadataName("D+B")
            Assert.Same(db, ef.BaseType)
        End Sub

        <WorkItem(539968, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539968")>
        <Fact>
        Public Sub BindingInaccessibleType()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="BindingInaccessibleType">
        <file name="a.vb">
Class A
    Private Class D
    End Class
End Class
 
Class C
    Shared Sub M()
        Dim x As A.D
    End Sub
End Class
    </file>
    </compilation>)

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim a_d = treeA.FindNodeOrTokenByKind(SyntaxKind.QualifiedName)

            Dim model = compilation.GetSemanticModel(treeA)

            Dim info = model.GetSemanticInfoSummary(CType(a_d.AsNode(), ExpressionSyntax))

            Assert.Null(info.Symbol)
            Assert.Equal(1, info.CandidateSymbols.Length)
            Assert.Equal(CandidateReason.Inaccessible, info.CandidateReason)
            Dim symbol = info.CandidateSymbols(0)

            Dim type = info.Type
            Assert.NotNull(type)
            Assert.Equal(type, symbol)
            Assert.Equal("A.D", type.ToDisplayString())
            Assert.Equal(SymbolKind.NamedType, info.Type.Kind)
            Assert.Equal(TypeKind.Class, info.Type.TypeKind)
        End Sub

        <WorkItem(539968, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539968")>
        <Fact>
        Public Sub InstantiatingNamespace()

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
    <compilation name="InstantiatingNamespace">
        <file name="a.vb">
Namespace A
    Namespace D
        Class B
        End Class
    End Namespace
End Namespace
 
Class C
    Shared Sub M()
        Dim x As A.D
    End Sub
End Class
    </file>
    </compilation>)

            Dim treeA = CompilationUtils.GetTree(compilation, "a.vb")
            Dim a_d = treeA.FindNodeOrTokenByKind(SyntaxKind.QualifiedName)

            Dim model = compilation.GetSemanticModel(treeA)

            Dim info = model.GetSemanticInfoSummary(CType(a_d.AsNode(), ExpressionSyntax))
            Assert.Null(info.Type)
        End Sub

    End Class

End Namespace
