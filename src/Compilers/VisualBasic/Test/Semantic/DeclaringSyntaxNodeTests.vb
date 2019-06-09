' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Text
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class DeclaringSyntaxNodeTests
        Inherits BasicTestBase

        ' Check that the given symbol has the expected number of declaring syntax nodes.
        ' and that each declared node goes back to the given symbol.
        Private Function CheckDeclaringSyntaxNodes(compilation As VisualBasicCompilation, symbol As Symbol, expectedNumber As Integer) As ImmutableArray(Of SyntaxReference)
            Dim declaringNodes As ImmutableArray(Of SyntaxReference) = symbol.DeclaringSyntaxReferences
            Assert.Equal(expectedNumber, declaringNodes.Length)
            If expectedNumber = 0 Then
                Assert.True(Not symbol.IsFromCompilation(compilation) OrElse symbol.IsImplicitlyDeclared, "non-implicitly declares source symbol should have declaring location")
            Else
                Assert.True(symbol.IsFromCompilation(compilation) OrElse TypeOf symbol Is MergedNamespaceSymbol, "symbol with declaration should be in source, except for merged namespaces")
                Assert.False(symbol.IsImplicitlyDeclared)
                For Each node In declaringNodes.Select(Function(d) d.GetSyntax())
                    ' Make sure GetDeclaredSymbol gets back to the symbol for each node.
                    Dim tree As SyntaxTree = node.SyntaxTree
                    Dim model As SemanticModel = compilation.GetSemanticModel(tree)
                    Dim declaredSymbol = model.GetDeclaredSymbol(node)
                    Assert.Equal(symbol.OriginalDefinition, declaredSymbol)

                    ' We don't want to return block nodes.
                    Dim beginStatement As StatementSyntax = Nothing
                    Dim beginTerminator As SyntaxToken = Nothing
                    Dim body As SyntaxList(Of StatementSyntax) = Nothing
                    Dim endStatement As StatementSyntax = Nothing
                    Assert.False(SyntaxFacts.IsBlockStatement(node, beginStatement, beginTerminator, body, endStatement))
                Next

            End If

            Return declaringNodes
        End Function

        Private Function CheckDeclaringSyntaxNodesIncludingParameters(compilation As VisualBasicCompilation, symbol As Symbol, expectedNumber As Integer) As ImmutableArray(Of SyntaxReference)
            Dim nodes As ImmutableArray(Of SyntaxReference) = CheckDeclaringSyntaxNodes(compilation, symbol, expectedNumber)
            Dim meth As MethodSymbol = TryCast(symbol, MethodSymbol)
            If meth IsNot Nothing Then
                For Each p In meth.Parameters
                    CheckDeclaringSyntaxNodes(compilation, p, If(meth.IsAccessor() AndAlso p.Name <> "value", 0, expectedNumber))
                Next
            End If

            Dim prop As PropertySymbol = TryCast(symbol, PropertySymbol)
            If prop IsNot Nothing Then
                For Each p In prop.Parameters
                    CheckDeclaringSyntaxNodes(compilation, p, expectedNumber)
                Next
            End If

            Return nodes
        End Function

        ' Check that the given symbol has the expected number of declaring syntax nodes.
        ' and that the syntax has the expected kind. Does NOT test GetDeclaringSymbol
        Private Function CheckDeclaringSyntaxNodesWithoutGetDeclaredSymbol(compilation As VisualBasicCompilation, symbol As Symbol, expectedNumber As Integer, expectedSyntaxKind As SyntaxKind) As ImmutableArray(Of SyntaxReference)
            Dim declaringNodes As ImmutableArray(Of SyntaxReference) = symbol.DeclaringSyntaxReferences
            Assert.Equal(expectedNumber, declaringNodes.Length)
            If expectedNumber = 0 Then
                Assert.True(Not symbol.IsFromCompilation(compilation) OrElse symbol.IsImplicitlyDeclared, "non-implicitly declares source symbol should have declaring location")
            Else
                Assert.True(symbol.IsFromCompilation(compilation) OrElse TypeOf symbol Is MergedNamespaceSymbol, "symbol with declaration should be in source, except for merged namespaces")

                If symbol.Kind = SymbolKind.Namespace AndAlso DirectCast(symbol, NamespaceSymbol).IsGlobalNamespace Then
                    Assert.True(symbol.IsImplicitlyDeclared)
                Else
                    Assert.False(symbol.IsImplicitlyDeclared)
                End If

                For Each node In declaringNodes.Select(Function(d) d.GetSyntax())
                    Assert.Equal(expectedSyntaxKind, node.Kind)

                    ' We don't want to return block nodes.
                    Dim beginStatement As StatementSyntax = Nothing
                    Dim beginTerminator As SyntaxToken = Nothing
                    Dim body As SyntaxList(Of StatementSyntax) = Nothing
                    Dim endStatement As StatementSyntax = Nothing
                    Assert.False(SyntaxFacts.IsBlockStatement(node, beginStatement, beginTerminator, body, endStatement))
                Next

            End If

            Return declaringNodes
        End Function

        Private Sub CheckDeclaringSyntax(Of TNode As VisualBasicSyntaxNode)(comp As VisualBasicCompilation, tree As SyntaxTree, name As String, kind As SymbolKind)
            Dim model = comp.GetSemanticModel(tree)
            Dim code As String = tree.GetText().ToString()
            Dim position As Integer = code.IndexOf(name, StringComparison.Ordinal)
            Dim token = tree.GetCompilationUnitRoot().FindToken(position)
            Dim node = token.Parent.FirstAncestorOrSelf(Of TNode)()
            Dim sym As Symbol = model.GetDeclaredSymbolFromSyntaxNode(node)

            Assert.Equal(kind, sym.Kind)
            Assert.Equal(name, sym.Name)

            CheckDeclaringSyntaxNodes(comp, sym, 1)
        End Sub

        Private Sub CheckDeclaringSyntaxIsNoDeclaration(Of TNode As VisualBasicSyntaxNode)(comp As VisualBasicCompilation, tree As SyntaxTree, name As String)
            Dim model = comp.GetSemanticModel(tree)
            Dim code As String = tree.GetText().ToString()
            Dim position As Integer = code.IndexOf(name, StringComparison.Ordinal)
            Dim token = tree.GetCompilationUnitRoot().FindToken(position)
            Dim node = token.Parent.FirstAncestorOrSelf(Of TNode)()
            Dim sym As Symbol = model.GetDeclaredSymbolFromSyntaxNode(node)

            Assert.Null(sym)
        End Sub

        Private Sub CheckLambdaDeclaringSyntax(Of TNode As ExpressionSyntax)(comp As VisualBasicCompilation, tree As SyntaxTree, textToSearchFor As String)
            Dim model = comp.GetSemanticModel(tree)
            Dim code As String = tree.GetText().ToString()
            Dim position As Integer = code.IndexOf(textToSearchFor, StringComparison.Ordinal)
            Dim node = tree.GetCompilationUnitRoot().FindToken(position).Parent.FirstAncestorOrSelf(Of TNode)()
            Dim sym As MethodSymbol = TryCast(model.GetSymbolInfo(node).Symbol, MethodSymbol)
            Assert.NotNull(sym)
            Assert.Equal(MethodKind.LambdaMethod, sym.MethodKind)
            Dim nodes = CheckDeclaringSyntaxNodesWithoutGetDeclaredSymbol(comp, sym, 1, node.Kind)
            Assert.Equal(nodes(0).GetSyntax(), node)
            For Each p In sym.Parameters
                CheckDeclaringSyntaxNodes(comp, p, 1)
            Next
        End Sub


        <Fact>
        Public Sub SourceNamedTypeDeclaringSyntax()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="SourceNamedTypeDeclaringSyntax">
        <file name="a.vb">
