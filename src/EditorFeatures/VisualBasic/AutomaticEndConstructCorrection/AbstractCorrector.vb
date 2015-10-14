' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
    Partial Friend MustInherit Class AbstractCorrector
        Implements ICorrector

        Private ReadOnly _buffer As ITextBuffer
        Private ReadOnly _waitIndicator As IWaitIndicator
        Private ReadOnly _session As Session

        Private _previousDocument As Document
        Private _referencingViews As Integer

        ' Private ReadOnly _session As Session

        Protected Sub New(subjectBuffer As ITextBuffer, waitIndicator As IWaitIndicator)
            _buffer = subjectBuffer
            _waitIndicator = waitIndicator

            _session = New Session(subjectBuffer, AddressOf ShouldReplaceText)
            _previousDocument = Nothing
            _referencingViews = 0
        End Sub

        Protected MustOverride Function IsAllowableWordAtIndex(lineText As String, wordStartIndex As Integer, wordLength As Integer) As Boolean
        Protected MustOverride Function TryGetValidTokens(wordStartIndex As Integer, ByRef startToken As SyntaxToken, ByRef endToken As SyntaxToken, cancellationToken As CancellationToken) As Boolean
        Protected MustOverride Function ShouldReplaceText(text As String) As Boolean

        Protected ReadOnly Property PreviousDocument As Document
            Get
                Return _previousDocument
            End Get
        End Property

        Public Sub Connect() Implements ICorrector.Connect
            If _referencingViews = 0 Then
                AddHandler _buffer.Changing, AddressOf OnTextBufferChanging
                AddHandler _buffer.Changed, AddressOf OnTextBufferChanged
            End If

            _referencingViews = _referencingViews + 1
        End Sub

        Public Sub Disconnect() Implements ICorrector.Disconnect
            If _referencingViews = 1 Then
                RemoveHandler _buffer.Changed, AddressOf OnTextBufferChanged
                RemoveHandler _buffer.Changing, AddressOf OnTextBufferChanging
            End If

            _referencingViews = Math.Max(_referencingViews - 1, 0)
        End Sub

        Public ReadOnly Property IsDisconnected As Boolean Implements ICorrector.IsDisconnected
            Get
                Return _referencingViews = 0
            End Get
        End Property

        Protected Function CreateTrackingSpans(startToken As SyntaxToken, endToken As SyntaxToken, snapshot As ITextSnapshot) As IEnumerable(Of ITrackingSpan)
            Return {CreateTrackingSpan(startToken, snapshot), New LetterOnlyTrackingSpan(endToken.Span.ToSnapshotSpan(snapshot))}
        End Function

        Private Function CreateTrackingSpan(token As SyntaxToken, snapshot As ITextSnapshot) As ITrackingSpan
            Return New LetterOnlyTrackingSpan(token.Span.ToSnapshotSpan(snapshot))
        End Function

        Private Sub OnTextBufferChanging(sender As Object, e As TextContentChangingEventArgs)
            If Me._session.Alive Then
                _previousDocument = Nothing
                Return
            End If

            ' try holding onto previous Document so that we can use it when we diff syntax tree
            _previousDocument = e.Before.GetOpenDocumentInCurrentContextWithChanges()
        End Sub

        Private Sub OnTextBufferChanged(sender As Object, e As TextContentChangedEventArgs)
            _waitIndicator.Wait(
                "IntelliSense",
                allowCancel:=True,
                action:=Sub(c) StartSession(e, c.CancellationToken))

            ' clear previous document
            _previousDocument = Nothing
        End Sub

        Private Sub StartSession(e As TextContentChangedEventArgs, cancellationToken As CancellationToken)
            If e.Changes.Count = 0 Then
                Return
            End If

            ' If this is a reiterated version, then it's part of undo/redo and we should ignore it
            If e.AfterVersion.ReiteratedVersionNumber <> e.AfterVersion.VersionNumber Then
                Return
            End If

            If Me._session.Alive Then
                If Me._session.OnTextChange(e) Then
                    Return
                End If
            End If

            Dim wordStartIndex As Integer
            If Not IsValidTextualChange(e, wordStartIndex, cancellationToken) Then
                Return
            End If

            Dim startToken, endToken As SyntaxToken
            If Not TryGetValidTokens(wordStartIndex, startToken, endToken, cancellationToken) Then
                Return
            End If

            Me._session.Start(CreateTrackingSpans(startToken, endToken, e.Before), e)
        End Sub

        Private Function IsValidTextualChange(bufferChanges As TextContentChangedEventArgs,
                                              ByRef wordStartIndex As Integer,
                                              cancellationToken As CancellationToken) As Boolean
            ' we will be very conservative when staring session
            Dim changes = bufferChanges.Changes

            ' change should not contain any line changes
            If changes.IncludesLineChanges Then
                Return False
            End If

            ' we only start session if one edit happens not multiedits
            If changes.Count <> 1 Then
                Return False
            End If

            Dim textChange = changes.Item(0)
            If Not IsChangeOnSameLine(bufferChanges.After, textChange) Then
                Return False
            End If

            If Not IsChangeOnCorrectText(bufferChanges.Before, textChange.OldPosition, wordStartIndex) Then
                Return False
            End If

            If _previousDocument Is Nothing Then
                Return False
            End If

            Return True
        End Function

        Private Shared Function IsChangeOnSameLine(snapshot As ITextSnapshot, change As ITextChange) As Boolean
            ' changes on same line
            Return snapshot.GetLineNumberFromPosition(change.NewPosition) = snapshot.GetLineNumberFromPosition(change.NewEnd)
        End Function

        Private Function IsChangeOnCorrectText(snapshot As ITextSnapshot,
                                               position As Integer,
                                               ByRef wordStartIndex As Integer) As Boolean
            Dim line = snapshot.GetLineFromPosition(position)

            Dim lineText = line.GetText()
            Dim positionInText = position - line.Start.Position
            Contract.ThrowIfFalse(positionInText >= 0)

            If lineText.Length = 0 OrElse lineText.Length < positionInText Then
                Return False
            End If

            If lineText.Length <= positionInText OrElse Not Char.IsLetter(lineText(positionInText)) Then
                positionInText = positionInText - 1

                If Not Char.IsLetter(lineText(Math.Max(0, positionInText))) Then
                    Return False
                End If
            End If

            Dim wordStartIndexOnLine = GetStartIndexOfWord(lineText, positionInText)
            Dim wordLength = GetEndIndexOfWord(lineText, positionInText) - wordStartIndexOnLine + 1

            wordStartIndex = line.Start + wordStartIndexOnLine

            Return IsAllowableWordAtIndex(lineText, wordStartIndexOnLine, wordLength)
        End Function

        Private Function GetStartIndexOfWord(text As String, position As Integer) As Integer
            For index = position To 0 Step -1
                If Not Char.IsLetter(text(index)) Then
                    Return index + 1
                End If
            Next

            Return 0
        End Function

        Private Function GetEndIndexOfWord(text As String, position As Integer) As Integer
            For index = position To text.Length - 1
                If Not Char.IsLetter(text(index)) Then
                    Return index - 1
                End If
            Next

            Return text.Length - 1
        End Function
    End Class
End Namespace
