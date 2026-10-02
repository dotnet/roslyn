// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Sdk;
using Xunit.v3;

namespace Roslyn.Test.Utilities;

public sealed class WpfTestCase : XunitTestCase, ISelfExecutingXunitTestCase
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public WpfTestCase()
    {
    }

    public WpfTestCase(XunitTestCase testCase)
        : base(
            testCase.TestMethod,
            testCase.TestCaseDisplayName,
            testCase.UniqueID,
            testCase.Explicit,
            testCase.TestLabel,
            testCase.DisableParallelization,
            testCase.SkipExceptions,
            testCase.SkipReason,
            testCase.SkipType,
            testCase.SkipUnless,
            testCase.SkipWhen,
            testCase.Traits,
            testCase.TestMethodArguments,
            testCase.SourceFilePath,
            testCase.SourceLineNumber,
            testCase.Timeout)
    {
    }

    public ValueTask<RunSummary> Run(
        ExplicitOption explicitOption,
        IMessageBus messageBus,
        object?[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource,
        ParallelMode parallelMode,
        ExecutionScheduler scheduler,
        FixtureMappingManager methodFixtureMappings)
        => WpfTestCaseRunner.Run(
            this,
            explicitOption,
            messageBus,
            constructorArguments,
            aggregator,
            cancellationTokenSource,
            parallelMode,
            scheduler,
            methodFixtureMappings);
}

public sealed class WpfDelayEnumeratedTheoryTestCase : XunitDelayEnumeratedTheoryTestCase, ISelfExecutingXunitTestCase
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
    public WpfDelayEnumeratedTheoryTestCase()
    {
    }

    public WpfDelayEnumeratedTheoryTestCase(XunitDelayEnumeratedTheoryTestCase testCase)
        : base(
            testCase.TestMethod,
            testCase.TestCaseDisplayName,
            testCase.UniqueID,
            testCase.Explicit,
            testCase.SkipTestWithoutData,
            testCase.SkipExceptions,
            testCase.SkipReason,
            testCase.SkipType,
            testCase.SkipUnless,
            testCase.SkipWhen,
            testCase.Traits,
            testCase.SourceFilePath,
            testCase.SourceLineNumber,
            testCase.Timeout)
    {
    }

    public ValueTask<RunSummary> Run(
        ExplicitOption explicitOption,
        IMessageBus messageBus,
        object?[] constructorArguments,
        ExceptionAggregator aggregator,
        CancellationTokenSource cancellationTokenSource,
        ParallelMode parallelMode,
        ExecutionScheduler scheduler,
        FixtureMappingManager methodFixtureMappings)
        => WpfTestCaseRunner.Run(
            this,
            explicitOption,
            messageBus,
            constructorArguments,
            aggregator,
            cancellationTokenSource,
            parallelMode,
            scheduler,
            methodFixtureMappings);
}
