// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
            DocumentMap.ValueSet documentQueue)
        {
            await _progress.OnFindInDocumentStartedAsync(document).ConfigureAwait(false);

            SemanticModel model = null;
            try
            {
                model = await document.GetSemanticModelAsync(_cancellationToken).ConfigureAwait(false);

                // start cache for this semantic model
                FindReferenceCache.Start(model);

                foreach (var symbolAndFinder in documentQueue)
                {
                    var symbol = symbolAndFinder.symbolAndProjectId;
                    var finder = symbolAndFinder.finder;

                    await ProcessDocumentAsync(document, symbol, finder).ConfigureAwait(false);
                }
            }
            finally
            {
                FindReferenceCache.Stop(model);

                await _progress.OnFindInDocumentCompletedAsync(document).ConfigureAwait(false);
            }
        }

        private static readonly Func<Document, ISymbol, string> s_logDocument = (d, s) =>
        {
            return (d.Name != null && s.Name != null) ? string.Format("{0} - {1}", d.Name, s.Name) : string.Empty;
        };

        private async Task ProcessDocumentAsync(
            Document document,
            SymbolAndProjectId symbolAndProjectId,
            IReferenceFinder finder)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ProcessDocumentAsync, s_logDocument, document, symbolAndProjectId.Symbol, _cancellationToken))
            {
                try
                {
                    var references = await finder.FindReferencesInDocumentAsync(symbolAndProjectId, document, _cancellationToken).ConfigureAwait(false);
                    foreach (var location in references)
                    {
                        await HandleLocationAsync(symbolAndProjectId, location).ConfigureAwait(false);
                    }
                }
                finally
                {
                    await _progressTracker.ItemCompletedAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
