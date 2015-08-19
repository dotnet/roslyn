' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class MidAssignmentSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New MidAssignmentSignatureHelpProvider()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
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
                                     $"Mid({StringName}, {StartIndex}, [{Length}]) = {StringExpression}",
                                     ReplacesChars,
                                     NameOfStringVariable,
                                     currentParameterIndex:=0))
            Test(markup, expectedOrderedItems)
            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
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
                                     $"Mid({StringName}, {StartIndex}, [{Length}]) = {StringExpression}",
                                     ReplacesChars,
                                     OneBasedStartPos,
                                     currentParameterIndex:=1))
            Test(markup, expectedOrderedItems)
            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
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
                                     $"Mid({StringName}, {StartIndex}, [{Length}]) = {StringExpression}",
                                     ReplacesChars,
                                     NumberOfCharsToReplace,
                                     currentParameterIndex:=2))
            Test(markup, expectedOrderedItems)
            Test(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Sub
    End Class
End Namespace
