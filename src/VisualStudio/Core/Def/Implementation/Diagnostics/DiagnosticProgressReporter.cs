// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskStatusCenter;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    [Export(typeof(DiagnosticProgressReporter))]
    internal sealed class DiagnosticProgressReporter
    {
        private static readonly TimeSpan s_minimumInterval = TimeSpan.FromMilliseconds(200);

        private readonly IVsTaskStatusCenterService _taskCenterService;
        private readonly IDiagnosticService _diagnosticService;
        private readonly TaskHandlerOptions _options;

        // these fields are never accessed concurrently
        private TaskCompletionSource<VoidResult> _currentTask;
        private object _lastReportedDocumentOrProject;
        private DateTimeOffset _lastTimeReported;

        // this is only field that is shared between 2 events streams (IDiagnosticService and ISolutionCrawlerProgressReporter)
        // and can be called concurrently.
        private volatile ITaskHandler _taskHandler;

        [ImportingConstructor]
        public DiagnosticProgressReporter(
            SVsTaskStatusCenterService taskStatusCenterService,
            IDiagnosticService diagnosticService,
            VisualStudioWorkspace workspace)
        {
            _lastTimeReported = DateTimeOffset.UtcNow;

            _taskCenterService = (IVsTaskStatusCenterService)taskStatusCenterService;
            _diagnosticService = diagnosticService;

            _options = new TaskHandlerOptions()
            {
                Title = ServicesVSResources.Live_code_analysis,
                ActionsAfterCompletion = CompletionActions.None
            };

            var crawlerService = workspace.Services.GetService<ISolutionCrawlerService>();
            var reporter = crawlerService.GetProgressReporter(workspace);

            Started(reporter.InProgress);

            // no event unsubscription since it will remain alive until VS shutdown
            reporter.ProgressChanged += OnSolutionCrawlerProgressChanged;
            _diagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;
        }

        private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
        {
            // there is no concurrent call to this method since IDiagnosticService will serialize all
            // events to preserve event ordering
            var solution = e.Solution;
            if (solution == null)
            {
                return;
            }

            // we ignore report for same document/project as last time
            // even if they are from different analyzers
            // since, for the progress report, they all show up same
            var documentOrProject = (object)e.DocumentId ?? e.ProjectId;
            if (_lastReportedDocumentOrProject == documentOrProject)
            {
                return;
            }

            var current = DateTimeOffset.UtcNow;
            if (current - _lastTimeReported < s_minimumInterval)
            {
                // make sure we are not flooding UI. 
                // this is just presentation, fine to not updating UI especially since
                // at the end, this notification will go away automatically
                return;
            }

            // only update when we actually change message
            _lastReportedDocumentOrProject = documentOrProject;
            _lastTimeReported = current;

            var document = solution.GetDocument(e.DocumentId);
            if (document != null)
            {
                ChangeProgress(string.Format(ServicesVSResources.Analyzing_0, document.Name ?? document.FilePath ?? "..."));
                return;
            }

            var project = solution.GetProject(e.ProjectId);
            if (project != null)
            {
                ChangeProgress(string.Format(ServicesVSResources.Analyzing_0, project.Name ?? project.FilePath ?? "..."));
                return;
            }
        }

        private void OnSolutionCrawlerProgressChanged(object sender, bool running)
        {
            // there is no concurrent call to this method since ISolutionCrawlerProgressReporter will serialize all
            // events to preserve event ordering
            Started(running);
        }

        private void Started(bool running)
        {
            if (running)
            {
                // if there is any pending one. make sure it is finished.
                _currentTask?.TrySetResult(default);

                var taskHandler = _taskCenterService.PreRegister(_options, data: default);

                _currentTask = new TaskCompletionSource<VoidResult>();
                taskHandler.RegisterTask(_currentTask.Task);

                var data = new TaskProgressData
                {
                    ProgressText = null,
                    CanBeCanceled = false,
                    PercentComplete = null,
                };

                // report initial progress
                taskHandler.Progress.Report(data);

                // set handler
                _taskHandler = taskHandler;
            }
            else
            {
                // stop progress
                _currentTask?.TrySetResult(default);
                _currentTask = null;

                _taskHandler = null;
            }
        }

        private void ChangeProgress(string message)
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

