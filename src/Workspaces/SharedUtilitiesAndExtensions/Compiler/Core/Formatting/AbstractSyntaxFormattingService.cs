// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract class AbstractSyntaxFormattingService : ISyntaxFormattingService
    {
        private static readonly Func<TextSpan, bool> s_notEmpty = s => !s.IsEmpty;

        protected AbstractSyntaxFormattingService()
        {
        }

        public abstract SyntaxFormattingOptions GetFormattingOptions(AnalyzerConfigOptions options);

        public abstract IEnumerable<AbstractFormattingRule> GetDefaultFormattingRules();

        protected abstract IFormattingResult CreateAggregatedFormattingResult(SyntaxNode node, IList<AbstractFormattingResult> results, SimpleIntervalTree<TextSpan, TextSpanIntervalIntrospector>? formattingSpans = null);

        protected abstract AbstractFormattingResult Format(SyntaxNode node, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule> rules, SyntaxToken startToken, SyntaxToken endToken, CancellationToken cancellationToken);

        public IFormattingResult GetFormattingResult(SyntaxNode node, IEnumerable<TextSpan>? spans, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken)
        {
            IReadOnlyList<TextSpan> spansToFormat;

            if (spans == null)
            {
                spansToFormat = node.FullSpan.IsEmpty ?
                    SpecializedCollections.EmptyReadOnlyList<TextSpan>() :
                    SpecializedCollections.SingletonReadOnlyList(node.FullSpan);
            }
            else
            {
                spansToFormat = new NormalizedTextSpanCollection(spans.Where(s_notEmpty));
            }

            if (spansToFormat.Count == 0)
            {
                return CreateAggregatedFormattingResult(node, SpecializedCollections.EmptyList<AbstractFormattingResult>());
            }

            rules ??= GetDefaultFormattingRules();

            List<AbstractFormattingResult>? results = null;
            foreach (var (startToken, endToken) in node.ConvertToTokenPairs(spansToFormat))
            {
                if (node.IsInvalidTokenRange(startToken, endToken))
                {
                    continue;
                }

                results ??= new List<AbstractFormattingResult>();
                results.Add(Format(node, options, rules, startToken, endToken, cancellationToken));
            }

            // quick simple case check
            if (results == null)
            {
                return CreateAggregatedFormattingResult(node, SpecializedCollections.EmptyList<AbstractFormattingResult>());
            }

            if (results.Count == 1)
            {
                return results[0];
            }

            // more expensive case
            return CreateAggregatedFormattingResult(node, results);
        }
    }
}
