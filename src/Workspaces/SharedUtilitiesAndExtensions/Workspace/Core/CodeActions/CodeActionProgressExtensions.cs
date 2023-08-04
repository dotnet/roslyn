// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions;

internal static class CodeActionProgressExtensions
{
    /// <summary>
    /// Bridge method from original <see cref="IProgressTracker"/> api to <see cref="IProgress{T}"/>.
    /// </summary>
    public static void AddItems(this IProgress<CodeActionProgress> progress, int count)
        => progress.Report(CodeActionProgress.IncompleteItems(count));

    /// <summary>
    /// Bridge method from original <see cref="IProgressTracker"/> api to <see cref="IProgress{T}"/>.
    /// </summary>
    public static void ItemCompleted(this IProgress<CodeActionProgress> progress)
        => progress.Report(CodeActionProgress.CompletedItem());

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
