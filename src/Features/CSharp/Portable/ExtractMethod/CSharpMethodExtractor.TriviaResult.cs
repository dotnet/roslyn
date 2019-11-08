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
                if (callsite == null)
                {
                    return null;
                }

                if (method is MethodDeclarationSyntax methodDefinition)
                {
                    return (node, location, annotation) => AnnotationResolver(node, location, annotation, callsite, methodDefinition.Body, methodDefinition.ExpressionBody, methodDefinition.SemicolonToken);
                }
                else if (method is LocalFunctionStatementSyntax localMethodDefinition)
                {
                    return (node, location, annotation) => AnnotationResolver(node, location, annotation, callsite, localMethodDefinition.Body, localMethodDefinition.ExpressionBody, localMethodDefinition.SemicolonToken);
                }
                else
                {
                    return null;
                }
            }

            protected override TriviaResolver GetTriviaResolver(SyntaxNode method)
            {
                if (method is MethodDeclarationSyntax methodDefinition)
                {
                    return (location, tokenPair, triviaMap) => TriviaResolver(location, tokenPair, triviaMap, methodDefinition.Body, methodDefinition.ExpressionBody, methodDefinition.SemicolonToken);
                }
                else if (method is LocalFunctionStatementSyntax localMethodDefinition)
                {
                    return (location, tokenPair, triviaMap) => TriviaResolver(location, tokenPair, triviaMap, localMethodDefinition.Body, localMethodDefinition.ExpressionBody, localMethodDefinition.SemicolonToken);
                }
                else
                {
                    return null;
                }
            }

            private SyntaxToken AnnotationResolver(
                SyntaxNode node,
                TriviaLocation location,
                SyntaxAnnotation annotation,
                SyntaxNode callsite,
                BlockSyntax methodBody,
                ArrowExpressionClauseSyntax expressionBody,
                SyntaxToken semicolonToken)
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
                    TriviaLocation.AfterBeginningOfSpan => methodBody != null
                        ? methodBody.OpenBraceToken.GetNextToken(includeZeroWidth: true)
                        : expressionBody.ArrowToken.GetNextToken(includeZeroWidth: true),
                    TriviaLocation.BeforeEndOfSpan => methodBody != null
                        ? methodBody.CloseBraceToken.GetPreviousToken(includeZeroWidth: true)
                        : semicolonToken,
                    _ => Contract.FailWithReturn<SyntaxToken>("can't happen"),
                };
            }

            private IEnumerable<SyntaxTrivia> TriviaResolver(
                TriviaLocation location,
                PreviousNextTokenPair tokenPair,
                Dictionary<SyntaxToken, LeadingTrailingTriviaPair> triviaMap,
                BlockSyntax methodBody,
                ArrowExpressionClauseSyntax methodExpressionBody,
                SyntaxToken methodSemicolonToken)
            {
                // Resolve trivia at the edge of the selection. simple case is easy to deal with, but complex cases where
                // elastic trivia and user trivia are mixed (hybrid case) and we want to preserve some part of user coding style
                // but not others can be dealt with here.

                // method has no statement in them. so basically two trivia list now pointing to same thing. "{" and "}"
                if (methodBody != null)
                {
                    if (tokenPair.PreviousToken == methodBody.OpenBraceToken &&
                        tokenPair.NextToken == methodBody.CloseBraceToken)
                    {
                        return (location == TriviaLocation.AfterBeginningOfSpan)
                            ? SpecializedCollections.SingletonEnumerable(SyntaxFactory.ElasticMarker)
                            : SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();
                    }
                }
                else
                {
                    if (tokenPair.PreviousToken == methodExpressionBody.ArrowToken &&
                        tokenPair.NextToken.GetPreviousToken() == methodSemicolonToken)
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
