' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.FileHeaders
Imports Microsoft.CodeAnalysis.Formatting

Namespace Microsoft.CodeAnalysis.VisualBasic.FileHeaders
    Friend Module FileHeaderHelpers
        Friend Function ParseFileHeader(root As SyntaxNode) As FileHeader
            Dim firstToken = root.GetFirstToken(includeZeroWidth:=True)
            Dim firstNonWhitespaceTrivia = IndexOfFirstNonWhitespaceTrivia(firstToken.LeadingTrivia, True)

            If firstNonWhitespaceTrivia = -1 Then
                Return FileHeader.MissingFileHeader
            End If

            Dim sb = StringBuilderPool.Allocate()
            Dim endOfLineCount = 0
            Dim done = False
            Dim fileHeaderStart = Integer.MaxValue
            Dim fileHeaderEnd = Integer.MinValue

            For i = firstNonWhitespaceTrivia To firstToken.LeadingTrivia.Count - 1
                If done Then
                    Exit For
                End If

                Dim trivia = firstToken.LeadingTrivia(i)

                Select Case trivia.Kind()
                    Case SyntaxKind.WhitespaceTrivia
                        endOfLineCount = 0

                    Case SyntaxKind.CommentTrivia
                        endOfLineCount = 0

                        Dim commentString = trivia.ToFullString()

                        fileHeaderStart = Math.Min(trivia.FullSpan.Start, fileHeaderStart)
                        fileHeaderEnd = trivia.FullSpan.End

                        sb.AppendLine(commentString.Substring(1).Trim())

                    Case SyntaxKind.EndOfLineTrivia
                        endOfLineCount += 1
                        done = endOfLineCount > 1

                    Case Else
                        done = fileHeaderStart < fileHeaderEnd OrElse Not trivia.IsDirective
                End Select
            Next

            If fileHeaderStart > fileHeaderEnd Then
                StringBuilderPool.Free(sb)
                Return FileHeader.MissingFileHeader
            End If

            If sb.Length > 0 Then
                ' remove the final newline
                Dim eolLength = Environment.NewLine.Length
                sb.Remove(sb.Length - eolLength, eolLength)
            End If

            Return New FileHeader(StringBuilderPool.ReturnAndFree(sb), fileHeaderStart, fileHeaderEnd, commentPrefixLength:=1)
        End Function
    End Module
End Namespace
