' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
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
        Public Async Function TestEvent() As Task
            Const code = "
Class C1
    Event $$AnEvent(ByVal EventNumber As Integer)
End Class
"

            Await VerifyNoRegionsAsync(code)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestEventWithComments() As Task
            Const code = "
Class C1
    {|span:'My
    'Event|}
    Event $$AnEvent(ByVal EventNumber As Integer)
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "' My ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestCustomEvent() As Task
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

            Await VerifyRegionsAsync(code,
                Region("span", "Custom Event eventName As EventHandler ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestPrivateCustomEvent() As Task
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

            Await VerifyRegionsAsync(code,
                Region("span", "Private Custom Event eventName As EventHandler ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestCustomEventWithComments() As Task
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

            Await VerifyRegionsAsync(code,
                Region("span1", "' My ...", autoCollapse:=True),
                Region("span2", "Custom Event eventName As EventHandler ...", autoCollapse:=True))
        End Function

    End Class
End Namespace
