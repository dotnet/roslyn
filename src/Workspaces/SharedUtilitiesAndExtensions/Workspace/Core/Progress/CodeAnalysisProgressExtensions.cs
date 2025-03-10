// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis;

internal static class CodeAnalysisProgressExtensions
{
    public static void AddItems(this IProgress<CodeAnalysisProgress> progress, int count)
        => progress.Report(CodeAnalysisProgress.AddIncompleteItems(count));

    public static void ItemCompleted(this IProgress<CodeAnalysisProgress> progress)
        => progress.Report(CodeAnalysisProgress.AddCompleteItems(count: 1));

    /// <summary>
    /// Opens a scope that will call <see cref="IProgress{T}.Report(T)"/> with an instance of <see
    /// cref="CodeAnalysisProgress.AddCompleteItems"/> on <paramref name="progress"/> once disposed. This is useful to
    /// easily wrap a series of operations and now that progress will be reported no matter how it completes.
    /// </summary>
    public static ItemCompletedDisposer ItemCompletedScope(this IProgress<CodeAnalysisProgress> progress, string? description = null)
    {
        if (description != null)
            progress.Report(CodeAnalysisProgress.Description(description));

        return new ItemCompletedDisposer(progress);
    }

    public readonly struct ItemCompletedDisposer(IProgress<CodeAnalysisProgress> progress) : IDisposable
    {
        public void Dispose()
            => progress.ItemCompleted();
    }
}
