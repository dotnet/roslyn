' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    <Trait(Traits.Feature, Traits.Features.SignatureHelp)>
    Public Class AddRemoveHandlerSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Friend Overrides Function GetSignatureHelpProviderType() As Type
            Return GetType(AddRemoveHandlerSignatureHelpProvider)
        End Function

        <Fact>
        Public Async Function TestInvocationForAddHandler() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        AddHandler $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"AddHandler {VBWorkspaceResources.event_}, {VBWorkspaceResources.handler}",
                                     VBWorkspaceResources.Associates_an_event_with_an_event_handler_delegate_or_lambda_expression_at_run_time,
                                     VBWorkspaceResources.The_event_to_associate_an_event_handler_delegate_or_lambda_expression_with,
                                     currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact>
        Public Async Function TestInvocationForAddHandlerAfterComma() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        AddHandler goo, $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"AddHandler {VBWorkspaceResources.event_}, {VBWorkspaceResources.handler}",
                                     VBWorkspaceResources.Associates_an_event_with_an_event_handler_delegate_or_lambda_expression_at_run_time,
                                     VBWorkspaceResources.The_event_handler_to_associate_with_the_event_This_may_take_the_form_of_AddressOf_eventHandler_delegate_lambdaExpression,
                                     currentParameterIndex:=1))

            Await TestAsync(markup, expectedOrderedItems)
            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function

        <Fact>
        Public Async Function TestInvocationForRemoveHandler() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        RemoveHandler $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"RemoveHandler {VBWorkspaceResources.event_}, {VBWorkspaceResources.handler}",
                                     VBWorkspaceResources.Removes_the_association_between_an_event_and_an_event_handler_or_delegate_at_run_time,
                                     VBWorkspaceResources.The_event_to_disassociate_an_event_handler_or_delegate_from,
                                     currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact>
        Public Async Function TestInvocationForRemoveHandlerAfterComma() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        [|RemoveHandler goo, $$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"RemoveHandler {VBWorkspaceResources.event_}, {VBWorkspaceResources.handler}",
                                     VBWorkspaceResources.Removes_the_association_between_an_event_and_an_event_handler_or_delegate_at_run_time,
                                     VBWorkspaceResources.The_event_handler_to_disassociate_from_the_event_This_may_take_the_form_of_AddressOf_eventHandler_delegate,
                                     currentParameterIndex:=1))

            Await TestAsync(markup, expectedOrderedItems)
            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function
    End Class
End Namespace
