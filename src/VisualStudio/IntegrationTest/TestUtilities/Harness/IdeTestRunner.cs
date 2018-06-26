// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Harness
{
    public sealed class IdeTestRunner : WpfTestRunner
    {
        public IdeTestRunner(
            WpfTestSharedData sharedData,
            VisualStudioVersion visualStudioVersion,
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
            : base(sharedData, test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
            VisualStudioVersion = visualStudioVersion;
        }

        public VisualStudioVersion VisualStudioVersion
        {
            get;
        }

        protected override async Task<decimal> InvokeTestMethodAsync(ExceptionAggregator aggregator)
        {
            return await base.InvokeTestMethodAsync(aggregator).ConfigureAwait(true);
        }

        protected override Func<Task<decimal>> CreateTestInvoker(ExceptionAggregator aggregator)
        {
            return async () =>
            {
                Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());
                var instanceFactory = ConstructorArguments.OfType<VisualStudioInstanceFactory>().Single();

                // Install a COM message filter to handle retry operations when the first attempt fails
                using (var messageFilter = RegisterMessageFilter())
                {
                    Helper.Automation.TransactionTimeout = 20000;
                    using (var visualStudioContext = await instanceFactory.GetNewOrUsedInstanceAsync(SharedIntegrationHostFixture.RequiredPackageIds).ConfigureAwait(true))
                    {
                        visualStudioContext.Instance.LoadAssembly(typeof(ITest).Assembly.Location);
                        visualStudioContext.Instance.LoadAssembly(typeof(TestClassMessage).Assembly.Location);
                        visualStudioContext.Instance.LoadAssembly(typeof(Assert).Assembly.Location);
                        visualStudioContext.Instance.LoadAssembly(typeof(TheoryAttribute).Assembly.Location);
                        visualStudioContext.Instance.LoadAssembly(typeof(TestClass).Assembly.Location);

                        Assert.Empty(BeforeAfterAttributes);
                        var result = visualStudioContext.Instance.TestInvoker.InvokeTest(Test, new IpcMessageBus(MessageBus), TestClass, ConstructorArguments, TestMethod, TestMethodArguments);
                        if (result.Item2 != null)
                        {
                            aggregator.Add(result.Item2);
                        }

                        return result.Item1;
                    }
                }
            };
        }

        private static Version GetVersion(VisualStudioVersion visualStudioVersion)
        {
            switch (visualStudioVersion)
            {
                case VisualStudioVersion.VS2017:
                    return new Version(15, 0);

                default:
                    throw new ArgumentException();
            }
        }

        private MessageFilter RegisterMessageFilter()
            => new MessageFilter();

        private class IpcMessageBus : MarshalByRefObject, IMessageBus
        {
            private readonly IMessageBus _messageBus;

            public IpcMessageBus(IMessageBus messageBus)
            {
                _messageBus = messageBus;
            }

            public void Dispose() => _messageBus.Dispose();

            public bool QueueMessage(IMessageSinkMessage message) => _messageBus.QueueMessage(message);
        }
    }
}
