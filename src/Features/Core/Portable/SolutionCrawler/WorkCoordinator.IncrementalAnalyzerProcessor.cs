// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService
    {
        internal partial class WorkCoordinator
        {
            private partial class IncrementalAnalyzerProcessor
            {
                private static readonly Func<int, object, bool, string> s_enqueueLogger = EnqueueLogger;

                private readonly Registration _registration;
                private readonly IAsynchronousOperationListener _listener;
                private readonly IDocumentTrackingService _documentTracker;

                private readonly HighPriorityProcessor _highPriorityProcessor;
                private readonly NormalPriorityProcessor _normalPriorityProcessor;
                private readonly LowPriorityProcessor _lowPriorityProcessor;

                /// <summary>
                /// The keys in this are either a string or a (string, Guid) tuple. See <see cref="SolutionCrawlerLogger.LogIncrementalAnalyzerProcessorStatistics"/>
                /// for what is writing this out.
                /// </summary>
                private CountLogAggregator<object> _logAggregator = new();

                public IncrementalAnalyzerProcessor(
                    IAsynchronousOperationListener listener,
                    IEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> analyzerProviders,
                    bool initializeLazily,
                    Registration registration,
                    TimeSpan highBackOffTimeSpan,
                    TimeSpan normalBackOffTimeSpan,
                    TimeSpan lowBackOffTimeSpan,
                    CancellationToken shutdownToken)
                {
                    _listener = listener;
                    _registration = registration;

                    var analyzersGetter = new AnalyzersGetter(analyzerProviders);

                    // create analyzers lazily.
                    var lazyActiveFileAnalyzers = new Lazy<ImmutableArray<IIncrementalAnalyzer>>(() => GetIncrementalAnalyzers(_registration, analyzersGetter, onlyHighPriorityAnalyzer: true));
                    var lazyAllAnalyzers = new Lazy<ImmutableArray<IIncrementalAnalyzer>>(() => GetIncrementalAnalyzers(_registration, analyzersGetter, onlyHighPriorityAnalyzer: false));

                    if (!initializeLazily)
                    {
                        // realize all analyzer right away
                        _ = lazyActiveFileAnalyzers.Value;
                        _ = lazyAllAnalyzers.Value;
                    }

                    // event and worker queues
                    _documentTracker = _registration.Workspace.Services.GetRequiredService<IDocumentTrackingService>();

                    var globalNotificationService = _registration.Workspace.Services.SolutionServices.ExportProvider
                        .GetExports<IGlobalOperationNotificationService>().FirstOrDefault()?.Value;

                    _highPriorityProcessor = new HighPriorityProcessor(listener, this, lazyActiveFileAnalyzers, highBackOffTimeSpan, shutdownToken);
                    _normalPriorityProcessor = new NormalPriorityProcessor(listener, this, lazyAllAnalyzers, globalNotificationService, normalBackOffTimeSpan, shutdownToken);
                    _lowPriorityProcessor = new LowPriorityProcessor(listener, this, lazyAllAnalyzers, globalNotificationService, lowBackOffTimeSpan, shutdownToken);
                }

                private static ImmutableArray<IIncrementalAnalyzer> GetIncrementalAnalyzers(Registration registration, AnalyzersGetter analyzersGetter, bool onlyHighPriorityAnalyzer)
                {
                    var orderedAnalyzers = analyzersGetter.GetOrderedAnalyzers(registration.Workspace, onlyHighPriorityAnalyzer);

                    SolutionCrawlerLogger.LogAnalyzers(registration.CorrelationId, registration.Workspace, orderedAnalyzers, onlyHighPriorityAnalyzer);
                    return orderedAnalyzers;
                }

                public void Enqueue(WorkItem item)
                {
                    Contract.ThrowIfNull(item.DocumentId);

                    _highPriorityProcessor.Enqueue(item);
                    _normalPriorityProcessor.Enqueue(item);
                    _lowPriorityProcessor.Enqueue(item);

                    ReportPendingWorkItemCount();
                }

                public void AddAnalyzer(IIncrementalAnalyzer analyzer, bool highPriorityForActiveFile)
                {
                    if (highPriorityForActiveFile)
                    {
                        _highPriorityProcessor.AddAnalyzer(analyzer);
                    }

                    _normalPriorityProcessor.AddAnalyzer(analyzer);
                    _lowPriorityProcessor.AddAnalyzer(analyzer);
                }

                public void Shutdown()
                {
                    _highPriorityProcessor.Shutdown();
                    _normalPriorityProcessor.Shutdown();
                    _lowPriorityProcessor.Shutdown();
                }

                public ImmutableArray<IIncrementalAnalyzer> Analyzers => _normalPriorityProcessor.Analyzers;

                private ProjectDependencyGraph DependencyGraph => _registration.GetSolutionToAnalyze().GetProjectDependencyGraph();

                public Task AsyncProcessorTask
                {
                    get
                    {
                        return Task.WhenAll(
                            _highPriorityProcessor.AsyncProcessorTask,
                            _normalPriorityProcessor.AsyncProcessorTask,
                            _lowPriorityProcessor.AsyncProcessorTask);
                    }
                }

                private IEnumerable<DocumentId> GetOpenDocumentIds()
                    => _registration.Workspace.GetOpenDocumentIds();

                private void ResetLogAggregator()
                    => _logAggregator = new CountLogAggregator<object>();

                private void ReportPendingWorkItemCount()
                {
                    var pendingItemCount = _highPriorityProcessor.WorkItemCount + _normalPriorityProcessor.WorkItemCount + _lowPriorityProcessor.WorkItemCount;
                    _registration.ProgressReporter.UpdatePendingItemCount(pendingItemCount);
                }

                private async Task ProcessDocumentAnalyzersAsync(
                    TextDocument textDocument, ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, CancellationToken cancellationToken)
                {
                    // process special active document switched request, if any.
                    if (await ProcessActiveDocumentSwitchedAsync(analyzers, workItem, textDocument, cancellationToken).ConfigureAwait(false))
                    {
                        return;
                    }

                    // process all analyzers for each categories in this order - syntax, body, document
                    var reasons = workItem.InvocationReasons;
                    if (workItem.MustRefresh || reasons.Contains(PredefinedInvocationReasons.SyntaxChanged))
                    {
                        await RunAnalyzersAsync(analyzers, textDocument, workItem, (analyzer, document, cancellationToken) =>
                            AnalyzeSyntaxAsync(analyzer, document, reasons, cancellationToken), cancellationToken).ConfigureAwait(false);
                    }

                    if (textDocument is not Document document)
                    {
                        // Semantic analysis is not supported for non-source documents.
                        return;
                    }

                    if (workItem.MustRefresh || reasons.Contains(PredefinedInvocationReasons.SemanticChanged))
                    {
                        await RunAnalyzersAsync(analyzers, document, workItem, (analyzer, document, cancellationToken) =>
                            analyzer.AnalyzeDocumentAsync(document, null, reasons, cancellationToken), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // if we don't need to re-analyze whole body, see whether we need to at least re-analyze one method.
                        await RunBodyAnalyzersAsync(analyzers, workItem, document, cancellationToken).ConfigureAwait(false);
                    }

                    return;

                    static async Task AnalyzeSyntaxAsync(IIncrementalAnalyzer analyzer, TextDocument textDocument, InvocationReasons reasons, CancellationToken cancellationToken)
                    {
                        if (textDocument is Document document)
                        {
                            await analyzer.AnalyzeSyntaxAsync(document, reasons, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await analyzer.AnalyzeNonSourceDocumentAsync(textDocument, reasons, cancellationToken).ConfigureAwait(false);
                        }
                    }

                    async Task<bool> ProcessActiveDocumentSwitchedAsync(ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, TextDocument document, CancellationToken cancellationToken)
                    {
                        try
                        {
                            if (!workItem.InvocationReasons.Contains(PredefinedInvocationReasons.ActiveDocumentSwitched))
                            {
                                return false;
                            }

                            await RunAnalyzersAsync(analyzers, document, workItem, (analyzer, document, cancellationToken) =>
                                analyzer.ActiveDocumentSwitchedAsync(document, cancellationToken), cancellationToken).ConfigureAwait(false);
                            return true;
                        }
                        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
                        {
                            throw ExceptionUtilities.Unreachable();
                        }
                    }
                }

                private async Task RunAnalyzersAsync<T>(
                    ImmutableArray<IIncrementalAnalyzer> analyzers,
                    T value,
                    WorkItem workItem,
                    Func<IIncrementalAnalyzer, T, CancellationToken, Task> runnerAsync,
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

                private async Task RunBodyAnalyzersAsync(ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, Document document, CancellationToken cancellationToken)
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
                                analyzer.AnalyzeDocumentAsync(document, null, reasons, cancellationToken), cancellationToken).ConfigureAwait(false);
                            return;
                        }

                        // check whether we know what body has changed. currently, this is an optimization toward typing case. if there are more than one body changes
                        // it will be considered as semantic change and whole document analyzer will take care of that case.
                        var activeMember = GetMemberNode(syntaxFactsService, root, workItem.ActiveMember);
                        if (activeMember == null)
                        {
                            // no active member means, change is out side of a method body, but it didn't affect semantics (such as change in comment)
                            // in that case, we update whole document (just this document) so that we can have updated locations.
                            await RunAnalyzersAsync(analyzers, document, workItem, (analyzer, document, cancellationToken) =>
                                analyzer.AnalyzeDocumentAsync(document, null, reasons, cancellationToken), cancellationToken).ConfigureAwait(false);
                            return;
                        }

                        // re-run just the body
                        await RunAnalyzersAsync(analyzers, document, workItem, (analyzer, document, cancellationToken) =>
                            analyzer.AnalyzeDocumentAsync(document, activeMember, reasons, cancellationToken), cancellationToken).ConfigureAwait(false);
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

                private static SyntaxNode? GetMemberNode(ISyntaxFactsService service, SyntaxNode? root, SyntaxPath? memberPath)
                {
                    if (root == null || memberPath == null)
                    {
                        return null;
                    }

                    if (!memberPath.TryResolve(root, out SyntaxNode? memberNode))
                    {
                        return null;
                    }

                    return service.IsMethodLevelMember(memberNode) ? memberNode : null;
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
                    private readonly IncrementalAnalyzerProcessor _incrementalAnalyzerProcessor;

                    internal TestAccessor(IncrementalAnalyzerProcessor incrementalAnalyzerProcessor)
                    {
                        _incrementalAnalyzerProcessor = incrementalAnalyzerProcessor;
                    }

                    internal void WaitUntilCompletion(ImmutableArray<IIncrementalAnalyzer> analyzers, List<WorkItem> items)
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

                private class AnalyzersGetter(IEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> analyzerProviders)
                {
                    private readonly List<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> _analyzerProviders = analyzerProviders.ToList();
                    private readonly Dictionary<Workspace, ImmutableArray<(IIncrementalAnalyzer analyzer, bool highPriorityForActiveFile)>> _analyzerMap = new();

                    public ImmutableArray<IIncrementalAnalyzer> GetOrderedAnalyzers(Workspace workspace, bool onlyHighPriorityAnalyzer)
                    {
                        lock (_analyzerMap)
                        {
                            if (!_analyzerMap.TryGetValue(workspace, out var analyzers))
                            {
                                // Sort list so DiagnosticIncrementalAnalyzers (if any) come first.
                                analyzers = _analyzerProviders.Select(p => (analyzer: p.Value.CreateIncrementalAnalyzer(workspace), highPriorityForActiveFile: p.Metadata.HighPriorityForActiveFile))
                                                .Where(t => t.analyzer != null)
                                                .OrderBy(t => t.analyzer!.Priority)
                                                .ToImmutableArray()!;

                                _analyzerMap[workspace] = analyzers;
                            }

                            if (onlyHighPriorityAnalyzer)
                            {
                                // include only high priority analyzer for active file
                                return analyzers.SelectAsArray(t => t.highPriorityForActiveFile, t => t.analyzer);
                            }

                            // return all analyzers
                            return analyzers.Select(t => t.analyzer).ToImmutableArray();
                        }
                    }
                }
            }
        }
    }
}
