' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend Class TestState
        Inherits AbstractCommandHandlerTestState

        Private Const timeoutMs = 60000
        Friend ReadOnly EditorCompletionCommandHandler As ICommandHandler
        Friend ReadOnly CompletionPresenterProvider As ICompletionPresenterProvider

        Protected ReadOnly SessionTestState As IIntelliSenseTestState
        Private ReadOnly SignatureHelpBeforeCompletionCommandHandler As SignatureHelpBeforeCompletionCommandHandler
        Protected ReadOnly SignatureHelpAfterCompletionCommandHandler As SignatureHelpAfterCompletionCommandHandler
        Protected ReadOnly CompleteStatementCommandHandler As CompleteStatementCommandHandler
        Private ReadOnly FormatCommandHandler As FormatCommandHandler

        Public Shared ReadOnly CompositionWithoutCompletionTestParts As TestComposition = EditorTestCompositions.EditorFeaturesWpf.
            AddExcludedPartTypes(
                GetType(IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession)),
                GetType(FormatCommandHandler)).
            AddParts(
                GetType(TestSignatureHelpPresenter),
                GetType(IntelliSenseTestState),
                GetType(MockCompletionPresenterProvider))

        Friend ReadOnly Property CurrentSignatureHelpPresenterSession As TestSignatureHelpPresenterSession
            Get
                Return SessionTestState.CurrentSignatureHelpPresenterSession
            End Get
        End Property

        ' Do not call directly. Use TestStateFactory
        Friend Sub New(workspaceElement As XElement,
                       excludedTypes As IEnumerable(Of Type),
                       extraExportedTypes As IEnumerable(Of Type),
                       includeFormatCommandHandler As Boolean,
                       workspaceKind As String,
                       Optional makeSeparateBufferForCursor As Boolean = False,
                       Optional roles As ImmutableArray(Of String) = Nothing)
            MyBase.New(workspaceElement, GetComposition(excludedTypes, extraExportedTypes, includeFormatCommandHandler), workspaceKind:=workspaceKind, makeSeparateBufferForCursor, roles)

            ' Disable editor's responsive completion option to ensure a deterministic test behavior
            MyBase.TextView.Options.GlobalOptions.SetOptionValue(DefaultOptions.ResponsiveCompletionOptionId, False)
            MyBase.TextView.Options.GlobalOptions.SetOptionValue(DefaultOptions.IndentStyleId, IndentingStyle.Smart)

            Dim language = Me.Workspace.CurrentSolution.Projects.First().Language

            Me.SessionTestState = GetExportedValue(Of IIntelliSenseTestState)()

            Me.SignatureHelpBeforeCompletionCommandHandler = GetExportedValue(Of SignatureHelpBeforeCompletionCommandHandler)()

            Me.SignatureHelpAfterCompletionCommandHandler = GetExportedValue(Of SignatureHelpAfterCompletionCommandHandler)()
            Me.CompleteStatementCommandHandler = GetExportedValue(Of CompleteStatementCommandHandler)()

            Me.FormatCommandHandler = If(includeFormatCommandHandler, GetExportedValue(Of FormatCommandHandler)(), Nothing)
            Me.CompleteStatementCommandHandler = Workspace.ExportProvider.GetCommandHandler(Of CompleteStatementCommandHandler)(NameOf(CompleteStatementCommandHandler))

            CompletionPresenterProvider = GetExportedValues(Of ICompletionPresenterProvider)().
                Single(Function(e As ICompletionPresenterProvider) e.GetType().FullName = "Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense.MockCompletionPresenterProvider")
            EditorCompletionCommandHandler = GetExportedValues(Of ICommandHandler)().
                Single(Function(e As ICommandHandler) e.GetType().Name = PredefinedCompletionNames.CompletionCommandHandler)
        End Sub

        Private Overloads Shared Function GetComposition(
            excludedTypes As IEnumerable(Of Type),
            extraExportedTypes As IEnumerable(Of Type),
            includeFormatCommandHandler As Boolean) As TestComposition

            Dim composition = CompositionWithoutCompletionTestParts.
                AddExcludedPartTypes(excludedTypes).
                AddParts(extraExportedTypes)

            If includeFormatCommandHandler Then
                ' FormatCommandHandler would generally be included in the catalog, but is excluded from tests by adding
                ' it to the list of excluded part types. Here we validate the input state and restore the default
                ' behavior of the catalog by removing FormatCommandHandler from the excluded parts list.
                Assert.Contains(GetType(FormatCommandHandler).Assembly, composition.Assemblies)
                Assert.Contains(GetType(FormatCommandHandler), composition.ExcludedPartTypes)
                composition = composition.RemoveExcludedPartTypes(GetType(FormatCommandHandler))
            End If

            Return composition
        End Function

