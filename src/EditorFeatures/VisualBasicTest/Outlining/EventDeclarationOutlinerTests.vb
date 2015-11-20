' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class EventDeclarationOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of EventStatementSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New EventDeclarationOutliner()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestEvent()
            Const code = "
Class C1
    Event $$AnEvent(ByVal EventNumber As Integer)
End Class
"

            NoRegions(code)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestEventWithComments()
            Const code = "
Class C1
    {|span:'My
    'Event|}
    Event $$AnEvent(ByVal EventNumber As Integer)
End Class
"

            Regions(code,
                Region("span", "' My ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEvent()
            Const code = "
Class C1
    {|span:Custom Event $$eventName As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        End RaiseEvent
    End Event|}
End Class
"

            Regions(code,
                Region("span", "Custom Event eventName As EventHandler ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestPrivateCustomEvent()
            Const code = "
Class C1
    {|span:Private Custom Event $$eventName As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        End RaiseEvent
    End Event|}
End Class
"

            Regions(code,
                Region("span", "Private Custom Event eventName As EventHandler ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestCustomEventWithComments()
            Const code = "
Class C1
    {|span1:'My
    'Event|}
    {|span2:Custom Event $$eventName As EventHandler
        AddHandler(ByVal value As EventHandler)
        End AddHandler
        RemoveHandler(ByVal value As EventHandler)
        End RemoveHandler
        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
        End RaiseEvent
    End Event|}
End Class
"

            Regions(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Custom Event eventName As EventHandler ...", autoCollapse:=True))
        End Sub

    End Class
End Namespace
