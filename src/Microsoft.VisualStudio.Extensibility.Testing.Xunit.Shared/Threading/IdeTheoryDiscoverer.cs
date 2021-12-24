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
            foreach (var supportedInstance in IdeFactDiscoverer.GetSupportedInstances(testMethod, theoryAttribute))
            {
                yield return new IdeTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedInstance);
                if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, DiagnosticMessageSink, supportedInstance) is { } instanceTestCase)
                {
                    yield return instanceTestCase;
                }
            }
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForSkippedDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object?[] dataRow, string skipReason)
        {
            foreach (var supportedInstance in IdeFactDiscoverer.GetSupportedInstances(testMethod, theoryAttribute))
            {
                yield return new IdeSkippedDataRowTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedInstance, skipReason, dataRow);
            }
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object?[] dataRow)
        {
            foreach (var supportedInstance in IdeFactDiscoverer.GetSupportedInstances(testMethod, theoryAttribute))
            {
                yield return new IdeTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedInstance, dataRow);
                if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, DiagnosticMessageSink, supportedInstance) is { } instanceTestCase)
                {
                    yield return instanceTestCase;
                }
            }
        }

        protected override IEnumerable<IXunitTestCase> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
        {
            foreach (var supportedInstance in IdeFactDiscoverer.GetSupportedInstances(testMethod, theoryAttribute))
            {
                yield return new IdeTheoryTestCase(DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedInstance);
                if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, DiagnosticMessageSink, supportedInstance) is { } instanceTestCase)
                {
                    yield return instanceTestCase;
                }
            }
        }
    }
}
