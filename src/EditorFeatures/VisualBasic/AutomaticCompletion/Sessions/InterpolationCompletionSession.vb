' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion.Sessions
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.VisualStudio.Text.BraceCompletion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion.Sessions
    Friend Class InterpolationCompletionSession
        Inherits AbstractTokenBraceCompletionSession

        Public Sub New(syntaxFactsService As ISyntaxFactsService)
            MyBase.New(syntaxFactsService, SyntaxKind.OpenBraceToken, SyntaxKind.CloseBraceToken)
        End Sub

        Public Overrides Function CheckOpeningPoint(session As IBraceCompletionSession, cancellationToken As CancellationToken) As Boolean
            Dim snapshot = session.SubjectBuffer.CurrentSnapshot
            Dim position = session.OpeningPoint.GetPosition(snapshot)
            Dim token = snapshot.FindToken(position, cancellationToken)

            Return token.IsKind(SyntaxKind.DollarSignDoubleQuoteToken, SyntaxKind.InterpolatedStringTextToken) OrElse
                   (token.IsKind(SyntaxKind.CloseBraceToken) AndAlso token.Parent.IsKind(SyntaxKind.Interpolation))
        End Function

        Public Overrides Function AllowOverType(session As IBraceCompletionSession, cancellationToken As CancellationToken) As Boolean
            Return CheckClosingTokenKind(session, cancellationToken)
        End Function

        Public Shared Function IsContext(document As Document, position As Integer, cancellationToken As CancellationToken) As Boolean
            If position = 0 Then
                Return False
            End If

            ' Check to see if the character to the left of the position is an open curly brace. Note that we have to
            ' count braces to ensure that the character isn't actually an escaped brace.
            Dim text = document.GetTextSynchronously(cancellationToken)
            Dim index = position - 1
            Dim openCurlyCount = 0
            For index = index To 0 Step -1
                If text(index) = "{"c Then
                    openCurlyCount += 1
                Else
                    Exit For
                End If
            Next

            If openCurlyCount Mod 2 > 0 Then
                Return False
            End If

            ' Next, check to see if the token we're typing is part of an existing interpolated string.
            '
            Dim tree = document.GetSyntaxTreeSynchronously(cancellationToken)
            Dim token = tree.GetRoot(cancellationToken).FindTokenOnRightOfPosition(position)

            If Not token.Span.IntersectsWith(position) Then
                Return False
            End If

            Return token.IsKind(SyntaxKind.DollarSignDoubleQuoteToken, SyntaxKind.InterpolatedStringTextToken) OrElse
                   (token.IsKind(SyntaxKind.CloseBraceToken) AndAlso token.Parent.IsKind(SyntaxKind.Interpolation))
        End Function

    End Class
End Namespace
