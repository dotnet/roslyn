' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Roslyn.Utilities
Imports VSCommanding = Microsoft.VisualStudio.Commanding

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Partial Friend Class ModernCompletionTestState
        Inherits TestStateBase

        Private Const timeoutMs = 10000
        Private Const editorTimeoutMs = 20000
        Friend Const RoslynItem = "RoslynItem"
        Private Shared ReadOnly ResponsiveCompletionThreshold As String = NameOf(ResponsiveCompletionThreshold)
        Private Shared ReadOnly ResponsiveCompletionThresholdOption As EditorOptionKey(Of Integer) = New EditorOptionKey(Of Integer)(ResponsiveCompletionThreshold)
        Friend ReadOnly EditorCompletionCommandHandler As VSCommanding.ICommandHandler
        Friend ReadOnly CompletionPresenterProvider As ICompletionPresenterProvider

        ' Do not call directly. Use TestStateFactory
        Friend Sub New(workspaceElement As XElement,
                        extraCompletionProviders As CompletionProvider(),
                        excludedTypes As List(Of Type),
                        extraExportedTypes As List(Of Type),
                        includeFormatCommandHandler As Boolean,
                        workspaceKind As String)

            MyBase.New(
                workspaceElement,
                extraCompletionProviders,
                excludedTypes:=excludedTypes,
                extraExportedTypes,
                includeFormatCommandHandler,
                workspaceKind:=workspaceKind)

            ' The current default timeout defined in the Editor may not work on virtual test machines.
            ' Need to use a safe timeout there.
            TextView.Options.GlobalOptions.SetOptionValue(Of Integer)(ResponsiveCompletionThresholdOption, editorTimeoutMs)

            CompletionPresenterProvider = GetExportedValues(Of ICompletionPresenterProvider)().
                Single(Function(e As ICompletionPresenterProvider) e.GetType().FullName = "Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense.MockCompletionPresenterProvider")
            EditorCompletionCommandHandler = GetExportedValues(Of VSCommanding.ICommandHandler)().
                Single(Function(e As VSCommanding.ICommandHandler) e.GetType().FullName = "Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation.CompletionCommandHandler")
        End Sub

