// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.CustomProtocol
{
    internal class FindUsagesLSPContext : FindUsagesContext
    {

        private const int MaxResultsChunkSize = 32;

        private int _id = 0;

        private readonly object _gate = new object();

        private readonly Dictionary<DefinitionItem, int> _definitionToId =
            new Dictionary<DefinitionItem, int>();

        private readonly List<VSReferenceItem> _resultsChunk =
            new List<VSReferenceItem>();

        private readonly IProgress<object[]> _progress;
        private readonly Document _document;
        private readonly int _position;
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

        public override CancellationToken CancellationToken { get; }

        public FindUsagesLSPContext(
            IProgress<object[]> progress,
            Document document,
            int position,
            IMetadataAsSourceFileService metadataAsSourceFileService,
            CancellationToken cancellationToken)
        {
            _progress = progress;
            _document = document;
            _position = position;
            _metadataAsSourceFileService = metadataAsSourceFileService;

            CancellationToken = cancellationToken;
        }

        public string? Message { get; private set; }
        public string? SearchTitle { get; private set; }

        public override Task ReportMessageAsync(string message)
        {
            Message = message;
            return Task.CompletedTask;
        }

        public override Task SetSearchTitleAsync(string title)
        {
            SearchTitle = title;
            return Task.CompletedTask;
        }

        public override Task OnCompletedAsync()
        {
            lock (_gate)
            {
                if (!_resultsChunk.IsEmpty())
                {
                    _progress.Report(_resultsChunk.ToArray());
                }

                return Task.CompletedTask;
            }
        }

        public async override Task OnDefinitionFoundAsync(DefinitionItem definition)
        {
            if (!_definitionToId.ContainsKey(definition))
            {
                // Assinging a new id to the definition
                _definitionToId.Add(definition, _id);

                // Creating a new VSReferenceItem for the definition
                var definitionItem = await GenerateVSReferenceItemAsync(
                    _id, definitionId: _id, _document, _position, definition.SourceSpans.FirstOrDefault(),
                    definition.DisplayableProperties, _metadataAsSourceFileService, definition.GetClassifiedText(),
                    symbolUsageInfo: null, CancellationToken).ConfigureAwait(false);

                if (definitionItem.Location != null)
                {
                    AddAndReportResultsIfAtMax(definitionItem);
                    _id++;
                }
            }
        }

        public async override Task OnReferenceFoundAsync(SourceReferenceItem reference)
        {
            if (_definitionToId.TryGetValue(reference.Definition, out var definitionId))
            {
                // Creating a new VSReferenceItem for the reference
                var referenceItem = await GenerateVSReferenceItemAsync(
                    _id, definitionId, _document, _position, reference.SourceSpan,
                    reference.AdditionalProperties, _metadataAsSourceFileService, definitionText: null,
                    reference.SymbolUsageInfo, CancellationToken).ConfigureAwait(false);

                if (referenceItem.Location != null)
                {
                    AddAndReportResultsIfAtMax(referenceItem);
                    _id++;
                }
            }
        }

        private void AddAndReportResultsIfAtMax(VSReferenceItem item)
        {
            lock (_gate)
            {
                _resultsChunk.Add(item);
                if (_resultsChunk.Count >= MaxResultsChunkSize)
                {
                    _progress.Report(_resultsChunk.ToArray());
                    _resultsChunk.Clear();
                }
            }
        }

        private static async Task<LSP.VSReferenceItem> GenerateVSReferenceItemAsync(
            int id,
            int? definitionId,
            Document document,
            int position,
            DocumentSpan documentSpan,
            ImmutableDictionary<string, string> properties,
            IMetadataAsSourceFileService metadataAsSourceFileService,
            ClassifiedTextElement? definitionText,
            SymbolUsageInfo? symbolUsageInfo,
            CancellationToken cancellationToken)
        {
            LSP.Location? location = null;

            // If we have no source span, our location may be in metadata.
            if (documentSpan == default)
            {
                var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (symbol != null && symbol.Locations != null && !symbol.Locations.IsEmpty && symbol.Locations.First().IsInMetadata)
                {
                    var declarationFile = await metadataAsSourceFileService.GetGeneratedFileAsync(
                        document.Project, symbol, allowDecompilation: false, cancellationToken).ConfigureAwait(false);

                    var linePosSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;
                    location = new LSP.Location
                    {
                        Uri = new Uri(declarationFile.FilePath),
                        Range = ProtocolConversions.LinePositionToRange(linePosSpan),
                    };
                }
            }
            else
            {
                location = await ProtocolConversions.DocumentSpanToLocationAsync(documentSpan, cancellationToken).ConfigureAwait(false);
            }

            // TO-DO: The Origin property should be added once Rich-Nav is completed.
            // https://github.com/dotnet/roslyn/issues/42847
            var result = new LSP.VSReferenceItem
            {
                ContainingMember = properties.TryGetValue(
                    AbstractReferenceFinder.ContainingMemberInfoPropertyName, out var referenceContainingMember) ? referenceContainingMember : null,
                ContainingType = properties.TryGetValue(
                    AbstractReferenceFinder.ContainingTypeInfoPropertyName, out var referenceContainingType) ? referenceContainingType : null,
                DefinitionId = definitionId,
                DefinitionText = definitionText,    // Only definitions should have a non-null DefinitionText
                DisplayPath = location?.Uri.LocalPath,
                DocumentName = documentSpan == default ? null : documentSpan.Document.Name,
                Id = id,
                Kind = symbolUsageInfo.HasValue ? ProtocolConversions.SymbolUsageInfoToReferenceKinds(symbolUsageInfo.Value) : new ReferenceKind[] { },
                Location = location,
                ProjectName = documentSpan == default ? null : documentSpan.Document.Project.Name,
                ResolutionStatus = ResolutionStatusKind.ConfirmedAsReference,
            };

            // Properly assigning the text property.
            if (id == definitionId)
            {
                result.Text = definitionText;
            }
            else if (documentSpan != default)
            {
                var classifiedSpansAndHighlightSpan = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(
                    documentSpan, cancellationToken).ConfigureAwait(false);
                var classifiedSpans = classifiedSpansAndHighlightSpan.ClassifiedSpans;
                var docText = await documentSpan.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                result.Text = new ClassifiedTextElement(
                    classifiedSpans.Select(cspan => new ClassifiedTextRun(cspan.ClassificationType, docText.ToString(cspan.TextSpan))));
            }

            return result;
        }
    }
}
