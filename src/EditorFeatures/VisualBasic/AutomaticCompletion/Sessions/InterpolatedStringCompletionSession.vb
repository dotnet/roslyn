' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
Imports Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion.Sessions
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.VisualStudio.Text.BraceCompletion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion.Sessions
    Friend Class InterpolatedStringCompletionSession
        Inherits AbstractTokenBraceCompletionSession

        Public Sub New(syntaxFactsService As ISyntaxFactsService)
            MyBase.New(syntaxFactsService, SyntaxKind.DollarSignDoubleQuoteToken, SyntaxKind.DoubleQuoteToken)
        End Sub

        Public Overrides Function CheckOpeningPoint(session As IBraceCompletionSession, cancellationToken As CancellationToken) As Boolean
            Dim snapshot = session.SubjectBuffer.CurrentSnapshot
            Dim position = session.OpeningPoint.GetPosition(snapshot)
            Dim token = snapshot.FindToken(position, cancellationToken)

            Return token.IsKind(SyntaxKind.DollarSignDoubleQuoteToken) AndAlso
                   token.Span.End - 1 = position
        End Function

        Public Overrides Function AllowOverType(session As IBraceCompletionSession, cancellationToken As CancellationToken) As Boolean
            Return CheckClosingTokenKind(session, cancellationToken)
        End Function

        Public Shared Function IsContext(document As Document, position As Integer, cancellationToken As CancellationToken) As Boolean
            If position = 0 Then
                Return False
            End If

            Dim text = document.GetTextSynchronously(cancellationToken)

            Return text(position - 1) = "$"c
        End Function
    End Class
End Namespace
