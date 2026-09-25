// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Runner.Common;
    using Xunit.Sdk;
    using Xunit.Threading;
    using Xunit.v3;

    public class InProcessIdeTestAssemblyRunner : LongLivedMarshalByRefObject
    {
        public InProcessIdeTestAssemblyRunner()
        {
        }

        public Tuple<int, int, int, decimal> RunTestCollection(string testAssembly, HashSet<string> testCaseUniqueIds, DeserializingMessageSink executionMessageSink, ITestFrameworkDiscoveryOptions discoveryOptions, ITestFrameworkExecutionOptions executionOptions)
        {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            var result = RunTestCollectionAsync(testAssembly, testCaseUniqueIds, executionMessageSink, discoveryOptions, executionOptions).AsTask().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            return Tuple.Create(result.Total, result.Failed, result.Skipped, result.Time);
        }

        private static async ValueTask<RunSummary> RunTestCollectionAsync(string testAssemblyPath, HashSet<string> testCaseUniqueIds, DeserializingMessageSink executionMessageSink, ITestFrameworkDiscoveryOptions discoveryOptions, ITestFrameworkExecutionOptions executionOptions)
        {
            var assembly = Assembly.LoadFrom(testAssemblyPath);
            var testAssembly = new XunitTestAssembly(assembly, configFilePath: null);
            var discoverer = new XunitTestFrameworkDiscoverer(testAssembly);

            var discoveredTestCases = new List<IXunitTestCase>();
            await discoverer.Find(
                testCase =>
                {
                    if (testCase is IXunitTestCase xunitTestCase && testCaseUniqueIds.Contains(xunitTestCase.UniqueID))
                    {
                        discoveredTestCases.Add(xunitTestCase);
                    }

                    return new ValueTask<bool>(true);
                },
                discoveryOptions).ConfigureAwait(false);

            using var cancellationTokenSource = new CancellationTokenSource();
            return await XunitTestAssemblyRunner.Instance.Run(testAssembly, discoveredTestCases, new SerializingMessageSink(executionMessageSink), executionOptions, cancellationTokenSource.Token).ConfigureAwait(false);
        }

        private sealed class SerializingMessageSink : IMessageSink
        {
            private readonly DeserializingMessageSink _messageSink;

            public SerializingMessageSink(DeserializingMessageSink messageSink)
            {
                _messageSink = messageSink;
            }

            public bool OnMessage(IMessageSinkMessage message)
            {
                var serializableMessage = (Xunit.v3.MessageSinkMessage)message;
                return _messageSink.OnMessage(serializableMessage.ToJson());
            }
        }
    }
}