#Region "Editor Related Operations"

        Protected Overloads Sub ExecuteTypeCharCommand(args As TypeCharCommandArgs, finalHandler As Action, context As CommandExecutionContext, completionCommandHandler As IChainedCommandHandler(Of TypeCharCommandArgs))
            Dim sigHelpHandler = DirectCast(SignatureHelpBeforeCompletionCommandHandler, IChainedCommandHandler(Of TypeCharCommandArgs))
            Dim formatHandler = DirectCast(FormatCommandHandler, IChainedCommandHandler(Of TypeCharCommandArgs))

            If formatHandler Is Nothing Then
                sigHelpHandler.ExecuteCommand(
                    args, Sub() completionCommandHandler.ExecuteCommand(
                                    args, Sub() CompleteStatementCommandHandler.ExecuteCommand(args, finalHandler, context), context), context)
            Else
                formatHandler.ExecuteCommand(
                    args, Sub() sigHelpHandler.ExecuteCommand(
                                    args, Sub() completionCommandHandler.ExecuteCommand(
                                                    args, Sub() CompleteStatementCommandHandler.ExecuteCommand(args, finalHandler, context), context), context), context)
            End If
        End Sub

        Public Overloads Sub SendTab()
            Dim handler = GetHandler(Of IChainedCommandHandler(Of TabKeyCommandArgs))()
            MyBase.SendTab(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() EditorOperations.InsertText(vbTab))
        End Sub

        Public Overloads Sub SendReturn()
            Dim handler = GetHandler(Of IChainedCommandHandler(Of ReturnKeyCommandArgs))()
            MyBase.SendReturn(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() EditorOperations.InsertNewLine())
        End Sub

        Public Sub SendBackspaces(count As Integer)
            For i = 0 To count - 1
                Me.SendBackspace()
            Next
        End Sub

        Public Overrides Sub SendBackspace()
            Dim compHandler = GetHandler(Of IChainedCommandHandler(Of BackspaceKeyCommandArgs))()
            MyBase.SendBackspace(Sub(a, n, c) compHandler.ExecuteCommand(a, n, c), AddressOf MyBase.SendBackspace)
        End Sub

        Public Overrides Sub SendDelete()
            Dim compHandler = GetHandler(Of IChainedCommandHandler(Of DeleteKeyCommandArgs))()
            MyBase.SendDelete(Sub(a, n, c) compHandler.ExecuteCommand(a, n, c), AddressOf MyBase.SendDelete)
        End Sub

        Public Sub SendDeleteToSpecificViewAndBuffer(view As IWpfTextView, buffer As ITextBuffer)
            Dim compHandler = GetHandler(Of IChainedCommandHandler(Of DeleteKeyCommandArgs))()
            compHandler.ExecuteCommand(New DeleteKeyCommandArgs(view, buffer), AddressOf MyBase.SendDelete, TestCommandExecutionContext.Create())
        End Sub

        Private Overloads Sub ExecuteTypeCharCommand(args As TypeCharCommandArgs, finalHandler As Action, context As CommandExecutionContext)
            Dim compHandler = GetHandler(Of IChainedCommandHandler(Of TypeCharCommandArgs))()
            ExecuteTypeCharCommand(args, finalHandler, context, compHandler)
        End Sub

        Public Overloads Sub SendTypeChars(typeChars As String)
            MyBase.SendTypeChars(typeChars, Sub(a, n, c) ExecuteTypeCharCommand(a, n, c))
        End Sub

        Public Async Function SendTypeCharsAndWaitForUiRenderAsync(typeChars As String) As Task
            Dim uiRender = WaitForUIRenderedAsync()
            SendTypeChars(typeChars)
            Await uiRender
        End Function

        Public Overloads Sub SendEscape()
            MyBase.SendEscape(Sub(a, n, c) EditorCompletionCommandHandler.ExecuteCommand(a, Sub() SignatureHelpAfterCompletionCommandHandler.ExecuteCommand(a, n, c), c), Sub() Return)
        End Sub

        Public Overloads Sub SendDownKey()
            MyBase.SendDownKey(
                Sub(a, n, c) EditorCompletionCommandHandler.ExecuteCommand(a, Sub() SignatureHelpAfterCompletionCommandHandler.ExecuteCommand(a, n, c), c),
                Sub()
                    EditorOperations.MoveLineDown(extendSelection:=False)
                End Sub)
        End Sub

        Public Overloads Sub SendUpKey()
            MyBase.SendUpKey(
                Sub(a, n, c) EditorCompletionCommandHandler.ExecuteCommand(a, Sub() SignatureHelpAfterCompletionCommandHandler.ExecuteCommand(a, n, c), c),
                Sub()
                    EditorOperations.MoveLineUp(extendSelection:=False)
                End Sub)
        End Sub

        Public Overloads Sub SendPageUp()
            Dim handler = DirectCast(EditorCompletionCommandHandler, ICommandHandler(Of PageUpKeyCommandArgs))
            MyBase.SendPageUp(Sub(a, n, c) EditorCompletionCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendCut()
            MyBase.SendCut(Sub(a, n, c) EditorCompletionCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendPaste()
            MyBase.SendPaste(Sub(a, n, c) EditorCompletionCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendInvokeCompletionList()
            MyBase.SendInvokeCompletionList(Sub(a, n, c) EditorCompletionCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Async Function SendInvokeCompletionListAndWaitForUiRenderAsync() As Task
            Dim uiRender = WaitForUIRenderedAsync()
            MyBase.SendInvokeCompletionList(Sub(a, n, c) EditorCompletionCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
            Await uiRender
        End Function

        Public Overloads Sub SendInsertSnippetCommand()
            MyBase.SendInsertSnippetCommand(Sub(a, n, c) EditorCompletionCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendSurroundWithCommand()
            MyBase.SendSurroundWithCommand(Sub(a, n, c) EditorCompletionCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendSave()
            MyBase.SendSave(Sub(a, n, c) EditorCompletionCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Sub SendSelectAll()

            MyBase.SendSelectAll(Sub(a, n, c) EditorCompletionCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overrides Sub SendDeleteWordToLeft()
            Dim compHandler = DirectCast(EditorCompletionCommandHandler, ICommandHandler(Of WordDeleteToStartCommandArgs))
            MyBase.SendWordDeleteToStart(Sub(a, n, c) compHandler.ExecuteCommand(a, n, c), AddressOf MyBase.SendDeleteWordToLeft)
        End Sub

        Public Overloads Sub SendToggleCompletionMode()
            Dim handler = DirectCast(EditorCompletionCommandHandler, ICommandHandler(Of ToggleCompletionModeCommandArgs))
            MyBase.SendToggleCompletionMode(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Protected Function GetHandler(Of T As ICommandHandler)() As T
            Return DirectCast(EditorCompletionCommandHandler, T)
        End Function

#End Region

#Region "Completion Operations"

        Public Async Function SendCommitUniqueCompletionListItemAsync() As Task
            Await WaitForAsynchronousOperationsAsync()

            ' When we send the commit completion list item, it processes asynchronously; we can find out when it's complete
            ' by seeing that either the items are updated or the list is dismissed. We'll use a TaskCompletionSource to track
            ' when it's done which will release an async token.
            Dim sessionComplete = New TaskCompletionSource(Of Object)()
            Dim asynchronousOperationListenerProvider = Workspace.ExportProvider.GetExportedValue(Of AsynchronousOperationListenerProvider)()
            Dim asyncToken = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.CompletionSet) _
                .BeginAsyncOperation(NameOf(SendCommitUniqueCompletionListItemAsync))

#Disable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed
            sessionComplete.Task.CompletesAsyncOperation(asyncToken)
            Dim waitingForUI = WaitForUIRenderedAsync()
#Enable Warning BC42358 ' Because this call is not awaited, execution of the current method continues before the call is completed

            Dim itemsUpdatedHandler = Sub(sender As Object, e As Data.ComputedCompletionItemsEventArgs)
                                          ' If there is 0 or more than one item left, then it means this was the filter operation that resulted and we're done. 
                                          ' Otherwise we know a Dismiss operation is coming so we should wait for it.
                                          If e.Items.Items.Count() <> 1 Then
                                              Dim threadingContext = Workspace.ExportProvider.GetExportedValue(Of IThreadingContext)()

                                              ' Set up a timeout path to make sure tests don't deadlock
                                              Dim asyncToken2 = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.CompletionSet) _
                                                  .BeginAsyncOperation(NameOf(SendCommitUniqueCompletionListItemAsync))
                                              Task.Run(
                                                  Async Function() As Task
                                                      Using cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(threadingContext.DisposalToken)
                                                          Await Task.WhenAny(sessionComplete.Task, Task.Delay(TimeSpan.FromSeconds(2), cancellationSource.Token))
                                                          If sessionComplete.TrySetException(New TimeoutException()) Then
                                                              Throw New TimeoutException()
                                                          End If
                                                      End Using
                                                  End Function).CompletesAsyncOperation(asyncToken2).ReportNonFatalErrorUnlessCancelledAsync(threadingContext.DisposalToken)

                                              ' Now set up the expected path of just waiting for the UI to complete rendering
                                              threadingContext.JoinableTaskFactory.RunAsync(
                                                  Async Function() As Task
                                                      Await waitingForUI
                                                      sessionComplete.SetResult(Nothing)
                                                  End Function)
                                          End If
                                      End Sub

            Dim sessionDismissedHandler = Sub(sender As Object, e As EventArgs) sessionComplete.TrySetResult(Nothing)

            Dim session As IAsyncCompletionSession

            Dim addHandlers = Sub(sender As Object, e As Data.CompletionTriggeredEventArgs)
                                  AddHandler e.CompletionSession.ItemsUpdated, itemsUpdatedHandler
                                  AddHandler e.CompletionSession.Dismissed, sessionDismissedHandler
                                  session = e.CompletionSession
                              End Sub

            Dim asyncCompletionBroker As IAsyncCompletionBroker = GetExportedValue(Of IAsyncCompletionBroker)()
            session = asyncCompletionBroker.GetSession(TextView)
            If session Is Nothing Then
                AddHandler asyncCompletionBroker.CompletionTriggered, addHandlers
            Else
                ' A session was already active so we'll fake the event
                addHandlers(asyncCompletionBroker, New Data.CompletionTriggeredEventArgs(session, TextView))
            End If

            MyBase.SendCommitUniqueCompletionListItem(Sub(a, n, c) EditorCompletionCommandHandler.ExecuteCommand(a, n, c), Sub() Return)

            Await WaitForAsynchronousOperationsAsync()

            RemoveHandler session.ItemsUpdated, itemsUpdatedHandler
            RemoveHandler session.Dismissed, sessionDismissedHandler
            RemoveHandler asyncCompletionBroker.CompletionTriggered, addHandlers

            ' It's possible for the wait to bail and give up if it was clear nothing was completing; ensure we clean up our
            ' async token so as not to interfere with later tests.
            sessionComplete.TrySetResult(Nothing)
        End Function

        Public Async Function AssertNoCompletionSession() As Task
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

        Public Sub AssertNoCompletionSessionWithNoBlock()
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

        Public Async Function GetCompletionSession(Optional projectionsView As ITextView = Nothing) As Task(Of IAsyncCompletionSession)
            Await WaitForAsynchronousOperationsAsync()
            Dim view = If(projectionsView, TextView)

            Return GetExportedValue(Of IAsyncCompletionBroker)().GetSession(view)
        End Function

        Public Async Function AssertCompletionSession(Optional projectionsView As ITextView = Nothing) As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim view = If(projectionsView, TextView)

            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(view)
            Assert.NotNull(session)
        End Function

        Public Async Function AssertCompletionItemsDoNotContainAny(ParamArray displayText As String()) As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim items = GetCompletionItems()
            Assert.False(displayText.Any(Function(v) items.Any(Function(i) i.DisplayText = v)))
        End Function

        Public Async Function AssertCompletionItemsContainAll(ParamArray displayText As String()) As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim items = GetCompletionItems()
            Assert.All(displayText, Sub(v) Assert.Contains(v, items.Select(Function(i) i.DisplayText)))
        End Function

        Public Async Function AssertCompletionItemsContain(displayText As String, displayTextSuffix As String) As Task
            Await AssertCompletionItemsContain(Function(i) i.DisplayText = displayText AndAlso i.DisplayTextSuffix = displayTextSuffix)
        End Function

        Public Async Function AssertCompletionItemsContain(predicate As Func(Of CompletionItem, Boolean)) As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim items = GetCompletionItems()
            Assert.True(items.Any(predicate))
        End Function

        Public Sub AssertItemsInOrder(expectedOrder As String())
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim items = session.GetComputedItems(CancellationToken.None).Items
            Assert.Equal(expectedOrder.Count, items.Count)
            For i = 0 To expectedOrder.Count - 1
                Assert.Equal(expectedOrder(i), items(i).DisplayText)
            Next
        End Sub

        Public Sub AssertItemsInOrder(expectedOrder As (String, String)())
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim items = session.GetComputedItems(CancellationToken.None).Items
            Assert.Equal(expectedOrder.Count, items.Count)
            For i = 0 To expectedOrder.Count - 1
                Assert.Equal(expectedOrder(i).Item1, items(i).DisplayText)
                Assert.Equal(expectedOrder(i).Item2, items(i).Suffix)
            Next
        End Sub

        Public Async Function AssertSelectedCompletionItem(
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
                Dim itemDescription = Await GetSelectedItemDescriptionAsync()
                Assert.Equal(description, itemDescription.Text)
            End If

            If inlineDescription IsNot Nothing Then
                Assert.Equal(inlineDescription, items.SelectedItem.Suffix)
            End If

            If automationText IsNot Nothing Then
                Assert.Equal(automationText, items.SelectedItem.AutomationText)
            End If
        End Function

        Public Async Function GetSelectedItemDescriptionAsync() As Task(Of CompletionDescription)
            Dim document = Me.Workspace.CurrentSolution.Projects.First().Documents.First()
            Dim service = CompletionService.GetService(document)
            Dim roslynItem = GetSelectedItem()
            Dim options = CompletionOptions.Default
            Return Await service.GetDescriptionAsync(document, roslynItem, options, SymbolDescriptionOptions.Default)
        End Function

        Public Sub AssertCompletionItemExpander(isAvailable As Boolean, isSelected As Boolean)
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

        Public Async Function SetCompletionItemExpanderStateAndWaitForUiRenderAsync(isSelected As Boolean) As Task
            Dim uiRender = WaitForUIRenderedAsync()
            Dim presenter = DirectCast(CompletionPresenterProvider.GetOrCreate(Me.TextView), MockCompletionPresenter)
            Dim expander = presenter.GetExpander()
            Assert.NotNull(expander)
            presenter.SetExpander(isSelected)
            Await uiRender
        End Function

        Public Async Function AssertSessionIsNothingOrNoCompletionItemLike(text As String) As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            If Not session Is Nothing Then
                Await AssertCompletionItemsDoNotContainAny(text)
            End If
        End Function

        Public Function GetSelectedItem() As CompletionItem
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim items = session.GetComputedItems(CancellationToken.None)
            Return GetRoslynCompletionItem(items.SelectedItem)
        End Function

        Public Sub CalculateItemsIfSessionExists()
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            If session IsNot Nothing Then
                Dim item = session.GetComputedItems(CancellationToken.None).SelectedItem
            End If
        End Sub

        Public Function GetCompletionItems() As IList(Of CompletionItem)
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Return session.GetComputedItems(CancellationToken.None).Items.Select(Function(item) GetRoslynCompletionItem(item)).ToList()
        End Function

        Private Shared Function GetRoslynCompletionItem(item As Data.CompletionItem) As CompletionItem
            If (item Is Nothing) Then
                Return Nothing
            End If

            Dim roslynItemData As CompletionItemData = Nothing
            If (CompletionItemData.TryGetData(item, roslynItemData) = False) Then
                Return Nothing
            End If
            Return roslynItemData.RoslynItem
        End Function

        Public Async Function RaiseFiltersChangedAndWaitForUiRenderAsync(args As ImmutableArray(Of Data.CompletionFilterWithState)) As Task
            Dim uiRender = WaitForUIRenderedAsync()
            Dim presenter = DirectCast(CompletionPresenterProvider.GetOrCreate(Me.TextView), MockCompletionPresenter)
            Dim newArgs = New Data.CompletionFilterChangedEventArgs(args)
            presenter.TriggerFiltersChanged(Me, newArgs)
            Await uiRender
        End Function

        Public Function GetCompletionItemFilters() As ImmutableArray(Of Data.CompletionFilterWithState)
            Dim presenter = DirectCast(CompletionPresenterProvider.GetOrCreate(Me.TextView), MockCompletionPresenter)
            Return presenter.GetFilters()
        End Function

        Public Function HasSuggestedItem() As Boolean
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim computedItems = session.GetComputedItems(CancellationToken.None)
            Return computedItems.SuggestionItem IsNot Nothing
        End Function

        Public Sub AssertSuggestedItemSelected(displayText As String)
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim computedItems = session.GetComputedItems(CancellationToken.None)
            Assert.True(computedItems.SuggestionItemSelected)
            Assert.Equal(computedItems.SuggestionItem.DisplayText, displayText)
        End Sub

        Public Function IsSoftSelected() As Boolean
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Assert.NotNull(session)
            Dim computedItems = session.GetComputedItems(CancellationToken.None)
            Return computedItems.UsesSoftSelection
        End Function

        Public Sub SendSelectCompletionItem(displayText As String)
            Dim session = GetExportedValue(Of IAsyncCompletionBroker)().GetSession(TextView)
            Dim operations = DirectCast(session, IAsyncCompletionSessionOperations)
            operations.SelectCompletionItem(session.GetComputedItems(CancellationToken.None).Items.Single(Function(i) i.DisplayText = displayText))
        End Sub

        Public Async Function WaitForUIRenderedAsync() As Task
            Dim tcs = New TaskCompletionSource(Of Boolean)
            Dim presenter = DirectCast(CompletionPresenterProvider.GetOrCreate(TextView), MockCompletionPresenter)
            Dim uiUpdated As EventHandler(Of Data.CompletionItemSelectedEventArgs)

            uiUpdated = Sub()
                            RemoveHandler presenter.UiUpdated, uiUpdated
                            tcs.TrySetResult(True)
                        End Sub

            AddHandler presenter.UiUpdated, uiUpdated
            Dim ct = New CancellationTokenSource(timeoutMs)
            Using registration = ct.Token.Register(Sub() tcs.TrySetCanceled(), useSynchronizationContext:=False)
                Await tcs.Task.ConfigureAwait(True)
            End Using
        End Function

        Public Overloads Sub SendTypeCharsToSpecificViewAndBuffer(typeChars As String, view As IWpfTextView, buffer As ITextBuffer)
            For Each ch In typeChars
                Dim localCh = ch
                ExecuteTypeCharCommand(New TypeCharCommandArgs(view, buffer, localCh), Sub() EditorOperations.InsertText(localCh.ToString()), TestCommandExecutionContext.Create())
            Next
        End Sub

        Public Async Function AssertLineTextAroundCaret(expectedTextBeforeCaret As String, expectedTextAfterCaret As String) As Task
            Await WaitForAsynchronousOperationsAsync()

            Dim actual = GetLineTextAroundCaretPosition()

            Assert.Equal(expectedTextBeforeCaret, actual.TextBeforeCaret)
            Assert.Equal(expectedTextAfterCaret, actual.TextAfterCaret)
        End Function

        Public Sub NavigateToDisplayText(targetText As String)
            Dim currentText = GetSelectedItem().DisplayText

            ' GetComputedItems provided by the Editor for tests does not guarantee that 
            ' the order of items match the order of items actually displayed in the completion popup.
            ' For example, they put starred items (intellicode) below non-starred ones.
            ' And the order they display those items in the UI is opposite.
            ' Therefore, we do the full traverse: down to the bottom and if not found up to the top.
            Do While currentText <> targetText
                SendDownKey()
                Dim newText = GetSelectedItem().DisplayText
                If currentText = newText Then
                    ' Nothing found on going down. Try going up
                    Do While currentText <> targetText
                        SendUpKey()
                        newText = GetSelectedItem().DisplayText
                        Assert.True(newText <> currentText, "Reached the bottom, then the top and didn't find the match")
                        currentText = newText
                    Loop
                End If

                currentText = newText
            Loop
        End Sub

#End Region

#Region "Signature Help Operations"

        Public Overloads Sub SendInvokeSignatureHelp()
            Dim handler = DirectCast(SignatureHelpBeforeCompletionCommandHandler, IChainedCommandHandler(Of InvokeSignatureHelpCommandArgs))
            MyBase.SendInvokeSignatureHelp(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Public Overloads Async Function AssertNoSignatureHelpSession(Optional block As Boolean = True) As Task
            If block Then
                Await WaitForAsynchronousOperationsAsync()
            End If

            Assert.Null(Me.CurrentSignatureHelpPresenterSession)
        End Function

        Public Overloads Async Function AssertSignatureHelpSession() As Task
            Await WaitForAsynchronousOperationsAsync()
            Assert.NotNull(Me.CurrentSignatureHelpPresenterSession)
        End Function

        Public Overloads Function GetSignatureHelpItems() As IList(Of SignatureHelpItem)
            Return CurrentSignatureHelpPresenterSession.SignatureHelpItems
        End Function

        Public Async Function AssertSignatureHelpItemsContainAll(displayText As String()) As Task
            Await WaitForAsynchronousOperationsAsync()
            Assert.True(displayText.All(Function(v) CurrentSignatureHelpPresenterSession.SignatureHelpItems.Any(
                                            Function(i) GetDisplayText(i, CurrentSignatureHelpPresenterSession.SelectedParameter.Value) = v)))
        End Function

        Public Async Function AssertSelectedSignatureHelpItem(Optional displayText As String = Nothing,
                               Optional documentation As String = Nothing,
                               Optional selectedParameter As String = Nothing) As Task
            Await WaitForAsynchronousOperationsAsync()

            If displayText IsNot Nothing Then
                Assert.Equal(displayText, GetDisplayText(Me.CurrentSignatureHelpPresenterSession.SelectedItem, Me.CurrentSignatureHelpPresenterSession.SelectedParameter.Value))
            End If

            If documentation IsNot Nothing Then
                Assert.Equal(documentation, Me.CurrentSignatureHelpPresenterSession.SelectedItem.DocumentationFactory(CancellationToken.None).GetFullText())
            End If

            If selectedParameter IsNot Nothing Then
                Assert.Equal(selectedParameter, GetDisplayText(
                    Me.CurrentSignatureHelpPresenterSession.SelectedItem.Parameters(
                        Me.CurrentSignatureHelpPresenterSession.SelectedParameter.Value).DisplayParts))
            End If
        End Function
#End Region

#Region "Helpers"

        Private Shared Function GetDisplayText(item As SignatureHelpItem, selectedParameter As Integer) As String
            Dim suffix = If(selectedParameter < item.Parameters.Count,
                            GetDisplayText(item.Parameters(selectedParameter).SuffixDisplayParts),
                            String.Empty)
            Return String.Join(
                String.Empty,
                GetDisplayText(item.PrefixDisplayParts),
                String.Join(
                    GetDisplayText(item.SeparatorDisplayParts),
                    item.Parameters.Select(Function(p) GetDisplayText(p.DisplayParts))),
                GetDisplayText(item.SuffixDisplayParts),
                suffix)
        End Function

        Private Shared Function GetDisplayText(parts As IEnumerable(Of TaggedText)) As String
            Return String.Join(String.Empty, parts.Select(Function(p) p.ToString()))
        End Function

#End Region

    End Class
End Namespace
