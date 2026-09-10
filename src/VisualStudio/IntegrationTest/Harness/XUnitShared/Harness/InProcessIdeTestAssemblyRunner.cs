// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Threading;
    using Xunit.InProcess;
    using Xunit.Runner.Common;
    using Xunit.Sdk;
    using Xunit.v3;

    [Serializable]
    internal sealed class TestExecutionRequest
    {
        public TestExecutionRequest(
            IReadOnlyCollection<IXunitTestCase> testCases,
            ITestFrameworkExecutionOptions executionOptions)
        {
            SerializedTestCases = testCases.Select(testCase => SerializationHelper.Instance.Serialize(testCase)).ToArray();
            SerializedExecutionOptions = executionOptions.ToJson();
        }

        public string[] SerializedTestCases { get; }

        public string SerializedExecutionOptions { get; }
    }

    [Serializable]
    internal sealed class TestExecutionResult
    {
        public TestExecutionResult(RunSummary summary)
        {
            Total = summary.Total;
            Failed = summary.Failed;
            Skipped = summary.Skipped;
            NotRun = summary.NotRun;
            Time = summary.Time;
        }

        public int Total { get; }

        public int Failed { get; }

        public int Skipped { get; }

        public int NotRun { get; }

        public decimal Time { get; }
    }

    internal abstract class TestExecutionMessageSink : MarshalByRefObject
    {
        public abstract bool OnMessage(string message);

        public override object? InitializeLifetimeService()
            => null;
    }

    internal static class InProcessIdeTestAssemblyRunner
    {
        public static async Task<TestExecutionResult> RunAsync(
            TestExecutionRequest request,
            TestExecutionMessageSink messageSink)
        {
            var dispatcher = Application.Current?.Dispatcher
                ?? throw new InvalidOperationException("The Visual Studio WPF dispatcher is unavailable.");

            if (!dispatcher.CheckAccess())
            {
                throw new InvalidOperationException("Integration tests must be invoked on the Visual Studio WPF dispatcher.");
            }

            var testCases = request.SerializedTestCases
                .Select(serializedTestCase => SerializationHelper.Instance.Deserialize<IXunitTestCase>(serializedTestCase)!)
                .ToArray();

            if (testCases.Length == 0)
            {
                return new TestExecutionResult(new RunSummary());
            }

            var executionOptions = TestFrameworkOptions.ForExecutionFromSerialization(request.SerializedExecutionOptions);
            var executionMessageSink = new InProcessMessageSink(messageSink);
            var synchronizationContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher, DispatcherPriority.Background));

            try
            {
                DataCollectionService.InstallFirstChanceExceptionHandler();
                var summary = await XunitTestAssemblyRunner.Instance.Run(
                    testCases[0].TestCollection.TestAssembly,
                    testCases,
                    executionMessageSink,
                    executionOptions,
                    CancellationToken.None);
                return new TestExecutionResult(summary);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            }
        }

        private sealed class InProcessMessageSink : IMessageSink
        {
            private readonly TestExecutionMessageSink _messageSink;

            public InProcessMessageSink(TestExecutionMessageSink messageSink)
            {
                _messageSink = messageSink;
            }

            public bool OnMessage(IMessageSinkMessage message)
            {
                if (message is ITestStarting testStarting)
                {
                    DataCollectionService.CurrentTestName = testStarting.TestDisplayName;
                }

                try
                {
                    return _messageSink.OnMessage(message.ToJson() ?? throw new InvalidOperationException("xUnit could not serialize an execution message."));
                }
                finally
                {
                    if (message is ITestFinished)
                    {
                        DataCollectionService.CurrentTestName = null;
                    }
                }
            }
        }
    }
}
