// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal sealed class FormattingTheoryDiscoverer(IMessageSink diagnosticMessageSink)
    : TheoryDiscoverer(diagnosticMessageSink)
{
    public override IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
    {
        // We have to force pre-enumeration of theories for this discoverer to work correctly. Normally its true in VS,
        // but false in command line/CI. Since we're injecting "fake" data rows, we rely on it everywhere. Without this
        // set to true, the method below that we override doesn't get called.
        discoveryOptions.SetValue("xunit.discovery.PreEnumerateTheories", true);
        return base.Discover(discoveryOptions, testMethod, theoryAttribute);
    }

    protected override IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute, object[] dataRow)
    {
        return FormattingFactDiscoverer.CreateTestCases(discoveryOptions, testMethod, DiagnosticMessageSink, dataRow);
    }
}
