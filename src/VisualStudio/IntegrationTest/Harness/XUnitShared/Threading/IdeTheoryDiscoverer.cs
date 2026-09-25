// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit.Sdk;
    using Xunit.v3;

    // NOTE: [IdeTheory] currently uses the stock v3 TheoryDiscoverer rather than this class (see IdeTheoryAttribute).
    // IdeTheory VS-instance fan-out support is deferred; this type is kept buildable but is not wired up to any
    // attribute in this initial xUnit v3 port.
    public class IdeTheoryDiscoverer : TheoryDiscoverer
    {
        /*
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
        */

        protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, ITheoryAttribute theoryAttribute, ITheoryDataRow dataRow, object?[] testMethodArguments, string? index)
        {
            var testCases = new List<IXunitTestCase>();
            foreach (var supportedInstance in IdeFactDiscoverer.GetSupportedInstances(testMethod, theoryAttribute))
            {
                testCases.Add(new IdeTestCase(testMethod, supportedInstance, testMethodArguments));
            }

            return new ValueTask<IReadOnlyCollection<IXunitTestCase>>(testCases);
        }

        protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, ITheoryAttribute theoryAttribute)
        {
            var testCases = new List<IXunitTestCase>();
            foreach (var supportedInstance in IdeFactDiscoverer.GetSupportedInstances(testMethod, theoryAttribute))
            {
                testCases.Add(new IdeTheoryTestCase(testMethod, supportedInstance));
            }

            return new ValueTask<IReadOnlyCollection<IXunitTestCase>>(testCases);
        }
    }
}
