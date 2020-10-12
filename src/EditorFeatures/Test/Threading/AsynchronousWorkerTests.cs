﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Threading
{
    [UseExportProvider]
    public class AsynchronousWorkerTests
    {
        private readonly SynchronizationContext _foregroundSyncContext;

        public AsynchronousWorkerTests()
        {
            WpfTestRunner.RequireWpfFact($"Tests are testing {nameof(AsynchronousSerialWorkQueue)} which is designed to run methods on the UI thread");
            _foregroundSyncContext = SynchronizationContext.Current;
            Assert.NotNull(_foregroundSyncContext);
        }

        // Ensure a background action actually runs on the background.
        [WpfFact]
        public void TestBackgroundAction()
        {
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var threadingContext = exportProvider.GetExportedValue<IThreadingContext>();
            var listenerProvider = exportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();

            var worker = new AsynchronousSerialWorkQueue(threadingContext, listenerProvider.GetListener("Test"));
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
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var threadingContext = exportProvider.GetExportedValue<IThreadingContext>();
            var listenerProvider = exportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();

            // Test that background actions don't run at the same time.
            var worker = new AsynchronousSerialWorkQueue(threadingContext, listenerProvider.GetListener("Test"));
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
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var threadingContext = exportProvider.GetExportedValue<IThreadingContext>();
            var listenerProvider = exportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();

            // Ensure that we can cancel a background action.
            var worker = new AsynchronousSerialWorkQueue(threadingContext, listenerProvider.GetListener("Test"));

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
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var threadingContext = exportProvider.GetExportedValue<IThreadingContext>();
            var listenerProvider = exportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();

            // Ensure that when a background action is cancelled the next
            // one starts (if it has a different cancellation token).
            var worker = new AsynchronousSerialWorkQueue(threadingContext, listenerProvider.GetListener("Test"));

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
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var threadingContext = exportProvider.GetExportedValue<IThreadingContext>();
            var listenerProvider = exportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();

            // Ensure that multiple background actions are cancelled if they
            // use the same cancellation token.
            var worker = new AsynchronousSerialWorkQueue(threadingContext, listenerProvider.GetListener("Test"));

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
                worker.GetTestAccessor().WaitUntilCompletion();
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
