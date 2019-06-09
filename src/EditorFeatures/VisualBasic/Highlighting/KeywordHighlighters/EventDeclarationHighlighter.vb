' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class EventDeclarationHighlighter
        Inherits AbstractKeywordHighlighter(Of EventStatementSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overrides Function GetHighlights(eventDeclaration As EventStatementSyntax, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            ' If the ancestor is not a event block, treat this as a single line event.
            ' Otherwise, let the EventBlockHighlighter take over.
            Dim eventBlock = eventDeclaration.GetAncestor(Of EventBlockSyntax)()
            If eventBlock IsNot Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            Dim highlights As New List(Of TextSpan)()

            With eventDeclaration
                Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                highlights.Add(TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End))

                If .ImplementsClause IsNot Nothing Then
                    highlights.Add(.ImplementsClause.ImplementsKeyword.Span)
                End If
            End With

            Return highlights
        End Function
    End Class
End Namespace
