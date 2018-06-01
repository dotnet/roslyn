' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class EnumBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        Protected Overloads Overrides Iterator Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return
            Dim endBlockStatement = TryCast(node, EndBlockStatementSyntax)
            If endBlockStatement IsNot Nothing AndAlso endBlockStatement.Kind <> SyntaxKind.EndEnumStatement Then Return


            Dim enumBlock = node.GetAncestor(Of EnumBlockSyntax)()
            If enumBlock Is Nothing Then Return

            With enumBlock
                With .EnumStatement
                    Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .EnumKeyword)
                    Yield TextSpan.FromBounds(firstKeyword.SpanStart, .EnumKeyword.Span.End)
                End With

                Yield .EndEnumStatement.Span
            End With

        End Function
    End Class
End Namespace
