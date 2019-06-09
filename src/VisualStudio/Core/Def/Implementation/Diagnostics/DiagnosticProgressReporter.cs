// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
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

        // these fields are never accessed concurrently
        private TaskCompletionSource<VoidResult> _currentTask;
        private DateTimeOffset _lastTimeReported;

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

            _taskCenterService = (IVsTaskStatusCenterService)taskStatusCenterService;

            _options = new TaskHandlerOptions()
            {
                Title = ServicesVSResources.Live_analysis,
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
                case ProgressStatus.Updated:
                    ProgressUpdated(progressData.FilePathOpt);
                    break;
                case ProgressStatus.Stoped:
                    StartedOrStopped(started: false);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(progressData.Status);
            }
        }

        private void ProgressUpdated(string filePathOpt)
        {
            var current = DateTimeOffset.UtcNow;
            if (current - _lastTimeReported < s_minimumInterval)
            {
                // make sure we are not flooding UI. 
                // this is just presentation, fine to not updating UI especially since
                // at the end, this notification will go away automatically
                return;
            }

            _lastTimeReported = current;
            ChangeProgress(_taskHandler, filePathOpt != null ? string.Format(ServicesVSResources.Analyzing_0, FileNameUtilities.GetFileName(filePathOpt)) : null);
        }

        private void StartedOrStopped(bool started)
        {
            if (started)
            {
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
            }
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

