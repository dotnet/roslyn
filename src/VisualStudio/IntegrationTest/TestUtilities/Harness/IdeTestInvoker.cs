// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Harness
{
    public class IdeTestInvoker : XunitTestInvoker
    {
        public IdeTestInvoker(ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
        }

        protected override object CallTestMethod(object testClassInstance)
        {
            // Exceptions thrown synchronously when invoking the test method will be captured via the TargetInvocationException
            var result = base.CallTestMethod(testClassInstance);
            if (!(result is Task task))
            {
                return result;
            }

            return ScreenshotOnFailureAsync(task);
        }

        private async Task ScreenshotOnFailureAsync(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex) when (!InProcessIdeTestAssemblyRunner.IsCapturedFirstChanceException(ex))
            {
                // This exception failed a test, but wasn't by the first-chance exception handler. Make sure to record
                // it here.
                InProcessIdeTestAssemblyRunner.SaveScreenshot(ex);
                throw;
            }
        }
    }
}
