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
    Friend Class RegionHighlighter
        Inherits AbstractKeywordHighlighter(Of DirectiveTriviaSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
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
