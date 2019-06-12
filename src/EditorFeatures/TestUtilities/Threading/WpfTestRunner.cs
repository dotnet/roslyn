// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// This type is actually responsible for spinning up the STA context to run all of the
    /// tests. 
    /// 
    /// Overriding the <see cref="XunitTestInvoker"/> to setup the STA context is not the correct 
    /// approach. That type begins constructing types before RunAsync and hence ctors end up 
    /// running on the current thread vs. the STA ones. Just completely wrapping the invocation
    /// here is the best case. 
    /// </summary>
    public sealed class WpfTestRunner : XunitTestRunner
    {
        private static readonly ImmutableDictionary<string, TestInfo> _passedTests;
        private static string s_wpfFactRequirementReason;

        public WpfTestSharedData SharedData { get; }

        static WpfTestRunner()
        {
            _passedTests = TestInfo.GetPassedTestsInfo();
        }

        public WpfTestRunner(
            WpfTestSharedData sharedData,
            ITest test,
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod,
            object[] testMethodArguments,
            string skipReason,
            IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
            SharedData = sharedData;
        }

        protected override Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
        {
            SharedData.ExecutingTest(TestMethod);
            var sta = StaTaskScheduler.DefaultSta;
            var task = Task.Factory.StartNew(async () =>
            {
                Debug.Assert(sta.StaThread == Thread.CurrentThread);

                using (await SharedData.TestSerializationGate.DisposableWaitAsync(CancellationToken.None))
                {
                    try
                    {
                        Debug.Assert(SynchronizationContext.Current is DispatcherSynchronizationContext);

                        // Reset our flag ensuring that part of this test actually needs WpfFact
                        s_wpfFactRequirementReason = null;

                        if (_passedTests.TryGetValue(Test.DisplayName, out var info))
                        {
                            return info.Time;
                        }
                        else
                        {
                            // Just call back into the normal xUnit dispatch process now that we are on an STA Thread with no synchronization context.
                            var invoker = new XunitTestInvoker(Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource);
                            return invoker.RunAsync().JoinUsingDispatcher(CancellationTokenSource.Token);
                        }
                    }
                    finally
                    {
                        // Cleanup the synchronization context even if the test is failing exceptionally
                        SynchronizationContext.SetSynchronizationContext(null);
                    }
                }
            }, CancellationTokenSource.Token, TaskCreationOptions.None, new SynchronizationContextTaskScheduler(sta.DispatcherSynchronizationContext));

            return task.Unwrap();
        }

        /// <summary>
        /// Asserts that the test is running on a <see cref="WpfFactAttribute"/> or <see cref="WpfTheoryAttribute"/>
        /// test method, and records the reason for requiring the use of an STA thread.
        /// </summary>
        internal static void RequireWpfFact(string reason)
        {
            if (!(TestExportJoinableTaskContext.GetEffectiveSynchronizationContext() is DispatcherSynchronizationContext))
            {
                throw new InvalidOperationException($"This test requires {nameof(WpfFactAttribute)} because '{reason}' but is missing {nameof(WpfFactAttribute)}. Either the attribute should be changed, or the reason it needs an STA thread audited.");
            }

            s_wpfFactRequirementReason = reason;
        }
    }
}
