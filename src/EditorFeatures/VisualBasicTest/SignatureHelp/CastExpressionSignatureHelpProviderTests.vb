' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class CastExpressionSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New CastExpressionSignatureHelpProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationForCType()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim x = CType($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     "CType(<expression>, <typeName>) As <result>",
                                     "Returns the result of explicitly converting an expression to a specified data type.",
                                     "The expression to be evaluated and converted.",
                                     currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationForCTypeAfterComma()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim x = CType(bar, $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     "CType(<expression>, <typeName>) As <result>",
                                     "Returns the result of explicitly converting an expression to a specified data type.",
                                     "The name of the data type to which the value of expression will be converted.",
                                     currentParameterIndex:=1))

            Test(markup, expectedOrderedItems)
            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationForDirectCast()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim x = DirectCast($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     "DirectCast(<expression>, <typeName>) As <result>",
                                     "Introduces a type conversion operation similar to CType. The difference is that CType succeeds as long as there is a valid conversion, whereas DirectCast requires that one type inherit from or implement the other type.",
                                     "The expression to be evaluated and converted.",
                                     currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

        <WorkItem(530132)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationForTryCast()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Dim x = [|TryCast($$
    |]End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     "TryCast(<expression>, <typeName>) As <result>",
                                     "Introduces a type conversion operation that does not throw an exception. If an attempted conversion fails, TryCast returns Nothing, which your program can test for.",
                                     "The expression to be evaluated and converted.",
                                     currentParameterIndex:=0))

            Test(markup, expectedOrderedItems)
        End Sub

    End Class
End Namespace
