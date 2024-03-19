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
    Friend Class ConditionalPreprocessorHighlighter
        Inherits AbstractKeywordHighlighter(Of DirectiveTriviaSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Sub AddHighlights(directive As DirectiveTriviaSyntax, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            Dim conditionals = directive.GetMatchingConditionalDirectives(cancellationToken)
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
