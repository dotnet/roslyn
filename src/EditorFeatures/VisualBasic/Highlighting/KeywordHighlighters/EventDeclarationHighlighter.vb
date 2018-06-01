' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class EventDeclarationHighlighter
        Inherits AbstractKeywordHighlighter(Of EventStatementSyntax)

        Protected Overrides Iterator Function GetHighlights(eventDeclaration As EventStatementSyntax, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return
            ' If the ancestor is not a event block, treat this as a single line event.
            ' Otherwise, let the EventBlockHighlighter take over.
            Dim eventBlock = eventDeclaration.GetAncestor(Of EventBlockSyntax)()
            If eventBlock IsNot Nothing Then Return

            Dim highlights As New List(Of TextSpan)()

            With eventDeclaration
                Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                Yield TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End)

                If .ImplementsClause IsNot Nothing Then Yield .ImplementsClause.ImplementsKeyword.Span

            End With
        End Function
    End Class
End Namespace
