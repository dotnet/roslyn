// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TaskStatusCenter;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// This reports table source (error list or todo comments) updating progress to Task Status Center. 
    /// this is tailored for table sources specifically. and not designed for generic usage. it expects certain
    /// behavior of error list where all events are serialized from the caller. in another word,
    /// there will be no concurrent call to any of these methods
    /// </summary>
    internal class ProgressReporter
    {
        private readonly IVsTaskStatusCenterService _taskCenterService;
        private readonly TaskHandlerOptions _options;

        private TaskCompletionSource<bool> _currentTask;
        private ITaskHandler _taskHandler;
        private string _lastMessage;

        public ProgressReporter(IVsTaskStatusCenterService taskCenterService)
        {
            _taskCenterService = taskCenterService;

            _options = new TaskHandlerOptions()
            {
                Title = ServicesVSResources.Live_code_analysis,
                ActionsAfterCompletion = CompletionActions.None
            };
        }

        public void Started(bool running)
        {
            if (running)
            {
                // if there is any pending one. make sure it is finished.
                _currentTask?.TrySetResult(true);

                _taskHandler = _taskCenterService.PreRegister(_options, data: default);

                _currentTask = new TaskCompletionSource<bool>();
                _taskHandler.RegisterTask(_currentTask.Task);

                var data = new TaskProgressData
                {
                    ProgressText = null,
                    CanBeCanceled = false,
                    PercentComplete = null,
                };

                // report progress
                _taskHandler.Progress.Report(data);
            }
            else
            {
                // stop progress
                _currentTask?.TrySetResult(false);
                _currentTask = null;

                _taskHandler = null;
            }
        }

        public void ChangeProgress(string message)
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

