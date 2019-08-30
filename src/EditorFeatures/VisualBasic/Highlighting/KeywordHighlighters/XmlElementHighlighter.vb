' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class XmlElementHighlighter
        Inherits AbstractKeywordHighlighter(Of XmlNodeSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Function GetHighlights(node As XmlNodeSyntax, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            Dim highlights As New List(Of TextSpan)

            Dim xmlElement = node.GetAncestor(Of XmlElementSyntax)()
            With xmlElement
                If xmlElement IsNot Nothing AndAlso
                   Not .ContainsDiagnostics AndAlso
                   Not .HasAncestor(Of DocumentationCommentTriviaSyntax)() Then

                    With .StartTag
                        If .Attributes.Count = 0 Then
                            highlights.Add(.Span)
                        Else
                            highlights.Add(TextSpan.FromBounds(.LessThanToken.SpanStart, .Name.Span.End))
                            highlights.Add(.GreaterThanToken.Span)
                        End If
                    End With
                    highlights.Add(.EndTag.Span)
                End If

            End With

            Return highlights
        End Function
    End Class
End Namespace
