' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp
Imports Microsoft.VisualStudio.Composition

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class MidAssignmentSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New MidAssignmentSignatureHelpProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationForMidAssignmentFirstArgument()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Mid($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     "Mid(<stringName>, <startIndex>, [<length>]) = <stringExpression>",
                                     "Replaces a specified number of characters in a String variable with characters from another string.",
                                     "The name of the string variable to modify.",
                                     currentParameterIndex:=0))
            Test(markup, expectedOrderedItems)
            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationForMidAssignmentSecondArgument()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Mid(s, $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     "Mid(<stringName>, <startIndex>, [<length>]) = <stringExpression>",
                                     "Replaces a specified number of characters in a String variable with characters from another string.",
                                     "The one-based character position in the string where the replacement of text begins.",
                                     currentParameterIndex:=1))
            Test(markup, expectedOrderedItems)
            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestInvocationForMidAssignmentThirdArgument()
            Dim markup = <a><![CDATA[
Class C
    Sub Foo()
        Mid(s, 1, $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     "Mid(<stringName>, <startIndex>, [<length>]) = <stringExpression>",
                                     "Replaces a specified number of characters in a String variable with characters from another string.",
                                     "The number of characters to replace. If omitted, the length of <stringExpression> is used.",
                                     currentParameterIndex:=2))
            Test(markup, expectedOrderedItems)
            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub
    End Class
End Namespace
