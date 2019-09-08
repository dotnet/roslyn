// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpMethodExtractor
    {
        private class CSharpTriviaResult : TriviaResult
        {
            public static async Task<CSharpTriviaResult> ProcessAsync(SelectionResult selectionResult, CancellationToken cancellationToken)
            {
                var preservationService = selectionResult.SemanticDocument.Document.Project.LanguageServices.GetService<ISyntaxTriviaService>();
                var root = selectionResult.SemanticDocument.Root;
                var result = preservationService.SaveTriviaAroundSelection(root, selectionResult.FinalSpan);
                return new CSharpTriviaResult(
                    await selectionResult.SemanticDocument.WithSyntaxRootAsync(result.Root, cancellationToken).ConfigureAwait(false),
                    result);
            }

            private CSharpTriviaResult(SemanticDocument document, ITriviaSavedResult result)
                : base(document, result, (int)SyntaxKind.EndOfLineTrivia, (int)SyntaxKind.WhitespaceTrivia)
            {
            }

            protected override AnnotationResolver GetAnnotationResolver(SyntaxNode callsite, SyntaxNode method)
            {
                if (callsite == null || !(method is MethodDeclarationSyntax methodDefinition))
                {
                    return null;
                }

                return (node, location, annotation) => AnnotationResolver(node, location, annotation, callsite, methodDefinition);
            }

            protected override TriviaResolver GetTriviaResolver(SyntaxNode method)
            {
                if (!(method is MethodDeclarationSyntax methodDefinition))
                {
                    return null;
                }

                return (location, tokenPair, triviaMap) => TriviaResolver(location, tokenPair, triviaMap, methodDefinition);
            }

            private SyntaxToken AnnotationResolver(
                SyntaxNode node,
                TriviaLocation location,
                SyntaxAnnotation annotation,
                SyntaxNode callsite,
                MethodDeclarationSyntax method)
            {
                var token = node.GetAnnotatedNodesAndTokens(annotation).FirstOrDefault().AsToken();
                if (token.RawKind != 0)
                {
                    return token;
                }

                return location switch
                {
                    TriviaLocation.BeforeBeginningOfSpan => callsite.GetFirstToken(includeZeroWidth: true).GetPreviousToken(includeZeroWidth: true),
                    TriviaLocation.AfterEndOfSpan => callsite.GetLastToken(includeZeroWidth: true).GetNextToken(includeZeroWidth: true),
                    TriviaLocation.AfterBeginningOfSpan => method.Body != null
                        ? method.Body.OpenBraceToken.GetNextToken(includeZeroWidth: true)
                        : method.ExpressionBody.ArrowToken.GetNextToken(includeZeroWidth: true),
                    TriviaLocation.BeforeEndOfSpan => method.Body != null
                        ? method.Body.CloseBraceToken.GetPreviousToken(includeZeroWidth: true)
                        : method.SemicolonToken,
                    _ => Contract.FailWithReturn<SyntaxToken>("can't happen"),
                };
            }

            private IEnumerable<SyntaxTrivia> TriviaResolver(
                TriviaLocation location,
                PreviousNextTokenPair tokenPair,
                Dictionary<SyntaxToken, LeadingTrailingTriviaPair> triviaMap,
                MethodDeclarationSyntax method)
            {
                // Resolve trivia at the edge of the selection. simple case is easy to deal with, but complex cases where
                // elastic trivia and user trivia are mixed (hybrid case) and we want to preserve some part of user coding style
                // but not others can be dealt with here.

                // method has no statement in them. so basically two trivia list now pointing to same thing. "{" and "}"
                if (method.Body != null)
                {
                    if (tokenPair.PreviousToken == method.Body.OpenBraceToken &&
                        tokenPair.NextToken == method.Body.CloseBraceToken)
                    {
                        return (location == TriviaLocation.AfterBeginningOfSpan)
                            ? SpecializedCollections.SingletonEnumerable(SyntaxFactory.ElasticMarker)
                            : SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();
                    }
                }
                else
                {
                    if (tokenPair.PreviousToken == method.ExpressionBody.ArrowToken &&
                        tokenPair.NextToken.GetPreviousToken() == method.SemicolonToken)
                    {
                        return (location == TriviaLocation.AfterBeginningOfSpan)
                            ? SpecializedCollections.SingletonEnumerable(SyntaxFactory.ElasticMarker)
                            : SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();
                    }
                }

                var previousTriviaPair = triviaMap.ContainsKey(tokenPair.PreviousToken) ? triviaMap[tokenPair.PreviousToken] : default;
                var nextTriviaPair = triviaMap.ContainsKey(tokenPair.NextToken) ? triviaMap[tokenPair.NextToken] : default;

                var trailingTrivia = previousTriviaPair.TrailingTrivia ?? SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();
                var leadingTrivia = nextTriviaPair.LeadingTrivia ?? SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();

                var list = trailingTrivia.Concat(leadingTrivia);

                return location switch
                {
                    TriviaLocation.BeforeBeginningOfSpan => FilterBeforeBeginningOfSpan(tokenPair, list),
                    TriviaLocation.AfterEndOfSpan => FilterTriviaList(list.Concat(tokenPair.NextToken.LeadingTrivia)),
                    TriviaLocation.AfterBeginningOfSpan => FilterTriviaList(AppendTrailingTrivia(tokenPair).Concat(list).Concat(tokenPair.NextToken.LeadingTrivia)),
                    TriviaLocation.BeforeEndOfSpan => FilterTriviaList(tokenPair.PreviousToken.TrailingTrivia.Concat(list).Concat(tokenPair.NextToken.LeadingTrivia)),
                    _ => Contract.FailWithReturn<IEnumerable<SyntaxTrivia>>("Shouldn't reach here"),
                };
            }

            private IEnumerable<SyntaxTrivia> FilterBeforeBeginningOfSpan(PreviousNextTokenPair tokenPair, IEnumerable<SyntaxTrivia> list)
            {
                var allList = FilterTriviaList(tokenPair.PreviousToken.TrailingTrivia.Concat(list).Concat(AppendLeadingTrivia(tokenPair)));

                if (tokenPair.PreviousToken.RawKind == (int)SyntaxKind.OpenBraceToken)
                {
                    return RemoveBlankLines(allList);
                }

                return allList;
            }

            private IEnumerable<SyntaxTrivia> AppendLeadingTrivia(PreviousNextTokenPair tokenPair)
            {
                if (tokenPair.PreviousToken.RawKind == (int)SyntaxKind.OpenBraceToken ||
                    tokenPair.PreviousToken.RawKind == (int)SyntaxKind.SemicolonToken)
                {
                    return tokenPair.NextToken.LeadingTrivia;
                }

                return SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();
            }

            private IEnumerable<SyntaxTrivia> AppendTrailingTrivia(PreviousNextTokenPair tokenPair)
            {
                if (tokenPair.PreviousToken.RawKind == (int)SyntaxKind.OpenBraceToken ||
                    tokenPair.PreviousToken.RawKind == (int)SyntaxKind.SemicolonToken)
                {
                    return tokenPair.PreviousToken.TrailingTrivia;
                }

                return SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();
            }
        }
    }
}
