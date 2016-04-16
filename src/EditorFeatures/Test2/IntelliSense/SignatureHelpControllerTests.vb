' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows.Threading
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Moq

#Disable Warning RS0007 ' Avoid zero-length array allocations. This is non-shipping test code.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Public Class SignatureHelpControllerTests
        Public Sub New()
            ' The controller expects to be on a UI thread
            TestWorkspace.ResetThreadAffinity()
        End Sub

        <WpfFact>
        Public Sub InvokeSignatureHelpWithoutDocumentShouldNotStartNewSession()
            Dim emptyProvider = New Mock(Of IDocumentProvider)
            emptyProvider.Setup(Function(p) p.GetDocumentAsync(It.IsAny(Of ITextSnapshot), It.IsAny(Of CancellationToken))).Returns(Task.FromResult(DirectCast(Nothing, Document)))
            Dim controller As Controller = CreateController(documentProvider:=emptyProvider)
            controller.WaitForController()

            Assert.Equal(0, GetMocks(controller).Provider.GetItemsCount)
        End Sub

        <WpfFact>
        Public Sub InvokeSignatureHelpWithDocumentShouldStartNewSession()
            Dim controller = CreateController()

            GetMocks(controller).Presenter.Verify(Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of ISignatureHelpSession)), Times.Once)
        End Sub

        <WpfFact>
        Public Sub EmptyModelShouldStopSession()
            Dim controller = CreateController(items:={}, waitForPresentation:=True)

            GetMocks(controller).PresenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact>
        Public Sub UpKeyShouldDismissWhenThereIsOnlyOneItem()
            Dim controller = CreateController(items:=CreateItems(1), waitForPresentation:=True)

            Dim handled = controller.TryHandleUpKey()

            Assert.False(handled)
            GetMocks(controller).PresenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact>
        Public Sub UpKeyShouldNavigateWhenThereAreMultipleItems()
            Dim controller = CreateController(items:=CreateItems(2), waitForPresentation:=True)

            Dim handled = controller.TryHandleUpKey()

            Assert.True(handled)
            GetMocks(controller).PresenterSession.Verify(Sub(p) p.SelectPreviousItem(), Times.Once)
        End Sub

        <WpfFact>
        <WorkItem(985007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/985007")>
        Public Sub UpKeyShouldNotCrashWhenSessionIsDismissed()
            ' Create a provider that will return an empty state when queried the second time
            Dim slowProvider = New Mock(Of ISignatureHelpProvider)
            slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), It.IsAny(Of CancellationToken))) _
                .Returns(Task.FromResult(New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 0), selectedItem:=0, argumentIndex:=0, argumentCount:=0, argumentName:=Nothing)))
            Dim controller = CreateController(provider:=slowProvider.Object, waitForPresentation:=True)

            ' Now force an update to the model that will result in stopping the session
            slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), It.IsAny(Of CancellationToken))) _
                .Returns(Task.FromResult(Of SignatureHelpItems)(Nothing))

            DirectCast(controller, ICommandHandler(Of TypeCharCommandArgs)).ExecuteCommand(
                New TypeCharCommandArgs(CreateMock(Of ITextView), CreateMock(Of ITextBuffer), " "c),
                Sub() GetMocks(controller).Buffer.Insert(0, " "))

            Dim handled = controller.TryHandleUpKey() ' this will block on the model being updated which should dismiss the session

            Assert.False(handled)
            GetMocks(controller).PresenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact>
        <WorkItem(179726, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workItems?id=179726&_a=edit")>
        Public Sub DownKeyShouldNotBlockOnModelComputation()
            Dim mre = New ManualResetEvent(False)
            Dim controller = CreateController(items:=CreateItems(2), waitForPresentation:=False)
            Dim slowProvider = New Mock(Of ISignatureHelpProvider)
            slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), It.IsAny(Of CancellationToken))) _
                .Returns(Function()
                             mre.WaitOne()
                             Return Task.FromResult(New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 0), selectedItem:=0, argumentIndex:=0, argumentCount:=0, argumentName:=Nothing))
                         End Function)


            Dim handled = controller.TryHandleDownKey()

            Assert.False(handled)
        End Sub

        <WpfFact>
        <WorkItem(179726, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workItems?id=179726&_a=edit")>
        Public Sub UpKeyShouldNotBlockOnModelComputation()
            Dim mre = New ManualResetEvent(False)
            Dim controller = CreateController(items:=CreateItems(2), waitForPresentation:=False)
            Dim slowProvider = New Mock(Of ISignatureHelpProvider)
            slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), It.IsAny(Of CancellationToken))) _
                .Returns(Function()
                             mre.WaitOne()
                             Return Task.FromResult(New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 0), selectedItem:=0, argumentIndex:=0, argumentCount:=0, argumentName:=Nothing))
                         End Function)


            Dim handled = controller.TryHandleUpKey()

            Assert.False(handled)
        End Sub

        <WpfFact>
        <WorkItem(179726, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workItems?id=179726&_a=edit")>
        Public Async Function UpKeyShouldBlockOnRecomputationAfterPresentation() As Task
            Dim dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher
            Dim worker = Async Function()
                             Dim slowProvider = New Mock(Of ISignatureHelpProvider)
                             slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), It.IsAny(Of CancellationToken))) _
                                 .Returns(Task.FromResult(New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 0), selectedItem:=0, argumentIndex:=0, argumentCount:=0, argumentName:=Nothing)))

                             Dim controller = dispatcher.Invoke(Function() CreateController(provider:=slowProvider.Object, waitForPresentation:=True))

                             ' Update session so that providers are requeried.
                             ' SlowProvider now blocks on the checkpoint's task.
                             Dim checkpoint = New Checkpoint()
                             slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), It.IsAny(Of CancellationToken))) _
                                 .Returns(Function()
                                              checkpoint.Task.Wait()
                                              Return Task.FromResult(New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 2), selectedItem:=0, argumentIndex:=0, argumentCount:=0, argumentName:=Nothing))
                                          End Function)

                             dispatcher.Invoke(Sub() DirectCast(controller, ICommandHandler(Of TypeCharCommandArgs)).ExecuteCommand(
                                 New TypeCharCommandArgs(CreateMock(Of ITextView), CreateMock(Of ITextBuffer), " "c),
                                 Sub() GetMocks(controller).Buffer.Insert(0, " ")))

                             Dim handled = dispatcher.InvokeAsync(Function() controller.TryHandleUpKey()) ' Send the controller an up key, which should block on the computation
                             checkpoint.Release() ' Allow slowprovider to finish
                             Await handled.Task.ConfigureAwait(False)

                             ' We expect 2 calls to the presenter (because we had an existing presentation session when we started the second computation).
                             Assert.True(handled.Result)
                             GetMocks(controller).PresenterSession.Verify(Sub(p) p.PresentItems(It.IsAny(Of ITrackingSpan), It.IsAny(Of IList(Of SignatureHelpItem)),
                                                                                                It.IsAny(Of SignatureHelpItem), It.IsAny(Of Integer?)), Times.Exactly(2))
                         End Function
            Await worker().ConfigureAwait(False)

        End Function

        <WpfFact>
        Public Sub DownKeyShouldNavigateWhenThereAreMultipleItems()
            Dim controller = CreateController(items:=CreateItems(2), waitForPresentation:=True)

            Dim handled = controller.TryHandleDownKey()

            Assert.True(handled)
            GetMocks(controller).PresenterSession.Verify(Sub(p) p.SelectNextItem(), Times.Once)
        End Sub

        <WorkItem(1181, "https://github.com/dotnet/roslyn/issues/1181")>
        <WpfFact>
        Public Sub UpAndDownKeysShouldStillNavigateWhenDuplicateItemsAreFiltered()
            Dim item = CreateItems(1).Single()
            Dim controller = CreateController(items:={item, item}, waitForPresentation:=True)

            Dim handled = controller.TryHandleUpKey()

            Assert.False(handled)
            GetMocks(controller).PresenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact>
        Public Sub CaretMoveWithActiveSessionShouldRecomputeModel()
            Dim controller = CreateController(waitForPresentation:=True)

            Mock.Get(GetMocks(controller).View.Object.Caret).Raise(Sub(c) AddHandler c.PositionChanged, Nothing, New CaretPositionChangedEventArgs(Nothing, Nothing, Nothing))
            controller.WaitForController()

            ' GetItemsAsync is called once initially, and then once as a result of handling the PositionChanged event
            Assert.Equal(2, GetMocks(controller).Provider.GetItemsCount)
        End Sub

        <WpfFact>
        Public Sub RetriggerActiveSessionOnClosingBrace()
            Dim controller = CreateController(waitForPresentation:=True)

            DirectCast(controller, ICommandHandler(Of TypeCharCommandArgs)).ExecuteCommand(
                New TypeCharCommandArgs(CreateMock(Of ITextView), CreateMock(Of ITextBuffer), ")"c),
                Sub() GetMocks(controller).Buffer.Insert(0, ")"))
            controller.WaitForController()

            ' GetItemsAsync is called once initially, and then once as a result of handling the typechar command
            Assert.Equal(2, GetMocks(controller).Provider.GetItemsCount)
        End Sub

        <WpfFact>
        <WorkItem(959116, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/959116")>
        Public Sub TypingNonTriggerCharacterShouldNotRequestDocument()
            Dim controller = CreateController(triggerSession:=False)

            DirectCast(controller, ICommandHandler(Of TypeCharCommandArgs)).ExecuteCommand(
                New TypeCharCommandArgs(CreateMock(Of ITextView), CreateMock(Of ITextBuffer), "a"c),
                Sub() GetMocks(controller).Buffer.Insert(0, "a"))

            GetMocks(controller).DocumentProvider.Verify(Function(p) p.GetDocumentAsync(It.IsAny(Of ITextSnapshot), It.IsAny(Of CancellationToken)), Times.Never)
        End Sub

        ' Create an empty document to use as a non-null parameter when needed
        Private Shared ReadOnly s_document As Document =
            (Function()
                 Dim workspace = TestWorkspace.CreateWorkspace(
                     <Workspace>
                         <Project Language="C#">
                             <Document>
                             </Document>
                         </Project>
                     </Workspace>)
                 Return workspace.CurrentSolution.GetDocument(workspace.Documents.Single().Id)
             End Function)()
        Private Shared ReadOnly s_bufferFactory As ITextBufferFactoryService = DirectCast(s_document.Project.Solution.Workspace, TestWorkspace).GetService(Of ITextBufferFactoryService)

        Private Shared ReadOnly s_controllerMocksMap As New ConditionalWeakTable(Of Controller, ControllerMocks)
        Private Shared Function GetMocks(controller As Controller) As ControllerMocks
            Dim result As ControllerMocks = Nothing
            Roslyn.Utilities.Contract.ThrowIfFalse(s_controllerMocksMap.TryGetValue(controller, result))
            Return result
        End Function

        Private Shared Function CreateController(Optional documentProvider As Mock(Of IDocumentProvider) = Nothing,
                                                 Optional presenterSession As Mock(Of ISignatureHelpPresenterSession) = Nothing,
                                                 Optional items As IList(Of SignatureHelpItem) = Nothing,
                                                 Optional provider As ISignatureHelpProvider = Nothing,
                                                 Optional waitForPresentation As Boolean = False,
                                                 Optional triggerSession As Boolean = True) As Controller
            Dim buffer = s_bufferFactory.CreateTextBuffer()
            Dim view = CreateMockTextView(buffer)
            Dim asyncListener = New Mock(Of IAsynchronousOperationListener)
            If documentProvider Is Nothing Then
                documentProvider = New Mock(Of IDocumentProvider)
                documentProvider.Setup(Function(p) p.GetDocumentAsync(It.IsAny(Of ITextSnapshot), It.IsAny(Of CancellationToken))).Returns(Task.FromResult(s_document))
                documentProvider.Setup(Function(p) p.GetOpenDocumentInCurrentContextWithChanges(It.IsAny(Of ITextSnapshot))).Returns(s_document)
            End If

            If provider Is Nothing Then
                items = If(items, CreateItems(1))
                provider = New MockSignatureHelpProvider(items)
            End If

            Dim presenter = New Mock(Of IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession)) With {.DefaultValue = DefaultValue.Mock}
            presenterSession = If(presenterSession, New Mock(Of ISignatureHelpPresenterSession) With {.DefaultValue = DefaultValue.Mock})
            presenter.Setup(Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of ISignatureHelpSession))).Returns(presenterSession.Object)
            presenterSession.Setup(Sub(p) p.PresentItems(It.IsAny(Of ITrackingSpan), It.IsAny(Of IList(Of SignatureHelpItem)), It.IsAny(Of SignatureHelpItem), It.IsAny(Of Integer?))) _
                .Callback(Sub() presenterSession.SetupGet(Function(p) p.EditorSessionIsActive).Returns(True))


            Dim controller = New Controller(
                view.Object,
                buffer,
                presenter.Object,
                asyncListener.Object,
                documentProvider.Object,
                {provider})

            s_controllerMocksMap.Add(controller, New ControllerMocks(
                      view,
                      buffer,
                      presenter,
                      presenterSession,
                      asyncListener,
                      documentProvider,
                      TryCast(provider, MockSignatureHelpProvider)))

            If triggerSession Then
                DirectCast(controller, ICommandHandler(Of InvokeSignatureHelpCommandArgs)).ExecuteCommand(New InvokeSignatureHelpCommandArgs(view.Object, buffer), Nothing)
                If waitForPresentation Then
                    controller.WaitForController()
                End If
            End If

            Return controller
        End Function

        Private Shared Function CreateItems(count As Integer) As IList(Of SignatureHelpItem)
            Return Enumerable.Range(0, count).Select(Function(i) New SignatureHelpItem(isVariadic:=False, documentationFactory:=Nothing, prefixParts:={}, separatorParts:={}, suffixParts:={}, parameters:={}, descriptionParts:={})).ToList()
        End Function

        Friend Class MockSignatureHelpProvider
            Implements ISignatureHelpProvider

            Private ReadOnly _items As IList(Of SignatureHelpItem)

            Public Sub New(items As IList(Of SignatureHelpItem))
                Me._items = items
            End Sub

            Public Property GetItemsCount As Integer

            Public Function GetItemsAsync(document As Document, position As Integer, triggerInfo As SignatureHelpTriggerInfo, cancellationToken As CancellationToken) As Task(Of SignatureHelpItems) Implements ISignatureHelpProvider.GetItemsAsync
                GetItemsCount += 1
                Return Task.FromResult(If(_items.Any(),
                                       New SignatureHelpItems(_items, TextSpan.FromBounds(position, position), selectedItem:=0, argumentIndex:=0, argumentCount:=0, argumentName:=Nothing),
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
            Dim caret = New Mock(Of ITextCaret)
            caret.Setup(Function(c) c.Position).Returns(Function() New CaretPosition(New VirtualSnapshotPoint(buffer.CurrentSnapshot, buffer.CurrentSnapshot.Length), CreateMock(Of IMappingPoint), PositionAffinity.Predecessor))
            Dim view = New Mock(Of ITextView) With {.DefaultValue = DefaultValue.Mock}
            view.Setup(Function(v) v.Caret).Returns(caret.Object)
            view.Setup(Function(v) v.TextBuffer).Returns(buffer)
            view.Setup(Function(v) v.TextSnapshot).Returns(buffer.CurrentSnapshot)
            Return view
        End Function

        Private Shared Function CreateMock(Of T As Class)() As T
            Dim mock = New Mock(Of T)
            Return mock.Object
        End Function

        Private Class ControllerMocks
            Public ReadOnly View As Mock(Of ITextView)
            Public ReadOnly Buffer As ITextBuffer
            Public ReadOnly Presenter As Mock(Of IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession))
            Public ReadOnly PresenterSession As Mock(Of ISignatureHelpPresenterSession)
            Public ReadOnly AsyncListener As Mock(Of IAsynchronousOperationListener)
            Public ReadOnly DocumentProvider As Mock(Of IDocumentProvider)
            Public ReadOnly Provider As MockSignatureHelpProvider

            Public Sub New(view As Mock(Of ITextView), buffer As ITextBuffer, presenter As Mock(Of IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession)), presenterSession As Mock(Of ISignatureHelpPresenterSession), asyncListener As Mock(Of IAsynchronousOperationListener), documentProvider As Mock(Of IDocumentProvider), provider As MockSignatureHelpProvider)
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
