// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Tags;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages;

/// <summary>
/// Information about a symbol's definition that can be displayed in an editor
/// and used for navigation.
/// 
/// Standard implementations can be obtained through the various <see cref="DefinitionItem"/>.Create overloads.
/// 
/// Subclassing is also supported for scenarios that fall outside the bounds of
/// these common cases.
/// </summary>
internal abstract partial class DefinitionItem
{
    /// <summary>
    /// The definition item corresponding to the initial symbol the user was trying to find. This item should get
    /// prominent placement in the final UI for the user.
    /// </summary>
    internal const string Primary = nameof(Primary);

    // Existing behavior is to do up to two lookups for 3rd party navigation for FAR.  One
    // for the symbol itself and one for a 'fallback' symbol.  For example, if we're FARing
    // on a constructor, then the fallback symbol will be the actual type that the constructor
    // is contained within.
    internal const string RQNameKey1 = nameof(RQNameKey1);
    internal const string RQNameKey2 = nameof(RQNameKey2);

    /// <summary>
    /// For metadata symbols we encode information in the <see cref="Properties"/> so we can 
    /// retrieve the symbol later on when navigating.  This is needed so that we can go to
    /// metadata-as-source for metadata symbols.  We need to store the <see cref="SymbolKey"/>
    /// for the symbol and the project ID that originated the symbol.  With these we can correctly recover the symbol.
    /// </summary>
    internal const string MetadataSymbolKey = nameof(MetadataSymbolKey);
    internal const string MetadataSymbolOriginatingProjectIdGuid = nameof(MetadataSymbolOriginatingProjectIdGuid);
    internal const string MetadataSymbolOriginatingProjectIdDebugName = nameof(MetadataSymbolOriginatingProjectIdDebugName);

    /// <summary>
    /// If this item is something that cannot be navigated to.  We store this in our
    /// <see cref="Properties"/> to act as an explicit marker that navigation is not possible.
    /// </summary>
    private const string NonNavigable = nameof(NonNavigable);

    /// <summary>
    /// Descriptive tags from <see cref="WellKnownTags"/>. These tags may influence how the 
    /// item is displayed.
    /// </summary>
    public ImmutableArray<string> Tags { get; }

    /// <summary>
    /// Additional properties that can be attached to the definition for clients that want to
    /// keep track of additional data.
    /// </summary>
    public ImmutableDictionary<string, string> Properties { get; }

    /// <summary>
    /// Additional displayable properties that can be attached to the definition for clients that want to display
    /// additional data.
    /// </summary>
    public ImmutableArray<(string key, string value)> DisplayableProperties { get; }

    /// <summary>
    /// The DisplayParts just for the name of this definition.  Generally used only for 
    /// error messages.
    /// </summary>
    public ImmutableArray<TaggedText> NameDisplayParts { get; }

    /// <summary>
    /// The full display parts for this definition.  Displayed in a classified 
    /// manner when possible.
    /// </summary>
    public ImmutableArray<TaggedText> DisplayParts { get; }

    /// <summary>
    /// Additional locations to present in the UI.  A definition may have multiple locations 
    /// for cases like partial types/members.
    /// </summary>
    public ImmutableArray<DocumentSpan> SourceSpans { get; }

    /// <summary>
    /// Precomputed classified spans for the corresponding <see cref="SourceSpans"/>.
    /// </summary>
    public ImmutableArray<ClassifiedSpansAndHighlightSpan?> ClassifiedSpans { get; }

    /// <summary>
    /// Identities of assemblies that contain the metadata for this definition.
    /// </summary>
    public ImmutableArray<AssemblyLocation> MetadataLocations { get; }

    /// <summary>
    /// Whether or not this definition should be presented if we never found any references to
    /// it.  For example, when searching for a property, the FindReferences engine will cascade
    /// to the accessors in case any code specifically called those accessors (can happen in 
    /// cross-language cases).  However, in the normal case where there were no calls specifically
    /// to the accessor, we would not want to display them in the UI.  
    /// 
    /// For most definitions we will want to display them, even if no references were found.  
    /// This property allows for this customization in behavior.
    /// </summary>
    public bool DisplayIfNoReferences { get; }

