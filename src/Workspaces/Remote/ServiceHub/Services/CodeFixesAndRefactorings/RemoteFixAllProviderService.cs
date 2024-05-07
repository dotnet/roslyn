// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class RemoteFixAllProviderService(in BrokeredServiceBase.ServiceConstructionArguments arguments)
    : BrokeredServiceBase(arguments), IRemoteFixAllProviderService
{
    internal sealed class Factory : FactoryBase<IRemoteFixAllProviderService>
    {
        protected override IRemoteFixAllProviderService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteFixAllProviderService(arguments);
    }

    public ValueTask<string> PerformCleanupAsync(
        Checksum solutionChecksum, DocumentId documentId, CodeCleanupOptions codeCleanupOptions,
        Dictionary<TextSpan, List<string>> nodeAnnotations, Dictionary<TextSpan, List<string>> tokenAnnotations,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var document = solution.GetRequiredDocument(documentId);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var updatedRoot = DocumentBasedFixAllProviderHelpers.WithAnnotations(root, nodeAnnotations, tokenAnnotations);
            var updatedDocument = document.WithSyntaxRoot(updatedRoot);

            var sourceText = await DocumentBasedFixAllProviderHelpers.PerformCleanupInCurrentProcessAsync(
                updatedDocument, codeCleanupOptions, cancellationToken).ConfigureAwait(false);
            return sourceText.ToString();
        }, cancellationToken);
    }
}
