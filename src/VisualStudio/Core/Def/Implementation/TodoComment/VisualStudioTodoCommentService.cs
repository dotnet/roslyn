// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ProjectTelemetry;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.TodoComment;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TodoComment
{
    internal class VisualStudioTodoCommentService
        : ForegroundThreadAffinitizedObject, ITodoCommentService, ITodoCommentServiceCallback
    {
        private readonly VisualStudioWorkspaceImpl _workspace;

        /// <summary>
        /// Our connections to the remote OOP server. Created on demand when we startup and then
        /// kept around for the lifetime of this service.
        /// </summary>
        private KeepAliveSession? _keepAliveSession;

        /// <summary>
        /// Queue where we enqueue the information we get from OOP to process in batch in the future.
        /// </summary>
        private AsyncBatchingWorkQueue<TodoCommentInfo> _workQueue = null!;

        public VisualStudioTodoCommentService(VisualStudioWorkspaceImpl workspace, IThreadingContext threadingContext) : base(threadingContext)
            => _workspace = workspace;

        void ITodoCommentService.Start(CancellationToken cancellationToken)
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
            _workQueue = new AsyncBatchingWorkQueue<TodoCommentInfo>(
                TimeSpan.FromSeconds(1),
                ProcessTodoCommentInfosAsync,
                cancellationToken);

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
                return;

            // Pass ourselves in as the callback target for the OOP service.  As it discovers
            // designer attributes it will call back into us to notify VS about it.
            _keepAliveSession = await client.TryCreateKeepAliveSessionAsync(
                WellKnownServiceHubServices.RemoteTodoCommentService,
                callbackTarget: this, cancellationToken).ConfigureAwait(false);
            if (_keepAliveSession == null)
                return;

            // Now kick off scanning in the OOP process.
            var success = await _keepAliveSession.TryInvokeAsync(
                nameof(IRemoteTodoCommentService.ComputeTodoCommentsAsync),
                solution: null,
                arguments: Array.Empty<object>(),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Callback from the OOP service back into us.
        /// </summary>
        public Task ReportTodoCommentsAsync(List<TodoCommentInfo> infos, CancellationToken cancellationToken)
        {
            _workQueue.AddWork(infos);
            return Task.CompletedTask;
        }

        private async Task ProcessTodoCommentInfosAsync(
            ImmutableArray<TodoCommentInfo> infos, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var _1 = ArrayBuilder<TodoCommentInfo>.GetInstance(out var filteredInfos);
            AddFilteredInfos(infos, filteredInfos);

            using var _2 = ArrayBuilder<Task>.GetInstance(out var tasks);
            foreach (var info in filteredInfos)
                tasks.Add(Task.Run(() => NotifyTelemetryService(info), cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private void AddFilteredInfos(ImmutableArray<TodoCommentInfo> infos, ArrayBuilder<TodoCommentInfo> filteredInfos)
        {
            using var _ = PooledHashSet<DocumentId>.GetInstance(out var seenProjectIds);

            // Walk the list of telemetry items in reverse, and skip any items for a document once
            // we've already seen it once.  That way, we're only reporting the most up to date
            // information for a document, and we're skipping the stale information.
            for (var i = infos.Length - 1; i >= 0; i--)
            {
                var info = infos[i];
                if (seenProjectIds.Add(info.DocumentId))
                    filteredInfos.Add(info);
            }
        }

        private void ProcessTodoCommentInfosAsync(ProjectTelemetryInfo info)
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
    }
}
