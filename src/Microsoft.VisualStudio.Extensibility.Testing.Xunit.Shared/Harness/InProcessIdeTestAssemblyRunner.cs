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
                    return new IdeTestCase(diagnosticMessageSink, ideTestCase.DefaultMethodDisplay, ideTestCase.DefaultMethodDisplayOptions, ideTestCase.TestMethod, ideTestCase.VisualStudioVersion, ideTestCase.RootSuffix, ideTestCase.TestMethodArguments);
                }
                else if (testCase is IdeTheoryTestCase ideTheoryTestCase)
                {
                    return new IdeTheoryTestCase(diagnosticMessageSink, ideTheoryTestCase.DefaultMethodDisplay, ideTheoryTestCase.DefaultMethodDisplayOptions, ideTheoryTestCase.TestMethod, ideTheoryTestCase.VisualStudioVersion, ideTheoryTestCase.RootSuffix, ideTheoryTestCase.TestMethodArguments);
                }

                return testCase;
            });

            _testAssemblyRunner = new XunitTestAssemblyRunner(testAssembly, reconstructedTestCases.ToArray(), diagnosticMessageSink, executionMessageSink, executionOptions);
        }

        public Tuple<int, int, int, decimal> RunTestCollection(IMessageBus messageBus, ITestCollection testCollection, IXunitTestCase[] testCases)
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                var result = _testAssemblyRunner.RunAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                return Tuple.Create(result.Total, result.Failed, result.Skipped, result.Time);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // The life of this object is managed explicitly
        public override object? InitializeLifetimeService()
        {
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
