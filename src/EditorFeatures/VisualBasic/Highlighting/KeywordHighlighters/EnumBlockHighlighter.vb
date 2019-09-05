' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class EnumBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            Dim endBlockStatement = TryCast(node, EndBlockStatementSyntax)
            If endBlockStatement IsNot Nothing Then
                If endBlockStatement.Kind <> SyntaxKind.EndEnumStatement Then
                    Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
                End If
            End If

            Dim enumBlock = node.GetAncestor(Of EnumBlockSyntax)()
            If enumBlock Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            Dim highlights As New List(Of TextSpan)

            With enumBlock
                With .EnumStatement
                    Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .EnumKeyword)
                    highlights.Add(TextSpan.FromBounds(firstKeyword.SpanStart, .EnumKeyword.Span.End))
                End With

                highlights.Add(.EndEnumStatement.Span)
            End With

            Return highlights
        End Function
    End Class
End Namespace
