' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class TryBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        Protected Overloads Overrides Iterator Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return
            If TypeOf node Is ExitStatementSyntax AndAlso node.Kind <> SyntaxKind.ExitTryStatement Then Return

            Dim tryBlock = node.GetAncestor(Of TryBlockSyntax)()
            If tryBlock Is Nothing Then Return

            With tryBlock
                Yield .TryStatement.TryKeyword.Span

                For Each highlight In HighlightRelatedStatements(tryBlock, cancellationToken)
                    If cancellationToken.IsCancellationRequested Then Return
                    Yield highlight
                Next

                For Each catchBlock In .CatchBlocks
                    With catchBlock.CatchStatement
                        Yield .CatchKeyword.Span

                        If .WhenClause IsNot Nothing Then Yield .WhenClause.WhenKeyword.Span

                    End With
                    For Each highlight In HighlightRelatedStatements(catchBlock, cancellationToken)
                        If cancellationToken.IsCancellationRequested Then Return
                        Yield highlight
                    Next
                Next

                If .FinallyBlock IsNot Nothing Then Yield .FinallyBlock.FinallyStatement.FinallyKeyword.Span

                Yield .EndTryStatement.Span

            End With
        End Function

        Private Iterator Function HighlightRelatedStatements(thisnode As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            Dim nodes As New LinkedList(Of SyntaxNode)
            nodes.AddFirst(thisnode)
            While nodes.Count > 0

                Dim node = nodes(0) : nodes.RemoveFirst()
                If cancellationToken.IsCancellationRequested Then Continue While

                If node.Kind = SyntaxKind.ExitTryStatement Then
                    Yield node.Span
                Else
                    Dim children = node.ChildNodes.Where(Function(child) TypeOf child IsNot TryBlockSyntax AndAlso TypeOf child IsNot LambdaExpressionSyntax)
                    nodes.AddRangeAtHead(children)

                    'For Each childNodeOrToken In node.ChildNodesAndTokens()
                    'If childNodeOrToken.IsToken Then Continue For
                    'Dim child = childNodeOrToken.AsNode()
                    '    If TypeOf child IsNot TryBlockSyntax AndAlso TypeOf child Is Not LambdaExpressionSyntax Then
                    '        For Each highlight In HighlightRelatedStatements(child)
                    '            Yield highlight
                    '        Next
                    '    End If
                    'Next
                End If
            End While
        End Function
    End Class
End Namespace
