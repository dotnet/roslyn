// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

internal class PackageRegistrationTasks(JoinableTaskFactory jtf)
{
    private readonly List<Func<IProgress<ServiceProgressData>, PackageRegistrationTasks, CancellationToken, Task>> _bgThreadWorkTasks = new();
    private readonly List<Func<IProgress<ServiceProgressData>, PackageRegistrationTasks, CancellationToken, Task>> _mainThreadWorkTasks = new();
    private readonly JoinableTaskFactory _jtf = jtf;

    public void AddTask(bool isMainThreadTask, Func<IProgress<ServiceProgressData>, PackageRegistrationTasks, CancellationToken, Task> task)
    {
        if (isMainThreadTask)
            _mainThreadWorkTasks.Add(task);
        else
            _bgThreadWorkTasks.Add(task);
    }

    public async Task ProcessTasksAsync(IProgress<ServiceProgressData> progress, CancellationToken cancellationToken)
    {
        // prime the pump by doing the first group of bg thread work if the initiating thread is not the main thread
        if (!_jtf.Context.IsOnMainThread)
            await PerformWorkAsync(useMainThread: false, progress, cancellationToken).ConfigureAwait(false);

        // Continue processing work until everything is completed, switching between main and bg threads as needed.
        while (_mainThreadWorkTasks.Count > 0 || _bgThreadWorkTasks.Count > 0)
        {
            await PerformWorkAsync(useMainThread: true, progress, cancellationToken).ConfigureAwait(false);
            await PerformWorkAsync(useMainThread: false, progress, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task PerformWorkAsync(bool useMainThread, IProgress<ServiceProgressData> progress, CancellationToken cancellationToken)
    {
        var workTasks = useMainThread ? _mainThreadWorkTasks : _bgThreadWorkTasks;

        if (workTasks.Count == 0)
            return;

        // Ensure we're invoking the task on the right thread
        if (useMainThread)
            await _jtf.SwitchToMainThreadAsync(cancellationToken);
        else if (_jtf.Context.IsOnMainThread)
            await TaskScheduler.Default;

        for (var i = 0; i < workTasks.Count; i++)
        {
            var work = workTasks[i];

            // CA(true) is important here, as we want to ensure that each iteration is done in the same
            // captured context. Thus, even poorly behaving tasks (ie, those that do their own thread switching)
            // don't effect the next loop iteration.
            await work(progress, this, cancellationToken).ConfigureAwait(true);
        }

        workTasks.Clear();
    }
}
