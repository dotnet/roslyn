' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class BinaryConditionalExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New BinaryConditionalExpressionSignatureHelpProvider
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForIf() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
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

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForIfAfterComma() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
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