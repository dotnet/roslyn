// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Progress;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents the progress of an operation.  Commonly used to update a UI visible to a user when a long running
/// operation is happening.
/// </summary>
public sealed class CodeAnalysisProgress
{
    /// <summary>
    /// Used when bridging from an api that does not show progress to the user to an api that can update progress if
    /// available.  This should be used sparingly.  Locations that currently do not show progress should ideally be
    /// migrated to ones that do so that long running operations are visible to the user in a coherent fashion.
    /// </summary>
    internal static readonly IProgress<CodeAnalysisProgress> None = NullProgress<CodeAnalysisProgress>.Instance;

    internal bool ClearValue { get; init; }
    internal int? CompleteItemValue { get; init; }
    internal int? IncompleteItemsValue { get; init; }
    internal string? DescriptionValue { get; init; }

    /// <summary>
    /// When passed to an appropriate <see cref="IProgress{T}"/>, will updates the UI showing the progress of the
    /// current operation to the specified <paramref name="description"/>.
    /// </summary>
    /// <example>
    /// progress.Report(CodeAnalysisProgress.Description("Renaming files"));
    /// </example>
    public static CodeAnalysisProgress Description(string description)
        => new() { DescriptionValue = description ?? throw new ArgumentNullException(nameof(description)) };

    /// <summary>
    /// When passed to an appropriate <see cref="IProgress{T}"/>, will add the requested number of incomplete items to
    /// the UI showing the progress of the current operation.  This is commonly presented with a progress bar.  An
    /// optional <paramref name="description"/> can also be provided to update the UI accordingly (see <see
    /// cref="Description"/>).
    /// </summary>
    /// <param name="count">The number of incomplete items left to perform.</param>
    /// <param name="description">Optional description to update the UI to.</param>
    /// <example>
    /// progress.Report(CodeAnalysisProgress.AddIncompleteItems(20));
    /// </example>
    public static CodeAnalysisProgress AddIncompleteItems(int count, string? description = null)
        => new()
        {
            IncompleteItemsValue = count >= 0 ? count : throw new ArgumentOutOfRangeException(nameof(count)),
            DescriptionValue = description,
        };

    /// <summary>
    /// By default, When passed to an appropriate <see cref="IProgress{T}"/>, will indicate that an item of work has
    /// transitioned from being incomplete (see <see cref="AddIncompleteItems"/> to complete.  This is commonly
    /// presented with a progress bar. An optional <paramref name="description"/> can also be provided to update the UI
    /// accordingly (see <see cref="Description"/>).
    /// </summary>
    /// <remarks>
    /// Multiple items of work can be transitioned to be complete by passing an explicit value to <paramref
    /// name="count"/>.
    /// </remarks>
    /// <param name="count">The number of items that were completed.  Defaults to <c>1</c> for the trivial case where
    /// only a single item has been completed.  Must be greater than or equal to 1.</param>
    /// <param name="description">Optional description to update the UI to.</param>
    /// <example>
    /// progress.Report(CodeAnalysisProgress.CompleteItem());
    /// </example>
    public static CodeAnalysisProgress CompleteItem(int count = 1, string? description = null)
        => new()
        {
            CompleteItemValue = count >= 1 ? count : throw new ArgumentOutOfRangeException(nameof(count)),
            DescriptionValue = description,
        };

    /// <summary>
    /// When passed to an appropriate <see cref="IProgress{T}"/>, will indicate that all progress should be reset for
    /// the current operation. This is normally done when the code action is performing some new phase and wishes for
    /// the UI progress bar to restart from the beginning.
    /// </summary>
    /// <remarks>
    /// Currently internal as only roslyn needs this in the impl of our suggested action (we use a progress bar to
    /// compute the work, then reset the progress to apply all the changes).  Could be exposed later to 3rd party code
    /// if a demonstrable need is presented.
    /// </remarks>
    internal static CodeAnalysisProgress Clear()
        => new() { ClearValue = true };
}

internal sealed class CodeAnalysisProgressTracker(Action<string?, int, int>? updateAction) : IProgress<CodeAnalysisProgress>
{
    private string? _description;
    private int _completedItems;
    private int _totalItems;

    public CodeAnalysisProgressTracker()
        : this(null)
    {
    }

    public string? Description
    {
        get => _description;
        set
        {
            _description = value;
            Update();
        }
    }

    public int CompletedItems => _completedItems;

    public int TotalItems => _totalItems;

    public void AddItems(int count)
    {
        Interlocked.Add(ref _totalItems, count);
        Update();
    }

    public void CompleteItems(int count)
    {
        Interlocked.Add(ref _completedItems, count);
        Update();
    }

    public void Clear()
    {
        _totalItems = 0;
        _completedItems = 0;
        _description = null;
        Update();
    }

    private void Update()
        => updateAction?.Invoke(_description, _completedItems, _totalItems);

    public void Report(CodeAnalysisProgress value)
    {
        if (value.DescriptionValue != null)
            this.Description = value.DescriptionValue;

        if (value.IncompleteItemsValue != null)
            this.AddItems(value.IncompleteItemsValue.Value);

        if (value.CompleteItemValue != null)
            this.CompleteItems(value.CompleteItemValue.Value);
    }
}
