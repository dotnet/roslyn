' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.Shared.Options
Imports Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Completion
    <[UseExportProvider]>
    Public Class CSharpCompletionSnippetNoteTests
        Private _markup As XElement = <document>
                                          <![CDATA[using System;
class C
{
    $$

    void M() { }
}]]></document>

        <WorkItem(726497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/726497")>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteAddedToDescription_ExactMatch() As Task
            Using state = CreateCSharpSnippetExpansionNoteTestState(_markup, "interface")
                state.SendTypeChars("interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:="title" & vbCrLf &
                    "description" & vbCrLf &
                    String.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, "interface"))
            End Using
        End Function

        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ColonDoesntTriggerSnippetInTupleLiteral() As Task
            Using state = CreateCSharpSnippetExpansionNoteTestState(_markup, "interface")
                state.SendTypeChars("var t = (interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="interface", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(interfac:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ColonDoesntTriggerSnippetInTupleLiteralAfterComma() As Task
            Using state = CreateCSharpSnippetExpansionNoteTestState(_markup, "interface")
                state.SendTypeChars("var t = (1, interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="interface", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(1, interfac:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(726497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/726497")>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteAddedToDescription_DifferentSnippetShortcutCasing() As Task
            Using state = CreateCSharpSnippetExpansionNoteTestState(_markup, "intErfaCE")
                state.SendTypeChars("interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=$"{String.Format(FeaturesResources._0_Keyword, "interface")}
{String.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, "interface")}")
            End Using
        End Function

        <WorkItem(726497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/726497")>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Sub SnippetExpansionNoteNotAddedToDescription_ShortcutIsProperSubstringOfInsertedText()
            Using state = CreateCSharpSnippetExpansionNoteTestState(_markup, "interfac")
                state.SendTypeChars("interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:="title" & vbCrLf &
                    "description" & vbCrLf &
                    String.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, "interfac"))
            End Using
        End Sub

        <WorkItem(726497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/726497")>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Sub SnippetExpansionNoteNotAddedToDescription_ShortcutIsProperSuperstringOfInsertedText()
            Using state = CreateCSharpSnippetExpansionNoteTestState(_markup, "interfaces")
                state.SendTypeChars("interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources._0_Keyword, "interface"))
            End Using
        End Sub

        <WorkItem(726497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/726497")>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Sub SnippetExpansionNoteAddedToDescription_DisplayTextDoesNotMatchShortcutButInsertionTextDoes()
            Using state = CreateCSharpSnippetExpansionNoteTestState(_markup, "InsertionText")

                state.SendTypeChars("DisplayTex")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, "InsertionText"))
            End Using
        End Sub

        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion), Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Async Function SnippetExpansionNoteNotAddedToDescription_Interactive() As Task
            Dim workspaceXml =
                <Workspace>
                    <Submission Language="C#" CommonReferences="true">
                        $$
                    </Submission>
                </Workspace>

            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                workspaceXml,
                New CompletionProvider() {New MockCompletionProvider()},
                New List(Of Type) From {GetType(TestCSharpSnippetInfoService)},
                WorkspaceKind.Interactive)

                Dim testSnippetInfoService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService(Of ISnippetInfoService)(), TestCSharpSnippetInfoService)
                testSnippetInfoService.SetSnippetShortcuts({"for"})

                state.Workspace.Options = state.Workspace.Options.WithChangedOption(InternalFeatureOnOffOptions.Snippets, False)

                state.SendTypeChars("for")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(
                    description:=String.Format(FeaturesResources._0_Keyword, "for") & vbCrLf &
                                 String.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, "for"))
            End Using
        End Function

        Private Function CreateCSharpSnippetExpansionNoteTestState(xElement As XElement, ParamArray snippetShortcuts As String()) As TestState
            Dim state = TestStateFactory.CreateCSharpTestState(
                xElement,
                New CompletionProvider() {New MockCompletionProvider()},
                extraExportedTypes:=New List(Of Type) From {GetType(TestCSharpSnippetInfoService)})

            Dim testSnippetInfoService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService(Of ISnippetInfoService)(), TestCSharpSnippetInfoService)
            testSnippetInfoService.SetSnippetShortcuts(snippetShortcuts)
            Return state
        End Function
    End Class
End Namespace
