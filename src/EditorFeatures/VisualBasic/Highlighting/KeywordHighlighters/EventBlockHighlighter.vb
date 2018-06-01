' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class EventBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        Protected Overrides Iterator Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return
            Dim eventBlock = node.GetAncestor(Of EventBlockSyntax)()
            If eventBlock Is Nothing Then Return

            With eventBlock
                With .EventStatement
                    ' This span calculation should also capture the Custom keyword
                    Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                    Yield TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End)

                    If .ImplementsClause IsNot Nothing Then Yield .ImplementsClause.ImplementsKeyword.Span

                End With

                Yield .EndEventStatement.Span
            End With

        End Function
    End Class
End Namespace
