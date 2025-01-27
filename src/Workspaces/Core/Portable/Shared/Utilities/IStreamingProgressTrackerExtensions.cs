// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal static class IStreamingProgressTrackerExtensions
{
    /// <summary>
    /// Returns an <see cref="IAsyncDisposable"/> that will call <see cref="ItemCompletedAsync"/> on
    /// <paramref name="progressTracker"/> when it is disposed.
    /// </summary>
    public static async Task<IAsyncDisposable> AddSingleItemAsync(this IStreamingProgressTracker progressTracker, CancellationToken cancellationToken)
    {
        await progressTracker.AddItemsAsync(1, cancellationToken).ConfigureAwait(false);
        return new StreamingProgressDisposer(progressTracker, cancellationToken);
    }

    public static ValueTask ItemCompletedAsync(this IStreamingProgressTracker tracker, CancellationToken cancellationToken)
        => tracker.ItemsCompletedAsync(1, cancellationToken);

    private sealed class StreamingProgressDisposer(IStreamingProgressTracker progressTracker, CancellationToken cancellationToken) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
            => await progressTracker.ItemCompletedAsync(cancellationToken).ConfigureAwait(false);
    }
}
