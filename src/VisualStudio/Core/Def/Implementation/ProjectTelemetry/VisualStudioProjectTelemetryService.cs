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
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ProjectTelemetry;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectTelemetry
{
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal class VisualStudioProjectTelemetryService
        : ForegroundThreadAffinitizedObject, IProjectTelemetryListener, IEventListener<object>
    {
        private const string EventPrefix = "VS/Compilers/Compilation/";
        private const string PropertyPrefix = "VS.Compilers.Compilation.Inputs.";

        private const string TelemetryEventPath = EventPrefix + "Inputs";
        private const string TelemetryExceptionEventPath = EventPrefix + "TelemetryUnhandledException";

        private const string TelemetryProjectIdName = PropertyPrefix + "ProjectId";
        private const string TelemetryProjectGuidName = PropertyPrefix + "ProjectGuid";
        private const string TelemetryLanguageName = PropertyPrefix + "Language";
        private const string TelemetryAnalyzerReferencesCountName = PropertyPrefix + "AnalyzerReferences.Count";
        private const string TelemetryProjectReferencesCountName = PropertyPrefix + "ProjectReferences.Count";
        private const string TelemetryMetadataReferencesCountName = PropertyPrefix + "MetadataReferences.Count";
        private const string TelemetryDocumentsCountName = PropertyPrefix + "Documents.Count";
        private const string TelemetryAdditionalDocumentsCountName = PropertyPrefix + "AdditionalDocuments.Count";

        private readonly VisualStudioWorkspaceImpl _workspace;

        /// <summary>
        /// Our connection to the remote OOP server. Created on demand when we startup and then
        /// kept around for the lifetime of this service.
        /// </summary>
        private RemoteServiceConnection? _connection;

        /// <summary>
        /// Queue where we enqueue the information we get from OOP to process in batch in the future.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<ProjectTelemetryData>? _workQueue;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioProjectTelemetryService(
            VisualStudioWorkspaceImpl workspace,
            IThreadingContext threadingContext) : base(threadingContext)
        {
            _workspace = workspace;

            _workQueue = new AsyncBatchingWorkQueue<ProjectTelemetryData>(
                TimeSpan.FromSeconds(1),
                NotifyTelemetryServiceAsync,
                threadingContext.DisposalToken);
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

        private async Task StartWorkerAsync()
        {
            var cancellationToken = ThreadingContext.DisposalToken;

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
                return;

            // Pass ourselves in as the callback target for the OOP service.  As it discovers
            // designer attributes it will call back into us to notify VS about it.
            _connection = await client.CreateConnectionAsync(
                WellKnownServiceHubService.RemoteProjectTelemetryService,
                callbackTarget: this, cancellationToken).ConfigureAwait(false);

            // Now kick off scanning in the OOP process.
            await _connection.RunRemoteAsync(
                nameof(IRemoteProjectTelemetryService.ComputeProjectTelemetryAsync),
                solution: null,
                arguments: Array.Empty<object>(),
                cancellationToken).ConfigureAwait(false);
        }

        private async Task NotifyTelemetryServiceAsync(
            ImmutableArray<ProjectTelemetryData> infos, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _1 = ArrayBuilder<ProjectTelemetryData>.GetInstance(out var filteredInfos);
            AddFilteredData(infos, filteredInfos);

            using var _2 = ArrayBuilder<Task>.GetInstance(out var tasks);
            foreach (var info in filteredInfos)
                tasks.Add(Task.Run(() => NotifyTelemetryService(info), cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private void AddFilteredData(ImmutableArray<ProjectTelemetryData> infos, ArrayBuilder<ProjectTelemetryData> filteredInfos)
        {
            using var _ = PooledHashSet<ProjectId>.GetInstance(out var seenProjectIds);

            // Walk the list of telemetry items in reverse, and skip any items for a project once
            // we've already seen it once.  That way, we're only reporting the most up to date
            // information for a project, and we're skipping the stale information.
            for (var i = infos.Length - 1; i >= 0; i--)
            {
                var info = infos[i];
                if (seenProjectIds.Add(info.ProjectId))
                    filteredInfos.Add(info);
            }
        }

        private void NotifyTelemetryService(ProjectTelemetryData info)
        {
            try
            {
                var telemetryEvent = TelemetryHelper.TelemetryService.CreateEvent(TelemetryEventPath);
                telemetryEvent.SetStringProperty(TelemetryProjectIdName, info.ProjectId.Id.ToString());
                telemetryEvent.SetStringProperty(TelemetryProjectGuidName, Guid.Empty.ToString());
                telemetryEvent.SetStringProperty(TelemetryLanguageName, info.Language);
                telemetryEvent.SetIntProperty(TelemetryAnalyzerReferencesCountName, info.AnalyzerReferencesCount);
                telemetryEvent.SetIntProperty(TelemetryProjectReferencesCountName, info.ProjectReferencesCount);
                telemetryEvent.SetIntProperty(TelemetryMetadataReferencesCountName, info.MetadataReferencesCount);
                telemetryEvent.SetIntProperty(TelemetryDocumentsCountName, info.DocumentsCount);
                telemetryEvent.SetIntProperty(TelemetryAdditionalDocumentsCountName, info.AdditionalDocumentsCount);

                TelemetryHelper.DefaultTelemetrySession.PostEvent(telemetryEvent);
            }
            catch (Exception e)
            {
                // The telemetry service itself can throw.
                // So, to be very careful, put this in a try/catch too.
                try
                {
                    var exceptionEvent = TelemetryHelper.TelemetryService.CreateEvent(TelemetryExceptionEventPath);
                    exceptionEvent.SetStringProperty("Type", e.GetTypeDisplayName());
                    exceptionEvent.SetStringProperty("Message", e.Message);
                    exceptionEvent.SetStringProperty("StackTrace", e.StackTrace);
                    TelemetryHelper.DefaultTelemetrySession.PostEvent(exceptionEvent);
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Callback from the OOP service back into us.
        /// </summary>
        public Task ReportProjectTelemetryDataAsync(ProjectTelemetryData info, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_workQueue);
            _workQueue.AddWork(info);
            return Task.CompletedTask;
        }
    }
}