Namespace N1
    Public Class C1(Of T)
        Class Nested(Of U)
        End Class

        Delegate Function NestedDel(s As String) As Integer
    End Class

    Public Structure S1
        Public f As C1(Of Integer)
    End Structure

    Friend Interface I1
    End Interface

    Enum E1
        Red
    End Enum

    Delegate Sub D1(i As Integer)

    Module M1
    End Module
End Namespace
    </file>
    </compilation>)

            Dim globalNS = comp.GlobalNamespace
            Dim n1 = TryCast(globalNS.GetMembers("N1").Single(), NamespaceSymbol)
            Dim types = n1.GetTypeMembers()
            For Each s In types
                CheckDeclaringSyntaxNodes(comp, s, 1)
            Next

            Dim c1 = TryCast(n1.GetTypeMembers("C1").Single(), NamedTypeSymbol)
            Dim s1 = TryCast(n1.GetTypeMembers("S1").Single(), NamedTypeSymbol)
            Dim f = TryCast(s1.GetMembers("f").Single(), FieldSymbol)
            CheckDeclaringSyntaxNodes(comp, f.Type, 1)
            For Each s In c1.GetTypeMembers()
                CheckDeclaringSyntaxNodes(comp, s, 1)
            Next
        End Sub

        <Fact>
        Public Sub NonSourceTypeDeclaringSyntax()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NonSourceTypeDeclaringSyntax">
        <file name="a.vb">
