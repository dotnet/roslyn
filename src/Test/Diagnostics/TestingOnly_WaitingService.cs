// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Roslyn.Hosting.Diagnostics.Waiters
{
    [Export, Shared]
    public class TestingOnly_WaitingService
    {
        private readonly AsynchronousOperationListenerProvider _provider;

        [ImportingConstructor]
        private TestingOnly_WaitingService(IAsynchronousOperationListenerProvider provider)
        {
            _provider = (AsynchronousOperationListenerProvider)provider;
        }

        public void WaitForAsyncOperations(string featureName, bool waitForWorkspaceFirst = true)
        {
            WaitForAsyncOperations(TimeSpan.FromMilliseconds(-1), featureName, waitForWorkspaceFirst);
        }

        public void WaitForAsyncOperations(TimeSpan timeout, string featureName, bool waitForWorkspaceFirst = true)
        {
            var workspaceWaiter = _provider.GetWaiter(FeatureAttribute.Workspace);
            var featureWaiter = _provider.GetWaiter(featureName);
            Contract.ThrowIfNull(featureWaiter);

            using var cancellationTokenSource = new CancellationTokenSource(timeout);

            // wait for each of the features specified in the featuresToWaitFor string
            if (waitForWorkspaceFirst)
            {
                // at least wait for the workspace to finish processing everything.
                var task = workspaceWaiter.CreateExpeditedWaitTask();
                task.Wait(cancellationTokenSource.Token);
            }

            var waitTask = featureWaiter.CreateExpeditedWaitTask();
            WaitForTask(waitTask, cancellationTokenSource.Token);

            // Debugging trick: don't let the listeners collection get optimized away during execution.
            // This means if the process is killed during integration tests and the test was waiting, you can
            // easily look at the listeners and see what is going on. This is not actually needed to
            // affect the GC, nor is it needed for correctness.
            GC.KeepAlive(featureWaiter);
        }

        public void WaitForAllAsyncOperations(params string[] featureNames)
        {
            WaitForAllAsyncOperations(TimeSpan.FromMilliseconds(-1), featureNames);
        }

        public void WaitForAllAsyncOperations(TimeSpan timeout, params string[] featureNames)
        {
            var task = _provider.WaitAllAsync(
                featureNames,
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
                eventProcessingAction: () => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle));
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs

            if (timeout == TimeSpan.FromMilliseconds(-1))
            {
                WaitForTask(task, CancellationToken.None);
            }
            else
            {
                using (var cancellationTokenSource = new CancellationTokenSource(timeout))
                {
                    WaitForTask(task, cancellationTokenSource.Token);
                }
            }
        }

        public void EnableActiveTokenTracking(bool enable)
        {
            _provider.EnableDiagnosticTokens(enable);
        }

        public void Enable(bool enable)
        {
            AsynchronousOperationListenerProvider.Enable(enable);
        }

        private void WaitForTask(Task task, CancellationToken cancellationToken)
        {
            while (!task.Wait(100, cancellationToken))
            {
                // set breakpoint here when debugging
                var tokens = _provider.GetTokens();

                GC.KeepAlive(tokens);

                // make sure pending task that require UI threads to finish as well.
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle, cancellationToken);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
            }
        }
    }
}
