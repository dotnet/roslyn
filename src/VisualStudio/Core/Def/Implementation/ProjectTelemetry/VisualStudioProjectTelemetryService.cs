// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ProjectTelemetry;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectTelemetry
{
    internal class VisualStudioProjectTelemetryService
        : ForegroundThreadAffinitizedObject, IProjectTelemetryService, IProjectTelemetryServiceCallback
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
        private readonly IThreadingContext _threadingContext;

        /// <summary>
        /// Our connections to the remote OOP server. Created on demand when we startup and then
        /// kept around for the lifetime of this service.
        /// </summary>
        private KeepAliveSession? _keepAliveSession;

        // We'll get notifications from the OOP server about new attribute arguments. Batch those
        // notifications up and deliver them to VS every second.
        #region protected by lock

        /// <summary>
        /// Lock we will use to ensure the remainder of these fields can be accessed in a threadsafe
        /// manner.  When OOP calls back into us, we'll place the data it produced into
        /// <see cref="_updatedInfos"/>.  We'll then kick of a task to process this in the future if
        /// we don't already have an existing task in flight for that.
        /// </summary>
        private readonly object _gate = new object();

        /// <summary>
        /// Data produced by OOP that we want to process in our next update task.
        /// </summary>
        private readonly List<ProjectTelemetryInfo> _updatedInfos = new List<ProjectTelemetryInfo>();

        /// <summary>
        /// Task kicked off to do the next batch of processing of <see cref="_updatedInfos"/>. These
        /// tasks form a chain so that the next task only processes when the previous one completes.
        /// </summary>
        private Task _updateTask = Task.CompletedTask;

        /// <summary>
        /// Whether or not there is an existing task in flight that will process the current batch
        /// of <see cref="_updatedInfos"/>.  If there is an existing in flight task, we don't need 
        /// to kick off a new one if we receive more notifications before it runs.
        /// </summary>
        private bool _taskInFlight = false;

        #endregion

        public VisualStudioProjectTelemetryService(
            VisualStudioWorkspaceImpl workspace,
            IThreadingContext threadingContext)
            : base(threadingContext)
        {
            _workspace = workspace;
        }

        void IProjectTelemetryService.Start(CancellationToken cancellationToken)
            => _ = StartAsync(cancellationToken);

        private async Task StartAsync(CancellationToken cancellationToken)
        {
            // Have to catch all exceptions coming through here as this is called from a
            // fire-and-forget method and we want to make sure nothing leaks out.
            try
            {
                await StartWorkerAsync(cancellationToken).ConfigureAwait(false);
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

        private async Task StartWorkerAsync(CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
                return;

            // Pass ourselves in as the callback target for the OOP service.  As it discovers
            // designer attributes it will call back into us to notify VS about it.
            _keepAliveSession = await client.TryCreateKeepAliveSessionAsync(
                WellKnownServiceHubServices.RemoteProjectTelemetryService,
                callbackTarget: this, cancellationToken).ConfigureAwait(false);
            if (_keepAliveSession == null)
                return;

            // Now kick off scanning in the OOP process.
            var success = await _keepAliveSession.TryInvokeAsync(
                nameof(IRemoteProjectTelemetryService.ComputeProjectTelemetryAsync),
                solution: null,
                arguments: Array.Empty<object>(),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Callback from the OOP service back into us.
        /// </summary>
        public Task RegisterProjectTelemetryInfoAsync(
            ProjectTelemetryInfo info, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                // add our work to the set we'll process in the next batch.
                _updatedInfos.Add(info);

                if (!_taskInFlight)
                {
                    // No in-flight task.  Kick one off to process these messages a second from now.
                    // We always attach the task to the previous one so that notifications to the ui
                    // follow the same order as the notification the OOP server sent to us.
                    _updateTask = _updateTask.ContinueWithAfterDelayFromAsync(
                        _ => NotifyTelemetryServiceAsync(cancellationToken),
                        cancellationToken,
                        1000/*ms*/,
                        TaskContinuationOptions.RunContinuationsAsynchronously,
                        TaskScheduler.Default);
                    _taskInFlight = true;
                }
            }

            return Task.CompletedTask;
        }

        private async Task NotifyTelemetryServiceAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _1 = ArrayBuilder<ProjectTelemetryInfo>.GetInstance(out var telemetryInfos);
            AddInfosAndResetQueue(telemetryInfos);

            using var _2 = ArrayBuilder<Task>.GetInstance(out var tasks);
            foreach (var info in telemetryInfos)
                tasks.Add(Task.Run(() => NotifyTelemetryService(info), cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private void NotifyTelemetryService(ProjectTelemetryInfo info)
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

        private void AddInfosAndResetQueue(ArrayBuilder<ProjectTelemetryInfo> attributeInfos)
        {
            using var _ = PooledHashSet<ProjectId>.GetInstance(out var seenProjectIds);

            lock (_gate)
            {
                // walk the set of updates in reverse, and ignore projects if we see them a second
                // time.  This ensures that if we're batching up multiple notifications for the same
                // project, that we only bother processing the last one since it should beat out all
                // the prior ones.
                for (var i = _updatedInfos.Count - 1; i >= 0; i--)
                {
                    var info = _updatedInfos[i];
                    if (seenProjectIds.Add(info.ProjectId))
                        attributeInfos.Add(info);
                }

                // mark there being no existing update task so that the next OOP notification will
                // kick one off.
                _updatedInfos.Clear();
                _taskInFlight = false;
            }
        }
    }
}
