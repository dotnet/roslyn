// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal sealed class FormattingFactDiscoverer(IMessageSink diagnosticMessageSink)
    : FactDiscoverer(diagnosticMessageSink)
{
    public override IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        return CreateTestCases(discoveryOptions, testMethod, DiagnosticMessageSink);
    }

    public static IEnumerable<IXunitTestCase> CreateTestCases(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IMessageSink messageSink, object[]? dataRow = null)
    {
        yield return CreateTestCase(shouldFlipLineEndings: false);
        yield return CreateTestCase(shouldFlipLineEndings: true);

        FormattingTestCase CreateTestCase(bool shouldFlipLineEndings)
        {
            return new FormattingTestCase(shouldFlipLineEndings, messageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, dataRow);
        }
    }
}
