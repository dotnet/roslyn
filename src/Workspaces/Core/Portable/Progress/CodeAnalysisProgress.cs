// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Progress;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents the progress of an operation.  Commonly used to update a UI visible to a user when a long running
/// operation is happening.
/// </summary>
public sealed class CodeAnalysisProgress
{
    internal static readonly IProgress<CodeAnalysisProgress> Null = NullProgress<CodeAnalysisProgress>.Instance;

    internal bool ClearValue { get; init; }
    internal bool CompletedItemValue { get; init; }
    internal int? IncompleteItemsValue { get; init; }
    internal string? DescriptionValue { get; init; }

    /// <summary>
    /// Updates the UI showing the progress of the current operation to the specified <paramref name="description"/>.
    /// </summary>
    public static CodeAnalysisProgress Description(string description)
        => new() { DescriptionValue = description ?? throw new ArgumentNullException(nameof(description)) };

    /// <summary>
    /// Adds the requested number of incomplete items to the UI showing the progress of the current operation.  This is
    /// commonly presented with a progress bar.
    /// </summary>
    public static CodeAnalysisProgress IncompleteItems(int count)
        => new() { IncompleteItemsValue = count >= 0 ? count : throw new ArgumentOutOfRangeException(nameof(count)) };

    /// <summary>
    /// Indicates that one item of work has transitioned from being incomplete (see <see cref="IncompleteItems"/> to
    /// complete.  This is commonly presented with a progress bar.
    /// </summary>
    public static CodeAnalysisProgress CompletedItem()
        => new() { CompletedItemValue = true };

    /// <summary>
    /// Indicates that all progress should be reset for the current operation. This is normally done when the code
    /// action is performing some new phase and wishes for the UI progress bar to restart from the beginning.
    /// </summary>
    public static CodeAnalysisProgress Clear()
        => new() { ClearValue = true };

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

    public void ItemCompleted()
    {
        Interlocked.Increment(ref _completedItems);
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

        if (value.CompletedItemValue)
            this.ItemCompleted();
    }
}