Namespace N1
    Public Class C1
        Dim o As Object
        Dim i As Integer
        Dim lst As System.Collections.Generic.List(Of String)
        Dim arr As C1()
        Dim arr2d As C1(,)
        Dim err As ErrType
        Dim consErr As ConsErrType(Of Object)
    End Class
End Namespace    </file>
    </compilation>)

            Dim globalNS = comp.GlobalNamespace
            Dim n1 = TryCast(globalNS.GetMembers("N1").Single(), NamespaceSymbol)
            Dim c1 = TryCast(n1.GetTypeMembers("C1").Single(), NamedTypeSymbol)
            For Each f In c1.GetMembers().OfType(Of FieldSymbol)()
                CheckDeclaringSyntaxNodes(comp, f.Type, 0)
            Next
        End Sub

        <Fact>
        Public Sub AnonTypeDeclaringSyntax()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AnonTypeDeclaringSyntax">
        <file name="a.vb">
Public Class C1
    Sub f()
        Dim a1 = New With {.a = 5, .b = "hi"}
    End Sub
End Class
     </file>
    </compilation>)

            Dim tree = comp.SyntaxTrees(0)
            Dim text = tree.GetText().ToString()
            Dim model = comp.GetSemanticModel(tree)
            Dim globalNS = comp.GlobalNamespace
            Dim posA1 As Integer = text.IndexOf("a1", StringComparison.Ordinal)
            Dim declaratorA1 = tree.GetCompilationUnitRoot().FindToken(posA1).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            Dim localA1 = DirectCast(model.GetDeclaredSymbol(declaratorA1.Names(0)), LocalSymbol)
            Dim localA1Type = localA1.Type
            Assert.True(localA1Type.IsAnonymousType)
            CheckDeclaringSyntaxNodesWithoutGetDeclaredSymbol(comp, localA1Type, 1, SyntaxKind.AnonymousObjectCreationExpression)
            For Each memb In localA1Type.GetMembers()
                Dim expectedDeclaringNodes As Integer = 0
                If TypeOf memb Is PropertySymbol Then
                    expectedDeclaringNodes = 1
                End If

                CheckDeclaringSyntaxNodes(comp, memb, expectedDeclaringNodes)
            Next

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub AnonTypeDeclaringSyntaxStaticLocal()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AnonTypeDeclaringSyntax">
        <file name="a.vb">
Public Class C1
    Sub f()
        Static a1 = New With {.a = 5, .b = "hi"}
    End Sub
