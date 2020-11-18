// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SemanticClassificationCache
{
    [ExportWorkspaceService(typeof(ISemanticClassificationCacheService), ServiceLayer.Host), Shared]
    internal class VisualStudioSemanticClassificationCacheService
        : ForegroundThreadAffinitizedObject, ISemanticClassificationCacheService
    {
        private readonly VisualStudioWorkspaceImpl _workspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioSemanticClassificationCacheService(
            VisualStudioWorkspaceImpl workspace,
            IThreadingContext threadingContext)
            : base(threadingContext)
        {
            _workspace = workspace;
        }

        public async Task<ImmutableArray<ClassifiedSpan>> GetCachedSemanticClassificationsAsync(
            DocumentKey documentKey,
            TextSpan textSpan,
            Checksum checksum,
            CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                // We don't do anything if we fail to get the external process.  That's the case when something has gone
                // wrong, or the user is explicitly choosing to run inproc only.   In neither of those cases do we want
                // to bog down the VS process with the work to semantically classify files.
                return default;
            }

            var classifiedSpans = await client.RunRemoteAsync<SerializableClassifiedSpans>(
                WellKnownServiceHubService.CodeAnalysis,
                nameof(IRemoteSemanticClassificationCacheService.GetCachedSemanticClassificationsAsync),
                solution: null,
                arguments: new object[] { documentKey.Dehydrate(), textSpan, checksum },
                callbackTarget: null,
                cancellationToken).ConfigureAwait(false);
            if (classifiedSpans == null)
                return default;

            var list = ClassificationUtilities.GetOrCreateClassifiedSpanList();
            classifiedSpans.Rehydrate(list);

            var result = list.ToImmutableArray();
            ClassificationUtilities.ReturnClassifiedSpanList(list);
            return result;
        }
    }
}
