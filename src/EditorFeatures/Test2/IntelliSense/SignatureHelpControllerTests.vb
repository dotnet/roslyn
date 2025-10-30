' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
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
        Public Async Function InvokeSignatureHelpWithoutDocumentShouldNotStartNewSession() As Task
            Dim emptyProvider = New Mock(Of IDocumentProvider)(MockBehavior.Strict)
            emptyProvider.Setup(Function(p) p.GetDocument(It.IsAny(Of ITextSnapshot), It.IsAny(Of CancellationToken))).Returns(DirectCast(Nothing, Document))
            Dim controller As Controller = Await CreateController(CreateWorkspace(), documentProvider:=emptyProvider)

            GetMocks(controller).PresenterSession.Setup(Sub(p) p.Dismiss())

            Await controller.WaitForModelComputation_ForTestingPurposesOnlyAsync()

            Assert.Equal(0, GetMocks(controller).Provider.GetItemsCount)
        End Function

        <WpfFact>
        Public Async Function InvokeSignatureHelpWithDocumentShouldStartNewSession() As Task
            Dim controller = Await CreateController(CreateWorkspace())

            GetMocks(controller).Presenter.Verify(Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of ISignatureHelpSession)), Times.Once)
        End Function

        <WpfFact>
        Public Async Function EmptyModelShouldStopSession() As Task
            Dim presenterSession = New Mock(Of ISignatureHelpPresenterSession)(MockBehavior.Strict)
            presenterSession.Setup(Sub(p) p.Dismiss())

            Dim controller = Await CreateController(CreateWorkspace(), presenterSession:=presenterSession, items:={}, waitForPresentation:=True)

            GetMocks(controller).PresenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/985007")>
        Public Async Function UpKeyShouldNotCrashWhenSessionIsDismissed() As Task
            Dim options = New MemberDisplayOptions()

            ' Create a provider that will return an empty state when queried the second time
            Dim slowProvider = New Mock(Of ISignatureHelpProvider)(MockBehavior.Strict)
            slowProvider.Setup(Function(p) p.TriggerCharacters).Returns(ImmutableArray.Create(Of Char)(" "c))
            slowProvider.Setup(Function(p) p.RetriggerCharacters).Returns(ImmutableArray.Create(Of Char)(" "c))
            slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), options, It.IsAny(Of CancellationToken))) _
                .Returns(Task.FromResult(New SignatureHelpItems(CreateItems(2), TextSpan.FromBounds(0, 0), selectedItem:=0, semanticParameterIndex:=0, syntacticArgumentCount:=0, argumentName:=Nothing)))
            Dim controller = Await CreateController(CreateWorkspace(), provider:=slowProvider.Object, waitForPresentation:=True)

            ' Now force an update to the model that will result in stopping the session
            slowProvider.Setup(Function(p) p.GetItemsAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of SignatureHelpTriggerInfo), options, It.IsAny(Of CancellationToken))) _
                .Returns(Task.FromResult(Of SignatureHelpItems)(Nothing))

            DirectCast(controller, IChainedCommandHandler(Of TypeCharCommandArgs)).ExecuteCommand(
                New TypeCharCommandArgs(CreateMock(Of ITextView), CreateMock(Of ITextBuffer), " "c),
                Sub() GetMocks(controller).Buffer.Insert(0, " "), TestCommandExecutionContext.Create())
        End Function

        <WpfFact>
        Public Async Function CaretMoveWithActiveSessionShouldRecomputeModel() As Task
            Dim controller = Await CreateController(CreateWorkspace(), waitForPresentation:=True)

            Mock.Get(GetMocks(controller).View.Object.Caret).Raise(Sub(c) AddHandler c.PositionChanged, Nothing, New CaretPositionChangedEventArgs(Nothing, Nothing, Nothing))
            Await controller.WaitForModelComputation_ForTestingPurposesOnlyAsync()

            ' GetItemsAsync is called once initially, and then once as a result of handling the PositionChanged event
            Assert.Equal(2, GetMocks(controller).Provider.GetItemsCount)
        End Function

        <WpfFact>
        Public Async Function RetriggerActiveSessionOnClosingBrace() As Task
            Dim controller = Await CreateController(CreateWorkspace(), waitForPresentation:=True)

            DirectCast(controller, IChainedCommandHandler(Of TypeCharCommandArgs)).ExecuteCommand(
                New TypeCharCommandArgs(CreateMock(Of ITextView), CreateMock(Of ITextBuffer), ")"c),
                Sub() GetMocks(controller).Buffer.Insert(0, ")"), TestCommandExecutionContext.Create())
            Await controller.WaitForModelComputation_ForTestingPurposesOnlyAsync()

            ' GetItemsAsync is called once initially, and then once as a result of handling the typechar command
            Assert.Equal(2, GetMocks(controller).Provider.GetItemsCount)
        End Function

        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/959116")>
        Public Async Function TypingNonTriggerCharacterShouldNotRequestDocument() As Task
            Dim controller = Await CreateController(CreateWorkspace(), triggerSession:=False)

            DirectCast(controller, IChainedCommandHandler(Of TypeCharCommandArgs)).ExecuteCommand(
                New TypeCharCommandArgs(CreateMock(Of ITextView), CreateMock(Of ITextBuffer), "a"c),
                Sub() GetMocks(controller).Buffer.Insert(0, "a"), TestCommandExecutionContext.Create())

            GetMocks(controller).DocumentProvider.Verify(Function(p) p.GetDocument(It.IsAny(Of ITextSnapshot), It.IsAny(Of CancellationToken)), Times.Never)
        End Function

        Private Shared ReadOnly s_controllerMocksMap As New ConditionalWeakTable(Of Controller, ControllerMocks)
        Private Shared Function GetMocks(controller As Controller) As ControllerMocks
            Dim result As ControllerMocks = Nothing
            Contract.ThrowIfFalse(s_controllerMocksMap.TryGetValue(controller, result))
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

        Private Shared Async Function CreateController(
                workspace As TestWorkspace,
                Optional documentProvider As Mock(Of IDocumentProvider) = Nothing,
                Optional presenterSession As Mock(Of ISignatureHelpPresenterSession) = Nothing,
                Optional items As IList(Of SignatureHelpItem) = Nothing,
                Optional provider As ISignatureHelpProvider = Nothing,
                Optional waitForPresentation As Boolean = False,
                Optional triggerSession As Boolean = True) As Task(Of Controller)
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
                    Await controller.WaitForModelComputation_ForTestingPurposesOnlyAsync()
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

            Public ReadOnly Property TriggerCharacters As ImmutableArray(Of Char) Implements ISignatureHelpProvider.TriggerCharacters
                Get
                    Return ImmutableArray.Create("("c)
                End Get
            End Property

            Public ReadOnly Property RetriggerCharacters As ImmutableArray(Of Char) Implements ISignatureHelpProvider.RetriggerCharacters
                Get
                    Return ImmutableArray.Create(")"c)
                End Get
            End Property
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