End Class
     </file>
    </compilation>)

            Dim tree = comp.SyntaxTrees(0)
            Dim text = tree.GetText().ToString()
            Dim model = comp.GetSemanticModel(tree)
            Dim globalNS = comp.GlobalNamespace
            Dim posA1 As Integer = text.IndexOf("a1", StringComparison.Ordinal)
            Dim declaratorA1 = tree.GetCompilationUnitRoot().FindToken(posA1).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            Dim localA1 = DirectCast(model.GetDeclaredSymbol(declaratorA1.Names(0)), LocalSymbol)
            Dim localA1Type = localA1.Type


            Dim declaringNodes As ImmutableArray(Of SyntaxReference) = localA1Type.DeclaringSyntaxReferences

            Assert.False(localA1Type.IsAnonymousType)  'Object Not Anonymous Type
            CheckDeclaringSyntaxNodesWithoutGetDeclaredSymbol(comp, localA1Type, 0, SyntaxKind.AnonymousObjectCreationExpression)

            For Each memb In localA1Type.GetMembers()
                Dim expectedDeclaringNodes As Integer = 0
                If TypeOf memb Is PropertySymbol Then
                    expectedDeclaringNodes = 1
                End If

                CheckDeclaringSyntaxNodes(comp, memb, expectedDeclaringNodes)
            Next

        End Sub

        <WorkItem(15925, "DevDiv_Projects/Roslyn")>
        <Fact()>
        Public Sub AnonTypeDeclaringSyntaxStaticLocalWithDimKeyword()
            'This should be fixed when multiple modifiers issues is resolved so that this should work in the same
            'way as static and not normal local declarations as at present.
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AnonTypeDeclaringSyntax">
        <file name="a.vb">
Public Class C1
    Sub f()
        Static Dim a1 = New With {.a = 5, .b = "hi"}
    End Sub
End Class
     </file>
    </compilation>)

            Dim tree = comp.SyntaxTrees(0)
            Dim text = tree.GetText().ToString()
            Dim model = comp.GetSemanticModel(tree)
            Dim globalNS = comp.GlobalNamespace
            Dim posA1 As Integer = text.IndexOf("a1", StringComparison.Ordinal)
            Dim declaratorA1 = tree.GetCompilationUnitRoot().FindToken(posA1).Parent.FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
            Dim localA1 = DirectCast(model.GetDeclaredSymbol(declaratorA1.Names(0)), LocalSymbol)
            Dim localA1Type = localA1.Type


            Dim declaringNodes As ImmutableArray(Of SyntaxReference) = localA1Type.DeclaringSyntaxReferences

            'Object Not Anonymous Type - when multiple modifier issues is resolved
            Assert.False(localA1Type.IsAnonymousType)
            CheckDeclaringSyntaxNodesWithoutGetDeclaredSymbol(comp, localA1Type, 0, SyntaxKind.AnonymousObjectCreationExpression)

            For Each memb In localA1Type.GetMembers()
                Dim expectedDeclaringNodes As Integer = 0
                If TypeOf memb Is PropertySymbol Then
                    expectedDeclaringNodes = 1
                End If

                CheckDeclaringSyntaxNodes(comp, memb, expectedDeclaringNodes)
            Next

        End Sub


        <Fact>
        Public Sub NamespaceDeclaringSyntax()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NamespaceDeclaringSyntax">
        <file name="a.vb">
Namespace N1
    Namespace N2
        Namespace N3
        End Namespace
    End Namespace
End Namespace

Namespace N1.N2
    Namespace N3
    End Namespace
End Namespace

Namespace System
End Namespace     </file>
    </compilation>)

            Dim tree = comp.SyntaxTrees(0)
            Dim globalNS = comp.GlobalNamespace
            Dim system = TryCast(globalNS.GetMembers("System").Single(), NamespaceSymbol)
            Dim n1 = TryCast(globalNS.GetMembers("N1").Single(), NamespaceSymbol)
            Dim n2 = TryCast(n1.GetMembers("N2").Single(), NamespaceSymbol)
            Dim n3 = TryCast(n2.GetMembers("N3").Single(), NamespaceSymbol)
            CheckDeclaringSyntaxNodes(comp, n2, 2)
            CheckDeclaringSyntaxNodes(comp, n3, 2)
            CheckDeclaringSyntaxNodes(comp, system, 1)
            CheckDeclaringSyntaxNodesWithoutGetDeclaredSymbol(comp, n1, 2, SyntaxKind.NamespaceStatement)
            CheckDeclaringSyntaxNodesWithoutGetDeclaredSymbol(comp, globalNS, 1, SyntaxKind.CompilationUnit)
        End Sub

        <Fact>
        Public Sub NamespaceDeclaringSyntax2()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="NamespaceDeclaringSyntax">
        <file name="a.vb">
Namespace N2
    Namespace N3
    End Namespace
