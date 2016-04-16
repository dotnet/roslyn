' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class EventDocumentationCommentTests

        Private ReadOnly _compilation As VisualBasicCompilation
        Private ReadOnly _acmeNamespace As NamespaceSymbol
        Private ReadOnly _widgetClass As NamedTypeSymbol

        Public Sub New()
            _compilation = CompilationUtils.CreateCompilationWithMscorlib(
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

            _acmeNamespace = DirectCast(_compilation.GlobalNamespace.GetMembers("Acme").Single(), NamespaceSymbol)
            _widgetClass = DirectCast(_acmeNamespace.GetTypeMembers("Widget").Single(), NamedTypeSymbol)
        End Sub

        <Fact, WorkItem(530915, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530915")>
        Public Sub SimpleEvent()
            Dim member = _widgetClass.GetMembers("S").First
            Assert.Equal("E:Acme.Widget.S",
                         member.GetDocumentationCommentId())
        End Sub

        <Fact, WorkItem(530915, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530915")>
        Public Sub SimpleEventBackingFIeld()
            Dim member = _widgetClass.GetMembers("SEvent").First
            Assert.Equal("F:Acme.Widget.SEvent",
                         member.GetDocumentationCommentId())
        End Sub

        <Fact, WorkItem(530915, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530915")>
        Public Sub CustomEvent()
            Dim member = _widgetClass.GetMembers("C").First
            Assert.Equal("E:Acme.Widget.C",
                         member.GetDocumentationCommentId())
        End Sub
    End Class
End Namespace
