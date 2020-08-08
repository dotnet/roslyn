// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeLens
{
    /// <summary>
    /// Represents the result of a FindReferences Count operation.
    /// </summary>
    internal class ReferenceCount
    {
        /// <summary>
        /// Represents the number of references to a given symbol.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Represents if the count is capped by a certain maximum.
        /// </summary>
        public bool IsCapped { get; }

        public ReferenceCount(int count, bool isCapped)
        {
            Count = count;
            IsCapped = isCapped;
        }
    }
}
