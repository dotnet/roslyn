// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
                var preservationService = selectionResult.SemanticDocument.Document.Project.Services.GetService<ISyntaxTriviaService>();
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
                var isMethodOrLocalFunction = method is MethodDeclarationSyntax or LocalFunctionStatementSyntax;
                if (callsite == null || !isMethodOrLocalFunction)
                {
                    return null;
                }

                return (node, location, annotation) => AnnotationResolver(node, location, annotation, callsite, method);
            }

            protected override TriviaResolver GetTriviaResolver(SyntaxNode method)
            {
                var isMethodOrLocalFunction = method is MethodDeclarationSyntax or LocalFunctionStatementSyntax;
                if (!isMethodOrLocalFunction)
                {
                    return null;
                }

                return (location, tokenPair, triviaMap) => TriviaResolver(location, tokenPair, triviaMap, method);
            }

            private static SyntaxToken AnnotationResolver(
                SyntaxNode node,
                TriviaLocation location,
                SyntaxAnnotation annotation,
                SyntaxNode callsite,
                SyntaxNode method)
            {
                var token = node.GetAnnotatedNodesAndTokens(annotation).FirstOrDefault().AsToken();
                if (token.RawKind != 0)
                {
                    return token;
                }

                var (body, expressionBody, semicolonToken) = GetResolverElements(method);
                return location switch
                {
                    TriviaLocation.BeforeBeginningOfSpan => callsite.GetFirstToken(includeZeroWidth: true).GetPreviousToken(includeZeroWidth: true),
                    TriviaLocation.AfterEndOfSpan => callsite.GetLastToken(includeZeroWidth: true).GetNextToken(includeZeroWidth: true),
                    TriviaLocation.AfterBeginningOfSpan => body != null
                        ? body.OpenBraceToken.GetNextToken(includeZeroWidth: true)
                        : expressionBody.ArrowToken.GetNextToken(includeZeroWidth: true),
                    TriviaLocation.BeforeEndOfSpan => body != null
                        ? body.CloseBraceToken.GetPreviousToken(includeZeroWidth: true)
                        : semicolonToken,
                    _ => throw ExceptionUtilities.UnexpectedValue(location)
                };
            }

            private IEnumerable<SyntaxTrivia> TriviaResolver(
                TriviaLocation location,
                PreviousNextTokenPair tokenPair,
                Dictionary<SyntaxToken, LeadingTrailingTriviaPair> triviaMap,
                SyntaxNode method)
            {
                // Resolve trivia at the edge of the selection. simple case is easy to deal with, but complex cases where
                // elastic trivia and user trivia are mixed (hybrid case) and we want to preserve some part of user coding style
                // but not others can be dealt with here.

                var (body, expressionBody, semicolonToken) = GetResolverElements(method);

                // method has no statement in them. so basically two trivia list now pointing to same thing. "{" and "}"
                if (body != null)
                {
                    if (tokenPair.PreviousToken == body.OpenBraceToken &&
                        tokenPair.NextToken == body.CloseBraceToken)
                    {
                        return (location == TriviaLocation.AfterBeginningOfSpan)
                            ? SpecializedCollections.SingletonEnumerable(SyntaxFactory.ElasticMarker)
                            : SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();
                    }
                }
                else
                {
                    if (tokenPair.PreviousToken == expressionBody.ArrowToken &&
                        tokenPair.NextToken.GetPreviousToken() == semicolonToken)
                    {
                        return (location == TriviaLocation.AfterBeginningOfSpan)
                            ? SpecializedCollections.SingletonEnumerable(SyntaxFactory.ElasticMarker)
                            : SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();
                    }
                }

                triviaMap.TryGetValue(tokenPair.PreviousToken, out var previousTriviaPair);
                var trailingTrivia = previousTriviaPair.TrailingTrivia ?? SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();

                triviaMap.TryGetValue(tokenPair.NextToken, out var nextTriviaPair);
                var leadingTrivia = nextTriviaPair.LeadingTrivia ?? SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();

                var list = trailingTrivia.Concat(leadingTrivia);

                return location switch
                {
                    TriviaLocation.BeforeBeginningOfSpan => FilterBeforeBeginningOfSpan(tokenPair, list),
                    TriviaLocation.AfterEndOfSpan => FilterTriviaList(list.Concat(tokenPair.NextToken.LeadingTrivia)),
                    TriviaLocation.AfterBeginningOfSpan => FilterTriviaList(AppendTrailingTrivia(tokenPair).Concat(list).Concat(tokenPair.NextToken.LeadingTrivia)),
                    TriviaLocation.BeforeEndOfSpan => FilterTriviaList(tokenPair.PreviousToken.TrailingTrivia.Concat(list).Concat(tokenPair.NextToken.LeadingTrivia)),
                    _ => throw ExceptionUtilities.UnexpectedValue(location),
                };
            }

            private static (BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, SyntaxToken semicolonToken) GetResolverElements(SyntaxNode method)
            {
                return method switch
                {
                    MethodDeclarationSyntax methodDeclaration => (methodDeclaration.Body, methodDeclaration.ExpressionBody, methodDeclaration.SemicolonToken),
                    LocalFunctionStatementSyntax localFunctionDeclaration => (localFunctionDeclaration.Body, localFunctionDeclaration.ExpressionBody, localFunctionDeclaration.SemicolonToken),
                    _ => throw ExceptionUtilities.UnexpectedValue(method)
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

            private static IEnumerable<SyntaxTrivia> AppendLeadingTrivia(PreviousNextTokenPair tokenPair)
            {
                if (tokenPair.PreviousToken.RawKind is ((int)SyntaxKind.OpenBraceToken) or
                    ((int)SyntaxKind.SemicolonToken))
                {
                    return tokenPair.NextToken.LeadingTrivia;
                }

                return SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();
            }

            private static IEnumerable<SyntaxTrivia> AppendTrailingTrivia(PreviousNextTokenPair tokenPair)
            {
                if (tokenPair.PreviousToken.RawKind is ((int)SyntaxKind.OpenBraceToken) or
                    ((int)SyntaxKind.SemicolonToken))
                {
                    return tokenPair.PreviousToken.TrailingTrivia;
                }

                return SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();
            }
        }
    }
}
