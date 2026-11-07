// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Xunit.Sdk;
using Xunit.v3;

namespace Roslyn.Test.Utilities;

internal static class WpfTestCaseRunner
{
    private static readonly TaskScheduler s_taskScheduler = new SynchronizationContextTaskScheduler(StaTaskScheduler.DefaultSta.DispatcherSynchronizationContext);

#pragma warning disable RS0030 // This is only acquired asynchronously when we are starting a WPF test
    private static readonly SemaphoreSlim s_testSerializationGate = new(1, 1);
#pragma warning restore RS0030

    internal static async ValueTask<RunSummary> Run(
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
        using (await s_testSerializationGate.DisposableWaitAsync(cancellationTokenSource.Token).ConfigureAwait(false))
        {
            WpfTestRunner.ResetWpfFactRequirementReason();

            return await Task.Factory.StartNew(
                () => XunitRunnerHelper.RunXunitTestCase(
                    testCase,
                    messageBus,
                    cancellationTokenSource,
                    parallelMode,
                    scheduler,
                    aggregator,
                    explicitOption,
                    constructorArguments,
                    methodFixtureMappings).AsTask(),
                cancellationTokenSource.Token,
                TaskCreationOptions.None,
                s_taskScheduler).Unwrap().ConfigureAwait(false);
        }
    }
}
