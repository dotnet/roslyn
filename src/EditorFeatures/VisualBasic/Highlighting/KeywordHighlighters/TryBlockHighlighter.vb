' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class TryBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If TypeOf node Is ExitStatementSyntax AndAlso node.Kind <> SyntaxKind.ExitTryStatement Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            Dim tryBlock = node.GetAncestor(Of TryBlockSyntax)()
            If tryBlock Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            Dim highlights As New List(Of TextSpan)

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

                Return highlights
            End With
        End Function

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
