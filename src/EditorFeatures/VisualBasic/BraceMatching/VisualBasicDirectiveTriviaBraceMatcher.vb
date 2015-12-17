' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.BraceMatching

    <ExportBraceMatcher(LanguageNames.VisualBasic)>
    Friend Class VisualBasicDirectiveTriviaBraceMatcher
        Inherits AbstractDirectiveTriviaBraceMatcher(Of DirectiveTriviaSyntax,
             IfDirectiveTriviaSyntax, IfDirectiveTriviaSyntax,
             ElseDirectiveTriviaSyntax, EndIfDirectiveTriviaSyntax,
             RegionDirectiveTriviaSyntax, EndRegionDirectiveTriviaSyntax)

        Friend Overrides Function GetMatchingConditionalDirectives(directive As DirectiveTriviaSyntax, cancellationToken As CancellationToken) As List(Of DirectiveTriviaSyntax)
            Return directive.GetMatchingConditionalDirectives(cancellationToken)?.ToList()
        End Function

        Friend Overrides Function GetMatchingDirective(directive As DirectiveTriviaSyntax, cancellationToken As CancellationToken) As DirectiveTriviaSyntax
            Return directive.GetMatchingStartOrEndDirective(cancellationToken)
        End Function

        Friend Overrides Function GetSpanForTagging(directive As DirectiveTriviaSyntax) As TextSpan
            Dim keywordToken = directive.TypeSwitch(
                                           Function(context As IfDirectiveTriviaSyntax) context.IfOrElseIfKeyword,
                                           Function(context As ElseDirectiveTriviaSyntax) context.ElseKeyword,
                                           Function(context As EndIfDirectiveTriviaSyntax) context.IfKeyword,
                                           Function(context As RegionDirectiveTriviaSyntax) context.RegionKeyword,
                                           Function(context As EndRegionDirectiveTriviaSyntax) context.RegionKeyword)

            Return TextSpan.FromBounds(directive.HashToken.SpanStart, keywordToken.Span.End)
        End Function

    End Class

End Namespace
