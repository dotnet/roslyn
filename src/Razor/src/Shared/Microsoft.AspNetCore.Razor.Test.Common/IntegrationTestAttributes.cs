// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Test.Common;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("Microsoft.AspNetCore.Razor.Test.Common." + nameof(IntegrationFactDiscoverer), "Microsoft.AspNetCore.Razor.Test.Common")]
internal class IntegrationTestFactAttribute : FactAttribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[XunitTestCaseDiscoverer("Microsoft.AspNetCore.Razor.Test.Common." + nameof(IntegrationTheoryDiscoverer), "Microsoft.AspNetCore.Razor.Test.Common")]
internal class IntegrationTestTheoryAttribute : TheoryAttribute { }

internal class IntegrationFactDiscoverer(IMessageSink diagnosticMessageSink)
    : FactDiscoverer(diagnosticMessageSink)
{
    public override IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
    {
        return [
            new IntegrationTestCase(designTime: true, DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod),
            new IntegrationTestCase(designTime: false, DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod)
        ];
    }
}

internal class IntegrationTheoryDiscoverer(IMessageSink diagnosticMessageSink)
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
        return [
            new IntegrationTestCase(designTime: true, DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, dataRow),
            new IntegrationTestCase(designTime: false, DiagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, dataRow)
        ];
    }
}

internal class IntegrationTestCase : XunitTestCase
{
    private bool _designTime;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public IntegrationTestCase() { }

    public IntegrationTestCase(bool designTime, IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod, object[] testMethodArguments = null!)
        : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
    {
        _designTime = designTime;
    }

    protected override string GetDisplayName(IAttributeInfo factAttribute, string displayName)
    {
        return base.GetDisplayName(factAttribute, displayName) + (_designTime ? " (Design Time)" : " (Run Time)");
    }

    public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
    {
        Debug.Assert(constructorArguments.Length == 1 && constructorArguments[0] is bool);
        constructorArguments[0] = _designTime;
        return base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);
    }

    public override void Deserialize(IXunitSerializationInfo data)
    {
        _designTime = data.GetValue<bool>(nameof(_designTime));
        base.Deserialize(data);
    }

    public override void Serialize(IXunitSerializationInfo data)
    {
        data.AddValue(nameof(_designTime), _designTime);
        base.Serialize(data);
    }

    protected override string GetUniqueID()
    {
        return base.GetUniqueID() + (_designTime ? "dt" : "rt");
    }
}
