// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal interface IRemoteFindUsagesService
    {
        Task FindReferencesAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectIdArg,
            SerializableFindReferencesSearchOptions options,
            CancellationToken cancellationToken);

        Task FindImplementationsAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectIdArg,
            CancellationToken cancellationToken);
    }

    internal class FindUsagesServerCallback
    {
        private readonly Solution _solution;
        private readonly IFindUsagesContext _context;
        private readonly Dictionary<int, DefinitionItem> _idToDefinition = new Dictionary<int, DefinitionItem>();

        public FindUsagesServerCallback(Solution solution, IFindUsagesContext context)
        {
            _solution = solution;
            _context = context;
        }

        public Task AddItemsAsync(int count)
            => _context.ProgressTracker.AddItemsAsync(count);

        public Task ItemCompletedAsync()
            => _context.ProgressTracker.ItemCompletedAsync();

        public Task ReportMessageAsync(string message)
            => _context.ReportMessageAsync(message).AsTask();

        [Obsolete]
        public Task ReportProgressAsync(int current, int maximum)
            => _context.ReportProgressAsync(current, maximum).AsTask();

        public Task SetSearchTitleAsync(string title)
            => _context.SetSearchTitleAsync(title).AsTask();

        public Task OnDefinitionFoundAsync(SerializableDefinitionItem definition)
        {
            var id = definition.Id;
            var rehydrated = definition.Rehydrate(_solution);

            lock (_idToDefinition)
            {
                _idToDefinition.Add(id, rehydrated);
            }

            return _context.OnDefinitionFoundAsync(rehydrated).AsTask();
        }

        public Task OnReferenceFoundAsync(SerializableSourceReferenceItem reference)
            => _context.OnReferenceFoundAsync(reference.Rehydrate(_solution, GetDefinition(reference.DefinitionId))).AsTask();

        private DefinitionItem GetDefinition(int definitionId)
        {
            lock (_idToDefinition)
            {
                Contract.ThrowIfFalse(_idToDefinition.ContainsKey(definitionId));
                return _idToDefinition[definitionId];
            }
        }
    }

    internal class SerializableDocumentSpan
    {
        public DocumentId DocumentId;
        public TextSpan SourceSpan;

        public static SerializableDocumentSpan Dehydrate(DocumentSpan documentSpan)
            => new SerializableDocumentSpan
            {
                DocumentId = documentSpan.Document.Id,
                SourceSpan = documentSpan.SourceSpan,
            };

        public DocumentSpan Rehydrate(Solution solution)
            => new DocumentSpan(solution.GetDocument(DocumentId), SourceSpan);
    }

    internal class SerializableTaggedText
    {
        public string Tag;
        public string Text;
        public TaggedTextStyle Style;
        public string NavigationTarget;
        public string NavigationHint;

        public static SerializableTaggedText Dehydrate(TaggedText text)
            => new SerializableTaggedText
            {
                Tag = text.Tag,
                Text = text.Text,
                Style = text.Style,
                NavigationTarget = text.NavigationTarget,
                NavigationHint = text.NavigationHint,
            };

        public TaggedText Rehydrate()
            => new TaggedText(Tag, Text, Style, NavigationTarget, NavigationHint);
    }

    internal class SerializableDefinitionItem
    {
        public int Id;
        public string[] Tags;
        public SerializableTaggedText[] DisplayParts;
        public SerializableTaggedText[] NameDisplayParts;
        public SerializableTaggedText[] OriginationParts;
        public SerializableDocumentSpan[] SourceSpans;
        public (string key, string value)[] Properties;
        public (string key, string value)[] DisplayableProperties;
        public bool DisplayIfNoReferences;

        public static SerializableDefinitionItem Dehydrate(int id, DefinitionItem item)
            => new SerializableDefinitionItem
            {
                Id = id,
                Tags = item.Tags.ToArray(),
                DisplayParts = item.DisplayParts.Select(p => SerializableTaggedText.Dehydrate(p)).ToArray(),
                NameDisplayParts = item.NameDisplayParts.Select(p => SerializableTaggedText.Dehydrate(p)).ToArray(),
                OriginationParts = item.OriginationParts.Select(p => SerializableTaggedText.Dehydrate(p)).ToArray(),
                SourceSpans = item.SourceSpans.Select(ss => SerializableDocumentSpan.Dehydrate(ss)).ToArray(),
                Properties = item.Properties.Select(kvp => (kvp.Key, kvp.Value)).ToArray(),
                DisplayableProperties = item.DisplayableProperties.Select(kvp => (kvp.Key, kvp.Value)).ToArray(),
                DisplayIfNoReferences = item.DisplayIfNoReferences,
            };

        public DefinitionItem Rehydrate(Solution solution)
            => new DefinitionItem.DefaultDefinitionItem(
                Tags.ToImmutableArray(),
                DisplayParts.SelectAsArray(dp => dp.Rehydrate()),
                NameDisplayParts.SelectAsArray(dp => dp.Rehydrate()),
                OriginationParts.SelectAsArray(dp => dp.Rehydrate()),
                SourceSpans.SelectAsArray(ss => ss.Rehydrate(solution)),
                Properties.ToImmutableDictionary(t => t.key, t => t.value),
                DisplayableProperties.ToImmutableDictionary(t => t.key, t => t.value),
                DisplayIfNoReferences);
    }

    internal class SerializableSourceReferenceItem
    {
        public int DefinitionId;
        public SerializableDocumentSpan SourceSpan;
        public SerializableSymbolUsageInfo SymbolUsageInfo;
        public (string Key, string Value)[] AdditionalProperties;

        public static SerializableSourceReferenceItem Dehydrate(
            int definitionId, SourceReferenceItem item)
        {
            return new SerializableSourceReferenceItem
            {
                DefinitionId = definitionId,
                SourceSpan = SerializableDocumentSpan.Dehydrate(item.SourceSpan),
                SymbolUsageInfo = SerializableSymbolUsageInfo.Dehydrate(item.SymbolUsageInfo),
                AdditionalProperties = item.AdditionalProperties.Select(kvp => (kvp.Key, kvp.Value)).ToArray(),
            };
        }

        public SourceReferenceItem Rehydrate(Solution solution, DefinitionItem definition)
        {
            return new SourceReferenceItem(
                definition,
                SourceSpan.Rehydrate(solution),
                SymbolUsageInfo.Rehydrate(),
                AdditionalProperties.ToImmutableDictionary(t => t.Key, t => t.Value));
        }
    }
}
