﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal interface IRemoteFindUsagesService
    {
        internal interface ICallback
        {
            ValueTask AddItemsAsync(RemoteServiceCallbackId callbackId, int count);
            ValueTask ItemCompletedAsync(RemoteServiceCallbackId callbackId);
            ValueTask ReportMessageAsync(RemoteServiceCallbackId callbackId, string message);
            ValueTask ReportProgressAsync(RemoteServiceCallbackId callbackId, int current, int maximum);
            ValueTask SetSearchTitleAsync(RemoteServiceCallbackId callbackId, string title);
            ValueTask OnDefinitionFoundAsync(RemoteServiceCallbackId callbackId, SerializableDefinitionItem definition);
            ValueTask OnReferenceFoundAsync(RemoteServiceCallbackId callbackId, SerializableSourceReferenceItem reference);
        }

        ValueTask FindReferencesAsync(
            PinnedSolutionInfo solutionInfo,
            RemoteServiceCallbackId callbackId,
            SerializableSymbolAndProjectId symbolAndProjectId,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken);

        ValueTask FindImplementationsAsync(
            PinnedSolutionInfo solutionInfo,
            RemoteServiceCallbackId callbackId,
            SerializableSymbolAndProjectId symbolAndProjectId,
            CancellationToken cancellationToken);
    }

    [ExportRemoteServiceCallbackDispatcher(typeof(IRemoteFindUsagesService)), Shared]
    internal sealed class FindUsagesServerCallbackDispatcher : RemoteServiceCallbackDispatcher, IRemoteFindUsagesService.ICallback
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FindUsagesServerCallbackDispatcher()
        {
        }

        private new FindUsagesServerCallback GetCallback(RemoteServiceCallbackId callbackId)
            => (FindUsagesServerCallback)base.GetCallback(callbackId);

        public ValueTask AddItemsAsync(RemoteServiceCallbackId callbackId, int count)
            => GetCallback(callbackId).AddItemsAsync(count);

        public ValueTask ItemCompletedAsync(RemoteServiceCallbackId callbackId)
            => GetCallback(callbackId).ItemCompletedAsync();

        public ValueTask OnDefinitionFoundAsync(RemoteServiceCallbackId callbackId, SerializableDefinitionItem definition)
            => GetCallback(callbackId).OnDefinitionFoundAsync(definition);

        public ValueTask OnReferenceFoundAsync(RemoteServiceCallbackId callbackId, SerializableSourceReferenceItem reference)
            => GetCallback(callbackId).OnReferenceFoundAsync(reference);

        public ValueTask ReportMessageAsync(RemoteServiceCallbackId callbackId, string message)
            => GetCallback(callbackId).ReportMessageAsync(message);

        [Obsolete]
        public ValueTask ReportProgressAsync(RemoteServiceCallbackId callbackId, int current, int maximum)
            => GetCallback(callbackId).ReportProgressAsync(current, maximum);

        public ValueTask SetSearchTitleAsync(RemoteServiceCallbackId callbackId, string title)
            => GetCallback(callbackId).SetSearchTitleAsync(title);
    }

    internal sealed class FindUsagesServerCallback
    {
        private readonly Solution _solution;
        private readonly IFindUsagesContext _context;
        private readonly Dictionary<int, DefinitionItem> _idToDefinition = new();

        public FindUsagesServerCallback(Solution solution, IFindUsagesContext context)
        {
            _solution = solution;
            _context = context;
        }

        public ValueTask AddItemsAsync(int count)
            => _context.ProgressTracker.AddItemsAsync(count);

        public ValueTask ItemCompletedAsync()
            => _context.ProgressTracker.ItemCompletedAsync();

        public ValueTask ReportMessageAsync(string message)
            => _context.ReportMessageAsync(message);

        [Obsolete]
        public ValueTask ReportProgressAsync(int current, int maximum)
            => _context.ReportProgressAsync(current, maximum);

        public ValueTask SetSearchTitleAsync(string title)
            => _context.SetSearchTitleAsync(title);

        public async ValueTask OnDefinitionFoundAsync(SerializableDefinitionItem definition)
        {
            try
            {
                var id = definition.Id;
                var rehydrated = await definition.RehydrateAsync(_solution, _context.CancellationToken).ConfigureAwait(false);

                lock (_idToDefinition)
                {
                    _idToDefinition.Add(id, rehydrated);
                }

                await _context.OnDefinitionFoundAsync(rehydrated).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == _context.CancellationToken)
            {
                // Eat the cancellation exception since we're unsure if it's safe to propagate back to the remote side
            }
        }

        public async ValueTask OnReferenceFoundAsync(SerializableSourceReferenceItem reference)
        {
            try
            {
                var rehydrated = await reference.RehydrateAsync(_solution, GetDefinition(reference.DefinitionId), _context.CancellationToken).ConfigureAwait(false);

                await _context.OnReferenceFoundAsync(rehydrated).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (oce.CancellationToken == _context.CancellationToken)
            {
                // Eat the cancellation exception since we're unsure if it's safe to propagate back to the remote side
            }
        }

        private DefinitionItem GetDefinition(int definitionId)
        {
            lock (_idToDefinition)
            {
                Contract.ThrowIfFalse(_idToDefinition.ContainsKey(definitionId));
                return _idToDefinition[definitionId];
            }
        }
    }

    [DataContract]
    internal readonly struct SerializableDocumentSpan
    {
        [DataMember(Order = 0)]
        public readonly DocumentId DocumentId;

        [DataMember(Order = 1)]
        public readonly TextSpan SourceSpan;

        public SerializableDocumentSpan(DocumentId documentId, TextSpan sourceSpan)
        {
            DocumentId = documentId;
            SourceSpan = sourceSpan;
        }

        public static SerializableDocumentSpan Dehydrate(DocumentSpan documentSpan)
            => new(documentSpan.Document.Id, documentSpan.SourceSpan);

        public async ValueTask<DocumentSpan> RehydrateAsync(Solution solution, CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(DocumentId) ??
                           await solution.GetSourceGeneratedDocumentAsync(DocumentId, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(document);
            return new DocumentSpan(document, SourceSpan);
        }
    }

    [DataContract]
    internal readonly struct SerializableDefinitionItem
    {
        [DataMember(Order = 0)]
        public readonly int Id;

        [DataMember(Order = 1)]
        public readonly ImmutableArray<string> Tags;

        [DataMember(Order = 2)]
        public readonly ImmutableArray<TaggedText> DisplayParts;

        [DataMember(Order = 3)]
        public readonly ImmutableArray<TaggedText> NameDisplayParts;

        [DataMember(Order = 4)]
        public readonly ImmutableArray<TaggedText> OriginationParts;

        [DataMember(Order = 5)]
        public readonly ImmutableArray<SerializableDocumentSpan> SourceSpans;

        [DataMember(Order = 6)]
        public readonly ImmutableDictionary<string, string> Properties;

        [DataMember(Order = 7)]
        public readonly ImmutableDictionary<string, string> DisplayableProperties;

        [DataMember(Order = 8)]
        public readonly bool DisplayIfNoReferences;

        public SerializableDefinitionItem(
            int id,
            ImmutableArray<string> tags,
            ImmutableArray<TaggedText> displayParts,
            ImmutableArray<TaggedText> nameDisplayParts,
            ImmutableArray<TaggedText> originationParts,
            ImmutableArray<SerializableDocumentSpan> sourceSpans,
            ImmutableDictionary<string, string> properties,
            ImmutableDictionary<string, string> displayableProperties,
            bool displayIfNoReferences)
        {
            Id = id;
            Tags = tags;
            DisplayParts = displayParts;
            NameDisplayParts = nameDisplayParts;
            OriginationParts = originationParts;
            SourceSpans = sourceSpans;
            Properties = properties;
            DisplayableProperties = displayableProperties;
            DisplayIfNoReferences = displayIfNoReferences;
        }

        public static SerializableDefinitionItem Dehydrate(int id, DefinitionItem item)
            => new(id,
                   item.Tags,
                   item.DisplayParts,
                   item.NameDisplayParts,
                   item.OriginationParts,
                   item.SourceSpans.SelectAsArray(ss => SerializableDocumentSpan.Dehydrate(ss)),
                   item.Properties,
                   item.DisplayableProperties,
                   item.DisplayIfNoReferences);

        public async ValueTask<DefinitionItem> RehydrateAsync(Solution solution, CancellationToken cancellationToken)
        {
            var sourceSpans = await SourceSpans.SelectAsArrayAsync(ss => ss.RehydrateAsync(solution, cancellationToken)).ConfigureAwait(false);

            return new DefinitionItem.DefaultDefinitionItem(
                Tags,
                DisplayParts,
                NameDisplayParts,
                OriginationParts,
                sourceSpans,
                Properties,
                DisplayableProperties,
                DisplayIfNoReferences);
        }
    }

    [DataContract]
    internal readonly struct SerializableSourceReferenceItem
    {
        [DataMember(Order = 0)]
        public readonly int DefinitionId;

        [DataMember(Order = 1)]
        public readonly SerializableDocumentSpan SourceSpan;

        [DataMember(Order = 2)]
        public readonly SymbolUsageInfo SymbolUsageInfo;

        [DataMember(Order = 3)]
        public readonly ImmutableDictionary<string, string> AdditionalProperties;

        public SerializableSourceReferenceItem(
            int definitionId,
            SerializableDocumentSpan sourceSpan,
            SymbolUsageInfo symbolUsageInfo,
            ImmutableDictionary<string, string> additionalProperties)
        {
            DefinitionId = definitionId;
            SourceSpan = sourceSpan;
            SymbolUsageInfo = symbolUsageInfo;
            AdditionalProperties = additionalProperties;
        }

        public static SerializableSourceReferenceItem Dehydrate(int definitionId, SourceReferenceItem item)
            => new(definitionId,
                   SerializableDocumentSpan.Dehydrate(item.SourceSpan),
                   item.SymbolUsageInfo,
                   item.AdditionalProperties);

        public async Task<SourceReferenceItem> RehydrateAsync(Solution solution, DefinitionItem definition, CancellationToken cancellationToken)
            => new(definition,
                   await SourceSpan.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false),
                   SymbolUsageInfo,
                   AdditionalProperties.ToImmutableDictionary(t => t.Key, t => t.Value));
    }
}
