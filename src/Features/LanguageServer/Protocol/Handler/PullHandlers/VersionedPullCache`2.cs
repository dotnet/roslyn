// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Specialized cache used by the 'pull' LSP handlers.  Supports storing data to know when to tell a client
    /// that existing results can be reused, or if new results need to be computed.  Multiple keys can be used,
    /// with different computation costs to determine if the previous cached data is still valid.
    /// </summary>
    internal class VersionedPullCache<TCheapVersion, TExpensiveVersion>
    {
        private readonly string _uniqueKey;

        /// <summary>
        /// Lock to protect <see cref="_documentIdToLastResult"/> and <see cref="_nextDocumentResultId"/>.
        /// This enables this type to be used by request handlers that process requests concurrently.
        /// </summary>
        private readonly SemaphoreSlim _semaphore = new(1);

        /// <summary>
        /// Mapping of a document to the data used to make the last pull report which contains:
        /// <list type="bullet">
        ///   <item>The resultId reported to the client.</item>
        ///   <item>The TCheapVersion of the data that was used to calculate results.
        ///       <para>
        ///       Note that this version can change even when nothing has actually changed (for example, forking the 
        ///       LSP text, reloading the same project). So we additionally store:</para></item>
        ///   <item>A TExpensiveVersion (normally a checksum) checksum that will still allow us to reuse data even when
        ///   unimportant changes happen that trigger the cheap version change detection.</item>
        /// </list>
        /// This is used to determine if we need to re-calculate results.
        /// </summary>
        private readonly Dictionary<(Workspace workspace, DocumentId documentId), (string resultId, TCheapVersion cheapVersion, TExpensiveVersion expensiveVersion)> _documentIdToLastResult = new();

        /// <summary>
        /// The next available id to label results with.  Note that results are tagged on a per-document bases.  That
        /// way we can update results with the client with per-doc granularity.
        /// </summary>
        private long _nextDocumentResultId;

        public VersionedPullCache(string uniqueKey)
        {
            _uniqueKey = uniqueKey;
        }

        /// <summary>
        /// If results have changed since the last request this calculates and returns a new
        /// non-null resultId to use for subsequent computation and caches it.
        /// </summary>
        /// <param name="documentToPreviousResult">the resultIds the client sent us.</param>
        /// <param name="document">the document we are currently calculating results for.</param>
        /// <returns>Null when results are unchanged, otherwise returns a non-null new resultId.</returns>
        public async Task<string?> GetNewResultIdAsync(
            Dictionary<Document, PreviousPullResult> documentToPreviousResult,
            Document document,
            Func<Task<TCheapVersion>> computeCheapVersionAsync,
            Func<Task<TExpensiveVersion>> computeExpensiveVersionAsync,
            CancellationToken cancellationToken)
        {
            TCheapVersion cheapVersion;
            TExpensiveVersion expensiveVersion;

            var workspace = document.Project.Solution.Workspace;
            using (await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (documentToPreviousResult.TryGetValue(document, out var previousResult) &&
                    previousResult.PreviousResultId != null &&
                    _documentIdToLastResult.TryGetValue((workspace, document.Id), out var lastResult) &&
                    lastResult.resultId == previousResult.PreviousResultId)
                {
                    cheapVersion = await computeCheapVersionAsync().ConfigureAwait(false);
                    if (cheapVersion != null && cheapVersion.Equals(lastResult.cheapVersion))
                    {
                        // The client's resultId matches our cached resultId and the cheap version is an
                        // exact match for our current cheap version. We return early here to avoid calculating
                        // expensive versions as we know nothing is changed.
                        return null;
                    }

                    // The current cheap version does not match the last reported.  This may be because we've forked
                    // or reloaded a project, so fall back to calculating the full expensive version to determine if
                    // anything is actually changed.
                    expensiveVersion = await computeExpensiveVersionAsync().ConfigureAwait(false);
                    if (expensiveVersion != null && expensiveVersion.Equals(lastResult.expensiveVersion))
                    {
                        return null;
                    }
                }
                else
                {
                    // Client didn't give us a resultId or we have nothing cached
                    // We need to calculate new results and store what we calculated the results for.
                    cheapVersion = await computeCheapVersionAsync().ConfigureAwait(false);
                    expensiveVersion = await computeExpensiveVersionAsync().ConfigureAwait(false);
                }

                // Keep track of the results we reported here so that we can short-circuit producing results for
                // the same state of the world in the future.  Use a custom result-id per type (doc requests or workspace
                // requests) so that clients of one don't errantly call into the other.
                //
                // For example, a client getting document diagnostics should not ask for workspace diagnostics with the result-ids it got for
                // doc-diagnostics.  The two systems are different and cannot share results, or do things like report
                // what changed between each other.
                //
                // Note that we can safely update the map before computation as any cancellation or exception
                // during computation means that the client will never recieve this resultId and so cannot ask us for it.
                var newResultId = $"{_uniqueKey}:{_nextDocumentResultId++}";
                _documentIdToLastResult[(document.Project.Solution.Workspace, document.Id)] = (newResultId, cheapVersion, expensiveVersion);
                return newResultId;
            }
        }
    }
}
