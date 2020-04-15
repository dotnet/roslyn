// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal interface IProgressTracker
    {
        string Description { get; set; }
        int CompletedItems { get; }
        int TotalItems { get; }

        void AddItems(int count);
        void ItemCompleted();
        void Clear();
    }
}
