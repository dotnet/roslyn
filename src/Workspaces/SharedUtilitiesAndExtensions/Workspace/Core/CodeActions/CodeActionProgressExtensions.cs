// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CodeActions;

internal static class CodeActionProgressExtensions
{
    /// <summary>
    /// Opens a scope that will call <see cref="IProgress{T}.Report(T)"/> with an instance of <see
    /// cref="CodeActionProgress.CompletedItem"/> on <paramref name="progress"/> once disposed. This is useful to easily
    /// wrap a series of operations and now that progress will be reported no matter how it completes.
    /// </summary>
    public static ItemCompletedDisposer ItemCompletedScope(this IProgress<CodeActionProgress> progress, string? description = null)
    {
        if (description != null)
            progress.Report(CodeActionProgress.Description(description));

        return new ItemCompletedDisposer(progress);
    }

    public readonly struct ItemCompletedDisposer(IProgress<CodeActionProgress> progress) : IDisposable
    {
        public void Dispose()
            => progress.Report(CodeActionProgress.CompletedItem());
    }
}
