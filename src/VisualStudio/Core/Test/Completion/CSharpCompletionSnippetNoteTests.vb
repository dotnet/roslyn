' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Completion
    Public Class CSharpCompletionSnippetNoteTests
        Private _markup As XElement = <document>
                                          <![CDATA[using System;
class C
{
    $$

    void M() { }
}]]></document>

        <WorkItem(726497)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteAddedToDescription_ExactMatch() As Threading.Tasks.Task
            Using state = Await CreateCSharpSnippetExpansionNoteTestState(_markup, "interface").ConfigureAwait(True)
                state.SendTypeChars("interfac")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(description:="title" & vbCrLf &
                    "description" & vbCrLf &
                    String.Format(FeaturesResources.NoteTabTwiceToInsertTheSnippet, "interface")).ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(726497)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteAddedToDescription_DifferentSnippetShortcutCasing() As Threading.Tasks.Task
            Using state = Await CreateCSharpSnippetExpansionNoteTestState(_markup, "intErfaCE").ConfigureAwait(True)
                state.SendTypeChars("interfac")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(description:=$"{String.Format(FeaturesResources.Keyword, "interface")}
{String.Format(FeaturesResources.NoteTabTwiceToInsertTheSnippet, "interface")}").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(726497)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Sub SnippetExpansionNoteNotAddedToDescription_ShortcutIsProperSubstringOfInsertedText()
            Using state = Await CreateCSharpSnippetExpansionNoteTestState(_markup, "interfac").ConfigureAwait(True)
                state.SendTypeChars("interfac")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(description:="title" & vbCrLf &
                    "description" & vbCrLf &
                    String.Format(FeaturesResources.NoteTabTwiceToInsertTheSnippet, "interfac")).ConfigureAwait(True)
            End Using
        End Sub

        <WorkItem(726497)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Sub SnippetExpansionNoteNotAddedToDescription_ShortcutIsProperSuperstringOfInsertedText()
            Using state = Await CreateCSharpSnippetExpansionNoteTestState(_markup, "interfaces").ConfigureAwait(True)
                state.SendTypeChars("interfac")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources.Keyword, "interface")).ConfigureAwait(True)
            End Using
        End Sub

        <WorkItem(726497)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Sub SnippetExpansionNoteAddedToDescription_DisplayTextDoesNotMatchShortcutButInsertionTextDoes()
            Using state = Await CreateCSharpSnippetExpansionNoteTestState(_markup, "InsertionText").ConfigureAwait(True)

                state.SendTypeChars("DisplayTex")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources.NoteTabTwiceToInsertTheSnippet, "InsertionText")).ConfigureAwait(True)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion), Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Async Function SnippetExpansionNoteNotAddedToDescription_Interactive() As Threading.Tasks.Task
            Dim workspaceXml =
                <Workspace>
                    <Submission Language="C#" CommonReferences="true">
                        $$
                    </Submission>
                </Workspace>

            Using state = TestState.CreateTestStateFromWorkspace(
                workspaceXml,
                New CompletionListProvider() {New MockCompletionProvider(New TextSpan(31, 10))},
                Nothing,
                New List(Of Type) From {GetType(TestCSharpSnippetInfoService)},
                WorkspaceKind.Interactive)

                Dim testSnippetInfoService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService(Of ISnippetInfoService)(), TestCSharpSnippetInfoService)
                Await testSnippetInfoService.SetSnippetShortcuts({"for"}).ConfigureAwait(True)

                state.Workspace.Options = state.Workspace.Options.WithChangedOption(InternalFeatureOnOffOptions.Snippets, False)

                state.SendTypeChars("for")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources.Keyword, "for")).ConfigureAwait(True)
            End Using
        End Function

        Private Async Function CreateCSharpSnippetExpansionNoteTestState(xElement As XElement, ParamArray snippetShortcuts As String()) As Task(Of TestState)
            Dim state = TestState.CreateCSharpTestState(
                xElement,
                New CompletionListProvider() {New MockCompletionProvider(New TextSpan(31, 10))},
                Nothing,
                New List(Of Type) From {GetType(TestCSharpSnippetInfoService)})

            Dim testSnippetInfoService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService(Of ISnippetInfoService)(), TestCSharpSnippetInfoService)
            Await testSnippetInfoService.SetSnippetShortcuts(snippetShortcuts).ConfigureAwait(True)

            Return state
        End Function
    End Class
End Namespace
