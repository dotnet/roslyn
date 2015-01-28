' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class BinaryConditionalExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New BinaryConditionalExpressionSignatureHelpProvider
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
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
                                     "If(<expression>, <expressionIfNothing>) As <result>",
                                     "If <expression> evaluates to a reference or Nullable value that is not Nothing, the function returns that value. Otherwise, it calculates and returns <expressionIfNothing>.",
                                     "Returned if it evaluates to a reference or nullable type that is not Nothing.",
                                     currentParameterIndex:=0))
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     "If(<condition> As Boolean, <expressionIfTrue>, <expressionIfFalse>) As <result>",
                                     "If <condition> returns True, the function calculates and returns <expressionIfTrue>. Otherwise, it returns <expressionIfFalse>.",
                                     "The expression to evaluate.",
                                     currentParameterIndex:=0))
            Test(markup, expectedOrderedItems)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
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
                                     "If(<expression>, <expressionIfNothing>) As <result>",
                                     "If <expression> evaluates to a reference or Nullable value that is not Nothing, the function returns that value. Otherwise, it calculates and returns <expressionIfNothing>.",
                                     "Evaluated and returned if <expression> evaluates to Nothing.",
                                     currentParameterIndex:=1))
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     "If(<condition> As Boolean, <expressionIfTrue>, <expressionIfFalse>) As <result>",
                                     "If <condition> returns True, the function calculates and returns <expressionIfTrue>. Otherwise, it returns <expressionIfFalse>.",
                                     "Evaluated and returned if <condition> evaluates to True.",
                                     currentParameterIndex:=1))
            Test(markup, expectedOrderedItems)
        End Sub
    End Class
End Namespace
