// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
    private static readonly SemaphoreSlim s_testSerializationGate = new(1, 1);

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
                    IReadOnlyCollection<IXunitTest> tests = await aggregator.RunAsync(testCase.CreateTests, []);
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
                    s_testSerializationGate.Release();
                }
            },
            cancellationTokenSource.Token,
            TaskCreationOptions.None,
            s_taskScheduler).Unwrap();

        return new ValueTask<RunSummary>(runTask.GetAwaiter().GetResult());
    }

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
            => synchronizationContext.Post(_ => TryExecuteTask(task), null);

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            => SynchronizationContext.Current == synchronizationContext && TryExecuteTask(task);

        protected override IEnumerable<Task> GetScheduledTasks()
            => [];

        public override int MaximumConcurrencyLevel => 1;
    }
}
