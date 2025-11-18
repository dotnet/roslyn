// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class RemoteSemanticClassificationService : BrokeredServiceBase, IRemoteSemanticClassificationService
{
    internal sealed class Factory : FactoryBase<IRemoteSemanticClassificationService>
    {
        protected override IRemoteSemanticClassificationService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteSemanticClassificationService(arguments);
    }

    public ValueTask<SerializableClassifiedSpans> GetClassificationsAsync(
        Checksum solutionChecksum,
        DocumentId documentId,
        ImmutableArray<TextSpan> spans,
        ClassificationType type,
        ClassificationOptions options,
        bool isFullyLoaded,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var document = solution.GetDocument(documentId) ?? await solution.GetSourceGeneratedDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(document);

            // Frozen partial semantics is not automatically passed to OOP, so enable it explicitly when desired
            document = options.FrozenPartialSemantics ? document.WithFrozenPartialSemantics(cancellationToken) : document;
            solution = document.Project.Solution;

            using var _ = Classifier.GetPooledList(out var temp);

            // Safe to do this.  The remote classification service only runs for C#/VB.  So we know we'll always
            // have this service and it will always be this type.
            var classificationService = (AbstractClassificationService)document.GetRequiredLanguageService<IClassificationService>();
            await classificationService.AddClassificationsAsync(
                document, spans, options, type, temp, cancellationToken).ConfigureAwait(false);

            if (isFullyLoaded)
            {
                // Once fully loaded, there's no need for us to keep around any of the data we cached in-memory
                // during the time the solution was loading.
                lock (_cachedData)
                    _cachedData.Clear();

                // Enqueue this document into our work queue to fully classify and cache.
                _workQueue.AddWork((document, type, options));
            }

            return SerializableClassifiedSpans.Dehydrate(temp);
        }, cancellationToken);
    }
}
