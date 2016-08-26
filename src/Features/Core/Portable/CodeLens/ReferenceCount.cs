// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
