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
    Friend Class SingleLineIfBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SingleLineIfStatementSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Sub AddHighlights(ifStatement As SingleLineIfStatementSyntax, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            highlights.Add(ifStatement.IfKeyword.Span)

            highlights.Add(ifStatement.ThenKeyword.Span)

            If ifStatement.ElseClause IsNot Nothing Then
                highlights.Add(ifStatement.ElseClause.ElseKeyword.Span)
            End If
        End Sub
    End Class
End Namespace
