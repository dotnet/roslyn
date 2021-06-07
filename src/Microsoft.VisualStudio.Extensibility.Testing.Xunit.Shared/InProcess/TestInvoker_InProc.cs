// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.InProcess
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Xunit.Abstractions;
    using Xunit.Harness;
    using Xunit.Sdk;
    using Xunit.Threading;

    internal class TestInvoker_InProc : InProcComponent
    {
        private TestInvoker_InProc()
        {
            AppDomain.CurrentDomain.AssemblyResolve += VisualStudioInstanceFactory.AssemblyResolveHandler;
        }

        public static TestInvoker_InProc Create()
            => new TestInvoker_InProc();

        public void LoadAssembly(string codeBase)
        {
            var assembly = Assembly.LoadFrom(codeBase);
        }

        public InProcessIdeTestAssemblyRunner CreateTestAssemblyRunner(ITestAssembly testAssembly, IXunitTestCase[] testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            return new InProcessIdeTestAssemblyRunner(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions);
        }

        public Tuple<decimal, Exception> InvokeTest(
            ITest test,
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod,
            object[] testMethodArguments)
        {
            var aggregator = new ExceptionAggregator();
            var beforeAfterAttributes = new BeforeAfterTestAttribute[0];
            var cancellationTokenSource = new CancellationTokenSource();

            var synchronizationContext = new DispatcherSynchronizationContext(Application.Current.Dispatcher, DispatcherPriority.Background);
            var result = Task.Factory.StartNew(
                async () =>
                {
                    try
                    {
                        var invoker = new XunitTestInvoker(
                            test,
                            messageBus,
                            testClass,
                            constructorArguments,
                            testMethod,
                            testMethodArguments,
                            beforeAfterAttributes,
                            aggregator,
                            cancellationTokenSource);
                        return await invoker.RunAsync();
                    }
                    catch (Exception)
                    {
                        Debugger.Launch();
                        throw;
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.None,
                new SynchronizationContextTaskScheduler(synchronizationContext)).Unwrap().GetAwaiter().GetResult();

            return Tuple.Create(result, aggregator.ToException());
        }
    }
}
