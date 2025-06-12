// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

using WorkTask = Func<PackageLoadTasks, CancellationToken, Task>;

/// <summary>
/// Provides a mechanism for registering work to be done during package initialization. Work is registered
/// as either main thread or background thread appropriate. This allows processing of these work items
/// in a batched manner, reducing the number of thread switches required during the performance sensitive
/// package loading timeframe.
/// 
/// Note that currently the processing of these tasks isn't done concurrently. A future optimization may
/// allow parallel background thread task execution, or even concurrent main and background thread work.
/// </summary>
internal sealed class PackageLoadTasks(JoinableTaskFactory jtf)
{
    private readonly ConcurrentQueue<WorkTask> _backgroundThreadWorkTasks = [];
    private readonly ConcurrentQueue<WorkTask> _mainThreadWorkTasks = [];
    private readonly JoinableTaskFactory _jtf = jtf;

    public void AddTask(bool isMainThreadTask, WorkTask task)
    {
        var workTasks = GetWorkTasks(isMainThreadTask);
        workTasks.Enqueue(task);
    }

    public async Task ProcessTasksAsync(CancellationToken cancellationToken)
    {
        // prime the pump by doing the first group of bg thread work if the initiating thread is not the main thread
        if (!_jtf.Context.IsOnMainThread)
            await PerformWorkAsync(isMainThreadTask: false, cancellationToken).ConfigureAwait(false);

        // Continue processing work until everything is completed, switching between main and bg threads as needed.
        while (!_mainThreadWorkTasks.IsEmpty || !_backgroundThreadWorkTasks.IsEmpty)
        {
            await PerformWorkAsync(isMainThreadTask: true, cancellationToken).ConfigureAwait(false);
            await PerformWorkAsync(isMainThreadTask: false, cancellationToken).ConfigureAwait(false);
        }
    }

    private ConcurrentQueue<WorkTask> GetWorkTasks(bool isMainThreadTask)
        => isMainThreadTask ? _mainThreadWorkTasks : _backgroundThreadWorkTasks;

    private async Task PerformWorkAsync(bool isMainThreadTask, CancellationToken cancellationToken)
    {
        var workTasks = GetWorkTasks(isMainThreadTask);
        if (workTasks.IsEmpty)
            return;

        // Ensure we're invoking the task on the right thread
        if (isMainThreadTask)
            await _jtf.SwitchToMainThreadAsync(cancellationToken);
        else if (_jtf.Context.IsOnMainThread)
            await TaskScheduler.Default;

        while (workTasks.TryDequeue(out var work))
        {
            // CA(true) is important here, as we want to ensure that each iteration is done in the same
            // captured context. Thus, even poorly behaving tasks (ie, those that do their own thread switching)
            // don't effect the next loop iteration.
            await work(this, cancellationToken).ConfigureAwait(true);
        }
    }
}
