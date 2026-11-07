// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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
        => [.. (await base.CreateTestCasesForDataRow(discoveryOptions, testMethod, theoryAttribute, dataRow, testMethodArguments, index)).Select(ToWpfTestCase)];

    protected override async ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForTheory(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute)
        => [.. (await base.CreateTestCasesForTheory(discoveryOptions, testMethod, theoryAttribute)).Select(ToWpfTestCase)];

    // The base discoverer only creates these two kinds of test cases here.
    private static IXunitTestCase ToWpfTestCase(IXunitTestCase testCase)
        => testCase switch
        {
            XunitDelayEnumeratedTheoryTestCase delayEnumeratedTheoryTestCase => new WpfDelayEnumeratedTheoryTestCase(delayEnumeratedTheoryTestCase),
            XunitTestCase xunitTestCase => new WpfTestCase(xunitTestCase),
            _ => throw ExceptionUtilities.UnexpectedValue(testCase),
        };
}
