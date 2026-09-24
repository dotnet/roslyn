// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    using Xunit.Sdk;
    using Xunit.Threading;
    using Xunit.v3;

    internal class IdeTestAssemblyRunner : XunitTestAssemblyRunner
    {
        private readonly IMessageSink _executionMessageSink;
        private readonly ITestFrameworkDiscoveryOptions _discoveryOptions;
        private readonly ITestFrameworkExecutionOptions _executionOptions;

        /// <summary>
        /// A long timeout used to avoid hangs in tests, where a test failure manifests as an operation never occurring.
        /// </summary>
        private static readonly TimeSpan HangMitigatingTimeout = TimeSpan.FromMinutes(1);

        private HashSet<VisualStudioInstanceKey>? _ideInstancesInTests;

        public IdeTestAssemblyRunner(IMessageSink executionMessageSink, ITestFrameworkDiscoveryOptions discoveryOptions, ITestFrameworkExecutionOptions executionOptions)
        {
            _executionMessageSink = executionMessageSink;
            _discoveryOptions = discoveryOptions;
            _executionOptions = executionOptions;
        }

        protected override async ValueTask<bool> OnTestAssemblyStarting(XunitTestAssemblyRunnerContext ctxt)
        {
            if (!await base.OnTestAssemblyStarting(ctxt).ConfigureAwait(false))
            {
                return false;
            }

            _ideInstancesInTests = new HashSet<VisualStudioInstanceKey>();
            return true;
        }

        protected override async ValueTask<bool> OnTestAssemblyFinished(XunitTestAssemblyRunnerContext ctxt, RunSummary summary)
        {
            _ideInstancesInTests = null;
            return await base.OnTestAssemblyFinished(ctxt, summary).ConfigureAwait(false);
        }

        protected override async ValueTask<RunSummary> RunTestCollection(XunitTestAssemblyRunnerContext ctxt, IXunitTestCollection testCollection, IReadOnlyCollection<IXunitTestCase> testCases)
        {
#pragma warning disable SA1129 // Do not use default value type constructor
            var result = new RunSummary();
#pragma warning restore SA1129 // Do not use default value type constructor
            var completedTestCaseIds = new HashSet<string>();
            try
            {
                // Handle [Fact], and also handle IdeSkippedDataRowTestCase that doesn't run inside Visual Studio
                var nonIdeTestCases = testCases.Where(testCase => testCase is not IdeTestCaseBase).ToArray();
                if (nonIdeTestCases.Any())
                {
                    var summary = await RunTestCollectionForUnspecifiedVersionAsync(ctxt.TestAssembly, testCollection, nonIdeTestCases, completedTestCaseIds, ctxt.CancellationTokenSource).ConfigureAwait(true);
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
                        var summary = await RunTestCollectionForVersionAsync(visualStudioInstanceFactory, currentAttempt, currentInstance, ctxt, testCollection, currentTests, completedTestCaseIds, ctxt.CancellationTokenSource).ConfigureAwait(true);
                        result.Aggregate(summary);

                        currentTests = currentTests.Where(test => !completedTestCaseIds.Contains(test.UniqueID)).ToArray();
                        if (currentTests.Length == 0)
                        {
                            break;
                        }
                    }
                }

#if IDE_INSTANCE_TEST_CASE_SUPPORT

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
                        var summary = await RunTestCollectionForVersionAsync(visualStudioInstanceFactory, currentAttempt: 0, ideInstanceTestCase.VisualStudioInstanceKey, completedTestCaseIds, ctxt.MessageBus, testCollection, new[] { ideInstanceTestCase }, cancellationTokenSource);
                        result.Aggregate(summary);
                    }
                }

