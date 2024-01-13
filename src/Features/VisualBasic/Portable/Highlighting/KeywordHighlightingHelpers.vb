' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting
    Friend Module KeywordHighlightingHelpers

        Private Sub HighlightRelatedStatements(Of T As SyntaxNode)(
            node As SyntaxNode,
            highlights As List(Of TextSpan),
            blockKind As SyntaxKind,
            checkReturns As Boolean)

            If blockKind <> SyntaxKind.None AndAlso TypeOf node Is ExitStatementSyntax Then
                With DirectCast(node, ExitStatementSyntax)
                    If .BlockKeyword.Kind = blockKind Then
                        highlights.Add(.Span)
                    End If
                End With
            ElseIf blockKind <> SyntaxKind.None AndAlso TypeOf node Is ContinueStatementSyntax Then
                With DirectCast(node, ContinueStatementSyntax)
                    If .BlockKeyword.Kind = blockKind Then
                        highlights.Add(.Span)
                    End If
                End With
            ElseIf checkReturns AndAlso TypeOf node Is ReturnStatementSyntax Then
                With DirectCast(node, ReturnStatementSyntax)
                    highlights.Add(.ReturnKeyword.Span)
                End With
            Else
                For Each child In node.ChildNodes()
                    If Not TypeOf child Is T AndAlso
                       Not TypeOf child Is LambdaExpressionSyntax Then

                        HighlightRelatedStatements(Of T)(child, highlights, blockKind, checkReturns)
                    End If
                Next
            End If
        End Sub

        <Extension()>
        Friend Function GetRelatedStatementHighlights(Of T As SyntaxNode)(
            node As T,
            blockKind As SyntaxKind,
            Optional checkReturns As Boolean = False) As IEnumerable(Of TextSpan)

            Dim highlights As New List(Of TextSpan)
            HighlightRelatedStatements(Of T)(node, highlights, blockKind, checkReturns)
            Return highlights
        End Function

        <Extension()>
        Friend Function IsIncorrectContinueStatement(node As SyntaxNode, expectedKind As SyntaxKind) As Boolean
            Dim continueStatement = TryCast(node, ContinueStatementSyntax)
            If continueStatement IsNot Nothing Then
                Return continueStatement.Kind <> expectedKind
            End If

            Return False
        End Function

        <Extension()>
        Friend Function IsIncorrectExitStatement(node As SyntaxNode, expectedKind As SyntaxKind) As Boolean
            Dim exitStatement = TryCast(node, ExitStatementSyntax)
            If exitStatement IsNot Nothing Then
                Return exitStatement.Kind <> expectedKind
            End If

            Return False
        End Function

        <Extension>
        Friend Sub HighlightRelatedAwaits(node As SyntaxNode, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            If TypeOf node Is AwaitExpressionSyntax Then
                With DirectCast(node, AwaitExpressionSyntax)
                    ' If there is already a highlight for the previous token and it is on the same line,
                    ' we should expand the span of that highlight to include the Await keyword.
                    ' Otherwise, just add the Await keyword span.

                    Dim handled = False
                    Dim previousToken = .AwaitKeyword.GetPreviousToken()
                    If Not previousToken.Span.IsEmpty Then
                        Dim text = node.SyntaxTree.GetText(cancellationToken)
                        Dim previousLine = text.Lines.IndexOf(previousToken.SpanStart)
                        Dim awaitLine = text.Lines.IndexOf(.AwaitKeyword.SpanStart)

                        If previousLine = awaitLine Then
                            Dim index = highlights.FindIndex(Function(s) s.Contains(previousToken.Span))
                            If index >= 0 Then
                                Dim span = highlights(index)
                                highlights(index) = TextSpan.FromBounds(span.Start, .AwaitKeyword.Span.End)
                                handled = True
                            End If
                        End If
                    End If

                    If Not handled Then
                        highlights.Add(.AwaitKeyword.Span)
                    End If
                End With
            End If

            For Each child In node.ChildNodes()
                If Not TypeOf child Is LambdaExpressionSyntax Then
                    HighlightRelatedAwaits(child, highlights, cancellationToken)
                End If
            Next
        End Sub

        Private Sub HighlightRelatedYieldStatements(Of T)(node As SyntaxNode, highlights As List(Of TextSpan))
            If TypeOf node Is YieldStatementSyntax Then
                With DirectCast(node, YieldStatementSyntax)
                    highlights.Add(.YieldKeyword.Span)
                End With
            Else
                For Each child In node.ChildNodes()
                    If Not TypeOf child Is LambdaExpressionSyntax Then
                        HighlightRelatedYieldStatements(Of T)(child, highlights)
                    End If
                Next
            End If
        End Sub

        <Extension>
        Friend Function GetRelatedYieldStatementHighlights(Of T As SyntaxNode)(node As T) As IEnumerable(Of TextSpan)
            Dim highlights As New List(Of TextSpan)
            HighlightRelatedYieldStatements(Of T)(node, highlights)
            Return highlights
        End Function

    End Module
End Namespace
