// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
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
        private static string s_wpfFactRequirementReason;

        public WpfTestSharedData SharedData { get; }

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
                        // All WPF Tests need a DispatcherSynchronizationContext and we dont want to block pending keyboard
                        // or mouse input from the user. So use background priority which is a single level below user input.
                        var context = new DispatcherSynchronizationContext();

                        // xUnit creates its own synchronization context and wraps any existing context so that messages are
                        // still pumped as necessary. So we are safe setting it here, where we are not safe setting it in test.
                        SynchronizationContext.SetSynchronizationContext(context);

                        // Sync up FTAO to the context that we are creating here. 
                        ForegroundThreadAffinitizedObject.CurrentForegroundThreadData = new ForegroundThreadData(
                            Thread.CurrentThread,
                            new SynchronizationContextTaskScheduler(new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher, DispatcherPriority.Background)),
                            ForegroundThreadDataKind.StaUnitTest);

                        // Reset our flag ensuring that part of this test actually needs WpfFact
                        s_wpfFactRequirementReason = null;

                        // Just call back into the normal xUnit dispatch process now that we are on an STA Thread with no synchronization context.
                        var invoker = new WpfTestInvoker(SharedData, Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource);
                        return invoker.RunAsync().JoinUsingDispatcher(CancellationTokenSource.Token);
                    }
                    finally
                    {
                        // Cleanup the synchronization context even if the test is failing exceptionally
                        SynchronizationContext.SetSynchronizationContext(null);
                    }
                }
            }, CancellationTokenSource.Token, TaskCreationOptions.None, sta);

            return task.Unwrap();
        }

        /// <summary>
        /// Asserts that the test is running on a <see cref="WpfFactAttribute"/> or <see cref="WpfTheoryAttribute"/>
        /// test method, and records the reason for requiring the use of an STA thread.
        /// </summary>
        public static void RequireWpfFact(string reason)
        {
            if (ForegroundThreadDataInfo.CurrentForegroundThreadDataKind != ForegroundThreadDataKind.StaUnitTest)
            {
                throw new Exception($"This test requires {nameof(WpfFactAttribute)} because '{reason}' but is missing {nameof(WpfFactAttribute)}. Either the attribute should be changed, or the reason it needs an STA thread audited.");
            }

            s_wpfFactRequirementReason = reason;
        }
    }
}
