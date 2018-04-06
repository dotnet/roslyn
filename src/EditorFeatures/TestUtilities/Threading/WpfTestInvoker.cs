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
