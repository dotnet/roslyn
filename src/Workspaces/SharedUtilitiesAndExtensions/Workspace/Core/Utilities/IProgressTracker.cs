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
        /// <summary>
        /// Opens a scope that will call <see cref="IProgressTracker.ItemCompleted"/> on <paramref name="tracker"/> once
        /// disposed. This is useful to easily wrap a series of operations and now that progress will be reported no
        /// matter how it completes.
        /// </summary>
        public static ItemCompletedDisposer ItemCompletedScope(this IProgressTracker tracker, string? description = null)
        {
            if (description != null)
                tracker.Description = description;

            return new ItemCompletedDisposer(tracker);
        }

        public readonly struct ItemCompletedDisposer(IProgressTracker tracker) : IDisposable
        {
            public void Dispose()
            {
                tracker.ItemCompleted();
            }
        }
    }
}
