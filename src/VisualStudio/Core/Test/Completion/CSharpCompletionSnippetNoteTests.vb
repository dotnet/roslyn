' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
        Private ReadOnly _markup As XElement = <document>
                                                   <![CDATA[using System;
class C
{
    $$

    void M() { }
}]]></document>

        <WorkItem(726497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/726497")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteAddedToDescription_ExactMatch() As Task
            Using state = CreateCSharpSnippetExpansionNoteTestState(_markup, "interface")
                state.SendTypeChars("interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:="title" & vbCrLf &
                    "description" & vbCrLf &
                    String.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, "interface"))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function ColonDoesntTriggerSnippetInTupleLiteral() As Task
            Using state = CreateCSharpSnippetExpansionNoteTestState(_markup, "interface")
                state.SendTypeChars("var t = (interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="interface", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(interfac:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteAddedToDescription_DifferentSnippetShortcutCasing() As Task
            Using state = CreateCSharpSnippetExpansionNoteTestState(_markup, "intErfaCE")
                state.SendTypeChars("interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=$"{String.Format(FeaturesResources._0_Keyword, "interface")}
{String.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, "interface")}")
            End Using
        End Function

        <WorkItem(726497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/726497")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteNotAddedToDescription_ShortcutIsProperSubstringOfInsertedText() As Task
            Using state = CreateCSharpSnippetExpansionNoteTestState(_markup, "interfac")
                state.SendTypeChars("interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:="title" & vbCrLf &
                    "description" & vbCrLf &
                    String.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, "interfac"))
            End Using
        End Function

        <WorkItem(726497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/726497")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteNotAddedToDescription_ShortcutIsProperSuperstringOfInsertedText() As Task
            Using state = CreateCSharpSnippetExpansionNoteTestState(_markup, "interfaces")
                state.SendTypeChars("interfac")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources._0_Keyword, "interface"))
            End Using
        End Function

        <WorkItem(726497, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/726497")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SnippetExpansionNoteAddedToDescription_DisplayTextDoesNotMatchShortcutButInsertionTextDoes() As Task
            Using state = CreateCSharpSnippetExpansionNoteTestState(_markup, "InsertionText")

                state.SendTypeChars("DisplayTex")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(description:=String.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, "InsertionText"))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion), Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Async Function SnippetExpansionNoteNotAddedToDescription_Interactive() As Task
            Dim workspaceXml =
                <Workspace>
                    <Submission Language="C#" CommonReferences="true">
                        $$
                    </Submission>
                </Workspace>

            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                workspaceXml,
                New List(Of Type) From {GetType(CSharpMockCompletionProvider), GetType(TestCSharpSnippetInfoService)},
                WorkspaceKind.Interactive)

                Dim testSnippetInfoService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService(Of ISnippetInfoService)(), TestCSharpSnippetInfoService)
                testSnippetInfoService.SetSnippetShortcuts({"for"})

                Dim workspace = state.Workspace
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options _
                    .WithChangedOption(InternalFeatureOnOffOptions.Snippets, False)))

                state.SendTypeChars("for")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(
                    description:=String.Format(FeaturesResources._0_Keyword, "for") & vbCrLf &
                                 String.Format(FeaturesResources.Note_colon_Tab_twice_to_insert_the_0_snippet, "for"))
            End Using
        End Function

        Private Shared Function CreateCSharpSnippetExpansionNoteTestState(xElement As XElement, ParamArray snippetShortcuts As String()) As TestState
            Dim state = TestStateFactory.CreateCSharpTestState(
                xElement,
                extraExportedTypes:=New List(Of Type) From {GetType(CSharpMockCompletionProvider), GetType(TestCSharpSnippetInfoService)})

            Dim testSnippetInfoService = DirectCast(state.Workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService(Of ISnippetInfoService)(), TestCSharpSnippetInfoService)
            testSnippetInfoService.SetSnippetShortcuts(snippetShortcuts)
            Return state
        End Function
    End Class
End Namespace
