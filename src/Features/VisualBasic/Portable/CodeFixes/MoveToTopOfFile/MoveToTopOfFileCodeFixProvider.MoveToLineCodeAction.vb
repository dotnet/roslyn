' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.MoveToTopOfFile
    Partial Friend Class MoveToTopOfFileCodeFixProvider
        Private Class MoveToLineCodeAction
            Inherits CodeAction

            Private ReadOnly _destinationLine As Integer
            Private ReadOnly _document As Document
            Private _token As SyntaxToken
            Private ReadOnly _title As String

            Public Sub New(document As Document, token As SyntaxToken, destinationLine As Integer, title As String)
                _document = document
                _token = token
                _destinationLine = destinationLine
                _title = title
            End Sub

            Public Overrides ReadOnly Property Title As String
                Get
                    Return _title
                End Get
            End Property

            Protected Overrides Async Function GetChangedDocumentAsync(cancellationToken As CancellationToken) As Task(Of Document)
                Dim text = Await _document.GetTextAsync(cancellationToken).ConfigureAwait(False)
                Dim destinationLineSpan = text.Lines(_destinationLine).Start

                Dim lineToMove = _token.GetLocation().GetLineSpan().StartLinePosition.Line
                Dim textLineToMove = text.Lines(lineToMove)
                Dim textWithoutLine = text.WithChanges(New TextChange(textLineToMove.SpanIncludingLineBreak, ""))
                Dim textWithMovedLine = textWithoutLine.WithChanges(New TextChange(TextSpan.FromBounds(destinationLineSpan, destinationLineSpan), textLineToMove.ToString().TrimStart() + vbCrLf))
                Return _document.WithText(textWithMovedLine)
            End Function
        End Class
    End Class
End Namespace
