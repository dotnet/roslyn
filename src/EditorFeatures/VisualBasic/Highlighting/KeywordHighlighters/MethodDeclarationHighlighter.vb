' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class MethodDeclarationHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            Dim methodBlock = node.GetAncestor(Of MethodBlockBaseSyntax)()
            If methodBlock Is Nothing OrElse Not TypeOf methodBlock.BlockStatement Is MethodStatementSyntax Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            Dim highlights As New List(Of TextSpan)()

            With methodBlock
                Dim isAsync = False
                Dim isIterator = False

                With DirectCast(.BlockStatement, MethodStatementSyntax)
                    isAsync = .Modifiers.Any(SyntaxKind.AsyncKeyword)
                    isIterator = .Modifiers.Any(SyntaxKind.IteratorKeyword)

                    Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                    highlights.Add(TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End))

                    If .HandlesClause IsNot Nothing Then
                        highlights.Add(.HandlesClause.HandlesKeyword.Span)
                    End If

                    If .ImplementsClause IsNot Nothing Then
                        highlights.Add(.ImplementsClause.ImplementsKeyword.Span)
                    End If
                End With

                highlights.AddRange(
                    .GetRelatedStatementHighlights(
                        blockKind:= .BlockStatement.DeclarationKeyword.Kind,
                        checkReturns:=True))

                If isIterator Then
                    highlights.AddRange(.GetRelatedYieldStatementHighlights())
                End If

                If isAsync Then
                    HighlightRelatedAwaits(methodBlock, highlights, cancellationToken)
                End If

                highlights.Add(.EndBlockStatement.Span)
            End With

            Return highlights
        End Function
    End Class
End Namespace
