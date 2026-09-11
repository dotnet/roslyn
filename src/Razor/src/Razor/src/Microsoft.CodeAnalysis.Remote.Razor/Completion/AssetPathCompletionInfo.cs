// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.CodeAnalysis.Remote.Razor.Completion;

/// <summary>
/// The project-scoped data an asset path completion needs: which element/attribute pairs opted into
/// <c>~/</c> expansion, and the asset keys that can be offered inside one.
/// </summary>
internal sealed class AssetPathCompletionInfo
{
    public static readonly AssetPathCompletionInfo Empty = new([], new(StringComparer.OrdinalIgnoreCase));

    private readonly Dictionary<string, HashSet<string>> _allowedElementAttributes;

    public ImmutableArray<string> Assets { get; }

    private AssetPathCompletionInfo(ImmutableArray<string> assets, Dictionary<string, HashSet<string>> allowedElementAttributes)
    {
        Assets = assets;
        _allowedElementAttributes = allowedElementAttributes;
    }

    public bool IsEmpty => Assets.Length == 0;

    public bool AcceptsAssetPath(string elementName, string attributeName)
        => _allowedElementAttributes.TryGetValue(elementName, out var attributes) &&
           attributes.Contains(attributeName);

    public AssetPathCompletionInfo WithAssets(ImmutableArray<string> assets)
        => new(assets, _allowedElementAttributes);

    /// <summary>
    /// Builds the allowlist from every discovered tag helper rather than the document's in-scope set.
    /// The carriers produced from <c>[AcceptsAssetPath]</c> live on a type in the runtime's namespace,
    /// so scoping them by the document's <c>@using</c> directives would make expansion and completion
    /// disagree about which attributes opted in. <c>ComponentTildePathPass</c> reads the full set for
    /// the same reason.
    /// </summary>
    public static AssetPathCompletionInfo Create(TagHelperCollection tagHelpers)
    {
        // Both comparisons are case-insensitive, matching HTML semantics.
        var allowedElementAttributes = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var tagHelper in tagHelpers)
        {
            if (tagHelper.Metadata is not AssetPathMetadata { Element: var element, Attribute: var attribute })
            {
                continue;
            }

            if (!allowedElementAttributes.TryGetValue(element, out var attributes))
            {
                attributes = new(StringComparer.OrdinalIgnoreCase);
                allowedElementAttributes.Add(element, attributes);
            }

            attributes.Add(attribute);
        }

        return allowedElementAttributes.Count == 0
            ? Empty
            : new AssetPathCompletionInfo([], allowedElementAttributes);
    }
}
