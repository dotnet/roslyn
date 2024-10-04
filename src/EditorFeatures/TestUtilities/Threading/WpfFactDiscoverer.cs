// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Roslyn.Test.Utilities
{
    public class WpfFactDiscoverer : FactDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;

        public WpfFactDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
            => _diagnosticMessageSink = diagnosticMessageSink;

        protected override IXunitTestCase CreateTestCase(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
            => new WpfTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod);
    }

    public class WpfTheoryDiscoverer : TheoryDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;

        public WpfTheoryDiscoverer(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink)
            => _diagnosticMessageSink = diagnosticMessageSink;

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object[] dataRow)
        {
            var testCase = new WpfTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, dataRow);
            return [testCase];
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
        {
            var testCase = new WpfTheoryTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod);
            return [testCase];
        }
    }
}
