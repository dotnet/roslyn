' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    <Trait(Traits.Feature, Traits.Features.SignatureHelp)>
    Public Class CastExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Friend Overrides Function GetSignatureHelpProviderType() As Type
            Return GetType(CastExpressionSignatureHelpProvider)
        End Function

        <Fact>
        Public Async Function TestInvocationForCType() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        Dim x = CType($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"CType({VBWorkspaceResources.expression}, {VBWorkspaceResources.typeName}) As {VBWorkspaceResources.result}",
                                     VBWorkspaceResources.Returns_the_result_of_explicitly_converting_an_expression_to_a_specified_data_type,
                                     VBWorkspaceResources.The_expression_to_be_evaluated_and_converted,
                                     currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact>
        Public Async Function TestInvocationForCTypeAfterComma() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        Dim x = CType(bar, $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"CType({VBWorkspaceResources.expression}, {VBWorkspaceResources.typeName}) As {VBWorkspaceResources.result}",
                                     VBWorkspaceResources.Returns_the_result_of_explicitly_converting_an_expression_to_a_specified_data_type,
                                     VBWorkspaceResources.The_name_of_the_data_type_to_which_the_value_of_expression_will_be_converted,
                                     currentParameterIndex:=1))

            Await TestAsync(markup, expectedOrderedItems)
            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function

        <Fact>
        Public Async Function TestInvocationForDirectCast() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        Dim x = DirectCast($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"DirectCast({VBWorkspaceResources.expression}, {VBWorkspaceResources.typeName}) As {VBWorkspaceResources.result}",
                                     VBWorkspaceResources.Introduces_a_type_conversion_operation_similar_to_CType_The_difference_is_that_CType_succeeds_as_long_as_there_is_a_valid_conversion_whereas_DirectCast_requires_that_one_type_inherit_from_or_implement_the_other_type,
                                     VBWorkspaceResources.The_expression_to_be_evaluated_and_converted,
                                     currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530132")>
        Public Async Function TestInvocationForTryCast() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        Dim x = [|TryCast($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"TryCast({VBWorkspaceResources.expression}, {VBWorkspaceResources.typeName}) As {VBWorkspaceResources.result}",
                                     VBWorkspaceResources.Introduces_a_type_conversion_operation_that_does_not_throw_an_exception_If_an_attempted_conversion_fails_TryCast_returns_Nothing_which_your_program_can_test_for,
                                     VBWorkspaceResources.The_expression_to_be_evaluated_and_converted,
                                     currentParameterIndex:=0))

            Await TestAsync(markup, expectedOrderedItems)
        End Function
    End Class
End Namespace
