// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class FindReferencesSearchEngine
    {
        private async Task ProcessDocumentQueueAsync(
            Document document,
            List<ValueTuple<ISymbol, IReferenceFinder>> documentQueue,
            ProgressWrapper wrapper)
        {
            this.progress.OnFindInDocumentStarted(document);

            SemanticModel model = null;
            try
            {
                model = await document.GetSemanticModelAsync(this.cancellationToken).ConfigureAwait(false);

                // start cache for this semantic model
                FindReferenceCache.Start(model);

#if PARALLEL
                Roslyn.Utilities.TaskExtensions.RethrowIncorrectAggregateExceptions(cancellationToken, () =>
                    {
                        documentQueue.AsParallel().WithCancellation(cancellationToken).ForAll(symbolAndFinder =>
                        {
                            var symbol = symbolAndFinder.Item1;
                            var finder = symbolAndFinder.Item2;

                            ProcessDocument(document, symbol, finder, wrapper);
                        });
                    });
#else
                foreach (var symbolAndFinder in documentQueue)
                {
                    var symbol = symbolAndFinder.Item1;
                    var finder = symbolAndFinder.Item2;

                    await ProcessDocumentAsync(document, symbol, finder, wrapper).ConfigureAwait(false);
                }
#endif
            }
            finally
            {
                FindReferenceCache.Stop(model);

                this.progress.OnFindInDocumentCompleted(document);
            }
        }

        private static readonly Func<Document, ISymbol, string> logDocument = (d, s) =>
        {
            return (d.Name != null && s.Name != null) ? string.Format("{0} - {1}", d.Name, s.Name) : string.Empty;
        };

        private async Task ProcessDocumentAsync(
            Document document,
            ISymbol symbol,
            IReferenceFinder finder,
            ProgressWrapper wrapper)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ProcessDocumentAsync, logDocument, document, symbol, this.cancellationToken))
            {
                try
                {
                    var references = await finder.FindReferencesInDocumentAsync(symbol, document, cancellationToken).ConfigureAwait(false) ?? SpecializedCollections.EmptyEnumerable<ReferenceLocation>();
                    foreach (var location in references)
                    {
                        HandleLocation(symbol, location);
                    }
                }
                finally
                {
                    wrapper.Increment();
                }
            }
        }
    }
}