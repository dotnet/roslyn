// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xunit.Sdk;
using Xunit.v3;

namespace Roslyn.Test.Utilities;

internal static class WpfTestCaseRunner
{
    private static readonly DispatcherSynchronizationContext s_dispatcherSynchronizationContext = CreateDispatcherSynchronizationContext();
    private static readonly TaskScheduler s_taskScheduler = new SynchronizationContextTaskScheduler(s_dispatcherSynchronizationContext);
#pragma warning disable RS0030 // The runner acquires this gate asynchronously before starting a WPF test.
    private static readonly SemaphoreSlim s_testSerializationGate = new(1, 1);
#pragma warning restore RS0030

    internal static ValueTask<RunSummary> Run(
        IXunitTestCase testCase,
        ExplicitOption explicitOption,
        IMessageBus messageBus,
        object?[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource,
        ParallelMode parallelMode,
        ExecutionScheduler scheduler,
        FixtureMappingManager methodFixtureMappings)
    {
        var runTask = Task.Factory.StartNew(
            async () =>
            {
                await s_testSerializationGate.WaitAsync(cancellationTokenSource.Token);
                try
                {
                    var tests = await aggregator.RunAsync(testCase.CreateTests, []);
                    if (aggregator.ToException() is Exception exception)
                    {
                        if (exception.Message.StartsWith(DynamicSkipToken.Value, StringComparison.Ordinal))
                        {
                            return XunitRunnerHelper.SkipTestCases(
                                messageBus,
                                cancellationTokenSource,
                                [testCase],
                                exception.Message.Substring(DynamicSkipToken.Value.Length),
                                sendTestCaseMessages: false);
                        }

                        return XunitRunnerHelper.FailTestCases(
                            messageBus,
                            cancellationTokenSource,
                            [testCase],
                            exception,
                            sendTestCaseMessages: false);
                    }

                    return await XunitTestCaseRunner.Instance.Run(
                        testCase,
                        tests,
                        messageBus,
                        aggregator,
                        cancellationTokenSource,
                        parallelMode,
                        scheduler,
                        testCase.TestCaseDisplayName,
                        testCase.SkipReason,
                        explicitOption,
                        constructorArguments,
                        methodFixtureMappings);
                }
                finally
                {
                    ReleaseClipboardOwnership();
                    s_testSerializationGate.Release();
                }
            },
            cancellationTokenSource.Token,
            TaskCreationOptions.None,
            s_taskScheduler).Unwrap();

        return new ValueTask<RunSummary>(runTask.GetAwaiter().GetResult());
    }

    /// <summary>
    /// All WPF tests share this STA thread, so clipboard data copied by one test stays owned by a live window on the
    /// thread. Clipboard listeners in other processes (e.g. clipboard history) may then need this thread to render
    /// that data while a later test is running synchronously, holding the clipboard open and making clipboard
    /// operations in that test fail with <c>CLIPBRD_E_CANT_OPEN</c>. Release ownership after each test, similar to
    /// how the clipboard is released when a per-test STA thread exits.
    /// </summary>
    private static void ReleaseClipboardOwnership()
    {
        var owner = GetClipboardOwner();
        if (owner == IntPtr.Zero || GetWindowThreadProcessId(owner, out _) != GetCurrentThreadId())
            return;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                EmptyClipboard();
                CloseClipboard();
                return;
            }

            // Another process may be waiting on this thread to render clipboard data. Process sent messages before
            // retrying.
            _ = MsgWaitForMultipleObjectsEx(0, IntPtr.Zero, 50, QS_SENDMESSAGE, 0);
            _ = PeekMessage(out _, IntPtr.Zero, 0, 0, PM_NOREMOVE);
        }
    }

    private const uint QS_SENDMESSAGE = 0x0040;
    private const uint PM_NOREMOVE = 0x0000;

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int ptX;
        public int ptY;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardOwner();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(IntPtr newOwner);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PeekMessage(out MSG message, IntPtr hwnd, uint messageFilterMin, uint messageFilterMax, uint removeMessage);

    [DllImport("user32.dll")]
    private static extern uint MsgWaitForMultipleObjectsEx(uint count, IntPtr handles, uint milliseconds, uint wakeMask, uint flags);

    private static DispatcherSynchronizationContext CreateDispatcherSynchronizationContext()
    {
        var synchronizationContextSource = new TaskCompletionSource<DispatcherSynchronizationContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var synchronizationContext = new DispatcherSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            synchronizationContextSource.SetResult(synchronizationContext);
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "Roslyn WPF test thread",
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return synchronizationContextSource.Task.GetAwaiter().GetResult();
    }

    private sealed class SynchronizationContextTaskScheduler(SynchronizationContext synchronizationContext) : TaskScheduler
    {
        protected override void QueueTask(Task task)
        {
#pragma warning disable VSTHRD001 // Post to the dispatcher context to preserve WPF thread affinity.
            synchronizationContext.Post(_ => TryExecuteTask(task), null);
#pragma warning restore VSTHRD001 // Post to the dispatcher context to preserve WPF thread affinity.
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            => SynchronizationContext.Current == synchronizationContext && TryExecuteTask(task);

        protected override IEnumerable<Task> GetScheduledTasks()
            => [];

        public override int MaximumConcurrencyLevel => 1;
    }
}
