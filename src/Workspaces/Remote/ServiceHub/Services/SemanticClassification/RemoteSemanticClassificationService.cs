// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteSemanticClassificationService : BrokeredServiceBase, IRemoteSemanticClassificationService
    {
        internal sealed class Factory : FactoryBase<IRemoteSemanticClassificationService>
        {
            protected override IRemoteSemanticClassificationService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteSemanticClassificationService(arguments);
        }

        public RemoteSemanticClassificationService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
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

                if (options.ForceFrozenPartialSemanticsForCrossProcessOperations)
                {
                    // Frozen partial semantics is not automatically passed to OOP, so enable it explicitly when desired
                    document = document.WithFrozenPartialSemantics(cancellationToken);
                }

                var cachingService = GetWorkspaceServices().GetRequiredService<IClassificationCachingService>();
                return await cachingService.GetClassificationsAsync(document, spans, type, options, isFullyLoaded, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask<SerializableClassifiedSpans?> GetCachedClassificationsAsync(DocumentKey documentKey, ImmutableArray<TextSpan> textSpans, ClassificationType type, Checksum checksum, CancellationToken cancellationToken)
        {
            return RunServiceAsync(
                async cancellationToken =>
                {
                    var workspaceServices = GetWorkspaceServices();
                    var cachingService = workspaceServices.GetRequiredService<IClassificationCachingService>();
                    return await cachingService.GetCachedClassificationsAsync(workspaceServices, documentKey, textSpans, type, checksum, cancellationToken).ConfigureAwait(false);
                },
                cancellationToken);
        }
    }
}
