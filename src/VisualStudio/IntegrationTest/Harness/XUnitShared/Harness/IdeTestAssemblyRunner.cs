// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Harness
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Threading;
    using Xunit.Runner.Common;
    using Xunit.Sdk;
    using Xunit.Threading;
    using Xunit.v3;

    internal sealed class IdeTestAssemblyRunner : CoreTestAssemblyRunner<IdeTestAssemblyRunner.Context, IXunitTestAssembly, IXunitTestCollection, IXunitTestCase>
    {
        private static readonly TimeSpan HangMitigatingTimeout = TimeSpan.FromMinutes(1);

        private IdeTestAssemblyRunner()
        {
        }

        public static IdeTestAssemblyRunner Instance { get; } = new();

        public async ValueTask<RunSummary> Run(
            IXunitTestAssembly testAssembly,
            IReadOnlyCollection<IXunitTestCase> testCases,
            IMessageSink executionMessageSink,
            ITestFrameworkExecutionOptions executionOptions,
            CancellationToken cancellationToken)
        {
            await using var context = new Context(testAssembly, testCases, executionMessageSink, executionOptions, cancellationToken);
            await context.InitializeAsync();
            return await Run(context);
        }

        protected override ValueTask<string> GetTestFrameworkDisplayName(Context context)
            => new("xUnit.net v3");

        protected override async ValueTask<RunSummary> RunTestCollections(
            Context context,
            Exception? exception)
        {
            if (exception is not null)
            {
                return await base.RunTestCollections(context, exception);
            }

            var summary = new RunSummary();
            var completedTestCaseIds = new HashSet<string>();

            var nonIdeTestCases = context.TestCases.Where(static testCase => testCase is not IIdeTestCase).ToArray();
            if (nonIdeTestCases.Length != 0)
            {
                summary.Aggregate(await RunLocallyAsync(context, nonIdeTestCases, completedTestCaseIds));
            }

            var ideTestCases = context.TestCases.Where(static testCase => testCase is IIdeTestCase).ToArray();
            var staticallySkippedIdeTestCases = ideTestCases.Where(IsStaticallySkipped).ToArray();
            if (staticallySkippedIdeTestCases.Length != 0)
            {
                summary.Aggregate(await RunLocallyAsync(context, staticallySkippedIdeTestCases, completedTestCaseIds));
            }

            foreach (var testCasesByInstance in ideTestCases
                .Where(static testCase => !IsStaticallySkipped(testCase))
                .Cast<IIdeTestCase>()
                .GroupBy(static testCase => testCase.VisualStudioInstanceKey))
            {
                var instance = testCasesByInstance.Key;
                var currentTests = testCasesByInstance.Cast<IXunitTestCase>().ToArray();

                for (var currentAttempt = 0; currentAttempt < instance.MaxAttempts && currentTests.Length != 0; currentAttempt++)
                {
                    var finalAttempt = currentAttempt == instance.MaxAttempts - 1;
                    try
                    {
                        var attemptSummary = await RunOnStaThreadAsync(
                            context,
                            instance,
                            currentTests,
                            finalAttempt,
                            completedTestCaseIds);
                        summary.Aggregate(attemptSummary);
                    }
                    catch (Exception ex)
                    {
                        DataCollectionService.CaptureFailureState("Unknown", ex);
                        var remainingTests = currentTests.Where(testCase => !completedTestCaseIds.Contains(testCase.UniqueID)).ToArray();
                        summary.Aggregate(await RunHarnessFailuresAsync(context, remainingTests, ex, completedTestCaseIds));
                        break;
                    }

                    currentTests = currentTests.Where(testCase => !completedTestCaseIds.Contains(testCase.UniqueID)).ToArray();
                }
            }

            return summary;
        }

        private static bool IsStaticallySkipped(IXunitTestCase testCase)
            => !string.IsNullOrWhiteSpace(testCase.SkipReason)
                && string.IsNullOrWhiteSpace(testCase.SkipUnless)
                && string.IsNullOrWhiteSpace(testCase.SkipWhen);

        private static async Task<RunSummary> RunOnStaThreadAsync(
            Context context,
            VisualStudioInstanceKey visualStudioInstanceKey,
            IXunitTestCase[] testCases,
            bool finalAttempt,
            HashSet<string> completedTestCaseIds)
        {
            if (visualStudioInstanceKey.Version == VisualStudioVersion.Unspecified
                || !IdeTestCaseBase.IsInstalled(visualStudioInstanceKey.Version))
            {
                return await RunLocallyAsync(context, testCases, completedTestCaseIds);
            }

            DispatcherSynchronizationContext? synchronizationContext = null;
            Dispatcher? dispatcher = null;
            Thread staThread;

            using (var staThreadStartedEvent = new ManualResetEventSlim(initialState: false))
            {
                staThread = new Thread(() =>
                {
                    synchronizationContext = new DispatcherSynchronizationContext();
                    dispatcher = Dispatcher.CurrentDispatcher;
                    SynchronizationContext.SetSynchronizationContext(synchronizationContext);
                    staThreadStartedEvent.Set();
                    Dispatcher.Run();
                })
                {
                    Name = nameof(IdeTestAssemblyRunner),
                };

                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThreadStartedEvent.Wait();
                Debug.Assert(synchronizationContext is not null);
            }

            var scheduler = new SynchronizationContextTaskScheduler(synchronizationContext!);
            var task = Task.Factory.StartNew(
                async () =>
                {
                    using (await WpfTestSharedData.Instance.TestSerializationGate.DisposableWaitAsync(CancellationToken.None).ConfigureAwait(true))
                    {
                        return await RunInVisualStudioAsync(
                            context,
                            visualStudioInstanceKey,
                            testCases,
                            finalAttempt,
                            completedTestCaseIds).ConfigureAwait(true);
                    }
                },
                context.CancellationTokenSource.Token,
                TaskCreationOptions.None,
                scheduler).Unwrap();

            try
            {
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
                return await task.ConfigureAwait(false);
#pragma warning restore VSTHRD003 // Avoid awaiting foreign Tasks
            }
            finally
            {
                dispatcher!.InvokeShutdown();
                staThread.Join(HangMitigatingTimeout);
            }
        }

        private static async Task<RunSummary> RunInVisualStudioAsync(
            Context context,
            VisualStudioInstanceKey visualStudioInstanceKey,
            IXunitTestCase[] testCases,
            bool finalAttempt,
            HashSet<string> completedTestCaseIds)
        {
            Assert.Equal(ApartmentState.STA, Thread.CurrentThread.GetApartmentState());

            var leaveRunning = testCases.All(static testCase => testCase is IdeInstanceTestCase);
            using var marshalledObjects = new MarshalledObjects();
            using var visualStudioInstanceFactory = new VisualStudioInstanceFactory(leaveRunning);
            marshalledObjects.Add(visualStudioInstanceFactory);
            using var messageFilter = new MessageFilter();

            var environmentVariables = ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase).SetItems(
                visualStudioInstanceKey.EnvironmentVariables.Select(
                    variable => variable.IndexOf('=') is var index && index > 0
                        ? new KeyValuePair<string, string>(variable.Substring(0, index), variable.Substring(index + 1))
                        : new KeyValuePair<string, string>(variable, string.Empty)));

            using var visualStudioContext = await visualStudioInstanceFactory.GetNewOrUsedInstanceAsync(
                GetVersion(visualStudioInstanceKey.Version),
                visualStudioInstanceKey.RootSuffix,
                environmentVariables,
                GetExtensionFiles(testCases),
                ImmutableHashSet<string>.Empty).ConfigureAwait(true);

            var messageSink = new BufferedMessageSink(context, testCases, finalAttempt, completedTestCaseIds);
            marshalledObjects.Add(messageSink);
            var request = new TestExecutionRequest(testCases, context.ExecutionOptionsValue);
            TestExecutionResult result;
            var publishMessages = false;
            try
            {
                result = visualStudioContext.Instance.TestInvoker.RunTests(request, messageSink);
                publishMessages = true;
            }
            finally
            {
                messageSink.Flush(publishMessages);
            }

            return messageSink.AdjustRunSummary(new RunSummary
            {
                Total = result.Total,
                Failed = result.Failed,
                Skipped = result.Skipped,
                NotRun = result.NotRun,
                Time = result.Time,
            });
        }

        private static async ValueTask<RunSummary> RunLocallyAsync(
            Context context,
            IReadOnlyCollection<IXunitTestCase> testCases,
            HashSet<string> completedTestCaseIds)
        {
            var messageSink = new BufferedMessageSink(context, testCases, finalAttempt: true, completedTestCaseIds);
            var summary = await XunitTestAssemblyRunner.Instance.Run(
                context.TestAssembly,
                testCases,
                new SerializingMessageSink(messageSink),
                context.ExecutionOptionsValue,
                context.CancellationTokenSource.Token);
            messageSink.Flush();
            return summary;
        }

        private static async ValueTask<RunSummary> RunHarnessFailuresAsync(
            Context context,
            IReadOnlyCollection<IXunitTestCase> testCases,
            Exception exception,
            HashSet<string> completedTestCaseIds)
        {
            var errorTestCases = testCases
                .Select(testCase => (IXunitTestCase)new ExecutionErrorTestCase(
                    testCase.TestMethod,
                    testCase.TestCaseDisplayName,
                    testCase.UniqueID,
                    testCase.SourceFilePath,
                    testCase.SourceLineNumber,
                    $"Test did not run due to a harness failure.{Environment.NewLine}{exception}"))
                .ToArray();

            return errorTestCases.Length == 0
                ? new RunSummary()
                : await RunLocallyAsync(context, errorTestCases, completedTestCaseIds);
        }

        private static ImmutableList<string> GetExtensionFiles(IEnumerable<IXunitTestCase> testCases)
        {
            var extensionFiles = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<System.Reflection.Assembly>();

            foreach (var testCase in testCases)
            {
                var assembly = testCase.TestMethod.TestClass.Class.Assembly;
                if (!visited.Add(assembly))
                {
                    continue;
                }

                var assemblyDirectory = Path.GetDirectoryName(assembly.Location);
                var requiredExtensions = assembly.GetCustomAttributes<RequireExtensionAttribute>()
                    .Select(attribute => Path.IsPathRooted(attribute.ExtensionFile)
                        ? attribute.ExtensionFile
                        : Path.Combine(assemblyDirectory!, attribute.ExtensionFile));
                extensionFiles = extensionFiles.Union(requiredExtensions);
            }

            return extensionFiles.ToImmutableList();
        }

        private static Version GetVersion(VisualStudioVersion visualStudioVersion)
        {
            return visualStudioVersion switch
            {
                VisualStudioVersion.VS2012 => new Version(11, 0),
                VisualStudioVersion.VS2013 => new Version(12, 0),
                VisualStudioVersion.VS2015 => new Version(14, 0),
                VisualStudioVersion.VS2017 => new Version(15, 0),
                VisualStudioVersion.VS2019 => new Version(16, 0),
                VisualStudioVersion.VS2022 => new Version(17, 0),
                VisualStudioVersion.VS18 => new Version(18, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(visualStudioVersion)),
            };
        }

        private sealed class SerializingMessageSink : IMessageSink
        {
            private readonly TestExecutionMessageSink _messageSink;

            public SerializingMessageSink(TestExecutionMessageSink messageSink)
            {
                _messageSink = messageSink;
            }

            public bool OnMessage(IMessageSinkMessage message)
                => _messageSink.OnMessage(message.ToJson() ?? throw new InvalidOperationException("xUnit could not serialize an execution message."));
        }

        private sealed class BufferedMessageSink : TestExecutionMessageSink
        {
            private readonly Context _context;
            private readonly IReadOnlyDictionary<string, IXunitTestCase> _knownTestCasesByUniqueId;
            private readonly bool _finalAttempt;
            private readonly HashSet<string> _completedTestCaseIds;
            private readonly List<string> _messages = new();
            private readonly Dictionary<string, RetrySummaryAdjustment> _retrySummaryAdjustmentsByUniqueId = new(StringComparer.Ordinal);

            public BufferedMessageSink(
                Context context,
                IEnumerable<IXunitTestCase> testCases,
                bool finalAttempt,
                HashSet<string> completedTestCaseIds)
            {
                _context = context;
                var knownTestCasesByUniqueId = new Dictionary<string, IXunitTestCase>(StringComparer.Ordinal);
                foreach (var testCase in testCases)
                {
                    knownTestCasesByUniqueId[testCase.UniqueID] = testCase;
                }

                _knownTestCasesByUniqueId = knownTestCasesByUniqueId;
                _finalAttempt = finalAttempt;
                _completedTestCaseIds = completedTestCaseIds;
            }

            public override bool OnMessage(string message)
            {
                lock (_messages)
                {
                    _messages.Add(message);
                }

                return !_context.CancellationTokenSource.IsCancellationRequested;
            }

            public void Flush(bool publishMessages = true)
            {
                if (!publishMessages)
                {
                    _messages.Clear();
                    return;
                }

                foreach (var serializedMessage in _messages)
                {
                    var message = MessageSinkMessageDeserializer.Deserialize(serializedMessage, diagnosticMessageSink: null)
                        ?? throw new InvalidOperationException("xUnit returned an unrecognized execution message.");

                    if (message is ITestAssemblyStarting or ITestAssemblyFinished)
                    {
                        continue;
                    }

                    if (message is ITestCaseFinished testCaseFinished)
                    {
                        if (_finalAttempt || testCaseFinished.TestsFailed == 0)
                        {
                            _completedTestCaseIds.Add(testCaseFinished.TestCaseUniqueID);
                        }
                        else
                        {
                            _retrySummaryAdjustmentsByUniqueId.Add(
                                testCaseFinished.TestCaseUniqueID,
                                new RetrySummaryAdjustment(testCaseFinished.TestsTotal, testCaseFinished.TestsFailed));

                            var concreteMessage = (Xunit.v3.TestCaseFinished)message;
                            if (_knownTestCasesByUniqueId.TryGetValue(testCaseFinished.TestCaseUniqueID, out var knownTestCase))
                            {
                                concreteMessage.TestCaseUniqueID = knownTestCase.UniqueID;
                                concreteMessage.TestMethodUniqueID = knownTestCase.TestMethod.UniqueID;
                                concreteMessage.TestClassUniqueID = knownTestCase.TestMethod.TestClass.UniqueID;
                                concreteMessage.TestCollectionUniqueID = knownTestCase.TestMethod.TestClass.TestCollection.UniqueID;
                                concreteMessage.AssemblyUniqueID = knownTestCase.TestMethod.TestClass.TestCollection.TestAssembly.UniqueID;
                            }

                            concreteMessage.TestsSkipped += concreteMessage.TestsFailed;
                            concreteMessage.TestsFailed = 0;
                        }
                    }
                    else if (!_finalAttempt && message is ITestMethodFinished testMethodFinished)
                    {
                        var retrySummaryAdjustment = GetRetrySummaryAdjustment(_knownTestCasesByUniqueId.Values.Where(tc => tc.TestMethod.UniqueID == testMethodFinished.TestMethodUniqueID));
                        if (!retrySummaryAdjustment.IsDefault)
                        {
                            var concreteMessage = (Xunit.v3.TestMethodFinished)message;
                            concreteMessage.TestsFailed -= retrySummaryAdjustment.TestsFailed;
                            concreteMessage.TestsSkipped += retrySummaryAdjustment.TestsFailed;
                        }
                    }
                    else if (!_finalAttempt && message is ITestClassFinished testClassFinished)
                    {
                        var retrySummaryAdjustment = GetRetrySummaryAdjustment(_knownTestCasesByUniqueId.Values.Where(tc => tc.TestMethod.TestClass.UniqueID == testClassFinished.TestClassUniqueID));
                        if (!retrySummaryAdjustment.IsDefault)
                        {
                            var concreteMessage = (Xunit.v3.TestClassFinished)message;
                            concreteMessage.TestsFailed -= retrySummaryAdjustment.TestsFailed;
                            concreteMessage.TestsSkipped += retrySummaryAdjustment.TestsFailed;
                        }
                    }
                    else if (!_finalAttempt && message is ITestCollectionFinished testCollectionFinished)
                    {
                        var retrySummaryAdjustment = GetRetrySummaryAdjustment(_knownTestCasesByUniqueId.Values.Where(tc => tc.TestMethod.TestClass.TestCollection.UniqueID == testCollectionFinished.TestCollectionUniqueID));
                        if (!retrySummaryAdjustment.IsDefault)
                        {
                            var concreteMessage = (Xunit.v3.TestCollectionFinished)message;
                            concreteMessage.TestsFailed -= retrySummaryAdjustment.TestsFailed;
                            concreteMessage.TestsSkipped += retrySummaryAdjustment.TestsFailed;
                        }
                    }
                    else if (!_finalAttempt && message is ITestFailed testFailed)
                    {
                        message = new Xunit.v3.TestSkipped
                        {
                            AssemblyUniqueID = testFailed.AssemblyUniqueID,
                            TestCollectionUniqueID = testFailed.TestCollectionUniqueID,
                            TestClassUniqueID = testFailed.TestClassUniqueID,
                            TestMethodUniqueID = testFailed.TestMethodUniqueID,
                            TestCaseUniqueID = testFailed.TestCaseUniqueID,
                            TestUniqueID = testFailed.TestUniqueID,
                            ExecutionTime = testFailed.ExecutionTime,
                            FinishTime = testFailed.FinishTime,
                            Output = testFailed.Output,
                            Warnings = testFailed.Warnings,
                            Reason = "Test will automatically retry.",
                        };
                    }

                    if (!_context.MessageBus.QueueMessage(message))
                    {
                        _context.CancellationTokenSource.Cancel();
                        break;
                    }
                }

                _messages.Clear();
            }

            public RunSummary AdjustRunSummary(RunSummary runSummary)
            {
                if (_retrySummaryAdjustmentsByUniqueId.Count == 0)
                {
                    return runSummary;
                }

                var retriedTestsRun = 0;
                var retriedTestsFailed = 0;
                foreach (var retrySummaryAdjustment in _retrySummaryAdjustmentsByUniqueId.Values)
                {
                    retriedTestsRun += retrySummaryAdjustment.TestsRun;
                    retriedTestsFailed += retrySummaryAdjustment.TestsFailed;
                }

                return new RunSummary
                {
                    Total = runSummary.Total - retriedTestsRun,
                    Failed = runSummary.Failed - retriedTestsFailed,
                    Skipped = runSummary.Skipped,
                    NotRun = runSummary.NotRun,
                    Time = runSummary.Time,
                };
            }

            private IXunitTestCase GetKnownTestCase(IXunitTestCase testCase)
                => _knownTestCasesByUniqueId.TryGetValue(testCase.UniqueID, out var knownTestCase) ? knownTestCase : testCase;

            private IReadOnlyCollection<IXunitTestCase> GetKnownTestCases(IEnumerable<IXunitTestCase>? testCases)
                => testCases is null ? [] : testCases.Select(GetKnownTestCase).ToArray();

            private RetrySummaryAdjustment GetRetrySummaryAdjustment(IEnumerable<IXunitTestCase>? testCases)
            {
                if (testCases is null)
                {
                    return default;
                }

                var testsRun = 0;
                var testsFailed = 0;
                foreach (var testCase in testCases)
                {
                    if (_retrySummaryAdjustmentsByUniqueId.TryGetValue(testCase.UniqueID, out var retrySummaryAdjustment))
                    {
                        testsRun += retrySummaryAdjustment.TestsRun;
                        testsFailed += retrySummaryAdjustment.TestsFailed;
                    }
                }

                return new RetrySummaryAdjustment(testsRun, testsFailed);
            }

            private readonly struct RetrySummaryAdjustment
            {
                public RetrySummaryAdjustment(int testsRun, int testsFailed)
                {
                    TestsRun = testsRun;
                    TestsFailed = testsFailed;
                }

                public int TestsRun { get; }

                public int TestsFailed { get; }

                public bool IsDefault => TestsRun == 0 && TestsFailed == 0;
            }
        }

        internal sealed class Context : XunitTestAssemblyRunnerContext
        {
            public Context(
                IXunitTestAssembly testAssembly,
                IReadOnlyCollection<IXunitTestCase> testCases,
                IMessageSink executionMessageSink,
                ITestFrameworkExecutionOptions executionOptions,
                CancellationToken cancellationToken)
                : base(testAssembly, testCases, executionMessageSink, executionOptions, cancellationToken)
            {
                ExecutionOptionsValue = executionOptions;
            }

            public ITestFrameworkExecutionOptions ExecutionOptionsValue { get; }
        }
    }
}
