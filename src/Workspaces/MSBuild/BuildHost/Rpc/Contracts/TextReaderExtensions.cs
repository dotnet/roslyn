// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

internal static class TextReaderExtensions
{
    /// <summary>
    /// Returns the next line from the string, or null if the stream is closed or the cancellation token was triggered.
    /// </summary>
    public static async Task<string?> TryReadLineOrReturnNullIfCancelledAsync(this TextReader streamReader, CancellationToken cancellationToken)
    {
        // If we're on .NET Core 8.0 the implementation is easy, but on older versions we don't have the helper and since we also don't have
        // Microsoft.VisualStudio.Threading's WithCancellation helper we have to inline the same approach.

#if NET8_0_OR_GREATER

        try
        {
            return await streamReader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }

#else

        var cancellationTaskSource = new TaskCompletionSource<object?>();
        var readLineTask = streamReader.ReadLineAsync();

        using (cancellationToken.Register(() => cancellationTaskSource.TrySetResult(null)))
        {
            await Task.WhenAny(readLineTask, cancellationTaskSource.Task).ConfigureAwait(false);

            if (readLineTask.Status == TaskStatus.RanToCompletion)
                return readLineTask.Result;
            else
                return null;
        }

#endif
    }
}
