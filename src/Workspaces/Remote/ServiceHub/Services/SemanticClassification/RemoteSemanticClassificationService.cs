// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteSemanticClassificationService : BrokeredServiceBase, IRemoteSemanticClassificationService
    {
        internal sealed class Factory : FactoryBase<IRemoteSemanticClassificationService>
        {
            protected override IRemoteSemanticClassificationService CreateService(IServiceProvider serviceProvider, IServiceBroker serviceBroker)
                => new RemoteSemanticClassificationService(serviceProvider, serviceBroker);
        }

        public RemoteSemanticClassificationService(IServiceProvider serviceProvider, IServiceBroker serviceBroker)
            : base(serviceProvider, serviceBroker, clientDisconnectedSource: null)
        {
        }

        public ValueTask<SerializableClassifiedSpans> GetSemanticClassificationsAsync(
            PinnedSolutionInfo solutionInfo, DocumentId documentId,
            TextSpan span, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);

                    using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var temp);
                    await AbstractClassificationService.AddSemanticClassificationsInCurrentProcessAsync(
                        document, span, temp, cancellationToken).ConfigureAwait(false);

                    return SerializableClassifiedSpans.Dehydrate(temp.ToImmutable());
                }
            }, cancellationToken);
        }
    }
}
