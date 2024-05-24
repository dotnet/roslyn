// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.IntelliCode.Api;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.IntelliCode;

[Export(typeof(IIntentSourceProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class IntentSourceProvider(
    [ImportMany] IEnumerable<Lazy<IIntentProvider, IIntentProviderMetadata>> lazyIntentProviders,
    IGlobalOptionService globalOptions) : IIntentSourceProvider
{
    private readonly ImmutableDictionary<(string LanguageName, string IntentName), Lazy<IIntentProvider, IIntentProviderMetadata>> _lazyIntentProviders = CreateProviderMap(lazyIntentProviders);
    private readonly IGlobalOptionService _globalOptions = globalOptions;

    private static ImmutableDictionary<(string LanguageName, string IntentName), Lazy<IIntentProvider, IIntentProviderMetadata>> CreateProviderMap(
        IEnumerable<Lazy<IIntentProvider, IIntentProviderMetadata>> lazyIntentProviders)
    {
        return lazyIntentProviders.ToImmutableDictionary(
            provider => (provider.Metadata.LanguageName, provider.Metadata.IntentName),
            provider => provider);
    }

    public async Task<ImmutableArray<IntentSource>> ComputeIntentsAsync(IntentRequestContext intentRequestContext, CancellationToken cancellationToken)
    {
        var currentDocument = intentRequestContext.CurrentSnapshotSpan.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (currentDocument == null)
        {
            throw new ArgumentException("could not retrieve document for request snapshot");
        }

        var languageName = currentDocument.Project.Language;
        if (!_lazyIntentProviders.TryGetValue((LanguageName: languageName, IntentName: intentRequestContext.IntentName), out var provider))
        {
            Logger.Log(FunctionId.Intellicode_UnknownIntent, KeyValueLogMessage.Create(LogType.UserAction, m =>
            {
                m["intent"] = intentRequestContext.IntentName;
                m["language"] = languageName;
            }));

            return [];
        }

        var currentText = await currentDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var originalDocument = currentDocument.WithText(currentText.WithChanges(intentRequestContext.PriorTextEdits));

        var selectionTextSpan = intentRequestContext.PriorSelection;

        var results = await provider.Value.ComputeIntentAsync(
            originalDocument,
            selectionTextSpan,
            currentDocument,
            new IntentDataProvider(
                intentRequestContext.IntentData,
                _globalOptions.CreateProvider()),
            cancellationToken).ConfigureAwait(false);

        if (results.IsDefaultOrEmpty)
        {
            return [];
        }

        using var _ = ArrayBuilder<IntentSource>.GetInstance(out var convertedResults);
        foreach (var result in results)
        {
            var convertedIntent = await ConvertToIntelliCodeResultAsync(result, originalDocument, currentDocument, cancellationToken).ConfigureAwait(false);
            convertedResults.AddIfNotNull(convertedIntent);
        }

        return convertedResults.ToImmutableAndClear();
    }

    private static async Task<IntentSource?> ConvertToIntelliCodeResultAsync(
        IntentProcessorResult processorResult,
        Document originalDocument,
        Document currentDocument,
        CancellationToken cancellationToken)
    {
        var newSolution = processorResult.Solution;
        // Merge linked file changes so all linked files have the same text changes.
        newSolution = await newSolution.WithMergedLinkedFileChangesAsync(originalDocument.Project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);

        using var _ = PooledDictionary<DocumentId, ImmutableArray<TextChange>>.GetInstance(out var results);
        foreach (var changedDocumentId in processorResult.ChangedDocuments)
        {
            // Calculate the text changes by comparing the solution with intent applied to the current solution (not to be confused with the original solution, the one prior to intent detection).
            var docChanges = await GetTextChangesForDocumentAsync(newSolution, currentDocument.Project.Solution, changedDocumentId, cancellationToken).ConfigureAwait(false);
            if (docChanges != null)
            {
                results[changedDocumentId] = docChanges.Value;
            }
        }

        return new IntentSource(processorResult.Title, processorResult.ActionName, results.ToImmutableDictionary());
    }

    private static async Task<ImmutableArray<TextChange>?> GetTextChangesForDocumentAsync(
        Solution changedSolution,
        Solution currentSolution,
        DocumentId changedDocumentId,
        CancellationToken cancellationToken)
    {
        var changedDocument = changedSolution.GetRequiredDocument(changedDocumentId);
        var currentDocument = currentSolution.GetRequiredDocument(changedDocumentId);

        var textDiffService = changedSolution.Services.GetRequiredService<IDocumentTextDifferencingService>();
        // Compute changes against the current version of the document.
        var textDiffs = await textDiffService.GetTextChangesAsync(currentDocument, changedDocument, cancellationToken).ConfigureAwait(false);
        if (textDiffs.IsEmpty)
        {
            return null;
        }

        return textDiffs;
    }
}
