' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Projection
Imports Moq

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    <[UseExportProvider]>
    Public Class SignatureHelpControllerTests
        <WpfFact>
        Public Sub InvokeSignatureHelpWithoutDocumentShouldNotStartNewSession()
            Dim emptyProvider = New Mock(Of IDocumentProvider)(MockBehavior.Strict)
            emptyProvider.Setup(Function(p) p.GetDocument(It.IsAny(Of ITextSnapshot), It.IsAny(Of CancellationToken))).Returns(DirectCast(Nothing, Document))
            Dim controller As Controller = CreateController(CreateWorkspace(), documentProvider:=emptyProvider)

            GetMocks(controller).PresenterSession.Setup(Sub(p) p.Dismiss())

            controller.WaitForController()

            Assert.Equal(0, GetMocks(controller).Provider.GetItemsCount)
        End Sub

        <WpfFact>
        Public Sub InvokeSignatureHelpWithDocumentShouldStartNewSession()
            Dim controller = CreateController(CreateWorkspace())

            GetMocks(controller).Presenter.Verify(Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of ISignatureHelpSession)), Times.Once)
        End Sub

        <WpfFact>
        Public Sub EmptyModelShouldStopSession()
            Dim presenterSession = New Mock(Of ISignatureHelpPresenterSession)(MockBehavior.Strict)
            presenterSession.Setup(Sub(p) p.Dismiss())

            Dim controller = CreateController(CreateWorkspace(), presenterSession:=presenterSession, items:={}, waitForPresentation:=True)

            GetMocks(controller).PresenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact>
        Public Sub UpKeyShouldDismissWhenThereIsOnlyOneItem()
            Dim controller = CreateController(CreateWorkspace(), items:=CreateItems(1), waitForPresentation:=True)

            GetMocks(controller).PresenterSession.Setup(Sub(p) p.Dismiss())

            Dim handled = controller.TryHandleUpKey()

            Assert.False(handled)
            GetMocks(controller).PresenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact>
        Public Sub UpKeyShouldNavigateWhenThereAreMultipleItems()
            Dim controller = CreateController(CreateWorkspace(), items:=CreateItems(2), waitForPresentation:=True)

            GetMocks(controller).PresenterSession.Setup(Sub(p) p.SelectPreviousItem())

            Dim handled = controller.TryHandleUpKey()

            Assert.True(handled)
            GetMocks(controller).PresenterSession.Verify(Sub(p) p.SelectPreviousItem(), Times.Once)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/985007")>
        Public Sub UpKeyShouldNotCrashWhenSessionIsDismissed()
            Dim options = New MemberDisplayOptions()

            ' Create a provider that will return an empty state when queried the second time
            Dim slowProvider = New Mock(Of ISignatureHelpProvider)(MockBehavior.Strict)
            slowProvider.Setup(Function(p) p.IsTriggerCharacter(" "c)).Returns(True)
            slowProvider.Setup(Function(p) p.IsRetriggerCharacter(" "c)).Returns(True)
            slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), options, It.IsAny(Of CancellationToken))) _
                .Returns(Task.FromResult(New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 0), selectedItem:=0, semanticParameterIndex:=0, syntacticArgumentCount:=0, argumentName:=Nothing)))
            Dim controller = CreateController(CreateWorkspace(), provider:=slowProvider.Object, waitForPresentation:=True)

            ' Now force an update to the model that will result in stopping the session
            slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), options, It.IsAny(Of CancellationToken))) _
                .Returns(Task.FromResult(Of SignatureHelpItems)(Nothing))

            DirectCast(controller, IChainedCommandHandler(Of TypeCharCommandArgs)).ExecuteCommand(
                New TypeCharCommandArgs(CreateMock(Of ITextView), CreateMock(Of ITextBuffer), " "c),
                Sub() GetMocks(controller).Buffer.Insert(0, " "), TestCommandExecutionContext.Create())

            GetMocks(controller).PresenterSession.Setup(Sub(p) p.Dismiss())

            Dim handled = controller.TryHandleUpKey() ' this will block on the model being updated which should dismiss the session

            Assert.False(handled)
            GetMocks(controller).PresenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact, WorkItem("https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workItems?id=179726&_a=edit")>
        Public Sub DownKeyShouldNotBlockOnModelComputation()
            Dim options = New MemberDisplayOptions()
            Dim mre = New ManualResetEvent(False)
            Dim controller = CreateController(CreateWorkspace(), items:=CreateItems(2), waitForPresentation:=False)
            Dim slowProvider = New Mock(Of ISignatureHelpProvider)(MockBehavior.Strict)
            slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), options, It.IsAny(Of CancellationToken))) _
                .Returns(Function()
                             mre.WaitOne()
                             Return Task.FromResult(New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 0), selectedItem:=0, semanticParameterIndex:=0, syntacticArgumentCount:=0, argumentName:=Nothing))
                         End Function)

            GetMocks(controller).PresenterSession.Setup(Sub(p) p.Dismiss())
            GetMocks(controller).PresenterSession.Setup(Function(p) p.EditorSessionIsActive).Returns(False)

            Dim handled = controller.TryHandleDownKey()

            Assert.False(handled)
        End Sub

        <WpfFact, WorkItem("https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workItems?id=179726&_a=edit")>
        Public Sub UpKeyShouldNotBlockOnModelComputation()
            Dim options = New MemberDisplayOptions()
            Dim mre = New ManualResetEvent(False)
            Dim controller = CreateController(CreateWorkspace(), items:=CreateItems(2), waitForPresentation:=False)
            Dim slowProvider = New Mock(Of ISignatureHelpProvider)(MockBehavior.Strict)
            slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), options, It.IsAny(Of CancellationToken))) _
                .Returns(Function()
                             mre.WaitOne()
                             Return Task.FromResult(New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 0), selectedItem:=0, semanticParameterIndex:=0, syntacticArgumentCount:=0, argumentName:=Nothing))
                         End Function)

            GetMocks(controller).PresenterSession.Setup(Sub(p) p.Dismiss())
            GetMocks(controller).PresenterSession.Setup(Function(p) p.EditorSessionIsActive).Returns(False)

            Dim handled = controller.TryHandleUpKey()

            Assert.False(handled)
        End Sub

        <WpfFact, WorkItem("https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workItems?id=179726&_a=edit")>
        Public Async Function UpKeyShouldBlockOnRecomputationAfterPresentation() As Task
            Dim options = New MemberDisplayOptions()
            Dim workspace = CreateWorkspace()
            Dim threadingContext = workspace.GetService(Of IThreadingContext)()

            Dim worker = Async Function()
                             Dim slowProvider = New Mock(Of ISignatureHelpProvider)(MockBehavior.Strict)
                             slowProvider.Setup(Function(p) p.IsTriggerCharacter(" "c)).Returns(True)
                             slowProvider.Setup(Function(p) p.IsRetriggerCharacter(" "c)).Returns(True)
                             slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), options, It.IsAny(Of CancellationToken))) _
                                 .Returns(Task.FromResult(New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 0), selectedItem:=0, semanticParameterIndex:=0, syntacticArgumentCount:=0, argumentName:=Nothing)))

                             Await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync()
                             Dim controller = CreateController(workspace, provider:=slowProvider.Object, waitForPresentation:=True)

                             ' Update session so that providers are requeried.
                             ' SlowProvider now blocks on the checkpoint's task.
                             Dim checkpoint = New Checkpoint()
                             slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), options, It.IsAny(Of CancellationToken))) _
                                 .Returns(Function()
                                              checkpoint.Task.Wait()
                                              Return Task.FromResult(New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 2), selectedItem:=0, semanticParameterIndex:=0, syntacticArgumentCount:=0, argumentName:=Nothing))
                                          End Function)

                             Await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync()
                             DirectCast(controller, IChainedCommandHandler(Of TypeCharCommandArgs)).ExecuteCommand(
                                 New TypeCharCommandArgs(CreateMock(Of ITextView), CreateMock(Of ITextBuffer), " "c),
                                 Sub() GetMocks(controller).Buffer.Insert(0, " "), TestCommandExecutionContext.Create())

                             GetMocks(controller).PresenterSession.Setup(Sub(p) p.SelectPreviousItem())

                             Dim handled = threadingContext.JoinableTaskFactory.RunAsync(Async Function()
                                                                                             Await Task.Yield()
                                                                                             ' Send the controller an up key, which should block on the computation
                                                                                             Return controller.TryHandleUpKey()
                                                                                         End Function)
                             checkpoint.Release() ' Allow slowprovider to finish
                             Await handled.JoinAsync().ConfigureAwait(False)

                             ' We expect 2 calls to the presenter (because we had an existing presentation session when we started the second computation).
                             Assert.True(handled.Task.Result)
                             GetMocks(controller).PresenterSession.Verify(Sub(p) p.PresentItems(It.IsAny(Of ITrackingSpan), It.IsAny(Of IList(Of SignatureHelpItem)),
                                                                                                It.IsAny(Of SignatureHelpItem), It.IsAny(Of Integer?)), Times.Exactly(2))
                         End Function

            Await worker().ConfigureAwait(False)

        End Function

        <WpfFact>
        Public Sub DownKeyShouldNavigateWhenThereAreMultipleItems()
            Dim controller = CreateController(CreateWorkspace(), items:=CreateItems(2), waitForPresentation:=True)

            GetMocks(controller).PresenterSession.Setup(Sub(p) p.SelectNextItem())

            Dim handled = controller.TryHandleDownKey()

            Assert.True(handled)
            GetMocks(controller).PresenterSession.Verify(Sub(p) p.SelectNextItem(), Times.Once)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/1181")>
        Public Sub UpAndDownKeysShouldStillNavigateWhenDuplicateItemsAreFiltered()
            Dim item = CreateItems(1).Single()
            Dim controller = CreateController(CreateWorkspace(), items:={item, item}, waitForPresentation:=True)

            GetMocks(controller).PresenterSession.Setup(Sub(p) p.Dismiss())

            Dim handled = controller.TryHandleUpKey()

            Assert.False(handled)
            GetMocks(controller).PresenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact>
        Public Sub CaretMoveWithActiveSessionShouldRecomputeModel()
            Dim controller = CreateController(CreateWorkspace(), waitForPresentation:=True)

            Mock.Get(GetMocks(controller).View.Object.Caret).Raise(Sub(c) AddHandler c.PositionChanged, Nothing, New CaretPositionChangedEventArgs(Nothing, Nothing, Nothing))
            controller.WaitForController()

            ' GetItemsAsync is called once initially, and then once as a result of handling the PositionChanged event
            Assert.Equal(2, GetMocks(controller).Provider.GetItemsCount)
        End Sub

        <WpfFact>
        Public Sub RetriggerActiveSessionOnClosingBrace()
            Dim controller = CreateController(CreateWorkspace(), waitForPresentation:=True)

            DirectCast(controller, IChainedCommandHandler(Of TypeCharCommandArgs)).ExecuteCommand(
                New TypeCharCommandArgs(CreateMock(Of ITextView), CreateMock(Of ITextBuffer), ")"c),
                Sub() GetMocks(controller).Buffer.Insert(0, ")"), TestCommandExecutionContext.Create())
            controller.WaitForController()

            ' GetItemsAsync is called once initially, and then once as a result of handling the typechar command
            Assert.Equal(2, GetMocks(controller).Provider.GetItemsCount)
        End Sub

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/959116")>
        Public Sub TypingNonTriggerCharacterShouldNotRequestDocument()
            Dim controller = CreateController(CreateWorkspace(), triggerSession:=False)

            DirectCast(controller, IChainedCommandHandler(Of TypeCharCommandArgs)).ExecuteCommand(
                New TypeCharCommandArgs(CreateMock(Of ITextView), CreateMock(Of ITextBuffer), "a"c),
                Sub() GetMocks(controller).Buffer.Insert(0, "a"), TestCommandExecutionContext.Create())

            GetMocks(controller).DocumentProvider.Verify(Function(p) p.GetDocument(It.IsAny(Of ITextSnapshot), It.IsAny(Of CancellationToken)), Times.Never)
        End Sub

        Private Shared ReadOnly s_controllerMocksMap As New ConditionalWeakTable(Of Controller, ControllerMocks)
        Private Shared Function GetMocks(controller As Controller) As ControllerMocks
            Dim result As ControllerMocks = Nothing
            Roslyn.Utilities.Contract.ThrowIfFalse(s_controllerMocksMap.TryGetValue(controller, result))
            Return result
        End Function

        Private Shared Function CreateWorkspace() As TestWorkspace
            Return TestWorkspace.CreateWorkspace(
                <Workspace>
                    <Project Language="C#">
                        <Document>
                        </Document>
                    </Project>
                </Workspace>, composition:=EditorTestCompositions.EditorFeatures)
        End Function

        Private Shared Function CreateController(workspace As TestWorkspace,
                                                 Optional documentProvider As Mock(Of IDocumentProvider) = Nothing,
                                                 Optional presenterSession As Mock(Of ISignatureHelpPresenterSession) = Nothing,
                                                 Optional items As IList(Of SignatureHelpItem) = Nothing,
                                                 Optional provider As ISignatureHelpProvider = Nothing,
                                                 Optional waitForPresentation As Boolean = False,
                                                 Optional triggerSession As Boolean = True) As Controller
            Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.Single().Id)

            Dim threadingContext = workspace.GetService(Of IThreadingContext)
            Dim bufferFactory As ITextBufferFactoryService = workspace.GetService(Of ITextBufferFactoryService)
            Dim buffer = bufferFactory.CreateTextBuffer()
            Dim view = CreateMockTextView(buffer)
            Dim asyncListener = AsynchronousOperationListenerProvider.NullListener
            If documentProvider Is Nothing Then
                documentProvider = New Mock(Of IDocumentProvider)(MockBehavior.Strict)
                documentProvider.Setup(Function(p) p.GetDocument(It.IsAny(Of ITextSnapshot), It.IsAny(Of CancellationToken))).Returns(document)
            End If

            If provider Is Nothing Then
                items = If(items, CreateItems(1))
                provider = New MockSignatureHelpProvider(items)
            End If

            Dim presenter = New Mock(Of IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession))(MockBehavior.Strict) With {.DefaultValue = DefaultValue.Mock}
            presenterSession = If(presenterSession, New Mock(Of ISignatureHelpPresenterSession)(MockBehavior.Strict) With {.DefaultValue = DefaultValue.Mock})
            presenter.Setup(Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of ISignatureHelpSession))).Returns(presenterSession.Object)
            presenterSession.Setup(Sub(p) p.PresentItems(It.IsAny(Of ITrackingSpan), It.IsAny(Of IList(Of SignatureHelpItem)), It.IsAny(Of SignatureHelpItem), It.IsAny(Of Integer?))) _
                .Callback(Sub() presenterSession.SetupGet(Function(p) p.EditorSessionIsActive).Returns(True))

            Dim mockCompletionBroker = New Mock(Of IAsyncCompletionBroker)(MockBehavior.Strict)
            mockCompletionBroker.Setup(Function(b) b.GetSession(It.IsAny(Of ITextView))).Returns(DirectCast(Nothing, IAsyncCompletionSession))

            Dim controller = New Controller(
                workspace.GlobalOptions,
                threadingContext,
                view.Object,
                buffer,
                presenter.Object,
                asyncListener,
                documentProvider.Object,
                {provider},
                mockCompletionBroker.Object)

            s_controllerMocksMap.Add(controller, New ControllerMocks(
                      view,
                      buffer,
                      presenter,
                      presenterSession,
                      asyncListener,
                      documentProvider,
                      TryCast(provider, MockSignatureHelpProvider)))

            If triggerSession Then
                DirectCast(controller, IChainedCommandHandler(Of InvokeSignatureHelpCommandArgs)).ExecuteCommand(
                    New InvokeSignatureHelpCommandArgs(view.Object, buffer), Nothing, TestCommandExecutionContext.Create())
                If waitForPresentation Then
                    controller.WaitForController()
                End If
            End If

            Return controller
        End Function

        Private Shared Function CreateItems(count As Integer) As IList(Of SignatureHelpItem)
            Return Enumerable.Range(0, count).Select(Function(i) New SignatureHelpItem(isVariadic:=False, documentationFactory:=Nothing, prefixParts:=New List(Of TaggedText), separatorParts:={}, suffixParts:={}, parameters:={}, descriptionParts:={})).ToList()
        End Function

        Friend Class MockSignatureHelpProvider
            Implements ISignatureHelpProvider

            Private ReadOnly _items As IList(Of SignatureHelpItem)

            Public Sub New(items As IList(Of SignatureHelpItem))
                Me._items = items
            End Sub

            Public Property GetItemsCount As Integer

            Public Function GetItemsAsync(document As Document, position As Integer, triggerInfo As SignatureHelpTriggerInfo, options As MemberDisplayOptions, cancellationToken As CancellationToken) As Task(Of SignatureHelpItems) Implements ISignatureHelpProvider.GetItemsAsync
                GetItemsCount += 1
                Return Task.FromResult(If(_items.Any(),
                                       New SignatureHelpItems(_items, TextSpan.FromBounds(position, position), selectedItem:=0, semanticParameterIndex:=0, syntacticArgumentCount:=0, argumentName:=Nothing),
                                       Nothing))
            End Function

            Public Function IsTriggerCharacter(ch As Char) As Boolean Implements ISignatureHelpProvider.IsTriggerCharacter
                Return ch = "("c
            End Function

            Public Function IsRetriggerCharacter(ch As Char) As Boolean Implements ISignatureHelpProvider.IsRetriggerCharacter
                Return ch = ")"c
            End Function
        End Class

        Private Shared Function CreateMockTextView(buffer As ITextBuffer) As Mock(Of ITextView)
            Dim caret = New Mock(Of ITextCaret)(MockBehavior.Strict)
            caret.Setup(Function(c) c.Position).Returns(Function() New CaretPosition(New VirtualSnapshotPoint(buffer.CurrentSnapshot, buffer.CurrentSnapshot.Length), CreateMock(Of IMappingPoint), PositionAffinity.Predecessor))
            Dim view = New Mock(Of ITextView)(MockBehavior.Strict) With {.DefaultValue = DefaultValue.Mock}
            view.Setup(Function(v) v.Caret).Returns(caret.Object)
            view.Setup(Function(v) v.TextBuffer).Returns(buffer)
            view.Setup(Function(v) v.TextSnapshot).Returns(buffer.CurrentSnapshot)
            Dim bufferGraph = New Mock(Of IBufferGraph)(MockBehavior.Strict)
            view.Setup(Function(v) v.BufferGraph).Returns(bufferGraph.Object)
            Return view
        End Function

        Private Shared Function CreateMock(Of T As Class)() As T
            Dim mock = New Mock(Of T)(MockBehavior.Strict)
            Return mock.Object
        End Function

        Private Class ControllerMocks
            Public ReadOnly View As Mock(Of ITextView)
            Public ReadOnly Buffer As ITextBuffer
            Public ReadOnly Presenter As Mock(Of IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession))
            Public ReadOnly PresenterSession As Mock(Of ISignatureHelpPresenterSession)
            Public ReadOnly AsyncListener As IAsynchronousOperationListener
            Public ReadOnly DocumentProvider As Mock(Of IDocumentProvider)
            Public ReadOnly Provider As MockSignatureHelpProvider

            Public Sub New(view As Mock(Of ITextView),
                           buffer As ITextBuffer,
                           presenter As Mock(Of IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession)),
                           presenterSession As Mock(Of ISignatureHelpPresenterSession),
                           asyncListener As IAsynchronousOperationListener,
                           documentProvider As Mock(Of IDocumentProvider),
                           provider As MockSignatureHelpProvider)
                Me.View = view
                Me.Buffer = buffer
                Me.Presenter = presenter
                Me.PresenterSession = presenterSession
                Me.AsyncListener = asyncListener
                Me.DocumentProvider = documentProvider
                Me.Provider = provider
            End Sub
        End Class
    End Class
End Namespace
