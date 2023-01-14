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
    Friend Class XmlElementHighlighter
        Inherits AbstractKeywordHighlighter(Of XmlNodeSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Sub AddHighlights(node As XmlNodeSyntax, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
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
        End Sub
    End Class
End Namespace
