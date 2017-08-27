﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class AbstractSyntaxTriviaService
    {
        private class Result : ITriviaSavedResult
        {
            private static readonly AnnotationResolver s_defaultAnnotationResolver = ResolveAnnotation;
            private static readonly TriviaResolver s_defaultTriviaResolver = ResolveTrivia;

            private readonly SyntaxNode _root;
            private readonly int _endOfLineKind;

            private readonly Dictionary<TriviaLocation, SyntaxAnnotation> _annotations;
            private readonly Dictionary<TriviaLocation, IEnumerable<SyntaxTrivia>> _triviaList;

            public Result(
                SyntaxNode root,
                int endOfLineKind,
                Dictionary<TriviaLocation, SyntaxAnnotation> annotations,
                Dictionary<TriviaLocation, IEnumerable<SyntaxTrivia>> triviaList)
            {
                Contract.ThrowIfNull(root);
                Contract.ThrowIfNull(annotations);
                Contract.ThrowIfNull(triviaList);

                _root = root;
                _endOfLineKind = endOfLineKind;

                _annotations = annotations;
                _triviaList = triviaList;
            }

            public SyntaxNode Root => _root;

            public SyntaxNode RestoreTrivia(
                SyntaxNode root,
                AnnotationResolver annotationResolver = null,
                TriviaResolver triviaResolver = null)
            {
                var tokens = RecoverTokensAtEdges(root, annotationResolver);
                var map = CreateOldToNewTokensMap(tokens, triviaResolver);

                return root.ReplaceTokens(map.Keys, (o, n) => map[o]);
            }

            private Dictionary<SyntaxToken, SyntaxToken> CreateOldToNewTokensMap(
                Dictionary<TriviaLocation, PreviousNextTokenPair> tokenPairs,
                Dictionary<TriviaLocation, LeadingTrailingTriviaPair> triviaPairs)
            {
                var map = new Dictionary<SyntaxToken, SyntaxToken>();
                foreach (var pair in CreateUniqueTokenTriviaPairs(tokenPairs, triviaPairs))
                {
                    var localCopy = pair;
                    var previousToken = map.GetOrAdd(localCopy.Item1.PreviousToken, _ => localCopy.Item1.PreviousToken);
                    map[localCopy.Item1.PreviousToken] = previousToken.WithTrailingTrivia(localCopy.Item2.TrailingTrivia);

                    var nextToken = map.GetOrAdd(localCopy.Item1.NextToken, _ => localCopy.Item1.NextToken);
                    map[localCopy.Item1.NextToken] = nextToken.WithLeadingTrivia(localCopy.Item2.LeadingTrivia);
                }

                return map;
            }

            private LeadingTrailingTriviaPair GetTrailingAndLeadingTrivia(TriviaLocation locationKind, PreviousNextTokenPair tokenPair, IEnumerable<SyntaxTrivia> trivia)
            {
                var list = trivia.ToList();

                // there are some noisy trivia
                var index = GetFirstEndOfLineIndex(list);

                return new LeadingTrailingTriviaPair
                {
                    TrailingTrivia = CreateTriviaListFromTo(list, 0, index),
                    LeadingTrivia = CreateTriviaListFromTo(list, index + 1, list.Count - 1)
                };
            }

            private int GetFirstEndOfLineIndex(List<SyntaxTrivia> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].RawKind == _endOfLineKind)
                    {
                        return i;
                    }
                }

                return list.Count - 1;
            }

            private Dictionary<TriviaLocation, SyntaxToken> RecoverTokensAtEdges(
                SyntaxNode root,
                AnnotationResolver annotationResolver)
            {
                var resolver = annotationResolver ?? s_defaultAnnotationResolver;

                var tokens = Enumerable.Range((int)TriviaLocation.BeforeBeginningOfSpan, TriviaLocationsCount)
                                       .Cast<TriviaLocation>()
                                       .ToDictionary(
                                            location => location,
                                            location => resolver(root, location, _annotations[location]));

                // check variable assumption. ordering of two pairs can't be changed
                Contract.ThrowIfFalse(
                    (tokens[TriviaLocation.BeforeBeginningOfSpan].RawKind == 0 /* && don't care */) ||
                    (/* don't care && */ tokens[TriviaLocation.AfterEndOfSpan].RawKind == 0) ||
                    (tokens[TriviaLocation.BeforeBeginningOfSpan].Span.End <= tokens[TriviaLocation.AfterEndOfSpan].SpanStart));

                Contract.ThrowIfFalse(
                    (tokens[TriviaLocation.AfterBeginningOfSpan].RawKind == 0 /* && don't care */) ||
                    (/* don't care && */ tokens[TriviaLocation.BeforeEndOfSpan].RawKind == 0) ||
                    (tokens[TriviaLocation.AfterBeginningOfSpan] == tokens[TriviaLocation.BeforeEndOfSpan]) ||
                    (tokens[TriviaLocation.AfterBeginningOfSpan].GetPreviousToken(includeZeroWidth: true) == tokens[TriviaLocation.BeforeEndOfSpan]) ||
                    (tokens[TriviaLocation.AfterBeginningOfSpan].Span.End <= tokens[TriviaLocation.BeforeEndOfSpan].SpanStart));

                return tokens;
            }

            private Dictionary<SyntaxToken, SyntaxToken> CreateOldToNewTokensMap(
                Dictionary<TriviaLocation, SyntaxToken> tokens,
                TriviaResolver triviaResolver)
            {
                var tokenPairs = CreatePreviousNextTokenPairs(tokens);
                var tokenToLeadingTrailingTriviaMap = CreateTokenLeadingTrailingTriviaMap(tokens);

                var resolver = triviaResolver ?? s_defaultTriviaResolver;

                var triviaPairs = Enumerable.Range((int)TriviaLocation.BeforeBeginningOfSpan, TriviaLocationsCount)
                                            .Cast<TriviaLocation>()
                                            .ToDictionary(
                                                location => location,
                                                location => CreateTriviaPairs(
                                                                location,
                                                                tokenPairs[location],
                                                                resolver(location, tokenPairs[location], tokenToLeadingTrailingTriviaMap)));

                return CreateOldToNewTokensMap(tokenPairs, triviaPairs);
            }

            private LeadingTrailingTriviaPair CreateTriviaPairs(
                TriviaLocation locationKind,
                PreviousNextTokenPair tokenPair,
                IEnumerable<SyntaxTrivia> trivia)
            {
                // beginning of the tree
                if (tokenPair.PreviousToken.RawKind == 0)
                {
                    return new LeadingTrailingTriviaPair { TrailingTrivia = SpecializedCollections.EmptyEnumerable<SyntaxTrivia>(), LeadingTrivia = trivia };
                }

                return GetTrailingAndLeadingTrivia(locationKind, tokenPair, trivia);
            }

            private IEnumerable<Tuple<PreviousNextTokenPair, LeadingTrailingTriviaPair>> CreateUniqueTokenTriviaPairs(
                Dictionary<TriviaLocation, PreviousNextTokenPair> tokenPairs,
                Dictionary<TriviaLocation, LeadingTrailingTriviaPair> triviaPairs)
            {
                // if there are dup, duplicated one will be ignored.
                var set = new HashSet<PreviousNextTokenPair>();
                for (int i = (int)TriviaLocation.BeforeBeginningOfSpan; i <= (int)TriviaLocation.AfterEndOfSpan; i++)
                {
                    var location = (TriviaLocation)i;
                    var key = tokenPairs[location];
                    if (set.Contains(key))
                    {
                        continue;
                    }

                    yield return Tuple.Create(key, triviaPairs[location]);
                    set.Add(key);
                }
            }

            private Dictionary<SyntaxToken, LeadingTrailingTriviaPair> CreateTokenLeadingTrailingTriviaMap(
                Dictionary<TriviaLocation, SyntaxToken> tokens)
            {
                var tuple = default(LeadingTrailingTriviaPair);
                var map = new Dictionary<SyntaxToken, LeadingTrailingTriviaPair>();

                tuple = map.GetOrAdd(tokens[TriviaLocation.BeforeBeginningOfSpan], _ => default);
                map[tokens[TriviaLocation.BeforeBeginningOfSpan]] = new LeadingTrailingTriviaPair
                {
                    LeadingTrivia = tuple.LeadingTrivia,
                    TrailingTrivia = _triviaList[TriviaLocation.BeforeBeginningOfSpan]
                };

                tuple = map.GetOrAdd(tokens[TriviaLocation.AfterBeginningOfSpan], _ => default);
                map[tokens[TriviaLocation.AfterBeginningOfSpan]] = new LeadingTrailingTriviaPair
                {
                    LeadingTrivia = _triviaList[TriviaLocation.AfterBeginningOfSpan],
                    TrailingTrivia = tuple.TrailingTrivia
                };

                tuple = map.GetOrAdd(tokens[TriviaLocation.BeforeEndOfSpan], _ => default);
                map[tokens[TriviaLocation.BeforeEndOfSpan]] = new LeadingTrailingTriviaPair
                {
                    LeadingTrivia = tuple.LeadingTrivia,
                    TrailingTrivia = _triviaList[TriviaLocation.BeforeEndOfSpan]
                };

                tuple = map.GetOrAdd(tokens[TriviaLocation.AfterEndOfSpan], _ => default);
                map[tokens[TriviaLocation.AfterEndOfSpan]] = new LeadingTrailingTriviaPair
                {
                    LeadingTrivia = _triviaList[TriviaLocation.AfterEndOfSpan],
                    TrailingTrivia = tuple.TrailingTrivia
                };

                return map;
            }

            private Dictionary<TriviaLocation, PreviousNextTokenPair> CreatePreviousNextTokenPairs(
                Dictionary<TriviaLocation, SyntaxToken> tokens)
            {
                var tokenPairs = new Dictionary<TriviaLocation, PreviousNextTokenPair>();

                tokenPairs[TriviaLocation.BeforeBeginningOfSpan] = new PreviousNextTokenPair
                {
                    PreviousToken = tokens[TriviaLocation.BeforeBeginningOfSpan],
                    NextToken = tokens[TriviaLocation.BeforeBeginningOfSpan].GetNextToken(includeZeroWidth: true)
                };

                tokenPairs[TriviaLocation.AfterBeginningOfSpan] = new PreviousNextTokenPair
                {
                    PreviousToken = tokens[TriviaLocation.AfterBeginningOfSpan].GetPreviousToken(includeZeroWidth: true),
                    NextToken = tokens[TriviaLocation.AfterBeginningOfSpan]
                };

                tokenPairs[TriviaLocation.BeforeEndOfSpan] = new PreviousNextTokenPair
                {
                    PreviousToken = tokens[TriviaLocation.BeforeEndOfSpan],
                    NextToken = tokens[TriviaLocation.BeforeEndOfSpan].GetNextToken(includeZeroWidth: true)
                };

                tokenPairs[TriviaLocation.AfterEndOfSpan] = new PreviousNextTokenPair
                {
                    PreviousToken = tokens[TriviaLocation.AfterEndOfSpan].GetPreviousToken(includeZeroWidth: true),
                    NextToken = tokens[TriviaLocation.AfterEndOfSpan]
                };

                return tokenPairs;
            }

            private IEnumerable<SyntaxTrivia> CreateTriviaListFromTo(
                List<SyntaxTrivia> list,
                int startIndex,
                int endIndex)
            {
                if (startIndex > endIndex)
                {
                    yield break;
                }

                for (int i = startIndex; i <= endIndex; i++)
                {
                    yield return list[i];
                }
            }

            private static SyntaxToken ResolveAnnotation(
                SyntaxNode root,
                TriviaLocation location,
                SyntaxAnnotation annotation)
            {
                return root.GetAnnotatedNodesAndTokens(annotation).FirstOrDefault().AsToken();
            }

            private static IEnumerable<SyntaxTrivia> ResolveTrivia(
                TriviaLocation location,
                PreviousNextTokenPair tokenPair,
                Dictionary<SyntaxToken, LeadingTrailingTriviaPair> triviaMap)
            {
                var previousTriviaPair = triviaMap.ContainsKey(tokenPair.PreviousToken) ? triviaMap[tokenPair.PreviousToken] : default;
                var nextTriviaPair = triviaMap.ContainsKey(tokenPair.NextToken) ? triviaMap[tokenPair.NextToken] : default;

                var trailingTrivia = previousTriviaPair.TrailingTrivia ?? SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();
                var leadingTrivia = nextTriviaPair.LeadingTrivia ?? SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();

                return tokenPair.PreviousToken.TrailingTrivia.Concat(trailingTrivia).Concat(leadingTrivia).Concat(tokenPair.NextToken.LeadingTrivia);
            }
        }
    }
}
