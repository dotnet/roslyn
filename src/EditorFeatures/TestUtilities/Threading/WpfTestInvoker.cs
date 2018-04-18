// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Roslyn.Test.Utilities
{
    public class WpfTestInvoker : XunitTestInvoker
    {
        public WpfTestSharedData SharedData { get; }
        public WpfTestInvoker(
            WpfTestSharedData sharedData,
            ITest test,
            IMessageBus messageBus,
            Type testClass,
            object[] constructorArguments,
            MethodInfo testMethod,
            object[] testMethodArguments,
            IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
        : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
            SharedData = sharedData;
        }

        protected override object CallTestMethod(object testClassInstance)
        {
            SharedData.MonitorActiveAsyncTestSyncContext();
            return base.CallTestMethod(testClassInstance);
        }
    }
}
