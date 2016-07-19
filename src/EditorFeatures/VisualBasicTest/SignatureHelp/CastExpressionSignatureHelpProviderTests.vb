' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class CastExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New CastExpressionSignatureHelpProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForCType() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim x = CType($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"CType({VBWorkspaceResources.Expression1}, {VBWorkspaceResources.Typename}) As {VBWorkspaceResources.Result}",
                                     VBWorkspaceResources.ReturnsConvertResult,
                                     VBWorkspaceResources.ExpressionToConvert,
                                     currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForCTypeAfterComma() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim x = CType(bar, $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"CType({VBWorkspaceResources.Expression1}, {VBWorkspaceResources.Typename}) As {VBWorkspaceResources.Result}",
                                     VBWorkspaceResources.ReturnsConvertResult,
                                     VBWorkspaceResources.NameOfTypeToConvert,
                                     currentParameterIndex:=1))

            Await TestAsync(markup, expectedOrderedItems)
            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForDirectCast() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim x = DirectCast($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"DirectCast({VBWorkspaceResources.Expression1}, {VBWorkspaceResources.Typename}) As {VBWorkspaceResources.Result}",
                                     VBWorkspaceResources.IntroducesTypeConversion,
                                     VBWorkspaceResources.ExpressionToConvert,
                                     currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <WorkItem(530132, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530132")>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForTryCast() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim x = [|TryCast($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"TryCast({VBWorkspaceResources.Expression1}, {VBWorkspaceResources.Typename}) As {VBWorkspaceResources.Result}",
                                     VBWorkspaceResources.IntroducesSafeTypeConversion,
                                     VBWorkspaceResources.ExpressionToConvert,
                                     currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function
    End Class
End Namespace