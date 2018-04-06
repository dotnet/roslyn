using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Vim.EditorHost;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Vim.UnitTest.Utilities
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

                        // Just call back into the normal xUnit dispatch process now that we are on an STA Thread with no synchronization context.
                        var invoker = new WpfTestInvoker(SharedData, Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource);
                        var baseTask = invoker.RunAsync();
                        do
                        {
                            var delay = Task.Delay(TimeSpan.FromMilliseconds(10), CancellationTokenSource.Token);
                            var completed = await Task.WhenAny(baseTask, delay).ConfigureAwait(false);
                            if (completed == baseTask)
                            {
                                return await baseTask.ConfigureAwait(false);
                            }
                        }
                        while (true);
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
        
    }
}
