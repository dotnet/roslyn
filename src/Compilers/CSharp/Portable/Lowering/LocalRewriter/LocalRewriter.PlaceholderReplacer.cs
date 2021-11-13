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
        private readonly Dictionary<BoundValuePlaceholderBase, BoundExpression> _placeholders;

        PlaceholderReplacer(Dictionary<BoundValuePlaceholderBase, BoundExpression> placeholders)
        {
            _placeholders = placeholders;
        }

        public static BoundExpression Replace(Dictionary<BoundValuePlaceholderBase, BoundExpression> placeholders, BoundExpression expr)
        {
            var result = new PlaceholderReplacer(placeholders).Visit(expr);
            Debug.Assert(result is not null);
            return (BoundExpression)result;
        }

        private BoundNode ReplacePlaceholder(BoundValuePlaceholderBase placeholder)
        {
            var value = _placeholders[placeholder];
            Debug.Assert(value is not null);
            return value;
        }

        public override BoundNode? VisitIndexOrRangePatternIndexerAccess(BoundIndexOrRangePatternIndexerAccess node)
        {
            var argument = (BoundExpression)this.Visit(node.Argument);
            var lengthOrCountAccess = (BoundExpression)this.Visit(node.LengthOrCountAccess);
            var receiverPlaceholder = (BoundIndexOrRangeIndexerPatternReceiverPlaceholder)this.Visit(node.ReceiverPlaceholder);
            var indexerAccess = (BoundExpression)this.Visit(node.IndexerAccess);
            var argumentPlaceholders = this.VisitList(node.ArgumentPlaceholders);
            var type = this.VisitType(node.Type);
            return node.Update(argument, lengthOrCountAccess, receiverPlaceholder, indexerAccess, argumentPlaceholders, type);
        }

        public override BoundNode? VisitListPatternReceiverPlaceholder(BoundListPatternReceiverPlaceholder node)
        {
            return ReplacePlaceholder(node);
        }

        public override BoundNode? VisitListPatternIndexPlaceholder(BoundListPatternIndexPlaceholder node)
        {
            return ReplacePlaceholder(node);
        }

        public override BoundNode? VisitSlicePatternReceiverPlaceholder(BoundSlicePatternReceiverPlaceholder node)
        {
            return ReplacePlaceholder(node);
        }

        public override BoundNode? VisitSlicePatternRangePlaceholder(BoundSlicePatternRangePlaceholder node)
        {
            return ReplacePlaceholder(node);
        }
    }
}
