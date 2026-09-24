// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal sealed class FormattingTestCase : XunitTestCase, ISelfExecutingXunitTestCase
{
    private bool _shouldFlipLineEndings;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public FormattingTestCase() { }

    public FormattingTestCase(bool shouldFlipLineEndings, IXunitTestMethod testMethod, IFactAttribute factAttribute, object?[]? testMethodArguments = null)
        : base(testMethod, GetDisplayName(testMethod, factAttribute, shouldFlipLineEndings, testMethodArguments), GetUniqueID(testMethod, shouldFlipLineEndings), factAttribute.Explicit, factAttribute.SkipExceptions, factAttribute.Skip, factAttribute.SkipType, factAttribute.SkipUnless, factAttribute.SkipWhen, traits: null, testMethodArguments, factAttribute.SourceFilePath, factAttribute.SourceLineNumber, factAttribute.Timeout)
    {
        _shouldFlipLineEndings = shouldFlipLineEndings;
    }

    private static string GetDisplayName(IXunitTestMethod testMethod, IFactAttribute factAttribute, bool shouldFlipLineEndings, object?[]? testMethodArguments)
    {
        return testMethod.GetDisplayName(factAttribute.DisplayName, shouldFlipLineEndings ? "LF" : "CRLF", testMethodArguments, methodGenericTypes: null);
    }

    private static string GetUniqueID(IXunitTestMethod testMethod, bool shouldFlipLineEndings)
    {
        return testMethod.UniqueID + (shouldFlipLineEndings ? "_lf" : "_crlf");
    }

    public async ValueTask<RunSummary> Run(ExplicitOption explicitOption, IMessageBus messageBus, object?[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
    {
        Debug.Assert(constructorArguments.Length >= 1 && constructorArguments[0] is FormattingTestContext, $"{TestClassName}.{TestMethodName} uses a formatting test attribute in a class without a FormattingTestContext parameter?");
        constructorArguments[0] = new FormattingTestContext
        {
            ShouldFlipLineEndings = _shouldFlipLineEndings,
            CreatedByFormattingDiscoverer = true
        };
        return await XunitTestCaseRunner.Instance.Run(this, await CreateTests(), messageBus, aggregator, cancellationTokenSource, TestCaseDisplayName, SkipReason, explicitOption, constructorArguments);
    }

    protected override void Deserialize(IXunitSerializationInfo data)
    {
        _shouldFlipLineEndings = data.GetValue<bool>(nameof(_shouldFlipLineEndings));
        base.Deserialize(data);
    }

    protected override void Serialize(IXunitSerializationInfo data)
    {
        data.AddValue(nameof(_shouldFlipLineEndings), _shouldFlipLineEndings);
        base.Serialize(data);
    }
}
