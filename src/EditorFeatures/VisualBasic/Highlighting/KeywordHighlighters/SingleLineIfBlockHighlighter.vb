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

#Disable Warning RS0033 ' Importing constructor should be [Obsolete]
        <ImportingConstructor>
        Public Sub New()
#Enable Warning RS0033 ' Importing constructor should be [Obsolete]
        End Sub

        Protected Overloads Overrides Function GetHighlights(ifStatement As SingleLineIfStatementSyntax, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            Dim highlights As New List(Of TextSpan)

            highlights.Add(ifStatement.IfKeyword.Span)

            highlights.Add(ifStatement.ThenKeyword.Span)

            If ifStatement.ElseClause IsNot Nothing Then
                highlights.Add(ifStatement.ElseClause.ElseKeyword.Span)
            End If

            Return highlights
        End Function
    End Class
End Namespace
