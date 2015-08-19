' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Friend Class BaseFormattingRule
        Inherits AbstractFormattingRule

        Public Sub New()
        End Sub

        Protected Sub AddIndentBlockOperation(operations As List(Of IndentBlockOperation), startToken As SyntaxToken, endToken As SyntaxToken, Optional [option] As IndentBlockOption = IndentBlockOption.RelativePosition)
            If startToken.Kind = SyntaxKind.None OrElse endToken.Kind = SyntaxKind.None Then
                Return
            End If

            Dim span = GetIndentBlockSpan(startToken, endToken)
            operations.Add(FormattingOperations.CreateIndentBlockOperation(startToken, endToken, span, indentationDelta:=1, [option]:=[option]))
        End Sub

        Protected Sub AddIndentBlockOperation(operations As List(Of IndentBlockOperation),
                                              baseToken As SyntaxToken,
                                              startToken As SyntaxToken,
                                              endToken As SyntaxToken,
                                              Optional textSpan As TextSpan = Nothing,
                                              Optional [option] As IndentBlockOption = IndentBlockOption.RelativePosition)
            Dim span = If(textSpan = Nothing, GetIndentBlockSpan(startToken, endToken), textSpan)
            operations.Add(FormattingOperations.CreateRelativeIndentBlockOperation(baseToken, startToken, endToken, span, indentationDelta:=1, [option]:=[option]))
        End Sub

        Protected Sub SetAlignmentBlockOperation(operations As List(Of IndentBlockOperation), baseToken As SyntaxToken, startToken As SyntaxToken, endToken As SyntaxToken, Optional [option] As IndentBlockOption = IndentBlockOption.RelativePosition)
            SetAlignmentBlockOperation(operations, baseToken, startToken, endToken, GetAlignmentSpan(startToken, endToken), [option])
        End Sub

        Protected Sub SetAlignmentBlockOperation(operations As List(Of IndentBlockOperation), baseToken As SyntaxToken, startToken As SyntaxToken, endToken As SyntaxToken, span As TextSpan, Optional [option] As IndentBlockOption = IndentBlockOption.RelativePosition)
            operations.Add(FormattingOperations.CreateRelativeIndentBlockOperation(baseToken, startToken, endToken, span, indentationDelta:=0, [option]:=[option]))
        End Sub

        Protected Sub AddAbsolutePositionIndentBlockOperation(operations As List(Of IndentBlockOperation), startToken As SyntaxToken, endToken As SyntaxToken, indentation As Integer, Optional [option] As IndentBlockOption = IndentBlockOption.AbsolutePosition)
            AddAbsolutePositionIndentBlockOperation(operations, startToken, endToken, indentation, GetIndentBlockSpan(startToken, endToken), [option])
        End Sub

        Protected Sub AddAbsolutePositionIndentBlockOperation(operations As List(Of IndentBlockOperation), startToken As SyntaxToken, endToken As SyntaxToken, indentation As Integer, span As TextSpan, Optional [option] As IndentBlockOption = IndentBlockOption.AbsolutePosition)
            operations.Add(FormattingOperations.CreateIndentBlockOperation(startToken, endToken, span, indentation, [option]))
        End Sub

        Private Function GetAlignmentSpan(startToken As SyntaxToken, endToken As SyntaxToken) As TextSpan
            Dim previousToken = startToken.GetPreviousToken(includeZeroWidth:=True)
            Return TextSpan.FromBounds(previousToken.Span.End, endToken.FullSpan.End)
        End Function

        Private Function GetIndentBlockSpan(startToken As SyntaxToken, endToken As SyntaxToken) As TextSpan
            ' special case for colon trivia
            Dim spanStart = startToken.GetPreviousToken(includeZeroWidth:=True).Span.End
            Dim nextToken = endToken.GetNextToken(includeZeroWidth:=True)

            For Each trivia In nextToken.LeadingTrivia.Reverse()
                If trivia.Kind = SyntaxKind.EndOfLineTrivia Then
                    Exit For
                ElseIf trivia.Kind = SyntaxKind.ColonTrivia Then
                    Return TextSpan.FromBounds(spanStart, trivia.FullSpan.Start)
                End If
            Next

            Return TextSpan.FromBounds(spanStart, nextToken.SpanStart)
        End Function

        Protected Sub AddSuppressWrappingIfOnSingleLineOperation(operations As List(Of SuppressOperation), startToken As SyntaxToken, endToken As SyntaxToken)
            Contract.Fail("VB doesn't need to use this operation")
        End Sub

        Protected Sub AddSuppressAllOperationIfOnMultipleLine(operations As List(Of SuppressOperation), startToken As SyntaxToken, endToken As SyntaxToken)
            Contract.Fail("VB doesn't need to use this operation")
        End Sub

        Protected Sub AddAnchorIndentationOperation(operations As List(Of AnchorIndentationOperation), startToken As SyntaxToken, endToken As SyntaxToken)
            If startToken.Kind = SyntaxKind.None OrElse endToken.Kind = SyntaxKind.None Then
                Return
            End If

            operations.Add(FormattingOperations.CreateAnchorIndentationOperation(startToken, endToken))
        End Sub

        Protected Sub AddAlignIndentationOfTokensToBaseTokenOperation(operations As List(Of AlignTokensOperation), containingNode As SyntaxNode, baseToken As SyntaxToken, tokens As IEnumerable(Of SyntaxToken))
            If containingNode Is Nothing OrElse tokens Is Nothing Then
                Return
            End If

            operations.Add(FormattingOperations.CreateAlignTokensOperation(baseToken, tokens, AlignTokensOption.AlignIndentationOfTokensToBaseToken))
        End Sub

        Protected Function CreateAdjustNewLinesOperation(line As Integer, [option] As AdjustNewLinesOption) As AdjustNewLinesOperation
            Return FormattingOperations.CreateAdjustNewLinesOperation(line, [option])
        End Function

        Protected Function CreateAdjustSpacesOperation(space As Integer, [option] As AdjustSpacesOption) As AdjustSpacesOperation
            Return FormattingOperations.CreateAdjustSpacesOperation(space, [option])
        End Function
    End Class
End Namespace
