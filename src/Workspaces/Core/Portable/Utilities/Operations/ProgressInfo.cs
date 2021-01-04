// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Utilities
{
    /// <summary>
    /// Represents an update of a progress.
    /// </summary>
    public readonly struct ProgressInfo
    {
        public ProgressInfo(int completedItems, int totalItems)
        {
            CompletedItems = completedItems;
            TotalItems = totalItems;
        }

        /// <summary>
        /// A number of already completed items.
        /// </summary>
        public int CompletedItems { get; }

        /// <summary>
        /// A total number of items.
        /// </summary>
        public int TotalItems { get; }
    }
}
