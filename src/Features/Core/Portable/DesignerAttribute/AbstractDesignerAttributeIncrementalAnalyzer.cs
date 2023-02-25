// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    [ExportWorkspaceService(typeof(IDesignerAttributeDiscoveryService)), Shared]
    internal sealed partial class DesignerAttributeDiscoveryService : IDesignerAttributeDiscoveryService
    {
        /// <summary>
        /// Protects mutable state in this type.
        /// </summary>
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);

        /// <summary>
        /// Keep track of the last information we reported.  We will avoid notifying the host if we recompute and these
        /// don't change.
        /// </summary>
        private readonly ConcurrentDictionary<DocumentId, (string? category, VersionStamp projectVersion)> _documentToLastReportedInformation = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DesignerAttributeDiscoveryService()
        {
        }

        public async ValueTask ProcessSolutionAsync(
            Solution solution,
            DocumentId? priorityDocumentId,
            IDesignerAttributeDiscoveryService.ICallback callback,
            CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // Remove any documents that are now gone.
                foreach (var docId in _documentToLastReportedInformation.Keys)
                {
                    if (!solution.ContainsDocument(docId))
                        _documentToLastReportedInformation.TryRemove(docId, out _);
                }

                // Handle the priority doc first.
                var priorityDocument = solution.GetDocument(priorityDocumentId);
                if (priorityDocument != null)
                    await ProcessProjectAsync(priorityDocument.Project, priorityDocument, callback, cancellationToken).ConfigureAwait(false);

                // Wait a little after the priority document and process the rest at a lower priority.
                await Task.Delay(DelayTimeSpan.Short, cancellationToken).ConfigureAwait(false);

                // Process the rest of the projects in dependency order so that their data is ready when we hit the 
                // projects that depend on them.
                var dependencyGraph = solution.GetProjectDependencyGraph();
                foreach (var projectId in dependencyGraph.GetTopologicallySortedProjects(cancellationToken))
                {
                    if (projectId != priorityDocumentId?.ProjectId)
                        await ProcessProjectAsync(solution.GetRequiredProject(projectId), specificDocument: null, callback, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task ProcessProjectAsync(
            Project project,
            Document? specificDocument,
            IDesignerAttributeDiscoveryService.ICallback callback,
            CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation)
                return;

            // Defer expensive work until it's actually needed.
            var lazyProjectVersion = AsyncLazy.Create(project.GetSemanticVersionAsync, cacheResult: true);
            var lazyDesignerCategoryType = AsyncLazy.Create(
                async cancellationToken =>
                {
                    var firstDocument = project.Documents.FirstOrDefault();
                    if (firstDocument is null)
                        return null;

                    // we don't want to run source generators just to find out if the project has the designer category
                    // attribute.  So freeze the project and try to get the type off of that.
                    var frozenProject = firstDocument.WithFrozenPartialSemantics(cancellationToken).Project;

                    var compilation = await frozenProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                    return compilation.DesignerCategoryAttributeType();
                },
                cacheResult: true);

            await ScanForDesignerCategoryUsageAsync(
                project, specificDocument, callback, lazyProjectVersion, lazyDesignerCategoryType, cancellationToken).ConfigureAwait(false);

            // If we scanned just a specific document in the project, now scan the rest of the files.
            if (specificDocument != null)
                await ScanForDesignerCategoryUsageAsync(project, specificDocument: null, callback, lazyProjectVersion, lazyDesignerCategoryType, cancellationToken).ConfigureAwait(false);
        }

        private async Task ScanForDesignerCategoryUsageAsync(
            Project project,
            Document? specificDocument,
            IDesignerAttributeDiscoveryService.ICallback callback,
            AsyncLazy<VersionStamp> lazyProjectVersion,
            AsyncLazy<INamedTypeSymbol?> lazyDesignerCategoryType,
            CancellationToken cancellationToken)
        {
            // Now get all the values that actually changed and notify VS about them. We don't need
            // to tell it about the ones that didn't change since that will have no effect on the
            // user experience.
            var changedData = await ComputeChangedDataAsync(
                project, specificDocument, lazyProjectVersion, lazyDesignerCategoryType, cancellationToken).ConfigureAwait(false);

            // Only bother reporting non-empty information to save an unnecessary RPC.
            if (!changedData.IsEmpty)
                await callback.ReportDesignerAttributeDataAsync(changedData.SelectAsArray(d => d.data), cancellationToken).ConfigureAwait(false);

            // Now, keep track of what we've reported to the host so we won't report unchanged files in the future. We
            // do this after the report has gone through as we want to make sure that if it cancels for any reason we
            // don't hold onto values that may not have made it all the way to the project system.
            foreach (var (data, projectVersion) in changedData)
                _documentToLastReportedInformation[data.DocumentId] = (data.Category, projectVersion);
        }

        private async Task<ImmutableArray<(DesignerAttributeData data, VersionStamp version)>> ComputeChangedDataAsync(
            Project project,
            Document? specificDocument,
            AsyncLazy<VersionStamp> lazyProjectVersion,
            AsyncLazy<INamedTypeSymbol?> lazyDesignerCategoryType,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<(DesignerAttributeData data, VersionStamp version)>.GetInstance(out var results);
            foreach (var document in project.Documents)
            {
                // If we're only analyzing a specific document, then skip the rest.
                if (specificDocument != null && document != specificDocument)
                    continue;

                // If we don't have a path for this document, we cant proceed with it.
                // We need that path to inform the project system which file we're referring to.
                if (document.FilePath == null)
                    continue;

                // If nothing has changed at the top level between the last time we analyzed this document and now, then
                // no need to analyze again.
                var projectVersion = await lazyProjectVersion.GetValueAsync(cancellationToken).ConfigureAwait(false);
                if (_documentToLastReportedInformation.TryGetValue(document.Id, out var existingInfo) &&
                    existingInfo.projectVersion == projectVersion)
                {
                    continue;
                }

                var data = await ComputeDesignerAttributeDataAsync(document).ConfigureAwait(false);
                if (data.Category != existingInfo.category)
                    results.Add((data, projectVersion));
            }

            return results.ToImmutable();

            async Task<DesignerAttributeData> ComputeDesignerAttributeDataAsync(Document document)
            {
                Contract.ThrowIfNull(document.FilePath);

                // We either haven't computed the designer info, or our data was out of date.  We need
                // So recompute here.  Figure out what the current category is, and if that's different
                // from what we previously stored.
                var category = await DesignerAttributeHelpers.ComputeDesignerAttributeCategoryAsync(
                    lazyDesignerCategoryType, document, cancellationToken).ConfigureAwait(false);

                return new DesignerAttributeData
                {
                    Category = category,
                    DocumentId = document.Id,
                    FilePath = document.FilePath,
                };
            }
        }
    }
}
