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

        Protected Overloads Overrides Function GetHighlights(directive As DirectiveTriviaSyntax, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
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

                    Return {TextSpan.FromBounds(region.HashToken.SpanStart, region.RegionKeyword.Span.End),
                            TextSpan.FromBounds(endRegion.HashToken.SpanStart, endRegion.RegionKeyword.Span.End)}
                End If
            End If

            Return Enumerable.Empty(Of TextSpan)()
        End Function
    End Class
End Namespace
