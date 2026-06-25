// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.v3;

namespace Roslyn.Test.Utilities;

public class WpfFactDiscoverer : FactDiscoverer
{
    // Inject the WPF-aware runner during discovery so tests run on the STA thread.
    static WpfFactDiscoverer()
        => WpfTestCaseRunner.InjectIfNeeded();

    protected override IXunitTestCase CreateTestCase(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        IFactAttribute factAttribute)
    {
        var details = TestIntrospectionHelper.GetTestCaseDetails(
            discoveryOptions, testMethod, factAttribute,
            testMethodArguments: null, timeout: null, baseDisplayName: null);
        var traits = TestIntrospectionHelper.GetTraits(testMethod, dataRow: null);
        return new WpfTestCase(
            testMethod,
            details.Item1,       // testCaseDisplayName
            details.Rest.Item4,  // uniqueID
            details.Item2,       // @explicit
            details.Item3,       // skipExceptions
            details.Item4,       // skipReason
            details.Item5,       // skipType
            details.Item6,       // skipUnless
            details.Item7,       // skipWhen
            traits);
    }
}

public class WpfTheoryDiscoverer : TheoryDiscoverer
{
    static WpfTheoryDiscoverer()
        => WpfTestCaseRunner.InjectIfNeeded();

    protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForDataRow(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute,
        ITheoryDataRow dataRow,
        object[] testMethodArguments)
    {
        var details = TestIntrospectionHelper.GetTestCaseDetailsForTheoryDataRow(
            discoveryOptions, testMethod, theoryAttribute, dataRow, testMethodArguments);
        var traits = TestIntrospectionHelper.GetTraits(testMethod, dataRow);
        return new ValueTask<IReadOnlyCollection<IXunitTestCase>>(
        [
            new WpfTestCase(
                testMethod,
                details.Item1,       // testCaseDisplayName
                details.Rest.Item4,  // uniqueID
                details.Item2,       // @explicit
                details.Item3,       // skipExceptions
                details.Item4,       // skipReason
                details.Item5,       // skipType
                details.Item6,       // skipUnless
                details.Item7,       // skipWhen
                traits,
                testMethodArguments)
        ]);
    }

    protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForTheory(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute)
    {
        var details = TestIntrospectionHelper.GetTestCaseDetails(
            discoveryOptions, testMethod, theoryAttribute,
            testMethodArguments: null, timeout: null, baseDisplayName: null);
        var traits = TestIntrospectionHelper.GetTraits(testMethod, dataRow: null);
        return new ValueTask<IReadOnlyCollection<IXunitTestCase>>(
        [
            new WpfTheoryTestCase(
                testMethod,
                details.Item1,       // testCaseDisplayName
                details.Rest.Item4,  // uniqueID
                details.Item2,       // @explicit
                details.Item3,       // skipExceptions
                details.Item4,       // skipReason
                details.Item5,       // skipType
                details.Item6,       // skipUnless
                details.Item7,       // skipWhen
                traits)
        ]);
    }
}
