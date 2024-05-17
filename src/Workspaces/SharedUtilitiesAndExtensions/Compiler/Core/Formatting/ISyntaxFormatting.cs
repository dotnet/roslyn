// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting;

internal interface ISyntaxFormatting
{
    SyntaxFormattingOptions DefaultOptions { get; }
    SyntaxFormattingOptions GetFormattingOptions(IOptionsReader options, SyntaxFormattingOptions? fallbackOptions);

    ImmutableArray<AbstractFormattingRule> GetDefaultFormattingRules();
    IFormattingResult GetFormattingResult(SyntaxNode node, IEnumerable<TextSpan>? spans, SyntaxFormattingOptions options, ImmutableArray<AbstractFormattingRule> rules, CancellationToken cancellationToken);
}
