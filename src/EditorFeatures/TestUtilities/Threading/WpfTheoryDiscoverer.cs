// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Roslyn.Test.Utilities;

public sealed class WpfTheoryDiscoverer : TheoryDiscoverer
{
    protected override async ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForDataRow(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute,
        ITheoryDataRow dataRow,
        object?[] testMethodArguments,
        string? index)
    {
        var testCases = await base.CreateTestCasesForDataRow(
            discoveryOptions, testMethod, theoryAttribute, dataRow, testMethodArguments, index);
        var result = new IXunitTestCase[testCases.Count];

        var indexInResult = 0;
        foreach (var testCase in testCases)
        {
            result[indexInResult++] = testCase is XunitTestCase xunitTestCase
                ? new WpfTestCase(xunitTestCase)
                : testCase;
        }

        return result;
    }

    protected override async ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForTheory(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute)
    {
        var testCases = await base.CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute);
        var result = new IXunitTestCase[testCases.Count];

        var indexInResult = 0;
        foreach (var testCase in testCases)
        {
            result[indexInResult++] = testCase switch
            {
                XunitDelayEnumeratedTheoryTestCase delayEnumeratedTheoryTestCase => new WpfDelayEnumeratedTheoryTestCase(delayEnumeratedTheoryTestCase),
                XunitTestCase xunitTestCase => new WpfTestCase(xunitTestCase),
                _ => testCase,
            };
        }

        return result;
    }
}
