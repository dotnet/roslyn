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

    Public Class MyBaseMyClassSemanticsTests : Inherits BasicTestBase

        <Fact>
        Public Sub MeMyBaseMyClassSymbolsTest()
            Dim compilationDef =
    <compilation name="MeMyBaseMyClassSymbolsTest">
        <file name="a.vb">
Imports System
Module M1

    Class B1
        Protected Overridable Function F() As String
            Return "B1::F"
        End Function
    End Class

    Class B2
        Inherits B1

        Protected Overrides Function F() As String
            Return "B2::F"
        End Function

        Public Sub TestMe()
            Dim prefix = "->"
            Dim f1 As Func(Of String) = Function() prefix &amp; DirectCast(AddressOf [#0 Me 0#].F, Func(Of String))()
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyClass()
            Dim prefix = "->"
            Dim f1 As Func(Of String) = Function() prefix &amp; DirectCast(AddressOf [#1 MyClass 1#].F, Func(Of String))()
            Console.WriteLine(f1())
        End Sub

        Public Sub TestMyBase()
            Dim prefix = "->"
            Dim f1 As Func(Of String) = Function() prefix &amp; DirectCast(AddressOf [#2 MyBase 2#].F, Func(Of String))()
            Console.WriteLine(f1())
        End Sub
    End Class

End Module
        </file>
    </compilation>

            Dim tree As SyntaxTree = Nothing
            Dim nodes As New List(Of SyntaxNode)
            Dim compilation = Compile(compilationDef, tree, nodes)
            Assert.Equal(3, nodes.Count)

            Dim model = compilation.GetSemanticModel(tree)

            Dim info0 = model.GetSemanticInfoSummary(DirectCast(nodes(0), ExpressionSyntax))
            Assert.Equal(info0.Type.Name, "B2")

            Dim info1 = model.GetSemanticInfoSummary(DirectCast(nodes(1), ExpressionSyntax))
            Assert.Equal(info1.Type.Name, "B2")

            Dim info2 = model.GetSemanticInfoSummary(DirectCast(nodes(2), ExpressionSyntax))
            Assert.Equal(info2.Type.Name, "B1")
        End Sub

#Region "Utils"

        Private Sub CheckFieldNameAndLocation(type As TypeSymbol, tree As SyntaxTree, identifierIndex As Integer, fieldName As String, Optional isKey As Boolean = False)
            Dim node = DirectCast(tree.FindNodeOrTokenByKind(SyntaxKind.IdentifierName, identifierIndex).AsNode, IdentifierNameSyntax)
            Dim span As TextSpan = node.Span
            Assert.Equal(fieldName, node.ToString())

            Dim anonymousType = DirectCast(type, NamedTypeSymbol)

            Dim [property] As PropertySymbol = anonymousType.GetMember(Of PropertySymbol)(fieldName)
            Assert.NotNull([property])
            Assert.Equal(fieldName, [property].Name)
            Assert.Equal(1, [property].Locations.Length)
            Assert.Equal(span, [property].Locations(0).SourceSpan)

            Dim getter As MethodSymbol = [property].GetMethod
            Assert.NotNull(getter)
            Assert.Equal("get_" & fieldName, getter.Name)

            If Not isKey Then
                Dim setter As MethodSymbol = [property].SetMethod
                Assert.NotNull(setter)
                Assert.Equal("set_" & fieldName, setter.Name)
            Else
                Assert.True([property].IsReadOnly)
            End If

            ' Do we actually need this??
            'Dim field As FieldSymbol = anonymousType.GetMember(Of FieldSymbol)("$" & fieldName)
            'Assert.NotNull(field)
            'Assert.Equal("$" & fieldName, field.Name)     
            'Assert.Equal(isKey, field.IsReadOnly)
        End Sub

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

