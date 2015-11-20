' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class AccessorDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of AccessorStatementSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New AccessorDeclarationOutliner()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestReadOnlyPropertyGet()
            Const code = "
Class C1
    ReadOnly Property P1 As Integer
        {|span:$$Get
            Return 0
        End Get|}
    End Property
EndClass
"

            Regions(code,
                Region("span", "Get ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestReadOnlyPropertyGetWithComments()
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

            Regions(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Get ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPropertyGet()
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

            Regions(code,
                Region("span", "Get ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPropertyGetWithComments()
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

            Regions(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Get ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestWriteOnlyPropertySet()
            Const code = "
Class C1
    WriteOnly Property P1 As Integer
        {|span:$$Set(ByVal value As Integer)
            Return 0
        End Set|}
    End Property
EndClass
"

            Regions(code,
                Region("span", "Set(value As Integer) ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestWriteOnlyPropertySetWithComments()
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

            Regions(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Set(value As Integer) ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPropertySet()
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

            Regions(code,
                Region("span", "Set(value As Integer) ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPropertySetWithPrivateModifier()
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

            Regions(code,
                Region("span", "Private Set(value As Integer) ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPropertySetWithComments()
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

            Regions(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Private Set(value As Integer) ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEventAddHandler()
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

            Regions(code,
                Region("span", "AddHandler(value As EventHandler) ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEventAddHandlerWithComments()
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

            Regions(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "AddHandler(value As EventHandler) ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEventRemoveHandler()
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

            Regions(code,
                Region("span", "RemoveHandler(value As EventHandler) ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEventRemoveHandlerWithComments()
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

            Regions(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "RemoveHandler(value As EventHandler) ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEventRaiseHandler()
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

            Regions(code,
                Region("span", "RaiseEvent(sender As Object, e As EventArgs) ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEventRaiseHandlerWithComments()
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

            Regions(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "RaiseEvent(sender As Object, e As EventArgs) ...", autoCollapse:=True))
        End Sub

    End Class
End Namespace
