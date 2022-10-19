// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.IntelliCode.Api;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.IntelliCode
{
    [Export(typeof(IIntentSourceProvider)), Shared]
    internal class IntentSourceProvider : IIntentSourceProvider
    {
        private readonly ImmutableDictionary<(string LanguageName, string IntentName), Lazy<IIntentProvider, IIntentProviderMetadata>> _lazyIntentProviders;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IntentSourceProvider([ImportMany] IEnumerable<Lazy<IIntentProvider, IIntentProviderMetadata>> lazyIntentProviders)
        {
            _lazyIntentProviders = CreateProviderMap(lazyIntentProviders);
        }

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

                return ImmutableArray<IntentSource>.Empty;
            }

            var currentText = await currentDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var originalDocument = currentDocument.WithText(currentText.WithChanges(intentRequestContext.PriorTextEdits));

            var selectionTextSpan = intentRequestContext.PriorSelection;

            var results = await provider.Value.ComputeIntentAsync(
                originalDocument,
                selectionTextSpan,
                currentDocument,
                new IntentDataProvider(intentRequestContext.IntentData),
                cancellationToken).ConfigureAwait(false);
            if (results.IsDefaultOrEmpty)
            {
                return ImmutableArray<IntentSource>.Empty;
            }

            using var _ = ArrayBuilder<IntentSource>.GetInstance(out var convertedResults);
            foreach (var result in results)
            {
                var convertedIntent = await ConvertToIntelliCodeResultAsync(result, originalDocument, currentDocument, cancellationToken).ConfigureAwait(false);
                convertedResults.AddIfNotNull(convertedIntent);
            }

            return convertedResults.ToImmutable();
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

            // For now we only support changes to the current document.  Everything else is dropped.
            var changedDocument = newSolution.GetRequiredDocument(currentDocument.Id);

            var textDiffService = newSolution.Workspace.Services.GetRequiredService<IDocumentTextDifferencingService>();
            // Compute changes against the current version of the document.
            var textDiffs = await textDiffService.GetTextChangesAsync(currentDocument, changedDocument, cancellationToken).ConfigureAwait(false);
            if (textDiffs.IsEmpty)
            {
                return null;
            }

            return new IntentSource(processorResult.Title, textDiffs, processorResult.ActionName);
        }
    }
}