#endif

            }
            catch (Exception ex)
            {
                // We have had a failure in the test harness entirely; rather than trying to restart Visual Studio which will probably fail due to the same
                // reason, we'll report failures for all the remaining tests.
                // TODO: we can probably simplify this by moving this to an implementation of TestCaseRunnerBase where the RunTestcase method simply returns the known exception.
                var completedTestCases = testCases.Where(testCase => completedTestCaseIds.Contains(testCase.UniqueID));
                var remainingTestCases = testCases.Except(completedTestCases);
                foreach (var casesByTestClass in remainingTestCases.GroupBy(testCase => testCase.TestMethod.TestClass))
                {
                    var testClass = casesByTestClass.Key;
                    var testClassStartTime = DateTimeOffset.UtcNow;

                    _executionMessageSink.OnMessage(new TestClassStarting
                    {
                        AssemblyUniqueID = ctxt.TestAssembly.UniqueID,
                        StartTime = testClassStartTime,
                        TestClassName = testClass.TestClassName,
                        TestClassNamespace = testClass.TestClassNamespace,
                        TestClassSimpleName = testClass.TestClassSimpleName,
                        TestClassUniqueID = testClass.UniqueID,
                        TestCollectionUniqueID = testClass.TestCollection.UniqueID,
                        Traits = testClass.Traits,
                    });

                    foreach (var casesByTestMethod in casesByTestClass.GroupBy(testCase => testCase.TestMethod))
                    {
                        var testMethod = casesByTestMethod.Key;
                        var testMethodStartTime = DateTimeOffset.UtcNow;

                        _executionMessageSink.OnMessage(new TestMethodStarting
                        {
                            AssemblyUniqueID = ctxt.TestAssembly.UniqueID,
                            MethodArity = testMethod.MethodArity,
                            MethodName = testMethod.MethodName,
                            StartTime = testMethodStartTime,
                            TestClassUniqueID = testClass.UniqueID,
                            TestCollectionUniqueID = testClass.TestCollection.UniqueID,
                            TestMethodUniqueID = testMethod.UniqueID,
                            Traits = testMethod.Traits,
                        });

                        foreach (var testCase in casesByTestMethod)
                        {
                            var testCaseStartTime = DateTimeOffset.UtcNow;

                            _executionMessageSink.OnMessage(new TestCaseStarting
                            {
                                AssemblyUniqueID = ctxt.TestAssembly.UniqueID,
                                Explicit = testCase.Explicit,
                                SkipReason = testCase.SkipReason,
                                SourceFilePath = testCase.SourceFilePath,
                                SourceLineNumber = testCase.SourceLineNumber,
                                StartTime = testCaseStartTime,
                                TestCaseDisplayName = testCase.TestCaseDisplayName,
                                TestCaseUniqueID = testCase.UniqueID,
                                TestClassMetadataToken = testCase.TestClassMetadataToken,
                                TestClassName = testClass.TestClassName,
                                TestClassNamespace = testClass.TestClassNamespace,
                                TestClassSimpleName = testClass.TestClassSimpleName,
                                TestClassUniqueID = testClass.UniqueID,
                                TestCollectionUniqueID = testClass.TestCollection.UniqueID,
                                TestMethodArity = testMethod.MethodArity,
                                TestMethodMetadataToken = testCase.TestMethodMetadataToken,
                                TestMethodName = testMethod.MethodName,
                                TestMethodParameterTypesVSTest = testCase.TestMethodParameterTypesVSTest,
                                TestMethodReturnTypeVSTest = testCase.TestMethodReturnTypeVSTest,
                                TestMethodUniqueID = testMethod.UniqueID,
                                Traits = testMethod.Traits,
                            });

                            _executionMessageSink.OnMessage(new TestStarting
                            {
                                AssemblyUniqueID = ctxt.TestAssembly.UniqueID,
                                TestCaseUniqueID = testCase.UniqueID,
                                TestClassUniqueID = testClass.UniqueID,
                                TestCollectionUniqueID = testClass.TestCollection.UniqueID,
                                TestDisplayName = testCase.TestCaseDisplayName,
                                TestLabel = null,
                                TestMethodUniqueID = testMethod.UniqueID,
                                TestUniqueID = testCase.UniqueID,
                                Explicit = testCase.Explicit,
                                StartTime = DateTimeOffset.UtcNow,
                                Timeout = 0,
                                Traits = testCase.Traits,
                            });
                            _executionMessageSink.OnMessage(new TestFailed
                            {
                                AssemblyUniqueID = ctxt.TestAssembly.UniqueID,
                                Cause = FailureCause.Exception,
                                ExceptionParentIndices = new[] { -1 },
                                ExceptionTypes = new[] { "System.InvalidOperationException" },
                                ExecutionTime = 0,
                                FinishTime = DateTimeOffset.UtcNow,
                                Messages = new[] { "Test did not run due to a harness failure." },
                                Output = string.Empty,
                                StackTraces = new[] { ex.ToString() },
                                TestCaseUniqueID = testCase.UniqueID,
                                TestClassUniqueID = testClass.UniqueID,
                                TestCollectionUniqueID = testClass.TestCollection.UniqueID,
                                TestMethodUniqueID = testMethod.UniqueID,
                                TestUniqueID = testCase.UniqueID,
                                Warnings = null,
                            });
                            result.Failed++;
                            _executionMessageSink.OnMessage(new TestFinished
                            {
                                AssemblyUniqueID = ctxt.TestAssembly.UniqueID,
                                ExecutionTime = 0,
                                FinishTime = DateTimeOffset.UtcNow,
                                Output = string.Empty,
                                TestCaseUniqueID = testCase.UniqueID,
                                TestClassUniqueID = testClass.UniqueID,
                                TestCollectionUniqueID = testClass.TestCollection.UniqueID,
                                TestMethodUniqueID = testMethod.UniqueID,
                                TestUniqueID = testCase.UniqueID,
                                Attachments = new Dictionary<string, TestAttachment>(),
                                Warnings = null,
                            });

                            _executionMessageSink.OnMessage(new TestCaseFinished
                            {
                                AssemblyUniqueID = ctxt.TestAssembly.UniqueID,
                                ExecutionTime = 0m,
                                FinishTime = DateTimeOffset.UtcNow,
                                TestCaseUniqueID = testCase.UniqueID,
                                TestClassUniqueID = testClass.UniqueID,
                                TestCollectionUniqueID = testClass.TestCollection.UniqueID,
                                TestMethodUniqueID = testMethod.UniqueID,
                                TestsFailed = 1,
                                TestsNotRun = 0,
                                TestsSkipped = 0,
                                TestsTotal = 1,
                            });
                        }

                        _executionMessageSink.OnMessage(new TestMethodFinished
                        {
                            AssemblyUniqueID = ctxt.TestAssembly.UniqueID,
                            ExecutionTime = 0m,
                            FinishTime = DateTimeOffset.UtcNow,
                            TestClassUniqueID = testClass.UniqueID,
                            TestCollectionUniqueID = testClass.TestCollection.UniqueID,
                            TestMethodUniqueID = testMethod.UniqueID,
                            TestsFailed = casesByTestMethod.Count(),
                            TestsNotRun = 0,
                            TestsSkipped = 0,
                            TestsTotal = casesByTestMethod.Count(),
                        });
                    }

                    _executionMessageSink.OnMessage(new TestClassFinished
                    {
                        AssemblyUniqueID = ctxt.TestAssembly.UniqueID,
                        ExecutionTime = 0m,
                        FinishTime = DateTimeOffset.UtcNow,
                        TestClassUniqueID = testClass.UniqueID,
                        TestCollectionUniqueID = testClass.TestCollection.UniqueID,
                        TestsFailed = casesByTestClass.Count(),
                        TestsNotRun = 0,
                        TestsSkipped = 0,
                        TestsTotal = casesByTestClass.Count(),
                    });
                }
            }

            return result;
        }

        /// <param name="currentAttempt">The 0-based attempt number. If this value is
        /// <c><see cref="VisualStudioInstanceKey.MaxAttempts"/> - 1</c>, a failed test will not be retried.</param>
        protected virtual Task<RunSummary> RunTestCollectionForVersionAsync(
            VisualStudioInstanceFactory visualStudioInstanceFactory,
            int currentAttempt,
            VisualStudioInstanceKey visualStudioInstanceKey,
            XunitTestAssemblyRunnerContext ctxt,
            ITestCollection testCollection,
            IReadOnlyCollection<IXunitTestCase> testCases,
            HashSet<string> completedTestCaseIds,
            CancellationTokenSource cancellationTokenSource)
        {
            if (visualStudioInstanceKey.Version == VisualStudioVersion.Unspecified
                || !IdeTestCaseBase.IsInstalled(visualStudioInstanceKey.Version))
            {
                return RunTestCollectionForUnspecifiedVersionAsync(ctxt.TestAssembly, testCollection, testCases, completedTestCaseIds, cancellationTokenSource);
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

                    using (await WpfTestSharedData.Instance.TestSerializationGate.DisposableWaitAsync(CancellationToken.None).ConfigureAwait(true))
                    {
                        // Just call back into the normal xUnit dispatch process now that we are on an STA Thread with no synchronization context.
                        var invoker = CreateTestCollectionInvoker(visualStudioInstanceFactory, currentAttempt, visualStudioInstanceKey, ctxt, ctxt.TestAssembly, testCollection, testCases, completedTestCaseIds, cancellationTokenSource);
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

        private async Task<RunSummary> RunTestCollectionForUnspecifiedVersionAsync(IXunitTestAssembly testAssembly, ITestCollection testCollection, IReadOnlyCollection<IXunitTestCase> testCases, HashSet<string> completedTestCaseIds, CancellationTokenSource cancellationTokenSource)
        {
            // TODO: figure out where this hooking is coming from
            // These tests just run in the current process, but we still need to hook the assembly and collection events
            // to work correctly in mixed-testing scenarios.
            using var marshalledObjects = new MarshalledObjects();

            // TODO: fix this
            // var executionMessageSinkFilter = new IpcMessageSink(ExecutionMessageSink, testCases.ToDictionary<IXunitTestCase, string, ITestCase>(testCase => testCase.UniqueID, testCase => testCase), finalAttempt: true, completedTestCaseIds, cancellationTokenSource.Token);
            return await XunitTestAssemblyRunner.Instance.Run(testAssembly, testCases, _executionMessageSink, _executionOptions, cancellationTokenSource.Token);
        }

        /// <param name="currentAttempt">The 0-based attempt number. If this value is
        /// <c><see cref="VisualStudioInstanceKey.MaxAttempts"/> - 1</c>, a failed test will not be retried.</param>
        private Func<Task<RunSummary>> CreateTestCollectionInvoker(
            VisualStudioInstanceFactory visualStudioInstanceFactory,
            int currentAttempt,
            VisualStudioInstanceKey visualStudioInstanceKey,
            XunitTestAssemblyRunnerContext ctxt,
            IXunitTestAssembly testAssembly,
            ITestCollection testCollection,
            IReadOnlyCollection<IXunitTestCase> testCases,
            HashSet<string> completedTestCaseIds,
            CancellationTokenSource cancellationTokenSource)
        {
            return async () =>
            {
                Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());

                using var marshalledObjects = new MarshalledObjects();

                try
                {
                    var finalAttempt = currentAttempt == visualStudioInstanceKey.MaxAttempts - 1;
                    var knownTestCasesByUniqueId = testCases.ToDictionary<IXunitTestCase, string, ITestCase>(testCase => testCase.UniqueID, testCase => testCase);

                    // Use SetItems instead of ToImmutableDictionary to avoid exceptions in the case of value conflicts
                    var environmentVariables = ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase).SetItems(
                        visualStudioInstanceKey.EnvironmentVariables.Select(
                            variable => variable.IndexOf('=') is var index && index > 0
                                ? new KeyValuePair<string, string>(variable.Substring(0, index), variable.Substring(index + 1))
                                : new KeyValuePair<string, string>(variable, string.Empty)));

                    // Install a COM message filter to handle retry operations when the first attempt fails
                    using (var messageFilter = new MessageFilter())
                    using (var visualStudioContext = await visualStudioInstanceFactory.GetNewOrUsedInstanceAsync(GetVersion(visualStudioInstanceKey.Version), visualStudioInstanceKey.RootSuffix, environmentVariables, GetExtensionFiles(testAssembly), ImmutableHashSet.Create<string>()).ConfigureAwait(true))
                    {
                        var runner = visualStudioContext.Instance.TestInvoker.CreateTestAssemblyRunner();
                        marshalledObjects.Add(runner);

                        var messageSink = new DeserializingMessageSink(_executionMessageSink);
                        marshalledObjects.Add(messageSink);

                        var discoveryOptionsProxy = new IpcTestFrameworkDiscoveryOptions(_discoveryOptions);
                        marshalledObjects.Add(discoveryOptionsProxy);

                        var executionOptionsProxy = new IpcTestFrameworkExecutionOptions(_executionOptions);
                        marshalledObjects.Add(executionOptionsProxy);

                        var result = runner.RunTestCollection(ctxt.TestAssembly.AssemblyPath, [.. knownTestCasesByUniqueId.Keys], messageSink, discoveryOptionsProxy, executionOptionsProxy);
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
                catch (Exception e)
                {
                    // Since this exception occurred in the harness communication, we can't assume it was logged by the
                    // in-process data collection service. We need to log it separately here.
                    /*
                    DataCollectionService.CaptureFailureState(executionMessageSinkFilter?.CurrentTestCase ?? "Unknown", e);
                    */

                    var previousException = WpfTestSharedData.Instance.Exception;
                    try
                    {
                        // Run the tests again, but using an error reporting test runner that will report the exception.
                        WpfTestSharedData.Instance.Exception = e;
                        return await RunTestCollectionForUnspecifiedVersionAsync(ctxt.TestAssembly, testCollection, testCases, completedTestCaseIds, ctxt.CancellationTokenSource).ConfigureAwait(true);
                    }
                    finally
                    {
                        WpfTestSharedData.Instance.Exception = previousException;
                    }
                }
            };
        }

        private ImmutableList<string> GetExtensionFiles(IXunitTestAssembly testAssembly)
        {
            var attributes = testAssembly.Assembly.GetCustomAttributes<RequireExtensionAttribute>();
            return attributes.Select(a => a.ExtensionFile).Distinct().ToImmutableList();
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

                case VisualStudioVersion.VS18:
                    return new Version(18, 0);

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

        /*
        private class IpcMessageSink : IMessageSink
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

                    if (_cancellationToken.IsCancellationRequested)
                    {
                        return false;
                    }

                    return _messageSink.OnMessage(message);
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
        */

        private class IpcTestFrameworkOptions(ITestFrameworkOptions executionOptions) : LongLivedMarshalByRefObject, ITestFrameworkOptions
        {
            public TValue? GetValue<TValue>(string name) => executionOptions.GetValue<TValue>(name);

            public void SetValue<TValue>(string name, TValue value) => executionOptions.SetValue(name, value);

            public string ToJson() => executionOptions.ToJson();
        }

        private class IpcTestFrameworkExecutionOptions(ITestFrameworkExecutionOptions executionOptions) : IpcTestFrameworkOptions(executionOptions), ITestFrameworkExecutionOptions
        {
        }

        private class IpcTestFrameworkDiscoveryOptions(ITestFrameworkDiscoveryOptions executionOptions) : IpcTestFrameworkOptions(executionOptions), ITestFrameworkDiscoveryOptions
        {
        }

#pragma warning disable SA1316 // Tuple element names should use correct casing
#pragma warning disable SA1201 // Elements should appear in the correct order
        protected override List<(IXunitTestCollection Collection, List<IXunitTestCase> TestCases)> OrderTestCollections(XunitTestAssemblyRunnerContext ctxt)
#pragma warning restore SA1201 // Elements should appear in the correct order
#pragma warning restore SA1316 // Tuple element names should use correct casing
        {
            var collections = base.OrderTestCollections(ctxt);
            var collectionsWithoutIdeInstanceCases = collections.Where(tuple => !ContainsIdeInstanceCase(tuple.Collection));
            var collectionsWithIdeInstanceCases = collections.Where(tuple => ContainsIdeInstanceCase(tuple.Collection));
            return collectionsWithoutIdeInstanceCases.Concat(collectionsWithIdeInstanceCases).ToList();

            static bool ContainsIdeInstanceCase(IXunitTestCollection collection)
            {
                var assemblyName = collection.TestAssembly.Assembly.GetName();
                return assemblyName.Name == "Microsoft.VisualStudio.Extensibility.Testing.Xunit";
            }
        }
    }
}
