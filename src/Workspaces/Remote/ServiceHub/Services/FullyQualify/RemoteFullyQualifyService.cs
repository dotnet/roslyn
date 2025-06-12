// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes.FullyQualify;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteFullyQualifyService : BrokeredServiceBase, IRemoteFullyQualifyService
{
    internal sealed class Factory : FactoryBase<IRemoteFullyQualifyService>
    {
        protected override IRemoteFullyQualifyService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteFullyQualifyService(arguments);
    }

    public RemoteFullyQualifyService(in ServiceConstructionArguments arguments)
        : base(arguments)
    {
    }

    public ValueTask<FullyQualifyFixData?> GetFixDataAsync(Checksum solutionChecksum, DocumentId documentId, TextSpan span, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            // Including source generated documents as this service is used in Razor scenarios.
            var document = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

            var service = document.GetRequiredLanguageService<IFullyQualifyService>();

            return await service.GetFixDataAsync(document, span, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }
}
