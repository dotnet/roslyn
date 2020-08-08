// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class IStreamingProgressTrackerExtensions
    {
        /// <summary>
        /// Returns an <see cref="IAsyncDisposable"/> that will call <see
        /// cref="IStreamingProgressTracker.ItemCompletedAsync"/> on <paramref
        /// name="progressTracker"/> when it is disposed.
        /// </summary>
        public static async Task<IAsyncDisposable> AddSingleItemAsync(this IStreamingProgressTracker progressTracker)
        {
            await progressTracker.AddItemsAsync(1).ConfigureAwait(false);
            return new StreamingProgressDisposer(progressTracker);
        }

        private class StreamingProgressDisposer : IAsyncDisposable
        {
            private readonly IStreamingProgressTracker _progressTracker;

            public StreamingProgressDisposer(IStreamingProgressTracker progressTracker)
                => _progressTracker = progressTracker;

            public async ValueTask DisposeAsync()
                => await _progressTracker.ItemCompletedAsync().ConfigureAwait(false);
        }
    }
}
