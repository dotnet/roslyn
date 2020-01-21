' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class RegionHighlighter
        Inherits AbstractKeywordHighlighter(Of DirectiveTriviaSyntax)

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Protected Overloads Overrides Sub AddHighlights(directive As DirectiveTriviaSyntax, highlights As List(Of TextSpan), cancellationToken As CancellationToken)
            If TypeOf directive Is RegionDirectiveTriviaSyntax OrElse
               TypeOf directive Is EndRegionDirectiveTriviaSyntax Then

                Dim match = directive.GetMatchingStartOrEndDirective(cancellationToken)
                If match IsNot Nothing Then

                    Dim region = If(TypeOf directive Is RegionDirectiveTriviaSyntax,
                                    DirectCast(directive, RegionDirectiveTriviaSyntax),
                                    DirectCast(match, RegionDirectiveTriviaSyntax))

                    Dim endRegion = If(TypeOf directive Is EndRegionDirectiveTriviaSyntax,
                                       DirectCast(directive, EndRegionDirectiveTriviaSyntax),
                                       DirectCast(match, EndRegionDirectiveTriviaSyntax))

                    highlights.Add(TextSpan.FromBounds(region.HashToken.SpanStart, region.RegionKeyword.Span.End))
                    highlights.Add(TextSpan.FromBounds(endRegion.HashToken.SpanStart, endRegion.RegionKeyword.Span.End))
                End If
            End If
        End Sub
    End Class
End Namespace
