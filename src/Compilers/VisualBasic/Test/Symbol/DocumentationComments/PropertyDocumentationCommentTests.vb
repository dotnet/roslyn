' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class PropertyDocumentationCommentTests

        Private ReadOnly _compilation As VisualBasicCompilation
        Private ReadOnly _acmeNamespace As NamespaceSymbol
        Private ReadOnly _widgetClass As NamedTypeSymbol

        Public Sub New()
            _compilation = CompilationUtils.CreateCompilationWithMscorlib40(
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

            _acmeNamespace = DirectCast(_compilation.GlobalNamespace.GetMembers("Acme").Single(), NamespaceSymbol)
            _widgetClass = DirectCast(_acmeNamespace.GetTypeMembers("Widget").Single(), NamedTypeSymbol)
        End Sub

        <Fact>
        Public Sub TestProperty()
            Assert.Equal("P:Acme.Widget.Width",
                         _widgetClass.GetMembers("Width").Single().GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestIndexer1()
            Assert.Equal("P:Acme.Widget.Item(System.Int32)",
                         _widgetClass.GetMembers("Item")(0).GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestIndexer2()
            Assert.Equal("P:Acme.Widget.Item(System.String,System.Int32)",
                         _widgetClass.GetMembers("Item")(1).GetDocumentationCommentId())
        End Sub

    End Class
End Namespace
