' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class ConditionalPreprocessorHighlighter
        Inherits AbstractKeywordHighlighter(Of DirectiveTriviaSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Sub AddHighlights(directive As DirectiveTriviaSyntax, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            Dim conditionals = directive.GetMatchingConditionalDirectives(cancellationToken)
            If conditionals Is Nothing Then
                Return
            End If

            For Each conditional In conditionals
                If TypeOf conditional Is IfDirectiveTriviaSyntax Then
                    With DirectCast(conditional, IfDirectiveTriviaSyntax)
                        highlights.Add(TextSpan.FromBounds(.HashToken.SpanStart, .IfOrElseIfKeyword.Span.End))
                        If .ThenKeyword.Kind <> SyntaxKind.None Then
                            highlights.Add(.ThenKeyword.Span)
                        End If
                    End With
                ElseIf TypeOf conditional Is ElseDirectiveTriviaSyntax Then
                    With DirectCast(conditional, ElseDirectiveTriviaSyntax)
                        highlights.Add(TextSpan.FromBounds(.HashToken.SpanStart, .ElseKeyword.Span.End))
                    End With
                ElseIf TypeOf conditional Is EndIfDirectiveTriviaSyntax Then
                    With DirectCast(conditional, EndIfDirectiveTriviaSyntax)
                        highlights.Add(TextSpan.FromBounds(.HashToken.SpanStart, .IfKeyword.Span.End))
                    End With
                End If
            Next
        End Sub
    End Class
End Namespace
