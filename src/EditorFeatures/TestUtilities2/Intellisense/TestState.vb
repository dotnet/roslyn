' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.CommandHandlers
Imports Microsoft.CodeAnalysis.Editor.Implementation.Formatting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Roslyn.Utilities
Imports VSCommanding = Microsoft.VisualStudio.Commanding

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Friend Class TestState
        Inherits AbstractCommandHandlerTestState

        Private Const TimeoutMs = 10000
        Friend Const RoslynItem = "RoslynItem"
        Friend ReadOnly EditorCompletionCommandHandler As VSCommanding.ICommandHandler
        Friend ReadOnly CompletionPresenterProvider As ICompletionPresenterProvider

        Protected ReadOnly SessionTestState As IIntelliSenseTestState
        Private ReadOnly SignatureHelpBeforeCompletionCommandHandler As SignatureHelpBeforeCompletionCommandHandler
        Protected ReadOnly SignatureHelpAfterCompletionCommandHandler As SignatureHelpAfterCompletionCommandHandler
        Private ReadOnly FormatCommandHandler As FormatCommandHandler

        Private Shared s_lazyEntireAssemblyCatalogWithCSharpAndVisualBasicWithoutCompletionTestParts As Lazy(Of ComposableCatalog) =
            New Lazy(Of ComposableCatalog)(Function()
                                               Return TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.
                                               WithoutPartsOfTypes({
                                                                   GetType(IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession)),
                                                                   GetType(FormatCommandHandler)}).
                                               WithParts({
                                                         GetType(TestSignatureHelpPresenter),
                                                         GetType(IntelliSenseTestState),
                                                         GetType(MockCompletionPresenterProvider)
                                                         })
                                           End Function)

        Private Shared ReadOnly Property EntireAssemblyCatalogWithCSharpAndVisualBasicWithoutCompletionTestParts As ComposableCatalog
            Get
                Return s_lazyEntireAssemblyCatalogWithCSharpAndVisualBasicWithoutCompletionTestParts.Value
            End Get
        End Property

        Private Shared s_lazyExportProviderFactoryWithCSharpAndVisualBasicWithoutCompletionTestParts As Lazy(Of IExportProviderFactory) =
            New Lazy(Of IExportProviderFactory)(Function()
                                                    Return ExportProviderCache.GetOrCreateExportProviderFactory(EntireAssemblyCatalogWithCSharpAndVisualBasicWithoutCompletionTestParts)
                                                End Function)

        Private Shared ReadOnly Property ExportProviderFactoryWithCSharpAndVisualBasicWithoutCompletionTestParts As IExportProviderFactory
            Get
                Return s_lazyExportProviderFactoryWithCSharpAndVisualBasicWithoutCompletionTestParts.Value
            End Get
        End Property

        Friend ReadOnly Property CurrentSignatureHelpPresenterSession As TestSignatureHelpPresenterSession
            Get
                Return SessionTestState.CurrentSignatureHelpPresenterSession
            End Get
        End Property

        ' Do not call directly. Use TestStateFactory
        Friend Sub New(workspaceElement As XElement,
                       extraCompletionProviders As CompletionProvider(),
                       excludedTypes As List(Of Type),
                       extraExportedTypes As List(Of Type),
                       includeFormatCommandHandler As Boolean,
                       workspaceKind As String,
                       Optional cursorDocumentElement As XElement = Nothing,
                       Optional roles As ImmutableArray(Of String) = Nothing)
            MyBase.New(workspaceElement, GetExportProvider(excludedTypes, extraExportedTypes, includeFormatCommandHandler), workspaceKind:=workspaceKind, cursorDocumentElement, roles)

            Dim languageServices = Me.Workspace.CurrentSolution.Projects.First().LanguageServices
            Dim language = languageServices.Language

            Dim lazyExtraCompletionProviders = CreateLazyProviders(extraCompletionProviders, language, roles:=Nothing)
            If lazyExtraCompletionProviders IsNot Nothing Then
                Dim completionService = DirectCast(languageServices.GetService(Of CompletionService), CompletionServiceWithProviders)
                If completionService IsNot Nothing Then
                    completionService.SetTestProviders(lazyExtraCompletionProviders.Select(Function(lz) lz.Value).ToList())
                End If
            End If

            Me.SessionTestState = GetExportedValue(Of IIntelliSenseTestState)()

            Me.SignatureHelpBeforeCompletionCommandHandler = GetExportedValue(Of SignatureHelpBeforeCompletionCommandHandler)()

            Me.SignatureHelpAfterCompletionCommandHandler = GetExportedValue(Of SignatureHelpAfterCompletionCommandHandler)()

            Me.FormatCommandHandler = If(includeFormatCommandHandler, GetExportedValue(Of FormatCommandHandler)(), Nothing)

            CompletionPresenterProvider = GetExportedValues(Of ICompletionPresenterProvider)().
                Single(Function(e As ICompletionPresenterProvider) e.GetType().FullName = "Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense.MockCompletionPresenterProvider")
            EditorCompletionCommandHandler = GetExportedValues(Of VSCommanding.ICommandHandler)().
                Single(Function(e As VSCommanding.ICommandHandler) e.GetType().Name = PredefinedCompletionNames.CompletionCommandHandler)
        End Sub

        Private Overloads Shared Function GetExportProvider(excludedTypes As List(Of Type),
                                                  extraExportedTypes As List(Of Type),
                                                  includeFormatCommandHandler As Boolean) As ExportProvider
            If (excludedTypes Is Nothing OrElse excludedTypes.Count = 0) AndAlso
               (extraExportedTypes Is Nothing OrElse extraExportedTypes.Count = 0) AndAlso
               Not includeFormatCommandHandler Then
                Return ExportProviderFactoryWithCSharpAndVisualBasicWithoutCompletionTestParts.CreateExportProvider()
            End If

            Dim combinedExcludedTypes = CombineExcludedTypes(excludedTypes, includeFormatCommandHandler)
            Dim extraParts = ExportProviderCache.CreateTypeCatalog(CombineExtraTypes(If(extraExportedTypes, New List(Of Type))))
            Return GetExportProvider(combinedExcludedTypes, extraParts)
        End Function

