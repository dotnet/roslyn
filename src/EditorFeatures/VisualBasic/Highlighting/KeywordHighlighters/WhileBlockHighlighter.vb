' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class WhileBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        Protected Overloads Overrides Iterator Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return
            If node.IsIncorrectContinueStatement(SyntaxKind.ContinueWhileStatement) Then Return
            If node.IsIncorrectExitStatement(SyntaxKind.ExitWhileStatement) Then Return
            Dim whileBlock = node.GetAncestor(Of WhileBlockSyntax)()
            If whileBlock Is Nothing Then Return
            With whileBlock
                Yield .WhileStatement.WhileKeyword.Span
                For Each highlight In .GetRelatedStatementHighlights(blockKind:=SyntaxKind.WhileKeyword)
                    If cancellationToken.IsCancellationRequested Then Return
                    Yield highlight
                Next
                Yield .EndWhileStatement.Span
            End With
        End Function

    End Class
End Namespace
