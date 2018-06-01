' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Highlighting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.KeywordHighlighting
    <ExportHighlighter(LanguageNames.VisualBasic)>
    Friend Class RegionHighlighter
        Inherits AbstractKeywordHighlighter(Of DirectiveTriviaSyntax)

        Protected Overloads Overrides Iterator Function GetHighlights(directive As DirectiveTriviaSyntax, cancellationToken As CancellationToken) As IEnumerable(Of TextSpan)
            If cancellationToken.IsCancellationRequested Then Return
            If TypeOf directive IsNot RegionDirectiveTriviaSyntax AndAlso TypeOf directive IsNot EndRegionDirectiveTriviaSyntax Then Return
            Dim match = directive.GetMatchingStartOrEndDirective(cancellationToken)
            If match Is Nothing Then Return
            Dim region = DirectCast(If(TypeOf directive Is RegionDirectiveTriviaSyntax, directive, match), EndRegionDirectiveTriviaSyntax)
            Dim endRegion = DirectCast(If(TypeOf directive Is EndRegionDirectiveTriviaSyntax, directive, match), EndRegionDirectiveTriviaSyntax)
            Yield TextSpan.FromBounds(region.HashToken.SpanStart, region.RegionKeyword.Span.End)
            Yield TextSpan.FromBounds(endRegion.HashToken.SpanStart, endRegion.RegionKeyword.Span.End)
        End Function
    End Class
End Namespace
