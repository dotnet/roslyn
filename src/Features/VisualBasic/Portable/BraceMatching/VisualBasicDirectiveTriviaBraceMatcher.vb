' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.BraceMatching
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.BraceMatching
    <ExportBraceMatcher(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicDirectiveTriviaBraceMatcher
        Inherits AbstractDirectiveTriviaBraceMatcher(Of DirectiveTriviaSyntax,
             IfDirectiveTriviaSyntax, IfDirectiveTriviaSyntax,
             ElseDirectiveTriviaSyntax, EndIfDirectiveTriviaSyntax,
             RegionDirectiveTriviaSyntax, EndRegionDirectiveTriviaSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetMatchingConditionalDirectives(directive As DirectiveTriviaSyntax, cancellationToken As CancellationToken) As ImmutableArray(Of DirectiveTriviaSyntax)
            Return directive.GetMatchingConditionalDirectives(cancellationToken)
        End Function

        Protected Overrides Function GetMatchingDirective(directive As DirectiveTriviaSyntax, cancellationToken As CancellationToken) As DirectiveTriviaSyntax
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
