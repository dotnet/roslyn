// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias XunitV3;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.Test.Common;

public sealed class FormattingTheoryDiscoverer : TheoryDiscoverer
{
    public override async ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        IFactAttribute factAttribute)
    {
        // We have to force pre-enumeration of theories for this discoverer to work correctly. Normally it's true in VS,
        // but false in command line/CI. Since we're injecting "fake" data rows, we rely on it everywhere. Without this
        // set to true, the method below that we override doesn't get called.
        var preEnumerateTheories = discoveryOptions.GetValue<bool?>("xunit.discovery.PreEnumerateTheories");
        discoveryOptions.SetValue("xunit.discovery.PreEnumerateTheories", true);

        try
        {
            return await base.Discover(discoveryOptions, testMethod, factAttribute);
        }
        finally
        {
            discoveryOptions.SetValue("xunit.discovery.PreEnumerateTheories", preEnumerateTheories);
        }
    }

    protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForDataRow(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        ITheoryAttribute theoryAttribute,
        ITheoryDataRow dataRow,
        object?[] testMethodArguments,
        string? testCaseDisplayName)
    {
        var details = TestIntrospectionHelper.GetTestCaseDetailsForTheoryDataRow(
            discoveryOptions,
            testMethod,
            theoryAttribute,
            dataRow,
            testMethodArguments,
            testCaseDisplayName);
        var traits = TestIntrospectionHelper.GetTraits(testMethod, dataRow);

        return new(FormattingFactDiscoverer.CreateTestCases(details, traits, testMethodArguments));
    }
}
