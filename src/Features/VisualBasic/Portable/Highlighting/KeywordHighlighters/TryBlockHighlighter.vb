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
    Friend Class TryBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Sub AddHighlights(node As SyntaxNode, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            If TypeOf node Is ExitStatementSyntax AndAlso node.Kind <> SyntaxKind.ExitTryStatement Then
                Return
            End If

            Dim tryBlock = node.GetAncestor(Of TryBlockSyntax)()
            If tryBlock Is Nothing Then
                Return
            End If

            With tryBlock
                highlights.Add(.TryStatement.TryKeyword.Span)

                HighlightRelatedStatements(tryBlock, highlights)

                For Each catchBlock In .CatchBlocks
                    With catchBlock.CatchStatement
                        highlights.Add(.CatchKeyword.Span)

                        If .WhenClause IsNot Nothing Then
                            highlights.Add(.WhenClause.WhenKeyword.Span)
                        End If
                    End With

                    HighlightRelatedStatements(catchBlock, highlights)
                Next

                If .FinallyBlock IsNot Nothing Then
                    highlights.Add(.FinallyBlock.FinallyStatement.FinallyKeyword.Span)
                End If

                highlights.Add(.EndTryStatement.Span)
            End With
        End Sub

        Private Sub HighlightRelatedStatements(node As SyntaxNode, highlights As List(Of TextSpan))
            If node.Kind = SyntaxKind.ExitTryStatement Then
                highlights.Add(node.Span)
            Else
                For Each childNodeOrToken In node.ChildNodesAndTokens()
                    If childNodeOrToken.IsToken Then
                        Continue For
                    End If

                    Dim child = childNodeOrToken.AsNode()
                    If Not TypeOf child Is TryBlockSyntax AndAlso Not TypeOf child Is LambdaExpressionSyntax Then
                        HighlightRelatedStatements(child, highlights)
                    End If
                Next
            End If
        End Sub
    End Class
End Namespace
