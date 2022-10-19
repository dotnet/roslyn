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
            DocumentId? priorityDocumentId,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Ignore projects that don't support compilation or don't even have the DesignerCategoryAttribute in it.
            if (!project.SupportsCompilation)
                yield break;

            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var designerCategoryType = compilation.DesignerCategoryAttributeType();
            if (designerCategoryType == null)
                yield break;

            // If there is a priority doc, then scan that first.
            var priorityDocument = priorityDocumentId == null ? null : project.GetDocument(priorityDocumentId);
            if (priorityDocument is { FilePath: not null })
            {
                var data = await ComputeDesignerAttributeDataAsync(designerCategoryType, priorityDocument, cancellationToken).ConfigureAwait(false);
                if (data != null)
                    yield return data.Value;
            }

            // now process the rest of the documents.
            using var _ = ArrayBuilder<Task<DesignerAttributeData?>>.GetInstance(out var tasks);
            foreach (var document in project.Documents)
            {
                if (document == priorityDocument || document.FilePath is null)
                    continue;

                tasks.Add(ComputeDesignerAttributeDataAsync(designerCategoryType, document, cancellationToken));
            }

            // Convert the tasks into one final stream we can read all the results from.
            await foreach (var dataOpt in tasks.ToImmutable().StreamAsync(cancellationToken).ConfigureAwait(false))
            {
                if (dataOpt != null)
                    yield return dataOpt.Value;
            }
        }

        private static async Task<DesignerAttributeData?> ComputeDesignerAttributeDataAsync(
            INamedTypeSymbol? designerCategoryType, Document document, CancellationToken cancellationToken)
        {
            try
            {
                Contract.ThrowIfNull(document.FilePath);

                var category = await DesignerAttributeHelpers.ComputeDesignerAttributeCategoryAsync(
                    designerCategoryType, document, cancellationToken).ConfigureAwait(false);

                // If there's no category (the common case) don't return anything.  The host itself will see no results
                // returned and can handle that case (for example, if a type previously had the attribute but doesn't
                // any longer).
                if (category == null)
                    return null;

                return new DesignerAttributeData(category, document.Id, document.FilePath);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                return null;
            }
        }
    }
}
