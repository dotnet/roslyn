// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Harness
{
    internal class IdeTestAssemblyRunner : XunitTestAssemblyRunner
    {
        public IdeTestAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
            : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
        {
        }

        protected override async Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        {
            var result = new RunSummary();
            var testAssemblyFinishedMessages = new List<ITestAssemblyFinished>();
            var completedTestCaseIds = new HashSet<string>();
            try
            {
                ExecutionMessageSink.OnMessage(new TestCollectionStarting(testCases, testCollection));

                foreach (var testCasesByTargetVersion in testCases.GroupBy(GetVisualStudioVersionForTestCase))
                {
                    using (var visualStudioInstanceFactory = Activator.CreateInstance<VisualStudioInstanceFactory>())
                    {
                        var summary = await RunTestCollectionForVersionAsync(visualStudioInstanceFactory, testCasesByTargetVersion.Key.visualStudioVersion, completedTestCaseIds, messageBus, testCollection, testCasesByTargetVersion, cancellationTokenSource);
                        result.Aggregate(summary.Item1);
                        testAssemblyFinishedMessages.Add(summary.Item2);
                    }
                }
            }
            catch (Exception ex)
            {
                ReportHarnessFailure(testCases, completedTestCaseIds, ex);
                throw;
            }
            finally
            {
                var totalExecutionTime = testAssemblyFinishedMessages.Sum(message => message.ExecutionTime);
                var testsRun = testAssemblyFinishedMessages.Sum(message => message.TestsRun);
                var testsFailed = testAssemblyFinishedMessages.Sum(message => message.TestsFailed);
                var testsSkipped = testAssemblyFinishedMessages.Sum(message => message.TestsSkipped);
                ExecutionMessageSink.OnMessage(new TestCollectionFinished(testCases, testCollection, totalExecutionTime, testsRun, testsFailed, testsSkipped));
            }

            return result;
        }

        private void ReportHarnessFailure(IEnumerable<IXunitTestCase> testCases, HashSet<string> completedTestCaseIds, Exception ex)
        {
            var completedTestCases = testCases.Where(testCase => completedTestCaseIds.Contains(testCase.UniqueID));
            var remainingTestCases = testCases.Except(completedTestCases);
            foreach (var casesByTestClass in remainingTestCases.GroupBy(testCase => testCase.TestMethod.TestClass))
            {
                ExecutionMessageSink.OnMessage(new TestClassStarting(casesByTestClass.ToArray(), casesByTestClass.Key));

                foreach (var casesByTestMethod in casesByTestClass.GroupBy(testCase => testCase.TestMethod))
                {
                    ExecutionMessageSink.OnMessage(new TestMethodStarting(casesByTestMethod.ToArray(), casesByTestMethod.Key));

                    foreach (var testCase in casesByTestMethod)
                    {
                        ExecutionMessageSink.OnMessage(new TestCaseStarting(testCase));

                        var test = new XunitTest(testCase, testCase.DisplayName);
                        ExecutionMessageSink.OnMessage(new TestStarting(test));

                        if (!string.IsNullOrEmpty(testCase.SkipReason))
                        {
                            ExecutionMessageSink.OnMessage(new TestSkipped(test, testCase.SkipReason));
                        }
                        else
                        {
                            ExecutionMessageSink.OnMessage(new TestFailed(test, 0, null, new InvalidOperationException("Test did not run due to a harness failure.", ex)));
                        }

                        ExecutionMessageSink.OnMessage(new TestFinished(test, 0, null));

                        ExecutionMessageSink.OnMessage(new TestCaseFinished(testCase, 0, 1, 1, 0));
                    }

                    ExecutionMessageSink.OnMessage(new TestMethodFinished(casesByTestMethod.ToArray(), casesByTestMethod.Key, 0, casesByTestMethod.Count(), casesByTestMethod.Count(), 0));
                }

                ExecutionMessageSink.OnMessage(new TestClassFinished(casesByTestClass.ToArray(), casesByTestClass.Key, 0, casesByTestClass.Count(), casesByTestClass.Count(), 0));
            }
        }

        protected virtual Task<Tuple<RunSummary, ITestAssemblyFinished>> RunTestCollectionForVersionAsync(VisualStudioInstanceFactory visualStudioInstanceFactory, VisualStudioVersion visualStudioVersion, HashSet<string> completedTestCaseIds, IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        {
            if (visualStudioVersion == VisualStudioVersion.Unspecified)
            {
                return RunTestCollectionForUnspecifiedVersionAsync(completedTestCaseIds, messageBus, testCollection, testCases, cancellationTokenSource);
            }

            DispatcherSynchronizationContext synchronizationContext = null;
            Dispatcher dispatcher = null;
            Thread staThread;
            using (var staThreadStartedEvent = new ManualResetEventSlim(initialState: false))
            {
                staThread = new Thread((ThreadStart)(() =>
                {
                    // All WPF Tests need a DispatcherSynchronizationContext and we don't want to block pending keyboard
                    // or mouse input from the user. So use background priority which is a single level below user input.
                    synchronizationContext = new DispatcherSynchronizationContext();
                    dispatcher = Dispatcher.CurrentDispatcher;

                    // xUnit creates its own synchronization context and wraps any existing context so that messages are
                    // still pumped as necessary. So we are safe setting it here, where we are not safe setting it in test.
                    SynchronizationContext.SetSynchronizationContext(synchronizationContext);

                    staThreadStartedEvent.Set();

                    Dispatcher.Run();
                }));

                staThread.Name = $"{nameof(WpfTestRunner)}";
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();

                staThreadStartedEvent.Wait();
                Debug.Assert(synchronizationContext != null, "Assertion failed: synchronizationContext != null");
            }

            var taskScheduler = new SynchronizationContextTaskScheduler(synchronizationContext);
            var task = Task.Factory.StartNew(
                async () =>
                {
                    Debug.Assert(SynchronizationContext.Current is DispatcherSynchronizationContext, "Assertion failed: SynchronizationContext.Current is DispatcherSynchronizationContext");

                    using (await WpfTestSharedData.Instance.TestSerializationGate.DisposableWaitAsync(CancellationToken.None))
                    {
                        // Just call back into the normal xUnit dispatch process now that we are on an STA Thread with no synchronization context.
                        return await InvokeTestCollectionOnMainThreadAsync(visualStudioInstanceFactory, visualStudioVersion, completedTestCaseIds, messageBus, testCollection, testCases, cancellationTokenSource.Token);
                    }
                },
                cancellationTokenSource.Token,
                TaskCreationOptions.None,
                taskScheduler).Unwrap();

            return Task.Run(
                async () =>
                {
                    try
                    {
                        return await task.ConfigureAwait(false);
                    }
                    finally
                    {
                        // Make sure to shut down the dispatcher. Certain framework types listed for the dispatcher
                        // shutdown to perform cleanup actions. In the absence of an explicit shutdown, these actions
                        // are delayed and run during AppDomain or process shutdown, where they can lead to crashes of
                        // the test process.
                        dispatcher.InvokeShutdown();

                        // Join the STA thread, which ensures shutdown is complete.
                        staThread.Join(Helper.HangMitigatingTimeout);
                    }
                });
        }

        private async Task<Tuple<RunSummary, ITestAssemblyFinished>> RunTestCollectionForUnspecifiedVersionAsync(HashSet<string> completedTestCaseIds, IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        {
            // These tests just run in the current process, but we still need to hook the assembly and collection events
            // to work correctly in mixed-testing scenarios.
            var executionMessageSinkFilter = new IpcMessageSink(ExecutionMessageSink, completedTestCaseIds, cancellationTokenSource.Token);
            using (var runner = new XunitTestAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSinkFilter, ExecutionOptions))
            {
                var runSummary = await runner.RunAsync();
                return Tuple.Create(runSummary, executionMessageSinkFilter.TestAssemblyFinished);
            }
        }

        private async Task<Tuple<RunSummary, ITestAssemblyFinished>> InvokeTestCollectionOnMainThreadAsync(VisualStudioInstanceFactory visualStudioInstanceFactory, VisualStudioVersion visualStudioVersion, HashSet<string> completedTestCaseIds, IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationToken cancellationToken)
        {
            Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());

            // Install a COM message filter to handle retry operations when the first attempt fails
            using (var messageFilter = new MessageFilter())
            {
                Helper.Automation.TransactionTimeout = 20000;
                using (var visualStudioContext = await visualStudioInstanceFactory.GetNewOrUsedInstanceAsync(VisualStudioInstanceFactory.RequiredPackageIds).ConfigureAwait(true))
                {
                    var executionMessageSinkFilter = new IpcMessageSink(ExecutionMessageSink, completedTestCaseIds, cancellationToken);
                    using (var runner = visualStudioContext.Instance.TestInvoker.CreateTestAssemblyRunner(new IpcTestAssembly(TestAssembly), testCases.ToArray(), new IpcMessageSink(DiagnosticMessageSink, new HashSet<string>(), cancellationToken), executionMessageSinkFilter, ExecutionOptions))
                    {
                        var result = runner.RunTestCollection(new IpcMessageBus(messageBus), testCollection, testCases.ToArray());
                        var runSummary = new RunSummary
                        {
                            Total = result.Item1,
                            Failed = result.Item2,
                            Skipped = result.Item3,
                            Time = result.Item4,
                        };

                        return Tuple.Create(runSummary, executionMessageSinkFilter.TestAssemblyFinished);
                    }
                }
            }
        }

        private (VisualStudioVersion visualStudioVersion, Guid isolatedInstanceGuid) GetVisualStudioVersionForTestCase(IXunitTestCase testCase)
        {
            if (testCase is IdeTestCase ideTestCase && string.IsNullOrEmpty(testCase.SkipReason))
            {
                var isolatedInstanceGuid = ideTestCase.IsolatedInstanceMessage is null ? Guid.Empty : Guid.NewGuid();
                return (ideTestCase.VisualStudioVersion, isolatedInstanceGuid);
            }

            return (VisualStudioVersion.Unspecified, Guid.Empty);
        }

        private class IpcMessageSink : MarshalByRefObject, IMessageSink
        {
            private readonly IMessageSink _messageSink;
            private readonly CancellationToken _cancellationToken;
            private readonly HashSet<string> _completedTestCaseIds;

            public IpcMessageSink(IMessageSink messageSink, HashSet<string> completedTestCaseIds, CancellationToken cancellationToken)
            {
                _messageSink = messageSink;
                _completedTestCaseIds = completedTestCaseIds;
                _cancellationToken = cancellationToken;
            }

            public ITestAssemblyFinished TestAssemblyFinished
            {
                get;
                private set;
            }

            public bool OnMessage(IMessageSinkMessage message)
            {
                if (message is ITestAssemblyFinished testAssemblyFinished)
                {
                    TestAssemblyFinished = new TestAssemblyFinished(testAssemblyFinished.TestCases.ToArray(), testAssemblyFinished.TestAssembly, testAssemblyFinished.ExecutionTime, testAssemblyFinished.TestsRun, testAssemblyFinished.TestsFailed, testAssemblyFinished.TestsSkipped);
                    return !_cancellationToken.IsCancellationRequested;
                }
                else if (message is ITestCaseFinished testCaseFinished)
                {
                    _completedTestCaseIds.Add(testCaseFinished.TestCase.UniqueID);
                }
                else if (message is ITestAssemblyStarting
                    || message is ITestCollectionStarting
                    || message is ITestCollectionFinished)
                {
                    return !_cancellationToken.IsCancellationRequested;
                }

                return _messageSink.OnMessage(message);
            }
        }

        private class IpcMessageBus : MarshalByRefObject, IMessageBus
        {
            private readonly IMessageBus _messageBus;

            public IpcMessageBus(IMessageBus messageBus)
            {
                _messageBus = messageBus;
            }

            public void Dispose() => _messageBus.Dispose();

            public bool QueueMessage(IMessageSinkMessage message) => _messageBus.QueueMessage(message);
        }

        private class IpcTestAssembly : LongLivedMarshalByRefObject, ITestAssembly
        {
            private readonly ITestAssembly _testAssembly;
            private readonly IAssemblyInfo _assembly;

            [EditorBrowsable(EditorBrowsableState.Never)]
            [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes", error: true)]
            public IpcTestAssembly()
            {
            }

            public IpcTestAssembly(ITestAssembly testAssembly)
            {
                _testAssembly = testAssembly;
                _assembly = new IpcAssemblyInfo(_testAssembly.Assembly);
            }

            public IAssemblyInfo Assembly => _assembly;

            public string ConfigFileName => _testAssembly.ConfigFileName;

            public void Deserialize(IXunitSerializationInfo info)
            {
                _testAssembly.Deserialize(info);
            }

            public void Serialize(IXunitSerializationInfo info)
            {
                _testAssembly.Serialize(info);
            }
        }

        private class IpcAssemblyInfo : LongLivedMarshalByRefObject, IAssemblyInfo
        {
            private IAssemblyInfo _assemblyInfo;

            public IpcAssemblyInfo(IAssemblyInfo assemblyInfo)
            {
                _assemblyInfo = assemblyInfo;
            }

            public string AssemblyPath => _assemblyInfo.AssemblyPath;

            public string Name => _assemblyInfo.Name;

            public IEnumerable<IAttributeInfo> GetCustomAttributes(string assemblyQualifiedAttributeTypeName)
            {
                return _assemblyInfo.GetCustomAttributes(assemblyQualifiedAttributeTypeName).ToArray();
            }

            public ITypeInfo GetType(string typeName)
            {
                return _assemblyInfo.GetType(typeName);
            }

            public IEnumerable<ITypeInfo> GetTypes(bool includePrivateTypes)
            {
                return _assemblyInfo.GetTypes(includePrivateTypes).ToArray();
            }
        }
    }
}
