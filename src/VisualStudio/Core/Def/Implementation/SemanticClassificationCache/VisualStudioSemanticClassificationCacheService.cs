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
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SemanticClassificationCache
{
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    [ExportWorkspaceService(typeof(ISemanticClassificationCacheService), ServiceLayer.Host)]
    internal class VisualStudioSemanticClassificationCacheService
        : ForegroundThreadAffinitizedObject, ISemanticClassificationCacheService, IEventListener<object>
    {
        private readonly VisualStudioWorkspaceImpl _workspace;

        /// <summary>
        /// Used to acquire the legacy project designer service.
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Our connection to the remote OOP server. Created on demand when we startup and then
        /// kept around for the lifetime of this service.
        /// </summary>
        private readonly AsyncLazy<RemoteServiceConnection?> _lazyConnection;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioSemanticClassificationCacheService(
            VisualStudioWorkspaceImpl workspace,
            IThreadingContext threadingContext,
            Shell.SVsServiceProvider serviceProvider)
            : base(threadingContext)
        {
            _workspace = workspace;
            _serviceProvider = serviceProvider;

            _lazyConnection = new AsyncLazy<RemoteServiceConnection?>(c => CreateConnectionAsync(c), cacheResult: true);
        }

        void IEventListener<object>.StartListening(Workspace workspace, object _)
        {
            if (workspace is VisualStudioWorkspace)
                _ = StartAsync();
        }

        private async Task StartAsync()
        {
            // Have to catch all exceptions coming through here as this is called from a
            // fire-and-forget method and we want to make sure nothing leaks out.
            try
            {
                var statusService = _workspace.Services.GetService<IWorkspaceStatusService>();
                await statusService.WaitUntilFullyLoadedAsync(ThreadingContext.DisposalToken).ConfigureAwait(false);
                await StartWorkerAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is normal (during VS closing).  Just ignore.
            }
            catch (Exception e) when (FatalError.ReportWithoutCrash(e))
            {
                // Otherwise report a watson for any other exception.  Don't bring down VS.  This is
                // a BG service we don't want impacting the user experience.
            }
        }

        private async Task<RemoteServiceConnection?> CreateConnectionAsync(CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                // We don't do anything if we fail to get the external process.  That's the case when something has gone
                // wrong, or the user is explicitly choosing to run inproc only.   In neither of those cases do we want
                // to bog down the VS process with the work to semantically classify all files.
                return null;
            }

            var connection = await client.CreateConnectionAsync(
                WellKnownServiceHubService.RemoteSemanticClassificationCacheService,
                callbackTarget: null, cancellationToken).ConfigureAwait(false);

            return connection;
        }

        private async Task StartWorkerAsync()
        {
            var cancellationToken = ThreadingContext.DisposalToken;

            var connection = await _lazyConnection.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (connection == null)
                return;

            // Now kick off scanning in the OOP process.
            await connection.RunRemoteAsync(
                nameof(IRemoteSemanticClassificationCacheService.StartCachingSemanticClassificationsAsync),
                solution: null,
                arguments: Array.Empty<object>(),
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<ClassifiedSpan>> GetCachedSemanticClassificationsAsync(
            DocumentKey documentKey,
            TextSpan textSpan,
            Checksum checksum,
            CancellationToken cancellationToken)
        {
            var connection = await _lazyConnection.GetValueAsync(cancellationToken).ConfigureAwait(false);

            var classifiedSpans = await connection.RunRemoteAsync<SerializableClassifiedSpans>(
                nameof(IRemoteSemanticClassificationCacheService.GetCachedSemanticClassificationsAsync),
                solution: null,
                arguments: new object[] { documentKey.Dehydrate(), textSpan, checksum },
                cancellationToken).ConfigureAwait(false);

            var list = ClassificationUtilities.GetOrCreateClassifiedSpanList();
            classifiedSpans.Rehydrate(list);

            var result = list.ToImmutableArray();
            ClassificationUtilities.ReturnClassifiedSpanList(list);
            return result;
        }
    }
}
