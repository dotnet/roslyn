// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System.Collections.Generic;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public class IdeTheoryDiscoverer : TheoryDiscoverer
    {
        public IdeTheoryDiscoverer(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForSkip(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, string skipReason)
        {
            var rootSuffix = IdeFactDiscoverer.GetRootSuffix(testMethod, theoryAttribute);
            foreach (var supportedVersion in IdeFactDiscoverer.GetSupportedVersions(testMethod, theoryAttribute))
            {
                yield return new IdeTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedVersion, rootSuffix);
            }
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForSkippedDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object?[] dataRow, string skipReason)
        {
            foreach (var supportedVersion in IdeFactDiscoverer.GetSupportedVersions(testMethod, theoryAttribute))
            {
                yield return new IdeSkippedDataRowTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedVersion, skipReason, dataRow);
            }
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object?[] dataRow)
        {
            var rootSuffix = IdeFactDiscoverer.GetRootSuffix(testMethod, theoryAttribute);
            foreach (var supportedVersion in IdeFactDiscoverer.GetSupportedVersions(testMethod, theoryAttribute))
            {
                yield return new IdeTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedVersion, rootSuffix, dataRow);
            }
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
        {
            var rootSuffix = IdeFactDiscoverer.GetRootSuffix(testMethod, theoryAttribute);
            foreach (var supportedVersion in IdeFactDiscoverer.GetSupportedVersions(testMethod, theoryAttribute))
            {
                yield return new IdeTheoryTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedVersion, rootSuffix);
            }
        }
    }
}
