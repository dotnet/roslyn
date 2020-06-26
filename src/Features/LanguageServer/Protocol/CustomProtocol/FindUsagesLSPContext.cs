// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.CustomProtocol
{
    internal class FindUsagesLSPContext : FindUsagesContext
    {
        private readonly IProgress<object[]> _progress;
        private readonly Document _document;
        private readonly int _position;
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;

        /// <summary>
        /// Methods in FindUsagesLSPContext can be called by multiple threads concurrently.
        /// We need this sempahore to ensure that we aren't making concurrent
        /// modifications to data such as _id and _definitionToId.
        /// </summary>
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private readonly Dictionary<DefinitionItem, int> _definitionToId =
            new Dictionary<DefinitionItem, int>();

        /// <summary>
        /// Keeps track of definitions that cannot be reported without references and which we have
        /// not yet found a reference for.
        /// </summary>
        private readonly Dictionary<int, VSReferenceItem> _definitionsWithoutReference =
            new Dictionary<int, VSReferenceItem>();

        /// <summary>
        /// We report the results in chunks. A batch, if it contains results, is reported every 0.5s.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<VSReferenceItem> _workQueue;

        // Unique identifier given to each definition and reference.
        private int _id = 0;

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
            _workQueue = new AsyncBatchingWorkQueue<VSReferenceItem>(
                TimeSpan.FromMilliseconds(500), ReportReferencesAsync, cancellationToken);

            CancellationToken = cancellationToken;
        }

        // After all definitions/references have been found, wait here until all results have been reported.
        public override Task OnCompletedAsync() => _workQueue.WaitUntilCurrentBatchCompletesAsync();

        public override async Task OnDefinitionFoundAsync(DefinitionItem definition)
        {
            using (await _semaphore.DisposableWaitAsync(CancellationToken).ConfigureAwait(false))
            {
                if (_definitionToId.ContainsKey(definition))
                {
                    return;
                }

                // Assigning a new id to the definition
                _id++;
                _definitionToId.Add(definition, _id);

                // Creating a new VSReferenceItem for the definition
                var definitionItem = await GenerateVSReferenceItemAsync(
                    _id, definitionId: _id, _document, _position, definition.SourceSpans.FirstOrDefault(),
                    definition.DisplayableProperties, _metadataAsSourceFileService, definition.GetClassifiedText(),
                    symbolUsageInfo: null, CancellationToken).ConfigureAwait(false);

                if (definitionItem != null)
                {
                    // If a definition shouldn't be included in the results list if it doesn't have references, we
                    // have to hold off on reporting it until later when we do find a reference.
                    if (definition.DisplayIfNoReferences)
                    {
                        _workQueue.AddWork(definitionItem);
                    }
                    else
                    {
                        _definitionsWithoutReference.Add(definitionItem.Id, definitionItem);
                    }
                }
            }
        }

        public override async Task OnReferenceFoundAsync(SourceReferenceItem reference)
        {
            using (await _semaphore.DisposableWaitAsync(CancellationToken).ConfigureAwait(false))
            {
                // Each reference should be associated with a definition. If this somehow isn't the
                // case, we bail out early.
                if (!_definitionToId.TryGetValue(reference.Definition, out var definitionId))
                {
                    return;
                }

                // If the definition hasn't been reported yet, add it to our list of references to report.
                if (_definitionsWithoutReference.TryGetValue(definitionId, out var definition))
                {
                    _workQueue.AddWork(definition);
                    _definitionsWithoutReference.Remove(definitionId);
                }

                _id++;

                // Creating a new VSReferenceItem for the reference
                var referenceItem = await GenerateVSReferenceItemAsync(
                    _id, definitionId, _document, _position, reference.SourceSpan,
                    reference.AdditionalProperties, _metadataAsSourceFileService, definitionText: null,
                    reference.SymbolUsageInfo, CancellationToken).ConfigureAwait(false);

                if (referenceItem != null)
                {
                    _workQueue.AddWork(referenceItem);
                }
            }
        }

        private static async Task<LSP.VSReferenceItem?> GenerateVSReferenceItemAsync(
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
            var location = await ComputeLocationAsync(document, position, documentSpan, metadataAsSourceFileService, cancellationToken).ConfigureAwait(false);
            if (location == null)
            {
                return null;
            }

            // Getting the text for the Text property. If we somehow can't compute the text, that means we're probably dealing with a metadata
            // reference, and those don't show up in the results list in Roslyn FAR anyway.
            var text = await ComputeTextAsync(id, definitionId, documentSpan, definitionText, cancellationToken).ConfigureAwait(false);
            if (text == null)
            {
                return null;
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
                DisplayPath = location.Uri.LocalPath,
                DocumentName = documentSpan == default ? null : documentSpan.Document.Name,
                Id = id,
                Kind = symbolUsageInfo.HasValue ? ProtocolConversions.SymbolUsageInfoToReferenceKinds(symbolUsageInfo.Value) : Array.Empty<ReferenceKind>(),
                Location = location,
                ProjectName = documentSpan == default ? null : documentSpan.Document.Project.Name,
                ResolutionStatus = ResolutionStatusKind.ConfirmedAsReference,
                Text = text,
            };

            return result;

            static async Task<LSP.Location?> ComputeLocationAsync(
                Document document,
                int position,
                DocumentSpan documentSpan,
                IMetadataAsSourceFileService metadataAsSourceFileService,
                CancellationToken cancellationToken)
            {
                if (documentSpan != default)
                {
                    // We do have a source span, so compute location normally.
                    return await ProtocolConversions.DocumentSpanToLocationAsync(documentSpan, cancellationToken).ConfigureAwait(false);
                }

                // If we have no source span, our location may be in metadata.
                var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (symbol == null || symbol.Locations == null || symbol.Locations.IsEmpty || !symbol.Locations.First().IsInMetadata)
                {
                    // We couldn't find the location in metadata and it's not in any of our known documents.
                    return null;
                }

                var declarationFile = await metadataAsSourceFileService.GetGeneratedFileAsync(
                    document.Project, symbol, allowDecompilation: false, cancellationToken).ConfigureAwait(false);

                var linePosSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;

                if (string.IsNullOrEmpty(declarationFile.FilePath))
                {
                    return null;
                }

                try
                {
                    return new LSP.Location
                    {
                        Uri = ProtocolConversions.GetUriFromFilePath(declarationFile.FilePath),
                        Range = ProtocolConversions.LinePositionToRange(linePosSpan),
                    };
                }
                catch (UriFormatException e) when (FatalError.ReportWithoutCrash(e))
                {
                    // We might reach this point if the file path is formatted incorrectly.
                    return null;
                }
            }

            static async Task<object?> ComputeTextAsync(
                int id, int? definitionId,
                DocumentSpan documentSpan,
                ClassifiedTextElement? definitionText,
                CancellationToken cancellationToken)
            {
                if (id == definitionId)
                {
                    return definitionText;
                }
                else if (documentSpan != default)
                {
                    var classifiedSpansAndHighlightSpan = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(
                        documentSpan, cancellationToken).ConfigureAwait(false);
                    var classifiedSpans = classifiedSpansAndHighlightSpan.ClassifiedSpans;
                    var docText = await documentSpan.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    return new ClassifiedTextElement(
                        classifiedSpans.Select(cspan => new ClassifiedTextRun(cspan.ClassificationType, docText.ToString(cspan.TextSpan))));
                }

                return null;
            }
        }

        private Task ReportReferencesAsync(ImmutableArray<VSReferenceItem> referencesToReport, CancellationToken cancellationToken)
        {
            // We can report outside of the lock here since _progress is thread-safe.
            _progress.Report(referencesToReport.ToArray());
            return Task.CompletedTask;
        }
    }
}
