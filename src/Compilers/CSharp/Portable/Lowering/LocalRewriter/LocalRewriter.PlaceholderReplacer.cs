// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp;

internal sealed partial class LocalRewriter
{
    private sealed class PlaceholderReplacer : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        private readonly Dictionary<BoundEarlyValuePlaceholderBase, BoundExpression> _placeholders;

        private PlaceholderReplacer(Dictionary<BoundEarlyValuePlaceholderBase, BoundExpression> placeholders)
        {
            _placeholders = placeholders;
        }

        public static BoundExpression Replace(Dictionary<BoundEarlyValuePlaceholderBase, BoundExpression> placeholders, BoundExpression expr)
        {
            var result = new PlaceholderReplacer(placeholders).Visit(expr);
            Debug.Assert(result is not null);
            return (BoundExpression)result;
        }

        private BoundNode ReplacePlaceholder(BoundEarlyValuePlaceholderBase placeholder)
        {
            var value = _placeholders[placeholder];
            Debug.Assert(value is not null);
            return value;
        }

        public override BoundNode VisitListPatternReceiverPlaceholder(BoundListPatternReceiverPlaceholder node)
        {
            return ReplacePlaceholder(node);
        }

        public override BoundNode VisitListPatternIndexPlaceholder(BoundListPatternIndexPlaceholder node)
        {
            return ReplacePlaceholder(node);
        }

        public override BoundNode VisitSlicePatternReceiverPlaceholder(BoundSlicePatternReceiverPlaceholder node)
        {
            return ReplacePlaceholder(node);
        }

        public override BoundNode VisitSlicePatternRangePlaceholder(BoundSlicePatternRangePlaceholder node)
        {
            return ReplacePlaceholder(node);
        }
    }
}
