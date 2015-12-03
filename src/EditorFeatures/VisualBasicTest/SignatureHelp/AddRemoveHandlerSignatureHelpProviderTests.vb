' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class AddRemoveHandlerSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New AddRemoveHandlerSignatureHelpProvider()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForAddHandler() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        AddHandler $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"AddHandler {Event1}, {Handler}",
                                     AssociatesAnEvent,
                                     EventToAssociate,
                                     currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForAddHandlerAfterComma() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        AddHandler foo, $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"AddHandler {Event1}, {Handler}",
                                     AssociatesAnEvent,
                                     EventHandlerToAssociate,
                                     currentParameterIndex:=1))

            Await TestAsync(markup, expectedOrderedItems)
            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function


        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForRemoveHandler() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        RemoveHandler $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"RemoveHandler {Event1}, {Handler}",
                                     RemovesEventAssociation,
                                     EventToDisassociate,
                                     currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForRemoveHandlerAfterComma() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        [|RemoveHandler foo, $$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"RemoveHandler {Event1}, {Handler}",
                                     RemovesEventAssociation,
                                     EventHandlerToDisassociate,
                                     currentParameterIndex:=1))

            Await TestAsync(markup, expectedOrderedItems)
            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function
    End Class
End Namespace
