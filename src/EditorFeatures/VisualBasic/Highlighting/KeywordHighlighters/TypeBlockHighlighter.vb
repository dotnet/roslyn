' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class TypeBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        Protected Overloads Overrides Iterator Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return
            Dim endBlockStatement = TryCast(node, EndBlockStatementSyntax)
            If endBlockStatement IsNot Nothing Then
                If Not endBlockStatement.IsKind(SyntaxKind.EndClassStatement,
                                                SyntaxKind.EndInterfaceStatement,
                                                SyntaxKind.EndModuleStatement,
                                                SyntaxKind.EndStructureStatement) Then
                    Return
                End If
            End If

            Dim typeBlock = node.GetAncestor(Of TypeBlockSyntax)()
            If typeBlock Is Nothing Then Return

            With typeBlock
                With .BlockStatement
                    Dim firstKeyword = If(.Modifiers.Count > 0, .Modifiers.First(), .DeclarationKeyword)
                    Yield TextSpan.FromBounds(firstKeyword.SpanStart, .DeclarationKeyword.Span.End)
                End With

                Yield .EndBlockStatement.Span
            End With
        End Function
    End Class
End Namespace
