' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Completion
    Public Class VisualBasicCompletionSnippetNoteTests
        Private _markup As XElement = <document>
                                          <![CDATA[Imports System
Class Foo
    $$
End Class]]></document>

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteAddedToDescription_ExactMatch() As Task
            Using state = Await CreateVisualBasicSnippetExpansionNoteTestState(_markup, "Interface")
                state.SendTypeChars("Interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources.Keyword, "Interface") & vbCrLf &
                    VBFeaturesResources.InterfaceKeywordToolTip & vbCrLf &
                    String.Format(FeaturesResources.NoteTabTwiceToInsertTheSnippet, "Interface"))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteAddedToDescription_DifferentSnippetShortcutCasing() As Task
            Using state = Await CreateVisualBasicSnippetExpansionNoteTestState(_markup, "intErfaCE")
                state.SendTypeChars("Interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources.Keyword, "Interface") & vbCrLf &
                    VBFeaturesResources.InterfaceKeywordToolTip & vbCrLf &
                    String.Format(FeaturesResources.NoteTabTwiceToInsertTheSnippet, "Interface"))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteNotAddedToDescription_ShortcutIsProperSubstringOfInsertedText() As Task
            Using state = Await CreateVisualBasicSnippetExpansionNoteTestState(_markup, "Interfac")
                state.SendTypeChars("Interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources.Keyword, "Interface") & vbCrLf &
                    VBFeaturesResources.InterfaceKeywordToolTip)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteNotAddedToDescription_ShortcutIsProperSuperstringOfInsertedText() As Task
            Using state = Await CreateVisualBasicSnippetExpansionNoteTestState(_markup, "Interfaces")
                state.SendTypeChars("Interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources.Keyword, "Interface") & vbCrLf &
                    VBFeaturesResources.InterfaceKeywordToolTip)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteNotAddedToDescription_DisplayTextMatchesShortcutButInsertionTextDoesNot() As Task
            Using state = Await CreateVisualBasicSnippetExpansionNoteTestState(_markup, "DisplayText")

                state.SendTypeChars("DisplayTex")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:="")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteAddedToDescription_DisplayTextDoesNotMatchShortcutButInsertionTextDoes() As Task
            Using state = Await CreateVisualBasicSnippetExpansionNoteTestState(_markup, "InsertionText")

                state.SendTypeChars("DisplayTex")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources.NoteTabTwiceToInsertTheSnippet, "InsertionText"))
            End Using
        End Function

        Private Async Function CreateVisualBasicSnippetExpansionNoteTestState(xElement As XElement, ParamArray snippetShortcuts As String()) As Task(Of TestState)
            Dim state = TestState.CreateVisualBasicTestState(
                xElement,
                New CompletionListProvider() {New MockCompletionProvider(New TextSpan(31, 10))},
                Nothing,
                New List(Of Type) From {GetType(TestVisualBasicSnippetInfoService)})

            Dim testSnippetInfoService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetService(Of ISnippetInfoService)(), TestVisualBasicSnippetInfoService)
            Await testSnippetInfoService.SetSnippetShortcuts(snippetShortcuts)

            Return state
        End Function
    End Class
End Namespace
