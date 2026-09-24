// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal sealed class FormattingTheoryDiscoverer : TheoryDiscoverer
{
    public override ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, IFactAttribute factAttribute)
    {
        // We have to force pre-enumeration of theories for this discoverer to work correctly. Normally its true in VS,
        // but false in command line/CI. Since we're injecting "fake" data rows, we rely on it everywhere. Without this
        // set to true, the method below that we override doesn't get called.
        discoveryOptions.SetValue("xunit.discovery.PreEnumerateTheories", true);
        return base.Discover(discoveryOptions, testMethod, factAttribute);
    }

    protected override ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, ITheoryAttribute theoryAttribute, ITheoryDataRow dataRow, object?[] testMethodArguments)
    {
        return FormattingFactDiscoverer.CreateTestCases(discoveryOptions, testMethod, theoryAttribute, testMethodArguments);
    }
}
