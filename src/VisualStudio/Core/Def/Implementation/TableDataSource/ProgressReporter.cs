// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TaskStatusCenter;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class ProgressReporter
    {
        private readonly IVsTaskStatusCenterService _taskCenterService;
        private readonly TaskHandlerOptions _options;

        private TaskCompletionSource<bool> _currentTask;
        private ITaskHandler _taskHandler;
        private string _lastMessage;

        public ProgressReporter(string title, IVsTaskStatusCenterService taskCenterService)
        {
            _taskCenterService = taskCenterService;

            _options = new TaskHandlerOptions()
            {
                Title = title,
                ActionsAfterCompletion = CompletionActions.None
            };
        }

        public void Started(bool running)
        {
            if (running)
            {
                // if there is any pending one. make sure it is finished.
                _currentTask?.TrySetResult(true);

                _taskHandler = _taskCenterService.PreRegister(_options, default);

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

