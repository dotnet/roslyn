' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
    ''' <summary>
    ''' This is a workaround until Jason checkin actual custom tracking span
    ''' </summary>
    Friend Class LetterOnlyTrackingSpan
        Implements ITrackingSpan

        ' this is not thread safe. it is a workaround anyway.
        ' mutating underlying tracking span. we will adjust based on its content
        Private _trackingSpan As ITrackingSpan
        Private _version As ITextVersion

        Public Sub New(span As SnapshotSpan)
            Contract.ThrowIfNull(span.Snapshot)

            Me._trackingSpan = span.Snapshot.CreateTrackingSpan(span.Span, SpanTrackingMode.EdgeInclusive, TrackingFidelityMode.Backward)
        End Sub

        Public Function GetEndPoint(snapshot As ITextSnapshot) As SnapshotPoint Implements ITrackingSpan.GetEndPoint
            AdjustSpan(snapshot)
            Return Me._trackingSpan.GetEndPoint(snapshot)
        End Function

        Public Function GetSpan(snapshot As ITextSnapshot) As SnapshotSpan Implements ITrackingSpan.GetSpan
            AdjustSpan(snapshot)
            Return Me._trackingSpan.GetSpan(snapshot)

        End Function

        Public Function GetSpan(version As ITextVersion) As Span Implements ITrackingSpan.GetSpan
            Throw New NotSupportedException(VBEditorResources.not_supported)
        End Function

        Public Function GetStartPoint(snapshot As ITextSnapshot) As SnapshotPoint Implements ITrackingSpan.GetStartPoint
            AdjustSpan(snapshot)
            Return Me._trackingSpan.GetStartPoint(snapshot)
        End Function

        Public Function GetText(snapshot As ITextSnapshot) As String Implements ITrackingSpan.GetText
            AdjustSpan(snapshot)
            Return Me._trackingSpan.GetText(snapshot)
        End Function

        Public ReadOnly Property TextBuffer As ITextBuffer Implements ITrackingSpan.TextBuffer
            Get
                Return Me._trackingSpan.TextBuffer
            End Get
        End Property

        Public ReadOnly Property TrackingFidelity As TrackingFidelityMode Implements ITrackingSpan.TrackingFidelity
            Get
                Return TrackingFidelityMode.Backward
            End Get
        End Property

        Public ReadOnly Property TrackingMode As SpanTrackingMode Implements ITrackingSpan.TrackingMode
            Get
                Return SpanTrackingMode.Custom
            End Get
        End Property

        Private Shared Sub GetNextWordIndex(text As String, startIndex As Integer, ByRef firstLetterIndex As Integer, ByRef lastLetterIndex As Integer)
            firstLetterIndex = -1
            lastLetterIndex = -1

            For i = startIndex To 0 Step -1
                If lastLetterIndex < 0 AndAlso Char.IsLetter(text(i)) Then
                    lastLetterIndex = i
                End If

                If Char.IsLetter(text(i)) Then
                    firstLetterIndex = i
                End If

                If lastLetterIndex >= 0 AndAlso Not Char.IsLetter(text(i)) Then
                    Exit For
                End If
            Next
        End Sub

        Private Shared Function SameAsOriginal(span As SnapshotSpan, firstLetterIndex As Integer, lastLetterIndex As Integer) As Boolean
            Return firstLetterIndex = 0 AndAlso span.Length - 1 = lastLetterIndex
        End Function

        Private Sub AdjustSpanForErrorCase(span As SnapshotSpan, text As String)
            Dim snapshot = span.Snapshot
            Dim trimmedText = text.Trim()

            If trimmedText.Length = 0 Then
                ' all whitespace, make tracking span to stick to end
                Me._trackingSpan = snapshot.CreateTrackingSpan(span.End.Position, 0, SpanTrackingMode.EdgeInclusive, TrackingFidelityMode.Backward)
            Else
                ' something like punctuation is there
                ' make tracking span to stick after the punctuation
                Dim position = span.Start.Position + text.IndexOf(trimmedText, StringComparison.Ordinal) + trimmedText.Length
                Me._trackingSpan = snapshot.CreateTrackingSpan(position, 0, SpanTrackingMode.EdgeInclusive, TrackingFidelityMode.Backward)
            End If
        End Sub

        Private Sub SetNewTrackingSpan(span As SnapshotSpan, firstLetterIndex As Integer, length As Integer)
            ' need to re-adjust
            Dim startPosition = span.Start.Position + firstLetterIndex
            Me._trackingSpan = span.Snapshot.CreateTrackingSpan(startPosition, length, SpanTrackingMode.EdgeInclusive, TrackingFidelityMode.Backward)
        End Sub

        Private Sub AdjustSpanWorker(snapshot As ITextSnapshot)
            Dim span = Me._trackingSpan.GetSpan(snapshot)
            Dim text = span.GetText()

            If text.Length = 0 Then
                Return
            End If

            ' prefer word at the end
            Dim firstLetterIndex = -1
            Dim lastLetterIndex = -1

            GetNextWordIndex(text, text.Length - 1, firstLetterIndex, lastLetterIndex)

            ' there are letters in the text. re-adjust tracking span if it is changed
            If SameAsOriginal(span, firstLetterIndex, lastLetterIndex) Then
                Return
            End If

            ' no letter in the text
            If lastLetterIndex < 0 Then
                AdjustSpanForErrorCase(span, text)
                Return
            End If

            Dim length = lastLetterIndex - firstLetterIndex + 1

            ' check whether we found a best one
            If AutomaticEndConstructSet.Contains(text.Substring(firstLetterIndex, length)) Then
                SetNewTrackingSpan(span, firstLetterIndex, length)
                Return
            End If

            ' we found a keyword but it is not something we are interested.
            ' check whether we have a better choice
            While firstLetterIndex > 0
                GetNextWordIndex(text, firstLetterIndex - 1, firstLetterIndex, lastLetterIndex)

                ' couldn't find better choice, leave tracking span as it is
                If lastLetterIndex < 0 Then
                    Return
                End If

                length = lastLetterIndex - firstLetterIndex + 1
                Dim candidate = text.Substring(firstLetterIndex, length)

                ' found a better one
                If AutomaticEndConstructSet.Contains(candidate) Then
                    SetNewTrackingSpan(span, firstLetterIndex, length)
                    Return
                End If
            End While
        End Sub

        Private Sub AdjustSpan(snapshot As ITextSnapshot)
            ' we already processed this snapshot and re-adjusted the tracking span
            ' if needed. 
            If snapshot.Version.Equals(Me._version) Then
                Return
            End If

            AdjustSpanWorker(snapshot)
            Me._version = snapshot.Version
        End Sub
    End Class
End Namespace
