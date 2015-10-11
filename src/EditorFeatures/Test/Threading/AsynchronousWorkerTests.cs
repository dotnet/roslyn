// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Threading;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Threading
{
    public class AsynchronousWorkerTests
    {
        private readonly SynchronizationContext _foregroundSyncContext;

        public AsynchronousWorkerTests()
        {
            TestWorkspace.ResetThreadAffinity();
            _foregroundSyncContext = SynchronizationContext.Current;
            Assert.NotNull(_foregroundSyncContext);
        }

        // Ensure a background action actually runs on the background.
        [WpfFact]
        public void TestBackgroundAction()
        {
            var listener = new AggregateAsynchronousOperationListener(Enumerable.Empty<Lazy<IAsynchronousOperationListener, FeatureMetadata>>(), "Test");
            var worker = new AsynchronousSerialWorkQueue(listener);
            var doneEvent = new AutoResetEvent(initialState: false);

            var actionRan = false;
            worker.EnqueueBackgroundWork(() =>
            {
                // Assert.NotNull(SynchronizationContext.Current);
                Assert.NotSame(_foregroundSyncContext, SynchronizationContext.Current);
                actionRan = true;
                doneEvent.Set();
            }, GetType().Name + ".TestBackgroundAction", CancellationToken.None);

            doneEvent.WaitOne();
            Assert.True(actionRan);
        }

        [WpfFact]
        public void TestMultipleBackgroundAction()
        {
            // Test that background actions don't run at the same time.
            var listener = new AggregateAsynchronousOperationListener(Enumerable.Empty<Lazy<IAsynchronousOperationListener, FeatureMetadata>>(), "Test");
            var worker = new AsynchronousSerialWorkQueue(listener);
            var doneEvent = new AutoResetEvent(false);

            var action1Ran = false;
            var action2Ran = false;

            worker.EnqueueBackgroundWork(() =>
            {
                Assert.NotSame(_foregroundSyncContext, SynchronizationContext.Current);
                action1Ran = true;

                // Simulate work to ensure that if tasks overlap that we will 
                // see it.
                Thread.Sleep(1000);
                Assert.False(action2Ran);
            }, "Test", CancellationToken.None);

            worker.EnqueueBackgroundWork(() =>
            {
                Assert.NotSame(_foregroundSyncContext, SynchronizationContext.Current);
                action2Ran = true;
                doneEvent.Set();
            }, "Test", CancellationToken.None);

            doneEvent.WaitOne();
            Assert.True(action1Ran);
            Assert.True(action2Ran);
        }

        [WpfFact]
        public void TestBackgroundCancel1()
        {
            // Ensure that we can cancel a background action.
            var listener = new AggregateAsynchronousOperationListener(Enumerable.Empty<Lazy<IAsynchronousOperationListener, FeatureMetadata>>(), "Test");
            var worker = new AsynchronousSerialWorkQueue(listener);

            var taskRunningEvent = new AutoResetEvent(false);
            var cancelEvent = new AutoResetEvent(false);
            var doneEvent = new AutoResetEvent(false);

            var source = new CancellationTokenSource();
            var cancellationToken = source.Token;

            var actionRan = false;

            worker.EnqueueBackgroundWork(() =>
            {
                actionRan = true;

                Assert.NotSame(_foregroundSyncContext, SynchronizationContext.Current);
                Assert.False(cancellationToken.IsCancellationRequested);

                taskRunningEvent.Set();
                cancelEvent.WaitOne();

                Assert.True(cancellationToken.IsCancellationRequested);

                doneEvent.Set();
            }, "Test", source.Token);

            taskRunningEvent.WaitOne();

            source.Cancel();
            cancelEvent.Set();

            doneEvent.WaitOne();
            Assert.True(actionRan);
        }

        [WpfFact]
        public void TestBackgroundCancelOneAction()
        {
            // Ensure that when a background action is cancelled the next
            // one starts (if it has a different cancellation token).
            var listener = new AggregateAsynchronousOperationListener(Enumerable.Empty<Lazy<IAsynchronousOperationListener, FeatureMetadata>>(), "Test");
            var worker = new AsynchronousSerialWorkQueue(listener);

            var taskRunningEvent = new AutoResetEvent(false);
            var cancelEvent = new AutoResetEvent(false);
            var doneEvent = new AutoResetEvent(false);

            var source1 = new CancellationTokenSource();
            var source2 = new CancellationTokenSource();
            var token1 = source1.Token;
            var token2 = source2.Token;

            var action1Ran = false;
            var action2Ran = false;

            worker.EnqueueBackgroundWork(() =>
            {
                action1Ran = true;

                Assert.NotSame(_foregroundSyncContext, SynchronizationContext.Current);
                Assert.False(token1.IsCancellationRequested);

                taskRunningEvent.Set();
                cancelEvent.WaitOne();

                token1.ThrowIfCancellationRequested();
                Assert.True(false);
            }, "Test", source1.Token);

            worker.EnqueueBackgroundWork(() =>
            {
                action2Ran = true;

                Assert.NotSame(_foregroundSyncContext, SynchronizationContext.Current);
                Assert.False(token2.IsCancellationRequested);

                taskRunningEvent.Set();
                cancelEvent.WaitOne();

                doneEvent.Set();
            }, "Test", source2.Token);

            // Wait for the first task to start.
            taskRunningEvent.WaitOne();

            // Cancel it
            source1.Cancel();
            cancelEvent.Set();

            // Wait for the second task to start.
            taskRunningEvent.WaitOne();
            cancelEvent.Set();

            // Wait for the second task to complete.
            doneEvent.WaitOne();
            Assert.True(action1Ran);
            Assert.True(action2Ran);
        }

        [WpfFact]
        public void TestBackgroundCancelMultipleActions()
        {
            // Ensure that multiple background actions are cancelled if they
            // use the same cancellation token.
            var listener = new AggregateAsynchronousOperationListener(Enumerable.Empty<Lazy<IAsynchronousOperationListener, FeatureMetadata>>(), "Test");
            var worker = new AsynchronousSerialWorkQueue(listener);

            var taskRunningEvent = new AutoResetEvent(false);
            var cancelEvent = new AutoResetEvent(false);
            var doneEvent = new AutoResetEvent(false);

            var source = new CancellationTokenSource();
            var cancellationToken = source.Token;

            var action1Ran = false;
            var action2Ran = false;

            worker.EnqueueBackgroundWork(() =>
            {
                action1Ran = true;

                Assert.NotSame(_foregroundSyncContext, SynchronizationContext.Current);
                Assert.False(cancellationToken.IsCancellationRequested);

                taskRunningEvent.Set();
                cancelEvent.WaitOne();

                cancellationToken.ThrowIfCancellationRequested();
                Assert.True(false);
            }, "Test", source.Token);

            // We should not run this action.
            worker.EnqueueBackgroundWork(() =>
            {
                action2Ran = true;
                Assert.False(true);
            }, "Test", source.Token);

            taskRunningEvent.WaitOne();

            source.Cancel();
            cancelEvent.Set();

            try
            {
                worker.WaitUntilCompletion_ForTestingPurposesOnly();
                Assert.True(false);
            }
            catch (AggregateException ae)
            {
                Assert.IsAssignableFrom<OperationCanceledException>(ae.InnerException);
            }

            Assert.True(action1Ran);
            Assert.False(action2Ran);
        }
    }
}
