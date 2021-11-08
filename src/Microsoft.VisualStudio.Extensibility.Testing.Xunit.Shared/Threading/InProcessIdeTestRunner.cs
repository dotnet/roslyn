// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable disable

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Xunit.Abstractions;
    using Xunit.Harness;
    using Xunit.InProcess;
    using Xunit.Sdk;

    public class InProcessIdeTestRunner : XunitTestRunner
    {
        public InProcessIdeTestRunner(ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, string skipReason, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
        }

        protected override async Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
        {
            DataCollectionService.InstallFirstChanceExceptionHandler();
            VisualStudio_InProc.Create().ActivateMainWindow();

            var synchronizationContext = new DispatcherSynchronizationContext(Application.Current.Dispatcher, DispatcherPriority.Background);
            var taskScheduler = new SynchronizationContextTaskScheduler(synchronizationContext);
            try
            {
                DataCollectionService.CurrentTest = Test;
                return await Task.Factory.StartNew(
                    () => new InProcessIdeTestInvoker(Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource).RunAsync(),
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    taskScheduler).Unwrap();
            }
            finally
            {
                DataCollectionService.CurrentTest = null;
            }
        }
    }
}
