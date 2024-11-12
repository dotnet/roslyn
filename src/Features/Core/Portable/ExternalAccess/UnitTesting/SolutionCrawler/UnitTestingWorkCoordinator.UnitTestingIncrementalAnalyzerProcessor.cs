// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

internal sealed partial class UnitTestingSolutionCrawlerRegistrationService
{
    internal sealed partial class UnitTestingWorkCoordinator
    {
        private sealed partial class UnitTestingIncrementalAnalyzerProcessor
        {
            private static readonly Func<int, object, bool, string> s_enqueueLogger = EnqueueLogger;

            private readonly UnitTestingRegistration _registration;
            private readonly IAsynchronousOperationListener _listener;

            private readonly UnitTestingNormalPriorityProcessor _normalPriorityProcessor;
            private readonly UnitTestingLowPriorityProcessor _lowPriorityProcessor;

            /// <summary>
            /// The keys in this are either a string or a (string, Guid) tuple. See <see cref="UnitTestingSolutionCrawlerLogger.LogIncrementalAnalyzerProcessorStatistics"/>
            /// for what is writing this out.
            /// </summary>
            private CountLogAggregator<object> _logAggregator = new();

            public UnitTestingIncrementalAnalyzerProcessor(
                IAsynchronousOperationListener listener,
                IEnumerable<Lazy<IUnitTestingIncrementalAnalyzerProvider, UnitTestingIncrementalAnalyzerProviderMetadata>> analyzerProviders,
                UnitTestingRegistration registration,
                TimeSpan normalBackOffTimeSpan,
                TimeSpan lowBackOffTimeSpan,
                CancellationToken shutdownToken)
            {
                _listener = listener;
                _registration = registration;

                var analyzersGetter = new UnitTestingAnalyzersGetter(analyzerProviders);

                // create analyzers lazily.
                var lazyAllAnalyzers = new Lazy<ImmutableArray<IUnitTestingIncrementalAnalyzer>>(() => GetIncrementalAnalyzers(_registration, analyzersGetter, onlyHighPriorityAnalyzer: false));

                // event and worker queues
                var globalNotificationService = _registration.Services.ExportProvider.GetExports<IGlobalOperationNotificationService>().FirstOrDefault()?.Value;

                _normalPriorityProcessor = new UnitTestingNormalPriorityProcessor(listener, this, lazyAllAnalyzers, globalNotificationService, normalBackOffTimeSpan, shutdownToken);
                _lowPriorityProcessor = new UnitTestingLowPriorityProcessor(listener, this, lazyAllAnalyzers, globalNotificationService, lowBackOffTimeSpan, shutdownToken);
            }

            private static ImmutableArray<IUnitTestingIncrementalAnalyzer> GetIncrementalAnalyzers(UnitTestingRegistration registration, UnitTestingAnalyzersGetter analyzersGetter, bool onlyHighPriorityAnalyzer)
            {
                var orderedAnalyzers = analyzersGetter.GetOrderedAnalyzers(registration.WorkspaceKind, registration.Services, onlyHighPriorityAnalyzer);

                UnitTestingSolutionCrawlerLogger.LogAnalyzers(registration.CorrelationId, registration.WorkspaceKind, orderedAnalyzers, onlyHighPriorityAnalyzer);
                return orderedAnalyzers;
            }

            public void Enqueue(UnitTestingWorkItem item)
            {
                Contract.ThrowIfNull(item.DocumentId);

                _normalPriorityProcessor.Enqueue(item);
                _lowPriorityProcessor.Enqueue(item);

                ReportPendingWorkItemCount();
            }

            public void AddAnalyzer(
                IUnitTestingIncrementalAnalyzer analyzer)
            {
                _normalPriorityProcessor.AddAnalyzer(analyzer);
                _lowPriorityProcessor.AddAnalyzer(analyzer);
            }

            public void Shutdown()
            {
                _normalPriorityProcessor.Shutdown();
                _lowPriorityProcessor.Shutdown();
            }

