// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

internal sealed partial class UnitTestingSolutionCrawlerRegistrationService
{
    /// <summary>
    /// this will be used in the unit test to indicate certain action has happened or not.
    /// </summary>
    public const string EnqueueItem = nameof(EnqueueItem);

    internal sealed partial class UnitTestingWorkCoordinator
    {
        private sealed class UnitTestingSemanticChangeProcessor : UnitTestingIdleProcessor
        {
            private static readonly Func<int, DocumentId, bool, string> s_enqueueLogger = (tick, documentId, hint) => $"Tick:{tick}, {documentId}, {documentId.ProjectId}, hint:{hint}";

            private readonly SemaphoreSlim _gate;

            private readonly UnitTestingRegistration _registration;
            private readonly UnitTestingProjectProcessor _processor;

            private readonly SemaphoreSlim _workGate = new(initialCount: 1);
            private readonly Dictionary<DocumentId, UnitTestingData> _pendingWork = [];

            public UnitTestingSemanticChangeProcessor(
                IAsynchronousOperationListener listener,
                UnitTestingRegistration registration,
                UnitTestingIncrementalAnalyzerProcessor documentWorkerProcessor,
                TimeSpan backOffTimeSpan,
                TimeSpan projectBackOffTimeSpan,
                CancellationToken cancellationToken)
                : base(listener, backOffTimeSpan, cancellationToken)
            {
                _gate = new SemaphoreSlim(initialCount: 0);

                _registration = registration;

                _processor = new UnitTestingProjectProcessor(listener, registration, documentWorkerProcessor, projectBackOffTimeSpan, cancellationToken);

                Start();

                // Register a clean-up task to ensure pending work items are flushed from the queue if they will
                // never be processed.
                AsyncProcessorTask.ContinueWith(
                    _ => ClearQueueWorker(_workGate, _pendingWork, data => data.AsyncToken),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            protected override void OnPaused()
            {
            }

            public override Task AsyncProcessorTask
                => Task.WhenAll(base.AsyncProcessorTask, _processor.AsyncProcessorTask);

            protected override Task WaitAsync(CancellationToken cancellationToken)
                => _gate.WaitAsync(cancellationToken);

            protected override async Task ExecuteAsync()
            {
                var data = Dequeue();

                using (data.AsyncToken)
                {
                    // we have a hint. check whether we can take advantage of it
                    if (await TryEnqueueFromHintAsync(data).ConfigureAwait(continueOnCapturedContext: false))
                        return;

                    EnqueueFullProjectDependency(data.Project);
                }
            }

            private UnitTestingData Dequeue()
                => DequeueWorker(_workGate, _pendingWork, CancellationToken);

            private async Task<bool> TryEnqueueFromHintAsync(UnitTestingData data)
            {
                var changedMember = data.ChangedMember;
                if (changedMember == null)
                    return false;

                var document = data.GetRequiredDocument();

                // see whether we already have semantic model. otherwise, use the expansive full project dependency one
                // TODO: if there is a reliable way to track changed member, we could use GetSemanticModel here which could
                //       rebuild compilation from scratch
                if (!document.TryGetSemanticModel(out var model) ||
                    !changedMember.TryResolve(await document.GetSyntaxRootAsync(CancellationToken).ConfigureAwait(false), out SyntaxNode? declarationNode))
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
                    EnqueueFullProjectDependency(document.Project, assembly);
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
                => EnqueueWorkItemAsync(document, symbol.ContainingType != null ? symbol.ContainingType.Locations : symbol.Locations);

            private async Task EnqueueWorkItemAsync(Document thisDocument, ImmutableArray<Location> locations)
            {
                var solution = thisDocument.Project.Solution;
                var projectId = thisDocument.Id.ProjectId;

                foreach (var location in locations)
                {
                    Debug.Assert(location.IsInSource);

                    var documentId = solution.GetDocumentId(location.SourceTree, projectId);
                    if (documentId == null || thisDocument.Id == documentId)
                        continue;

                    await _processor.EnqueueWorkItemAsync(solution.GetRequiredProject(documentId.ProjectId), documentId, document: null).ConfigureAwait(false);
                }
            }

            private static bool IsInternal(ISymbol symbol)
            {
                return symbol.DeclaredAccessibility is Accessibility.Internal or
                       Accessibility.ProtectedAndInternal or
                       Accessibility.ProtectedOrInternal;
            }

            private static bool IsType(ISymbol symbol)
                => symbol.Kind == SymbolKind.NamedType;

            private static bool IsMember(ISymbol symbol)
            {
                return symbol.Kind is SymbolKind.Event or
                       SymbolKind.Field or
                       SymbolKind.Method or
                       SymbolKind.Property;
            }

            private void EnqueueFullProjectDependency(Project project, IAssemblySymbol? internalVisibleToAssembly = null)
            {
                var self = project.Id;

                // if there is no hint (this can happen for cases such as solution/project load and etc), 
                // we can postpone it even further
                if (internalVisibleToAssembly == null)
                {
                    _processor.Enqueue(self, needDependencyTracking: true);
                    return;
                }

                // most likely we got here since we are called due to typing.
                // calculate dependency here and register each affected project to the next pipe line
                var solution = project.Solution;
                foreach (var projectId in GetProjectsToAnalyze(solution, self))
                {
                    var otherProject = solution.GetProject(projectId);
                    if (otherProject == null)
                        continue;

                    if (otherProject.TryGetCompilation(out var compilation))
                    {
                        var assembly = compilation.Assembly;
                        if (assembly != null && !assembly.IsSameAssemblyOrHasFriendAccessTo(internalVisibleToAssembly))
                            continue;
                    }

                    _processor.Enqueue(projectId);
                }

                Logger.Log(FunctionId.WorkCoordinator_SemanticChange_FullProjects, internalVisibleToAssembly == null ? "full" : "internals");
            }

            public void Enqueue(Project project, DocumentId documentId, Document? document, SyntaxPath? changedMember)
            {
                UpdateLastAccessTime();

                using (_workGate.DisposableWait(CancellationToken))
                {
                    if (_pendingWork.TryGetValue(documentId, out var data))
                    {
                        // create new async token and dispose old one.
                        var newAsyncToken = Listener.BeginAsyncOperation(nameof(Enqueue), tag: _registration.Services);
                        data.AsyncToken.Dispose();

                        _pendingWork[documentId] = new UnitTestingData(project, documentId, document, data.ChangedMember == changedMember ? changedMember : null, newAsyncToken);
                        return;
                    }

                    _pendingWork.Add(documentId, new UnitTestingData(project, documentId, document, changedMember, Listener.BeginAsyncOperation(nameof(Enqueue), tag: _registration.Services)));
                    _gate.Release();
                }

                Logger.Log(FunctionId.WorkCoordinator_SemanticChange_Enqueue, s_enqueueLogger, Environment.TickCount, documentId, changedMember != null);
            }

            private static TValue DequeueWorker<TKey, TValue>(SemaphoreSlim gate, Dictionary<TKey, TValue> map, CancellationToken cancellationToken)
                where TKey : notnull
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

            private static void ClearQueueWorker<TKey, TValue>(SemaphoreSlim gate, Dictionary<TKey, TValue> map, Func<TValue, IDisposable> disposerSelector)
                where TKey : notnull
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

                // Reanalyze direct dependencies only as reanalyzing all transitive dependencies is very expensive.
                return graph.GetProjectsThatDirectlyDependOnThisProject(projectId).Concat(projectId);
            }

            private readonly struct UnitTestingData(Project project, DocumentId documentId, Document? document, SyntaxPath? changedMember, IAsyncToken asyncToken)
            {
                private readonly DocumentId _documentId = documentId;
                private readonly Document? _document = document;

                public readonly Project Project = project;
                public readonly SyntaxPath? ChangedMember = changedMember;
                public readonly IAsyncToken AsyncToken = asyncToken;

                public Document GetRequiredDocument()
                    => UnitTestingWorkCoordinator.GetRequiredDocument(Project, _documentId, _document);
            }

            private class UnitTestingProjectProcessor : UnitTestingIdleProcessor
            {
                private static readonly Func<int, ProjectId, string> s_enqueueLogger = (t, i) => string.Format("[{0}] {1}", t, i.ToString());

                private readonly SemaphoreSlim _gate;

                private readonly UnitTestingRegistration _registration;
                private readonly UnitTestingIncrementalAnalyzerProcessor _processor;

                private readonly SemaphoreSlim _workGate = new(initialCount: 1);
                private readonly Dictionary<ProjectId, UnitTestingData> _pendingWork = [];

                public UnitTestingProjectProcessor(
                    IAsynchronousOperationListener listener,
                    UnitTestingRegistration registration,
                    UnitTestingIncrementalAnalyzerProcessor processor,
                    TimeSpan backOffTimeSpan,
                    CancellationToken cancellationToken)
                    : base(listener, backOffTimeSpan, cancellationToken)
                {
                    _registration = registration;
                    _processor = processor;

                    _gate = new SemaphoreSlim(initialCount: 0);

                    Start();

                    // Register a clean-up task to ensure pending work items are flushed from the queue if they will
                    // never be processed.
                    AsyncProcessorTask.ContinueWith(
                        _ => ClearQueueWorker(_workGate, _pendingWork, data => data.AsyncToken),
                        CancellationToken.None,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }

                protected override void OnPaused()
                {
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

                        var data = new UnitTestingData(projectId, needDependencyTracking, Listener.BeginAsyncOperation(nameof(Enqueue), tag: _registration.Services));

                        _pendingWork.Add(projectId, data);
                        _gate.Release();
                    }

                    Logger.Log(FunctionId.WorkCoordinator_Project_Enqueue, s_enqueueLogger, Environment.TickCount, projectId);
                }

                public async Task EnqueueWorkItemAsync(Project project, DocumentId documentId, Document? document)
                {
                    // we are shutting down
                    CancellationToken.ThrowIfCancellationRequested();

                    // call to this method is serialized. and only this method does the writing.
                    var priorityService = project.GetLanguageService<IUnitTestingWorkCoordinatorPriorityService>();
                    var isLowPriority = priorityService != null && await priorityService.IsLowPriorityAsync(GetRequiredDocument(project, documentId, document), CancellationToken).ConfigureAwait(false);

                    _processor.Enqueue(
                        new UnitTestingWorkItem(documentId, project.Language, UnitTestingInvocationReasons.SemanticChanged,
                            isLowPriority, activeMember: null, Listener.BeginAsyncOperation(nameof(EnqueueWorkItemAsync), tag: EnqueueItem)));
                }

                protected override Task WaitAsync(CancellationToken cancellationToken)
                    => _gate.WaitAsync(cancellationToken);

                protected override async Task ExecuteAsync()
                {
                    var data = Dequeue();

                    using (data.AsyncToken)
                    {
                        var project = _registration.GetSolutionToAnalyze().GetProject(data.ProjectId);
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
                        var solution = _registration.GetSolutionToAnalyze();
                        foreach (var projectId in GetProjectsToAnalyze(solution, data.ProjectId))
                        {
                            project = solution.GetProject(projectId);
                            await EnqueueWorkItemAsync(project).ConfigureAwait(false);
                        }
                    }
                }

                private UnitTestingData Dequeue()
                    => DequeueWorker(_workGate, _pendingWork, CancellationToken);

                private async Task EnqueueWorkItemAsync(Project? project)
                {
                    if (project == null)
                        return;

                    foreach (var documentId in project.DocumentIds)
                        await EnqueueWorkItemAsync(project, documentId, document: null).ConfigureAwait(false);
                }

                private readonly struct UnitTestingData(ProjectId projectId, bool needDependencyTracking, IAsyncToken asyncToken)
                {
                    public readonly IAsyncToken AsyncToken = asyncToken;
                    public readonly ProjectId ProjectId = projectId;
                    public readonly bool NeedDependencyTracking = needDependencyTracking;
                }
            }
        }
    }
}
