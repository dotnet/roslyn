' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class AccessorDeclarationStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of AccessorStatementSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New AccessorDeclarationStructureProvider()
        End Function

        <Fact>
        Public Async Function TestReadOnlyPropertyGet() As Task
            Const code = "
Class C1
    ReadOnly Property P1 As Integer
        {|span:$$Get
            Return 0
        End Get|}
    End Property
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Get ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestReadOnlyPropertyGetWithComments() As Task
            Const code = "
Class C1
    ReadOnly Property P1 As Integer
        {|span1:'My
        'Getter|}
        {|span2:$$Get
            Return 0
        End Get|}
    End Property
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Get ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestPropertyGet() As Task
            Const code = "
Class C1
    Property P1 As Integer
        {|span:$$Get
            Return 0
        End Get|}
        Set(value As Integer)
        End Set
    End Property
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Get ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestPropertyGetWithComments() As Task
            Const code = "
Class C1
    Property P1 As Integer
        {|span1:'My
        'Getter|}
        {|span2:$$Get
            Return 0
        End Get|}
        Set(value As Integer)
        End Set
    End Property
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Get ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestWriteOnlyPropertySet() As Task
            Const code = "
Class C1
    WriteOnly Property P1 As Integer
        {|span:$$Set(ByVal value As Integer)
            Return 0
        End Set|}
    End Property
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Set(ByVal value As Integer) ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestWriteOnlyPropertySetWithComments() As Task
            Const code = "
Class C1
    WriteOnly Property P1 As Integer
        {|span1:'My
        'Setter|}
        {|span2:$$Set(ByVal value As Integer)
            Return 0
        End Set|}
    End Property
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Set(ByVal value As Integer) ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestPropertySet() As Task
            Const code = "
Class C1
    Property P1 As Integer
        Get
            Return 0
        End Get
        {|span:$$Set(value As Integer)
        End Set|}
    End Property
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Set(value As Integer) ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestPropertySetWithPrivateModifier() As Task
            Const code = "
Class C1
    Property P1 As Integer
        Get
            Return 0
        End Get
        {|span:Private $$Set(value As Integer)
        End Set|}
    End Property
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "Private Set(value As Integer) ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestPropertySetWithComments() As Task
            Const code = "
Class C1
    Property P1 As Integer
        Get
            Return 0
        End Get
        {|span1:'My
        'Setter|}
        {|span2:Private $$Set(value As Integer)
        End Set|}
    End Property
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Private Set(value As Integer) ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestCustomEventAddHandler() As Task
            Const code = "
Class C1
    Custom Event event As EventHandler
        {|span:AddHandler$$(ByVal value As EventHandler)
        End AddHandler|}
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        End RaiseEvent
    End Event
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "AddHandler(ByVal value As EventHandler) ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestCustomEventAddHandlerWithComments() As Task
            Const code = "
Class C1
    Custom Event event As EventHandler
        {|span1:'My
        'AddHandler|}
        {|span2:AddHandler$$(ByVal value As EventHandler)
        End AddHandler|}
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        End RaiseEvent
    End Event
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "AddHandler(ByVal value As EventHandler) ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestCustomEventRemoveHandler() As Task
            Const code = "
Class C1
    Custom Event event As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        {|span:RemoveHandler$$(ByVal value As EventHandler)
        End RemoveHandler|}
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        End RaiseEvent
    End Event
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "RemoveHandler(ByVal value As EventHandler) ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestCustomEventRemoveHandlerWithComments() As Task
            Const code = "
Class C1
    Custom Event event As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        {|span1:'My
        'RemoveHandler|}
        {|span2:RemoveHandler$$(ByVal value As EventHandler)
        End RemoveHandler|}
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        End RaiseEvent
    End Event
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "RemoveHandler(ByVal value As EventHandler) ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestCustomEventRaiseHandler() As Task
            Const code = "
Class C1
    Custom Event event As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        {|span:RaiseEvent$$(ByVal sender As Object, ByVal e As EventArgs)
        End RaiseEvent|}
    End Event
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "RaiseEvent(ByVal sender As Object, ByVal e As EventArgs) ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestCustomEventRaiseHandlerWithComments() As Task
            Const code = "
Class C1
    Custom Event event As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        {|span1:'My
        'RaiseEvent|}
        {|span2:RaiseEvent$$(ByVal sender As Object, ByVal e As EventArgs)
        End RaiseEvent|}
    End Event
EndClass
"

            Await VerifyBlockSpansAsync(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "RaiseEvent(ByVal sender As Object, ByVal e As EventArgs) ...", autoCollapse:=True))
        End Function

    End Class
End Namespace
