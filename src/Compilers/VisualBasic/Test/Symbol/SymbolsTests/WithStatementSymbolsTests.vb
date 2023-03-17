' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.ExtensionMethods

    Public Class WithStatementSymbolsTests
        Inherits BasicTestBase

        <Fact()>
        Public Sub LocalInEmptyWithStatementExpression()
            Dim compilationDef =
    <compilation name="LocalInEmptyWithStatementExpression">
        <file name="a.vb">
Module WithTestScoping
    Sub Main()
        Dim o1 As New Object()
        With [#0 o1 0#]
        End With
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(1, nodes.Count)

            Dim model = compilation.GetSemanticModel(tree)

            Dim info0 = model.GetSemanticInfoSummary(DirectCast(nodes(0), ExpressionSyntax))
            Assert.NotNull(info0.Symbol)
            Assert.Equal("o1 As System.Object", info0.Symbol.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub NestedWithWithLambdasAndObjectInitializers()
            Dim compilationDef =
    <compilation name="LocalInEmptyWithStatementExpression">
        <file name="a.vb">
Imports System

Structure SS1
    Public A As String
    Public B As String
End Structure

Structure SS2
    Public X As SS1
    Public Y As SS1
End Structure

Structure Clazz
    Shared Sub Main(args() As String)
        Dim t As New Clazz(1)
    End Sub
    Sub New(i As Integer)
        Dim outer As New SS2
        With outer
            Dim a As New [#0 SS2 0#]() With {.X = Function() As SS1
                                                 With .Y
                                                     a.Y.B = a.Y.A
                                                     a.Y.A = "1"
                                                 End With
                                                 Return .Y
                                             End Function.Invoke()}
        End With
    End Sub
End Structure
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(1, nodes.Count)

            Dim model = compilation.GetSemanticModel(tree)

            Dim info0 = model.GetSemanticInfoSummary(DirectCast(nodes(0), ExpressionSyntax))
            Assert.NotNull(info0.Symbol)
            Assert.Equal("SS2", info0.Symbol.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub LocalInEmptyWithStatementExpression_Struct()
            Dim compilationDef =
    <compilation name="LocalInEmptyWithStatementExpression_Struct">
        <file name="a.vb">
Structure STR
    Public F As String
End Structure

Module WithTestScoping
    Sub Main()
        Dim o1 As New STR()
        With [#0 o1 0#]
        End With
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(1, nodes.Count)

            Dim model = compilation.GetSemanticModel(tree)

            Dim info0 = model.GetSemanticInfoSummary(DirectCast(nodes(0), ExpressionSyntax))
            Assert.NotNull(info0.Symbol)
            Assert.Equal("o1 As STR", info0.Symbol.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub LocalInWithStatementExpression()
            Dim compilationDef =
    <compilation name="LocalInWithStatementExpression">
        <file name="a.vb">
Module WithTestScoping
    Sub Main()
        Dim o1 As New Object()
        With [#0 o1 0#]
            Dim o1 = Nothing
            With [#1 o1 1#]
                Dim o2 = Nothing
            End With
            With [#2 New Object() 2#]
                Dim o1 As New Object()
                Dim o2 = Nothing
            End With
        End With
    End Sub
End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes,
<errors>
BC30616: Variable 'o1' hides a variable in an enclosing block.
            Dim o1 = Nothing
                ~~
BC30616: Variable 'o1' hides a variable in an enclosing block.
                Dim o1 As New Object()
                    ~~
</errors>)
            Assert.Equal(3, nodes.Count)

            Dim model = compilation.GetSemanticModel(tree)

            Dim info0 = model.GetSemanticInfoSummary(DirectCast(nodes(0), ExpressionSyntax))
            Assert.NotNull(info0.Symbol)
            Assert.Equal("o1 As System.Object", info0.Symbol.ToTestDisplayString())

            Dim info1 = model.GetSemanticInfoSummary(DirectCast(nodes(1), ExpressionSyntax))
            Assert.NotNull(info1.Symbol)
            Assert.Equal("o1 As System.Object", info1.Symbol.ToTestDisplayString())
            Assert.NotSame(info0.Symbol, info1.Symbol)

            Dim info2 = model.GetSemanticInfoSummary(DirectCast(nodes(2), ExpressionSyntax))
            Assert.NotNull(info2.Symbol)
            Assert.Equal("Sub System.Object..ctor()", info2.Symbol.ToTestDisplayString())
        End Sub

        <Fact()>
        Public Sub NestedWithStatements()
            Dim compilationDef =
    <compilation name="NestedWithStatements">
        <file name="a.vb">
Structure Clazz
    Structure SS
        Public FLD As String
    End Structure

    Public FLD As SS

    Sub TEST()
        With Me
            With [#0 .FLD 0#]
                Dim v As String = .GetType() .ToString()
            End With
        End With
    End Sub
End Structure
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(1, nodes.Count)

            Dim model = compilation.GetSemanticModel(tree)

            Dim info0 = model.GetSemanticInfoSummary(DirectCast(nodes(0), ExpressionSyntax))
            Assert.NotNull(info0.Symbol)
            Assert.Equal("Clazz.FLD As Clazz.SS", info0.Symbol.ToTestDisplayString())

            Dim systemObject = compilation.GetTypeByMetadataName("System.Object")
            Dim conv = model.ClassifyConversion(DirectCast(nodes(0), ExpressionSyntax), systemObject)
            Assert.True(conv.IsWidening)

        End Sub

        <Fact()>
        Public Sub LocalInWithStatementExpression3()
            Dim compilationDef =
    <compilation name="LocalInWithStatementExpression3">
        <file name="a.vb">
Structure Clazz
    Structure SSS
        Public FLD As String
    End Structure

    Structure SS
        Public FS As SSS
    End Structure

    Public FLD As SS

    Sub TEST()
        With Me
            With .FLD
                With .FS 
                    Dim v = [#0 .FLD 0#]
                End With
            End With
        End With
    End Sub
End Structure
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(1, nodes.Count)

            Dim model = compilation.GetSemanticModel(tree)

            Dim info0 = model.GetSemanticInfoSummary(DirectCast(nodes(0), ExpressionSyntax))
            Assert.NotNull(info0.Symbol)
            Assert.Equal("Clazz.SSS.FLD As System.String", info0.Symbol.ToTestDisplayString())

            Dim systemObject = compilation.GetTypeByMetadataName("System.Object")
            Dim conv = model.ClassifyConversion(DirectCast(nodes(0), ExpressionSyntax), systemObject)
            Assert.True(conv.IsWidening)

        End Sub

#Region "Utils"

        Private Function Compile(text As XElement, ByRef tree As SyntaxTree, nodes As List(Of SyntaxNode), Optional errors As XElement = Nothing) As VisualBasicCompilation
            Dim spans As New List(Of TextSpan)
            ExtractTextIntervals(text, spans)

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib40AndReferences(text, {SystemRef, SystemCoreRef, MsvbRef})
            If errors Is Nothing Then
                CompilationUtils.AssertNoErrors(compilation)
            Else
                CompilationUtils.AssertTheseDiagnostics(compilation, errors)
            End If

            tree = compilation.SyntaxTrees(0)
            For Each span In spans

                Dim stack As New Stack(Of SyntaxNode)
                stack.Push(tree.GetRoot())

                While stack.Count > 0
                    Dim node = stack.Pop()

                    If span.Contains(node.Span) Then
                        nodes.Add(node)
                        Exit While
                    End If

                    For Each child In node.ChildNodes
                        stack.Push(child)
                    Next
                End While
            Next

            Return compilation
        End Function

        Private Shared Sub ExtractTextIntervals(text As XElement, nodes As List(Of TextSpan))
            text.<file>.Value = text.<file>.Value.Trim().Replace(vbLf, vbCrLf)

            Dim index As Integer = 0
            Do
                Dim startMarker = "[#" & index
                Dim endMarker = index & "#]"

                ' opening '[#{0-9}'
                Dim start = text.<file>.Value.IndexOf(startMarker, StringComparison.Ordinal)
                If start < 0 Then
                    Exit Do
                End If

                ' closing '{0-9}#]'
                Dim [end] = text.<file>.Value.IndexOf(endMarker, StringComparison.Ordinal)
                Assert.InRange([end], 0, Int32.MaxValue)

                nodes.Add(New TextSpan(start, [end] - start + 3))

                text.<file>.Value = text.<file>.Value.Replace(startMarker, "   ").Replace(endMarker, "   ")

                index += 1
                Assert.InRange(index, 0, 9)
            Loop
        End Sub

        Private Shared Function GetNamedTypeSymbol(c As VisualBasicCompilation, namedTypeName As String, Optional fromCorLib As Boolean = False) As NamedTypeSymbol
            Dim nameParts = namedTypeName.Split("."c)

            Dim srcAssembly = DirectCast(c.Assembly, SourceAssemblySymbol)
            Dim nsSymbol As NamespaceSymbol = (If(fromCorLib, srcAssembly.CorLibrary, srcAssembly)).GlobalNamespace
            For Each ns In nameParts.Take(nameParts.Length - 1)
                nsSymbol = DirectCast(nsSymbol.GetMember(ns), NamespaceSymbol)
            Next
            Return DirectCast(nsSymbol.GetTypeMember(nameParts(nameParts.Length - 1)), NamedTypeSymbol)
        End Function

#End Region

    End Class

End Namespace

