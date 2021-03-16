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
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Intents
{
    [Export(typeof(IIntentsEditsSource)), Shared]
    internal class IntentEditsSource : IIntentsEditsSource
    {
        private readonly ImmutableDictionary<(string LanguageName, string IntentName), Lazy<IIntentProvider, IIntentProviderMetadata>> _lazyIntentProviders;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IntentEditsSource([ImportMany] IEnumerable<Lazy<IIntentProvider, IIntentProviderMetadata>> lazyIntentProviders)
        {
            _lazyIntentProviders = CreateProviderMap(lazyIntentProviders);
        }

        private static ImmutableDictionary<(string LanguageName, string IntentName), Lazy<IIntentProvider, IIntentProviderMetadata>> CreateProviderMap(IEnumerable<Lazy<IIntentProvider, IIntentProviderMetadata>> lazyIntentProviders)
        {
            return lazyIntentProviders.ToImmutableDictionary(provider => (provider.Metadata.LanguageName, provider.Metadata.IntentName), provider => provider);
        }

        public async Task<IntentResult?> ComputeEditsAsync(IntentRequestContext intentRequestContext, CancellationToken cancellationToken)
        {
            var originalDocument = intentRequestContext.SnapshotBeforeIntent.GetOpenDocumentInCurrentContextWithChanges();
            var currentDocument = intentRequestContext.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();

            Contract.ThrowIfNull(originalDocument);
            Contract.ThrowIfNull(currentDocument);
            Contract.ThrowIfFalse(originalDocument.Id == currentDocument.Id);

            var languageName = originalDocument.Project.Language;
            if (!_lazyIntentProviders.TryGetValue((LanguageName: languageName, IntentName: intentRequestContext.IntentName), out var provider))
            {
                Logger.Log(FunctionId.Intellicode_UnknownIntent, KeyValueLogMessage.Create(LogType.UserAction, m =>
                {
                    m["intent"] = intentRequestContext.IntentName;
                    m["language"] = languageName;
                }));

                return null;
            }

            var selectionTextSpan = intentRequestContext.Selection.ToTextSpan();
            var newSolution = await provider.Value.ComputeActionForIntentAsync(originalDocument, selectionTextSpan, currentDocument, intentRequestContext.IntentData, cancellationToken).ConfigureAwait(false);
            if (newSolution == null)
            {
                return null;
            }

            // Merge linked file changes so all linked files have the same text changes.
            newSolution = await newSolution.WithMergedLinkedFileChangesAsync(originalDocument.Project.Solution, cancellationToken: cancellationToken).ConfigureAwait(false);
            var changes = newSolution.GetChanges(originalDocument.Project.Solution);

            // For now we only support changes to the current document.  Everything else is dropped.
            var changedDocument = newSolution.GetRequiredDocument(currentDocument.Id);

            var textDiffService = newSolution.Workspace.Services.GetRequiredService<IDocumentTextDifferencingService>();
            // Compute changes against the current version of the document.
            var textDiffs = await textDiffService.GetTextChangesAsync(currentDocument, changedDocument, cancellationToken).ConfigureAwait(false);
            if (textDiffs.IsEmpty)
            {
                return null;
            }

            return new IntentResult(textDiffs);
        }
    }
}
