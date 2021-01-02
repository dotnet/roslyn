// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal interface IProgressTracker
    {
        string? Description { get; set; }
        int CompletedItems { get; }
        int TotalItems { get; }

        void AddItems(int count);
        void ItemCompleted();
        void Clear();
    }

    internal static class IProgressTrackerExtensions
    {
        public static ItemCompletedDisposer ItemCompletedScope(this IProgressTracker tracker)
        {
            return new ItemCompletedDisposer(tracker);
        }

        public readonly struct ItemCompletedDisposer : IDisposable
        {
            private readonly IProgressTracker _tracker;

            public ItemCompletedDisposer(IProgressTracker tracker)
            {
                _tracker = tracker;
            }

            public void Dispose()
            {
                _tracker.ItemCompleted();
            }
        }
    }
}
