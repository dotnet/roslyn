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
                                     $"If({VBWorkspaceResources.Expression1}, {VBWorkspaceResources.ExpressionIfNothing}) As {VBWorkspaceResources.Result}",
                                     VBWorkspaceResources.ExpressionEvalReturns,
                                     VBWorkspaceResources.ReturnedIfINotNothing,
                                     currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"If({VBWorkspaceResources.Condition} As Boolean, {VBWorkspaceResources.ExpressionIfTrue}, {VBWorkspaceResources.ExpressionIfFalse}) As {VBWorkspaceResources.Result}",
                                     VBWorkspaceResources.IfConditionReturnsResults,
                                     VBWorkspaceResources.ExpressionToEvaluate,
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
                                     $"If({VBWorkspaceResources.Expression1}, {VBWorkspaceResources.ExpressionIfNothing}) As {VBWorkspaceResources.Result}",
                                     VBWorkspaceResources.ExpressionEvalReturns,
                                     VBWorkspaceResources.ReturnedIfNothing,
                                     currentParameterIndex:=1))
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"If({VBWorkspaceResources.Condition} As Boolean, {VBWorkspaceResources.ExpressionIfTrue}, {VBWorkspaceResources.ExpressionIfFalse}) As {VBWorkspaceResources.Result}",
                                     VBWorkspaceResources.IfConditionReturnsResults,
                                     VBWorkspaceResources.EvaluatedAndReturnedIfTrue,
                                     currentParameterIndex:=1))
            Await TestAsync(markup, expectedOrderedItems)
        End Function
    End Class
End Namespace