// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
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

        private HashSet<VisualStudioInstanceKey>? _ideInstancesInTests;

        public IdeTestAssemblyRunner(ITestAssembly testAssembly, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
            : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
        {
        }

        protected override async Task AfterTestAssemblyStartingAsync()
        {
            await base.AfterTestAssemblyStartingAsync().ConfigureAwait(false);
            TestCollectionOrderer = new TestCollectionOrdererWrapper(TestCollectionOrderer);
            _ideInstancesInTests = new HashSet<VisualStudioInstanceKey>();
        }

        protected override async Task BeforeTestAssemblyFinishedAsync()
        {
            _ideInstancesInTests = null;
            TestCollectionOrderer = ((TestCollectionOrdererWrapper)TestCollectionOrderer).Underlying;
            await base.BeforeTestAssemblyFinishedAsync();
        }

        protected override async Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        {
            var result = new RunSummary();
            var completedTestCaseIds = new HashSet<string>();
            try
            {
                // Handle [Fact], and also handle IdeSkippedDataRowTestCase that doesn't run inside Visual Studio
                var nonIdeTestCases = testCases.Where(testCase => testCase is not IdeTestCaseBase).ToArray();
                if (nonIdeTestCases.Any())
                {
                    var summary = await RunTestCollectionForUnspecifiedVersionAsync(completedTestCaseIds, messageBus, testCollection, nonIdeTestCases, cancellationTokenSource);
                    result.Aggregate(summary);
                }

                var ideTestCases = testCases.OfType<IdeTestCaseBase>().Where(testCase => testCase is not IdeInstanceTestCase).ToArray();
                foreach (var testCasesByTargetVersion in ideTestCases.GroupBy(GetVisualStudioVersionForTestCase))
                {
                    _ideInstancesInTests!.Add(testCasesByTargetVersion.Key);

                    var currentInstance = testCasesByTargetVersion.Key;
                    var currentTests = testCasesByTargetVersion.ToArray();

                    for (var currentAttempt = 0; currentAttempt < testCasesByTargetVersion.Key.MaxAttempts; currentAttempt++)
                    {
                        using var marshalledObjects = new MarshalledObjects();
                        using var visualStudioInstanceFactory = new VisualStudioInstanceFactory();

                        marshalledObjects.Add(visualStudioInstanceFactory);
                        var summary = await RunTestCollectionForVersionAsync(visualStudioInstanceFactory, currentAttempt, currentInstance, completedTestCaseIds, messageBus, testCollection, currentTests, cancellationTokenSource);
                        result.Aggregate(summary);

                        currentTests = currentTests.Where(test => !completedTestCaseIds.Contains(test.UniqueID)).ToArray();
                        if (currentTests.Length == 0)
                        {
                            break;
                        }
                    }
                }

                foreach (var ideInstanceTestCase in testCases.OfType<IdeInstanceTestCase>())
                {
                    if (_ideInstancesInTests!.Contains(ideInstanceTestCase.VisualStudioInstanceKey))
                    {
                        // Already had at least one test run in this version, so no need to launch it separately.
                        // Report it as passed and continue.
                        ExecutionMessageSink.OnMessage(new TestClassStarting(new[] { ideInstanceTestCase }, ideInstanceTestCase.TestMethod.TestClass));
                        ExecutionMessageSink.OnMessage(new TestMethodStarting(new[] { ideInstanceTestCase }, ideInstanceTestCase.TestMethod));
                        ExecutionMessageSink.OnMessage(new TestCaseStarting(ideInstanceTestCase));

                        var test = new XunitTest(ideInstanceTestCase, ideInstanceTestCase.DisplayName);
                        ExecutionMessageSink.OnMessage(new TestStarting(test));
                        ExecutionMessageSink.OnMessage(new TestPassed(test, 0, output: null));
                        ExecutionMessageSink.OnMessage(new TestFinished(test, 0, output: null));

                        ExecutionMessageSink.OnMessage(new TestCaseFinished(ideInstanceTestCase, 0, 1, 0, 0));
                        ExecutionMessageSink.OnMessage(new TestMethodFinished(new[] { ideInstanceTestCase }, ideInstanceTestCase.TestMethod, 0, 1, 0, 0));
                        ExecutionMessageSink.OnMessage(new TestClassFinished(new[] { ideInstanceTestCase }, ideInstanceTestCase.TestMethod.TestClass, 0, 1, 0, 0));

                        continue;
                    }

                    using var marshalledObjects = new MarshalledObjects();
                    using (var visualStudioInstanceFactory = new VisualStudioInstanceFactory(leaveRunning: true))
                    {
                        marshalledObjects.Add(visualStudioInstanceFactory);
                        var summary = await RunTestCollectionForVersionAsync(visualStudioInstanceFactory, currentAttempt: 0, ideInstanceTestCase.VisualStudioInstanceKey, completedTestCaseIds, messageBus, testCollection, new[] { ideInstanceTestCase }, cancellationTokenSource);
                        result.Aggregate(summary);
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
                            result.Failed++;
                            ExecutionMessageSink.OnMessage(new TestFinished(test, 0, null));

                            ExecutionMessageSink.OnMessage(new TestCaseFinished(testCase, 0, 1, 1, 0));
                        }

                        ExecutionMessageSink.OnMessage(new TestMethodFinished(casesByTestMethod.ToArray(), casesByTestMethod.Key, 0, casesByTestMethod.Count(), casesByTestMethod.Count(), 0));
                    }

                    ExecutionMessageSink.OnMessage(new TestClassFinished(casesByTestClass.ToArray(), casesByTestClass.Key, 0, casesByTestClass.Count(), casesByTestClass.Count(), 0));
                }
            }

            return result;
        }

        /// <param name="currentAttempt">The 0-based attempt number. If this value is
        /// <c><see cref="VisualStudioInstanceKey.MaxAttempts"/> - 1</c>, a failed test will not be retried.</param>
        protected virtual Task<RunSummary> RunTestCollectionForVersionAsync(VisualStudioInstanceFactory visualStudioInstanceFactory, int currentAttempt, VisualStudioInstanceKey visualStudioInstanceKey, HashSet<string> completedTestCaseIds, IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        {
            if (visualStudioInstanceKey.Version == VisualStudioVersion.Unspecified
                || !IdeTestCaseBase.IsInstalled(visualStudioInstanceKey.Version))
            {
                return RunTestCollectionForUnspecifiedVersionAsync(completedTestCaseIds, messageBus, testCollection, testCases, cancellationTokenSource);
            }

            DispatcherSynchronizationContext? synchronizationContext = null;
            Dispatcher? dispatcher = null;
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

            var taskScheduler = new SynchronizationContextTaskScheduler(synchronizationContext!);
            var task = Task.Factory.StartNew(
                async () =>
                {
                    Debug.Assert(SynchronizationContext.Current is DispatcherSynchronizationContext, "Assertion failed: SynchronizationContext.Current is DispatcherSynchronizationContext");

                    using (await WpfTestSharedData.Instance.TestSerializationGate.DisposableWaitAsync(CancellationToken.None))
                    {
                        // Just call back into the normal xUnit dispatch process now that we are on an STA Thread with no synchronization context.
                        var invoker = CreateTestCollectionInvoker(visualStudioInstanceFactory, currentAttempt, visualStudioInstanceKey, completedTestCaseIds, messageBus, testCollection, testCases, cancellationTokenSource);
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
                        dispatcher!.InvokeShutdown();

                        // Join the STA thread, which ensures shutdown is complete.
                        staThread.Join(HangMitigatingTimeout);
                    }
                });
        }

        private async Task<RunSummary> RunTestCollectionForUnspecifiedVersionAsync(HashSet<string> completedTestCaseIds, IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        {
            // These tests just run in the current process, but we still need to hook the assembly and collection events
            // to work correctly in mixed-testing scenarios.
            using var marshalledObjects = new MarshalledObjects();
            var executionMessageSinkFilter = new IpcMessageSink(ExecutionMessageSink, testCases.ToDictionary<IXunitTestCase, string, ITestCase>(testCase => testCase.UniqueID, testCase => testCase), finalAttempt: true, completedTestCaseIds, cancellationTokenSource.Token);
            marshalledObjects.Add(executionMessageSinkFilter);
            using (var runner = new XunitTestAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSinkFilter, ExecutionOptions))
            {
                var runSummary = await runner.RunAsync();
                return runSummary;
            }
        }

        /// <param name="currentAttempt">The 0-based attempt number. If this value is
        /// <c><see cref="VisualStudioInstanceKey.MaxAttempts"/> - 1</c>, a failed test will not be retried.</param>
        private Func<Task<RunSummary>> CreateTestCollectionInvoker(VisualStudioInstanceFactory visualStudioInstanceFactory, int currentAttempt, VisualStudioInstanceKey visualStudioInstanceKey, HashSet<string> completedTestCaseIds, IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        {
            return async () =>
            {
                Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());

                using var marshalledObjects = new MarshalledObjects();

                IpcMessageSink? executionMessageSinkFilter = null;

                try
                {
                    var finalAttempt = currentAttempt == visualStudioInstanceKey.MaxAttempts - 1;
                    var knownTestCasesByUniqueId = testCases.ToDictionary<IXunitTestCase, string, ITestCase>(testCase => testCase.UniqueID, testCase => testCase);
                    executionMessageSinkFilter = new IpcMessageSink(ExecutionMessageSink, knownTestCasesByUniqueId, finalAttempt, completedTestCaseIds, cancellationTokenSource.Token);
                    marshalledObjects.Add(executionMessageSinkFilter);

                    // Use SetItems instead of ToImmutableDictionary to avoid exceptions in the case of value conflicts
                    var environmentVariables = ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase).SetItems(
                        visualStudioInstanceKey.EnvironmentVariables.Select(
                            variable => variable.IndexOf('=') is var index && index > 0
                                ? new KeyValuePair<string, string>(variable.Substring(0, index), variable.Substring(index + 1))
                                : new KeyValuePair<string, string>(variable, string.Empty)));

                    // Install a COM message filter to handle retry operations when the first attempt fails
                    using (var messageFilter = new MessageFilter())
                    using (var visualStudioContext = await visualStudioInstanceFactory.GetNewOrUsedInstanceAsync(GetVersion(visualStudioInstanceKey.Version), visualStudioInstanceKey.RootSuffix, environmentVariables, GetExtensionFiles(testCases), ImmutableHashSet.Create<string>()).ConfigureAwait(true))
                    {
                        using (var runner = visualStudioContext.Instance.TestInvoker.CreateTestAssemblyRunner(new IpcTestAssembly(TestAssembly), testCases.ToArray(), new IpcMessageSink(DiagnosticMessageSink, knownTestCasesByUniqueId, finalAttempt, new HashSet<string>(), cancellationTokenSource.Token), executionMessageSinkFilter, ExecutionOptions))
                        {
                            marshalledObjects.Add(runner);

                            var ipcMessageBus = new IpcMessageBus(messageBus);
                            marshalledObjects.Add(ipcMessageBus);

                            var result = runner.RunTestCollection(ipcMessageBus, testCollection, testCases.ToArray());
                            var runSummary = new RunSummary
                            {
                                Total = result.Item1,
                                Failed = result.Item2,
                                Skipped = result.Item3,
                                Time = result.Item4,
                            };

                            return runSummary;
                        }
                    }
                }
                catch (Exception e)
                {
                    // Since this exception occurred in the harness communication, we can't assume it was logged by the
                    // in-process data collection service. We need to log it separately here.
                    DataCollectionService.CaptureFailureState(executionMessageSinkFilter?.CurrentTestCase ?? "Unknown", e);

                    var previousException = WpfTestSharedData.Instance.Exception;
                    try
                    {
                        WpfTestSharedData.Instance.Exception = e;
                        return await RunTestCollectionForUnspecifiedVersionAsync(completedTestCaseIds, messageBus, testCollection, testCases, cancellationTokenSource).ConfigureAwait(true);
                    }
                    finally
                    {
                        WpfTestSharedData.Instance.Exception = previousException;
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

        private VisualStudioInstanceKey GetVisualStudioVersionForTestCase(IXunitTestCase testCase)
        {
            if (testCase is IdeTestCaseBase ideTestCase)
            {
                return ideTestCase.VisualStudioInstanceKey;
            }

            return VisualStudioInstanceKey.Unspecified;
        }

        private class IpcMessageSink : MarshalByRefObject, IMessageSink
        {
            private readonly IMessageSink _messageSink;
            private readonly IReadOnlyDictionary<string, ITestCase> _knownTestCasesByUniqueId;
            private readonly CancellationToken _cancellationToken;

            private readonly bool _finalAttempt;
            private readonly HashSet<string> _completedTestCaseIds;

            public IpcMessageSink(IMessageSink messageSink, IReadOnlyDictionary<string, ITestCase> knownTestCasesByUniqueId, bool finalAttempt, HashSet<string> completedTestCaseIds, CancellationToken cancellationToken)
            {
                _messageSink = messageSink;
                _knownTestCasesByUniqueId = knownTestCasesByUniqueId;
                _finalAttempt = finalAttempt;
                _completedTestCaseIds = completedTestCaseIds;
                _cancellationToken = cancellationToken;
            }

            public string? CurrentTestCase
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
                            return new IdeTestCase(this, ideTestCase.DefaultMethodDisplay, ideTestCase.DefaultMethodDisplayOptions, ideTestCase.TestMethod, ideTestCase.VisualStudioInstanceKey, ideTestCase.TestMethodArguments);
                        }
                        else if (testCase is IdeTheoryTestCase ideTheoryTestCase)
                        {
                            return new IdeTheoryTestCase(this, ideTheoryTestCase.DefaultMethodDisplay, ideTheoryTestCase.DefaultMethodDisplayOptions, ideTheoryTestCase.TestMethod, ideTheoryTestCase.VisualStudioInstanceKey, ideTheoryTestCase.TestMethodArguments);
                        }
                        else if (testCase is IdeInstanceTestCase ideInstanceTestCase)
                        {
                            return new IdeInstanceTestCase(this, ideInstanceTestCase.DefaultMethodDisplay, ideInstanceTestCase.DefaultMethodDisplayOptions, ideInstanceTestCase.TestMethod, ideInstanceTestCase.VisualStudioInstanceKey, ideInstanceTestCase.TestMethodArguments);
                        }
                        else
                        {
                            return new XunitTestCase(this, TestMethodDisplay.ClassAndMethod, TestMethodDisplayOptions.None, testCase.TestMethod, testCase.TestMethodArguments);
                        }
                    });

                    return !_cancellationToken.IsCancellationRequested;
                }
                else if (message is ITestCaseStarting testCaseStarting)
                {
                    CurrentTestCase = DataCollectionService.GetTestName(testCaseStarting.TestCase);
                    return _messageSink.OnMessage(message);
                }
                else if (message is ITestCaseFinished testCaseFinished)
                {
                    CurrentTestCase = null;

                    if (_finalAttempt || testCaseFinished.TestsFailed == 0)
                    {
                        _completedTestCaseIds.Add(testCaseFinished.TestCase.UniqueID);
                    }
                    else
                    {
                        // This test will run again; report the statistics as skipped instead of failed
                        message = new TestCaseFinished(
                            testCaseFinished.TestCase,
                            testCaseFinished.ExecutionTime,
                            testCaseFinished.TestsRun,
                            testsFailed: 0,
                            testCaseFinished.TestsSkipped + testCaseFinished.TestsFailed);
                    }

                    return !_cancellationToken.IsCancellationRequested;
                }
                else if (!_finalAttempt && message is ITestFailed testFailed)
                {
                    // This test will run again; report it as skipped instead of failed
                    // TODO: What kind of additional logs should we include?
                    message = new TestSkipped(testFailed.Test, "Test will automatically retry.");
                }
                else if (message is ITestAssemblyStarting)
                {
                    return !_cancellationToken.IsCancellationRequested;
                }

                return _messageSink.OnMessage(message);
            }

            // The life of this object is managed explicitly
            public override object? InitializeLifetimeService()
            {
                return null;
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

            // The life of this object is managed explicitly
            public override object? InitializeLifetimeService() => null;
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

        /// <summary>
        /// A collection orderer wrapper that ensures <see cref="IdeInstanceTestCase"/> runs after other test cases.
        /// </summary>
        private sealed class TestCollectionOrdererWrapper : ITestCollectionOrderer
        {
            public TestCollectionOrdererWrapper(ITestCollectionOrderer underlying)
            {
                Underlying = underlying;
            }

            public ITestCollectionOrderer Underlying { get; }

            public IEnumerable<ITestCollection> OrderTestCollections(IEnumerable<ITestCollection> testCollections)
            {
                var collections = Underlying.OrderTestCollections(testCollections).ToArray();
                var collectionsWithoutIdeInstanceCases = collections.Where(collection => !ContainsIdeInstanceCase(collection));
                var collectionsWithIdeInstanceCases = collections.Where(collection => ContainsIdeInstanceCase(collection));
                return collectionsWithoutIdeInstanceCases.Concat(collectionsWithIdeInstanceCases);
            }

            private static bool ContainsIdeInstanceCase(ITestCollection collection)
            {
                var assemblyName = new AssemblyName(collection.TestAssembly.Assembly.Name);
                return assemblyName.Name == "Microsoft.VisualStudio.Extensibility.Testing.Xunit";
            }
        }
    }
}