#Region "Editor Related Operations"

        Protected Overloads Sub ExecuteTypeCharCommand(args As TypeCharCommandArgs, finalHandler As Action, context As CommandExecutionContext, completionCommandHandler As VSCommanding.IChainedCommandHandler(Of TypeCharCommandArgs))
            Dim sigHelpHandler = DirectCast(SignatureHelpBeforeCompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of TypeCharCommandArgs))
            Dim formatHandler = DirectCast(FormatCommandHandler, VSCommanding.IChainedCommandHandler(Of TypeCharCommandArgs))

            If formatHandler Is Nothing Then
                sigHelpHandler.ExecuteCommand(
                    args, Sub() completionCommandHandler.ExecuteCommand(
                                    args, finalHandler, context), context)
            Else
                formatHandler.ExecuteCommand(
                    args, Sub() sigHelpHandler.ExecuteCommand(
                                    args, Sub() completionCommandHandler.ExecuteCommand(
                                                    args, finalHandler, context), context), context)
            End If
        End Sub

        Public Overloads Sub SendTab()
            Dim handler = GetHandler(Of VSCommanding.IChainedCommandHandler(Of TabKeyCommandArgs))()
            MyBase.SendTab(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() EditorOperations.InsertText(vbTab))
        End Sub

        Public Overloads Sub SendReturn()
            Dim handler = GetHandler(Of VSCommanding.IChainedCommandHandler(Of ReturnKeyCommandArgs))()
            MyBase.SendReturn(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() EditorOperations.InsertNewLine())
        End Sub

        Public Overrides Sub SendBackspace()
            Dim compHandler = GetHandler(Of VSCommanding.IChainedCommandHandler(Of BackspaceKeyCommandArgs))()
            MyBase.SendBackspace(Sub(a, n, c) compHandler.ExecuteCommand(a, n, c), AddressOf MyBase.SendBackspace)
        End Sub

        Public Overrides Sub SendDelete()
            Dim compHandler = GetHandler(Of VSCommanding.IChainedCommandHandler(Of DeleteKeyCommandArgs))()
            MyBase.SendDelete(Sub(a, n, c) compHandler.ExecuteCommand(a, n, c), AddressOf MyBase.SendDelete)
        End Sub

        Public Sub SendDeleteToSpecificViewAndBuffer(view As IWpfTextView, buffer As ITextBuffer)
            Dim compHandler = GetHandler(Of VSCommanding.IChainedCommandHandler(Of DeleteKeyCommandArgs))()
            compHandler.ExecuteCommand(New DeleteKeyCommandArgs(view, buffer), AddressOf MyBase.SendDelete, TestCommandExecutionContext.Create())
        End Sub

        Private Overloads Sub ExecuteTypeCharCommand(args As TypeCharCommandArgs, finalHandler As Action, context As CommandExecutionContext)
            Dim compHandler = GetHandler(Of VSCommanding.IChainedCommandHandler(Of TypeCharCommandArgs))()
            ExecuteTypeCharCommand(args, finalHandler, context, compHandler)
        End Sub

        Public Overloads Sub SendTypeChars(typeChars As String)
            MyBase.SendTypeChars(typeChars, Sub(a, n, c) ExecuteTypeCharCommand(a, n, c))
        End Sub

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
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of PageUpKeyCommandArgs))
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
            Dim compHandler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of WordDeleteToStartCommandArgs))
            MyBase.SendWordDeleteToStart(Sub(a, n, c) compHandler.ExecuteCommand(a, n, c), AddressOf MyBase.SendDeleteWordToLeft)
        End Sub

        Public Overloads Sub SendToggleCompletionMode()
            Dim handler = DirectCast(EditorCompletionCommandHandler, VSCommanding.ICommandHandler(Of ToggleCompletionModeCommandArgs))
            MyBase.SendToggleCompletionMode(Sub(a, n, c) handler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

        Protected Function GetHandler(Of T As VSCommanding.ICommandHandler)() As T
            Return DirectCast(EditorCompletionCommandHandler, T)
        End Function

#End Region

#Region "Completion Operations"

        Public Overloads Sub SendCommitUniqueCompletionListItem()
            MyBase.SendCommitUniqueCompletionListItem(Sub(a, n, c) EditorCompletionCommandHandler.ExecuteCommand(a, n, c), Sub() Return)
        End Sub

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
            Assert.True(displayText.All(Function(v) items.Any(Function(i) i.DisplayText = v)))
        End Function

        Public Async Function AssertCompletionItemsContain(displayText As String, displayTextSuffix As String) As Task
            Await WaitForAsynchronousOperationsAsync()
            Dim items = GetCompletionItems()
            Assert.True(items.Any(Function(i) i.DisplayText = displayText AndAlso i.DisplayTextSuffix = displayTextSuffix))
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
            Return If(item IsNot Nothing, DirectCast(item.Properties(RoslynItem), CompletionItem), Nothing)
        End Function

        Public Sub RaiseFiltersChanged(args As ImmutableArray(Of Data.CompletionFilterWithState))
            Dim presenter = DirectCast(CompletionPresenterProvider.GetOrCreate(Me.TextView), MockCompletionPresenter)
            Dim newArgs = New Data.CompletionFilterChangedEventArgs(args)
            presenter.TriggerFiltersChanged(Me, newArgs)
        End Sub

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

        Public Sub SetCompletionItemExpanderState(isSelected As Boolean)
            Dim presenter = DirectCast(CompletionPresenterProvider.GetOrCreate(Me.TextView), MockCompletionPresenter)
            Dim expander = presenter.GetExpander()
            Assert.NotNull(expander)
            presenter.SetExpander(isSelected)
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
            Await WaitForAsynchronousOperationsAsync()
            Dim tcs = New TaskCompletionSource(Of Boolean)
            Dim presenter = DirectCast(CompletionPresenterProvider.GetOrCreate(TextView), MockCompletionPresenter)
            Dim uiUpdated As EventHandler(Of Data.CompletionItemSelectedEventArgs)

            uiUpdated = Sub()
                            RemoveHandler presenter.UiUpdated, uiUpdated
                            tcs.TrySetResult(True)
                        End Sub

            AddHandler presenter.UiUpdated, uiUpdated
            Dim ct = New CancellationTokenSource(TimeoutMs)
            ct.Token.Register(Sub() tcs.TrySetCanceled(), useSynchronizationContext:=False)

            Await tcs.Task.ConfigureAwait(True)
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
            Dim handler = DirectCast(SignatureHelpBeforeCompletionCommandHandler, VSCommanding.IChainedCommandHandler(Of InvokeSignatureHelpCommandArgs))
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

        Private Shared Function CombineExcludedTypes(excludedTypes As IList(Of Type), includeFormatCommandHandler As Boolean) As IList(Of Type)
            Dim result = New List(Of Type) From {
                GetType(IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession))
            }

            If Not includeFormatCommandHandler Then
                result.Add(GetType(FormatCommandHandler))
            End If

            If excludedTypes IsNot Nothing Then
                result.AddRange(excludedTypes)
            End If

            Return result
        End Function

        Private Shared Function CombineExtraTypes(extraExportedTypes As IList(Of Type)) As IList(Of Type)
            Dim result = New List(Of Type) From {
                GetType(TestSignatureHelpPresenter),
                GetType(IntelliSenseTestState),
                GetType(MockCompletionPresenterProvider)
            }

            If extraExportedTypes IsNot Nothing Then
                result.AddRange(extraExportedTypes)
            End If

            Return result
        End Function

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
