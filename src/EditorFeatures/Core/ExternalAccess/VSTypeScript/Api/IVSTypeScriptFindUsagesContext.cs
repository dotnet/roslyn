// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
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

    internal sealed class VSTypeScriptDefinitionItem
    {
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
                tags, displayParts, sourceSpans.SelectAsArray(span => span.ToDocumentSpan()), nameDisplayParts,
                properties: null, displayableProperties: ImmutableDictionary<string, string>.Empty, displayIfNoReferences: displayIfNoReferences));
        }

        public static VSTypeScriptDefinitionItem Create(VSTypeScriptDefinitionItemBase item)
            => new(item);

        [Obsolete]
        public VSTypeScriptDefinitionItem(
            ImmutableArray<string> tags,
            ImmutableArray<TaggedText> displayParts,
            ImmutableArray<DocumentSpan> sourceSpans,
            ImmutableArray<TaggedText> nameDisplayParts = default,
            ImmutableDictionary<string, string>? properties = null,
            ImmutableDictionary<string, string>? displayableProperties = null,
            bool displayIfNoReferences = true)
        {
            UnderlyingObject = new DefinitionItem.DefaultDefinitionItem(
                tags, displayParts, nameDisplayParts, originationParts: ImmutableArray<TaggedText>.Empty, sourceSpans, properties, displayableProperties, displayIfNoReferences);
        }

        public ImmutableArray<string> Tags => UnderlyingObject.Tags;
        public ImmutableArray<TaggedText> DisplayParts => UnderlyingObject.DisplayParts;

        [Obsolete]
        public ImmutableArray<DocumentSpan> SourceSpans => UnderlyingObject.SourceSpans;

        public ImmutableArray<VSTypeScriptDocumentSpan> GetSourceSpans()
            => UnderlyingObject.SourceSpans.SelectAsArray(span => new VSTypeScriptDocumentSpan(span));

        public Task<bool> CanNavigateToAsync(Workspace workspace, CancellationToken cancellationToken)
            => UnderlyingObject.CanNavigateToAsync(workspace, cancellationToken);

        public Task<bool> TryNavigateToAsync(Workspace workspace, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken)
            => UnderlyingObject.TryNavigateToAsync(workspace, showInPreviewTab, activateTab, cancellationToken);
    }

    internal sealed class VSTypeScriptSourceReferenceItem
    {
        internal readonly SourceReferenceItem UnderlyingObject;

        public VSTypeScriptSourceReferenceItem(
            VSTypeScriptDefinitionItem definition,
            VSTypeScriptDocumentSpan sourceSpan,
            VSTypeScriptSymbolUsageInfo symbolUsageInfo)
        {
            UnderlyingObject = new SourceReferenceItem(definition.UnderlyingObject, sourceSpan.ToDocumentSpan(), symbolUsageInfo.UnderlyingObject);
        }

        [Obsolete]
        public VSTypeScriptSourceReferenceItem(
            VSTypeScriptDefinitionItem definition,
            DocumentSpan sourceSpan,
            SymbolUsageInfo symbolUsageInfo)
        {
            UnderlyingObject = new SourceReferenceItem(definition.UnderlyingObject, sourceSpan, symbolUsageInfo);
        }

        public VSTypeScriptDocumentSpan GetSourceSpan()
            => new(UnderlyingObject.SourceSpan);

        [Obsolete]
        public DocumentSpan SourceSpan
            => UnderlyingObject.SourceSpan;
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
}
