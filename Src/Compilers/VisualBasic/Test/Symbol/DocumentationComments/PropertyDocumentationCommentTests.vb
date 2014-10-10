' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class PropertyDocumentationCommentTests

        Private m_compilation As VisualBasicCompilation
        Private m_acmeNamespace As NamespaceSymbol
        Private m_widgetClass As NamedTypeSymbol

        Public Sub New()
            m_compilation = CompilationUtils.CreateCompilationWithMscorlib(
                <compilation name="PropertyDocumentationCommentTests">
                    <file name="a.vb">
                    Namespace Acme
                        Class Widget
                            Public Property Width() As Integer
                                Get
                                End Get
                                Set (Value As Integer)
                                End Set
                            End Property

                            Public Default Property Item(i As Integer) As Integer
                                Get
                                End Get
                                Set (Value As Integer)
                                End Set
                            End Property

                            Public Default Property Item(s As String, _
                                i As Integer) As Integer
                                Get
                                End Get
                                Set (Value As Integer)
                                End Set
                            End Property
                        End Class
                    End Namespace
                    </file>
                </compilation>)

            m_acmeNamespace = DirectCast(m_compilation.GlobalNamespace.GetMembers("Acme").Single(), NamespaceSymbol)
            m_widgetClass = DirectCast(m_acmeNamespace.GetTypeMembers("Widget").Single(), NamedTypeSymbol)
        End Sub

        <Fact>
        Public Sub TestProperty()
            Assert.Equal("P:Acme.Widget.Width",
                         m_widgetClass.GetMembers("Width").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestIndexer1()
            Assert.Equal("P:Acme.Widget.Item(System.Int32)",
                         m_widgetClass.GetMembers("Item")(0).GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestIndexer2()
            Assert.Equal("P:Acme.Widget.Item(System.String,System.Int32)",
                         m_widgetClass.GetMembers("Item")(1).GetDocumentationCommentId())
        End Sub

    End Class
End Namespace
