// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class TestInvoker_InProc : InProcComponent
    {
        private TestInvoker_InProc()
        {
        }

        public static TestInvoker_InProc Create()
            => new TestInvoker_InProc();

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
                },
                CancellationToken.None,
                TaskCreationOptions.None,
                new SynchronizationContextTaskScheduler(synchronizationContext)).Unwrap().GetAwaiter().GetResult();

            return Tuple.Create(result, aggregator.ToException());
        }
    }
}
