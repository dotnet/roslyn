' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class SingleLineIfBlockHighlighter
        Inherits AbstractKeywordHighlighter(Of SingleLineIfStatementSyntax)

        Protected Overloads Overrides Iterator Function GetHighlights(ifStatement As SingleLineIfStatementSyntax, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return
            With ifStatement
                Yield .IfKeyword.Span
                Yield .ThenKeyword.Span
                If .ElseClause IsNot Nothing Then Yield .ElseClause.ElseKeyword.Span
            End With
        End Function
    End Class
End Namespace
