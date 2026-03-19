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
    Friend Class SelectBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Sub AddHighlights(node As SyntaxNode, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            If node.IsIncorrectExitStatement(SyntaxKind.ExitSelectStatement) Then
                Return
            End If

            Dim selectBlock = node.GetAncestor(Of SelectBlockSyntax)()
            If selectBlock Is Nothing Then
                Return
            End If

            With selectBlock
                With .SelectStatement
                    highlights.Add(
                        TextSpan.FromBounds(
                            .SelectKeyword.SpanStart,
                            If(.CaseKeyword.Kind <> SyntaxKind.None, .CaseKeyword, .SelectKeyword).Span.End))
                End With

                For Each caseBlock In .CaseBlocks
                    With caseBlock.CaseStatement
                        If caseBlock.Kind = SyntaxKind.CaseElseBlock Then
                            Dim elseKeyword = DirectCast(.Cases.First(), ElseCaseClauseSyntax).ElseKeyword
                            highlights.Add(TextSpan.FromBounds(.CaseKeyword.SpanStart, elseKeyword.Span.End))
                        Else
                            highlights.Add(.CaseKeyword.Span)
                        End If
                    End With

                    highlights.AddRange(
                        caseBlock.GetRelatedStatementHighlights(
                            blockKind:=SyntaxKind.SelectKeyword))
                Next

                highlights.Add(.EndSelectStatement.Span)
            End With
        End Sub
    End Class
End Namespace
