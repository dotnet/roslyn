// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

[Export(typeof(IProjectCodeModelFactory))]
[Export(typeof(ProjectCodeModelFactory))]
internal sealed class ProjectCodeModelFactory : IProjectCodeModelFactory
{
    private readonly ConcurrentDictionary<ProjectId, ProjectCodeModel> _projectCodeModels = [];

    private readonly VisualStudioWorkspace _visualStudioWorkspace;
    private readonly IServiceProvider _serviceProvider;
    private readonly IThreadingContext _threadingContext;

    private readonly AsyncBatchingWorkQueue<WorkspaceChangeEventArgs> _workspaceChangeEventsToFireEventsFor;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ProjectCodeModelFactory(
        VisualStudioWorkspace visualStudioWorkspace,
        [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        IThreadingContext threadingContext,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _visualStudioWorkspace = visualStudioWorkspace;
        _serviceProvider = serviceProvider;
        _threadingContext = threadingContext;

        Listener = listenerProvider.GetListener(FeatureAttribute.CodeModel);

        // Queue up notifications we hear about docs changing.  that way we don't have to fire events multiple times
        // for the same documents.  Once enough time has passed, take the documents that were changed and run
        // through them, firing their latest events.
        _workspaceChangeEventsToFireEventsFor = new AsyncBatchingWorkQueue<WorkspaceChangeEventArgs>(
            DelayTimeSpan.Idle,
            ProcessNextWorkspaceChangeEventBatchAsync,
            Listener,
            threadingContext.DisposalToken);

        _ = _visualStudioWorkspace.RegisterWorkspaceChangedHandler(OnWorkspaceChanged);
    }

    internal IAsynchronousOperationListener Listener { get; }

    private async ValueTask ProcessNextWorkspaceChangeEventBatchAsync(
        ImmutableSegmentedList<WorkspaceChangeEventArgs> workspaceChangeEvents, CancellationToken cancellationToken)
    {
        Debug.Assert(!_threadingContext.JoinableTaskContext.IsOnMainThread, "The following context switch is not expected to cause runtime overhead.");
        await TaskScheduler.Default;

        // Calculate the full set of changes over this set of events while on a background thread.
        using var _1 = PooledHashSet<DocumentId>.GetInstance(out var documentIds);
        AddChangedDocuments(workspaceChangeEvents, documentIds);

        if (documentIds.Count == 0)
            return;

        // get the file path information while on a background thread
        using var _2 = ArrayBuilder<(ProjectCodeModel projectCodeModel, string filename)>.GetInstance(out var projectCodeModelAndFileNames);
        AddProjectCodeModelAndFileNames(documentIds, projectCodeModelAndFileNames);

        // Ensure MEF services used by the code model are initially obtained on a background thread.
        EnsureServicesCreated();

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        // Fire off the code model events while on the main thread
        await FireEventsForChangedDocumentsAsync(projectCodeModelAndFileNames, cancellationToken).ConfigureAwait(false);
    }

    private static void AddChangedDocuments(ImmutableSegmentedList<WorkspaceChangeEventArgs> workspaceChangeEvents, PooledHashSet<DocumentId> documentIds)
    {
        if (workspaceChangeEvents.All(static e => e.Kind is WorkspaceChangeKind.DocumentRemoved or WorkspaceChangeKind.DocumentChanged))
        {
            // Fast path when we know we affected a document that could have had code model elements in it.  No
            // need to do a solution diff in this case.
            foreach (var e in workspaceChangeEvents)
                documentIds.Add(e.DocumentId!);

            return;
        }

        // Contains an event that could indicate a doc change/removal. Have to actually analyze the change to
        // determine what we should do here.
        var oldSolution = workspaceChangeEvents[0].OldSolution;
        var newSolution = workspaceChangeEvents[^1].NewSolution;

        var changes = oldSolution.GetChanges(newSolution);

        foreach (var project in changes.GetRemovedProjects())
            documentIds.AddRange(project.DocumentIds);

        foreach (var projectChange in changes.GetProjectChanges())
        {
            documentIds.AddRange(projectChange.GetRemovedDocuments());
            documentIds.AddRange(projectChange.GetChangedDocuments());
        }
    }

    private void AddProjectCodeModelAndFileNames(HashSet<DocumentId> documentIds, ArrayBuilder<(ProjectCodeModel, string)> projectCodeModelAndFileNames)
    {
        foreach (var documentId in documentIds)
        {
            var projectCodeModel = this.TryGetProjectCodeModel(documentId.ProjectId);
            if (projectCodeModel == null)
                return;

            var filename = _visualStudioWorkspace.GetFilePath(documentId);
            if (filename == null)
                return;

            projectCodeModelAndFileNames.Add((projectCodeModel, filename));
        }
    }

    private async Task FireEventsForChangedDocumentsAsync(
        ArrayBuilder<(ProjectCodeModel projectCodeModel, string filename)> projectCodeModelAndFileNames,
        CancellationToken cancellationToken)
    {
        // This logic preserves the previous behavior we had with IForegroundNotificationService.
        // Specifically, we don't run on the UI thread for more than 15ms at a time. And we don't
        // check the input queue more than once per ms. Once we have run for more than 15 ms,
        // we wait 50ms before continuing.  These constants are just what we defined from
        // legacy, and otherwise have no special meaning.
        const int MaxTimeSlice = 15;
        double nextInputCheckElapsedMs = 1;

        var stopwatch = SharedStopwatch.StartNew();
        foreach (var (projectCodeModel, filename) in projectCodeModelAndFileNames)
        {
            // If we've been asked to shutdown, don't bother reporting any more events.
            if (cancellationToken.IsCancellationRequested)
                return;

            FireEventsForDocument(projectCodeModel, filename);

            // Keep firing events for this doc, as long as we haven't exceeded MaxTimeSlice ms or input isn't pending.
            // We'll validate against those constraints at most once every 1 ms, to avoid spam checking the
            // input queue, as this has shown up in performance profiles.
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            if (elapsedMs > nextInputCheckElapsedMs)
            {
                if (elapsedMs > MaxTimeSlice || IsInputPending())
                {
                    await this.Listener.Delay(DelayTimeSpan.NearImmediate, cancellationToken).ConfigureAwait(true);
                    stopwatch = SharedStopwatch.StartNew();
                    elapsedMs = 0;
                }

                nextInputCheckElapsedMs = elapsedMs + 1;
            }
        }

        static void FireEventsForDocument(ProjectCodeModel projectCodeModel, string filename)
        {
            if (projectCodeModel.TryGetCachedFileCodeModel(filename, out var fileCodeModelHandle))
                fileCodeModelHandle.Object.FireEvents();
        }

        // Returns true if any keyboard or mouse button input is pending on the message queue.
        static bool IsInputPending()
        {
            // The code below invokes into user32.dll, which is not available in non-Windows.
            if (PlatformInformation.IsUnix)
                return false;

            // The return value of GetQueueStatus is HIWORD:LOWORD.
            // A non-zero value in HIWORD indicates some input message in the queue.
            var result = NativeMethods.GetQueueStatus(NativeMethods.QS_INPUT);

            const uint InputMask = NativeMethods.QS_INPUT | (NativeMethods.QS_INPUT << 16);
            return (result & InputMask) != 0;
        }
    }

    private void EnsureServicesCreated()
    {
        // Ensure MEF services used by the code model are initially obtained on a background thread.
        // This code avoids allocations where possible.
        // https://github.com/dotnet/roslyn/issues/54159
        using var _ = PooledHashSet<string>.GetInstance(out var previousLanguages);
        foreach (var projectState in _visualStudioWorkspace.CurrentSolution.SolutionState.SortedProjectStates)
        {
            // Avoid duplicate calls if the language has been seen
            if (previousLanguages.Add(projectState.Language))
            {
                projectState.LanguageServices.GetService<ICodeModelService>();
                projectState.LanguageServices.GetService<ISyntaxFactsService>();
                projectState.LanguageServices.GetService<ICodeGenerationService>();
            }
        }
    }

    private void OnWorkspaceChanged(WorkspaceChangeEventArgs e)
    {
        // Events that can't change existing code model items.  Can just ignore them.
        switch (e.Kind)
        {
            case WorkspaceChangeKind.SolutionAdded:
            case WorkspaceChangeKind.ProjectAdded:
            case WorkspaceChangeKind.DocumentAdded:
            case WorkspaceChangeKind.AdditionalDocumentAdded:
            case WorkspaceChangeKind.AdditionalDocumentRemoved:
            case WorkspaceChangeKind.AdditionalDocumentReloaded:
            case WorkspaceChangeKind.AdditionalDocumentChanged:
            case WorkspaceChangeKind.AnalyzerConfigDocumentAdded:
            case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
            case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
            case WorkspaceChangeKind.AnalyzerConfigDocumentChanged:
                return;
        }

        _workspaceChangeEventsToFireEventsFor.AddWork(e);
    }

    public IProjectCodeModel CreateProjectCodeModel(ProjectId id, ICodeModelInstanceFactory codeModelInstanceFactory)
    {
        var projectCodeModel = new ProjectCodeModel(_threadingContext, id, codeModelInstanceFactory, _visualStudioWorkspace, _serviceProvider, this);
        if (!_projectCodeModels.TryAdd(id, projectCodeModel))
        {
            throw new InvalidOperationException($"A {nameof(IProjectCodeModel)} has already been created for project with ID {id}");
        }

        return projectCodeModel;
    }

    public ProjectCodeModel GetProjectCodeModel(ProjectId id)
    {
        if (!_projectCodeModels.TryGetValue(id, out var projectCodeModel))
        {
            throw new InvalidOperationException($"No {nameof(ProjectCodeModel)} exists for project with ID {id}");
        }

        return projectCodeModel;
    }

    public IEnumerable<ProjectCodeModel> GetAllProjectCodeModels()
        => _projectCodeModels.Values;

    internal void OnProjectClosed(ProjectId projectId)
        => _projectCodeModels.TryRemove(projectId, out _);

    public ProjectCodeModel TryGetProjectCodeModel(ProjectId id)
    {
        _projectCodeModels.TryGetValue(id, out var projectCodeModel);
        return projectCodeModel;
    }

    public EnvDTE.FileCodeModel GetOrCreateFileCodeModel(ProjectId id, string filePath)
        => GetProjectCodeModel(id).GetOrCreateFileCodeModel(filePath).Handle;

    public EnvDTE.FileCodeModel CreateFileCodeModel(SourceGeneratedDocument sourceGeneratedDocument)
        => GetProjectCodeModel(sourceGeneratedDocument.Project.Id).CreateFileCodeModel(sourceGeneratedDocument);

    public void ScheduleDeferredCleanupTask(Action<CancellationToken> a)
    {
        _ = _threadingContext.RunWithShutdownBlockAsync(async cancellationToken =>
        {
            await _threadingContext.JoinableTaskFactory.StartOnIdle(
                () => a(cancellationToken),
                VsTaskRunContext.UIThreadNormalPriority);
        });
    }
}
