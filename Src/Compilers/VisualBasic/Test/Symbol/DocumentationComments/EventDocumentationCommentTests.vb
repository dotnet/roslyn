' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class EventDocumentationCommentTests

        Private m_compilation As VisualBasicCompilation
        Private m_acmeNamespace As NamespaceSymbol
        Private m_widgetClass As NamedTypeSymbol

        Public Sub New()
            m_compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation name="FieldDocumentationCommentTests">
    <file name="a.vb"><![CDATA[
Namespace Acme
    Class Widget
        Public Event S As EventHandler

        Private Events As New System.ComponentModel.EventHandlerList
        Public Custom Event C As EventHandler
            AddHandler(ByVal value As EventHandler)
                Events.AddHandler("CEvent", value)
            End AddHandler
            RemoveHandler(ByVal value As EventHandler)
                Events.RemoveHandler("CEvent", value)
            End RemoveHandler
            RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                CType(Events("CEvent"), EventHandler).Invoke(sender, e)
            End RaiseEvent
        End Event

    End Class
End Namespace
]]>
    </file>
</compilation>)

            m_acmeNamespace = DirectCast(m_compilation.GlobalNamespace.GetMembers("Acme").Single(), NamespaceSymbol)
            m_widgetClass = DirectCast(m_acmeNamespace.GetTypeMembers("Widget").Single(), NamedTypeSymbol)
        End Sub

        <Fact, WorkItem(530915, "DevDiv")>
        Public Sub SimpleEvent()
            Dim member = m_widgetClass.GetMembers("S").First
            Assert.Equal("E:Acme.Widget.S",
                         member.GetDocumentationCommentId())
        End Sub

        <Fact, WorkItem(530915, "DevDiv")>
        Public Sub SimpleEventBackingFIeld()
            Dim member = m_widgetClass.GetMembers("SEvent").First
            Assert.Equal("F:Acme.Widget.SEvent",
                         member.GetDocumentationCommentId())
        End Sub

        <Fact, WorkItem(530915, "DevDiv")>
        Public Sub CustomEvent()
            Dim member = m_widgetClass.GetMembers("C").First
            Assert.Equal("E:Acme.Widget.C",
                         member.GetDocumentationCommentId())
        End Sub
    End Class
End Namespace