            public ImmutableArray<IUnitTestingIncrementalAnalyzer> Analyzers => _normalPriorityProcessor.Analyzers;

            public Task AsyncProcessorTask
            {
                get
                {
                    return Task.WhenAll(
                        _normalPriorityProcessor.AsyncProcessorTask,
                        _lowPriorityProcessor.AsyncProcessorTask);
                }
            }

            private void ResetLogAggregator()
                => _logAggregator = new CountLogAggregator<object>();

            private void ReportPendingWorkItemCount()
            {
                var pendingItemCount =
                    _normalPriorityProcessor.WorkItemCount + _lowPriorityProcessor.WorkItemCount;
                _registration.ProgressReporter.UpdatePendingItemCount(pendingItemCount);
            }

            private async Task ProcessDocumentAnalyzersAsync(
                TextDocument textDocument, ImmutableArray<IUnitTestingIncrementalAnalyzer> analyzers, UnitTestingWorkItem workItem, CancellationToken cancellationToken)
            {
                // process all analyzers for each categories in this order - syntax, body, document
                var reasons = workItem.InvocationReasons;

                if (textDocument is not Document document)
                {
                    // Semantic analysis is not supported for non-source documents.
                    return;
                }

                if (reasons.Contains(UnitTestingPredefinedInvocationReasons.SemanticChanged))
                {
                    await RunAnalyzersAsync(analyzers, document, workItem, (analyzer, document, cancellationToken) =>
                        analyzer.AnalyzeDocumentAsync(document,
                            reasons,
                            cancellationToken), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // if we don't need to re-analyze whole body, see whether we need to at least re-analyze one method.
                    await RunBodyAnalyzersAsync(analyzers, workItem, document, cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            private async Task RunAnalyzersAsync<T>(
                ImmutableArray<IUnitTestingIncrementalAnalyzer> analyzers,
                T value,
                UnitTestingWorkItem workItem,
                Func<IUnitTestingIncrementalAnalyzer, T, CancellationToken, Task> runnerAsync,
                CancellationToken cancellationToken)
            {
                using var evaluating = _registration.ProgressReporter.GetEvaluatingScope();

                ReportPendingWorkItemCount();

                // Check if the work item is specific to some incremental analyzer(s).
                var analyzersToExecute = workItem.GetApplicableAnalyzers(analyzers) ?? analyzers;
                foreach (var analyzer in analyzersToExecute)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var local = analyzer;
                    if (local == null)
                    {
                        return;
                    }

                    await GetOrDefaultAsync(value, async (v, c) =>
                    {
                        await runnerAsync(local, v, c).ConfigureAwait(false);
                        return (object?)null;
                    }, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task RunBodyAnalyzersAsync(ImmutableArray<IUnitTestingIncrementalAnalyzer> analyzers, UnitTestingWorkItem workItem, Document document, CancellationToken cancellationToken)
            {
                try
                {
                    var root = await GetOrDefaultAsync(document, (d, c) => d.GetSyntaxRootAsync(c), cancellationToken).ConfigureAwait(false);
                    var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
                    var reasons = workItem.InvocationReasons;
                    if (root == null || syntaxFactsService == null)
                    {
                        // as a fallback mechanism, if we can't run one method body due to some missing service, run whole document analyzer.
                        await RunAnalyzersAsync(analyzers, document, workItem, (analyzer, document, cancellationToken) =>
                            analyzer.AnalyzeDocumentAsync(
                                document,
                                reasons,
                                cancellationToken), cancellationToken).ConfigureAwait(false);
                        return;
                    }

                    // re-run just the body
                    await RunAnalyzersAsync(analyzers, document, workItem, (analyzer, document, cancellationToken) =>
                        analyzer.AnalyzeDocumentAsync(
                            document,
                            reasons,
                            cancellationToken), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
                {
                    throw ExceptionUtilities.Unreachable();
                }
            }

            private static async Task<TResult?> GetOrDefaultAsync<TData, TResult>(TData value, Func<TData, CancellationToken, Task<TResult?>> funcAsync, CancellationToken cancellationToken)
                where TResult : class
            {
                try
                {
                    return await funcAsync(value, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
                catch (AggregateException e) when (ReportWithoutCrashUnlessAllCanceledAndPropagate(e))
                {
                    return null;
                }
                catch (Exception e) when (FatalError.ReportAndPropagate(e))
                {
                    // TODO: manage bad workers like what code actions does now
                    throw ExceptionUtilities.Unreachable();
                }

                static bool ReportWithoutCrashUnlessAllCanceledAndPropagate(AggregateException aggregate)
                {
                    var flattened = aggregate.Flatten();
                    if (flattened.InnerExceptions.All(e => e is OperationCanceledException))
                    {
                        return true;
                    }

                    return FatalError.ReportAndPropagate(flattened);
                }
            }

            private static string EnqueueLogger(int tick, object documentOrProjectId, bool replaced)
            {
                if (documentOrProjectId is DocumentId documentId)
                {
                    return $"Tick:{tick}, {documentId}, {documentId.ProjectId}, Replaced:{replaced}";
                }

                return $"Tick:{tick}, {documentOrProjectId}, Replaced:{replaced}";
            }

            internal TestAccessor GetTestAccessor()
            {
                return new TestAccessor(this);
            }

            internal readonly struct TestAccessor
            {
                private readonly UnitTestingIncrementalAnalyzerProcessor _incrementalAnalyzerProcessor;

                internal TestAccessor(UnitTestingIncrementalAnalyzerProcessor incrementalAnalyzerProcessor)
                {
                    _incrementalAnalyzerProcessor = incrementalAnalyzerProcessor;
                }

                internal void WaitUntilCompletion(ImmutableArray<IUnitTestingIncrementalAnalyzer> analyzers, List<UnitTestingWorkItem> items)
                {
                    _incrementalAnalyzerProcessor._normalPriorityProcessor.GetTestAccessor().WaitUntilCompletion(analyzers, items);

                    var projectItems = items.Select(i => i.ToProjectWorkItem(EmptyAsyncToken.Instance));
                    _incrementalAnalyzerProcessor._lowPriorityProcessor.GetTestAccessor().WaitUntilCompletion(analyzers, items);
                }

                internal void WaitUntilCompletion()
                {
                    _incrementalAnalyzerProcessor._normalPriorityProcessor.GetTestAccessor().WaitUntilCompletion();
                    _incrementalAnalyzerProcessor._lowPriorityProcessor.GetTestAccessor().WaitUntilCompletion();
                }
            }

            private sealed class UnitTestingAnalyzersGetter(IEnumerable<Lazy<IUnitTestingIncrementalAnalyzerProvider, UnitTestingIncrementalAnalyzerProviderMetadata>> analyzerProviders)
            {
                private readonly List<Lazy<IUnitTestingIncrementalAnalyzerProvider, UnitTestingIncrementalAnalyzerProviderMetadata>> _analyzerProviders = analyzerProviders.ToList();
                private readonly Dictionary<(string workspaceKind, SolutionServices services), ImmutableArray<IUnitTestingIncrementalAnalyzer>> _analyzerMap = [];

                public ImmutableArray<IUnitTestingIncrementalAnalyzer> GetOrderedAnalyzers(string workspaceKind, SolutionServices services, bool onlyHighPriorityAnalyzer)
                {
                    lock (_analyzerMap)
                    {
                        if (!_analyzerMap.TryGetValue((workspaceKind, services), out var analyzers))
                        {
                            analyzers = _analyzerProviders
                                .Select(p => p.Value.CreateIncrementalAnalyzer())
                                .WhereNotNull()
                                .ToImmutableArray();

                            _analyzerMap[(workspaceKind, services)] = analyzers;
                        }

                        if (onlyHighPriorityAnalyzer)
                        {
                            return [];
                        }

                        return analyzers;
                    }
                }
            }
        }
    }
}