    internal abstract bool IsExternal { get; }

    protected DefinitionItem(
        ImmutableArray<string> tags,
        ImmutableArray<TaggedText> displayParts,
        ImmutableArray<TaggedText> nameDisplayParts,
        ImmutableArray<DocumentSpan> sourceSpans,
        ImmutableArray<ClassifiedSpansAndHighlightSpan?> classifiedSpans,
        ImmutableArray<AssemblyLocation> metadataLocations,
        ImmutableDictionary<string, string>? properties,
        ImmutableArray<(string key, string value)> displayableProperties,
        bool displayIfNoReferences)
    {
        Tags = tags;
        DisplayParts = displayParts;
        NameDisplayParts = nameDisplayParts.IsDefaultOrEmpty ? displayParts : nameDisplayParts;
        SourceSpans = sourceSpans.NullToEmpty();
        ClassifiedSpans = classifiedSpans.NullToEmpty();
        MetadataLocations = metadataLocations.NullToEmpty();
        Properties = properties ?? ImmutableDictionary<string, string>.Empty;
        DisplayableProperties = displayableProperties.NullToEmpty();
        DisplayIfNoReferences = displayIfNoReferences;

        Contract.ThrowIfFalse(classifiedSpans.IsEmpty || sourceSpans.Length == classifiedSpans.Length);

        if (Properties.ContainsKey(MetadataSymbolKey))
        {
            Contract.ThrowIfFalse(Properties.ContainsKey(MetadataSymbolOriginatingProjectIdGuid));
            Contract.ThrowIfFalse(Properties.ContainsKey(MetadataSymbolOriginatingProjectIdDebugName));
        }
    }

