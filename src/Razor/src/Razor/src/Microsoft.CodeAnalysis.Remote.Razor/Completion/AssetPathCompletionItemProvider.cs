// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.Completion;

/// <summary>
/// Offers the project's static web assets inside an opted-in attribute value that starts with
/// <c>~/</c>, which the compiler rewrites into an <c>Assets["..."]</c> lookup.
/// </summary>
[Export(typeof(IRazorCompletionItemProvider)), Shared]
internal sealed class AssetPathCompletionItemProvider : IRazorCompletionItemProvider
{
    public ImmutableArray<RazorCompletionItem> GetCompletionItems(RazorCompletionContext context)
    {
        // Expansion only happens in components, and only from Razor 11.0, so offering the paths
        // anywhere else would suggest syntax that stays a literal.
        if (!AssetPathCompletionFacts.IsSupported(context.SyntaxTree.Options))
        {
            return [];
        }

        if (context.AssetPathInfo is not { IsEmpty: false } info)
        {
            return [];
        }

        var sourceText = context.CodeDocument.Source.Text;

        if (!AssetPathCompletionFacts.TryGetAssetPathContext(context.Owner, info, sourceText, context.AbsoluteIndex, out var replacementSpan))
        {
            return [];
        }

        var replacementRange = sourceText.GetLinePositionSpan(replacementSpan);

        using var completionItems = new PooledArrayBuilder<RazorCompletionItem>(info.Assets.Length);

        foreach (var asset in info.Assets)
        {
            completionItems.Add(RazorCompletionItem.CreateAssetPath(
                displayText: asset,
                insertText: asset,
                replacementRange: replacementRange));
        }

        return completionItems.ToImmutable();
    }
}
