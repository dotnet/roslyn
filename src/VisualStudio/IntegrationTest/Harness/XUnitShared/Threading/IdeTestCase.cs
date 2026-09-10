// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Harness;
    using Xunit.Sdk;
    using Xunit.v3;

    public sealed class IdeTestCase : IdeTestCaseBase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes", error: true)]
        public IdeTestCase()
        {
        }

        public IdeTestCase(
            IXunitTestMethod testMethod,
            string testCaseDisplayName,
            string uniqueID,
            bool @explicit,
            Type[]? skipExceptions,
            string? skipReason,
            Type? skipType,
            string? skipUnless,
            string? skipWhen,
            Dictionary<string, HashSet<string>> traits,
            object?[]? testMethodArguments,
            string? sourceFilePath,
            int? sourceLineNumber,
            int? timeout,
            VisualStudioInstanceKey visualStudioInstanceKey,
            string? testLabel = null,
            bool disableParallelization = false)
            : base(testMethod, testCaseDisplayName, uniqueID, @explicit, skipExceptions, skipReason, skipType, skipUnless, skipWhen, traits, testMethodArguments, sourceFilePath, sourceLineNumber, timeout, visualStudioInstanceKey, testLabel, disableParallelization)
        {
        }

        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            TestCaseRunner<IXunitTestCase> runner;
            if (!string.IsNullOrEmpty(SkipReason))
            {
                // Use XunitTestCaseRunner so the skip gets reported without trying to open VS
                runner = new XunitTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, TestMethodArguments, messageBus, aggregator, cancellationTokenSource);
            }
            else
            {
                runner = new IdeTestCaseRunner(SharedData, VisualStudioInstanceKey, this, DisplayName, SkipReason, constructorArguments, TestMethodArguments, messageBus, aggregator, cancellationTokenSource);
            }

            return runner.RunAsync();
        }
    }
}
