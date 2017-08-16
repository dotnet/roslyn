﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class PropertyBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        Protected Overrides Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            Dim propertyBlock = node.GetAncestor(Of PropertyBlockSyntax)()
            If propertyBlock Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            Dim highlights As New List(Of TextSpan)()

            With propertyBlock
                With .PropertyStatement
                    Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                    highlights.Add(TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End))

                    If .ImplementsClause IsNot Nothing Then
                        highlights.Add(.ImplementsClause.ImplementsKeyword.Span)
                    End If
                End With

                highlights.Add(.EndPropertyStatement.Span)
            End With

            Return highlights
        End Function
    End Class
End Namespace
