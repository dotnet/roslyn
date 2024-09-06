// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense;

internal class ModelComputation<TModel> where TModel : class
{
    #region Fields that can be accessed from either thread

    public readonly IThreadingContext ThreadingContext;
    private readonly CancellationToken _stopCancellationToken;

    /// <summary>
    /// Set when the first compute task completes
    /// </summary>
    private TModel _initialUnfilteredModel = null;

    #endregion

    #region Fields that can only be accessed from the foreground thread

    private readonly IController<TModel> _controller;
    private readonly TaskScheduler __taskScheduler;

    private TaskScheduler _taskScheduler
    {
        get
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            return __taskScheduler;
        }
    }

    private readonly CancellationTokenSource _stopTokenSource;

    // There may be multiple compute tasks chained together.  When a compute task finishes it
    // may end up with a null model (i.e. it found no items).  At that point *if* it is the
    // *last* compute task, then we will want to stop everything.  However, if it is not the
    // last compute task, then we just want to ignore that result and allow the actual
    // latest compute task to proceed.
    private Task<TModel> _lastTask;
    private Task _notifyControllerTask;

    #endregion

    public ModelComputation(
        IThreadingContext threadingContext,
        IController<TModel> controller,
        TaskScheduler computationTaskScheduler)
    {
        ThreadingContext = threadingContext;
        _controller = controller;
        __taskScheduler = computationTaskScheduler;

        _stopTokenSource = new CancellationTokenSource();
        _stopCancellationToken = _stopTokenSource.Token;

        // Dummy up a new task so we don't need to check for null.
        _notifyControllerTask = _lastTask = SpecializedTasks.Null<TModel>();
    }

    public TModel InitialUnfilteredModel
    {
        get
        {
            ThreadingContext.ThrowIfNotOnUIThread();
            return _initialUnfilteredModel;
        }
    }

    public Task<TModel> ModelTask
    {
        get
        {
            ThreadingContext.ThrowIfNotOnUIThread();

            // We should never be called if we were stopped.
            Contract.ThrowIfTrue(_stopCancellationToken.IsCancellationRequested);
            return _lastTask;
        }
    }

    public TModel WaitForController()
    {
        ThreadingContext.ThrowIfNotOnUIThread();

        var model = ModelTask.WaitAndGetResult(CancellationToken.None);
        if (!_notifyControllerTask.IsCompleted)
        {
            OnModelUpdated(model, updateController: true);

            // Reset lastTask so controller.OnModelUpdated is only called once
            _lastTask = Task.FromResult(model);
        }

        return model;
    }

    public virtual void Stop()
    {
        ThreadingContext.ThrowIfNotOnUIThread();

        // cancel all outstanding tasks.
        _stopTokenSource.Cancel();

        // reset task so that it doesn't hold onto things like WpfTextView
        _notifyControllerTask = _lastTask = SpecializedTasks.Null<TModel>();
    }

    public void ChainTaskAndNotifyControllerWhenFinished(
            Func<TModel, TModel> transformModel,
            bool updateController = true)
    {
        ChainTaskAndNotifyControllerWhenFinished((m, c) => Task.FromResult(transformModel(m)), updateController);
    }

    public void ChainTaskAndNotifyControllerWhenFinished(
        Func<TModel, CancellationToken, Task<TModel>> transformModelAsync,
        bool updateController = true)
    {
        ThreadingContext.ThrowIfNotOnUIThread();

        Contract.ThrowIfTrue(_stopCancellationToken.IsCancellationRequested, "should not chain tasks after we've been cancelled");

        // Mark that an async operation has begun.  This way tests know to wait until the
        // async operation is done before verifying results.  We will consider this
        // background task complete when its result has finally been displayed on the UI.
        var asyncToken = _controller.BeginAsyncOperation();

        // Create the task that will actually run the transformation step.
        var nextTask = _lastTask.SafeContinueWithFromAsync(
            t => transformModelAsync(t.Result, _stopCancellationToken),
            _stopCancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, _taskScheduler);

        // The next task is now the last task in the chain.
        _lastTask = nextTask;

        // When this task is complete *and* the last notification to the controller is
        // complete, then issue the next notification to the controller.  When we try to
        // issue the notification, see if we're still at the end of the chain.  If we're not,
        // then we don't need to notify as a later task will do so.
        _notifyControllerTask = Task.Factory.ContinueWhenAll(
            [_notifyControllerTask, nextTask],
            async tasks =>
            {
                await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, _stopCancellationToken);

                if (tasks.All(t => t.Status == TaskStatus.RanToCompletion))
                {
                    _stopCancellationToken.ThrowIfCancellationRequested();

                    // Check if we're still the last task.  If so then we should update the
                    // controller. Otherwise there's a pending task that should run.  We
                    // don't need to update the controller (and the presenters) until our
                    // chain is finished.
                    updateController &= nextTask == _lastTask;
                    OnModelUpdated(nextTask.Result, updateController);
                }
            },
            _stopCancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default).Unwrap();

        // When we've notified the controller of our result, we consider the async operation
        // to be completed.
        _notifyControllerTask.CompletesAsyncOperation(asyncToken);
    }

    private void OnModelUpdated(TModel result, bool updateController)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();

        // Store the first result so that anyone who cares knows we've computed something
        _initialUnfilteredModel ??= result;

        _controller.OnModelUpdated(result, updateController);
    }
}
