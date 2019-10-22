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

        private readonly IVsTaskStatusCenterService _taskCenterService;
        private readonly TaskHandlerOptions _options;

        /// <summary>
        /// Task handler to provide a task to the <see cref="_taskCenterService"/>
        /// </summary>
        private ITaskHandler? _taskHandler;

        /// <summary>
        /// Stores the currently running task center task.
        /// This is manually started and completed based on receiving start / stop events
        /// from the <see cref="ISolutionCrawlerProgressReporter"/>
        /// </summary>
        private TaskCompletionSource<VoidResult>? _taskCenterTask;

        /// <summary>
        /// Task used to trigger throttled UI updates in an interval
        /// defined by <see cref="s_minimumInterval"/>
        /// </summary>
        private Task? _intervalTask;

        /// <summary>
        /// Gate access to reporting sln crawler events so we cannot
        /// report UI changes concurrently.
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Stores the last shown <see cref="ProgressData"/>
        /// </summary>
        private ProgressData _lastProgressData;

        /// <summary>
        /// Unfortunately, <see cref="ProgressData.PendingItemCount"/> is only reported
        /// when the <see cref="ProgressData.Status"/> is <see cref="ProgressStatus.PendingItemCountUpdated"/>
        /// So we have to store this in addition to <see cref="_lastProgressData"/> so that we
        /// do not overwrite the last reported count with 0.
        /// </summary>
        private int _lastProgressCount;

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
                Started();
            }
            else
            {
                Stopped();
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

                // Report progress immediately to ensure we update the UI on the first event.
                ReportProgress();

                // Kick off task to update the UI after a delay to pick up any new events.
                _intervalTask = Task.CompletedTask.ContinueWithAfterDelay(() =>
                {
                    ReportProgress();
                }, CancellationToken.None, s_minimumInterval, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        private void ReportProgress()
        {
            ProgressData data;
            lock (_lock)
            {
                data = _lastProgressData;
                _intervalTask = null;
            }

            UpdateUI(data);
        }

        private void UpdateUI(ProgressData progressData)
        {
            switch (progressData.Status)
            {
                case ProgressStatus.Started:
                    Started();
                    break;
                case ProgressStatus.PendingItemCountUpdated:
                    _lastProgressCount = progressData.PendingItemCount ?? 0;
                    ChangeProgress(GetMessage(progressData, _lastProgressCount));
                    break;
                case ProgressStatus.Stopped:
                    Stopped();
                    break;
                case ProgressStatus.Evaluating:
                case ProgressStatus.Paused:
                    ChangeProgress(GetMessage(progressData, _lastProgressCount));
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(progressData.Status);
            }
        }

        private static string GetMessage(ProgressData progressData, int pendingItemCount)
        {
            var statusMessage = (progressData.Status == ProgressStatus.Paused) ? ServicesVSResources.Paused_0_tasks_in_queue : ServicesVSResources.Evaluating_0_tasks_in_queue;
            return string.Format(statusMessage, pendingItemCount);
        }

        private void Started()
        {
            _lastProgressData = default;
            _lastProgressCount = 0;

            // Make sure when we start a new task, the previous one is removed from the task center.
            _taskCenterTask?.TrySetResult(default);

            // Register a new task handler to handle a new task center task.
            // Each task handler can only register one task, so we must create a new one each time we start.
            var taskHandler = _taskCenterService.PreRegister(_options, data: default);

            // Create a new non-completed task to be tracked by the task handler.
            _taskCenterTask = new TaskCompletionSource<VoidResult>();
            taskHandler.RegisterTask(_taskCenterTask.Task);

            // Update the stored handler so progress changes update this task.
            _taskHandler = taskHandler;

            ChangeProgress(message: null);
        }

        private void Stopped()
        {
            // Clear progress message.
            ChangeProgress(message: null);

            // Mark the progress task as completed so it shows complete in the task center.
            _taskCenterTask?.TrySetResult(default);

            // Clear tasks and data.
            _taskCenterTask = null;
            _taskHandler = null;
            _lastProgressData = default;
            _lastProgressCount = 0;
        }

        private void ChangeProgress(string? message)
        {
            var data = new TaskProgressData
            {
                ProgressText = message,
                CanBeCanceled = false,
                PercentComplete = null,
            };

            _taskHandler?.Progress.Report(data);
        }
    }
}

