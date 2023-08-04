// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CodeActions;

/// <summary>
/// Represents the progress of a <see cref="CodeAction"/>.
/// </summary>
public sealed class CodeActionProgress
{
    internal readonly int? CompletedItems;
    internal readonly int? TotalItems;
    internal readonly Optional<string> Description;

    public CodeActionProgress(int completedItems, int totalItems)
        : this(description: default(Optional<string>), completedItems, totalItems)
    {
    }

    public CodeActionProgress(string description)
        : this(description, completedItems: null, totalItems: null)
    {
    }

    public CodeActionProgress(string description, int completedItems, int totalItems)
        : this(new Optional<string>(description), completedItems, totalItems)
    {
    }

    private CodeActionProgress(Optional<string> description, int? completedItems, int? totalItems)
    {
        if (description.HasValue && description.Value is null)
            throw new ArgumentNullException(nameof(description));

        if (completedItems is < 0)
            throw new ArgumentOutOfRangeException(nameof(completedItems));

        if (totalItems is < 0)
            throw new ArgumentOutOfRangeException(nameof(totalItems));

        if (completedItems > totalItems)
            throw new ArgumentOutOfRangeException(nameof(completedItems));

        CompletedItems = completedItems;
        TotalItems = totalItems;
        Description = description;
    }
}
