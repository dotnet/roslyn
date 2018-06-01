' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class XmlProcessingInstructionHighlighter
        Inherits AbstractKeywordHighlighter(Of XmlProcessingInstructionSyntax)
        Protected Overrides Iterator Function GetHighlights(xmlProcessingInstruction As XmlProcessingInstructionSyntax, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return
            With xmlProcessingInstruction
                If Not .ContainsDiagnostics AndAlso
                   Not .HasAncestor(Of DocumentationCommentTriviaSyntax)() Then
                    Yield .LessThanQuestionToken.Span
                    Yield .QuestionGreaterThanToken.Span
                End If
            End With
        End Function
    End Class
End Namespace
