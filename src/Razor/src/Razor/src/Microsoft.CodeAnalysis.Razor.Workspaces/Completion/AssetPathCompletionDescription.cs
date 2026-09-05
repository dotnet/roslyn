// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Completion;

/// <summary>
/// Placeholder description for static web asset completions. The asset key is the entire payload of
/// the item, and it is already the label, so there is nothing further to tell the user.
/// </summary>
internal sealed class AssetPathCompletionDescription : CompletionDescription
{
    public static readonly AssetPathCompletionDescription Instance = new();

    private AssetPathCompletionDescription()
    {
    }

    public override string Description => string.Empty;
}
