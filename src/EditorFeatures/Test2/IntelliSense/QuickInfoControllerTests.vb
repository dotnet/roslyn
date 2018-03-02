' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
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

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Public Class QuickInfoControllerTests

        Public Sub New()
            TestWorkspace.ResetThreadAffinity()
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.QuickInfo)>
        Public Sub GetQuickInfoItemAsync()

            Dim mocks = CreateMocks()

            Dim codeAnalysisQuickInfoItem _
                    = QuickInfoItem.Create(New Text.TextSpan(0, 0), ImmutableArray.Create({"Method", "Public"}),
                        ImmutableArray.Create _
                            ({QuickInfoSection.Create("Description",
                                ImmutableArray.Create({
                                    New TaggedText("Keyword", "void"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Class", "Console"),
                                    New TaggedText("Punctuation", "."),
                                    New TaggedText("Method", "WriteLine"),
                                    New TaggedText("Punctuation", "("),
                                    New TaggedText("Keyword", "string"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Parameter", "value"),
                                    New TaggedText("Punctuation", ")"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Punctuation", "("),
                                    New TaggedText("Punctuation", "+"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Text", "18"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Text", "overloads"),
                                    New TaggedText("Punctuation", ")")})),
                            QuickInfoSection.Create("DocumentationComments",
                                ImmutableArray.Create({New TaggedText("Text", "Writes the specified string value, followed by the current line terminator, to the standard output stream.")})),
                            QuickInfoSection.Create("Exception",
                                ImmutableArray.Create({
                                    New TaggedText("Text", "Exceptions"),
                                    New TaggedText("LineBreak", "\r\n"),
                                    New TaggedText("Space", " "),
                                    New TaggedText("Namespace", "System"),
                                    New TaggedText("Punctuation", "."),
                                    New TaggedText("Namespace", "IO"),
                                    New TaggedText("Punctuation", "."),
                                    New TaggedText("Class", "IOException")}))}))

            mocks.Service.SetItemToReturn(codeAnalysisQuickInfoItem)

            Dim snapshotPoint = New SnapshotPoint(mocks.View.TextSnapshot, mocks.View.Caret.Position.BufferPosition.Position)
            Dim intellisenseQuickInfo = mocks.Controller.GetQuickInfoItemAsync(snapshotPoint, New CancellationToken()).Result

            Assert.NotNull(intellisenseQuickInfo)

            Assert.IsType(Of Adornments.ContainerElement)(intellisenseQuickInfo.Item)
            Dim container = CType(intellisenseQuickInfo.Item, Adornments.ContainerElement)
            Assert.Equal(3, container.Elements.Count())


            Assert.IsType(Of Adornments.ClassifiedTextElement)(container.Elements.ElementAt(0))
            Dim element0 = CType(container.Elements.ElementAt(0), Adornments.ClassifiedTextElement)
            Assert.Equal(18, element0.Runs.Count())

            Assert.IsType(Of Adornments.ClassifiedTextElement)(container.Elements.ElementAt(1))
            Dim element1 = CType(container.Elements.ElementAt(1), Adornments.ClassifiedTextElement)
            Assert.Equal(1, element1.Runs.Count())

            Assert.IsType(Of Adornments.ClassifiedTextElement)(container.Elements.ElementAt(2))
            Dim element2 = CType(container.Elements.ElementAt(2), Adornments.ClassifiedTextElement)
            Assert.Equal(8, element2.Runs.Count())

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

            Public ReadOnly Property Service As MockQuickInfoService

            Public ReadOnly Property View As ITextView
                Get
                    Return _viewMock.Object
                End Get
            End Property

            Public ReadOnly Property Controller As Controller
                Get
                    Return _controller
                End Get
            End Property

            Public Sub New(
                controller As Controller,
                service As MockQuickInfoService,
                viewMock As Mock(Of ITextView))

                _controller = controller
                Me.Service = service
                _viewMock = viewMock
            End Sub

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
            Optional noDocument As Boolean = False,
            Optional augmentSession As IAsyncQuickInfoSession = Nothing
        ) As QuickInfoMocks

            Dim view = New Mock(Of ITextView) With {
                .DefaultValue = DefaultValue.Mock
            }

            Dim buffer = s_bufferFactory.CreateTextBuffer()
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
                documentProvider.Object,
                service)

            Return New QuickInfoMocks(controller, service, view)
        End Function

    End Class
End Namespace
