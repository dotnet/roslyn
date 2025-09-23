// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.SignatureHelp;

/// <summary>
/// Used for C#/VB sig help providers so they can build up information using SymbolDisplayParts.
/// These parts will then by used to properly replace anonymous type information in the parts.
/// Once that it done, this will be converted to normal SignatureHelpParameters which only 
/// point to TaggedText parts.
/// </summary>
internal sealed class SignatureHelpSymbolParameter(
    string? name,
    bool isOptional,
    Func<CancellationToken, IEnumerable<TaggedText>>? documentationFactory,
    ImmutableArray<SymbolDisplayPart> displayParts,
    ImmutableArray<SymbolDisplayPart>? prefixDisplayParts = null,
    ImmutableArray<SymbolDisplayPart>? suffixDisplayParts = null,
    ImmutableArray<SymbolDisplayPart>? selectedDisplayParts = null)
{
    /// <summary>
    /// The name of this parameter.
    /// </summary>
    public string Name { get; } = name ?? string.Empty;

    /// <summary>
    /// Documentation for this parameter.  This should normally be presented to the user when
    /// this parameter is selected.
    /// </summary>
    public Func<CancellationToken, IEnumerable<TaggedText>> DocumentationFactory { get; } = documentationFactory ?? s_emptyDocumentationFactory;

    /// <summary>
    /// Display parts to show before the normal display parts for the parameter.
    /// </summary>
    public ImmutableArray<SymbolDisplayPart> PrefixDisplayParts { get; } = prefixDisplayParts.NullToEmpty();

    /// <summary>
    /// Display parts to show after the normal display parts for the parameter.
    /// </summary>
    public ImmutableArray<SymbolDisplayPart> SuffixDisplayParts { get; } = suffixDisplayParts ?? [];

    /// <summary>
    /// Display parts for this parameter.  This should normally be presented to the user as part
    /// of the entire signature display.
    /// </summary>
    public ImmutableArray<SymbolDisplayPart> DisplayParts { get; } = displayParts.NullToEmpty();

    /// <summary>
    /// True if this parameter is optional or not.  Optional parameters may be presented in a
    /// different manner to users.
    /// </summary>
    public bool IsOptional { get; } = isOptional;

    /// <summary>
    /// Display parts for this parameter that should be presented to the user when this
    /// parameter is selected.
    /// </summary>
    public ImmutableArray<SymbolDisplayPart> SelectedDisplayParts { get; } = selectedDisplayParts ?? [];

    private static readonly Func<CancellationToken, IEnumerable<TaggedText>> s_emptyDocumentationFactory = _ => [];

    internal ImmutableArray<SymbolDisplayPart> GetAllParts()
        => [.. PrefixDisplayParts, .. DisplayParts, .. SuffixDisplayParts, .. SelectedDisplayParts];

    public static explicit operator SignatureHelpParameter(SignatureHelpSymbolParameter parameter)
    {
        return new SignatureHelpParameter(
            parameter.Name, parameter.IsOptional, parameter.DocumentationFactory,
            parameter.DisplayParts.ToTaggedText(),
            parameter.PrefixDisplayParts.ToTaggedText(),
            parameter.SuffixDisplayParts.ToTaggedText(),
            parameter.SelectedDisplayParts.ToTaggedText());
    }
}

internal sealed class SignatureHelpParameter(
    string? name,
    bool isOptional,
    Func<CancellationToken, IEnumerable<TaggedText>>? documentationFactory,
    ImmutableArray<TaggedText> displayParts,
    ImmutableArray<TaggedText>? prefixDisplayParts = null,
    ImmutableArray<TaggedText>? suffixDisplayParts = null,
    ImmutableArray<TaggedText>? selectedDisplayParts = null)
{
    /// <summary>
    /// The name of this parameter.
    /// </summary>
    public string Name { get; } = name ?? string.Empty;

    /// <summary>
    /// Documentation for this parameter.  This should normally be presented to the user when
    /// this parameter is selected.
    /// </summary>
    public Func<CancellationToken, IEnumerable<TaggedText>> DocumentationFactory { get; } = documentationFactory ?? s_emptyDocumentationFactory;

    /// <summary>
    /// Display parts to show before the normal display parts for the parameter.
    /// </summary>
    public ImmutableArray<TaggedText> PrefixDisplayParts { get; } = prefixDisplayParts ?? [];

    /// <summary>
    /// Display parts to show after the normal display parts for the parameter.
    /// </summary>
    public ImmutableArray<TaggedText> SuffixDisplayParts { get; } = suffixDisplayParts ?? [];

    /// <summary>
    /// Display parts for this parameter.  This should normally be presented to the user as part
    /// of the entire signature display.
    /// </summary>
    public ImmutableArray<TaggedText> DisplayParts { get; } = displayParts.NullToEmpty();

    /// <summary>
    /// True if this parameter is optional or not.  Optional parameters may be presented in a
    /// different manner to users.
    /// </summary>
    public bool IsOptional { get; } = isOptional;

    /// <summary>
    /// Display parts for this parameter that should be presented to the user when this
    /// parameter is selected.
    /// </summary>
    public ImmutableArray<TaggedText> SelectedDisplayParts { get; } = selectedDisplayParts ?? [];

    private static readonly Func<CancellationToken, IEnumerable<TaggedText>> s_emptyDocumentationFactory = _ => [];

    // Constructor kept for binary compat with TS.  Remove when they move to the new API.
    public SignatureHelpParameter(
        string name,
        bool isOptional,
        Func<CancellationToken, IEnumerable<SymbolDisplayPart>>? documentationFactory,
        ImmutableArray<SymbolDisplayPart> displayParts,
        ImmutableArray<SymbolDisplayPart>? prefixDisplayParts = null,
        ImmutableArray<SymbolDisplayPart>? suffixDisplayParts = null,
        ImmutableArray<SymbolDisplayPart>? selectedDisplayParts = null)
        : this(name, isOptional,
              documentationFactory is null ? null : c => documentationFactory(c).ToTaggedText(),
              displayParts.ToTaggedText(),
              prefixDisplayParts.ToTaggedText(),
              suffixDisplayParts.ToTaggedText(),
              selectedDisplayParts.ToTaggedText())
    {
    }

    internal ImmutableArray<TaggedText> GetAllParts()
        => [.. PrefixDisplayParts, .. DisplayParts, .. SuffixDisplayParts, .. SelectedDisplayParts];

    public override string ToString()
    {
        var prefix = string.Concat(PrefixDisplayParts);
        var display = string.Concat(DisplayParts);
        var suffix = string.Concat(SuffixDisplayParts);
        return string.Concat(prefix, display, suffix);
    }
}
