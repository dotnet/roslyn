// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit.Sdk;
using Xunit.v3;

namespace Roslyn.Test.Utilities;

public sealed class WpfFactDiscoverer : FactDiscoverer
{
    protected override IXunitTestCase CreateTestCase(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        IFactAttribute factAttribute)
        => new WpfTestCase((XunitTestCase)base.CreateTestCase(discoveryOptions, testMethod, factAttribute));
}
