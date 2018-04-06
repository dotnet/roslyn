using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class WpfTheoryTestCase : XunitTheoryTestCase
    {
        public WpfTestSharedData SharedData { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public WpfTheoryTestCase() { }

        public WpfTheoryTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod)
        {
            SharedData = WpfTestSharedData.Instance;
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            base.Deserialize(data);
            SharedData = WpfTestSharedData.Instance;
        }

        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            var runner = new WpfTheoryTestCaseRunner(SharedData, this, DisplayName, SkipReason, constructorArguments, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource);
            return runner.RunAsync();
        }
    }
}
