// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using DocumentMap = MultiDictionary<Document, (SymbolAndProjectId symbolAndProjectId, IReferenceFinder finder)>;

    internal partial class FindReferencesSearchEngine
    {
        private async Task ProcessDocumentQueueAsync(
            Document document,
            DocumentMap.ValueSet documentQueue,
            CancellationToken cancellationToken)
        {
            await _progress.OnFindInDocumentStartedAsync(document, cancellationToken).ConfigureAwait(false);

            SemanticModel model = null;
            try
            {
                model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // start cache for this semantic model
                FindReferenceCache.Start(model);

                foreach (var symbolAndFinder in documentQueue)
                {
                    var symbol = symbolAndFinder.symbolAndProjectId;
                    var finder = symbolAndFinder.finder;

                    await ProcessDocumentAsync(document, symbol, finder, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                FindReferenceCache.Stop(model);

                await _progress.OnFindInDocumentCompletedAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }

        private static readonly Func<Document, ISymbol, string> s_logDocument = (d, s) =>
        {
            return (d.Name != null && s.Name != null) ? string.Format("{0} - {1}", d.Name, s.Name) : string.Empty;
        };

        private async Task ProcessDocumentAsync(
            Document document,
            SymbolAndProjectId symbolAndProjectId,
            IReferenceFinder finder,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ProcessDocumentAsync, s_logDocument, document, symbolAndProjectId.Symbol, cancellationToken))
            {
                try
                {
                    var references = await finder.FindReferencesInDocumentAsync(symbolAndProjectId, document, cancellationToken).ConfigureAwait(false);
                    foreach (var location in references)
                    {
                        await HandleLocationAsync(symbolAndProjectId, location, cancellationToken).ConfigureAwait(false);
                    }
                }
                finally
                {
                    await _progressTracker.ItemCompletedAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}