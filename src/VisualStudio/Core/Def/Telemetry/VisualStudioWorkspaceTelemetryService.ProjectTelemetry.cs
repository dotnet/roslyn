// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ProjectTelemetry;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.Internal.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal partial class VisualStudioWorkspaceTelemetryService : IProjectTelemetryListener, IDisposable
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

        /// <summary>
        /// Our connection to the remote OOP server. Created on demand when we startup and then
        /// kept around for the lifetime of this service.
        /// </summary>
        private RemoteServiceConnection<IRemoteProjectTelemetryService>? _lazyConnection;

        public void Dispose()
        {
            _lazyConnection?.Dispose();
        }

        private async Task StartProjectTelemetryWorkerAsync(RemoteHostClient client, CancellationToken cancellationToken)
        {
            // Pass ourselves in as the callback target for the OOP service.  As it discovers
            // designer attributes it will call back into us to notify VS about it.
            _lazyConnection = client.CreateConnection<IRemoteProjectTelemetryService>(callbackTarget: this);

            // Now kick off scanning in the OOP process.
            // If the call fails an error has already been reported and there is nothing more to do.
            _ = await _lazyConnection.TryInvokeAsync(
                (service, callbackId, cancellationToken) => service.ComputeProjectTelemetryAsync(callbackId, cancellationToken),
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
        public ValueTask ReportProjectTelemetryDataAsync(ProjectTelemetryData info, CancellationToken cancellationToken)
        {
            _workQueue.AddWork(info);
            return ValueTaskFactory.CompletedTask;
        }
    }
}
