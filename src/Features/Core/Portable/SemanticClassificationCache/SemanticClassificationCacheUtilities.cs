// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SemanticClassificationCache
{
    internal static class SemanticClassificationCacheUtilities
    {
        public static async Task<(DocumentKey documentKey, Checksum checksum)> GetDocumentKeyAndChecksumAsync(
            Document document, CancellationToken cancellationToken)
        {
            var project = document.Project;

            // We very intentionally persist this information against using a null 'parseOptionsChecksum'.  This way the
            // results will be valid and something we can lookup regardless of the project configuration.  In other
            // words, if we've cached the information when in the DEBUG state of the project, but we lookup when in the
            // RELEASE state, we'll still find the entry.  The data may be inaccurate, but that's ok as this is just for
            // temporary classifying until the real classifier takes over when the solution fully loads.
            var projectKey = new ProjectKey(SolutionKey.ToSolutionKey(project.Solution), project.Id, project.FilePath, project.Name, Checksum.Null);
            var documentKey = new DocumentKey(projectKey, document.Id, document.FilePath, document.Name);

            // We only checksum off of the contents of the file.  During load, we can't really compute any other
            // information since we don't necessarily know about other files, metadata, or dependencies.  So during
            // load, we allow for the previous semantic classifications to be used as long as the file contents match.
            var checksums = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);

            return (documentKey, checksums.Text);
        }

        public static async Task AddSemanticClassificationsAsync(
            Document document,
            TextSpan textSpan,
            IClassificationService classificationService,
            ArrayBuilder<ClassifiedSpan> classifiedSpans,
            CancellationToken cancellationToken)
        {
            var workspaceStatusService = document.Project.Solution.Workspace.Services.GetRequiredService<IWorkspaceStatusService>();

            // Importantly, we do not await/wait on the fullyLoadedStateTask.  We do not want to ever be waiting on work
            // that may end up touching the UI thread (As we can deadlock if GetTagsSynchronous waits on us).  Instead,
            // we only check if the Task is completed.  Prior to that we will assume we are still loading.  Once this
            // task is completed, we know that the WaitUntilFullyLoadedAsync call will have actually finished and we're
            // fully loaded.
            var isFullyLoadedTask = workspaceStatusService.IsFullyLoadedAsync(cancellationToken);
            var isFullyLoaded = isFullyLoadedTask.IsCompleted && isFullyLoadedTask.GetAwaiter().GetResult();

            // If we're not fully loaded try to read from the cache instead so that classifications appear up to date.
            // New code will not be semantically classified, but will eventually when the project fully loads.
            if (await TryAddSemanticClassificationsFromCacheAsync(document, textSpan, classifiedSpans, isFullyLoaded, cancellationToken).ConfigureAwait(false))
                return;

            // We need to special case Razor. Since the C# syntactic classifier doesn't run on their end, we need to
            // return both syntactic + semantic classifications. The cache already special-cases Razor so both types
            // of classifications are returned. However, if the cache doesn't return results, we need to recompute
            // all tokens.
            // Ideally, Razor will eventually run the C# syntactic classifier on their end and we can then remove
            // this special casing: https://github.com/dotnet/razor-tooling/issues/5850
            if (document.IsRazorDocument())
            {
                var spans = await Classifier.GetClassifiedSpansAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                classifiedSpans.AddRange(spans);
            }
            else
            {
                var options = ClassificationOptions.From(document.Project);
                await classificationService.AddSemanticClassificationsAsync(
                    document, textSpan, options, classifiedSpans, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<bool> TryAddSemanticClassificationsFromCacheAsync(
            Document document,
            TextSpan textSpan,
            ArrayBuilder<ClassifiedSpan> classifiedSpans,
            bool isFullyLoaded,
            CancellationToken cancellationToken)
        {
            // Don't use the cache if we're fully loaded.  We should just compute values normally.
            if (isFullyLoaded)
                return false;

            var semanticCacheService = document.Project.Solution.Workspace.Services.GetService<ISemanticClassificationCacheService>();
            if (semanticCacheService == null)
                return false;

            var result = await semanticCacheService.GetCachedSemanticClassificationsAsync(
                document, textSpan, cancellationToken).ConfigureAwait(false);
            if (result.IsDefault)
                return false;

            classifiedSpans.AddRange(result);
            return true;
        }
    }
}
