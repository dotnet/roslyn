// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting;

internal abstract class AbstractSyntaxFormatting : ISyntaxFormatting
{
    private static readonly Func<TextSpan, bool> s_notEmpty = s => !s.IsEmpty;

    public abstract SyntaxFormattingOptions DefaultOptions { get; }
    public abstract SyntaxFormattingOptions GetFormattingOptions(IOptionsReader options);

    public abstract ImmutableArray<AbstractFormattingRule> GetDefaultFormattingRules();

    protected abstract IFormattingResult CreateAggregatedFormattingResult(SyntaxNode node, IList<AbstractFormattingResult> results, TextSpanMutableIntervalTree? formattingSpans = null);

    protected abstract AbstractFormattingResult Format(SyntaxNode node, SyntaxFormattingOptions options, ImmutableArray<AbstractFormattingRule> rules, SyntaxToken startToken, SyntaxToken endToken, CancellationToken cancellationToken);

    public IFormattingResult GetFormattingResult(SyntaxNode node, IEnumerable<TextSpan>? spans, SyntaxFormattingOptions options, ImmutableArray<AbstractFormattingRule> rules, CancellationToken cancellationToken)
    {
        IReadOnlyList<TextSpan> spansToFormat;

        if (spans == null)
        {
            spansToFormat = node.FullSpan.IsEmpty ? [] : [node.FullSpan];
        }
        else
        {
            spansToFormat = new NormalizedTextSpanCollection(spans.Where(s_notEmpty));
        }

        if (spansToFormat.Count == 0)
        {
            return CreateAggregatedFormattingResult(node, results: []);
        }

        if (rules.IsDefault)
            rules = GetDefaultFormattingRules();

        List<AbstractFormattingResult>? results = null;
        foreach (var (startToken, endToken) in node.ConvertToTokenPairs(spansToFormat))
        {
            if (node.IsInvalidTokenRange(startToken, endToken))
            {
                continue;
            }

            results ??= [];
            results.Add(Format(node, options, rules, startToken, endToken, cancellationToken));
        }

        // quick simple case check
        if (results == null)
        {
            return CreateAggregatedFormattingResult(node, results: []);
        }

        if (results.Count == 1)
        {
            return results[0];
        }

        // more expensive case
        return CreateAggregatedFormattingResult(node, results);
    }
}
