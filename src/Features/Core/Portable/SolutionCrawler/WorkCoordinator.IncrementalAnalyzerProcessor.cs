﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal partial class SolutionCrawlerRegistrationService
    {
        private partial class WorkCoordinator
        {
            private partial class IncrementalAnalyzerProcessor
            {
                private static readonly Func<int, object, bool, string> s_enqueueLogger = EnqueueLogger;

                private readonly Registration _registration;
                private readonly IAsynchronousOperationListener _listener;
                private readonly IDocumentTrackingService? _documentTracker;
                private readonly IProjectCacheService? _cacheService;

                private readonly HighPriorityProcessor _highPriorityProcessor;
                private readonly NormalPriorityProcessor _normalPriorityProcessor;
                private readonly LowPriorityProcessor _lowPriorityProcessor;

                // NOTE: IDiagnosticAnalyzerService can be null in test environment.
                private readonly Lazy<IDiagnosticAnalyzerService?> _lazyDiagnosticAnalyzerService;

                private LogAggregator _logAggregator;

                public IncrementalAnalyzerProcessor(
                    IAsynchronousOperationListener listener,
                    IEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> analyzerProviders,
                    bool initializeLazily,
                    Registration registration,
                    int highBackOffTimeSpanInMs,
                    int normalBackOffTimeSpanInMs,
                    int lowBackOffTimeSpanInMs,
                    CancellationToken shutdownToken)
                {
                    _logAggregator = new LogAggregator();

                    _listener = listener;
                    _registration = registration;
                    _cacheService = registration.Workspace.Services.GetService<IProjectCacheService>();

                    _lazyDiagnosticAnalyzerService = new Lazy<IDiagnosticAnalyzerService?>(() => GetDiagnosticAnalyzerService(analyzerProviders));

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
                    _documentTracker = _registration.Workspace.Services.GetService<IDocumentTrackingService>();

                    var globalNotificationService = _registration.Workspace.Services.GetRequiredService<IGlobalOperationNotificationService>();

                    _highPriorityProcessor = new HighPriorityProcessor(listener, this, lazyActiveFileAnalyzers, highBackOffTimeSpanInMs, shutdownToken);
                    _normalPriorityProcessor = new NormalPriorityProcessor(listener, this, lazyAllAnalyzers, globalNotificationService, normalBackOffTimeSpanInMs, shutdownToken);
                    _lowPriorityProcessor = new LowPriorityProcessor(listener, this, lazyAllAnalyzers, globalNotificationService, lowBackOffTimeSpanInMs, shutdownToken);
                }

                private static IDiagnosticAnalyzerService? GetDiagnosticAnalyzerService(IEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> analyzerProviders)
                {
                    // alternatively, we could just MEF import IDiagnosticAnalyzerService directly
                    // this can be null in test env.
                    return (IDiagnosticAnalyzerService?)analyzerProviders.Where(p => p.Value is IDiagnosticAnalyzerService).SingleOrDefault()?.Value;
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

                    var options = _registration.Workspace.Options;
                    var analysisScope = SolutionCrawlerOptions.GetBackgroundAnalysisScope(options, item.Language);

                    if (ShouldEnqueueForAllQueues(item, analysisScope))
                    {
                        _highPriorityProcessor.Enqueue(item);
                        _normalPriorityProcessor.Enqueue(item);
                        _lowPriorityProcessor.Enqueue(item);
                    }
                    else
                    {
                        if (TryGetItemWithOverriddenAnalysisScope(item, _highPriorityProcessor.Analyzers, options, analysisScope, _listener, out var newWorkItem))
                        {
                            _highPriorityProcessor.Enqueue(newWorkItem.Value);
                        }

                        if (TryGetItemWithOverriddenAnalysisScope(item, _normalPriorityProcessor.Analyzers, options, analysisScope, _listener, out newWorkItem))
                        {
                            _normalPriorityProcessor.Enqueue(newWorkItem.Value);
                        }

                        if (TryGetItemWithOverriddenAnalysisScope(item, _lowPriorityProcessor.Analyzers, options, analysisScope, _listener, out newWorkItem))
                        {
                            _lowPriorityProcessor.Enqueue(newWorkItem.Value);
                        }

                        item.AsyncToken.Dispose();
                    }

                    ReportPendingWorkItemCount();

                    return;

                    bool ShouldEnqueueForAllQueues(WorkItem item, BackgroundAnalysisScope analysisScope)
                    {
                        var reasons = item.InvocationReasons;

                        // For active file analysis scope we only process following:
                        //   1. Active documents
                        //   2. Closed and removed documents to ensure that data for removed and closed documents
                        //      is no longer held in memory and removed from any user visible components.
                        //      For example, this ensures that diagnostics for closed/removed documents are removed from error list.
                        // Note that we don't need to specially handle "Project removed" or "Project closed" case, as the solution crawler
                        // enqueues individual "DocumentRemoved" work items for each document in the removed project.
                        if (analysisScope == BackgroundAnalysisScope.ActiveFile &&
                            !reasons.Contains(PredefinedInvocationReasons.DocumentClosed) &&
                            !reasons.Contains(PredefinedInvocationReasons.DocumentRemoved))
                        {
                            return item.DocumentId == _documentTracker?.TryGetActiveDocument();
                        }

                        return true;
                    }
                }

                private static bool TryGetItemWithOverriddenAnalysisScope(
                    WorkItem item,
                    ImmutableArray<IIncrementalAnalyzer> allAnalyzers,
                    OptionSet options,
                    BackgroundAnalysisScope analysisScope,
                    IAsynchronousOperationListener listener,
                    [NotNullWhen(returnValue: true)] out WorkItem? newWorkItem)
                {
                    var analyzersToExecute = item.GetApplicableAnalyzers(allAnalyzers);

                    var analyzersWithOverriddenAnalysisScope = analyzersToExecute
                        .Where(a => a.GetOverriddenBackgroundAnalysisScope(options, analysisScope) != analysisScope)
                        .ToImmutableHashSet();

                    if (!analyzersWithOverriddenAnalysisScope.IsEmpty)
                    {
                        newWorkItem = item.With(analyzersWithOverriddenAnalysisScope, listener.BeginAsyncOperation("WorkItem"));
                        return true;
                    }

                    newWorkItem = null;
                    return false;
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

                private Solution CurrentSolution => _registration.CurrentSolution;
                private ProjectDependencyGraph DependencyGraph => CurrentSolution.GetProjectDependencyGraph();
                private IDiagnosticAnalyzerService? DiagnosticAnalyzerService => _lazyDiagnosticAnalyzerService?.Value;

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

                private IDisposable EnableCaching(ProjectId projectId)
                    => _cacheService?.EnableCaching(projectId) ?? NullDisposable.Instance;

                private IEnumerable<DocumentId> GetOpenDocumentIds()
                    => _registration.Workspace.GetOpenDocumentIds();

                private void ResetLogAggregator()
                    => _logAggregator = new LogAggregator();

                private void ReportPendingWorkItemCount()
                {
                    var pendingItemCount = _highPriorityProcessor.WorkItemCount + _normalPriorityProcessor.WorkItemCount + _lowPriorityProcessor.WorkItemCount;
                    _registration.ProgressReporter.UpdatePendingItemCount(pendingItemCount);
                }

                private async Task ProcessDocumentAnalyzersAsync(
                    Document document, ImmutableArray<IIncrementalAnalyzer> analyzers, WorkItem workItem, CancellationToken cancellationToken)
                {
                    // process all analyzers for each categories in this order - syntax, body, document
                    var reasons = workItem.InvocationReasons;
                    if (workItem.MustRefresh || reasons.Contains(PredefinedInvocationReasons.SyntaxChanged))
                    {
                        await RunAnalyzersAsync(analyzers, document, workItem, (a, d, c) => a.AnalyzeSyntaxAsync(d, reasons, c), cancellationToken).ConfigureAwait(false);
                    }

                    if (workItem.MustRefresh || reasons.Contains(PredefinedInvocationReasons.SemanticChanged))
                    {
                        await RunAnalyzersAsync(analyzers, document, workItem, (a, d, c) => a.AnalyzeDocumentAsync(d, null, reasons, c), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // if we don't need to re-analyze whole body, see whether we need to at least re-analyze one method.
                        await RunBodyAnalyzersAsync(analyzers, workItem, document, cancellationToken).ConfigureAwait(false);
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
                            await RunAnalyzersAsync(analyzers, document, workItem, (a, d, c) => a.AnalyzeDocumentAsync(d, null, reasons, c), cancellationToken).ConfigureAwait(false);
                            return;
                        }

                        // check whether we know what body has changed. currently, this is an optimization toward typing case. if there are more than one body changes
                        // it will be considered as semantic change and whole document analyzer will take care of that case.
                        var activeMember = GetMemberNode(syntaxFactsService, root, workItem.ActiveMember);
                        if (activeMember == null)
                        {
                            // no active member means, change is out side of a method body, but it didn't affect semantics (such as change in comment)
                            // in that case, we update whole document (just this document) so that we can have updated locations.
                            await RunAnalyzersAsync(analyzers, document, workItem, (a, d, c) => a.AnalyzeDocumentAsync(d, null, reasons, c), cancellationToken).ConfigureAwait(false);
                            return;
                        }

                        // re-run just the body
                        await RunAnalyzersAsync(analyzers, document, workItem, (a, d, c) => a.AnalyzeDocumentAsync(d, activeMember, reasons, c), cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                    {
                        throw ExceptionUtilities.Unreachable;
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
                    catch (AggregateException e) when (CrashUnlessCanceled(e))
                    {
                        return null;
                    }
                    catch (Exception e) when (FatalError.Report(e))
                    {
                        // TODO: manage bad workers like what code actions does now
                        throw ExceptionUtilities.Unreachable;
                    }
                }

                private static SyntaxNode? GetMemberNode(ISyntaxFactsService service, SyntaxNode? root, SyntaxPath? memberPath)
                {
                    if (root == null || memberPath == null)
                    {
                        return null;
                    }

                    if (!memberPath.TryResolve(root, out SyntaxNode memberNode))
                    {
                        return null;
                    }

                    return service.IsMethodLevelMember(memberNode) ? memberNode : null;
                }

                internal ProjectId? GetActiveProjectId()
                    => _documentTracker?.TryGetActiveDocument()?.ProjectId;

                private static string EnqueueLogger(int tick, object documentOrProjectId, bool replaced)
                {
                    if (documentOrProjectId is DocumentId documentId)
                    {
                        return $"Tick:{tick}, {documentId}, {documentId.ProjectId}, Replaced:{replaced}";
                    }

                    return $"Tick:{tick}, {documentOrProjectId}, Replaced:{replaced}";
                }

                private static bool CrashUnlessCanceled(AggregateException aggregate)
                {
                    var flattened = aggregate.Flatten();
                    if (flattened.InnerExceptions.All(e => e is OperationCanceledException))
                    {
                        return true;
                    }

                    FatalError.Report(flattened);
                    return false;
                }

                internal void WaitUntilCompletion_ForTestingPurposesOnly(ImmutableArray<IIncrementalAnalyzer> analyzers, List<WorkItem> items)
                {
                    _normalPriorityProcessor.WaitUntilCompletion_ForTestingPurposesOnly(analyzers, items);

                    var projectItems = items.Select(i => i.ToProjectWorkItem(EmptyAsyncToken.Instance));
                    _lowPriorityProcessor.WaitUntilCompletion_ForTestingPurposesOnly(analyzers, items);
                }

                internal void WaitUntilCompletion_ForTestingPurposesOnly()
                {
                    _normalPriorityProcessor.WaitUntilCompletion_ForTestingPurposesOnly();
                    _lowPriorityProcessor.WaitUntilCompletion_ForTestingPurposesOnly();
                }

                private class NullDisposable : IDisposable
                {
                    public static readonly IDisposable Instance = new NullDisposable();

                    public void Dispose() { }
                }

                private class AnalyzersGetter
                {
                    private readonly List<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> _analyzerProviders;
                    private readonly Dictionary<Workspace, ImmutableArray<ValueTuple<IIncrementalAnalyzer, bool>>> _analyzerMap;

                    public AnalyzersGetter(IEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>> analyzerProviders)
                    {
                        _analyzerMap = new Dictionary<Workspace, ImmutableArray<ValueTuple<IIncrementalAnalyzer, bool>>>();
                        _analyzerProviders = analyzerProviders.ToList();
                    }

                    public ImmutableArray<IIncrementalAnalyzer> GetOrderedAnalyzers(Workspace workspace, bool onlyHighPriorityAnalyzer)
                    {
                        lock (_analyzerMap)
                        {
                            if (!_analyzerMap.TryGetValue(workspace, out var analyzers))
                            {
                                // Sort list so DiagnosticIncrementalAnalyzers (if any) come first.  OrderBy orders 'false' keys before 'true'.
                                analyzers = _analyzerProviders.Select(p => ValueTuple.Create(p.Value.CreateIncrementalAnalyzer(workspace), p.Metadata.HighPriorityForActiveFile))
                                                .Where(t => t.Item1 != null)
                                                .OrderBy(t => !(t.Item1 is DiagnosticIncrementalAnalyzer))
                                                .ToImmutableArray();

                                _analyzerMap[workspace] = analyzers;
                            }

                            if (onlyHighPriorityAnalyzer)
                            {
                                // include only high priority analyzer for active file
                                return analyzers.Where(t => t.Item2).Select(t => t.Item1).ToImmutableArray();
                            }

                            // return all analyzers
                            return analyzers.Select(t => t.Item1).ToImmutableArray();
                        }
                    }
                }
            }
        }
    }
}
