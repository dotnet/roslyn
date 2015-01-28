' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class AddRemoveHandlerSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New AddRemoveHandlerSignatureHelpProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationForAddHandler()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        AddHandler $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     "AddHandler <event>, <handler>",
                                     "Associates an event with an event handler, delegate or lambda expression at run time.",
                                     "The event to associate an event handler, delegate or lambda expression with.",
                                     currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationForAddHandlerAfterComma()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        AddHandler foo, $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     "AddHandler <event>, <handler>",
                                     "Associates an event with an event handler, delegate or lambda expression at run time.",
                                     "The event handler to associate with the event. This may take the form of { AddressOf <eventHandler> | <delegate> | <lambdaExpression> }.",
                                     currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub


        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationForRemoveHandler()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        RemoveHandler $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     "RemoveHandler <event>, <handler>",
                                     "Removes the association between an event and an event handler or delegate at run time.",
                                     "The event to disassociate an event handler or delegate from.",
                                     currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationForRemoveHandlerAfterComma()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        [|RemoveHandler foo, $$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     "RemoveHandler <event>, <handler>",
                                     "Removes the association between an event and an event handler or delegate at run time.",
                                     "The event handler to disassociate from the event. This may take the form of { AddressOf <eventHandler> | <delegate> }.",
                                     currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub
    End Class
End Namespace
