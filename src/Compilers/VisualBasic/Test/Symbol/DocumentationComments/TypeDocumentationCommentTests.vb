' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class TypeDocumentationCommentTests

        Private ReadOnly _compilation As VisualBasicCompilation
        Private ReadOnly _acmeNamespace As NamespaceSymbol
        Private ReadOnly _widgetClass As NamedTypeSymbol

        Public Sub New()
            _compilation = CompilationUtils.CreateCompilationWithMscorlib40(
                <compilation name="TypeDocumentationCommentTests">
                    <file name="a.vb">
                    Enum Color
                        Red
                        Blue
                        Green
                    End Enum

                    Namespace Acme
                        Interface IProcess
                        End Interface

                        Structure ValueType
                            Dim i As Integer
                        End Structure

                        Class Widget
                            Public Class NestedClass
                            End Class

                            Public Interface IMenuItem
                            End Interface

                            Public Delegate Sub Del(i As Integer)

                            Public Enum Direction
                                North
                                South
                                East
                                West
                            End Enum
                        End Class

                        Class MyList(Of T)
                            Class Helper(Of U, V)
                            End Class
                        End Class

                        Module Module1
                        End Module
                    End Namespace
                    </file>
                </compilation>)

            _acmeNamespace = DirectCast(_compilation.GlobalNamespace.GetMembers("Acme").Single(), NamespaceSymbol)
            _widgetClass = DirectCast(_acmeNamespace.GetTypeMembers("Widget").Single(), NamedTypeSymbol)
        End Sub

        <Fact>
        Public Sub TestEnum()
            Assert.Equal("T:Color",
                         _compilation.GlobalNamespace.GetTypeMembers("Color").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestInterface()
            Assert.Equal("T:Acme.IProcess",
                         _acmeNamespace.GetTypeMembers("IProcess").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestStructure()
            Assert.Equal("T:Acme.ValueType",
                         _acmeNamespace.GetTypeMembers("ValueType").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestClass()
            Assert.Equal("T:Acme.Widget",
                         _acmeNamespace.GetTypeMembers("Widget").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestNestedClass()
            Assert.Equal("T:Acme.Widget.NestedClass",
                         _widgetClass.GetTypeMembers("NestedClass").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestNestedInterface()
            Assert.Equal("T:Acme.Widget.IMenuItem",
                         _widgetClass.GetTypeMembers("IMenuItem").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestNestedDelegate()
            Assert.Equal("T:Acme.Widget.Del",
                         _widgetClass.GetTypeMembers("Del").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestNestedEnum()
            Assert.Equal("T:Acme.Widget.Direction",
                         _widgetClass.GetTypeMembers("Direction").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestGenericType()
            Assert.Equal("T:Acme.MyList`1",
                         _acmeNamespace.GetTypeMembers("MyList", 1).Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestNestedGenericType()
            Assert.Equal("T:Acme.MyList`1.Helper`2",
                         _acmeNamespace.GetTypeMembers("MyList", 1).Single() _
                            .GetTypeMembers("Helper", 2).Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestModule()
            Assert.Equal("T:Acme.Module1",
                         _acmeNamespace.GetTypeMembers("Module1").Single().GetDocumentationCommentId())
        End Sub

    End Class
End Namespace
