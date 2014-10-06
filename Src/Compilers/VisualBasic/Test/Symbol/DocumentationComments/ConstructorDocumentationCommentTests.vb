' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class ConstructorDocumentationCommentTests

        Private m_compilation As VBCompilation
        Private m_acmeNamespace As NamespaceSymbol
        Private m_widgetClass As NamedTypeSymbol

        Public Sub New()
            m_compilation = CompilationUtils.CreateCompilationWithMscorlib(
                <compilation name="ConstructorDocumentationCommentTests">
                    <file name="a.vb">
                    Namespace Acme
                        Class Widget
                            Shared Sub New()
                            End Sub

                            Public Sub New()
                            End Sub

                            Public Sub New(s As String)
                            End Sub
                        End Class
                    End Namespace
                    </file>
                </compilation>)

            m_acmeNamespace = DirectCast(m_compilation.GlobalNamespace.GetMembers("Acme").Single(), NamespaceSymbol)
            m_widgetClass = DirectCast(m_acmeNamespace.GetTypeMembers("Widget").Single(), NamedTypeSymbol)
        End Sub

        <Fact>
        Public Sub TestSharedConstructor()
            Assert.Equal("M:Acme.Widget.#cctor",
                         m_widgetClass.GetMembers()(0).GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestInstanceConstructor1()
            Assert.Equal("M:Acme.Widget.#ctor",
                         m_widgetClass.GetMembers()(1).GetDocumentationCommentId())
        End Sub

        <Fact>
        Public Sub TestInstanceConstructor2()
            Assert.Equal("M:Acme.Widget.#ctor(System.String)",
                         m_widgetClass.GetMembers()(2).GetDocumentationCommentId())
        End Sub

    End Class
End Namespace
