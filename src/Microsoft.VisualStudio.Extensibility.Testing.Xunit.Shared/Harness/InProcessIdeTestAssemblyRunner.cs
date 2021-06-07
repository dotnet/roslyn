// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Xunit.Abstractions;
    using Xunit.Sdk;
    using Xunit.Threading;

    public class InProcessIdeTestAssemblyRunner : MarshalByRefObject, IDisposable
    {
        private readonly TestAssemblyRunner<IXunitTestCase> _testAssemblyRunner;

        public InProcessIdeTestAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            var reconstructedTestCases = testCases.Select(testCase =>
            {
                if (testCase is IdeTestCase ideTestCase)
                {
                    return new IdeTestCase(diagnosticMessageSink, ideTestCase.DefaultMethodDisplay, ideTestCase.TestMethod, ideTestCase.VisualStudioVersion, ideTestCase.TestMethodArguments);
                }

                return testCase;
            });

            _testAssemblyRunner = new XunitTestAssemblyRunner(testAssembly, reconstructedTestCases.ToArray(), diagnosticMessageSink, executionMessageSink, executionOptions);
        }

        public Tuple<int, int, int, decimal> RunTestCollection(IMessageBus messageBus, ITestCollection testCollection, IXunitTestCase[] testCases)
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var result = _testAssemblyRunner.RunAsync().GetAwaiter().GetResult();
                return Tuple.Create(result.Total, result.Failed, result.Skipped, result.Time);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override object InitializeLifetimeService()
        {
            // This object can live forever
            return null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _testAssemblyRunner.Dispose();
            }
        }
    }
}
