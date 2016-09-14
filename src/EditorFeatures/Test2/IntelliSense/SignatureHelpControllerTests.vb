' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows.Threading
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.SignatureHelp
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Moq

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class SignatureHelpControllerTests
        Public Sub New()
            ' The controller expects to be on a UI thread
            TestWorkspace.ResetThreadAffinity()
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub InvokeSignatureHelpWithoutDocumentShouldNotStartNewSession()
            Dim emptyProvider = New Mock(Of IDocumentProvider)
            emptyProvider _
                .Setup(Function(p) p.GetDocumentAsync(
                    snapshot:=It.IsAny(Of ITextSnapshot),
                    cancellationToken:=It.IsAny(Of CancellationToken))) _
                .Returns(Task.FromResult(Of Document)(Nothing))

            Dim testData = CreateTestData(documentProvider:=emptyProvider)
            testData.WaitForController()

            Assert.Equal(0, testData.Provider.GetItemsCount)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub InvokeSignatureHelpWithDocumentShouldStartNewSession()
            Dim testData = CreateTestData()

            testData.Presenter _
                .Verify(
                    Function(p) p.CreateSession(
                        textView:=It.IsAny(Of ITextView),
                        subjectBuffer:=It.IsAny(Of ITextBuffer),
                        sessionOpt:=It.IsAny(Of ISignatureHelpSession)),
                    Times.Once)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub EmptyModelShouldStopSession()
            Dim testData = CreateTestData(items:={}, waitForPresentation:=True)

            testData.PresenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub UpKeyShouldDismissWhenThereIsOnlyOneItem()
            Dim testData = CreateTestData(items:=CreateItems(1), waitForPresentation:=True)

            Dim handled = testData.Controller.TryHandleUpKey()

            Assert.False(handled)
            testData.PresenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub UpKeyShouldNavigateWhenThereAreMultipleItems()
            Dim testData = CreateTestData(items:=CreateItems(2), waitForPresentation:=True)

            Dim handled = testData.Controller.TryHandleUpKey()

            Assert.True(handled)
            testData.PresenterSession.Verify(Sub(p) p.SelectPreviousItem(), Times.Once)
        End Sub

        <WorkItem(985007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/985007")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub UpKeyShouldNotCrashWhenSessionIsDismissed()
            Dim items = New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 0), selectedItem:=0, argumentIndex:=0, argumentCount:=0, argumentName:=Nothing)

            ' Create a provider that will return an empty state when queried the second time
            Dim slowProvider = New Mock(Of ISignatureHelpProvider)
            slowProvider _
                .Setup(Function(p) p.GetItemsAsync(
                    document:=It.IsAny(Of Document),
                    position:=It.IsAny(Of Integer),
                    triggerInfo:=It.IsAny(Of SignatureHelpTriggerInfo),
                    cancellationToken:=It.IsAny(Of CancellationToken))) _
                .Returns(Task.FromResult(items))

            Dim testData = CreateTestData(provider:=slowProvider.Object, waitForPresentation:=True)

            ' Now force an update to the model that will result in stopping the session
            slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), It.IsAny(Of CancellationToken))) _
                .Returns(Task.FromResult(Of SignatureHelpItems)(Nothing))

            testData.TypeChar(" "c)

            Dim handled = testData.Controller.TryHandleUpKey() ' this will block on the model being updated which should dismiss the session

            Assert.False(handled)
            testData.PresenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WorkItem(179726, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workItems?id=179726&_a=edit")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DownKeyShouldNotBlockOnModelComputation()
            Dim manualResetEvent = New ManualResetEvent(False)
            Dim testData = CreateTestData(items:=CreateItems(2), waitForPresentation:=False)
            Dim slowProvider = New Mock(Of ISignatureHelpProvider)
            slowProvider _
                .Setup(Function(p) p.GetItemsAsync(
                    document:=It.IsAny(Of Document),
                    position:=It.IsAny(Of Integer),
                    triggerInfo:=It.IsAny(Of SignatureHelpTriggerInfo),
                    cancellationToken:=It.IsAny(Of CancellationToken))) _
                .Returns(Function()
                             manualResetEvent.WaitOne()
                             Dim items = New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 0), selectedItem:=0, argumentIndex:=0, argumentCount:=0, argumentName:=Nothing)
                             Return Task.FromResult(items)
                         End Function)

            Dim handled = testData.Controller.TryHandleDownKey()

            Assert.False(handled)
        End Sub

        <WorkItem(179726, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workItems?id=179726&_a=edit")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub UpKeyShouldNotBlockOnModelComputation()
            Dim manualResetEvent = New ManualResetEvent(False)
            Dim testData = CreateTestData(items:=CreateItems(2), waitForPresentation:=False)
            Dim slowProvider = New Mock(Of ISignatureHelpProvider)
            slowProvider _
                .Setup(Function(p) p.GetItemsAsync(
                    document:=It.IsAny(Of Document),
                    position:=It.IsAny(Of Integer),
                    triggerInfo:=It.IsAny(Of SignatureHelpTriggerInfo),
                    cancellationToken:=It.IsAny(Of CancellationToken))) _
                .Returns(Function()
                             manualResetEvent.WaitOne()
                             Dim items = New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 0), selectedItem:=0, argumentIndex:=0, argumentCount:=0, argumentName:=Nothing)
                             Return Task.FromResult(items)
                         End Function)

            Dim handled = testData.Controller.TryHandleUpKey()

            Assert.False(handled)
        End Sub

        <WorkItem(179726, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workItems?id=179726&_a=edit")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function UpKeyShouldBlockOnRecomputationAfterPresentation() As Task
            Dim dispatcher = Windows.Threading.Dispatcher.CurrentDispatcher
            Dim worker = Async Function()
                             Dim slowProvider = New Mock(Of ISignatureHelpProvider)
                             slowProvider _
                                 .Setup(Function(p) p.GetItemsAsync(
                                    document:=It.IsAny(Of Document),
                                    position:=It.IsAny(Of Integer),
                                    triggerInfo:=It.IsAny(Of SignatureHelpTriggerInfo),
                                    cancellationToken:=It.IsAny(Of CancellationToken))) _
                                 .Returns(Task.FromResult(New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 0), selectedItem:=0, argumentIndex:=0, argumentCount:=0, argumentName:=Nothing)))

                             Dim testData = dispatcher.Invoke(Function() CreateTestData(provider:=slowProvider.Object, waitForPresentation:=True))

                             ' Update session so that providers are requeried.
                             ' SlowProvider now blocks on the checkpoint's task.
                             Dim checkpoint = New Checkpoint()
                             slowProvider _
                                 .Setup(Function(p) p.GetItemsAsync(
                                    document:=It.IsAny(Of Document),
                                    position:=It.IsAny(Of Integer),
                                    triggerInfo:=It.IsAny(Of SignatureHelpTriggerInfo),
                                    cancellationToken:=It.IsAny(Of CancellationToken))) _
                                 .Returns(Function()
                                              checkpoint.Task.Wait()
                                              Dim items = New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 2), selectedItem:=0, argumentIndex:=0, argumentCount:=0, argumentName:=Nothing)
                                              Return Task.FromResult(items)
                                          End Function)

                             dispatcher.Invoke(Sub() testData.TypeChar(" "c))

                             Dim handled = dispatcher.InvokeAsync(Function() testData.Controller.TryHandleUpKey()) ' Send the controller an up key, which should block on the computation
                             checkpoint.Release() ' Allow slowprovider to finish
                             Await handled.Task.ConfigureAwait(False)

                             Assert.True(handled.Result)

                             ' We expect 2 calls to the presenter (because we had an existing presentation session when we started the second computation).
                             testData.PresenterSession.Verify(
                                Sub(p) p.PresentItems(
                                    triggerSpan:=It.IsAny(Of ITrackingSpan),
                                    items:=It.IsAny(Of IList(Of SignatureHelpItem)),
                                    selectedItem:=It.IsAny(Of SignatureHelpItem),
                                    selectedParameter:=It.IsAny(Of Integer?)),
                                Times.Exactly(2))
                         End Function

            Await worker().ConfigureAwait(False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DownKeyShouldNavigateWhenThereAreMultipleItems()
            Dim testData = CreateTestData(items:=CreateItems(2), waitForPresentation:=True)

            Dim handled = testData.Controller.TryHandleDownKey()

            Assert.True(handled)
            testData.PresenterSession.Verify(Sub(p) p.SelectNextItem(), Times.Once)
        End Sub

        <WorkItem(1181, "https://github.com/dotnet/roslyn/issues/1181")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub UpAndDownKeysShouldStillNavigateWhenDuplicateItemsAreFiltered()
            Dim item = CreateItems(1).Single()
            Dim testData = CreateTestData(items:={item, item}, waitForPresentation:=True)

            Dim handled = testData.Controller.TryHandleUpKey()

            Assert.False(handled)
            testData.PresenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub CaretMoveWithActiveSessionShouldRecomputeModel()
            Dim testData = CreateTestData(waitForPresentation:=True)

            testData.RaiseCaretPositionChanged()
            testData.Controller.WaitForController()

            ' GetItemsAsync is called once initially, and then once as a result of handling the PositionChanged event
            Assert.Equal(2, testData.Provider.GetItemsCount)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub RetriggerActiveSessionOnClosingBrace()
            Dim testData = CreateTestData(waitForPresentation:=True)

            testData.TypeChar(")"c)
            testData.Controller.WaitForController()

            ' GetItemsAsync is called once initially, and then once as a result of handling the typechar command to retrigger
            Assert.Equal(2, testData.Provider.GetItemsCount)
        End Sub

        <WorkItem(959116, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/959116")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TypingNonTriggerCharacterShouldNotRequestDocument()
            Dim testData = CreateTestData(triggerSession:=False)

            testData.TypeChar("a"c)

            testData.DocumentProvider.Verify(
                Function(p) p.GetDocumentAsync(
                    snapshot:=It.IsAny(Of ITextSnapshot),
                    cancellationToken:=It.IsAny(Of CancellationToken)),
                Times.Never)
        End Sub

        Private Shared Function CreateTestData(
            Optional documentProvider As Mock(Of IDocumentProvider) = Nothing,
            Optional presenterSession As Mock(Of ISignatureHelpPresenterSession) = Nothing,
            Optional items As IList(Of SignatureHelpItem) = Nothing,
            Optional provider As ISignatureHelpProvider = Nothing,
            Optional waitForPresentation As Boolean = False,
            Optional triggerSession As Boolean = True
        ) As TestData

            Dim workspace = TestWorkspace.CreateWorkspace(
                <Workspace>
                    <Project Language="C#">
                        <Document>
                        </Document>
                    </Project>
                </Workspace>)

            Dim hostDocument = workspace.Documents.Single()
            Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)

            Dim buffer = hostDocument.GetTextBuffer()
            Dim view = CreateMockTextView(buffer)

            If documentProvider Is Nothing Then
                documentProvider = SetupDefaultDocumentProvider(document)
            End If

            If provider Is Nothing Then
                items = If(items, CreateItems(1))
                provider = New MockSignatureHelpProvider(items)
            End If

            Dim signatureHelpService = DirectCast(document.GetLanguageService(Of SignatureHelpService), CommonSignatureHelpService)
            signatureHelpService.SetTestProviders({provider})

            Dim presenter = New Mock(Of IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession)) With {
                .DefaultValue = DefaultValue.Mock
            }

            If presenterSession Is Nothing Then
                presenterSession = New Mock(Of ISignatureHelpPresenterSession) With {
                    .DefaultValue = DefaultValue.Mock
                }
            End If

            presenter _
                .Setup(Function(p) p.CreateSession(
                    textView:=It.IsAny(Of ITextView),
                    subjectBuffer:=It.IsAny(Of ITextBuffer),
                    sessionOpt:=It.IsAny(Of ISignatureHelpSession))) _
                .Returns(presenterSession.Object)

            presenterSession _
                .Setup(Sub(p) p.PresentItems(
                    triggerSpan:=It.IsAny(Of ITrackingSpan),
                    items:=It.IsAny(Of IList(Of SignatureHelpItem)),
                    selectedItem:=It.IsAny(Of SignatureHelpItem),
                    selectedParameter:=It.IsAny(Of Integer?))) _
                .Callback(Sub() presenterSession.SetupGet(Function(p) p.EditorSessionIsActive).Returns(True))

            Dim asyncListener = New Mock(Of IAsynchronousOperationListener)

            Dim controller = New Controller(
                view.Object,
                buffer,
                presenter.Object,
                asyncListener.Object,
                documentProvider.Object)

            Dim testData = New TestData(
                controller,
                buffer,
                view,
                presenter,
                presenterSession,
                documentProvider,
                TryCast(provider, MockSignatureHelpProvider))

            If triggerSession Then
                testData.InvokeSignatureHelp(waitForPresentation)
            End If

            Return testData
        End Function

        Private Shared Function SetupDefaultDocumentProvider(document As Document) As Mock(Of IDocumentProvider)
            Dim documentProvider = New Mock(Of IDocumentProvider)

            documentProvider _
                .Setup(Function(p) p.GetDocumentAsync(
                    snapshot:=It.IsAny(Of ITextSnapshot),
                    cancellationToken:=It.IsAny(Of CancellationToken))) _
                .Returns(Task.FromResult(document))
            documentProvider _
                .Setup(Function(p) p.GetOpenDocumentInCurrentContextWithChanges(
                    snapshot:=It.IsAny(Of ITextSnapshot))) _
                .Returns(document)

            Return documentProvider
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

            Public Function GetItemsAsync(document As Document, position As Integer, triggerInfo As SignatureHelpTriggerInfo, cancellationToken As CancellationToken) As Task(Of SignatureHelpItems) Implements ISignatureHelpProvider.GetItemsAsync
                Trace.WriteLine("MockSignatureHelpProvider.GetItemsAsync")

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
            Dim mappingPoint = New Mock(Of IMappingPoint)
            caret.Setup(Function(c) c.Position).Returns(Function() New CaretPosition(New VirtualSnapshotPoint(buffer.CurrentSnapshot, buffer.CurrentSnapshot.Length), mappingPoint.Object, PositionAffinity.Predecessor))
            Dim view = New Mock(Of ITextView) With {.DefaultValue = DefaultValue.Mock}
            view.Setup(Function(v) v.Caret).Returns(caret.Object)
            view.Setup(Function(v) v.TextBuffer).Returns(buffer)
            view.Setup(Function(v) v.TextSnapshot).Returns(buffer.CurrentSnapshot)
            Return view
        End Function

        Private Class TestData
            Private ReadOnly _buffer As ITextBuffer
            Private ReadOnly _view As Mock(Of ITextView)

            Public ReadOnly Controller As Controller
            Public ReadOnly Presenter As Mock(Of IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession))
            Public ReadOnly PresenterSession As Mock(Of ISignatureHelpPresenterSession)
            Public ReadOnly DocumentProvider As Mock(Of IDocumentProvider)
            Public ReadOnly Provider As MockSignatureHelpProvider

            Public Sub New(controller As Controller, buffer As ITextBuffer, view As Mock(Of ITextView), presenter As Mock(Of IIntelliSensePresenter(Of ISignatureHelpPresenterSession, ISignatureHelpSession)), presenterSession As Mock(Of ISignatureHelpPresenterSession), documentProvider As Mock(Of IDocumentProvider), provider As MockSignatureHelpProvider)
                Me.Controller = controller
                Me._buffer = buffer
                Me._view = view
                Me.Presenter = presenter
                Me.PresenterSession = presenterSession
                Me.DocumentProvider = documentProvider
                Me.Provider = provider
            End Sub

            Public Sub InvokeSignatureHelp(Optional waitForPresentation As Boolean = False)
                Dim commandHandler = DirectCast(Controller, ICommandHandler(Of InvokeSignatureHelpCommandArgs))

                commandHandler.ExecuteCommand(
                    args:=New InvokeSignatureHelpCommandArgs(_view.Object, _buffer),
                    nextHandler:=Nothing)

                If waitForPresentation Then
                    Controller.WaitForController()
                End If
            End Sub

            Public Sub TypeChar(ch As Char)
                Dim commandHandler = DirectCast(Controller, ICommandHandler(Of TypeCharCommandArgs))
                commandHandler.ExecuteCommand(
                    args:=New TypeCharCommandArgs(_view.Object, _buffer, ch),
                    nextHandler:=Sub() _buffer.Insert(0, ch))
            End Sub
            Friend Sub RaiseCaretPositionChanged()
                Dim caretMock = Mock.Get(_view.Object.Caret)
                caretMock.Raise(Sub(c) AddHandler c.PositionChanged, Nothing, New CaretPositionChangedEventArgs(Nothing, Nothing, Nothing))
            End Sub

            Public Sub WaitForController()
                Controller.WaitForController()
            End Sub
        End Class
    End Class
End Namespace
