' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Highlighting
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic), [Shared]>
    Friend Class XmlEmbeddedExpressionHighlighter
        Inherits AbstractKeywordHighlighter(Of XmlEmbeddedExpressionSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Sub addHighlights(xmlEmbeddExpression As XmlEmbeddedExpressionSyntax, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            With xmlEmbeddExpression
                If Not .ContainsDiagnostics AndAlso
                   Not .HasAncestor(Of DocumentationCommentTriviaSyntax)() Then
                    highlights.Add(.LessThanPercentEqualsToken.Span)
                    highlights.Add(.PercentGreaterThanToken.Span)
                End If
            End With
        End Sub
    End Class
End Namespace
