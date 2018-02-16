' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
Imports Microsoft.CodeAnalysis.Editor.QuickInfo.Presentation
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.QuickInfo
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Utilities
Imports Moq
Imports Roslyn.Utilities
Imports QuickInfoItem = Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem

#Disable Warning BC40000 ' IQuickInfo* is obsolete
Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Public Class QuickInfoControllerTests

        Public Sub New()
            TestWorkspace.ResetThreadAffinity()
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub InvokeQuickInfoWithoutDocumentShouldNotQueryProviders()
            Dim mocks = CreateMocks(noDocument:=True, triggerQuickInfo:=True)
            mocks.WaitForController()

            Assert.Equal(0, mocks.Service.GetQuickInfoAsync_InvokeCount)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub InvokeQuickInfoWithDocumentShouldStartNewSession()
            Dim mocks = CreateMocks(triggerQuickInfo:=True)

            mocks.PresenterMock _
                .Verify(
                    Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of IQuickInfoSession)),
                    Times.Once)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub InactiveQuickInfoShouldNotHandleEscape()
            Dim mocks = CreateMocks()

            Dim handled = mocks.TryHandleEscapeKey()

            Assert.False(handled)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub ActiveQuickInfoShouldHandleEscape()
            Dim presenter = New Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession))
            Dim presenterSession = New Mock(Of IQuickInfoPresenterSession)
            presenter _
                .Setup(Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of IQuickInfoSession))) _
                .Returns(presenterSession.Object)

            Dim mocks = CreateMocks(presenter:=presenter, triggerQuickInfo:=True)

            Dim emptyQuickInfoItem = QuickInfoItem.Create(New Text.TextSpan(0, 0))
            mocks.Service.SetItemToReturn(emptyQuickInfoItem)

            mocks.WaitForController()

            Dim handled = mocks.TryHandleEscapeKey()

            Assert.True(handled)
            presenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub SlowQuickInfoShouldDismissSessionButNotHandleEscape()
            Dim presenter = New Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession))
            Dim presenterSession = New Mock(Of IQuickInfoPresenterSession)
            presenter _
                .Setup(Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of IQuickInfoSession))) _
                .Returns(presenterSession.Object)

            Dim mocks = CreateMocks(presenter:=presenter, triggerQuickInfo:=True)

            Dim checkpoint = New Checkpoint()
            mocks.Service.SetCheckpoint(checkpoint)

            Dim handled = mocks.TryHandleEscapeKey()

            Assert.False(handled)
            presenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub CaretPositionChangedShouldDismissSession()
            Dim presenter = New Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession))
            Dim presenterSession = New Mock(Of IQuickInfoPresenterSession)
            presenter _
                .Setup(Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of IQuickInfoSession))) _
                .Returns(presenterSession.Object)

            Dim mocks = CreateMocks(presenter:=presenter, triggerQuickInfo:=True)

            Mock.Get(mocks.View.Caret) _
                .Raise(Sub(c) AddHandler c.PositionChanged, Nothing, New CaretPositionChangedEventArgs(Nothing, Nothing, Nothing))

            presenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub SubjectBufferPostChangedShouldDismissSession()
            Dim presenter = New Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession))
            Dim presenterSession = New Mock(Of IQuickInfoPresenterSession)
            presenter _
                .Setup(Function(p) p.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), It.IsAny(Of IQuickInfoSession))) _
                .Returns(presenterSession.Object)

            Dim mocks = CreateMocks(presenter:=presenter, triggerQuickInfo:=True)

            Mock.Get(mocks.View.TextBuffer) _
                .Raise(Sub(b) AddHandler b.PostChanged, Nothing, New EventArgs())

            presenterSession.Verify(Sub(p) p.Dismiss(), Times.Once)
        End Sub

        <WorkItem(1106729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106729")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub PresenterUpdatesExistingSessionIfNotDismissed()
            Dim broker = New Mock(Of IQuickInfoBroker)()
            Dim presenter As IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession) =
                New QuickInfoPresenter(broker.Object, Nothing, Nothing, Nothing, Nothing, Nothing)

            Dim quickInfoSession = New Mock(Of IQuickInfoSession)
            With quickInfoSession
                .Setup(Function(m) m.IsDismissed).Returns(False)
                .Setup(Sub(m) m.Recalculate())
                .Setup(Function(m) m.Properties).Returns(New PropertyCollection())
            End With

            Dim presenterSession = presenter.CreateSession(It.IsAny(Of ITextView), It.IsAny(Of ITextBuffer), quickInfoSession.Object)
            presenterSession.PresentItem(Nothing, Nothing, False)
            quickInfoSession.Verify(Sub(m) m.Recalculate())
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub PresenterDoesNotRecalculateDismissedSession()
            Dim quickInfoSession = New Mock(Of IQuickInfoSession)()
            quickInfoSession _
                .Setup(Function(m) m.Properties) _
                .Returns(New PropertyCollection())

            Dim broker = New Mock(Of IQuickInfoBroker)()
            broker _
                .Setup(Function(m) m.CreateQuickInfoSession(It.IsAny(Of ITextView), It.IsAny(Of ITrackingPoint), It.IsAny(Of Boolean))) _
                .Returns(quickInfoSession.Object)

            Dim presenter As IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession) =
                New QuickInfoPresenter(broker.Object, Nothing, Nothing, Nothing, Nothing, Nothing)

            Dim mockEditorSession = New Mock(Of IQuickInfoSession)
            With mockEditorSession
                .Setup(Function(m) m.IsDismissed) _
                    .Returns(True)
                .Setup(Sub(m) m.Recalculate())
                .Setup(Function(m) m.Properties) _
                    .Returns(New PropertyCollection())
            End With

            Dim mockTextBuffer = New Mock(Of ITextBuffer)()
            mockTextBuffer _
                .Setup(Function(m) m.CurrentSnapshot) _
                .Returns(It.IsAny(Of ITextSnapshot))

            Dim mockTrackingSpan = New Mock(Of ITrackingSpan)
            With mockTrackingSpan
                .Setup(Function(m) m.TextBuffer) _
                    .Returns(mockTextBuffer.Object)
                .Setup(Function(m) m.GetSpan(It.IsAny(Of ITextSnapshot))) _
                    .Returns(New SnapshotSpan())
                .Setup(Function(m) m.GetStartPoint(It.IsAny(Of ITextSnapshot))) _
                    .Returns(New SnapshotPoint(New Mock(Of ITextSnapshot)().Object, 0))
            End With

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

        Private Class QuickInfoMocks
            Private ReadOnly _controller As Controller
            Private ReadOnly _viewMock As Mock(Of ITextView)

            Public ReadOnly Property PresenterMock As Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession))

            Public ReadOnly Property Service As MockQuickInfoService

            Public ReadOnly Property View As ITextView
                Get
                    Return _viewMock.Object
                End Get
            End Property

            Public Sub New(
                controller As Controller,
                service As MockQuickInfoService,
                viewMock As Mock(Of ITextView),
                presenterMock As Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession)))

                _controller = controller
                Me.Service = service
                _viewMock = viewMock
                Me.PresenterMock = presenterMock
            End Sub

            Public Sub WaitForController()
                _controller.WaitForController()
            End Sub

            Public Function TryHandleEscapeKey() As Boolean
                Return _controller.TryHandleEscapeKey()
            End Function
        End Class

        Private Class MockQuickInfoService
            Inherits QuickInfoService

            Private _item As QuickInfoItem
            Private _checkpoint As Checkpoint

            Public GetQuickInfoAsync_InvokeCount As Integer

            Public Sub SetItemToReturn(item As QuickInfoItem)
                _item = item
            End Sub

            Public Sub SetCheckpoint(checkpoint As Checkpoint)
                _checkpoint = checkpoint
            End Sub

            Public Overrides Async Function GetQuickInfoAsync(
                document As Document,
                position As Integer,
                Optional cancellationToken As CancellationToken = Nothing
            ) As Task(Of QuickInfoItem)

                If _checkpoint IsNot Nothing Then
                    Await _checkpoint.Task.ConfigureAwait(False)
                End If

                GetQuickInfoAsync_InvokeCount += 1
                Return _item
            End Function
        End Class

        Private Shared Function CreateMocks(
            Optional presenter As Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession)) = Nothing,
            Optional noDocument As Boolean = False,
            Optional triggerQuickInfo As Boolean = False,
            Optional augmentSession As IQuickInfoSession = Nothing
        ) As QuickInfoMocks

            Dim view = New Mock(Of ITextView) With {
                .DefaultValue = DefaultValue.Mock
            }

            Dim asyncListener = New Mock(Of IAsynchronousOperationListener)
            Dim buffer = s_bufferFactory.CreateTextBuffer()

            If presenter Is Nothing Then
                presenter = New Mock(Of IIntelliSensePresenter(Of IQuickInfoPresenterSession, IQuickInfoSession)) With {
                    .DefaultValue = DefaultValue.Mock
                }
            End If

            Dim documentTask = If(
                noDocument,
                SpecializedTasks.Default(Of Document),
                Task.FromResult(s_document))

            Dim documentProvider = New Mock(Of IDocumentProvider)
            documentProvider _
                .Setup(Function(p) p.GetDocumentAsync(It.IsAny(Of ITextSnapshot), It.IsAny(Of CancellationToken))) _
                .Returns(documentTask)

            Dim service = New MockQuickInfoService()

            Dim controller = New Controller(
                view.Object,
                buffer,
                presenter.Object,
                asyncListener.Object,
                documentProvider.Object,
                service)

            Dim mocks = New QuickInfoMocks(controller, service, view, presenter)

            If triggerQuickInfo Then
                controller.InvokeQuickInfo(position:=0, trackMouse:=False, augmentSession)
            End If

            Return mocks
        End Function

    End Class
End Namespace
#Enable Warning BC40000 ' IQuickInfo* is obsolete
