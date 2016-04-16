// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class AbstractSyntaxTriviaService : ISyntaxTriviaService
    {
        private const int TriviaLocationsCount = 4;

        private readonly ISyntaxFactsService _syntaxFactsService;
        private readonly int _endOfLineKind;

        protected AbstractSyntaxTriviaService(ISyntaxFactsService syntaxFactsService, int endOfLineKind)
        {
            _syntaxFactsService = syntaxFactsService;
            _endOfLineKind = endOfLineKind;
        }

        public ITriviaSavedResult SaveTriviaAroundSelection(SyntaxNode root, TextSpan textSpan)
        {
            Contract.ThrowIfNull(root);
            Contract.ThrowIfTrue(textSpan.IsEmpty);
            Contract.Requires(Enum.GetNames(typeof(TriviaLocation)).Length == TriviaLocationsCount);

            var tokens = GetTokensAtEdges(root, textSpan);

            // span must contain after and before spans at the both edges
            Contract.ThrowIfFalse(textSpan.Contains(tokens[TriviaLocation.AfterBeginningOfSpan].Span) && textSpan.Contains(tokens[TriviaLocation.BeforeEndOfSpan].Span));

            var triviaList = GetTriviaAtEdges(tokens, textSpan);

            var annotations = Enumerable.Range((int)TriviaLocation.BeforeBeginningOfSpan, TriviaLocationsCount)
                                        .Cast<TriviaLocation>()
                                        .ToDictionary(location => location, _ => new SyntaxAnnotation());

            var map = CreateOldToNewTokensMap(tokens, annotations);
            var rootWithAnnotation = ReplaceTokens(root, map.Keys, (o, n) => map[o]);

            return CreateResult(rootWithAnnotation, annotations, triviaList);
        }

        private SyntaxNode ReplaceTokens(
            SyntaxNode root,
            IEnumerable<SyntaxToken> oldTokens,
            Func<SyntaxToken, SyntaxToken, SyntaxToken> computeReplacementToken)
        {
            Contract.ThrowIfNull(root);
            Contract.ThrowIfNull(oldTokens);
            Contract.ThrowIfNull(computeReplacementToken);

            return root.ReplaceTokens(oldTokens, (o, n) => computeReplacementToken(o, n));
        }

        private ITriviaSavedResult CreateResult(
            SyntaxNode root,
            Dictionary<TriviaLocation, SyntaxAnnotation> annotations,
            Dictionary<TriviaLocation, IEnumerable<SyntaxTrivia>> triviaList)
        {
            return new Result(root, _endOfLineKind, annotations, triviaList);
        }

        private Dictionary<SyntaxToken, SyntaxToken> CreateOldToNewTokensMap(
            Dictionary<TriviaLocation, SyntaxToken> tokens,
            Dictionary<TriviaLocation, SyntaxAnnotation> annotations)
        {
            var token = default(SyntaxToken);
            var map = new Dictionary<SyntaxToken, SyntaxToken>();
            var emptyList = SpecializedCollections.EmptyEnumerable<SyntaxTrivia>();

            token = map.GetOrAdd(tokens[TriviaLocation.BeforeBeginningOfSpan], _ => tokens[TriviaLocation.BeforeBeginningOfSpan]);
            map[tokens[TriviaLocation.BeforeBeginningOfSpan]] = token.WithTrailingTrivia(emptyList).WithAdditionalAnnotations(annotations[TriviaLocation.BeforeBeginningOfSpan]);

            token = map.GetOrAdd(tokens[TriviaLocation.AfterBeginningOfSpan], _ => tokens[TriviaLocation.AfterBeginningOfSpan]);
            map[tokens[TriviaLocation.AfterBeginningOfSpan]] = token.WithLeadingTrivia(emptyList).WithAdditionalAnnotations(annotations[TriviaLocation.AfterBeginningOfSpan]);

            token = map.GetOrAdd(tokens[TriviaLocation.BeforeEndOfSpan], _ => tokens[TriviaLocation.BeforeEndOfSpan]);
            map[tokens[TriviaLocation.BeforeEndOfSpan]] = token.WithTrailingTrivia(emptyList).WithAdditionalAnnotations(annotations[TriviaLocation.BeforeEndOfSpan]);

            token = map.GetOrAdd(tokens[TriviaLocation.AfterEndOfSpan], _ => tokens[TriviaLocation.AfterEndOfSpan]);
            map[tokens[TriviaLocation.AfterEndOfSpan]] = token.WithLeadingTrivia(emptyList).WithAdditionalAnnotations(annotations[TriviaLocation.AfterEndOfSpan]);

            return map;
        }

        private Dictionary<TriviaLocation, IEnumerable<SyntaxTrivia>> GetTriviaAtEdges(Dictionary<TriviaLocation, SyntaxToken> tokens, TextSpan textSpan)
        {
            var triviaAtBeginning = SplitTrivia(tokens[TriviaLocation.BeforeBeginningOfSpan], tokens[TriviaLocation.AfterBeginningOfSpan], t => t.FullSpan.End <= textSpan.Start);
            var triviaAtEnd = SplitTrivia(tokens[TriviaLocation.BeforeEndOfSpan], tokens[TriviaLocation.AfterEndOfSpan], t => t.FullSpan.Start < textSpan.End);

            var triviaList = new Dictionary<TriviaLocation, IEnumerable<SyntaxTrivia>>();
            triviaList[TriviaLocation.BeforeBeginningOfSpan] = triviaAtBeginning.Item1;
            triviaList[TriviaLocation.AfterBeginningOfSpan] = triviaAtBeginning.Item2;

            triviaList[TriviaLocation.BeforeEndOfSpan] = triviaAtEnd.Item1;
            triviaList[TriviaLocation.AfterEndOfSpan] = triviaAtEnd.Item2;
            return triviaList;
        }

        private Dictionary<TriviaLocation, SyntaxToken> GetTokensAtEdges(SyntaxNode root, TextSpan textSpan)
        {
            var tokens = new Dictionary<TriviaLocation, SyntaxToken>();
            tokens[TriviaLocation.AfterBeginningOfSpan] = _syntaxFactsService.FindTokenOnRightOfPosition(root, textSpan.Start, includeSkipped: false);
            tokens[TriviaLocation.BeforeBeginningOfSpan] = tokens[TriviaLocation.AfterBeginningOfSpan].GetPreviousToken(includeZeroWidth: true);
            tokens[TriviaLocation.BeforeEndOfSpan] = _syntaxFactsService.FindTokenOnLeftOfPosition(root, textSpan.End, includeSkipped: false);
            tokens[TriviaLocation.AfterEndOfSpan] = tokens[TriviaLocation.BeforeEndOfSpan].GetNextToken(includeZeroWidth: true);
            return tokens;
        }

        private static Tuple<List<SyntaxTrivia>, List<SyntaxTrivia>> SplitTrivia(
                SyntaxToken token1,
                SyntaxToken token2,
                Func<SyntaxTrivia, bool> conditionToLeftAtCallSite)
        {
            var triviaLeftAtCallSite = new List<SyntaxTrivia>();
            var triviaMovedToDefinition = new List<SyntaxTrivia>();

            foreach (var trivia in token1.TrailingTrivia.Concat(token2.LeadingTrivia))
            {
                if (conditionToLeftAtCallSite(trivia))
                {
                    triviaLeftAtCallSite.Add(trivia);
                }
                else
                {
                    triviaMovedToDefinition.Add(trivia);
                }
            }

            return Tuple.Create(triviaLeftAtCallSite, triviaMovedToDefinition);
        }
    }
}
