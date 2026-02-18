// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundPattern
    {
        /// <summary>
        /// Sets <paramref name="innerPattern"/> to the inner pattern after stripping off outer
        /// <see cref="BoundNegatedPattern"/>s, and returns true if the original pattern is a
        /// negated form of the inner pattern.
        /// </summary>
        internal bool IsNegated(out BoundPattern innerPattern)
        {
            innerPattern = this;
            bool negated = false;

            if (innerPattern is BoundNegatedPattern { IsUnionMatching: true })
            {
                // This node doesn't represent a negation at the top level, it really represents a negation
                // in a property sub-pattern. Therefore, doing a semantically equivalent unwrapping for such
                // BoundNegatedPattern isn't trivial since we need to preserve the "union matching" piece stored
                // in the node, probably by rewriting innerPattern.Negated.
                // Bottom line, this will not be a trivial unwrapping. Since the unwrapping is not necessary for
                // correctness, we simply don't do it.
                return negated;
            }

            while (innerPattern is BoundNegatedPattern negatedPattern)
            {
                Debug.Assert(!negatedPattern.IsUnionMatching);
                negated = !negated;
                innerPattern = negatedPattern.Negated;
            }
            return negated;
        }

        public virtual bool IsUnionMatching => false;

        private partial void Validate()
        {
            Debug.Assert(!IsUnionMatching || InputType is NamedTypeSymbol { IsUnionType: true });
        }
    }
}
