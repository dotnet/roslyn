// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TaskStatusCenter;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    [Export(typeof(DiagnosticProgressReporter))]
    internal class DiagnosticProgressReporter
    {
        private readonly IVsTaskStatusCenterService _taskCenterService;
        private readonly IDiagnosticService _diagnosticService;
        private readonly TaskHandlerOptions _options;

        // this fields are never accessed concurrently
        private TaskCompletionSource<bool> _currentTask;
        private string _lastMessage;

        // this is only field that is shared between 2 events streams (IDiagnosticService and ISolutionCrawlerProgressReporter)
        // and can be called concurrently.
        // volatile to make sure value is shared properly between threads
        private volatile ITaskHandler _taskHandler;

        [ImportingConstructor]
        public DiagnosticProgressReporter(
            SVsServiceProvider serviceProvider,
            IDiagnosticService diagnosticService,
            VisualStudioWorkspace workspace)
        {
            // no event unsubscription since it will remain alive until VS shutdown
            _taskCenterService = (IVsTaskStatusCenterService)serviceProvider.GetService(typeof(SVsTaskStatusCenterService));
            _diagnosticService = diagnosticService;

            var crawlerService = workspace.Services.GetService<ISolutionCrawlerService>();
            var reporter = crawlerService.GetProgressReporter(workspace);

            Started(reporter.InProgress);

            reporter.ProgressChanged += OnSolutionCrawlerProgressChanged;
            _diagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;

            _options = new TaskHandlerOptions()
            {
                Title = ServicesVSResources.Live_code_analysis,
                ActionsAfterCompletion = CompletionActions.None
            };
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
                _currentTask?.TrySetResult(true);

                var taskHandler = _taskCenterService.PreRegister(_options, data: default);

                _currentTask = new TaskCompletionSource<bool>();
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
                _currentTask?.TrySetResult(false);
                _currentTask = null;

                _taskHandler = null;
            }
        }

        private void ChangeProgress(string message)
        {
            if (message == _lastMessage)
            {
                return;
            }

            var data = new TaskProgressData
            {
                ProgressText = message,
                CanBeCanceled = false,
                PercentComplete = null,
            };

            _lastMessage = message;
            _taskHandler?.Progress.Report(data);
        }
    }
}

