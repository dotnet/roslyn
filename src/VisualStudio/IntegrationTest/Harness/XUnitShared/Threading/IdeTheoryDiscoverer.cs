// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit.Sdk;
    using Xunit.v3;

    public class IdeTheoryDiscoverer : TheoryDiscoverer
    {
        protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, ITheoryAttribute theoryAttribute, ITheoryDataRow dataRow, object?[] testMethodArguments, string? index)
        {
            var details = TestIntrospectionHelper.GetTestCaseDetailsForTheoryDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow, testMethodArguments, index);
            var testCases = new List<IXunitTestCase>();
            foreach (var supportedInstance in IdeFactDiscoverer.GetSupportedInstances(testMethod, theoryAttribute))
            {
                var traits = TestIntrospectionHelper.GetTraits(testMethod, dataRow);
                testCases.Add(details.SkipReason is not null && details.SkipUnless is null && details.SkipWhen is null
                    ? new IdeSkippedDataRowTestCase(
                        details.ResolvedTestMethod,
                        details.TestCaseDisplayName,
                        details.UniqueID,
                        details.Explicit,
                        details.SkipExceptions,
                        details.SkipReason,
                        details.SkipType,
                        details.SkipUnless,
                        details.SkipWhen,
                        traits,
                        testMethodArguments,
                        details.SourceFilePath,
                        details.SourceLineNumber,
                        details.Timeout,
                        supportedInstance,
                        dataRow)
                    : new IdeTestCase(
                        details.ResolvedTestMethod,
                        details.TestCaseDisplayName,
                        details.UniqueID,
                        details.Explicit,
                        details.SkipExceptions,
                        details.SkipReason,
                        details.SkipType,
                        details.SkipUnless,
                        details.SkipWhen,
                        traits,
                        testMethodArguments,
                        details.SourceFilePath,
                        details.SourceLineNumber,
                        details.Timeout,
                        supportedInstance,
                        dataRow.Label,
                        dataRow.DisableParallelization ?? false));

                if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, supportedInstance) is { } instanceTestCase)
                    testCases.Add(instanceTestCase);
            }

            return new(testCases);
        }

        protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, ITheoryAttribute theoryAttribute)
        {
            var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, theoryAttribute);
            var testCases = new List<IXunitTestCase>();
            foreach (var supportedInstance in IdeFactDiscoverer.GetSupportedInstances(testMethod, theoryAttribute))
            {
                testCases.Add(new IdeTheoryTestCase(
                    details.ResolvedTestMethod,
                    details.TestCaseDisplayName,
                    details.UniqueID,
                    details.Explicit,
                    details.SkipExceptions,
                    details.SkipReason,
                    details.SkipType,
                    details.SkipUnless,
                    details.SkipWhen,
                    TestIntrospectionHelper.GetTraits(testMethod, dataRow: null),
                    theoryAttribute.SkipTestWithoutData,
                    details.SourceFilePath,
                    details.SourceLineNumber,
                    details.Timeout,
                    supportedInstance));
                if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, supportedInstance) is { } instanceTestCase)
                    testCases.Add(instanceTestCase);
            }

            return new(testCases);
        }
    }
}
