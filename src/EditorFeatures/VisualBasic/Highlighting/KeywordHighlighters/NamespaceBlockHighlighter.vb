' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class NamespaceBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        Protected Overrides Iterator Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return
            Dim namespaceBlock = node.GetAncestor(Of NamespaceBlockSyntax)()
            If namespaceBlock Is Nothing Then Return

            With namespaceBlock
                Yield .NamespaceStatement.NamespaceKeyword.Span
                Yield .EndNamespaceStatement.Span
            End With
        End Function
    End Class
End Namespace
