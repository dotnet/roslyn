using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.Extensions;
using Xunit.Abstractions;

namespace Vim.UnitTest.Utilities
{
    public class WpfFactDiscoverer : FactDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;
 
        public WpfFactDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        protected override IXunitTestCase CreateTestCase(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
            => new WpfTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod);
    }

    public class WpfTheoryDiscoverer : TheoryDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;
 
        public WpfTheoryDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object[] dataRow)
        {
            var testCase = new WpfTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, dataRow);
            return new[] { testCase };
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
        {
            var testCase = new WpfTheoryTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod);
            return new[] { testCase };
        }
    }
}
