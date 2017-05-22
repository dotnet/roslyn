// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.TaskStatusCenter;
using Roslyn.Utilities;
using VSShell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    [ExportWorkspaceService(typeof(ISymbolSearchProgressService), ServiceLayer.Host), Shared]
    internal class VisualStudioSymbolSearchProgressService : ISymbolSearchProgressService
    {
        private readonly object _gate;
        private readonly IVsTaskStatusCenterService _taskCenterServiceOpt;

        private TaskCompletionSource<bool> _taskCompletionSource;

        [ImportingConstructor]
        public VisualStudioSymbolSearchProgressService(VSShell.SVsServiceProvider serviceProvider)
        {
            _taskCenterServiceOpt = new ProgressService((IVsTaskStatusCenterService)serviceProvider.GetService(typeof(SVsTaskStatusCenterService)));
        }
        public Task OnDownloadFullDatabaseStartedAsync(string title)
        {
            lock (_gate)
            {
                var options = new TaskHandlerOptions
                {
                    Title = title,
                    Category = TaskCategory.BackgroundService,
                    RetentionAfterCompletion = CompletionRetention.Faulted,
                    DisplayTaskDetails = _ => { }
                };

                var data = new TaskProgressData
                {
                    CanBeCanceled = false,
                    PercentComplete = null,
                    ShutdownBehavior = AppShutdownBehavior.NoBlock,
                };

                _taskCompletionSource = new TaskCompletionSource<bool>();
                var handler = _taskCenterServiceOpt?.PreRegister(options, data);
                handler?.RegisterTask(_taskCompletionSource.Task);
            }

            return SpecializedTasks.EmptyTask;
        }

        public Task OnDownloadFullDatabaseSucceededAsync()
        {
            lock (_gate)
            {
                _taskCompletionSource.TrySetResult(true);
                return SpecializedTasks.EmptyTask;
            }
        }

        public Task OnDownloadFullDatabaseCanceledAsync()
        {
            lock (_gate)
            {
                _taskCompletionSource.TrySetCanceled();
                return SpecializedTasks.EmptyTask;
            }
        }

        public Task OnDownloadFullDatabaseFailedAsync(string message)
        {
            lock (_gate)
            {
                _taskCompletionSource.TrySetException(new Exception(message));
                return SpecializedTasks.EmptyTask;
            }
        }
    }
}