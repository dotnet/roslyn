// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed partial class RemoteSemanticClassificationService : BrokeredServiceBase, IRemoteSemanticClassificationService
    {
        internal sealed class Factory : FactoryBase<IRemoteSemanticClassificationService>
        {
            protected override IRemoteSemanticClassificationService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteSemanticClassificationService(arguments);
        }

        public ValueTask<SerializableClassifiedSpans> GetClassificationsAsync(
            PinnedSolutionInfo solutionInfo,
            DocumentId documentId,
            TextSpan span,
            ClassificationType type,
            ClassificationOptions options,
            StorageDatabase database,
            bool isFullyLoaded,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var document = solution.GetDocument(documentId) ?? await solution.GetSourceGeneratedDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
                Contract.ThrowIfNull(document);

                if (options.ForceFrozenPartialSemanticsForCrossProcessOperations)
                {
                    // Frozen partial semantics is not automatically passed to OOP, so enable it explicitly when desired
                    document = document.WithFrozenPartialSemantics(cancellationToken);
                }

                using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var temp);
                await AbstractClassificationService.AddClassificationsInCurrentProcessAsync(
                    document, span, type, options, temp, cancellationToken).ConfigureAwait(false);

                if (isFullyLoaded)
                {
                    // Once fully loaded, there's no need for us to keep around any of the data we cached in-memory
                    // during the time the solution was loading.
                    lock (_cachedData)
                        _cachedData.Clear();

                    // Enqueue this document into our work queue to fully classify and cache.
                    _workQueue.AddWork((document, type, options, database));
                }

                return SerializableClassifiedSpans.Dehydrate(temp.ToImmutable());
            }, cancellationToken);
        }
    }
}
