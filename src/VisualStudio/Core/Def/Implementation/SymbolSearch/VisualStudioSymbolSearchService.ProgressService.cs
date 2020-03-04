﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.TaskStatusCenter;
using VSShell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    [ExportWorkspaceService(typeof(ISymbolSearchProgressService), ServiceLayer.Host), Shared]
    internal class VisualStudioSymbolSearchProgressService : ISymbolSearchProgressService
    {
        private readonly object _gate = new object();
        private readonly Lazy<IVsTaskStatusCenterService> _taskCenterServiceOpt;

        private TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();

        [ImportingConstructor]
        public VisualStudioSymbolSearchProgressService(VSShell.SVsServiceProvider serviceProvider)
        {
            _taskCenterServiceOpt = new Lazy<IVsTaskStatusCenterService>(() =>
                (IVsTaskStatusCenterService)serviceProvider.GetService(typeof(SVsTaskStatusCenterService)));
        }

        public async Task OnDownloadFullDatabaseStartedAsync(string title)
        {
            try
            {
                await OnDownloadFullDatabaseStartedWorkerAsync(title).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
            }
        }

        private Task OnDownloadFullDatabaseStartedWorkerAsync(string title)
        {
            var options = GetOptions(title);
            var data = new TaskProgressData
            {
                CanBeCanceled = false,
                PercentComplete = null,
            };

            TaskCompletionSource<bool> localTaskCompletionSource;

            lock (_gate)
            {
                // Take any existing tasks and move them to the complete state.
                _taskCompletionSource.TrySetResult(true);

                // Now create an existing task to track the current download and let
                // vs know about it.
                _taskCompletionSource = new TaskCompletionSource<bool>();
                localTaskCompletionSource = _taskCompletionSource;
            }

            var handler = _taskCenterServiceOpt.Value?.PreRegister(options, data);
            handler?.RegisterTask(localTaskCompletionSource.Task);

            return Task.CompletedTask;
        }

        private static TaskHandlerOptions GetOptions(string title)
        {
            var options = new TaskHandlerOptions
            {
                Title = title,
                ActionsAfterCompletion = CompletionActions.None
            };

            return options;
        }

        public Task OnDownloadFullDatabaseSucceededAsync()
        {
            lock (_gate)
            {
                _taskCompletionSource?.TrySetResult(true);
                return Task.CompletedTask;
            }
        }

        public Task OnDownloadFullDatabaseCanceledAsync()
        {
            lock (_gate)
            {
                _taskCompletionSource?.TrySetCanceled();
                return Task.CompletedTask;
            }
        }

        public Task OnDownloadFullDatabaseFailedAsync(string message)
        {
            lock (_gate)
            {
                _taskCompletionSource?.TrySetException(new Exception(message));
                return Task.CompletedTask;
            }
        }
    }
}