    [Obsolete("Use GetNavigableLocationAsync instead")]
    public Task<bool> TryNavigateToAsync(Workspace workspace, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
        => TryNavigateToAsync(workspace, new NavigationOptions(showInPreviewTab, activateTab), cancellationToken);

    [Obsolete("Use GetNavigableLocationAsync instead")]
    public async Task<bool> TryNavigateToAsync(Workspace workspace, NavigationOptions options, CancellationToken cancellationToken)
    {
        var location = await GetNavigableLocationAsync(workspace, cancellationToken).ConfigureAwait(false);
        return location != null &&
            await location.NavigateToAsync(options, cancellationToken).ConfigureAwait(false);
    }

    public abstract Task<INavigableLocation?> GetNavigableLocationAsync(Workspace workspace, CancellationToken cancellationToken);

    // Kept around for binary compat with TypeScript.
    [Obsolete("TypeScript: Use external access APIs")]
    public static DefinitionItem Create(
        ImmutableArray<string> tags,
        ImmutableArray<TaggedText> displayParts,
        DocumentSpan sourceSpan,
        ImmutableArray<TaggedText> nameDisplayParts = default,
        bool displayIfNoReferences = true)
    {
        return Create(
            tags, displayParts,
            sourceSpan,
            classifiedSpans: null,
            nameDisplayParts, displayIfNoReferences);
    }

    [Obsolete("TypeScript: Use external access APIs")]
    public static DefinitionItem Create(
        ImmutableArray<string> tags,
        ImmutableArray<TaggedText> displayParts,
        DocumentSpan sourceSpan,
        ClassifiedSpansAndHighlightSpan? classifiedSpans,
        ImmutableArray<TaggedText> nameDisplayParts = default,
        bool displayIfNoReferences = true)
    {
        return Create(
            tags, displayParts,
            [sourceSpan],
            [classifiedSpans],
            nameDisplayParts, displayIfNoReferences);
    }

    // Kept around for binary compat with F#/TypeScript.
    [Obsolete("TypeScript: Use external access APIs")]
    public static DefinitionItem Create(
        ImmutableArray<string> tags,
        ImmutableArray<TaggedText> displayParts,
        ImmutableArray<DocumentSpan> sourceSpans,
        ImmutableArray<ClassifiedSpansAndHighlightSpan?> classifiedSpans,
        ImmutableArray<TaggedText> nameDisplayParts,
        bool displayIfNoReferences)
    {
        return Create(
            tags, displayParts, sourceSpans, classifiedSpans, ImmutableArray<AssemblyLocation>.Empty, nameDisplayParts,
            properties: null, displayableProperties: [], displayIfNoReferences: displayIfNoReferences);
    }

    [Obsolete("TypeScript: Use external access APIs")]
    public static DefinitionItem Create(
        ImmutableArray<string> tags,
        ImmutableArray<TaggedText> displayParts,
        ImmutableArray<DocumentSpan> sourceSpans,
        ImmutableArray<ClassifiedSpansAndHighlightSpan?> classifiedSpans,
        ImmutableArray<TaggedText> nameDisplayParts = default,
        ImmutableDictionary<string, string>? properties = null,
        bool displayIfNoReferences = true)
    {
        return Create(
            tags, displayParts, sourceSpans, classifiedSpans,
            ImmutableArray<AssemblyLocation>.Empty, nameDisplayParts, displayIfNoReferences: displayIfNoReferences);
    }

    public static DefinitionItem Create(
        ImmutableArray<string> tags,
        ImmutableArray<TaggedText> displayParts,
        ImmutableArray<DocumentSpan> sourceSpans,
        ImmutableArray<ClassifiedSpansAndHighlightSpan?> classifiedSpans,
        ImmutableArray<AssemblyLocation> metadataLocations,
        ImmutableArray<TaggedText> nameDisplayParts = default,
        ImmutableDictionary<string, string>? properties = null,
        ImmutableArray<(string key, string value)> displayableProperties = default,
        bool displayIfNoReferences = true)
    {
        Contract.ThrowIfTrue(sourceSpans.IsDefault);
        Contract.ThrowIfTrue(metadataLocations.IsDefault);

        return new DefaultDefinitionItem(
            tags, displayParts, nameDisplayParts,
            sourceSpans, classifiedSpans, metadataLocations, properties, displayableProperties, displayIfNoReferences);
    }

    // Kept around for binary compat with F#/TypeScript.
    [Obsolete("TypeScript: Use external access APIs")]
    public static DefinitionItem CreateNonNavigableItem(
        ImmutableArray<string> tags,
        ImmutableArray<TaggedText> displayParts,
        ImmutableArray<TaggedText> originationParts,
        bool displayIfNoReferences)
    {
        return CreateNonNavigableItem(
            tags, displayParts,
            properties: null, displayIfNoReferences: displayIfNoReferences);
    }

    public static DefinitionItem CreateNonNavigableItem(
        ImmutableArray<string> tags,
        ImmutableArray<TaggedText> displayParts,
        ImmutableArray<TaggedText> nameDisplayParts = default,
        ImmutableArray<AssemblyLocation> metadataLocations = default,
        ImmutableDictionary<string, string>? properties = null,
        bool displayIfNoReferences = true)
    {
        properties ??= ImmutableDictionary<string, string>.Empty;
        properties = properties.Add(NonNavigable, "");

        return new DefaultDefinitionItem(
            tags: tags,
            displayParts: displayParts,
            nameDisplayParts: nameDisplayParts,
            sourceSpans: [],
            classifiedSpans: [],
            metadataLocations,
            properties: properties,
            displayableProperties: [],
            displayIfNoReferences: displayIfNoReferences);
    }

    public DetachedDefinitionItem Detach()
        => new(Tags, DisplayParts, NameDisplayParts, SourceSpans.SelectAsArray(ss => (DocumentIdSpan)ss), MetadataLocations, Properties, DisplayableProperties, DisplayIfNoReferences);
}
