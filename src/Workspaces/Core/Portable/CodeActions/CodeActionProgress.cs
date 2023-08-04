// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions;

/// <summary>
/// Represents the progress of a <see cref="CodeAction"/>.
/// </summary>
public sealed class CodeActionProgress
{
    internal bool CompletedItemValue { get; init; }
    internal int? IncompleteItemsValue { get; init; }
    internal Optional<string> DescriptionValue { get; init; }

    /// <summary>
    /// Updates the UI showing the progress of the current <see cref="CodeAction"/> to the specified <paramref name="description"/>.
    /// </summary>
    public static CodeActionProgress Description(string description)
        => new() { DescriptionValue = description ?? throw new ArgumentNullException(nameof(description)) };

    /// <summary>
    /// Adds the requested number of incomplete items to the UI showing the progress of the current <see
    /// cref="CodeAction"/>.  This is commonly presented with a progress bar.
    /// </summary>
    public static CodeActionProgress IncompleteItems(int count)
        => new() { IncompleteItemsValue = count >= 0 ? count : throw new ArgumentOutOfRangeException(nameof(count)) };

    /// <summary>
    /// Indicates that one item of work has transitioned from being incomplete (see <see cref="IncompleteItems"/> to
    /// complete.  This is commonly presented with a progress bar.
    /// </summary>
    public static CodeActionProgress CompletedItem()
        => new() { CompletedItemValue = true };

    //public CodeActionProgress(int completedItems, int totalItems)
    //    : this(description: default(Optional<string>), completedItems, totalItems)
    //{
    //}

    //public CodeActionProgress(string description)
    //    : this(description, completedItems: null, totalItems: null)
    //{
    //}

    //public CodeActionProgress(string description, int completedItems, int totalItems)
    //    : this(new Optional<string>(description), completedItems, totalItems)
    //{
    //}

    //private CodeActionProgress(Optional<string> description, int? completedItems, int? totalItems)
    //{
    //    if (description.HasValue && description.Value is null)
    //        throw new ArgumentNullException(nameof(description));

    //    if (completedItems is < 0)
    //        throw new ArgumentOutOfRangeException(nameof(completedItems));

    //    if (totalItems is < 0)
    //        throw new ArgumentOutOfRangeException(nameof(totalItems));

    //    if (completedItems > totalItems)
    //        throw new ArgumentOutOfRangeException(nameof(completedItems));

    //    CompletedItems = completedItems;
    //    TotalItems = totalItems;
    //    Description = description;
    //}
}

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
