// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Roslyn.Test.Utilities
{
    public class WpfTheoryTestCase : XunitTheoryTestCase
    {
        public readonly IDictionary<string, TestInfo> _passedTests;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public WpfTheoryTestCase()
        {
            _passedTests = new Dictionary<string, TestInfo>();
        }

        public WpfTheoryTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions, ITestMethod testMethod, IDictionary<string, TestInfo> passedTests)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod)
        {
            _passedTests = passedTests;
        }

        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            var runner = new WpfTheoryTestCaseRunner(WpfTestSharedData.Instance, this, DisplayName, SkipReason, constructorArguments, diagnosticMessageSink, messageBus, aggregator, _passedTests, cancellationTokenSource);
            return runner.RunAsync();
        }
    }
}
