// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal class CSharpSyntaxFormatting : AbstractSyntaxFormatting
{
    public static readonly CSharpSyntaxFormatting Instance = new();

    private readonly ImmutableArray<AbstractFormattingRule> _rules =
    [
        new WrappingFormattingRule(),
        new SpacingFormattingRule(),
        new NewLineUserSettingFormattingRule(),
        new IndentUserSettingsFormattingRule(),
        new ElasticTriviaFormattingRule(),
        new EndOfFileTokenFormattingRule(),
        new StructuredTriviaFormattingRule(),
        new IndentBlockFormattingRule(),
        new SuppressFormattingRule(),
        new AnchorIndentationFormattingRule(),
        new QueryExpressionFormattingRule(),
        new TokenBasedFormattingRule(),
        DefaultOperationProvider.Instance,
    ];

    public override ImmutableArray<AbstractFormattingRule> GetDefaultFormattingRules()
        => _rules;

    public override SyntaxFormattingOptions DefaultOptions
        => CSharpSyntaxFormattingOptions.Default;

    public override SyntaxFormattingOptions GetFormattingOptions(IOptionsReader options)
        => new CSharpSyntaxFormattingOptions(options);

    protected override IFormattingResult CreateAggregatedFormattingResult(SyntaxNode node, IList<AbstractFormattingResult> results, TextSpanMutableIntervalTree? formattingSpans = null)
        => new AggregatedFormattingResult(node, results, formattingSpans);

    protected override AbstractFormattingResult Format(SyntaxNode node, SyntaxFormattingOptions options, ImmutableArray<AbstractFormattingRule> formattingRules, SyntaxToken startToken, SyntaxToken endToken, CancellationToken cancellationToken)
        => new CSharpFormatEngine(node, options, formattingRules, startToken, endToken).Format(cancellationToken);
}
