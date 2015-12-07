' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.BraceMatching

    <ExportBraceMatcher(LanguageNames.VisualBasic)>
    Friend Class DirectiveTriviaBraceMatcher
        Implements IBraceMatcher

        Public Async Function FindBraces(document As Document,
                                   position As Integer,
                                   Optional cancellationToken As CancellationToken = Nothing) As Task(Of BraceMatchingResult?) Implements IBraceMatcher.FindBracesAsync
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim token = root.FindToken(position, findInsideTrivia:=True)

            Dim directive = TryCast(token.Parent, DirectiveTriviaSyntax)
            If directive Is Nothing Then
                Return Nothing
            End If

            If (IsConditionalDirective(directive)) Then
                ' #If/#ElseIf/#Else/#EndIf directive cases
                Dim matchingDirectives = directive.GetMatchingConditionalDirectives(cancellationToken).ToList()
                Dim matchingDirective = matchingDirectives((matchingDirectives.IndexOf(directive) + 1) Mod matchingDirectives.Count)

                Dim directiveKeywordToken = directive.TypeSwitch(
                                            Function(context As IfDirectiveTriviaSyntax) context.IfOrElseIfKeyword,
                                            Function(context As ElseDirectiveTriviaSyntax) context.ElseKeyword,
                                            Function(context As EndIfDirectiveTriviaSyntax) context.IfKeyword)

                Dim matchingDirectiveKeywordToken = matchingDirective.TypeSwitch(
                                            Function(context As IfDirectiveTriviaSyntax) context.IfOrElseIfKeyword,
                                            Function(context As ElseDirectiveTriviaSyntax) context.ElseKeyword,
                                            Function(context As EndIfDirectiveTriviaSyntax) context.IfKeyword)

                Return New BraceMatchingResult(
                    TextSpan.FromBounds(
                        directive.HashToken.SpanStart,
                        directiveKeywordToken.Span.End),
                    TextSpan.FromBounds(
                        matchingDirective.HashToken.SpanStart,
                        matchingDirectiveKeywordToken.Span.End))
            Else
                ' #Region/#EndRegion or other directive cases.
                Dim matchingDirective = directive.GetMatchingStartOrEndDirective(cancellationToken)

                If matchingDirective Is Nothing Then
                    Return Nothing
                End If

                Dim region = If(TypeOf directive Is RegionDirectiveTriviaSyntax,
                                DirectCast(directive, RegionDirectiveTriviaSyntax),
                                DirectCast(matchingDirective, RegionDirectiveTriviaSyntax))

                Dim endRegion = If(TypeOf directive Is EndRegionDirectiveTriviaSyntax,
                                       DirectCast(directive, EndRegionDirectiveTriviaSyntax),
                                       DirectCast(matchingDirective, EndRegionDirectiveTriviaSyntax))

                Return New BraceMatchingResult(
                    TextSpan.FromBounds(
                        region.HashToken.SpanStart,
                        region.RegionKeyword.Span.End),
                    TextSpan.FromBounds(
                        endRegion.HashToken.SpanStart,
                        endRegion.RegionKeyword.Span.End))
            End If

            Return Nothing
        End Function

        Private Function IsConditionalDirective(directive As DirectiveTriviaSyntax) As Boolean
            Return directive.IsKind(SyntaxKind.IfDirectiveTrivia) OrElse
                directive.IsKind(SyntaxKind.ElseIfDirectiveTrivia) OrElse
                directive.IsKind(SyntaxKind.ElseDirectiveTrivia) OrElse
                directive.IsKind(SyntaxKind.EndIfDirectiveTrivia)
        End Function
    End Class

End Namespace