#Region "Editor Related Operations"

        Public Overrides Sub SendEscape()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of EscapeKeyCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of EscapeKeyCommandArgs))
            MyBase.SendEscape(Sub(a, n, c) handler.ExecuteCommand(a, Sub() SignatureHelpAfterCompletionCommandHandler.ExecuteCommand(a, n, c), c), Sub() Return)
        End Sub

        Public Overrides Sub SendDownKey()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of DownKeyCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of DownKeyCommandArgs))
            MyBase.SendDownKey(Sub(a, n, c) handler.ExecuteCommand(a, Sub() SignatureHelpAfterCompletionCommandHandler.ExecuteCommand(a, n, c), c), Sub()
                                                                                                                                                        EditorOperations.MoveLineDown(extendSelection:=False)
                                                                                                                                                    End Sub)
        End Sub

        Public Overrides Sub SendUpKey()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of UpKeyCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of UpKeyCommandArgs))
            MyBase.SendUpKey(Sub(a, n, c) handler.ExecuteCommand(a, Sub() SignatureHelpAfterCompletionCommandHandler.ExecuteCommand(a, n, c), c), Sub()
                                                                                                                                                      EditorOperations.MoveLineUp(extendSelection:=False)
                                                                                                                                                  End Sub)
        End Sub

        Public Overrides Sub SendPageUp()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of PageUpKeyCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of PageUpKeyCommandArgs))
            MyBase.SendPageUp(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendCut()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of CutCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of CutCommandArgs))
            MyBase.SendCut(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendPaste()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of PasteCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of PasteCommandArgs))
            MyBase.SendPaste(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendInvokeCompletionList()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of InvokeCompletionListCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of InvokeCompletionListCommandArgs))
            MyBase.SendInvokeCompletionList(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendInsertSnippetCommand()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of InsertSnippetCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of InsertSnippetCommandArgs))
            MyBase.SendInsertSnippetCommand(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendSurroundWithCommand()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of SurroundWithCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of SurroundWithCommandArgs))
            MyBase.SendSurroundWithCommand(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendSave()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of SaveCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of SaveCommandArgs))
            MyBase.SendSave(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendSelectAll()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of SelectAllCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of SelectAllCommandArgs))
            MyBase.SendSelectAll(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendDeleteWordToLeft()
            Dim compHandler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of WordDeleteToStartCommandArgs))
            MyBase.SendWordDeleteToStart(Sub(a, n, c) compHandler.ExecuteCommand(a, n, c), AddressOf MyBase.SendDeleteWordToLeft)
        End Sub

        Public Overrides Sub ToggleSuggestionMode()
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of ToggleCompletionModeCommandArgs))
            MyBase.ToggleSuggestionMode(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Protected Overrides Function GetHandler(Of T As VSCommanding.ICommandHandler)() As T
            Return DirectCast(EditorCompletionCommandHandler, T)
        End Function

#End Region

#Region "Completion Operations"

        Public Overrides Sub SendCommitUniqueCompletionListItem()
            ' The legacy handler implements VSCommanding.IChainedCommandHandler(Of CommitUniqueCompletionListItemCommandArgs)
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of CommitUniqueCompletionListItemCommandArgs))
            MyBase.SendCommitUniqueCompletionListItem(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Async Function AssertNoCompletionSession() As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            If session Is Nothing Then
                Return
            End If

            If session.IsDismissed Then
                Return
            End If

            Dim completionItems = session.GetComputedItems(CancellationToken.None)
            ' During the computation we can explicitly dismiss the session or we can return no items.
            ' Each of these conditions mean that there is no active completion.
            Assert.True(session.IsDismissed OrElse completionItems.Items.Count() = 0, "AssertNoCompletionSession")
        End Function

        Public Overrides Sub AssertNoCompletionSessionWithNoBlock()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            If session Is Nothing Then
                Return
            End If

            If session.IsDismissed Then
                Return
            End If

            ' If completionItems cannot be calculated in 5 seconds, no session exists.
            Dim task1 = Task.Delay(5000)
            Dim task2 = Task.Run(
                Sub()
                    Dim completionItems = session.GetComputedItems(CancellationToken.None)

                    ' In the non blocking mode, we are not interested for a session appeared later than in 5 seconds.
                    If task1.Status = TaskStatus.Running Then
                        ' During the computation we can explicitly dismiss the session or we can return no items.
                        ' Each of these conditions mean that there is no active completion.
                        Assert.True(session.IsDismissed OrElse completionItems.Items.Count() = 0)
                    End If
                End Sub)

            Task.WaitAny(task1, task2)
        End Sub

        Public Overrides Async Function AssertCompletionSession(Optional projectionsView As ITextView = Nothing) As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim view = If(projectionsView, TextView)

            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(view)
            Assert.True(session IsNot Nothing, "AssertCompletionSession")
        End Function

        Public Overrides Async Function AssertCompletionItemsDoNotContainAny(displayText As String()) As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim items = GetCompletionItems()
            Assert.False(displayText.Any(Function(v) items.Any(Function(i) i.DisplayText = v)))
        End Function

        Public Overrides Async Function AssertCompletionItemsContainAll(displayText As String()) As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim items = GetCompletionItems()
            Assert.True(displayText.All(Function(v) items.Any(Function(i) i.DisplayText = v)))
        End Function

        Public Overrides Async Function AssertCompletionItemsContain(displayText As String, displayTextSuffix As String) As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim items = GetCompletionItems()
            Assert.True(items.Any(Function(i) i.DisplayText = displayText AndAlso i.DisplayTextSuffix = displayTextSuffix))
        End Function

        Public Overrides Async Function AssertCompletionSessionAfterTypingHash() As Task
            ' starting with the modern completion implementation, # is treated as an IntelliSense trigger
            Await AssertCompletionSession()
        End Function

        Public Overrides Sub AssertItemsInOrder(expectedOrder As String())
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim items = session.GetComputedItems(CancellationToken.None).Items
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

            Await WaitForAsynchronousOperationsAsync()
            Dim view = If(projectionsView, TextView)

            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(view)
            Assert.NotNull(session)
            Dim items = session.GetComputedItems(CancellationToken.None)

            If isSoftSelected.HasValue Then
                If isSoftSelected.Value Then
                    Assert.True(items.UsesSoftSelection, "Current completion is not soft-selected. Expected: soft-selected")
                Else
                    Assert.False(items.UsesSoftSelection, "Current completion is soft-selected. Expected: not soft-selected")
                End If
            End If

            If isHardSelected.HasValue Then
                If isHardSelected.Value Then
                    Assert.True(Not items.UsesSoftSelection, "Current completion is not hard-selected. Expected: hard-selected")
                Else
                    Assert.True(items.UsesSoftSelection, "Current completion is hard-selected. Expected: not hard-selected")
                End If
            End If

            If displayText IsNot Nothing Then
                Assert.NotNull(items.SelectedItem)
                If displayTextSuffix IsNot Nothing Then
                    Assert.NotNull(items.SelectedItem)
                    Assert.Equal(displayText + displayTextSuffix, items.SelectedItem.DisplayText)
                Else
                    Assert.Equal(displayText, items.SelectedItem.DisplayText)
                End If
            End If

            If shouldFormatOnCommit.HasValue Then
                Assert.Equal(shouldFormatOnCommit.Value, GetRoslynCompletionItem(items.SelectedItem).Rules.FormatOnCommit)
            End If

            If description IsNot Nothing Then
                Dim document = Me.Workspace.CurrentSolution.Projects.First().Documents.First()
                Dim service = CompletionService.GetService(document)
                Dim roslynItem = GetRoslynCompletionItem(items.SelectedItem)
                Dim itemDescription = Await service.GetDescriptionAsync(document, roslynItem)
                Assert.Equal(description, itemDescription.Text)
            End If

            If inlineDescription IsNot Nothing Then
                Assert.Equal(inlineDescription, items.SelectedItem.Suffix)
            End If

            If automationText IsNot Nothing Then
                Assert.Equal(automationText, items.SelectedItem.AutomationText)
            End If
        End Function

        Public Overrides Async Function AssertSessionIsNothingOrNoCompletionItemLike(text As String) As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            If Not session Is Nothing Then
                Await AssertCompletionItemsDoNotContainAny({text})
            End If
        End Function

        Public Overrides Function GetSelectedItem() As CompletionItem
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim items = session.GetComputedItems(CancellationToken.None)
            Return GetRoslynCompletionItem(items.SelectedItem)
        End Function

        Public Overrides Sub CalculateItemsIfSessionExists()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            If session IsNot Nothing Then
                Dim item = session.GetComputedItems(CancellationToken.None).SelectedItem
            End If
        End Sub

        Public Overrides Function GetCompletionItems() As IList(Of CompletionItem)
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Return session.GetComputedItems(CancellationToken.None).Items.Select(Function(item) GetRoslynCompletionItem(item)).ToList()
        End Function

        Private Shared Function GetRoslynCompletionItem(item As Data.CompletionItem) As CompletionItem
            Return If(item IsNot Nothing, DirectCast(item.Properties(RoslynItem), CompletionItem), Nothing)
        End Function

        Public Overrides Sub RaiseFiltersChanged(args As CompletionItemFilterStateChangedEventArgs)
            Dim presenter = DirectCast(CompletionPresenterProvider.GetOrCreate(Me.TextView), MockCompletionPresenter)
            Dim newArray = args.FilterState.Select(Function(f) New Data.CompletionFilterWithState(New Data.CompletionFilter(f.Key.DisplayText, f.Key.AccessKey, image:=Nothing), isAvailable:=True, isSelected:=f.Value)).ToImmutableArrayOrEmpty()
            Dim newArgs = New Data.CompletionFilterChangedEventArgs(newArray)
            presenter.TriggerFiltersChanged(Me, newArgs)
        End Sub

        Public Overrides Function GetCompletionItemFilters() As ImmutableArray(Of CompletionItemFilter)
            Dim presenter = DirectCast(CompletionPresenterProvider.GetOrCreate(Me.TextView), MockCompletionPresenter)
            Return presenter.GetFilters().Select(Function(f) New CompletionItemFilter(f.Filter.DisplayText, "", f.Filter.AccessKey(0))).ToImmutableArray()
        End Function

        Public Overrides Sub AssertCompletionItemExpander(isAvailable As Boolean, isSelected As Boolean)
            Dim presenter = DirectCast(CompletionPresenterProvider.GetOrCreate(Me.TextView), MockCompletionPresenter)
            Dim expander = presenter.GetExpander()
            If Not isAvailable Then
                Assert.False(isSelected)
                Assert.Null(expander)
            Else
                Assert.NotNull(expander)
                Assert.Equal(expander.IsSelected, isSelected)
            End If
        End Sub

        Public Overrides Sub SetCompletionItemExpanderState(isSelected As Boolean)
            Dim presenter = DirectCast(CompletionPresenterProvider.GetOrCreate(Me.TextView), MockCompletionPresenter)
            Dim expander = presenter.GetExpander()
            Assert.NotNull(expander)
            presenter.SetExpander(isSelected)
        End Sub

        Public Overrides Function HasSuggestedItem() As Boolean
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim computedItems = session.GetComputedItems(CancellationToken.None)
            Return computedItems.SuggestionItem IsNot Nothing
        End Function

        Public Overrides Function IsSoftSelected() As Boolean
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim computedItems = session.GetComputedItems(CancellationToken.None)
            Return computedItems.UsesSoftSelection
        End Function

        Public Overrides Sub SendSelectCompletionItem(displayText As String)
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Dim operations = DirectCast(session, IAsyncCompletionSessionOperations)
            operations.SelectCompletionItem(session.GetComputedItems(CancellationToken.None).Items.Single(Function(i) i.DisplayText = displayText))
        End Sub

        Public Overrides Sub SendSelectCompletionItemThroughPresenterSession(item As CompletionItem)
            Throw ExceptionUtilities.Unreachable
        End Sub

        Public Overrides Async Function WaitForUIRenderedAsync() As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim tcs = New TaskCompletionSource(Of Boolean)
            Dim presenter = DirectCast(CompletionPresenterProvider.GetOrCreate(TextView), MockCompletionPresenter)
            Dim uiUpdated As EventHandler(Of Data.CompletionItemSelectedEventArgs)

            uiUpdated = Sub()
                            RemoveHandler presenter.UiUpdated, uiUpdated
                            tcs.TrySetResult(True)
                        End Sub

            AddHandler presenter.UiUpdated, uiUpdated
            Dim ct = New CancellationTokenSource(timeoutMs)
            ct.Token.Register(Sub() tcs.TrySetCanceled(), useSynchronizationContext:=False)

            Await tcs.Task.ConfigureAwait(True)
        End Function

#End Region
    End Class
End Namespace
