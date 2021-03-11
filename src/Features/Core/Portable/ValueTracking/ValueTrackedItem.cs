// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ValueTracking
{
    internal class ValueTrackedItem
    {
        public Location Location { get; }
        public ISymbol Symbol { get; }
        public SyntaxNode? ExpressionNode { get; }
        public ValueTrackedItem? PreviousTrackedItem { get; }

        public ValueTrackedItem(
            Location location,
            ISymbol symbol,
            SyntaxNode? expressionNode = null,
            ValueTrackedItem? previousTrackedItem = null)
        {
            Location = location;
            Symbol = symbol;
            ExpressionNode = expressionNode;
            PreviousTrackedItem = previousTrackedItem;
        }
    }
}
