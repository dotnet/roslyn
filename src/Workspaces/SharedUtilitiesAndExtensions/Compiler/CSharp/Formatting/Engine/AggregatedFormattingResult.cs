// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal sealed class AggregatedFormattingResult : AbstractAggregatedFormattingResult
{
    public AggregatedFormattingResult(SyntaxNode node, IList<AbstractFormattingResult> results, TextSpanMutableIntervalTree? formattingSpans)
        : base(node, results, formattingSpans)
    {
    }

    protected override SyntaxNode Rewriter(Dictionary<ValueTuple<SyntaxToken, SyntaxToken>, TriviaData> map, CancellationToken cancellationToken)
    {
        var rewriter = new TriviaRewriter(this.Node, GetFormattingSpans(), map, cancellationToken);
        return rewriter.Transform();
    }
}
