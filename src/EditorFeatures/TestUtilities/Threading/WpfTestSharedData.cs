// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Roslyn.Test.Utilities
{
    [Serializable]
    public sealed class WpfTestSharedData
    {
        internal static readonly WpfTestSharedData Instance = new WpfTestSharedData();

        /// <summary>
        /// The name of a <see cref="Semaphore"/> used to ensure that only a single
        /// <see cref="WpfFactAttribute"/>-attributed test runs at once. This requirement must be made because,
        /// currently, <see cref="WpfTestCase"/>'s logic sets various static state before a method runs. If two tests
        /// run interleaved on the same scheduler (i.e. if one yields with an await) then all bets are off.
        /// </summary>
        internal static readonly Guid TestSerializationGateName = Guid.NewGuid();

        /// <summary>
        /// Holds the last 10 test cases executed: more recent test cases will occur later in the 
        /// list. Useful for debugging deadlocks that occur because state leak between runs. 
        /// </summary>
        private readonly List<string> _recentTestCases = new List<string>();

        private readonly ConditionalWeakTable<AsyncTestSyncContext, object> _contextTrackingTable = new ConditionalWeakTable<AsyncTestSyncContext, object>();

        public Semaphore TestSerializationGate = new Semaphore(1, 1, TestSerializationGateName.ToString("N"));

        private WpfTestSharedData()
        {

        }

        public void ExecutingTest(ITestMethod testMethod)
        {
            var name = $"{testMethod.TestClass.Class.Name}::{testMethod.Method.Name}";
            lock (_recentTestCases)
            {
                _recentTestCases.Add(name);
            }
        }

        public void ExecutingTest(MethodInfo testMethod)
        {
            var name = $"{testMethod.DeclaringType.Name}::{testMethod.Name}";
            lock (_recentTestCases)
            {
                _recentTestCases.Add(name);
            }
        }

        /// <summary>
        /// When a <see cref="SynchronizationContext"/> instance is used in a <see cref="WpfFactAttribute"/>
        /// test it can cause a deadlock. This happens when there are posted actions that are not run and the test
        /// case is non-async. 
        /// 
        /// The xunit framework monitors all calls to the active <see cref="SynchronizationContext"/> and it will 
        /// wait on them to complete before finishing a test. Hence if anything is posted but not run the test will
        /// deadlock forever waiting for this to happen.
        /// 
        /// This code monitors the use of our <see cref="SynchronizationContext"/> and attempts to 
        /// detect this situation and actively fail the test when it happens. The code is a hueristic and hence 
        /// imprecise. But is effective in finding these problmes.
        /// </summary>
        public void MonitorActiveAsyncTestSyncContext()
        {
            // To cause the test to fail we need to post an action ot the AsyncTestContext. The xunit framework 
            // wraps such delegates in a try / catch and fails the test if any exception occurs. This is best
            // captured at the point a posted action occurs. 
            var asyncContext = SynchronizationContext.Current as AsyncTestSyncContext;
            if (_contextTrackingTable.TryGetValue(asyncContext, out _))
            {
                return;
            }

            var dispatcher = Dispatcher.CurrentDispatcher;

            void runCallbacks()
            {
                var fieldInfo = asyncContext.GetType().GetField("innerContext", BindingFlags.NonPublic | BindingFlags.Instance);
                var innerContext = fieldInfo.GetValue(asyncContext) as SynchronizationContext;
                switch (innerContext)
                {
                    case DispatcherSynchronizationContext _:
                        dispatcher.DoEvents();
                        break;
                    default:
                        Debug.Fail($"Unrecognized context: {asyncContext.GetType()}");
                        break;
                }
            }

            _contextTrackingTable.Add(asyncContext, new object());
            var startTime = DateTime.UtcNow;
            void checkForBad()
            {
                try
                {
                    if (!asyncContext.WaitForCompletionAsync().IsCompleted)
                    {
                        var span = DateTime.UtcNow - startTime;
                        if (span > TimeSpan.FromSeconds(30) && !Debugger.IsAttached)
                        {
                            asyncContext?.Post(_ => throw new Exception($"Unfulfilled {nameof(SynchronizationContext)} detected"), null);
                            runCallbacks();
                        }
                        else
                        {
                            var timer = new Task(() =>
                            {
                                Thread.Sleep(TimeSpan.FromSeconds(2));
                                queueCheckForBad();

                            });
                            timer.Start(TaskScheduler.Default);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Fail($"Exception monitoring {nameof(SynchronizationContext)}: {ex.Message}");
                }
            }

            void queueCheckForBad()
            {
                var task = new Task((Action)checkForBad);
                task.Start(new SynchronizationContextTaskScheduler(StaTaskScheduler.DefaultSta.DispatcherSynchronizationContext));
            }

            queueCheckForBad();
        }
    }
}
