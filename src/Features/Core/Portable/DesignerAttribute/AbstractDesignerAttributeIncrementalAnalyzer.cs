// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    [ExportWorkspaceService(typeof(IDesignerAttributeDiscoveryService)), Shared]
    internal sealed partial class DesignerAttributeDiscoveryService : IDesignerAttributeDiscoveryService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DesignerAttributeDiscoveryService()
        {
        }

        public async IAsyncEnumerable<DesignerAttributeData> ProcessProjectAsync(
            Project project,
            DocumentId? specificDocumentId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation)
                yield break;

            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var designerCategoryType = compilation.DesignerCategoryAttributeType();
            if (designerCategoryType == null)
                yield break;

            var specificDocument = project.GetDocument(specificDocumentId);
            await foreach (var item in ScanForDesignerCategoryUsageAsync(
                project, specificDocument, designerCategoryType, cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }

            // If we scanned just a specific document in the project, now scan the rest of the files.
            if (specificDocument != null)
            {
                await foreach (var item in ScanForDesignerCategoryUsageAsync(
                    project, specificDocument: null, designerCategoryType, cancellationToken).ConfigureAwait(false))
                {
                    yield return item;
                }
            }
        }

        private static async IAsyncEnumerable<DesignerAttributeData> ScanForDesignerCategoryUsageAsync(
            Project project,
            Document? specificDocument,
            INamedTypeSymbol designerCategoryType,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Kick off the work in parallel across the documents of interest.
            using var _1 = ArrayBuilder<Task<DesignerAttributeData?>>.GetInstance(out var tasks);
            foreach (var document in project.Documents)
            {
                // If we're only analyzing a specific document, then skip the rest.
                if (specificDocument != null && document != specificDocument)
                    continue;

                // If we don't have a path for this document, we cant proceed with it.
                // We need that path to inform the project system which file we're referring to.
                if (document.FilePath == null)
                    continue;

                tasks.Add(ComputeDesignerAttributeDataAsync(designerCategoryType, document, cancellationToken));
            }

            // Convert the tasks into one final stream we can read all the results from.
            await foreach (var dataOpt in tasks.ToImmutable().StreamAsync(cancellationToken).ConfigureAwait(false))
            {
                if (dataOpt is null)
                    continue;

                yield return dataOpt.Value;
            }
        }

        private static async Task<DesignerAttributeData?> ComputeDesignerAttributeDataAsync(
            INamedTypeSymbol? designerCategoryType, Document document, CancellationToken cancellationToken)
        {
            try
            {
                Contract.ThrowIfNull(document.FilePath);

                // We either haven't computed the designer info, or our data was out of date.  We need
                // So recompute here.  Figure out what the current category is, and if that's different
                // from what we previously stored.
                var category = await DesignerAttributeHelpers.ComputeDesignerAttributeCategoryAsync(
                    designerCategoryType, document, cancellationToken).ConfigureAwait(false);

                return new DesignerAttributeData
                {
                    Category = category,
                    DocumentId = document.Id,
                    FilePath = document.FilePath,
                };
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return null;
            }
        }
    }
}
