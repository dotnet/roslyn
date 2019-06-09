' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class SelectBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If node.IsIncorrectExitStatement(SyntaxKind.ExitSelectStatement) Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            Dim selectBlock = node.GetAncestor(Of SelectBlockSyntax)()
            If selectBlock Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            Dim highlights As New List(Of TextSpan)

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

            Return highlights
        End Function

    End Class
End Namespace
