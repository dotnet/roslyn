' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Snippets
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Snippets
    <[UseExportProvider]>
    Public Class SnippetCompletionProviderTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function SnippetCompletion() As Task
            Dim markup = "a?$$"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic, extraParts:={GetType(MockSnippetInfoService)})
            Using testState
                testState.SendTabToCompletion()

                Assert.Equal("a", testState.GetDocumentText())

                Await testState.WaitForAsynchronousOperationsAsync()
                Await testState.AssertSelectedCompletionItem(displayText:="Shortcut")

                Dim document = testState.Workspace.CurrentSolution.Projects.First().Documents.First()
                Dim service = CompletionService.GetService(document)
                Await testState.AssertSelectedCompletionItem(description:="Shortcut")
                Dim selectedItem = testState.GetSelectedItem()
                Dim itemDescription = Await service.GetDescriptionAsync(document, selectedItem)
                Assert.True(itemDescription.Text.StartsWith("Description"))

                testState.SendTabToCompletion()

                Assert.True(testState.SnippetExpansionClient.TryInsertExpansionCalled)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function TracksChangeSpanCorrectly() As Task
            Dim markup = "a?$$"
            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic, extraParts:={GetType(MockSnippetInfoService)})
            Using testState
                testState.SendTabToCompletion()
                Await testState.WaitForAsynchronousOperationsAsync()
                Await testState.AssertSelectedCompletionItem(displayText:="Shortcut")

                testState.SendBackspace()
                Await testState.AssertSelectedCompletionItem(displayText:="Shortcut")

                testState.SendTabToCompletion()

                Await testState.WaitForAsynchronousOperationsAsync()
                Assert.True(testState.SnippetExpansionClient.TryInsertExpansionCalled)
                Assert.Equal("Shortcut", testState.GetDocumentText())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function SnippetListOnlyIfTextBeforeQuestionMark() As Task
            Dim markup = <File>
Class C
    ?$$
End Class</File>.Value

            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic, extraParts:={GetType(MockSnippetInfoService)})
            Using testState
                testState.SendTabToCompletion()
                Await testState.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(21801, "https://github.com/dotnet/roslyn/issues/21801")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function SnippetNotOfferedInComments() As Task
            Dim markup = <File>
Class C
    $$
End Class</File>.Value

            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic, extraParts:={GetType(MockSnippetInfoService)})
            Using testState
                testState.Workspace.Options = testState.Workspace.Options.WithChangedOption(
                    New Options.OptionKey(CompletionOptions.SnippetsBehavior, LanguageNames.VisualBasic), SnippetsRule.AlwaysInclude)
                testState.SendTypeChars("'T")
                Await testState.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(21801, "https://github.com/dotnet/roslyn/issues/21801")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function SnippetsNotOfferedInDocComments() As Task
            Dim markup = <File>
Class C
    $$
End Class</File>.Value

            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic, extraParts:={GetType(MockSnippetInfoService)})
            Using testState
                testState.Workspace.Options = testState.Workspace.Options.WithChangedOption(
                    New Options.OptionKey(CompletionOptions.SnippetsBehavior, LanguageNames.VisualBasic), SnippetsRule.AlwaysInclude)
                testState.SendTypeChars("'''T")
                Await testState.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Snippets)>
        Public Async Function SnippetsAlwaysOfferedOutsideComment() As Task
            Dim markup = <File>
Class C
    $$
End Class</File>.Value

            Dim testState = SnippetTestState.CreateTestState(markup, LanguageNames.VisualBasic, extraParts:={GetType(MockSnippetInfoService)})
            Using testState
                testState.Workspace.Options = testState.Workspace.Options.WithChangedOption(
                    New Options.OptionKey(CompletionOptions.SnippetsBehavior, LanguageNames.VisualBasic), SnippetsRule.AlwaysInclude)
                testState.SendTypeChars("Shortcut")
                Await testState.AssertSelectedCompletionItem(displayText:="Shortcut")
            End Using
        End Function
    End Class

    <ExportLanguageService(GetType(ISnippetInfoService), LanguageNames.VisualBasic), [Shared]>
    Friend Class MockSnippetInfoService
        Implements ISnippetInfoService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Function GetSnippetsAsync_NonBlocking() As IEnumerable(Of SnippetInfo) Implements ISnippetInfoService.GetSnippetsIfAvailable
            Return SpecializedCollections.SingletonEnumerable(New SnippetInfo("Shortcut", "Title", "Description", "Path"))
        End Function

        Public Function ShouldFormatSnippet(snippetInfo As SnippetInfo) As Boolean Implements ISnippetInfoService.ShouldFormatSnippet
            Return False
        End Function

        Public Function SnippetShortcutExists_NonBlocking(shortcut As String) As Boolean Implements ISnippetInfoService.SnippetShortcutExists_NonBlocking
            Return shortcut = "Shortcut"
        End Function
    End Class
End Namespace

