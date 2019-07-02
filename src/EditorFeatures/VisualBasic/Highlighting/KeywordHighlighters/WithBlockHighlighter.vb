' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class WithBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SyntaxNode)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Function GetHighlights(node As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            Dim withBlock = node.GetAncestor(Of WithBlockSyntax)()
            If withBlock Is Nothing Then
                Return SpecializedCollections.EmptyEnumerable(Of TextSpan)()
            End If

            With withBlock
                Return { .WithStatement.WithKeyword.Span,
                        .EndWithStatement.Span}
            End With
        End Function
    End Class
End Namespace
