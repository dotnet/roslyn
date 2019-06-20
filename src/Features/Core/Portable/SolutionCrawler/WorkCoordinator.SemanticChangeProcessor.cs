// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal sealed partial class SolutionCrawlerRegistrationService
    {
        /// <summary>
        /// this will be used in the unit test to indicate certain action has happened or not.
        /// </summary>
        public const string EnqueueItem = nameof(EnqueueItem);

        private sealed partial class WorkCoordinator
        {
            private sealed class SemanticChangeProcessor : IdleProcessor
            {
                private static readonly Func<int, DocumentId, bool, string> s_enqueueLogger = (tick, documentId, hint) => $"Tick:{tick}, {documentId}, {documentId.ProjectId}, hint:{hint}";

                private readonly SemaphoreSlim _gate;

                private readonly Registration _registration;
                private readonly ProjectProcessor _processor;

                private readonly NonReentrantLock _workGate;
                private readonly Dictionary<DocumentId, Data> _pendingWork;

                public SemanticChangeProcessor(
                    IAsynchronousOperationListener listener,
                    Registration registration,
                    IncrementalAnalyzerProcessor documentWorkerProcessor,
                    int backOffTimeSpanInMS,
                    int projectBackOffTimeSpanInMS,
                    CancellationToken cancellationToken) :
                    base(listener, backOffTimeSpanInMS, cancellationToken)
                {
                    _gate = new SemaphoreSlim(initialCount: 0);

                    _registration = registration;

                    _processor = new ProjectProcessor(listener, registration, documentWorkerProcessor, projectBackOffTimeSpanInMS, cancellationToken);

                    _workGate = new NonReentrantLock();
                    _pendingWork = new Dictionary<DocumentId, Data>();

                    Start();

                    // Register a clean-up task to ensure pending work items are flushed from the queue if they will
                    // never be processed.
                    AsyncProcessorTask.ContinueWith(
                        _ => ClearQueueWorker(_workGate, _pendingWork, data => data.AsyncToken),
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }

                public override Task AsyncProcessorTask
                {
                    get
                    {
                        return Task.WhenAll(base.AsyncProcessorTask, _processor.AsyncProcessorTask);
                    }
                }

                protected override Task WaitAsync(CancellationToken cancellationToken)
                {
                    return _gate.WaitAsync(cancellationToken);
                }

                protected override async Task ExecuteAsync()
                {
                    var data = Dequeue();

                    using (data.AsyncToken)
                    {
                        // we have a hint. check whether we can take advantage of it
                        if (await TryEnqueueFromHint(data.Document, data.ChangedMember).ConfigureAwait(continueOnCapturedContext: false))
                        {
                            return;
                        }

                        EnqueueFullProjectDependency(data.Document);
                    }
                }

                private Data Dequeue()
                {
                    return DequeueWorker(_workGate, _pendingWork, CancellationToken);
                }

                private async Task<bool> TryEnqueueFromHint(Document document, SyntaxPath changedMember)
                {
                    if (changedMember == null)
                    {
                        return false;
                    }
                    // see whether we already have semantic model. otherwise, use the expansive full project dependency one
                    // TODO: if there is a reliable way to track changed member, we could use GetSemanticModel here which could
                    //       rebuild compilation from scratch
                    if (!document.TryGetSemanticModel(out var model) ||
                        !changedMember.TryResolve(await document.GetSyntaxRootAsync(CancellationToken).ConfigureAwait(false), out SyntaxNode declarationNode))
                    {
                        return false;
                    }

                    var symbol = model.GetDeclaredSymbol(declarationNode, CancellationToken);
                    if (symbol == null)
                    {
                        return false;
                    }

                    return await TryEnqueueFromMemberAsync(document, symbol).ConfigureAwait(false) ||
                        await TryEnqueueFromTypeAsync(document, symbol).ConfigureAwait(false);
                }

                private async Task<bool> TryEnqueueFromTypeAsync(Document document, ISymbol symbol)
                {
                    if (!IsType(symbol))
                    {
                        return false;
                    }

                    if (symbol.DeclaredAccessibility == Accessibility.Private)
                    {
                        await EnqueueWorkItemAsync(document, symbol).ConfigureAwait(false);

                        Logger.Log(FunctionId.WorkCoordinator_SemanticChange_EnqueueFromType, symbol.Name);
                        return true;
                    }

                    if (IsInternal(symbol))
                    {
                        var assembly = symbol.ContainingAssembly;
                        EnqueueFullProjectDependency(document, assembly);
                        return true;
                    }

                    return false;
                }

                private async Task<bool> TryEnqueueFromMemberAsync(Document document, ISymbol symbol)
                {
                    if (!IsMember(symbol))
                    {
                        return false;
                    }

                    var typeSymbol = symbol.ContainingType;

                    if (symbol.DeclaredAccessibility == Accessibility.Private)
                    {
                        await EnqueueWorkItemAsync(document, symbol).ConfigureAwait(false);

                        Logger.Log(FunctionId.WorkCoordinator_SemanticChange_EnqueueFromMember, symbol.Name);
                        return true;
                    }

                    if (typeSymbol == null)
                    {
                        return false;
                    }

                    return await TryEnqueueFromTypeAsync(document, typeSymbol).ConfigureAwait(false);
                }

                private Task EnqueueWorkItemAsync(Document document, ISymbol symbol)
                {
                    return EnqueueWorkItemAsync(document, symbol.ContainingType != null ? symbol.ContainingType.Locations : symbol.Locations);
                }

                private async Task EnqueueWorkItemAsync(Document thisDocument, ImmutableArray<Location> locations)
                {
                    var solution = thisDocument.Project.Solution;
                    var projectId = thisDocument.Id.ProjectId;

                    foreach (var location in locations)
                    {
                        Debug.Assert(location.IsInSource);

                        var document = solution.GetDocument(location.SourceTree, projectId);
                        if (document == null || thisDocument == document)
                        {
                            continue;
                        }

                        await _processor.EnqueueWorkItemAsync(document).ConfigureAwait(false);
                    }
                }

                private bool IsInternal(ISymbol symbol)
                {
                    return symbol.DeclaredAccessibility == Accessibility.Internal ||
                           symbol.DeclaredAccessibility == Accessibility.ProtectedAndInternal ||
                           symbol.DeclaredAccessibility == Accessibility.ProtectedOrInternal;
                }

                private bool IsType(ISymbol symbol)
                {
                    return symbol.Kind == SymbolKind.NamedType;
                }

                private bool IsMember(ISymbol symbol)
                {
                    return symbol.Kind == SymbolKind.Event ||
                           symbol.Kind == SymbolKind.Field ||
                           symbol.Kind == SymbolKind.Method ||
                           symbol.Kind == SymbolKind.Property;
                }

                private void EnqueueFullProjectDependency(Document document, IAssemblySymbol internalVisibleToAssembly = null)
                {
                    var self = document.Project.Id;

                    // if there is no hint (this can happen for cases such as solution/project load and etc), 
                    // we can postpone it even further
                    if (internalVisibleToAssembly == null)
                    {
                        _processor.Enqueue(self, needDependencyTracking: true);
                        return;
                    }

                    // most likely we got here since we are called due to typing.
                    // calculate dependency here and register each affected project to the next pipe line
                    var solution = document.Project.Solution;
                    foreach (var projectId in GetProjectsToAnalyze(solution, self))
                    {
                        var project = solution.GetProject(projectId);
                        if (project == null)
                        {
                            continue;
                        }

                        if (project.TryGetCompilation(out var compilation))
                        {
                            var assembly = compilation.Assembly;
                            if (assembly != null && !assembly.IsSameAssemblyOrHasFriendAccessTo(internalVisibleToAssembly))
                            {
                                continue;
                            }
                        }

                        _processor.Enqueue(projectId);
                    }

                    Logger.Log(FunctionId.WorkCoordinator_SemanticChange_FullProjects, internalVisibleToAssembly == null ? "full" : "internals");
                }

                public void Enqueue(Document document, SyntaxPath changedMember)
                {
                    UpdateLastAccessTime();

                    using (_workGate.DisposableWait(CancellationToken))
                    {
                        if (_pendingWork.TryGetValue(document.Id, out var data))
                        {
                            // create new async token and dispose old one.
                            var newAsyncToken = Listener.BeginAsyncOperation(nameof(Enqueue), tag: _registration.Workspace);
                            data.AsyncToken.Dispose();

                            _pendingWork[document.Id] = new Data(document, data.ChangedMember == changedMember ? changedMember : null, newAsyncToken);
                            return;
                        }

                        _pendingWork.Add(document.Id, new Data(document, changedMember, Listener.BeginAsyncOperation(nameof(Enqueue), tag: _registration.Workspace)));
                        _gate.Release();
                    }

                    Logger.Log(FunctionId.WorkCoordinator_SemanticChange_Enqueue, s_enqueueLogger, Environment.TickCount, document.Id, changedMember != null);
                }

                private static TValue DequeueWorker<TKey, TValue>(NonReentrantLock gate, Dictionary<TKey, TValue> map, CancellationToken cancellationToken)
                {
                    using (gate.DisposableWait(cancellationToken))
                    {
                        var first = default(KeyValuePair<TKey, TValue>);
                        foreach (var kv in map)
                        {
                            first = kv;
                            break;
                        }

                        // this is only one that removes data from the queue. so, it should always succeed
                        var result = map.Remove(first.Key);
                        Debug.Assert(result);

                        return first.Value;
                    }
                }

                private static void ClearQueueWorker<TKey, TValue>(NonReentrantLock gate, Dictionary<TKey, TValue> map, Func<TValue, IDisposable> disposerSelector)
                {
                    using (gate.DisposableWait(CancellationToken.None))
                    {
                        foreach (var (_, data) in map)
                        {
                            disposerSelector?.Invoke(data)?.Dispose();
                        }

                        map.Clear();
                    }
                }

                private static IEnumerable<ProjectId> GetProjectsToAnalyze(Solution solution, ProjectId projectId)
                {
                    var graph = solution.GetProjectDependencyGraph();

                    if (solution.Workspace.Options.GetOption(InternalSolutionCrawlerOptions.DirectDependencyPropagationOnly))
                    {
                        return graph.GetProjectsThatDirectlyDependOnThisProject(projectId).Concat(projectId);
                    }

                    // re-analyzing all transitive dependencies is very expensive. by default we will only
                    // re-analyze direct dependency for now. and consider flipping the default only if we must.
                    return graph.GetProjectsThatTransitivelyDependOnThisProject(projectId).Concat(projectId);
                }

                private readonly struct Data
                {
                    public readonly Document Document;
                    public readonly SyntaxPath ChangedMember;
                    public readonly IAsyncToken AsyncToken;

                    public Data(Document document, SyntaxPath changedMember, IAsyncToken asyncToken)
                    {
                        AsyncToken = asyncToken;
                        Document = document;
                        ChangedMember = changedMember;
                    }
                }

                private class ProjectProcessor : IdleProcessor
                {
                    private static readonly Func<int, ProjectId, string> s_enqueueLogger = (t, i) => string.Format("[{0}] {1}", t, i.ToString());

                    private readonly SemaphoreSlim _gate;

                    private readonly Registration _registration;
                    private readonly IncrementalAnalyzerProcessor _processor;

                    private readonly NonReentrantLock _workGate;
                    private readonly Dictionary<ProjectId, Data> _pendingWork;

                    public ProjectProcessor(
                        IAsynchronousOperationListener listener,
                        Registration registration,
                        IncrementalAnalyzerProcessor processor,
                        int backOffTimeSpanInMS,
                        CancellationToken cancellationToken) :
                        base(listener, backOffTimeSpanInMS, cancellationToken)
                    {
                        _registration = registration;
                        _processor = processor;

                        _gate = new SemaphoreSlim(initialCount: 0);

                        _workGate = new NonReentrantLock();
                        _pendingWork = new Dictionary<ProjectId, Data>();

                        Start();

                        // Register a clean-up task to ensure pending work items are flushed from the queue if they will
                        // never be processed.
                        AsyncProcessorTask.ContinueWith(
                            _ => ClearQueueWorker(_workGate, _pendingWork, data => data.AsyncToken),
                            CancellationToken.None,
                            TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default);
                    }

                    public void Enqueue(ProjectId projectId, bool needDependencyTracking = false)
                    {
                        UpdateLastAccessTime();

                        using (_workGate.DisposableWait(CancellationToken))
                        {
                            // the project is already in the queue. nothing needs to be done
                            if (_pendingWork.ContainsKey(projectId))
                            {
                                return;
                            }

                            var data = new Data(projectId, needDependencyTracking, Listener.BeginAsyncOperation(nameof(Enqueue), tag: _registration.Workspace));

                            _pendingWork.Add(projectId, data);
                            _gate.Release();
                        }

                        Logger.Log(FunctionId.WorkCoordinator_Project_Enqueue, s_enqueueLogger, Environment.TickCount, projectId);
                    }

                    public async Task EnqueueWorkItemAsync(Document document)
                    {
                        // we are shutting down
                        CancellationToken.ThrowIfCancellationRequested();

                        // call to this method is serialized. and only this method does the writing.
                        var priorityService = document.GetLanguageService<IWorkCoordinatorPriorityService>();
                        var isLowPriority = priorityService != null && await priorityService.IsLowPriorityAsync(document, CancellationToken).ConfigureAwait(false);

                        _processor.Enqueue(
                            new WorkItem(document.Id, document.Project.Language, InvocationReasons.SemanticChanged,
                                isLowPriority, Listener.BeginAsyncOperation(nameof(EnqueueWorkItemAsync), tag: EnqueueItem)));
                    }

                    protected override Task WaitAsync(CancellationToken cancellationToken)
                    {
                        return _gate.WaitAsync(cancellationToken);
                    }

                    protected override async Task ExecuteAsync()
                    {
                        var data = Dequeue();

                        using (data.AsyncToken)
                        {
                            var project = _registration.CurrentSolution.GetProject(data.ProjectId);
                            if (project == null)
                            {
                                return;
                            }

                            if (!data.NeedDependencyTracking)
                            {
                                await EnqueueWorkItemAsync(project).ConfigureAwait(false);
                                return;
                            }

                            // do dependency tracking here with current solution
                            var solution = _registration.CurrentSolution;
                            foreach (var projectId in GetProjectsToAnalyze(solution, data.ProjectId))
                            {
                                project = solution.GetProject(projectId);
                                await EnqueueWorkItemAsync(project).ConfigureAwait(false);
                            }
                        }
                    }

                    private Data Dequeue()
                    {
                        return DequeueWorker(_workGate, _pendingWork, CancellationToken);
                    }

                    private async Task EnqueueWorkItemAsync(Project project)
                    {
                        if (project == null)
                        {
                            return;
                        }

                        foreach (var document in project.Documents)
                        {
                            await EnqueueWorkItemAsync(document).ConfigureAwait(false);
                        }
                    }

                    private readonly struct Data
                    {
                        public readonly IAsyncToken AsyncToken;
                        public readonly ProjectId ProjectId;
                        public readonly bool NeedDependencyTracking;

                        public Data(ProjectId projectId, bool needDependencyTracking, IAsyncToken asyncToken)
                        {
                            AsyncToken = asyncToken;
                            ProjectId = projectId;
                            NeedDependencyTracking = needDependencyTracking;
                        }
                    }
                }
            }
        }
    }
}
