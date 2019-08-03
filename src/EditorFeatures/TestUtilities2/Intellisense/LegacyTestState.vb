' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Utilities
Imports Roslyn.Utilities
Imports VSCommanding = Microsoft.VisualStudio.Commanding

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Partial Friend Class LegacyTestState
        Inherits TestStateBase

        Friend ReadOnly CompletionCommandHandler As CompletionCommandHandler

        Friend ReadOnly Property CurrentCompletionPresenterSession As TestCompletionPresenterSession
            Get
                Return SessionTestState.CurrentCompletionPresenterSession
            End Get
        End Property

        ' Do not call directly. Use TestStateFactory
        Friend Sub New(workspaceElement As XElement,
                       extraCompletionProviders As CompletionProvider(),
                       excludedTypes As List(Of Type),
                       extraExportedTypes As List(Of Type),
                       includeFormatCommandHandler As Boolean,
                       workspaceKind As String)
            MyBase.New(workspaceElement, extraCompletionProviders, excludedTypes, extraExportedTypes, includeFormatCommandHandler, workspaceKind)

            Me.CompletionCommandHandler = GetExportedValue(Of CompletionCommandHandler)()

            Dim featureServiceFactory = GetExportedValue(Of IFeatureServiceFactory)()
            featureServiceFactory.GlobalFeatureService.Disable(PredefinedEditorFeatureNames.AsyncCompletion, EmptyFeatureController.Instance)
        End Sub

