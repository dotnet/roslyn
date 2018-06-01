' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class ConditionalPreprocessorHighlighter
        Inherits AbstractKeywordHighlighter(Of DirectiveTriviaSyntax)

        Protected Overloads Overrides Iterator Function GetHighlights(directive As DirectiveTriviaSyntax, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return
            Dim conditionals = directive.GetMatchingConditionalDirectives(cancellationToken)
            If conditionals Is Nothing Then Return

            Dim highlights As New List(Of TextSpan)

            For Each conditional In conditionals
                If TypeOf conditional Is IfDirectiveTriviaSyntax Then
                    With DirectCast(conditional, IfDirectiveTriviaSyntax)
                        Yield TextSpan.FromBounds(.HashToken.SpanStart, .IfOrElseIfKeyword.Span.End)
                        If .ThenKeyword.Kind <> SyntaxKind.None Then Yield .ThenKeyword.Span
                    End With
                ElseIf TypeOf conditional Is ElseDirectiveTriviaSyntax Then
                    With DirectCast(conditional, ElseDirectiveTriviaSyntax)
                        Yield TextSpan.FromBounds(.HashToken.SpanStart, .ElseKeyword.Span.End)
                    End With
                ElseIf TypeOf conditional Is EndIfDirectiveTriviaSyntax Then
                    With DirectCast(conditional, EndIfDirectiveTriviaSyntax)
                        Yield TextSpan.FromBounds(.HashToken.SpanStart, .IfKeyword.Span.End)
                    End With
                End If
            Next

        End Function
    End Class
End Namespace
