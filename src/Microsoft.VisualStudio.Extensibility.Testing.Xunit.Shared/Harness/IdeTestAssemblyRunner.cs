// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Threading;
    using Xunit.Abstractions;
    using Xunit.Sdk;
    using Xunit.Threading;

    internal class IdeTestAssemblyRunner : XunitTestAssemblyRunner
    {
        /// <summary>
        /// A long timeout used to avoid hangs in tests, where a test failure manifests as an operation never occurring.
        /// </summary>
        private static readonly TimeSpan HangMitigatingTimeout = TimeSpan.FromMinutes(1);

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
                ExecutionMessageSink.OnMessage(new TestAssemblyStarting(testCases, TestAssembly, DateTime.Now, GetTestFrameworkEnvironment(), GetTestFrameworkDisplayName()));

                foreach (var testCasesByTargetVersion in testCases.GroupBy(GetVisualStudioVersionForTestCase))
                {
                    using (var visualStudioInstanceFactory = new VisualStudioInstanceFactory())
                    {
                        var summary = await RunTestCollectionForVersionAsync(visualStudioInstanceFactory, testCasesByTargetVersion.Key, completedTestCaseIds, messageBus, testCollection, testCasesByTargetVersion, cancellationTokenSource);
                        result.Aggregate(summary.Item1);
                        testAssemblyFinishedMessages.Add(summary.Item2);
                    }
                }
            }
            catch (Exception ex)
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
                            ExecutionMessageSink.OnMessage(new TestFailed(test, 0, null, new InvalidOperationException("Test did not run due to a harness failure.", ex)));
                            ExecutionMessageSink.OnMessage(new TestFinished(test, 0, null));

                            ExecutionMessageSink.OnMessage(new TestCaseFinished(testCase, 0, 1, 1, 0));
                        }

                        ExecutionMessageSink.OnMessage(new TestMethodFinished(casesByTestMethod.ToArray(), casesByTestMethod.Key, 0, casesByTestMethod.Count(), casesByTestMethod.Count(), 0));
                    }

                    ExecutionMessageSink.OnMessage(new TestClassFinished(casesByTestClass.ToArray(), casesByTestClass.Key, 0, casesByTestClass.Count(), casesByTestClass.Count(), 0));
                }

                throw;
            }
            finally
            {
                var totalExecutionTime = testAssemblyFinishedMessages.Sum(message => message.ExecutionTime);
                var testsRun = testAssemblyFinishedMessages.Sum(message => message.TestsRun);
                var testsFailed = testAssemblyFinishedMessages.Sum(message => message.TestsFailed);
                var testsSkipped = testAssemblyFinishedMessages.Sum(message => message.TestsSkipped);
                ExecutionMessageSink.OnMessage(new TestAssemblyFinished(testCases, TestAssembly, totalExecutionTime, testsRun, testsFailed, testsSkipped));
            }

            return result;
        }

        protected virtual Task<Tuple<RunSummary, ITestAssemblyFinished>> RunTestCollectionForVersionAsync(VisualStudioInstanceFactory visualStudioInstanceFactory, VisualStudioVersion visualStudioVersion, HashSet<string> completedTestCaseIds, IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        {
            if (visualStudioVersion == VisualStudioVersion.Unspecified
                || !IdeTestCase.IsInstalled(visualStudioVersion))
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

                staThread.Name = $"{nameof(IdeTestAssemblyRunner)}";
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();

                staThreadStartedEvent.Wait();
#pragma warning disable CA1508 // Avoid dead conditional code
                Debug.Assert(synchronizationContext != null, "Assertion failed: synchronizationContext != null");
#pragma warning restore CA1508 // Avoid dead conditional code
            }

            var taskScheduler = new SynchronizationContextTaskScheduler(synchronizationContext);
            var task = Task.Factory.StartNew(
                async () =>
                {
                    Debug.Assert(SynchronizationContext.Current is DispatcherSynchronizationContext, "Assertion failed: SynchronizationContext.Current is DispatcherSynchronizationContext");

                    using (await WpfTestSharedData.Instance.TestSerializationGate.DisposableWaitAsync(CancellationToken.None))
                    {
                        // Just call back into the normal xUnit dispatch process now that we are on an STA Thread with no synchronization context.
                        var invoker = CreateTestCollectionInvoker(visualStudioInstanceFactory, visualStudioVersion, completedTestCaseIds, messageBus, testCollection, testCases, cancellationTokenSource);
                        return await invoker().ConfigureAwait(true);
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
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
                        return await task.ConfigureAwait(false);
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
                    }
                    finally
                    {
                        // Make sure to shut down the dispatcher. Certain framework types listed for the dispatcher
                        // shutdown to perform cleanup actions. In the absence of an explicit shutdown, these actions
                        // are delayed and run during AppDomain or process shutdown, where they can lead to crashes of
                        // the test process.
                        dispatcher.InvokeShutdown();

                        // Join the STA thread, which ensures shutdown is complete.
                        staThread.Join(HangMitigatingTimeout);
                    }
                });
        }

        private async Task<Tuple<RunSummary, ITestAssemblyFinished>> RunTestCollectionForUnspecifiedVersionAsync(HashSet<string> completedTestCaseIds, IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        {
            // These tests just run in the current process, but we still need to hook the assembly and collection events
            // to work correctly in mixed-testing scenarios.
            var executionMessageSinkFilter = new IpcMessageSink(ExecutionMessageSink, testCases.ToDictionary<IXunitTestCase, string, ITestCase>(testCase => testCase.UniqueID, testCase => testCase), completedTestCaseIds, cancellationTokenSource.Token);
            using (var runner = new XunitTestAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSinkFilter, ExecutionOptions))
            {
                var runSummary = await runner.RunAsync();
                return Tuple.Create(runSummary, executionMessageSinkFilter.TestAssemblyFinished);
            }
        }

        private Func<Task<Tuple<RunSummary, ITestAssemblyFinished>>> CreateTestCollectionInvoker(VisualStudioInstanceFactory visualStudioInstanceFactory, VisualStudioVersion visualStudioVersion, HashSet<string> completedTestCaseIds, IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        {
            return async () =>
            {
                Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());

                // Install a COM message filter to handle retry operations when the first attempt fails
                using (var messageFilter = new MessageFilter())
                {
                    using (var visualStudioContext = await visualStudioInstanceFactory.GetNewOrUsedInstanceAsync(GetVersion(visualStudioVersion), GetExtensionFiles(testCases), ImmutableHashSet.Create<string>()).ConfigureAwait(true))
                    {
                        var knownTestCasesByUniqueId = testCases.ToDictionary<IXunitTestCase, string, ITestCase>(testCase => testCase.UniqueID, testCase => testCase);
                        var executionMessageSinkFilter = new IpcMessageSink(ExecutionMessageSink, knownTestCasesByUniqueId, completedTestCaseIds, cancellationTokenSource.Token);
                        using (var runner = visualStudioContext.Instance.TestInvoker.CreateTestAssemblyRunner(new IpcTestAssembly(TestAssembly), testCases.ToArray(), new IpcMessageSink(DiagnosticMessageSink, knownTestCasesByUniqueId, new HashSet<string>(), cancellationTokenSource.Token), executionMessageSinkFilter, ExecutionOptions))
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
            };
        }

        private ImmutableList<string> GetExtensionFiles(IEnumerable<IXunitTestCase> testCases)
        {
            var extensionFiles = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<IAssemblyInfo>();
            foreach (var testCase in testCases)
            {
                var assemblyInfo = testCase.Method.Type.Assembly;
                if (!visited.Add(assemblyInfo))
                {
                    continue;
                }

                var requiredExtensions = assemblyInfo.GetCustomAttributes(typeof(RequireExtensionAttribute));
                extensionFiles = extensionFiles.Union(requiredExtensions.Select(attributeInfo => attributeInfo.GetConstructorArguments().First().ToString()));
            }

            return extensionFiles.ToImmutableList();
        }

        private static Version GetVersion(VisualStudioVersion visualStudioVersion)
        {
            switch (visualStudioVersion)
            {
            case VisualStudioVersion.VS2012:
                return new Version(11, 0);

            case VisualStudioVersion.VS2013:
                return new Version(12, 0);

            case VisualStudioVersion.VS2015:
                return new Version(14, 0);

            case VisualStudioVersion.VS2017:
                return new Version(15, 0);

            case VisualStudioVersion.VS2019:
                return new Version(16, 0);

            case VisualStudioVersion.VS2022:
                return new Version(17, 0);

            default:
                throw new ArgumentException();
            }
        }

        private VisualStudioVersion GetVisualStudioVersionForTestCase(IXunitTestCase testCase)
        {
            if (testCase is IdeTestCase ideTestCase)
            {
                return ideTestCase.VisualStudioVersion;
            }

            return VisualStudioVersion.Unspecified;
        }

        private class IpcMessageSink : MarshalByRefObject, IMessageSink
        {
            private readonly IMessageSink _messageSink;
            private readonly IReadOnlyDictionary<string, ITestCase> _knownTestCasesByUniqueId;
            private readonly CancellationToken _cancellationToken;

            private readonly HashSet<string> _completedTestCaseIds;

            public IpcMessageSink(IMessageSink messageSink, IReadOnlyDictionary<string, ITestCase> knownTestCasesByUniqueId, HashSet<string> completedTestCaseIds, CancellationToken cancellationToken)
            {
                _messageSink = messageSink;
                _knownTestCasesByUniqueId = knownTestCasesByUniqueId;
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
                    // The test cases in the ITestAssemblyFinished message are remote proxies, but the objects won't be
                    // used until after the remote process terminates. Recreate the objects in the current process (or
                    // map them to an equivalent object already in the current process) to avoid using objects that are
                    // no longer available.
                    var testCases = testAssemblyFinished.TestCases.Select(testCase =>
                    {
                        if (_knownTestCasesByUniqueId.TryGetValue(testCase.UniqueID, out var knownTestCase))
                        {
                            return knownTestCase;
                        }
                        else if (testCase is IdeTestCase ideTestCase)
                        {
                            return new IdeTestCase(this, ideTestCase.DefaultMethodDisplay, ideTestCase.TestMethod, ideTestCase.VisualStudioVersion, ideTestCase.TestMethodArguments);
                        }
                        else
                        {
                            return new XunitTestCase(this, TestMethodDisplay.ClassAndMethod, testCase.TestMethod, testCase.TestMethodArguments);
                        }
                    });

                    TestAssemblyFinished = new TestAssemblyFinished(testCases.ToArray(), testAssemblyFinished.TestAssembly, testAssemblyFinished.ExecutionTime, testAssemblyFinished.TestsRun, testAssemblyFinished.TestsFailed, testAssemblyFinished.TestsSkipped);
                    return !_cancellationToken.IsCancellationRequested;
                }
                else if (message is ITestCaseFinished testCaseFinished)
                {
                    _completedTestCaseIds.Add(testCaseFinished.TestCase.UniqueID);
                    return !_cancellationToken.IsCancellationRequested;
                }
                else if (message is ITestAssemblyStarting)
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