End Namespace

Namespace Global.N4
End Namespace
</file>
    </compilation>, options:=New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithRootNamespace("N1"))

            Dim tree = comp.SyntaxTrees(0)
            Dim globalNS = comp.GlobalNamespace
            Dim system = TryCast(globalNS.GetMembers("System").Single(), NamespaceSymbol)
            Dim n1 = TryCast(globalNS.GetMembers("N1").Single(), NamespaceSymbol)
            Dim n2 = TryCast(n1.GetMembers("N2").Single(), NamespaceSymbol)
            Dim n3 = TryCast(n2.GetMembers("N3").Single(), NamespaceSymbol)
            Dim n4 = TryCast(globalNS.GetMembers("N4").Single(), NamespaceSymbol)
            CheckDeclaringSyntaxNodes(comp, n2, 1)
            CheckDeclaringSyntaxNodes(comp, n3, 1)
            CheckDeclaringSyntaxNodes(comp, n4, 1)
            CheckDeclaringSyntaxNodesWithoutGetDeclaredSymbol(comp, n1, 1, SyntaxKind.CompilationUnit)
        End Sub


        <Fact>
        Public Sub TypeParameterDeclaringSyntax()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="TypeParameterDeclaringSyntax">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Namespace N1
    Class C1(Of T, U)
        Class C2(Of W)
            Public f1 As C1(Of Integer, String).C2(Of W)
            Public Sub m(Of R)()
            End Sub
        End Class

        Class C3(Of W)
            Public f2 As IEnumerable(Of U)
            Public f3 As Goo(Of Bar)
        End Class
    End Class

    Class M
    End Class
End Namespace     
    </file>
    </compilation>)
            Dim globalNS = comp.GlobalNamespace
            Dim n1 = TryCast(globalNS.GetMembers("N1").Single(), NamespaceSymbol)
            Dim c1 = TryCast(n1.GetTypeMembers("C1").Single(), NamedTypeSymbol)
            Dim c2 = TryCast(c1.GetTypeMembers("C2").Single(), NamedTypeSymbol)
            Dim c3 = TryCast(c1.GetTypeMembers("C3").Single(), NamedTypeSymbol)
            For Each s In c1.TypeParameters
                CheckDeclaringSyntaxNodes(comp, s, 1)
            Next

            For Each f In c2.GetMembers().OfType(Of FieldSymbol)()
                For Each tp In (DirectCast(f.Type, NamedTypeSymbol)).TypeParameters
                    CheckDeclaringSyntaxNodes(comp, tp, 1)
                Next
            Next

            For Each m In c2.GetMembers().OfType(Of MethodSymbol)()
                For Each tp In m.TypeParameters
                    CheckDeclaringSyntaxNodes(comp, tp, 1)
                Next
            Next

            For Each f In c3.GetMembers().OfType(Of FieldSymbol)()
                For Each tp In (DirectCast(f.Type, NamedTypeSymbol)).TypeParameters
                    CheckDeclaringSyntaxNodes(comp, tp, 0)
                Next
            Next
        End Sub

        <Fact>
        Public Sub MemberDeclaringSyntax()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="SourceNamedTypeDeclaringSyntax">
        <file name="a.vb">
Namespace N1
    Enum E1
        Red
        Blue = 5
        Green
    End Enum

    MustInherits Class C1(Of T)
        Dim v, w, x As C1(Of T)
        Const q = 4, r = 7
        Const s As Integer = 8
        Public Sub New(i As Integer)
        End Sub
        Shared Sub New()
        End Sub
        Function M(t As T, Optional y As Integer = 3) As Integer
            Return 4
        End Function
        Property p As Integer
            Get
                Return 0
            End Get
            Set(value As Integer)
            End Set
        End Property
        MustOverride Property Prop2 as Integer
        Property Prop3 as Integer
        Default Property Items(i As Integer, j As Integer) As String
            Get
                Return ""
            End Get
            Set(value As String)
            End Set
        End Property
        Event ev1 As EventHandler
        Event ev2(x As Integer, y As String)
        Custom Event ev3 As EventHandler
            AddHandler(value As EventHandler)
            End AddHandler

            RemoveHandler(value As EventHandler)
            End RemoveHandler

            RaiseEvent()
            End RaiseEvent
        End Event
    End Class

    Class C2(Of U)
        Shared x As Integer = 7
    End Class
