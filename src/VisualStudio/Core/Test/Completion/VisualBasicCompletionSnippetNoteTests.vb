' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Completion
    <[UseExportProvider]>
    Public Class VisualBasicCompletionSnippetNoteTests
        Private _markup As XElement = <document>
                                          <![CDATA[Imports System
Class Goo
    $$
End Class]]></document>

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ColonDoesntTriggerSnippetInTupleLiteral() As Task
            Using state = CreateVisualBasicSnippetExpansionNoteTestState(_markup, "Interface")
                state.SendTypeChars("Dim t = (Interfac")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteAddedToDescription_DifferentSnippetShortcutCasing() As Task
            Using state = CreateVisualBasicSnippetExpansionNoteTestState(_markup, "intErfaCE")
                state.SendTypeChars("Interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources._0_Keyword, "Interface") & vbCrLf &
                    VBFeaturesResources.Declares_the_name_of_an_interface_and_the_definitions_of_the_members_of_the_interface & vbCrLf &
                    String.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, "Interface"))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteNotAddedToDescription_ShortcutIsProperSubstringOfInsertedText() As Task
            Using state = CreateVisualBasicSnippetExpansionNoteTestState(_markup, "Interfac")
                state.SendTypeChars("Interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources._0_Keyword, "Interface") & vbCrLf &
                    VBFeaturesResources.Declares_the_name_of_an_interface_and_the_definitions_of_the_members_of_the_interface)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteNotAddedToDescription_ShortcutIsProperSuperstringOfInsertedText() As Task
            Using state = CreateVisualBasicSnippetExpansionNoteTestState(_markup, "Interfaces")
                state.SendTypeChars("Interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources._0_Keyword, "Interface") & vbCrLf &
                    VBFeaturesResources.Declares_the_name_of_an_interface_and_the_definitions_of_the_members_of_the_interface)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteNotAddedToDescription_DisplayTextMatchesShortcutButInsertionTextDoesNot() As Task
            Using state = CreateVisualBasicSnippetExpansionNoteTestState(_markup, "DisplayText")

                state.SendTypeChars("DisplayTex")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:="")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteAddedToDescription_DisplayTextDoesNotMatchShortcutButInsertionTextDoes() As Task
            Using state = CreateVisualBasicSnippetExpansionNoteTestState(_markup, "InsertionText")

                state.SendTypeChars("DisplayTex")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, "InsertionText"))
            End Using
        End Function

        Private Function CreateVisualBasicSnippetExpansionNoteTestState(xElement As XElement, ParamArray snippetShortcuts As String()) As TestState
            Dim state = TestStateFactory.CreateVisualBasicTestState(
                xElement,
                New CompletionProvider() {New MockCompletionProvider()},
                New List(Of Type) From {GetType(TestVisualBasicSnippetInfoService)})

            Dim testSnippetInfoService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetService(Of ISnippetInfoService)(), TestVisualBasicSnippetInfoService)
            testSnippetInfoService.SetSnippetShortcuts(snippetShortcuts)
            Return state
        End Function
    End Class
End Namespace
