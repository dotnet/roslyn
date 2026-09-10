// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias XunitV3;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using XunitV3::Xunit.Sdk;
using XunitV3::Xunit.v3;

namespace Microsoft.AspNetCore.Razor.Test.Common;

public sealed class FormattingTestCase : XunitTestCase, ISelfExecutingXunitTestCase
{
    private bool _shouldFlipLineEndings;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public FormattingTestCase()
    {
    }

    public FormattingTestCase(
        bool shouldFlipLineEndings,
        IXunitTestMethod testMethod,
        string testCaseDisplayName,
        string uniqueId,
        bool @explicit,
        Type[]? skipExceptions = null,
        string? skipReason = null,
        Type? skipType = null,
        string? skipUnless = null,
        string? skipWhen = null,
        Dictionary<string, HashSet<string>>? traits = null,
        object?[]? testMethodArguments = null,
        string? sourceFilePath = null,
        int? sourceLineNumber = null,
        int? timeout = null)
        : base(testMethod, testCaseDisplayName, uniqueId, @explicit, skipExceptions, skipReason, skipType, skipUnless, skipWhen, traits, testMethodArguments, sourceFilePath, sourceLineNumber, timeout)
    {
        _shouldFlipLineEndings = shouldFlipLineEndings;
    }

    public async ValueTask<RunSummary> Run(
        ExplicitOption explicitOption,
        IMessageBus messageBus,
        object?[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource,
        ParallelMode parallelMode,
        ExecutionScheduler executionScheduler,
        FixtureMappingManager fixtureMappingManager)
    {
        var updatedConstructorArguments = CreateConstructorArguments(constructorArguments);
        var tests = await CreateTests().ConfigureAwait(false);

        return await XunitTestCaseRunner.Instance.Run(
            this,
            tests,
            messageBus,
            aggregator,
            cancellationTokenSource,
            parallelMode,
            executionScheduler,
            TestCaseDisplayName,
            SkipReason,
            explicitOption,
            updatedConstructorArguments,
            fixtureMappingManager).ConfigureAwait(false);
    }

    protected override void Deserialize(IXunitSerializationInfo data)
    {
        _shouldFlipLineEndings = (bool)(data.GetValue(nameof(_shouldFlipLineEndings)) ?? false);
        base.Deserialize(data);
    }

    protected override void Serialize(IXunitSerializationInfo data)
    {
        data.AddValue(nameof(_shouldFlipLineEndings), _shouldFlipLineEndings, typeof(bool));
        base.Serialize(data);
    }

    private object?[] CreateConstructorArguments(object?[] constructorArguments)
    {
        var updatedConstructorArguments = (object?[])constructorArguments.Clone();
        var replacement = new FormattingTestContext
        {
            ShouldFlipLineEndings = _shouldFlipLineEndings,
            CreatedByFormattingDiscoverer = true
        };

        var replaced = false;
        for (var i = 0; i < updatedConstructorArguments.Length; i++)
        {
            if (updatedConstructorArguments[i] is FormattingTestContext)
            {
                updatedConstructorArguments[i] = replacement;
                replaced = true;
            }
        }

        if (replaced)
        {
            return updatedConstructorArguments;
        }

        if (updatedConstructorArguments.Length == 0)
        {
            return [replacement];
        }

        throw new InvalidOperationException($"{TestMethod.TestClass.Class.Name}.{TestMethod.Method.Name} uses a formatting test attribute without an injectable {nameof(FormattingTestContext)} fixture argument.");
    }
}
