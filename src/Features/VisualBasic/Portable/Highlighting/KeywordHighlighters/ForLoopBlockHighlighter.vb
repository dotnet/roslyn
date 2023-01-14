' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Highlighting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic), [Shared]>
    Friend Class ForLoopBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Sub AddHighlights(node As SyntaxNode, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            Dim forBlock = GetForBlockFromNode(node)
            If forBlock Is Nothing Then
                Return
            End If

            If TypeOf forBlock.ForOrForEachStatement Is ForStatementSyntax Then
                With DirectCast(forBlock.ForOrForEachStatement, ForStatementSyntax)
                    highlights.Add(.ForKeyword.Span)
                    highlights.Add(.ToKeyword.Span)
                    If .StepClause IsNot Nothing Then
                        highlights.Add(.StepClause.StepKeyword.Span)
                    End If
                End With
            ElseIf TypeOf forBlock.ForOrForEachStatement Is ForEachStatementSyntax Then
                With DirectCast(forBlock.ForOrForEachStatement, ForEachStatementSyntax)
                    highlights.Add(TextSpan.FromBounds(.ForKeyword.SpanStart, .EachKeyword.Span.End))
                    highlights.Add(.InKeyword.Span)
                End With
            Else
                Throw ExceptionUtilities.UnexpectedValue(forBlock.ForOrForEachStatement)
            End If

            highlights.AddRange(
                forBlock.GetRelatedStatementHighlights(
                    blockKind:=SyntaxKind.ForKeyword))

            Dim nextStatement = GetNextStatementMatchingForBlock(forBlock)

            If nextStatement IsNot Nothing Then
                highlights.Add(nextStatement.NextKeyword.Span)
            End If
        End Sub

        Private Shared Function GetForBlockFromNode(node As SyntaxNode) As ForOrForEachBlockSyntax
            If node.IsIncorrectContinueStatement(SyntaxKind.ContinueForStatement) OrElse
               node.IsIncorrectExitStatement(SyntaxKind.ExitForStatement) Then
                Return Nothing
            End If

            ' If cursor is in the Next statement, find the outermost For block logically associated
            ' with it because Next <identifier list> statements get tied to the innermost matching 
            ' For block but we want to highlight the outermost matching For block. 

            If TypeOf node Is NextStatementSyntax Then
                ' If there is no For block the correct number of levels out (consider 2 nested For 
                ' blocks terminated by "Next c, b, a"), then choose the outermost even if it's not 
                ' the exact match.
                Return GetForBlocksMatchingNextStatement(DirectCast(node, NextStatementSyntax)).FirstOrDefault()
            Else
                Return node.AncestorsAndSelf().OfType(Of ForOrForEachBlockSyntax)().FirstOrDefault()
            End If
        End Function

        ''' <summary>
        ''' Find the Next statement that closes this For block (if one exists). Normally that
        ''' would just be the one associated with forBlock, but if we have a "Next a, b" 
        ''' statement that is closing multiple loops, the Next statement is attached to the 
        ''' innermost matching loop.
        ''' </summary>
        Private Shared Function GetNextStatementMatchingForBlock(forBlock As ForOrForEachBlockSyntax) As NextStatementSyntax
            Dim forBlockChild = forBlock

            While forBlockChild IsNot Nothing
                If forBlockChild.NextStatement IsNot Nothing Then
                    ' Once we find a candidate Next statement, it must either be the one that
                    ' closes our For block or there must not exist a matching Next statement 
                    ' so give up the search. If a more deeply nested For block had the Next 
                    ' statement which closes our For block, then that Next statement would 
                    ' have been associated with the For block we're currently inspecting.

                    ' Check to see whether the Next statement found closes enough For blocks
                    If GetForBlocksMatchingNextStatement(forBlockChild.NextStatement).Contains(forBlock) Then
                        Return forBlockChild.NextStatement
                    Else
                        Return Nothing
                    End If
                End If

                ' Choose the last immediate child For block at each level. Any earlier For blocks 
                ' in the set of children cannot possibly close this For block because if it did 
                ' then IT would be the last For block instead. Similarly, if we find a Next 
                ' statement in this manner that closes sufficiently many For blocks then there 
                ' can be nothing between the end of that For block and the end of our For block.
                forBlockChild = forBlockChild.ChildNodes().OfType(Of ForOrForEachBlockSyntax).LastOrDefault()
            End While

            Return Nothing
        End Function

        ''' <summary>
        ''' Returns all For blocks logically matching the Next statement, ordered from outermost to 
        ''' innermost. Do not consider the actual names of the loop variables because highlighting 
        ''' should work even if the wrong identifier names are listed. 
        ''' </summary>
        Private Shared Function GetForBlocksMatchingNextStatement(nextStatement As NextStatementSyntax) As IEnumerable(Of ForOrForEachBlockSyntax)
            ' If there are 0 control variables, then one for block is closed by the Next statement
            Dim numExpectedForBlocksMatched = Math.Max(nextStatement.ControlVariables.Count(), 1)
            Return nextStatement.GetAncestors(Of ForOrForEachBlockSyntax).Take(numExpectedForBlocksMatched).Reverse()
        End Function
    End Class
End Namespace
