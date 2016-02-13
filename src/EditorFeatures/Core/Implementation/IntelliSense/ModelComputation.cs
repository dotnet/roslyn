// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
{
    internal class ModelComputation<TModel> : ForegroundThreadAffinitizedObject
    {
        #region Fields that can be accessed from either thread

        private readonly CancellationToken _stopCancellationToken;

        /// <summary>
        /// Set when the first compute task completes
        /// </summary>
        private TModel _initialUnfilteredModel = default(TModel);

        #endregion

        #region Fields that can only be accessed from the foreground thread

        private readonly IController<TModel> _controller;
        private readonly TaskScheduler __taskScheduler;

        private TaskScheduler _taskScheduler
        {
            get
            {
                AssertIsForeground();
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

        public ModelComputation(IController<TModel> controller, TaskScheduler computationTaskScheduler)
        {
            _controller = controller;
            __taskScheduler = computationTaskScheduler;

            _stopTokenSource = new CancellationTokenSource();
            _stopCancellationToken = _stopTokenSource.Token;

            // Dummy up a new task so we don't need to check for null.
            _notifyControllerTask = _lastTask = SpecializedTasks.Default<TModel>();
        }

        public TModel InitialUnfilteredModel
        {
            get
            {
                AssertIsForeground();
                return _initialUnfilteredModel;
            }
        }

        public Task<TModel> ModelTask
        {
            get
            {
                AssertIsForeground();

                // We should never be called if we were stopped.
                Contract.ThrowIfTrue(_stopCancellationToken.IsCancellationRequested);
                return _lastTask;
            }
        }

        public TModel WaitForController()
        {
            AssertIsForeground();

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
            AssertIsForeground();

            // cancel all outstanding tasks.
            _stopTokenSource.Cancel();

            // reset task so that it doesn't hold onto things like WpfTextView
            _notifyControllerTask = _lastTask = SpecializedTasks.Default<TModel>();
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
            AssertIsForeground();
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
                new[] { _notifyControllerTask, nextTask },
                tasks =>
                    {
                        this.AssertIsForeground();
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
                _stopCancellationToken, TaskContinuationOptions.None, ForegroundTaskScheduler);

            // When we've notified the controller of our result, we consider the async operation
            // to be completed.
            _notifyControllerTask.CompletesAsyncOperation(asyncToken);
        }

        private void OnModelUpdated(TModel result, bool updateController)
        {
            this.AssertIsForeground();

            // Store the first result so that anyone who cares knows we've computed something
            if (_initialUnfilteredModel == null)
            {
                _initialUnfilteredModel = result;
            }

            if (updateController)
            {
                _controller.OnModelUpdated(result);
            }
        }
    }
}
