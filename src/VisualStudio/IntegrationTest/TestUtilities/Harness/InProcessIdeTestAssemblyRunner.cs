// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Harness
{
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

                return new IdeTestCase(diagnosticMessageSink, TestMethodDisplay.ClassAndMethod, testCase.TestMethod, VisualStudioVersion.VS2017, testCase.TestMethodArguments);
                //throw new InvalidOperationException($"{testCase.GetType().AssemblyQualifiedName} is not a supported test case type. Expected {typeof(IdeTestCase).AssemblyQualifiedName}.");
            });

            _testAssemblyRunner = new XunitTestAssemblyRunner(testAssembly, reconstructedTestCases.ToArray(), diagnosticMessageSink, executionMessageSink, executionOptions);
        }

        public (int total, int failed, int skipped, decimal time) RunTestCollection(IMessageBus messageBus, ITestCollection testCollection, IXunitTestCase[] testCases)
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var result = _testAssemblyRunner.RunAsync().GetAwaiter().GetResult();
                return (result.Total, result.Failed, result.Skipped, result.Time);
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

        private class DeserializationExecutor : TestFrameworkExecutor<IXunitTestCase>
        {
            public DeserializationExecutor(AssemblyName assemblyName, IMessageSink diagnosticMessageSink)
                : base(assemblyName, new EmptySourceInformationProvider(), diagnosticMessageSink)
            {
            }

            protected override ITestFrameworkDiscoverer CreateDiscoverer() => throw new NotSupportedException();

            protected override void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions) => throw new NotSupportedException();
        }

        private sealed class EmptySourceInformationProvider : ISourceInformationProvider
        {
            void IDisposable.Dispose()
            {
            }

            ISourceInformation ISourceInformationProvider.GetSourceInformation(ITestCase testCase) => new SourceInformation();
        }
    }
}
