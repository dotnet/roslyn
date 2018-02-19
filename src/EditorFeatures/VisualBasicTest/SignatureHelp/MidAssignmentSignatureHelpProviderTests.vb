' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.VisualBasic.SignatureHelp

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SignatureHelp
    Public Class MidAssignmentSignatureHelpProviderTests
        Inherits AbstractVisualBasicSignatureHelpProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateSignatureHelpProvider() As ISignatureHelpProvider
            Return New MidAssignmentSignatureHelpProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForMidAssignmentFirstArgument() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        Mid($$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"Mid({VBWorkspaceResources.stringName}, {VBWorkspaceResources.startIndex}, [{VBWorkspaceResources.length}]) = {VBWorkspaceResources.stringExpression}",
                                     VBWorkspaceResources.Replaces_a_specified_number_of_characters_in_a_String_variable_with_characters_from_another_string,
                                     VBWorkspaceResources.The_name_of_the_string_variable_to_modify,
                                     currentParameterIndex:=0))
            Await TestAsync(markup, expectedOrderedItems)
            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForMidAssignmentSecondArgument() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        Mid(s, $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"Mid({VBWorkspaceResources.stringName}, {VBWorkspaceResources.startIndex}, [{VBWorkspaceResources.length}]) = {VBWorkspaceResources.stringExpression}",
                                     VBWorkspaceResources.Replaces_a_specified_number_of_characters_in_a_String_variable_with_characters_from_another_string,
                                     VBWorkspaceResources.The_one_based_character_position_in_the_string_where_the_replacement_of_text_begins,
                                     currentParameterIndex:=1))
            Await TestAsync(markup, expectedOrderedItems)
            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestInvocationForMidAssignmentThirdArgument() As Task
            Dim markup = <a><![CDATA[
Class C
    Sub Goo()
        Mid(s, 1, $$
    End Sub
End Class
]]></a>.Value

            Dim expectedOrderedItems = New List(Of SignatureHelpTestItem)()
            expectedOrderedItems.Add(New SignatureHelpTestItem(
                                     $"Mid({VBWorkspaceResources.stringName}, {VBWorkspaceResources.startIndex}, [{VBWorkspaceResources.length}]) = {VBWorkspaceResources.stringExpression}",
                                     VBWorkspaceResources.Replaces_a_specified_number_of_characters_in_a_String_variable_with_characters_from_another_string,
                                     VBWorkspaceResources.The_number_of_characters_to_replace_If_omitted_the_length_of_stringExpression_is_used,
                                     currentParameterIndex:=2))
            Await TestAsync(markup, expectedOrderedItems)
            Await TestAsync(markup, expectedOrderedItems, usePreviousCharAsTrigger:=True)
        End Function
    End Class
End Namespace