#Region "Editor Related Operations"

        Public Overrides Sub SendEscape()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of EscapeKeyCommandArgs))
            MyBase.SendEscape(Sub(a, n, c) handler.ExecuteCommand(a, Sub() SignatureHelpAfterCompletionCommandHandler.ExecuteCommand(a, n, c), c), Sub() Return)
        End Sub

        Public Overrides Sub SendDownKey()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of DownKeyCommandArgs))
            MyBase.SendDownKey(Sub(a, n, c) handler.ExecuteCommand(a, Sub() SignatureHelpAfterCompletionCommandHandler.ExecuteCommand(a, n, c), c), Sub()
                                                                                                                                                        EditorOperations.MoveLineDown(extendSelection:=False)
                                                                                                                                                    End Sub)
        End Sub

        Public Overrides Sub SendUpKey()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of UpKeyCommandArgs))
            MyBase.SendUpKey(Sub(a, n, c) handler.ExecuteCommand(a, Sub() SignatureHelpAfterCompletionCommandHandler.ExecuteCommand(a, n, c), c), Sub()
                                                                                                                                                      EditorOperations.MoveLineUp(extendSelection:=False)
                                                                                                                                                  End Sub)
        End Sub

        Public Overrides Sub SendPageUp()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of PageUpKeyCommandArgs))
            MyBase.SendPageUp(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendCut()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of CutCommandArgs))
            MyBase.SendCut(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendPaste()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of PasteCommandArgs))
            MyBase.SendPaste(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendInvokeCompletionList()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of InvokeCompletionListCommandArgs))
            MyBase.SendInvokeCompletionList(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub
        Public Overrides Sub SendInsertSnippetCommand()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of InsertSnippetCommandArgs))
            MyBase.SendInsertSnippetCommand(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendSurroundWithCommand()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of SurroundWithCommandArgs))
            MyBase.SendSurroundWithCommand(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendSave()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of SaveCommandArgs))
            MyBase.SendSave(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendSelectAll()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of SelectAllCommandArgs))
            MyBase.SendSelectAll(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub ToggleSuggestionMode()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of ToggleCompletionModeCommandArgs))
            MyBase.ToggleSuggestionMode(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Protected Overrides Function GetHandler(Of T As VSCommanding.ICommandHandler)() As T
            Return DirectCast(DirectCast(CompletionCommandHandler, VSCommanding.ICommandHandler), T)
        End Function
#End Region

#Region "Completion Operations"

        Public Overrides Function GetSelectedItem() As CompletionItem
            Return CurrentCompletionPresenterSession.SelectedItem
        End Function

        Public Overrides Sub CalculateItemsIfSessionExists()
            Throw ExceptionUtilities.Unreachable
        End Sub

        Public Overrides Function GetCompletionItems() As IList(Of CompletionItem)
            Return CurrentCompletionPresenterSession.CompletionItems
        End Function

        Public Overrides Sub RaiseFiltersChanged(args As CompletionItemFilterStateChangedEventArgs)
            CurrentCompletionPresenterSession.RaiseFiltersChanged(args)
        End Sub

        Public Overrides Function GetCompletionItemFilters() As ImmutableArray(Of CompletionItemFilter)
            Return CurrentCompletionPresenterSession.CompletionItemFilters
        End Function

        Public Overrides Function HasSuggestedItem() As Boolean
            ' SuggestionModeItem is always not null but is displayed only when SuggestionMode = True
            Return CurrentCompletionPresenterSession.SuggestionMode
        End Function

        Public Overrides Function IsSoftSelected() As Boolean
            Return CurrentCompletionPresenterSession.IsSoftSelected
        End Function

        Public Overrides Sub SendCommitUniqueCompletionListItem()
            Dim handler = DirectCast(CompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of CommitUniqueCompletionListItemCommandArgs))
            MyBase.SendCommitUniqueCompletionListItem(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub
        Public Overrides Sub SendSelectCompletionItem(displayText As String)
            Dim item = CurrentCompletionPresenterSession.CompletionItems.FirstOrDefault(Function(i) i.DisplayText = displayText)
            Assert.NotNull(item)
            CurrentCompletionPresenterSession.SetSelectedItem(item)
        End Sub

        Public Overrides Sub SendSelectCompletionItemThroughPresenterSession(item As CompletionItem)
            CurrentCompletionPresenterSession.SetSelectedItem(item)
        End Sub

        Public Overrides Async Function AssertNoCompletionSession() As Task
            Await WaitForAsynchronousOperationsAsync()
            Assert.Null(Me.CurrentCompletionPresenterSession)
        End Function

        Public Overrides Sub AssertNoCompletionSessionWithNoBlock()
            Assert.Null(Me.CurrentCompletionPresenterSession)
        End Sub

        Public Overrides Async Function AssertCompletionSessionAfterTypingHash() As Task
            ' The legacy completion implementation was not updated to treat # as an IntelliSense trigger
            Await AssertNoCompletionSession()
        End Function

        Public Overrides Async Function AssertCompletionSession(Optional projectionsView As ITextView = Nothing) As Task
            ' projectionsView is not used in this implementation
            Await WaitForAsynchronousOperationsAsync()
            Assert.NotNull(Me.CurrentCompletionPresenterSession)
        End Function

        Public Overrides Sub AssertItemsInOrder(expectedOrder As String())
            AssertNoAsynchronousOperationsRunning()
            Dim items = CurrentCompletionPresenterSession.CompletionItems
            Assert.Equal(expectedOrder.Count, items.Count)
            For i = 0 To expectedOrder.Count - 1
                Assert.Equal(expectedOrder(i), items(i).DisplayText)
            Next
        End Sub

        Public Overrides Async Function AssertSelectedCompletionItem(
                               Optional displayText As String = Nothing,
                               Optional displayTextSuffix As String = Nothing,
                               Optional description As String = Nothing,
                               Optional isSoftSelected As Boolean? = Nothing,
                               Optional isHardSelected As Boolean? = Nothing,
                               Optional shouldFormatOnCommit As Boolean? = Nothing,
                               Optional inlineDescription As String = Nothing,
                               Optional automationText As String = Nothing,
                               Optional projectionsView As ITextView = Nothing) As Task
            ' projectionsView is not used in this implementation.

            Await WaitForAsynchronousOperationsAsync()
            If isSoftSelected.HasValue Then
                Assert.True(isSoftSelected.Value = Me.CurrentCompletionPresenterSession.IsSoftSelected, "Current completion is not soft-selected.")
            End If

            If isHardSelected.HasValue Then
                Assert.True(isHardSelected.Value = Not Me.CurrentCompletionPresenterSession.IsSoftSelected, "Current completion is not hard-selected.")
            End If

            If displayText IsNot Nothing Then
                Assert.Equal(displayText, Me.CurrentCompletionPresenterSession.SelectedItem.DisplayText)
            End If

            If displayTextSuffix IsNot Nothing Then
                Assert.Equal(displayTextSuffix, Me.CurrentCompletionPresenterSession.SelectedItem.DisplayTextSuffix)
            End If

            If inlineDescription IsNot Nothing Then
                Assert.Equal(inlineDescription, Me.CurrentCompletionPresenterSession.SelectedItem.InlineDescription)
            End If

            If shouldFormatOnCommit.HasValue Then
                Assert.Equal(shouldFormatOnCommit.Value, Me.CurrentCompletionPresenterSession.SelectedItem.Rules.FormatOnCommit)
            End If

            If description IsNot Nothing Then
                Dim document = Me.Workspace.CurrentSolution.Projects.First().Documents.First()
                Dim service = CompletionService.GetService(document)
                Dim itemDescription = Await service.GetDescriptionAsync(
                    document, Me.CurrentCompletionPresenterSession.SelectedItem)
                Assert.Equal(description, itemDescription.Text)
            End If

            ' AutomationText property is only supported by Modern Completion
            Assert.Null(automationText)
        End Function

        Public Overrides Function AssertSessionIsNothingOrNoCompletionItemLike(text As String) As Task
            If Not CurrentCompletionPresenterSession Is Nothing Then
                AssertCompletionItemsDoNotContainAny({text})
            End If

            Return Task.CompletedTask
        End Function

        Public Overrides Async Function WaitForUIRenderedAsync() As Task
            Await WaitForAsynchronousOperationsAsync()
        End Function

#End Region
    End Class
End Namespace
