' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class CastExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New CastExpressionSignatureHelpProvider()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
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
                                     $"CType({Expression1}, {VBWorkspaceResources.Typename}) As {Result}",
                                     ReturnsConvertResult,
                                     ExpressionToConvert,
                                     currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
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
                                     $"CType({Expression1}, {VBWorkspaceResources.Typename}) As {Result}",
                                     ReturnsConvertResult,
                                     NameOfTypeToConvert,
                                     currentParameterIndex:=1))

            Await TestAsync(markup, expectedOrderedItems)
            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
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
                                     $"DirectCast({Expression1}, {VBWorkspaceResources.Typename}) As {Result}",
                                     IntroducesTypeConversion,
                                     ExpressionToConvert,
                                     currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <WorkItem(530132)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
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
                                     $"TryCast({Expression1}, {VBWorkspaceResources.Typename}) As {Result}",
                                     IntroducesSafeTypeConversion,
                                     ExpressionToConvert,
                                     currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function
    End Class
End Namespace