// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.TaskStatusCenter;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    [Export(typeof(TaskCenterSolutionAnalysisProgressReporter))]
    internal sealed class TaskCenterSolutionAnalysisProgressReporter
    {
        private static readonly TimeSpan s_minimumInterval = TimeSpan.FromMilliseconds(200);

        #region Fields protected by _lock

        /// <summary>
        /// Gate access to reporting sln crawler events so we cannot
        /// report UI changes concurrently.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Task used to trigger throttled UI updates in an interval
        /// defined by <see cref="s_minimumInterval"/>
        /// Protected from concurrent access by the <see cref="_lock"/>
        /// </summary>
        private Task? _intervalTask;

        /// <summary>
        /// Stores the last shown <see cref="ProgressData"/>
        /// Protected from concurrent access by the <see cref="_lock"/>
        /// </summary>
        private ProgressData _lastProgressData;

        /// <summary>
        /// Task used to ensure serialization of UI updates.
        /// Protected from concurrent access by the <see cref="_lock"/>
        /// </summary>
        private Task _updateUITask = Task.CompletedTask;

        #endregion

        #region Fields protected by _updateUITask 

        private readonly IVsTaskStatusCenterService _taskCenterService;
        private readonly TaskHandlerOptions _options;

        /// <summary>
        /// Task handler to provide a task to the <see cref="_taskCenterService"/>
        /// Protected from concurrent access due to serialization from <see cref="_updateUITask"/>
        /// </summary>
        private ITaskHandler? _taskHandler;

        /// <summary>
        /// Stores the currently running task center task.
        /// This is manually started and completed based on receiving start / stop events
        /// from the <see cref="ISolutionCrawlerProgressReporter"/>
        /// Protected from concurrent access due to serialization from <see cref="_updateUITask"/>
        /// </summary>
        private TaskCompletionSource<VoidResult>? _taskCenterTask;

        /// <summary>
        /// Unfortunately, <see cref="ProgressData.PendingItemCount"/> is only reported
        /// when the <see cref="ProgressData.Status"/> is <see cref="ProgressStatus.PendingItemCountUpdated"/>
        /// So we have to store the count separately for the UI so that we do not overwrite the last reported count with 0.
        /// Protected from concurrent access due to serialization from <see cref="_updateUITask"/>
        /// </summary>
        private int _lastPendingItemCount;

        #endregion

        [ImportingConstructor]
        public TaskCenterSolutionAnalysisProgressReporter(
            SVsTaskStatusCenterService taskStatusCenterService,
            IDiagnosticService diagnosticService,
            VisualStudioWorkspace workspace)
        {
            _taskCenterService = (IVsTaskStatusCenterService)taskStatusCenterService;
            _options = new TaskHandlerOptions()
            {
                Title = ServicesVSResources.Running_low_priority_background_processes,
                ActionsAfterCompletion = CompletionActions.None
            };

            var crawlerService = workspace.Services.GetRequiredService<ISolutionCrawlerService>();
            var reporter = crawlerService.GetProgressReporter(workspace);

            if (reporter.InProgress)
            {
                // The reporter was already sending events before we were able to subscribe, so trigger an update to the task center.
                OnSolutionCrawlerProgressChanged(this, new ProgressData(ProgressStatus.Started, pendingItemCount: null));
            }

            reporter.ProgressChanged += OnSolutionCrawlerProgressChanged;
        }

        /// <summary>
        /// Retrieve and throttle solution crawler events to be sent to the progress reporter UI.
        /// 
        /// there is no concurrent call to this method since ISolutionCrawlerProgressReporter will serialize all
        /// events to preserve event ordering
        /// </summary>
        /// <param name="progressData"></param>
        public void OnSolutionCrawlerProgressChanged(object sender, ProgressData progressData)
        {
            lock (_lock)
            {
                _lastProgressData = progressData;

                // The task is running which will update the progress.
                if (_intervalTask != null)
                {
                    return;
                }

                // Kick off task to update the UI after a delay to pick up any new events.
                _intervalTask = Task.Delay(s_minimumInterval).ContinueWith(_ => ReportProgress(),
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        private void ReportProgress()
        {
            lock (_lock)
            {
                var data = _lastProgressData;
                _intervalTask = null;

                _updateUITask = _updateUITask.ContinueWith(_ => UpdateUI(data),
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        private void UpdateUI(ProgressData progressData)
        {
            if (progressData.Status == ProgressStatus.Stopped)
            {
                StopTaskCenter();
                return;
            }

            // Update the pending item count if the progress data specifies a value.
            if (progressData.PendingItemCount.HasValue)
            {
                _lastPendingItemCount = progressData.PendingItemCount.Value;
            }

            // Start the task center task if not already running.
            if (_taskHandler == null)
            {
                // Make sure to stop the previous task center task if present.
                StopTaskCenter();

                // Register a new task handler to handle a new task center task.
                // Each task handler can only register one task, so we must create a new one each time we start.
                _taskHandler = _taskCenterService.PreRegister(_options, data: default);

                // Create a new non-completed task to be tracked by the task handler.
                _taskCenterTask = new TaskCompletionSource<VoidResult>();
                _taskHandler.RegisterTask(_taskCenterTask.Task);
            }

            var statusMessage = progressData.Status == ProgressStatus.Paused
                ? ServicesVSResources.Paused_0_tasks_in_queue
                : ServicesVSResources.Evaluating_0_tasks_in_queue;

            _taskHandler.Progress.Report(new TaskProgressData
            {
                ProgressText = string.Format(statusMessage, _lastPendingItemCount),
                CanBeCanceled = false,
                PercentComplete = null,
            });
        }

        private void StopTaskCenter()
        {
            // Mark the progress task as completed so it shows complete in the task center.
            _taskCenterTask?.TrySetResult(default);

            // Clear tasks and data.
            _taskCenterTask = null;
            _taskHandler = null;
            _lastProgressData = default;
            _lastPendingItemCount = 0;
        }
    }
}

