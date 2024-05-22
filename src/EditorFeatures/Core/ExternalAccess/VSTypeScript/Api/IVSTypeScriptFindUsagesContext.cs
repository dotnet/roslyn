// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal interface IVSTypeScriptFindUsagesContext
{
    /// <summary>
    /// Used for clients that are finding usages to push information about how far along they
    /// are in their search.
    /// </summary>
    IVSTypeScriptStreamingProgressTracker ProgressTracker { get; }

    /// <summary>
    /// Report a message to be displayed to the user.
    /// </summary>
    ValueTask ReportMessageAsync(string message, CancellationToken cancellationToken);

    /// <summary>
    /// Set the title of the window that results are displayed in.
    /// </summary>
    ValueTask SetSearchTitleAsync(string title, CancellationToken cancellationToken);

    ValueTask OnDefinitionFoundAsync(VSTypeScriptDefinitionItem definition, CancellationToken cancellationToken);
    ValueTask OnReferenceFoundAsync(VSTypeScriptSourceReferenceItem reference, CancellationToken cancellationToken);

    ValueTask OnCompletedAsync(CancellationToken cancellationToken);
}

internal interface IVSTypeScriptStreamingProgressTracker
{
    ValueTask AddItemsAsync(int count, CancellationToken cancellationToken);
    ValueTask ItemCompletedAsync(CancellationToken cancellationToken);
}

internal abstract class VSTypeScriptDefinitionItemNavigator
{
    public abstract Task<bool> CanNavigateToAsync(Workspace workspace, CancellationToken cancellationToken);
    public abstract Task<bool> TryNavigateToAsync(Workspace workspace, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken);
}

internal sealed class VSTypeScriptDefinitionItem
{
    private sealed class ExternalDefinitionItem(VSTypeScriptDefinitionItemNavigator navigator, ImmutableArray<string> tags, ImmutableArray<TaggedText> displayParts)
        : DefinitionItem(
            tags,
            displayParts,
            ImmutableArray<TaggedText>.Empty,
            sourceSpans: default,
            metadataLocations: [],
            classifiedSpans: default,
            properties: null,
            displayableProperties: [],
            displayIfNoReferences: true)
    {
        private readonly VSTypeScriptDefinitionItemNavigator _navigator = navigator;

        internal override bool IsExternal => true;

        public override async Task<INavigableLocation?> GetNavigableLocationAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            if (!await _navigator.CanNavigateToAsync(workspace, cancellationToken).ConfigureAwait(false))
                return null;

            return new NavigableLocation((options, cancellationToken) =>
                _navigator.TryNavigateToAsync(workspace, options.PreferProvisionalTab, options.ActivateTab, cancellationToken));
        }
    }

    internal readonly DefinitionItem UnderlyingObject;

    internal VSTypeScriptDefinitionItem(DefinitionItem underlyingObject)
        => UnderlyingObject = underlyingObject;

    public static VSTypeScriptDefinitionItem Create(
        ImmutableArray<string> tags,
        ImmutableArray<TaggedText> displayParts,
        ImmutableArray<VSTypeScriptDocumentSpan> sourceSpans,
        ImmutableArray<TaggedText> nameDisplayParts = default,
        bool displayIfNoReferences = true)
    {
        return new(DefinitionItem.Create(
            tags, displayParts, sourceSpans.SelectAsArray(static span => span.ToDocumentSpan()), classifiedSpans: [],
            metadataLocations: [], nameDisplayParts, properties: null, displayableProperties: [], displayIfNoReferences: displayIfNoReferences));
    }

    public static VSTypeScriptDefinitionItem CreateExternal(
        VSTypeScriptDefinitionItemNavigator navigator,
        ImmutableArray<string> tags,
        ImmutableArray<TaggedText> displayParts)
        => new(new ExternalDefinitionItem(navigator, tags, displayParts));

    [Obsolete]
    public static VSTypeScriptDefinitionItem Create(VSTypeScriptDefinitionItemBase item)
        => new(item);

    public ImmutableArray<string> Tags => UnderlyingObject.Tags;
    public ImmutableArray<TaggedText> DisplayParts => UnderlyingObject.DisplayParts;

    public ImmutableArray<VSTypeScriptDocumentSpan> GetSourceSpans()
        => UnderlyingObject.SourceSpans.SelectAsArray(span => new VSTypeScriptDocumentSpan(span));

    public async Task<bool> CanNavigateToAsync(Workspace workspace, CancellationToken cancellationToken)
        => await UnderlyingObject.GetNavigableLocationAsync(workspace, cancellationToken).ConfigureAwait(false) != null;

    public async Task<bool> TryNavigateToAsync(Workspace workspace, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
    {
        var location = await UnderlyingObject.GetNavigableLocationAsync(workspace, cancellationToken).ConfigureAwait(false);
        return location != null &&
            await location.NavigateToAsync(new NavigationOptions(showInPreviewTab, activateTab), cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class VSTypeScriptSourceReferenceItem(
    VSTypeScriptDefinitionItem definition,
    VSTypeScriptDocumentSpan sourceSpan,
    VSTypeScriptSymbolUsageInfo symbolUsageInfo)
{
    internal readonly SourceReferenceItem UnderlyingObject = new SourceReferenceItem(
        definition.UnderlyingObject, sourceSpan.ToDocumentSpan(), classifiedSpans: null, symbolUsageInfo.UnderlyingObject);

    public VSTypeScriptDocumentSpan GetSourceSpan()
        => new(UnderlyingObject.SourceSpan);
}

internal readonly struct VSTypeScriptSymbolUsageInfo
{
    internal readonly SymbolUsageInfo UnderlyingObject;

    private VSTypeScriptSymbolUsageInfo(SymbolUsageInfo underlyingObject)
        => UnderlyingObject = underlyingObject;

    public static VSTypeScriptSymbolUsageInfo Create(VSTypeScriptValueUsageInfo valueUsageInfo)
        => new(SymbolUsageInfo.Create((ValueUsageInfo)valueUsageInfo));
}

[Flags]
internal enum VSTypeScriptValueUsageInfo
{
    None = ValueUsageInfo.None,
    Read = ValueUsageInfo.Read,
    Write = ValueUsageInfo.Write,
    Reference = ValueUsageInfo.Reference,
    Name = ValueUsageInfo.Name,
    ReadWrite = ValueUsageInfo.ReadWrite,
    ReadableReference = ValueUsageInfo.ReadableReference,
    WritableReference = ValueUsageInfo.WritableReference,
    ReadableWritableReference = ValueUsageInfo.ReadableWritableReference
}
