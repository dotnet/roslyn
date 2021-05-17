﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
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
            HashSet<(SymbolGroup group, ISymbol symbol, IReferenceFinder finder)> documentQueue,
            CancellationToken cancellationToken)
        {
            await _progress.OnFindInDocumentStartedAsync(document, cancellationToken).ConfigureAwait(false);

            SemanticModel? model = null;
            try
            {
                model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                // start cache for this semantic model
                FindReferenceCache.Start(model);

                foreach (var (group, symbol, finder) in documentQueue)
                    await ProcessDocumentAsync(document, model, group, symbol, finder, cancellationToken).ConfigureAwait(false);
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
            SemanticModel semanticModel,
            SymbolGroup group,
            ISymbol symbol,
            IReferenceFinder finder,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ProcessDocumentAsync, s_logDocument, document, symbol, cancellationToken))
            {
                try
                {
                    var references = await finder.FindReferencesInDocumentAsync(
                        symbol, document, semanticModel, _options, cancellationToken).ConfigureAwait(false);
                    foreach (var (_, location) in references)
                    {
                        await HandleLocationAsync(group, symbol, location, cancellationToken).ConfigureAwait(false);
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
