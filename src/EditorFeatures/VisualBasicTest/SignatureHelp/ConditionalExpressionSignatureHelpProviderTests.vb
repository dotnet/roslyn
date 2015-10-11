' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class BinaryConditionalExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New BinaryConditionalExpressionSignatureHelpProvider
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationForIf()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim x = If($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"If({Expression1}, {ExpressionIfNothing}) As {Result}",
                                     ExpressionEvalReturns,
                                     ReturnedIfINotNothing,
                                     currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"If({Condition} As Boolean, {ExpressionIfTrue}, {ExpressionIfFalse}) As {Result}",
                                     IfConditionReturnsResults,
                                     ExpressionToEvaluate,
                                     currentParameterIndex:=0))
            Test(markup, expectedOrderedItems)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationForIfAfterComma()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim x = If(True, $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"If({Expression1}, {ExpressionIfNothing}) As {Result}",
                                     ExpressionEvalReturns,
                                     ReturnedIfNothing,
                                     currentParameterIndex:=1))
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"If({Condition} As Boolean, {ExpressionIfTrue}, {ExpressionIfFalse}) As {Result}",
                                     IfConditionReturnsResults,
                                     EvaluatedAndReturnedIfTrue,
                                     currentParameterIndex:=1))
            Test(markup, expectedOrderedItems)
        End Sub
    End Class
End Namespace
