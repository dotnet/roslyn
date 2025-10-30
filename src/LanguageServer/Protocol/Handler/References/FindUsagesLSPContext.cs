// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ReferenceHighlighting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Text.Adornments;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal sealed class FindUsagesLSPContext : FindUsagesContext
{
    private readonly IProgress<SumType<VSInternalReferenceItem, LSP.Location>[]> _progress;

    private readonly Workspace _workspace;
    private readonly Document _document;
    private readonly int _position;
    private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
    private readonly IGlobalOptionService _globalOptions;
    private readonly bool _supportsVSExtensions;

    /// <summary>
    /// Methods in FindUsagesLSPContext can be called by multiple threads concurrently. We need this semaphore to
    /// ensure that we aren't making concurrent modifications to data such as _id and _definitionToId.
    /// </summary>
    private readonly SemaphoreSlim _semaphore = new(1);

    private readonly Dictionary<DefinitionItem, int> _definitionToId = [];

    /// <summary>
    /// Keeps track of definitions that cannot be reported without references and which we have
    /// not yet found a reference for.
    /// </summary>
    private readonly Dictionary<int, SumType<VSInternalReferenceItem, LSP.Location>> _definitionsWithoutReference = [];

    /// <summary>
    /// Set of the locations we've found references at.  We may end up with multiple references
    /// being reported for the same location.  For example, this can happen in multi-targeting 
    /// scenarios when there are symbols in files linked into multiple projects.  Those symbols
    /// may have references that themselves are in linked locations, leading to multiple references
    /// found at different virtual locations that the user considers at the same physical location.
    /// For now we filter out these duplicates to not clutter the UI.  If LSP supports the ability
    /// to override an already reported VSReferenceItem, we could also reissue the item with the
    /// additional information about all the projects it is found in.
    /// </summary>
    private readonly HashSet<(string? filePath, TextSpan span)> _referenceLocations = [];

    /// <summary>
    /// We report the results in chunks. A batch, if it contains results, is reported every 0.5s.
    /// </summary>
    private readonly AsyncBatchingWorkQueue<SumType<VSInternalReferenceItem, LSP.Location>> _workQueue;

    // Unique identifier given to each definition and reference.
    private int _id = 0;

    public FindUsagesLSPContext(
        IProgress<SumType<VSInternalReferenceItem, LSP.Location>[]> progress,
        Workspace workspace,
        Document document,
        int position,
        IMetadataAsSourceFileService metadataAsSourceFileService,
        IAsynchronousOperationListener asyncListener,
        IGlobalOptionService globalOptions,
        bool supportsVSExtensions,
        CancellationToken cancellationToken)
    {
        _progress = progress;
        _workspace = workspace;
        _document = document;
        _position = position;
        _metadataAsSourceFileService = metadataAsSourceFileService;
        _globalOptions = globalOptions;
        _supportsVSExtensions = supportsVSExtensions;
        _workQueue = new AsyncBatchingWorkQueue<SumType<VSInternalReferenceItem, LSP.Location>>(
            DelayTimeSpan.Medium, ReportReferencesAsync, asyncListener, cancellationToken);
    }

    // After all definitions/references have been found, wait here until all results have been reported.
    public override async ValueTask OnCompletedAsync(CancellationToken cancellationToken)
        => await _workQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);

    public override async ValueTask OnDefinitionFoundAsync(DefinitionItem definition, CancellationToken cancellationToken)
    {
        using (await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
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
                definitionId: _id, id: _id, definition.SourceSpans.FirstOrNull(),
                definition.DisplayableProperties, definition.GetClassifiedText(),
                definition.Tags.GetFirstGlyph(), symbolUsageInfo: null, isWrittenTo: false, cancellationToken).ConfigureAwait(false);

            if (definitionItem != null)
            {
                // If a definition shouldn't be included in the results list if it doesn't have references, we
                // have to hold off on reporting it until later when we do find a reference.
                if (definition.DisplayIfNoReferences)
                {
                    _workQueue.AddWork(definitionItem.Value);
                }
                else
                {
                    _definitionsWithoutReference.Add(_id, definitionItem.Value);
                }
            }
        }
    }

    public override async ValueTask OnReferencesFoundAsync(IAsyncEnumerable<SourceReferenceItem> references, CancellationToken cancellationToken)
    {
        await foreach (var reference in references.ConfigureAwait(false))
        {
            using (await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // Each reference should be associated with a definition. If this somehow isn't the
                // case, we bail out early.
                if (!_definitionToId.TryGetValue(reference.Definition, out var definitionId))
                    continue;

                var documentSpan = reference.SourceSpan;
                var document = documentSpan.Document;

                // If this is reference to the same physical location we've already reported, just
                // filter this out.  it will clutter the UI to show the same places.
                if (!_referenceLocations.Add((document.FilePath, reference.SourceSpan.SourceSpan)))
                    continue;

                // If the definition hasn't been reported yet, add it to our list of references to report.
                if (_definitionsWithoutReference.TryGetValue(definitionId, out var definition))
                {
                    _workQueue.AddWork(definition);
                    _definitionsWithoutReference.Remove(definitionId);
                }

                // give this reference a fresh id.
                _id++;

                // Creating a new VSReferenceItem for the reference
                var referenceItem = await GenerateVSReferenceItemAsync(
                    definitionId, _id, reference.SourceSpan,
                    reference.AdditionalProperties, definitionText: null,
                    definitionGlyph: Glyph.None, reference.SymbolUsageInfo, reference.IsWrittenTo, cancellationToken).ConfigureAwait(false);

                if (referenceItem != null)
                    _workQueue.AddWork(referenceItem.Value);
            }
        }
    }

    private async Task<SumType<VSInternalReferenceItem, LSP.Location>?> GenerateVSReferenceItemAsync(
        int definitionId,
        int id,
        DocumentSpan? documentSpan,
        ImmutableArray<(string key, string value)> properties,
        ClassifiedTextElement? definitionText,
        Glyph definitionGlyph,
        SymbolUsageInfo? symbolUsageInfo,
        bool isWrittenTo,
        CancellationToken cancellationToken)
    {
        // Getting the text for the Text property. If we somehow can't compute the text, that means we're probably dealing with a metadata
        // reference, and those don't show up in the results list in Roslyn FAR anyway.
        var text = await ComputeTextAsync(definitionId, documentSpan, definitionText, isWrittenTo, cancellationToken).ConfigureAwait(false);
        if (text == null)
            return null;

        var location = await ComputeLocationAsync(documentSpan, cancellationToken).ConfigureAwait(false);

        return _supportsVSExtensions
            ? CreateVsReference(definitionId, id, text, documentSpan, properties, definitionText, definitionGlyph, symbolUsageInfo, location)
            : location;
    }

    private static SumType<VSInternalReferenceItem, LSP.Location>? CreateVsReference(
        int definitionId,
        int id,
        ClassifiedTextElement text,
        DocumentSpan? documentSpan,
        ImmutableArray<(string key, string value)> properties,
        ClassifiedTextElement? definitionText,
        Glyph definitionGlyph,
        SymbolUsageInfo? symbolUsageInfo,
        LSP.Location? location)
    {
        // TO-DO: The Origin property should be added once Rich-Nav is completed.
        // https://github.com/dotnet/roslyn/issues/42847
        var result = new VSInternalReferenceItem
        {
            DefinitionId = definitionId,
            DefinitionText = definitionText,    // Only definitions should have a non-null DefinitionText
            DefinitionIcon = new ImageElement(definitionGlyph.ToLSPImageId()),
            Location = location,
            DisplayPath = location?.DocumentUri.GetRequiredParsedUri().LocalPath,
            Id = id,
            Kind = symbolUsageInfo.HasValue ? ProtocolConversions.SymbolUsageInfoToReferenceKinds(symbolUsageInfo.Value) : [],
            ResolutionStatus = VSInternalResolutionStatusKind.ConfirmedAsReference,
            Text = text,
        };

        if (documentSpan is var (document, _, _))
        {
            result.DocumentName = document.Name;
            result.ProjectName = document.Project.Name;
        }

        foreach (var (key, value) in properties)
        {
            if (key == AbstractReferenceFinder.ContainingMemberInfoPropertyName)
                result.ContainingMember = value;
            else if (key == AbstractReferenceFinder.ContainingTypeInfoPropertyName)
                result.ContainingType = value;
        }

        return result;
    }

    private async Task<LSP.Location?> ComputeLocationAsync(DocumentSpan? documentSpan, CancellationToken cancellationToken)
    {
        // If we have no document span, our location may be in metadata.
        if (documentSpan != null)
        {
            // We do have a document span, so compute location normally.
            return await ProtocolConversions.DocumentSpanToLocationAsync(documentSpan.Value, cancellationToken).ConfigureAwait(false);
        }

        // If we have no document span, our location may be in metadata or may be a namespace.
        var symbol = await SymbolFinder.FindSymbolAtPositionAsync(_document, _position, cancellationToken).ConfigureAwait(false);
        if (symbol == null || symbol.Locations.IsEmpty || symbol.Kind is SymbolKind.Namespace)
        {
            // Either:
            // (1) We couldn't find the location in metadata and it's not in any of our known documents.
            // (2) The symbol is a namespace (and therefore has no location).
            return null;
        }

        if (symbol is IAliasSymbol aliasSymbol)
        {
            // If the location symbol is an alias symbol, we need to get the target symbol to find the original location in metadata.
            symbol = aliasSymbol.Target;
        }

        var options = _globalOptions.GetMetadataAsSourceOptions();
        var declarationFile = await _metadataAsSourceFileService.GetGeneratedFileAsync(
            _workspace, _document.Project, symbol, signaturesOnly: true, options: options, cancellationToken: cancellationToken).ConfigureAwait(false);

        var linePosSpan = declarationFile.IdentifierLocation.GetLineSpan().Span;

        if (string.IsNullOrEmpty(declarationFile.FilePath))
        {
            return null;
        }

        try
        {
            return new LSP.Location
            {
                DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(declarationFile.FilePath),
                Range = ProtocolConversions.LinePositionToRange(linePosSpan),
            };
        }
        catch (UriFormatException e) when (FatalError.ReportAndCatch(e))
        {
            // We might reach this point if the file path is formatted incorrectly.
            return null;
        }
    }

    private async Task<ClassifiedTextElement?> ComputeTextAsync(
        int? definitionId,
        DocumentSpan? documentSpan,
        ClassifiedTextElement? definitionText,
        bool isWrittenTo,
        CancellationToken cancellationToken)
    {
        // General case
        if (documentSpan != null)
        {
            var document = documentSpan.Value.Document;
            var options = _globalOptions.GetClassificationOptions(document.Project.Language);

            var classifiedSpansAndHighlightSpan = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(
                documentSpan.Value, classifiedSpans: null, options, cancellationToken).ConfigureAwait(false);

            var classifiedSpans = classifiedSpansAndHighlightSpan.ClassifiedSpans;
            var docText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var classifiedTextRuns = GetClassifiedTextRuns(_id, definitionId, documentSpan.Value, isWrittenTo, classifiedSpans, docText);

            return new ClassifiedTextElement([.. classifiedTextRuns]);
        }

        // Certain definitions may not have a DocumentSpan, such as namespace and metadata definitions
        if (_id == definitionId)
        {
            return definitionText;
        }

        return null;
    }

    private static ClassifiedTextRun[] GetClassifiedTextRuns(
        int id,
        int? definitionId,
        DocumentSpan documentSpan,
        bool isWrittenTo,
        ImmutableArray<ClassifiedSpan> classifiedSpans,
        SourceText docText)
    {
        using var _ = ArrayBuilder<ClassifiedTextRun>.GetInstance(out var classifiedTextRuns);
        foreach (var span in classifiedSpans)
        {
            // Default case: Don't highlight. For example, if the user invokes FAR on 'x' in 'var x = 1', then 'var',
            // '=', and '1' should not be highlighted.
            string? markerTagType = null;

            // Case 1: Highlight this span of text. For example, if the user invokes FAR on 'x' in 'var x = 1',
            // then 'x' should be highlighted.
            if (span.TextSpan == documentSpan.SourceSpan)
            {
                // Case 1a: Highlight a definition
                if (id == definitionId)
                {
                    markerTagType = ReferenceHighlightingConstants.DefinitionTagId;
                }
                // Case 1b: Highlight a written reference
                else if (isWrittenTo)
                {
                    markerTagType = ReferenceHighlightingConstants.WrittenReferenceTagId;
                }
                // Case 1c: Highlight a read reference
                else
                {
                    markerTagType = ReferenceHighlightingConstants.ReferenceTagId;
                }
            }

            classifiedTextRuns.Add(new ClassifiedTextRun(
                span.ClassificationType, docText.ToString(span.TextSpan), ClassifiedTextRunStyle.Plain, markerTagType));
        }

        return classifiedTextRuns.ToArray();
    }

    private ValueTask ReportReferencesAsync(ImmutableSegmentedList<SumType<VSInternalReferenceItem, LSP.Location>> referencesToReport, CancellationToken cancellationToken)
    {
        // We can report outside of the lock here since _progress is thread-safe.
        _progress.Report([.. referencesToReport]);
        return ValueTask.CompletedTask;
    }
}