End Namespace
    </file>
    </compilation>)


            Dim globalNS = comp.GlobalNamespace
            Dim n1 = TryCast(globalNS.GetMembers("N1").Single(), NamespaceSymbol)
            Dim c1 = TryCast(n1.GetTypeMembers("C1").Single(), NamedTypeSymbol)
            Dim c2 = TryCast(n1.GetTypeMembers("C2").Single(), NamedTypeSymbol)
            Dim e1 = TryCast(n1.GetTypeMembers("E1").Single(), NamedTypeSymbol)
            For Each memb In e1.GetMembers()
                If memb.Kind = SymbolKind.Method AndAlso (DirectCast(memb, MethodSymbol)).MethodKind = MethodKind.Constructor Then
                    CheckDeclaringSyntaxNodesIncludingParameters(comp, memb, 0)
                ElseIf memb.Name = "value__" Then
                    CheckDeclaringSyntaxNodesIncludingParameters(comp, memb, 0)
                Else
                    CheckDeclaringSyntaxNodesIncludingParameters(comp, memb, 1)
                End If
            Next

            Dim ev1 = TryCast(c1.GetMembers("ev1").Single(), EventSymbol)
            Dim ev2 = TryCast(c1.GetMembers("ev2").Single(), EventSymbol)
            Dim prop3 = TryCast(c1.GetMembers("Prop3").Single(), PropertySymbol)
            For Each memb In c1.GetMembers()
                Dim expectedDeclaringNodes As Integer = 1
                If TypeOf memb Is MethodSymbol Then
                    Dim meth As MethodSymbol = DirectCast(memb, MethodSymbol)
                    If meth.AssociatedSymbol IsNot Nothing AndAlso
                        (meth.AssociatedSymbol.OriginalDefinition.Equals(ev1) OrElse
                        meth.AssociatedSymbol.OriginalDefinition.Equals(ev2) OrElse
                        meth.AssociatedSymbol.OriginalDefinition.Equals(prop3)) Then
                        expectedDeclaringNodes = 0
                    End If
                    If meth.IsAccessor() AndAlso meth.AssociatedSymbol.IsMustOverride Then
                        expectedDeclaringNodes = 0
                    End If
                End If

                If TypeOf memb Is FieldSymbol Then
                    Dim fld As FieldSymbol = DirectCast(memb, FieldSymbol)
                    If fld.AssociatedSymbol IsNot Nothing AndAlso
                        (fld.AssociatedSymbol.OriginalDefinition.Equals(prop3) OrElse
                         fld.AssociatedSymbol.Kind = SymbolKind.Event) Then
                        expectedDeclaringNodes = 0
                    End If
                End If

                If TypeOf memb Is NamedTypeSymbol Then
                    Dim nt As NamedTypeSymbol = DirectCast(memb, NamedTypeSymbol)
                    If nt.TypeKind = TypeKind.Delegate AndAlso nt.Name.EndsWith("EventHandler", StringComparison.Ordinal) Then
                        expectedDeclaringNodes = 0
                    End If
                End If

                CheckDeclaringSyntaxNodesIncludingParameters(comp, memb, expectedDeclaringNodes)
            Next

            Dim fieldT = TryCast(c1.GetMembers("v").Single(), FieldSymbol)
            Dim constructedC1 = fieldT.Type
            For Each memb In constructedC1.GetMembers()
                Dim expectedDeclaringNodes As Integer = 1
                If TypeOf memb Is MethodSymbol Then
                    Dim meth As MethodSymbol = DirectCast(memb, MethodSymbol)
                    If meth.AssociatedSymbol IsNot Nothing AndAlso
                        (meth.AssociatedSymbol.OriginalDefinition.Equals(ev1) OrElse
                        meth.AssociatedSymbol.OriginalDefinition.Equals(ev2) OrElse
                        meth.AssociatedSymbol.OriginalDefinition.Equals(prop3)) Then
                        expectedDeclaringNodes = 0
                    End If
                    If meth.IsAccessor() AndAlso meth.AssociatedSymbol.IsMustOverride Then
                        expectedDeclaringNodes = 0
                    End If
                End If

                If TypeOf memb Is FieldSymbol Then
                    Dim fld As FieldSymbol = DirectCast(memb, FieldSymbol)
                    If fld.AssociatedSymbol IsNot Nothing AndAlso
                        (fld.AssociatedSymbol.OriginalDefinition.Equals(prop3) OrElse
                         fld.AssociatedSymbol.Kind = SymbolKind.Event) Then
                        expectedDeclaringNodes = 0
                    End If
                End If

                If TypeOf memb Is NamedTypeSymbol Then
                    Dim nt As NamedTypeSymbol = DirectCast(memb, NamedTypeSymbol)
                    If nt.TypeKind = TypeKind.Delegate AndAlso nt.Name.EndsWith("EventHandler", StringComparison.Ordinal) Then
                        expectedDeclaringNodes = 0
                    End If
                End If

                CheckDeclaringSyntaxNodesIncludingParameters(comp, memb, expectedDeclaringNodes)
            Next

            For Each memb In c2.GetMembers()
                If memb.Kind = SymbolKind.Method Then
                    CheckDeclaringSyntaxNodesIncludingParameters(comp, memb, 0)
                End If
            Next
        End Sub

        <Fact>
        Public Sub LocalDeclaringSyntax()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="LocalDeclaringSyntax">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Class C1
    Sub m()
        Dim loc1, loc2 As Integer
        Dim loc3 = 5
        Const loc4 = 6
        Const loc5 As Integer = 7
        Using loc6 As IDisposable = goo()
        End Using
        For loc7 as Integer = 1 To 10
        Next
        For loc8 = 1 To 10
        Next
        For Each loc9 as Integer In {5, 6, 6}
        Next
        For Each loc10 in {5, 6, 6}
        Next
    End Sub
    Function goo() As IDisposable
    End Function
