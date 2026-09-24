// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal sealed class FormattingFactDiscoverer : FactDiscoverer
{
    public override ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, IFactAttribute factAttribute)
    {
        return CreateTestCases(discoveryOptions, testMethod, factAttribute);
    }

    public static ValueTask<IReadOnlyCollection<IXunitTestCase>> CreateTestCases(ITestFrameworkDiscoveryOptions discoveryOptions, IXunitTestMethod testMethod, IFactAttribute factAttribute, object?[]? dataRow = null)
    {
        return new ValueTask<IReadOnlyCollection<IXunitTestCase>>([
            CreateTestCase(shouldFlipLineEndings: false),
            CreateTestCase(shouldFlipLineEndings: true)]);

        FormattingTestCase CreateTestCase(bool shouldFlipLineEndings)
        {
            return new FormattingTestCase(shouldFlipLineEndings, testMethod, factAttribute, dataRow);
        }
    }
}
