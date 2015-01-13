' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class TypeDocumentationCommentTests

        Private m_compilation As VisualBasicCompilation
        Private m_acmeNamespace As NamespaceSymbol
        Private m_widgetClass As NamedTypeSymbol

        Public Sub New()
            m_compilation = CompilationUtils.CreateCompilationWithMscorlib(
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

            m_acmeNamespace = DirectCast(m_compilation.GlobalNamespace.GetMembers("Acme").Single(), NamespaceSymbol)
            m_widgetClass = DirectCast(m_acmeNamespace.GetTypeMembers("Widget").Single(), NamedTypeSymbol)
        End Sub

        <Fact>
        Public Sub TestEnum()
            Assert.Equal("T:Color",
                         m_compilation.GlobalNamespace.GetTypeMembers("Color").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestInterface()
            Assert.Equal("T:Acme.IProcess",
                         m_acmeNamespace.GetTypeMembers("IProcess").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestStructure()
            Assert.Equal("T:Acme.ValueType",
                         m_acmeNamespace.GetTypeMembers("ValueType").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestClass()
            Assert.Equal("T:Acme.Widget",
                         m_acmeNamespace.GetTypeMembers("Widget").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestNestedClass()
            Assert.Equal("T:Acme.Widget.NestedClass",
                         m_widgetClass.GetTypeMembers("NestedClass").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestNestedInterface()
            Assert.Equal("T:Acme.Widget.IMenuItem",
                         m_widgetClass.GetTypeMembers("IMenuItem").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestNestedDelegate()
            Assert.Equal("T:Acme.Widget.Del",
                         m_widgetClass.GetTypeMembers("Del").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestNestedEnum()
            Assert.Equal("T:Acme.Widget.Direction",
                         m_widgetClass.GetTypeMembers("Direction").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestGenericType()
            Assert.Equal("T:Acme.MyList`1",
                         m_acmeNamespace.GetTypeMembers("MyList", 1).Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestNestedGenericType()
            Assert.Equal("T:Acme.MyList`1.Helper`2",
                         m_acmeNamespace.GetTypeMembers("MyList", 1).Single() _
                            .GetTypeMembers("Helper", 2).Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestModule()
            Assert.Equal("T:Acme.Module1",
                         m_acmeNamespace.GetTypeMembers("Module1").Single().GetDocumentationCommentId())
        End Sub

    End Class
End Namespace
