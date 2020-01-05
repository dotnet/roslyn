' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class SingleLineIfBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SingleLineIfStatementSyntax)

        <ImportingConstructor>
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