End Class
    </file>
    </compilation>)

            Dim tree = comp.SyntaxTrees(0)
            CheckDeclaringSyntax(Of ModifiedIdentifierSyntax)(comp, tree, "loc1", SymbolKind.Local)
            CheckDeclaringSyntax(Of ModifiedIdentifierSyntax)(comp, tree, "loc2", SymbolKind.Local)
            CheckDeclaringSyntax(Of ModifiedIdentifierSyntax)(comp, tree, "loc3", SymbolKind.Local)
            CheckDeclaringSyntax(Of ModifiedIdentifierSyntax)(comp, tree, "loc4", SymbolKind.Local)
            CheckDeclaringSyntax(Of ModifiedIdentifierSyntax)(comp, tree, "loc5", SymbolKind.Local)
            CheckDeclaringSyntax(Of ModifiedIdentifierSyntax)(comp, tree, "loc6", SymbolKind.Local)
            CheckDeclaringSyntax(Of ModifiedIdentifierSyntax)(comp, tree, "loc7", SymbolKind.Local)
            CheckDeclaringSyntaxIsNoDeclaration(Of ForStatementSyntax)(comp, tree, "loc8")
            CheckDeclaringSyntax(Of ModifiedIdentifierSyntax)(comp, tree, "loc9", SymbolKind.Local)
            CheckDeclaringSyntaxIsNoDeclaration(Of ForEachStatementSyntax)(comp, tree, "loc10")
        End Sub

        <Fact>
        Public Sub LabelDeclaringSyntax()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="LabelDeclaringSyntax">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Class C1
    Sub m()
lab1:
lab2:
        Console.WriteLine()
lab3:
    End Sub
End Class
    </file>
    </compilation>)

            Dim tree = comp.SyntaxTrees(0)
            CheckDeclaringSyntax(Of LabelStatementSyntax)(comp, tree, "lab1", SymbolKind.Label)
            CheckDeclaringSyntax(Of LabelStatementSyntax)(comp, tree, "lab2", SymbolKind.Label)
            CheckDeclaringSyntax(Of LabelStatementSyntax)(comp, tree, "lab3", SymbolKind.Label)
        End Sub

        <Fact>
        Public Sub AliasDeclaringSyntax()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="AliasDeclaringSyntax">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports ConsoleAlias = System.Console, GooAlias = System
