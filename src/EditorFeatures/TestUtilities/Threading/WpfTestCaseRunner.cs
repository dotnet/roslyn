// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit.v3;

namespace Roslyn.Test.Utilities;

/// <summary>
/// A xUnit v3 test case runner that dispatches <see cref="WpfTestCase"/> and
/// <see cref="WpfTheoryTestCase"/> tests to an STA thread with a WPF
/// <see cref="System.Windows.Threading.Dispatcher"/>.
/// </summary>
public sealed class WpfTestCaseRunner : XunitTestCaseRunner
{
    /// <summary>Singleton instance used to replace <see cref="XunitTestCaseRunner.Instance"/>.</summary>
    public static readonly WpfTestCaseRunner WpfInstance = new();

    private static int s_injected;

    /// <summary>
    /// Replaces <see cref="XunitTestCaseRunner.Instance"/> with <see cref="WpfInstance"/> so
    /// that WPF test cases are dispatched to the STA thread.  Safe to call multiple times.
    /// </summary>
    public static void InjectIfNeeded()
    {
        if (Interlocked.CompareExchange(ref s_injected, 1, 0) != 0)
            return;

        // XunitTestCaseRunner.Instance is a read-only auto-property; write the backing field directly.
        var field = typeof(XunitTestCaseRunner).GetField(
            "<Instance>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Static);
        field?.SetValue(null, WpfInstance);
    }

    protected override async ValueTask<RunSummary> RunTest(
        XunitTestCaseRunnerContext ctxt,
        IXunitTest test)
    {
        // Non-WPF test cases run normally.
        if (ctxt.TestCase is not WpfTestCase and not WpfTheoryTestCase)
            return await base.RunTest(ctxt, test);

        return await RunOnStaThread(ctxt, test);
    }

    private static async Task<RunSummary> RunOnStaThread(XunitTestCaseRunnerContext ctxt, IXunitTest test)
    {
        WpfTestSharedData.Instance.ExecutingTest(ctxt.TestCase.TestMethod.Method);
        var sta = StaTaskScheduler.DefaultSta;
        var task = Task.Factory.StartNew(async () =>
        {
            Debug.Assert(sta.StaThread == Thread.CurrentThread);

            using (await WpfTestSharedData.Instance.TestSerializationGate.DisposableWaitAsync(CancellationToken.None))
            {
                try
                {
                    Debug.Assert(SynchronizationContext.Current is DispatcherSynchronizationContext);

                    // Reset the WpfFact requirement reason before each test.
                    WpfTestRunner.s_wpfFactRequirementReason = null;

                    return await new XunitTestRunner().Run(
                        test,
                        ctxt.MessageBus,
                        ctxt.ConstructorArguments,
                        ctxt.ExplicitOption,
                        ctxt.Aggregator,
                        ctxt.CancellationTokenSource,
                        ctxt.BeforeAfterTestAttributes);
                }
                finally
                {
                    // Clean up the synchronization context after the test.
                    SynchronizationContext.SetSynchronizationContext(null);
                }
            }
        }, ctxt.CancellationTokenSource.Token, TaskCreationOptions.None,
           new SynchronizationContextTaskScheduler(sta.DispatcherSynchronizationContext));

        return await task.Unwrap();
    }
}
