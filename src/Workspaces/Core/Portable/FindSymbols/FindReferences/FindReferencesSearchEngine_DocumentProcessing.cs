﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class FindReferencesSearchEngine
    {
        private async Task ProcessDocumentQueueAsync(
            Document document,
            HashSet<(ISymbol symbol, IReferenceFinder finder)> documentQueue)
        {
            await _progress.OnFindInDocumentStartedAsync(document).ConfigureAwait(false);

            SemanticModel? model = null;
            try
            {
                model = await document.GetRequiredSemanticModelAsync(_cancellationToken).ConfigureAwait(false);

                // start cache for this semantic model
                FindReferenceCache.Start(model);

                foreach (var (symbol, finder) in documentQueue)
                {
                    await ProcessDocumentAsync(document, model, symbol, finder).ConfigureAwait(false);
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
            SemanticModel semanticModel,
            ISymbol symbol,
            IReferenceFinder finder)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ProcessDocumentAsync, s_logDocument, document, symbol, _cancellationToken))
            {
                try
                {
                    var references = await finder.FindReferencesInDocumentAsync(
                        symbol, document, semanticModel, _options, _cancellationToken).ConfigureAwait(false);
                    foreach (var (_, location) in references)
                    {
                        await HandleLocationAsync(symbol, location).ConfigureAwait(false);
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
