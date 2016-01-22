' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition.Hosting
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo.Presentation
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Utilities
Imports Moq
Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Public Class QuickInfoControllerTests

        Public Sub New()
            TestWorkspace.ResetThreadAffinity()
        End Sub

        <WpfFact>
        Public Sub InvokeQuickInfoWithoutDocumentShouldNotQueryProviders()
            Dim emptyProvider = New Mock(Of IDocumentProvider)
            emptyProvider.Setup(Function(p) p.GetDocumentAsync(It.IsAny(Of ITextSnapshot), It.IsAny(Of CancellationToken))).Returns(Task.FromResult(DirectCast(Nothing, Document)))
            Dim controller As Controller = CreateController(documentProvider:=emptyProvider, triggerQuickInfo:=True)
            controller.WaitForController()

            GetMocks(controller).Provider.Verify(Function(p) p.GetItemAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of CancellationToken)), Times.Never)
        End Sub

        <WpfFact>
        Public Sub InvokeQuickInfoWithDocumentShouldStartNewSession()
            Dim controller = CreateController(triggerQuickInfo:=True)

            GetMocks(controller).Presenter.Verify(Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of IQuickInfoSession)), Times.Once)
        End Sub

        <WpfFact>
        Public Sub InactiveQuickInfoShouldNotHandleEscape()
            Dim controller As Controller = CreateController()

            Dim handled = controller.TryHandleEscapeKey()

            Assert.False(handled)
        End Sub

        <WpfFact>
        Public Sub ActiveQuickInfoShouldHandleEscape()
            Dim presenter = New Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession))
            Dim presenterSession = New Mock(Of IQuickInfoPresenterSession)
            presenter.Setup(Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of IQuickInfoSession))).Returns(presenterSession.Object)

            Dim controller As Controller = CreateController(presenter:=presenter, triggerQuickInfo:=True)
            controller.WaitForController()

            Dim handled = controller.TryHandleEscapeKey()

            Assert.True(handled)
            presenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact>
        Public Sub SlowQuickInfoShouldDismissSessionButNotHandleEscape()
            Dim checkpoint As New Checkpoint
            Dim presenter = New Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession))
            Dim presenterSession = New Mock(Of IQuickInfoPresenterSession)
            presenter.Setup(Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of IQuickInfoSession))).Returns(presenterSession.Object)
            Dim provider = New Mock(Of IQuickInfoProvider)
            provider.Setup(Function(p) p.GetItemAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of CancellationToken))) _
                .Returns(Async Function()
                             Await checkpoint.Task.ConfigureAwait(False)
                             Return New QuickInfoItem(Nothing, Nothing)
                         End Function)

            Dim controller As Controller = CreateController(presenter:=presenter, provider:=provider, triggerQuickInfo:=True)

            Dim handled = controller.TryHandleEscapeKey()

            Assert.False(handled)
            presenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact>
        Public Sub CaretPositionChangedShouldDismissSession()
            Dim presenter = New Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession))
            Dim presenterSession = New Mock(Of IQuickInfoPresenterSession)
            presenter.Setup(Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of IQuickInfoSession))).Returns(presenterSession.Object)
            Dim controller = CreateController(presenter:=presenter, triggerQuickInfo:=True)

            Mock.Get(GetMocks(controller).View.Object.Caret).Raise(Sub(c) AddHandler c.PositionChanged, Nothing, New CaretPositionChangedEventArgs(Nothing, Nothing, Nothing))

            presenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact>
        Public Sub SubjectBufferPostChangedShouldDismissSession()
            Dim presenter = New Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession))
            Dim presenterSession = New Mock(Of IQuickInfoPresenterSession)
            presenter.Setup(Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of IQuickInfoSession))).Returns(presenterSession.Object)
            Dim controller = CreateController(presenter:=presenter, triggerQuickInfo:=True)

            Mock.Get(GetMocks(controller).View.Object.TextBuffer).Raise(Sub(b) AddHandler b.PostChanged, Nothing, New EventArgs())

            presenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact(), WorkItem(1106729)>
        Public Sub PresenterUpdatesExistingSessionIfNotDismissed()
            Dim broker = New Mock(Of IQuickInfoBroker)()
            Dim presenter As IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession) = New QuickInfoPresenter(broker.Object)
            Dim mockEditorSession = New Mock(Of IQuickInfoSession)
            mockEditorSession.Setup(Function(m) m.IsDismissed).Returns(False)
            mockEditorSession.Setup(Sub(m) m.Recalculate())
            mockEditorSession.Setup(Function(m) m.Properties).Returns(New PropertyCollection())

            Dim presenterSession = presenter.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), mockEditorSession.Object)
            presenterSession.PresentItem(Nothing, Nothing, False)
            mockEditorSession.Verify(Sub(m) m.Recalculate())
        End Sub

        <WpfFact>
        Public Sub PresenterDoesNotRecalculateDismissedSession()
            Dim broker = New Mock(Of IQuickInfoBroker)()
            Dim brokerSession = New Mock(Of IQuickInfoSession)()
            brokerSession.Setup(Function(m) m.Properties).Returns(New PropertyCollection())
            broker.Setup(Function(m) m.CreateQuickInfoSession(It.IsAny(Of ITextView), It.IsAny(Of ITrackingPoint), It.IsAny(Of Boolean))).Returns(brokerSession.Object)

            Dim presenter As IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession) = New QuickInfoPresenter(broker.Object)
            Dim mockEditorSession = New Mock(Of IQuickInfoSession)
            mockEditorSession.Setup(Function(m) m.IsDismissed).Returns(True)
            mockEditorSession.Setup(Sub(m) m.Recalculate())
            mockEditorSession.Setup(Function(m) m.Properties).Returns(New PropertyCollection())

            Dim mockTextBuffer = New Mock(Of ITextBuffer)()
            mockTextBuffer.Setup(Function(m) m.CurrentSnapshot).Returns(It.IsAny(Of ITextSnapshot))

            Dim mockTrackingSpan = New Mock(Of ITrackingSpan)
            mockTrackingSpan.Setup(Function(m) m.TextBuffer).Returns(mockTextBuffer.Object)
            mockTrackingSpan.Setup(Function(m) m.GetSpan(It.IsAny(Of ITextSnapshot))).Returns(New SnapshotSpan())
            mockTrackingSpan.Setup(Function(m) m.GetStartPoint(It.IsAny(Of ITextSnapshot))).Returns(New SnapshotPoint(New Mock(Of ITextSnapshot)().Object, 0))

            Dim presenterSession = presenter.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), mockEditorSession.Object)
            presenterSession.PresentItem(mockTrackingSpan.Object, Nothing, False)
            mockEditorSession.Verify(Sub(m) m.Recalculate(), Times.Never)
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
                                                 Optional presenter As Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession)) = Nothing,
                                                 Optional provider As Mock(Of IQuickInfoProvider) = Nothing,
                                                 Optional triggerQuickInfo As Boolean = False,
                                                 Optional augmentSession As IQuickInfoSession = Nothing) As Controller
            Dim view = New Mock(Of ITextView) With {.DefaultValue = DefaultValue.Mock}
            Dim asyncListener = New Mock(Of IAsynchronousOperationListener)
            Dim buffer = s_bufferFactory.CreateTextBuffer()

            presenter = If(presenter, New Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession)) With {.DefaultValue = DefaultValue.Mock})
            If documentProvider Is Nothing Then
                documentProvider = New Mock(Of IDocumentProvider)
                documentProvider.Setup(Function(p) p.GetDocumentAsync(It.IsAny(Of ITextSnapshot), It.IsAny(Of CancellationToken))).Returns(Task.FromResult(s_document))
            End If

            If provider Is Nothing Then
                provider = New Mock(Of IQuickInfoProvider)
                provider.Setup(Function(p) p.GetItemAsync(It.IsAny(Of Document), It.IsAny(Of Integer), It.IsAny(Of CancellationToken))) _
                    .Returns(Task.FromResult(New QuickInfoItem(Nothing, Nothing)))
            End If

            Dim controller = New Controller(
                view.Object,
                buffer,
                presenter.Object,
                asyncListener.Object,
                documentProvider.Object,
                {provider.Object})

            s_controllerMocksMap.Add(controller, New ControllerMocks(
                      view,
                      buffer,
                      presenter,
                      asyncListener,
                      documentProvider,
                      provider))

            If triggerQuickInfo Then
                controller.InvokeQuickInfo(position:=0, trackMouse:=False, augmentSession:=augmentSession)
            End If

            Return controller
        End Function

        Private Shared Function CreateMock(Of T As Class)() As T
            Dim mock = New Mock(Of T)
            Return mock.Object
        End Function

        Private Class ControllerMocks
            Public ReadOnly View As Mock(Of ITextView)
            Public ReadOnly Buffer As ITextBuffer
            Public ReadOnly Presenter As Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession))
            Public ReadOnly AsyncListener As Mock(Of IAsynchronousOperationListener)
            Public ReadOnly DocumentProvider As Mock(Of IDocumentProvider)
            Public ReadOnly Provider As Mock(Of IQuickInfoProvider)

            Public Sub New(view As Mock(Of ITextView), buffer As ITextBuffer, presenter As Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession)), asyncListener As Mock(Of IAsynchronousOperationListener), documentProvider As Mock(Of IDocumentProvider), provider As Mock(Of IQuickInfoProvider))
                Me.View = view
                Me.Buffer = buffer
                Me.Presenter = presenter
                Me.AsyncListener = asyncListener
                Me.DocumentProvider = documentProvider
                Me.Provider = provider
            End Sub


        End Class
    End Class
End Namespace
