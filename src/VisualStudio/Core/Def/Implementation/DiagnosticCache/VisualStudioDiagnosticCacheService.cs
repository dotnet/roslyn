// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.PersistentStorage;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DiagnosticCache
{
    internal sealed partial class VisualStudioDiagnosticCacheService : IDiagnosticCacheService
    {
        // TODO: loc
        private const string CachedMessageFormat = "(Loaded from cache) - {0}";

        private readonly VisualStudioWorkspace _workspace;
        private readonly UpdateTracker _updateTracker;
        private readonly CacheUpdater _cacheUpdater;

        public VisualStudioDiagnosticCacheService(
            VisualStudioWorkspace workspace,
            IDiagnosticService diagnosticService,
            IDiagnosticUpdateSourceRegistrationService registrationService,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _workspace = workspace;
            _updateTracker = new(_workspace, registrationService, listenerProvider);

            var globalOperationNotificationService = _workspace.Services.GetRequiredService<IGlobalOperationNotificationService>();
            _cacheUpdater = new(_workspace, diagnosticService, globalOperationNotificationService, CancellationToken.None);

            Log("CreateService", nameof(VisualStudioDiagnosticCacheService));
        }

        public async Task<bool> TryLoadCachedDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            if (document.Project.Solution.Workspace != _workspace)
            {
                return false;
            }

            var workspaceStatusService = document.Project.Solution.Workspace.Services.GetRequiredService<IWorkspaceStatusService>();
            var isFullyLoadedTask = workspaceStatusService.IsFullyLoadedAsync(cancellationToken);
            var isFullyLoaded = isFullyLoadedTask.IsCompleted && isFullyLoadedTask.GetAwaiter().GetResult();

            if (isFullyLoaded)
            {
                return false;
            }

            var cachedDiagnostics = await GetCachedDiagnosticsAsync(document, cancellationToken).ConfigureAwait(false);
            if (!cachedDiagnostics.IsDefaultOrEmpty)
            {
                _updateTracker.TryUpdateDiagnosticsLoadedFromCache(document, cachedDiagnostics);
            }

            return true;
        }

        public void OnAnalyzeDocument(Document document)
        {
            var isAnalyzedForFirstTime = _updateTracker.OnLiveAnalysisStarted(document);
            if (isAnalyzedForFirstTime && document.IsOpen())
            {
                // Only need to do this once, i.e. only the first time, in case there no diagnostics in this document,
                // then we want to make sure clear cached diagnostics for this document, after that it will be handled
                // by monitoring diagnostics update.
                _cacheUpdater.QueueUpdate(document.Id);
            }
        }

        private static async Task<ImmutableArray<DiagnosticData>> GetCachedDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(document.Project.Solution.Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return default;
            }

            var checksum = await GetChecksumAsync(document, cancellationToken).ConfigureAwait(false);

            var result = await client.TryInvokeAsync<IRemoteDiagnosticCacheService, ImmutableArray<DiagnosticData>>(
                (service, ct) => service.GetCachedDiagnosticsAsync((DocumentKey.ToDocumentKey(document)).Dehydrate(), checksum, ct),
                cancellationToken).ConfigureAwait(false);

            if (result.HasValue)
            {
                var diagnostics = result.Value.SelectAsArray(FixDescription);
                return diagnostics;
            }

            return default;

            // Change text to make it clear this is loaded from cache.
            static DiagnosticData FixDescription(DiagnosticData d) =>
                new(id: d.Id, category: d.Category,
                    message: string.Format(CachedMessageFormat, d.Message),
                    enuMessageForBingSearch: d.ENUMessageForBingSearch,
                    severity: d.Severity, defaultSeverity: d.DefaultSeverity, isEnabledByDefault: d.IsEnabledByDefault, warningLevel: d.WarningLevel,
                    customTags: d.CustomTags, properties: d.Properties, projectId: d.ProjectId, location: d.DataLocation, additionalLocations: d.AdditionalLocations,
                    language: d.Language, title: d.Title,
                    description: d.Description,
                    helpLink: d.HelpLink, isSuppressed: d.IsSuppressed);
        }

        private static async ValueTask<Checksum> GetChecksumAsync(Document document, CancellationToken cancellationToken)
        {
            // We only checksum off of the contents of the file.  During load, we can't really compute any other
            // information since we don't necessarily know about other files, metadata, or dependencies.  So during
            // load, we allow for the previous diagnostics to be used as long as the file contents match.
            var checksums = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            return checksums.Text;
        }

        public static void Log(string operation, string message)
            => Logger.Log(FunctionId.Diagnostics_CacheService, KeyValueLogMessage.Create(m => m[operation] = message));
    }
}
