' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class MultiLineLambdaExpressionHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            Dim lambdaExpression = node.GetAncestor(Of MultiLineLambdaExpressionSyntax)()
            If lambdaExpression Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            Dim highlights As New List(Of TextSpan)()

            With lambdaExpression
                Dim isAsync = False
                Dim isIterator = False

                With .SubOrFunctionHeader
                    isAsync = .Modifiers.Any(SyntaxKind.AsyncKeyword)
                    isIterator = .Modifiers.Any(SyntaxKind.IteratorKeyword)

                    Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                    highlights.Add(TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End))
                End With

                highlights.AddRange(
                    .GetRelatedStatementHighlights(
                        blockKind:= .SubOrFunctionHeader.DeclarationKeyword.Kind,
                        checkReturns:=True))

                If isIterator Then
                    highlights.AddRange(.GetRelatedYieldStatementHighlights())
                End If

                If isAsync Then
                    HighlightRelatedAwaits(lambdaExpression, highlights, cancellationToken)
                End If

                highlights.Add(.EndSubOrFunctionStatement.Span)
            End With

            Return highlights
        End Function
    End Class
End Namespace
