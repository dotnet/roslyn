' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class SelectBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        Protected Overloads Overrides Iterator Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If node.IsIncorrectExitStatement(SyntaxKind.ExitSelectStatement) Then Return

            Dim selectBlock = node.GetAncestor(Of SelectBlockSyntax)()
            If selectBlock Is Nothing Then Return

            With selectBlock
                With .SelectStatement
                    Yield TextSpan.FromBounds(.SelectKeyword.SpanStart, If(.CaseKeyword.Kind <> SyntaxKind.None, .CaseKeyword, .SelectKeyword).Span.End)
                End With

                For Each caseBlock In .CaseBlocks
                    If cancellationToken.IsCancellationRequested Then Return
                    With caseBlock.CaseStatement
                        If caseBlock.Kind = SyntaxKind.CaseElseBlock Then
                            Dim elseKeyword = DirectCast(.Cases.First(), ElseCaseClauseSyntax).ElseKeyword
                            Yield TextSpan.FromBounds(.CaseKeyword.SpanStart, elseKeyword.Span.End)
                        Else
                            Yield .CaseKeyword.Span
                        End If
                    End With

                    For Each highlight In caseBlock.GetRelatedStatementHighlights(blockKind:=SyntaxKind.SelectKeyword)
                        If cancellationToken.IsCancellationRequested Then Return
                        Yield highlight
                    Next
                Next

                Yield .EndSelectStatement.Span
            End With

        End Function

    End Class
End Namespace