Imports ListOfIntAlias = System.Collections.Generic.List(Of Integer)

Namespace N1

End Namespace
    </file>
    </compilation>)

            Dim tree = comp.SyntaxTrees(0)
            CheckDeclaringSyntax(Of SimpleImportsClauseSyntax)(comp, tree, "ConsoleAlias", SymbolKind.Alias)
            CheckDeclaringSyntax(Of SimpleImportsClauseSyntax)(comp, tree, "GooAlias", SymbolKind.Alias)
            CheckDeclaringSyntax(Of SimpleImportsClauseSyntax)(comp, tree, "ListOfIntAlias", SymbolKind.Alias)
        End Sub

        <Fact>
        Public Sub RangeVariableDeclaringSyntax()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="RangeVariableDeclaringSyntax">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Sub f()
        Dim a As IEnumerable(Of Integer) = {3, 4, 5}

        Dim y1 = From range1 In a Let range2 = a.ToString Select range3 = range1 * 2 Select range3 + 1
        Dim y2 = From range4 In a, range8 in a Join range5 In a On range4.ToString() Equals range5.ToString() Select range7 = range4 + range5 Aggregate range6 In a Into range9 = Sum(range6 + range7)
    End Sub
End Class    
</file>
    </compilation>)

            Dim tree = comp.SyntaxTrees(0)
            CheckDeclaringSyntax(Of CollectionRangeVariableSyntax)(comp, tree, "range1", SymbolKind.RangeVariable)
            CheckDeclaringSyntax(Of ExpressionRangeVariableSyntax)(comp, tree, "range2", SymbolKind.RangeVariable)
            CheckDeclaringSyntax(Of ExpressionRangeVariableSyntax)(comp, tree, "range3", SymbolKind.RangeVariable)
            CheckDeclaringSyntax(Of CollectionRangeVariableSyntax)(comp, tree, "range4", SymbolKind.RangeVariable)
            CheckDeclaringSyntax(Of CollectionRangeVariableSyntax)(comp, tree, "range5", SymbolKind.RangeVariable)
            CheckDeclaringSyntax(Of CollectionRangeVariableSyntax)(comp, tree, "range6", SymbolKind.RangeVariable)
            CheckDeclaringSyntax(Of ExpressionRangeVariableSyntax)(comp, tree, "range7", SymbolKind.RangeVariable)
            CheckDeclaringSyntax(Of CollectionRangeVariableSyntax)(comp, tree, "range8", SymbolKind.RangeVariable)
            CheckDeclaringSyntax(Of AggregationRangeVariableSyntax)(comp, tree, "range9", SymbolKind.RangeVariable)
        End Sub

        <Fact>
        Public Sub LambdaDeclaringSyntax()
            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40(
    <compilation name="RangeVariableDeclaringSyntax">
        <file name="a.vb">
Imports System
Imports System.Collections.Generic

Class C
    Sub f()
        Dim x As Integer = 0
        Dim f1 As Func(Of Integer, Integer, Integer) = Function(a, b) a + b '1
        Dim f2 As Func(Of Integer, Integer, Integer) = Function(a, b) 
                                                           Return a + b '2
                                                       End Function
        Dim f3 As Action(Of Integer, Integer) = Sub(a, b) x = a + b '3
        Dim f4 As Action(Of Integer, Integer) = Sub(a, b) 
                                                    x = a + b '4
                                                End Sub
    End Sub
End Class</file>
    </compilation>)

            Dim tree = comp.SyntaxTrees(0)
            CheckLambdaDeclaringSyntax(Of SingleLineLambdaExpressionSyntax)(comp, tree, "'1")
            CheckLambdaDeclaringSyntax(Of MultiLineLambdaExpressionSyntax)(comp, tree, "'2")
            CheckLambdaDeclaringSyntax(Of SingleLineLambdaExpressionSyntax)(comp, tree, "'3")
            CheckLambdaDeclaringSyntax(Of MultiLineLambdaExpressionSyntax)(comp, tree, "'4")
        End Sub
    End Class

End Namespace
