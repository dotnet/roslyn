' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    <Trait(Traits.Feature, Traits.Features.SignatureHelp)>
    Public Class BinaryConditionalExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Friend Overrides Function GetSignatureHelpProviderType() As Type
            Return GetType(BinaryConditionalExpressionSignatureHelpProvider)
        End Function

        <Fact>
        Public Async Function TestInvocationForIf() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        Dim x = If($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"If({VBWorkspaceResources.expression}, {VBWorkspaceResources.expressionIfNothing}) As {VBWorkspaceResources.result}",
                                     VBWorkspaceResources.If_expression_evaluates_to_a_reference_or_Nullable_value_that_is_not_Nothing_the_function_returns_that_value_Otherwise_it_calculates_and_returns_expressionIfNothing,
                                     VBWorkspaceResources.Returned_if_it_evaluates_to_a_reference_or_nullable_type_that_is_not_Nothing,
                                     currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"If({VBWorkspaceResources.condition} As Boolean, {VBWorkspaceResources.expressionIfTrue}, {VBWorkspaceResources.expressionIfFalse}) As {VBWorkspaceResources.result}",
                                     VBWorkspaceResources.If_condition_returns_True_the_function_calculates_and_returns_expressionIfTrue_Otherwise_it_returns_expressionIfFalse,
                                     VBWorkspaceResources.The_expression_to_evaluate,
                                     currentParameterIndex:=0))
            Await TestAsync(markup, expectedOrderedItems)
        End Function

        <Fact>
        Public Async Function TestInvocationForIfAfterComma() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        Dim x = If(True, $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"If({VBWorkspaceResources.expression}, {VBWorkspaceResources.expressionIfNothing}) As {VBWorkspaceResources.result}",
                                     VBWorkspaceResources.If_expression_evaluates_to_a_reference_or_Nullable_value_that_is_not_Nothing_the_function_returns_that_value_Otherwise_it_calculates_and_returns_expressionIfNothing,
                                     VBWorkspaceResources.Evaluated_and_returned_if_expression_evaluates_to_Nothing,
                                     currentParameterIndex:=1))
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"If({VBWorkspaceResources.condition} As Boolean, {VBWorkspaceResources.expressionIfTrue}, {VBWorkspaceResources.expressionIfFalse}) As {VBWorkspaceResources.result}",
                                     VBWorkspaceResources.If_condition_returns_True_the_function_calculates_and_returns_expressionIfTrue_Otherwise_it_returns_expressionIfFalse,
                                     VBWorkspaceResources.Evaluated_and_returned_if_condition_evaluates_to_True,
                                     currentParameterIndex:=1))
            Await TestAsync(markup, expectedOrderedItems)
        End Function
    End Class
End Namespace
