// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    [ExportWorkspaceService(typeof(ISemanticClassificationCacheService), ServiceLayer.Host), Shared]
    internal class EditorSemanticClassificationCacheService : ISemanticClassificationCacheService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorSemanticClassificationCacheService()
        {
        }

        public async Task<ImmutableArray<ClassifiedSpan>> GetCachedSemanticClassificationsAsync(
            Document document,
            TextSpan textSpan,
            Checksum checksum,
            CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(document.Project.Solution.Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                // We don't do anything if we fail to get the external process.  That's the case when something has gone
                // wrong, or the user is explicitly choosing to run inproc only.   In neither of those cases do we want
                // to bog down the VS process with the work to semantically classify files.
                return default;
            }

            var documentKey = SemanticClassificationCacheUtilities.GetDocumentKeyForCaching(document);
            var classifiedSpans = await client.TryInvokeAsync<IRemoteSemanticClassificationCacheService, SerializableClassifiedSpans?>(
                (service, cancellationToken) => service.GetCachedSemanticClassificationsAsync(documentKey, textSpan, checksum, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            if (!classifiedSpans.HasValue || classifiedSpans.Value == null)
                return default;

            using var _ = ArrayBuilder<ClassifiedSpan>.GetInstance(out var result);
            classifiedSpans.Value.Rehydrate(result);
            return result.ToImmutable();
        }
    }
}
