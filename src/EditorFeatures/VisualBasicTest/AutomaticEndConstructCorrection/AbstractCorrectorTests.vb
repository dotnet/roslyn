' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AutomaticEndConstructCorrection
    Public MustInherit Class AbstractCorrectorTests
        Friend MustOverride Function CreateCorrector(buffer As ITextBuffer, waitIndicator As TestWaitIndicator) As ICorrector

        Protected Sub VerifyContinuousEdits(codeWithMarker As String,
                                      type As String,
                                      expectedStringGetter As Func(Of String, String),
                                      removeOriginalContent As Boolean,
                                      Optional split As String = Nothing)
            ' do this since xml value put only vbLf
            codeWithMarker = codeWithMarker.Replace(vbLf, vbCrLf)

            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(codeWithMarker)
                Dim document = workspace.Documents.Single()

                Dim buffer = document.TextBuffer
                Dim snapshot = buffer.CurrentSnapshot

                Dim caretPosition = snapshot.CreateTrackingPoint(document.CursorPosition.Value,
                                                             PointTrackingMode.Positive,
                                                             TrackingFidelityMode.Backward)
                Dim corrector = CreateCorrector(buffer, New TestWaitIndicator())

                corrector.Connect()

                If removeOriginalContent Then
                    Dim spanToRemove = document.SelectedSpans.Single(Function(s) s.Contains(caretPosition.GetPosition(snapshot)))
                    buffer.Replace(spanToRemove.ToSpan(), "")
                End If

                For i = 0 To type.Length - 1
                    Dim charToInsert = type(i)
                    buffer.Insert(caretPosition.GetPosition(buffer.CurrentSnapshot), charToInsert)

                    Dim insertedString = type.Substring(0, i + 1)
                    For Each span In document.SelectedSpans.Skip(1)
                        Dim trackingSpan = New LetterOnlyTrackingSpan(span.ToSnapshotSpan(document.InitialTextSnapshot))
                        Assert.Equal(expectedStringGetter(insertedString), trackingSpan.GetText(document.TextBuffer.CurrentSnapshot))
                    Next
                Next

                If split IsNot Nothing Then
                    Dim beginSpan = document.SelectedSpans.First()
                    Dim trackingSpan = New LetterOnlyTrackingSpan(beginSpan.ToSnapshotSpan(document.InitialTextSnapshot))

                    buffer.Insert(trackingSpan.GetEndPoint(buffer.CurrentSnapshot).Position - type.Trim().Length, " ")

                    Assert.Equal(split, trackingSpan.GetText(buffer.CurrentSnapshot))
                End If

                corrector.Disconnect()
            End Using
        End Sub

        Protected Sub Verify(codeWithMarker As String, keyword As String)
            ' do this since xml value put only vbLf
            codeWithMarker = codeWithMarker.Replace(vbLf, vbCrLf)

            VerifyBegin(codeWithMarker, keyword)
            VerifyEnd(codeWithMarker, keyword)
        End Sub

        Protected Sub VerifyBegin(code As String, keyword As String, Optional expected As String = Nothing)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(code)
                Dim document = workspace.Documents.Single()

                Dim selectedSpans = document.SelectedSpans

                Dim spanToReplace = selectedSpans.First()
                Dim spanToVerify = selectedSpans.Skip(1).Single()

                Verify(workspace, document, keyword, expected, spanToReplace, spanToVerify)
            End Using
        End Sub

        Protected Sub VerifyEnd(code As String, keyword As String, Optional expected As String = Nothing)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(code)
                Dim document = workspace.Documents.Single()

                Dim selectedSpans = document.SelectedSpans

                Dim spanToReplace = selectedSpans.Skip(1).Single()
                Dim spanToVerify = selectedSpans.First()

                Verify(workspace, document, keyword, expected, spanToReplace, spanToVerify)
            End Using
        End Sub

        Protected Sub Verify(workspace As TestWorkspace, document As TestHostDocument, keyword As String, expected As String, spanToReplace As TextSpan, spanToVerify As TextSpan)
            Dim buffer = document.TextBuffer
            Dim corrector = CreateCorrector(buffer, New TestWaitIndicator())

            corrector.Connect()
            buffer.Replace(spanToReplace.ToSpan(), keyword)
            corrector.Disconnect()

            expected = If(expected Is Nothing, keyword, expected)

            Dim correspondingSpan = document.InitialTextSnapshot.CreateTrackingSpan(spanToVerify.ToSpan(), SpanTrackingMode.EdgeInclusive)
            Assert.Equal(expected, correspondingSpan.GetText(buffer.CurrentSnapshot))
        End Sub
    End Class
End Namespace