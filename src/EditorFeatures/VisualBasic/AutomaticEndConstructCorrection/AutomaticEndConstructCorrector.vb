' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
    ''' <summary>
    ''' Tracks user's interaction with editor
    ''' </summary>
    Partial Friend Class AutomaticEndConstructCorrector
        Private ReadOnly _buffer As ITextBuffer
        Private ReadOnly _session As Session
        Private ReadOnly _uiThreadOperationExecutor As IUIThreadOperationExecutor

        Private _previousDocument As Document
        Private _referencingViews As Integer

        Public Sub New(subjectBuffer As ITextBuffer, uiThreadOperationExecutor As IUIThreadOperationExecutor)
            Contract.ThrowIfNull(subjectBuffer)

            Me._buffer = subjectBuffer
            Me._uiThreadOperationExecutor = uiThreadOperationExecutor
            Me._session = New Session(subjectBuffer)

            Me._previousDocument = Nothing
            Me._referencingViews = 0
        End Sub

        Public Sub Connect()
            If _referencingViews = 0 Then
                AddHandler _buffer.Changing, AddressOf OnTextBufferChanging
                AddHandler _buffer.Changed, AddressOf OnTextBufferChanged
            End If

            _referencingViews = _referencingViews + 1
        End Sub

        Public Sub Disconnect()
            If _referencingViews = 1 Then
                RemoveHandler _buffer.Changed, AddressOf OnTextBufferChanged
                RemoveHandler _buffer.Changing, AddressOf OnTextBufferChanging
            End If

            _referencingViews = Math.Max(_referencingViews - 1, 0)
        End Sub

        Public ReadOnly Property IsDisconnected As Boolean
            Get
                Return _referencingViews = 0
            End Get
        End Property

        Private Sub OnTextBufferChanging(sender As Object, e As TextContentChangingEventArgs)
            If Me._session.Alive Then
                _previousDocument = Nothing
                Return
            End If

            ' try holding onto previous Document so that we can use it when we diff syntax tree
            _previousDocument = e.Before.GetOpenDocumentInCurrentContextWithChanges()
        End Sub

        Private Sub OnTextBufferChanged(sender As Object, e As TextContentChangedEventArgs)
            _uiThreadOperationExecutor.Execute(
                "IntelliSense",
                defaultDescription:="",
                allowCancellation:=True,
                showProgress:=False,
                action:=Sub(c) StartSession(e, c.UserCancellationToken))

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

            Dim token As SyntaxToken = Nothing
            If Not IsValidChange(e, token, cancellationToken) Then
                Return
            End If

            Me._session.Start(GetLinkedEditSpans(e.Before, token), e)
        End Sub

        Private Shared Function GetLinkedEditSpans(snapshot As ITextSnapshot, token As SyntaxToken) As IEnumerable(Of ITrackingSpan)
            Dim startToken = GetBeginToken(token.Parent)
            If startToken.Kind = SyntaxKind.None Then
                startToken = GetCorrespondingBeginToken(token)
            End If

            Dim endToken = GetCorrespondingEndToken(startToken)

            Return {New LetterOnlyTrackingSpan(startToken.Span.ToSnapshotSpan(snapshot)), New LetterOnlyTrackingSpan(endToken.Span.ToSnapshotSpan(snapshot))}
        End Function

        Private Function IsValidChange(bufferChanges As TextContentChangedEventArgs, ByRef token As SyntaxToken, cancellationToken As CancellationToken) As Boolean
            ' set out parameter first
            token = Nothing

            ' we will be very conservative when staring session
            Dim changes = bufferChanges.Changes

            ' change should not contain any line changes
            If changes.IncludesLineChanges Then
                Return False
            End If

            ' we only start session if one edit happens, not multi-edits
            If changes.Count <> 1 Then
                Return False
            End If

            Dim textChange = changes.Item(0)
            If Not IsChangeOnSameLine(bufferChanges.After, textChange) Then
                Return False
            End If

            If Not IsChangeOnCorrectText(bufferChanges.Before, textChange.OldPosition) Then
                Return False
            End If

            If _previousDocument Is Nothing Then
                Return False
            End If

            Dim root = _previousDocument.GetSyntaxRootSynchronously(cancellationToken)
            token = root.FindToken(textChange.OldPosition)

            If Not IsChangeOnCorrectToken(token) Then
                Return False
            End If

            Return True
        End Function

        Private Shared Function IsChangeOnSameLine(snapshot As ITextSnapshot, change As ITextChange) As Boolean
            Return snapshot.AreOnSameLine(change.NewPosition, change.NewEnd)
        End Function

        Private Shared Function IsChangeOnCorrectToken(token As SyntaxToken) As Boolean
            Select Case token.Kind
                Case SyntaxKind.StructureKeyword, SyntaxKind.EnumKeyword, SyntaxKind.InterfaceKeyword,
                     SyntaxKind.ClassKeyword, SyntaxKind.ModuleKeyword, SyntaxKind.NamespaceKeyword,
                     SyntaxKind.SubKeyword, SyntaxKind.FunctionKeyword, SyntaxKind.GetKeyword, SyntaxKind.SetKeyword

                    If token.Parent Is Nothing Then
                        Return False
                    End If

                    ' we found right token, let's see whether we are under right context
                    If IsChangeOnBeginToken(token) Then
                        Return CorrespondingEndTokenExist(token)
                    End If

                    If IsChangeOnEndToken(token) Then
                        Return CorrespondingBeginTokenExist(token)
                    End If
            End Select

            Return False
        End Function

        Private Shared Function CorrespondingBeginTokenExist(token As SyntaxToken) As Boolean
            Dim pairToken = GetCorrespondingBeginToken(token)

            Dim hasValidToken = pairToken.Kind <> SyntaxKind.None AndAlso Not pairToken.IsMissing AndAlso token.ValueText = pairToken.ValueText
            If Not hasValidToken Then
                Return False
            End If

            If BeginStatementIsInValidForm(pairToken.Parent) Then
                Return True
            End If

            Return False
        End Function

        Private Shared Function GetCorrespondingBeginToken(token As SyntaxToken) As SyntaxToken
            If token.Parent.Parent Is Nothing Then
                Return New SyntaxToken()
            End If

            Dim beginNode = token.Parent.Parent.TypeSwitch(
                Function(context As TypeBlockSyntax) context.BlockStatement,
                Function(context As EnumBlockSyntax) context.EnumStatement,
                Function(context As NamespaceBlockSyntax) context.NamespaceStatement,
                Function(context As MethodBlockBaseSyntax) context.BlockStatement,
                Function(context As MultiLineLambdaExpressionSyntax) context.SubOrFunctionHeader,
                Function(dontCare As SyntaxNode) CType(Nothing, SyntaxNode))

            If beginNode Is Nothing Then
                Return New SyntaxToken()
            End If

            Return GetBeginToken(beginNode)
        End Function

        Private Shared Function IsChangeOnEndToken(token As SyntaxToken) As Boolean
            Dim endBlockStatement = TryCast(token.Parent, EndBlockStatementSyntax)
            If endBlockStatement Is Nothing Then
                Return False
            End If

            Return endBlockStatement.BlockKeyword = token
        End Function

        Private Shared Function CorrespondingEndTokenExist(token As SyntaxToken) As Boolean
            Dim pairToken = GetCorrespondingEndToken(token)

            Return pairToken.Kind <> SyntaxKind.None AndAlso Not pairToken.IsMissing AndAlso token.ValueText = pairToken.ValueText
        End Function

        Private Shared Function GetCorrespondingEndToken(token As SyntaxToken) As SyntaxToken
            If token.Parent.Parent Is Nothing Then
                Return New SyntaxToken()
            End If

            Return token.Parent.Parent.TypeSwitch(
                Function(context As TypeBlockSyntax) context.EndBlockStatement.BlockKeyword,
                Function(context As EnumBlockSyntax) context.EndEnumStatement.BlockKeyword,
                Function(context As NamespaceBlockSyntax) context.EndNamespaceStatement.BlockKeyword,
                Function(context As MethodBlockBaseSyntax) context.EndBlockStatement.BlockKeyword,
                Function(context As MultiLineLambdaExpressionSyntax) context.EndSubOrFunctionStatement.BlockKeyword,
                Function(dontCare As SyntaxNode) New SyntaxToken())
        End Function

        Private Shared Function IsChangeOnBeginToken(token As SyntaxToken) As Boolean
            Dim pairToken = GetBeginToken(token.Parent)

            Dim hasValidToken = pairToken.Kind <> SyntaxKind.None AndAlso Not pairToken.IsMissing AndAlso token = pairToken
            If Not hasValidToken Then
                Return False
            End If

            If BeginStatementIsInValidForm(token.Parent) Then
                Return True
            End If

            Return False
        End Function

        Private Shared Function BeginStatementIsInValidForm(node As SyntaxNode) As Boolean
            ' turns out in malformed code, parser would pair some constructs together even if user wouldn't consider them being
            ' paired. So, rather than the feature being very naive, we will make sure begin construct have at least some valid shape.
            Return node.TypeSwitch(
                Function(context As TypeStatementSyntax) Not context.Identifier.IsMissing,
                Function(context As EnumStatementSyntax) Not context.Identifier.IsMissing,
                Function(context As NamespaceStatementSyntax) context.Name IsNot Nothing,
                Function(context As MethodStatementSyntax) Not context.Identifier.IsMissing,
                Function(context As AccessorStatementSyntax) Not context.DeclarationKeyword.IsMissing,
                Function(context As LambdaHeaderSyntax) True,
                Function(dontCare As SyntaxNode) False)
        End Function

        Private Shared Function GetBeginToken(node As SyntaxNode) As SyntaxToken
            Return node.TypeSwitch(
                Function(context As TypeStatementSyntax) context.DeclarationKeyword,
                Function(context As EnumStatementSyntax) context.EnumKeyword,
                Function(context As NamespaceStatementSyntax) context.NamespaceKeyword,
                Function(context As MethodStatementSyntax) context.DeclarationKeyword,
                Function(context As LambdaHeaderSyntax) context.DeclarationKeyword,
                Function(context As AccessorStatementSyntax) context.DeclarationKeyword,
                Function(dontCare As SyntaxNode) New SyntaxToken())
        End Function

        Private Shared Function IsChangeOnCorrectText(snapshot As ITextSnapshot, position As Integer) As Boolean
            Dim line = snapshot.GetLineFromPosition(position)

            Dim text = line.GetText()
            Dim positionInText = position - line.Start.Position
            Contract.ThrowIfFalse(positionInText >= 0)

            If text.Length = 0 OrElse text.Length < positionInText Then
                Return False
            End If

            If text.Length <= positionInText OrElse Not Char.IsLetter(text(positionInText)) Then
                positionInText = positionInText - 1

                If Not Char.IsLetter(text(Math.Max(0, positionInText))) Then
                    Return False
                End If
            End If

            Dim startIndex = GetStartIndexOfWord(text, positionInText)
            Dim length = GetEndIndexOfWord(text, positionInText) - startIndex + 1

            Dim textUnderPosition = text.Substring(startIndex, length)

            Return AutomaticEndConstructSet.Contains(textUnderPosition)
        End Function

        Private Shared Function GetStartIndexOfWord(text As String, position As Integer) As Integer
            For index = position To 0 Step -1
                If Not Char.IsLetter(text(index)) Then
                    Return index + 1
                End If
            Next

            Return 0
        End Function

        Private Shared Function GetEndIndexOfWord(text As String, position As Integer) As Integer
            For index = position To text.Length - 1
                If Not Char.IsLetter(text(index)) Then
                    Return index - 1
                End If
            Next

            Return text.Length - 1
        End Function

    End Class
End Namespace
