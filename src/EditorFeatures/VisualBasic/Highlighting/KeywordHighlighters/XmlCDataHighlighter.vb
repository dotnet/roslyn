' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class XmlCDataHighlighter
        Inherits AbstractKeywordHighlighter(Of XmlCDataSectionSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Function GetHighlights(xmlComment As XmlCDataSectionSyntax, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            Dim highlights As New List(Of TextSpan)

            With xmlComment
                If Not .ContainsDiagnostics AndAlso
                   Not .HasAncestor(Of DocumentationCommentTriviaSyntax)() Then
                    highlights.Add(.BeginCDataToken.Span)
                    highlights.Add(.EndCDataToken.Span)
                End If
            End With

            Return highlights
        End Function
    End Class
End Namespace
