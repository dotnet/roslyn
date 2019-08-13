// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
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

        // these fields are never accessed concurrently
        private TaskCompletionSource<VoidResult> _currentTask;
        private DateTimeOffset _lastTimeReported;

        private int _lastPendingItemCount;
        private ProgressStatus _lastProgressStatus;
        private ProgressStatus _lastShownProgressStatus;

        // enqueue refresh if updating task center is pushed out
        private ResettableDelay _resettableDelay;

        // this is only field that is shared between 2 events streams (IDiagnosticService and ISolutionCrawlerProgressReporter)
        // and can be called concurrently.
        private volatile ITaskHandler _taskHandler;

        [ImportingConstructor]
        public TaskCenterSolutionAnalysisProgressReporter(
            SVsTaskStatusCenterService taskStatusCenterService,
            IDiagnosticService diagnosticService,
            VisualStudioWorkspace workspace)
        {
            _lastTimeReported = DateTimeOffset.UtcNow;
            _resettableDelay = null;

            ResetProgressStatus();

            _taskCenterService = (IVsTaskStatusCenterService)taskStatusCenterService;

            _options = new TaskHandlerOptions()
            {
                Title = ServicesVSResources.Running_low_priority_background_processes,
                ActionsAfterCompletion = CompletionActions.None
            };

            var crawlerService = workspace.Services.GetService<ISolutionCrawlerService>();
            var reporter = crawlerService.GetProgressReporter(workspace);

            StartedOrStopped(reporter.InProgress);

            // no event unsubscription since it will remain alive until VS shutdown
            reporter.ProgressChanged += OnSolutionCrawlerProgressChanged;
        }

        private void OnSolutionCrawlerProgressChanged(object sender, ProgressData progressData)
        {
            // there is no concurrent call to this method since ISolutionCrawlerProgressReporter will serialize all
            // events to preserve event ordering
            switch (progressData.Status)
            {
                case ProgressStatus.Started:
                    StartedOrStopped(started: true);
                    break;
                case ProgressStatus.PendingItemUpdated:
                    _lastPendingItemCount = progressData.PendingItemCount.Value;
                    ProgressUpdated();
                    break;
                case ProgressStatus.Stoped:
                    StartedOrStopped(started: false);
                    break;
                case ProgressStatus.Evaluating:
                case ProgressStatus.Paused:
                    _lastProgressStatus = progressData.Status;
                    ProgressUpdated();
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(progressData.Status);
            }
        }

        private void ProgressUpdated()
        {
            // we prefer showing evaluating if progress is flipping between evaluate and pause
            // in short period of time.
            var forceUpdate = _lastShownProgressStatus == ProgressStatus.Paused &&
                              _lastProgressStatus == ProgressStatus.Evaluating;

            var current = DateTimeOffset.UtcNow;
            if (!forceUpdate && current - _lastTimeReported < s_minimumInterval)
            {
                // make sure we are not flooding UI. 
                // this is just presentation, fine to not updating UI right away especially since
                // at the end, this notification will go away automatically
                // but to make UI to be updated to right status eventually if task takes long time to finish
                // we enqueue refresh task.
                EnqueueRefresh();
                return;
            }

            _lastShownProgressStatus = _lastProgressStatus;
            _lastTimeReported = current;

            ChangeProgress(_taskHandler, GetMessage());

            string GetMessage()
            {
                var status = (_lastProgressStatus == ProgressStatus.Paused) ? ServicesVSResources.Paused : ServicesVSResources.Evaluating;
                return $"{status} ({_lastPendingItemCount} {ServicesVSResources.tasks_in_queue})";
            }

            void EnqueueRefresh()
            {
                if (_resettableDelay != null)
                {
                    _resettableDelay.Reset();
                    return;
                }

                _resettableDelay = new ResettableDelay((int)s_minimumInterval.TotalMilliseconds, AsynchronousOperationListenerProvider.NullListener);
                _resettableDelay.Task.SafeContinueWith(_ =>
                {
                    _resettableDelay = null;
                    ProgressUpdated();
                }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
        }

        private void StartedOrStopped(bool started)
        {
            if (started)
            {
                ResetProgressStatus();

                // if there is any pending one. make sure it is finished.
                _currentTask?.TrySetResult(default);

                var taskHandler = _taskCenterService.PreRegister(_options, data: default);

                _currentTask = new TaskCompletionSource<VoidResult>();
                taskHandler.RegisterTask(_currentTask.Task);

                // report initial progress
                ChangeProgress(taskHandler, message: null);

                // set handler
                _taskHandler = taskHandler;
            }
            else
            {
                // clear progress message
                ChangeProgress(_taskHandler, message: null);

                // stop progress
                _currentTask?.TrySetResult(default);
                _currentTask = null;

                _taskHandler = null;

                ResetProgressStatus();
            }
        }

        private void ResetProgressStatus()
        {
            _lastPendingItemCount = 0;
            _lastProgressStatus = ProgressStatus.Paused;
            _lastShownProgressStatus = ProgressStatus.Paused;
        }

        private static void ChangeProgress(ITaskHandler taskHandler, string message)
        {
            var data = new TaskProgressData
            {
                ProgressText = message,
                CanBeCanceled = false,
                PercentComplete = null,
            };

            taskHandler?.Progress.Report(data);
        }
    }
}

