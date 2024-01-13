// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            while (innerPattern is BoundNegatedPattern negatedPattern)
            {
                negated = !negated;
                innerPattern = negatedPattern.Negated;
            }
            return negated;
        }
    }
}
