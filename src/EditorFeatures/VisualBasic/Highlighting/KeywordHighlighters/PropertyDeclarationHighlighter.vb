' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class PropertyDeclarationHighlighter
        Inherits AbstractKeywordHighlighter(Of PropertyStatementSyntax)

        Protected Overrides Iterator Function GetHighlights(propertyDeclaration As PropertyStatementSyntax, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            ' If the ancestor is not a property block, treat this as an auto-property.
            ' Otherwise, let the PropertyBlockHighlighter take over.
            Dim propertyBlock = propertyDeclaration.GetAncestor(Of PropertyBlockSyntax)()
            If propertyBlock IsNot Nothing Then Return

            With propertyDeclaration
                Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                Yield TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End)

                If .ImplementsClause IsNot Nothing Then Yield .ImplementsClause.ImplementsKeyword.Span
            End With

        End Function
    End Class
End Namespace
