﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        Protected Overloads Overrides Sub AddHighlights(node As SyntaxNode, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            Dim withBlock = node.GetAncestor(Of WithBlockSyntax)()
            If withBlock Is Nothing Then
                Return
            End If

            With withBlock
                highlights.Add(.WithStatement.WithKeyword.Span)
                highlights.Add(.EndWithStatement.Span)
            End With
        End Sub
    End Class
End Namespace
