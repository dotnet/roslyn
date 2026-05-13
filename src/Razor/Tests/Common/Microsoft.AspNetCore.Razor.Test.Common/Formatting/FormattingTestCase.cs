// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal sealed class FormattingTestCase : XunitTestCase
{
    private bool _shouldFlipLineEndings;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public FormattingTestCase() { }

    public FormattingTestCase(bool shouldFlipLineEndings, IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod, object[]? testMethodArguments = null)
        : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
    {
        _shouldFlipLineEndings = shouldFlipLineEndings;
    }

    protected override string GetDisplayName(IAttributeInfo factAttribute, string displayName)
    {
        return base.GetDisplayName(factAttribute, displayName) +
            (_shouldFlipLineEndings ? " (LF)" : " (CRLF)");
    }

    public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
    {
        Debug.Assert(constructorArguments.Length >= 1 && constructorArguments[0] is FormattingTestContext, $"{TestMethod.TestClass.Class.Name}.{TestMethod.Method.Name} uses a formatting test attribute in a class without a FormattingTestContext parameter?");
        constructorArguments[0] = new FormattingTestContext
        {
            ShouldFlipLineEndings = _shouldFlipLineEndings,
            CreatedByFormattingDiscoverer = true
        };
        return base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);
    }

    public override void Deserialize(IXunitSerializationInfo data)
    {
        _shouldFlipLineEndings = data.GetValue<bool>(nameof(_shouldFlipLineEndings));
        base.Deserialize(data);
    }

    public override void Serialize(IXunitSerializationInfo data)
    {
        data.AddValue(nameof(_shouldFlipLineEndings), _shouldFlipLineEndings);
        base.Serialize(data);
    }

    protected override string GetUniqueID()
    {
        return base.GetUniqueID() +
            (_shouldFlipLineEndings ? "lf" : "crlf");
    }
}
