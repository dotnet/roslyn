// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.LanguageServices.Telemetry;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using VSLangProj;
using VSLangProj140;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using OleInterop = Microsoft.VisualStudio.OLE.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

/// <summary>
/// The Workspace for running inside Visual Studio.
/// </summary>
internal abstract partial class VisualStudioWorkspaceImpl : VisualStudioWorkspace
{
    private static readonly IntPtr s_docDataExisting_Unknown = new(-1);
    private const string AppCodeFolderName = "App_Code";

    private readonly IThreadingContext _threadingContext;
    private readonly IAsyncServiceProvider _asyncServiceProvider;
    private readonly ITextBufferFactoryService _textBufferFactoryService;
    private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
    private readonly IGlobalOptionService _globalOptions;

    private readonly ITextBufferCloneService _textBufferCloneService;

    /// <summary>
    /// Guards any updates to the maps here that aren't updated via interlocked updates.
    /// </summary>
    // Our usage of SemaphoreSlim is fine.  We don't perform blocking waits for it on the UI thread.
#pragma warning disable RS0030 // Do not use banned APIs
    private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);
#pragma warning restore RS0030 // Do not use banned APIs

    /// <summary>
    /// A <see cref="ForegroundThreadAffinitizedObject"/> to make assertions that stuff is on the right thread.
    /// </summary>
    private readonly ForegroundThreadAffinitizedObject _foregroundObject;

    private ImmutableDictionary<ProjectId, IVsHierarchy?> _projectToHierarchyMap = ImmutableDictionary<ProjectId, IVsHierarchy?>.Empty;
    private ImmutableDictionary<ProjectId, Guid> _projectToGuidMap = ImmutableDictionary<ProjectId, Guid>.Empty;

    /// <summary>
    /// A map to fetch the path to a rule set file for a project. This right now is only used to implement
    /// <see cref="TryGetRuleSetPathForProject(ProjectId)"/> and any other use is extremely suspicious, since direct use of this is out of
    /// sync with the Workspace if there is active batching happening.
    /// </summary>
    /// <remarks>Should be updated with <see cref="ImmutableInterlocked"/>.</remarks>
    private ImmutableDictionary<ProjectId, Func<string?>> _projectToRuleSetFilePath = ImmutableDictionary<ProjectId, Func<string?>>.Empty;

    private readonly Dictionary<string, List<ProjectSystemProject>> _projectSystemNameToProjectsMap = [];

    /// <summary>
    /// Only safe to use on the UI thread.
    /// </summary>
    private readonly Dictionary<string, UIContext?> _languageToProjectExistsUIContext = [];

    private VirtualMemoryNotificationListener? _memoryListener;

    private OpenFileTracker? _openFileTracker;
    internal IFileChangeWatcher FileChangeWatcher { get; }

    internal ProjectSystemProjectFactory ProjectSystemProjectFactory { get; }

    private readonly Lazy<IProjectCodeModelFactory> _projectCodeModelFactory;
    private readonly Lazy<ExternalErrorDiagnosticUpdateSource> _lazyExternalErrorDiagnosticUpdateSource;
    private readonly IAsynchronousOperationListener _workspaceListener;
    private bool _isExternalErrorDiagnosticUpdateSourceSubscribedToSolutionBuildEvents;

    public VisualStudioWorkspaceImpl(ExportProvider exportProvider, IAsyncServiceProvider asyncServiceProvider)
        : base(VisualStudioMefHostServices.Create(exportProvider))
    {
        _threadingContext = exportProvider.GetExportedValue<IThreadingContext>();
        _asyncServiceProvider = asyncServiceProvider;
        _globalOptions = exportProvider.GetExportedValue<IGlobalOptionService>();
        _textBufferCloneService = exportProvider.GetExportedValue<ITextBufferCloneService>();
        _textBufferFactoryService = exportProvider.GetExportedValue<ITextBufferFactoryService>();
        _projectionBufferFactoryService = exportProvider.GetExportedValue<IProjectionBufferFactoryService>();
        _projectCodeModelFactory = exportProvider.GetExport<IProjectCodeModelFactory>();

        _foregroundObject = new ForegroundThreadAffinitizedObject(_threadingContext);

        _textBufferFactoryService.TextBufferCreated += AddTextBufferCloneServiceToBuffer;
        _projectionBufferFactoryService.ProjectionBufferCreated += AddTextBufferCloneServiceToBuffer;

        FileChangeWatcher = exportProvider.GetExportedValue<FileChangeWatcherProvider>().Watcher;

        ProjectSystemProjectFactory = new ProjectSystemProjectFactory(this, FileChangeWatcher, CheckForAddedFileBeingOpenMaybeAsync, RemoveProjectFromMaps);

        _ = Task.Run(() => InitializeUIAffinitizedServicesAsync(asyncServiceProvider));

        _lazyExternalErrorDiagnosticUpdateSource = new Lazy<ExternalErrorDiagnosticUpdateSource>(() =>
            exportProvider.GetExportedValue<ExternalErrorDiagnosticUpdateSource>(),
            isThreadSafe: true);

        _workspaceListener = Services.GetRequiredService<IWorkspaceAsynchronousOperationListenerProvider>().GetListener();
    }

    internal ExternalErrorDiagnosticUpdateSource ExternalErrorDiagnosticUpdateSource => _lazyExternalErrorDiagnosticUpdateSource.Value;

    internal void SubscribeExternalErrorDiagnosticUpdateSourceToSolutionBuildEvents()
    {
        // TODO: further understand if this needs the foreground thread for any reason. UIContexts are safe to read from the UI thread;
        // it's not clear to me why this is being asserted.
        _foregroundObject.AssertIsForeground();

        if (_isExternalErrorDiagnosticUpdateSourceSubscribedToSolutionBuildEvents)
        {
            return;
        }

        // TODO: https://github.com/dotnet/roslyn/issues/36065
        // UIContextImpl requires IVsMonitorSelection service:
        if (ServiceProvider.GlobalProvider.GetService(typeof(IVsMonitorSelection)) == null)
        {
            return;
        }

        // This pattern ensures that we are called whenever the build starts/completes even if it is already in progress.
        KnownUIContexts.SolutionBuildingContext.WhenActivated(() =>
        {
            KnownUIContexts.SolutionBuildingContext.UIContextChanged += (object _, UIContextChangedEventArgs e) =>
            {
                if (e.Activated)
                {
                    ExternalErrorDiagnosticUpdateSource.OnSolutionBuildStarted();
                }
                else
                {
                    // A real build just finished.  Clear out any results from the last "run code analysis" command.
                    this.Services.GetRequiredService<ICodeAnalysisDiagnosticAnalyzerService>().Clear();
                    ExternalErrorDiagnosticUpdateSource.OnSolutionBuildCompleted();
                }
            };

            ExternalErrorDiagnosticUpdateSource.OnSolutionBuildStarted();
        });

        _isExternalErrorDiagnosticUpdateSourceSubscribedToSolutionBuildEvents = true;
    }

    public async Task InitializeUIAffinitizedServicesAsync(IAsyncServiceProvider asyncServiceProvider)
    {
        // Create services that are bound to the UI thread
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(_threadingContext.DisposalToken);

        // Fetch the session synchronously on the UI thread; if this doesn't happen before we try using this on
        // the background thread then we will experience hangs like we see in this bug:
        // https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?_a=edit&id=190808 or
        // https://devdiv.visualstudio.com/DevDiv/_workitems?id=296981&_a=edit
        var telemetrySession = TelemetryService.DefaultSession;

        var solutionClosingContext = UIContext.FromUIContextGuid(VSConstants.UICONTEXT.SolutionClosing_guid);
        solutionClosingContext.UIContextChanged += (_, e) => ProjectSystemProjectFactory.SolutionClosing = e.Activated;

        var openFileTracker = await OpenFileTracker.CreateAsync(this, ProjectSystemProjectFactory, asyncServiceProvider).ConfigureAwait(true);
        var memoryListener = await VirtualMemoryNotificationListener.CreateAsync(this, _threadingContext, asyncServiceProvider, _globalOptions, _threadingContext.DisposalToken).ConfigureAwait(true);

        // Update our fields first, so any asynchronous work that needs to use these is able to see the service.
        // WARNING: if we do .ConfigureAwait(true) here, it means we're trying to transition to the UI thread while
        // semaphore is acquired; if the UI thread is blocked trying to acquire the semaphore, we could deadlock.
        using (await _gate.DisposableWaitAsync().ConfigureAwait(false))
        {
            _openFileTracker = openFileTracker;
            _memoryListener = memoryListener;
        }

        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(_threadingContext.DisposalToken);

        // This must be called after the _openFileTracker was assigned; this way we know that a file added from the project system either got checked
        // in CheckForAddedFileBeingOpenMaybeAsync, or we catch it here.
        openFileTracker.CheckForOpenFilesThatWeMissed();

        // Switch to a background thread to avoid loading option providers on UI thread (telemetry is reading options).
        await TaskScheduler.Default;

        var logDelta = _globalOptions.GetOption(DiagnosticOptionsStorage.LogTelemetryForBackgroundAnalyzerExecution);
        var telemetryService = (VisualStudioWorkspaceTelemetryService)Services.GetRequiredService<IWorkspaceTelemetryService>();
        telemetryService.InitializeTelemetrySession(telemetrySession, logDelta);

        Logger.Log(FunctionId.Run_Environment,
            KeyValueLogMessage.Create(m => m["Version"] = FileVersionInfo.GetVersionInfo(typeof(VisualStudioWorkspace).Assembly.Location).FileVersion));
    }

    public Task CheckForAddedFileBeingOpenMaybeAsync(bool useAsync, ImmutableArray<string> newFileNames)
        => _openFileTracker?.CheckForAddedFileBeingOpenMaybeAsync(useAsync, newFileNames) ?? Task.CompletedTask;

    internal void AddProjectToInternalMaps(ProjectSystemProject project, IVsHierarchy? hierarchy, Guid guid, string projectSystemName)
    {
        using (_gate.DisposableWait())
        {
            _projectToHierarchyMap = _projectToHierarchyMap.Add(project.Id, hierarchy);
            _projectToGuidMap = _projectToGuidMap.Add(project.Id, guid);
            _projectSystemNameToProjectsMap.MultiAdd(projectSystemName, project);
        }
    }

    internal void AddProjectRuleSetFileToInternalMaps(ProjectSystemProject project, Func<string?> ruleSetFilePathFunc)
    {
        Contract.ThrowIfFalse(ImmutableInterlocked.TryAdd(ref _projectToRuleSetFilePath, project.Id, ruleSetFilePathFunc));
    }

    internal ProjectSystemProject? GetProjectWithHierarchyAndName(IVsHierarchy hierarchy, string projectName)
    {
        using (_gate.DisposableWait())
        {
            return GetProjectWithHierarchyAndName_NoLock(hierarchy, projectName);
        }
    }

    private ProjectSystemProject? GetProjectWithHierarchyAndName_NoLock(IVsHierarchy hierarchy, string projectName)
    {
        if (_projectSystemNameToProjectsMap.TryGetValue(projectName, out var projects))
        {
            foreach (var project in projects)
            {
                if (_projectToHierarchyMap.TryGetValue(project.Id, out var projectHierarchy))
                {
                    if (projectHierarchy == hierarchy)
                    {
                        return project;
                    }
                }
            }
        }

        return null;
    }

    // TODO: consider whether this should be going to the project system directly to get this path. This is only called on interactions from the
    // Solution Explorer in the SolutionExplorerShim, where if we just could more directly get to the rule set file it'd simplify this.
    internal override string? TryGetRuleSetPathForProject(ProjectId projectId)
    {
        // _projectToRuleSetFilePath is immutable, so can be used outside of locks
        if (_projectToRuleSetFilePath.TryGetValue(projectId, out var ruleSetPathFunc))
        {
            return ruleSetPathFunc();
        }
        else
        {
            return null;
        }
    }

    public override EnvDTE.FileCodeModel GetFileCodeModel(DocumentId documentId)
    {
        if (documentId == null)
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        var document = _threadingContext.JoinableTaskFactory.Run(() => CurrentSolution.GetDocumentAsync(documentId, includeSourceGenerated: true).AsTask());
        if (document == null)
        {
            throw new ArgumentException(ServicesVSResources.The_given_DocumentId_did_not_come_from_the_Visual_Studio_workspace, nameof(documentId));
        }

        if (document is SourceGeneratedDocument sourceGeneratedDocument)
        {
            return _projectCodeModelFactory.Value.CreateFileCodeModel(sourceGeneratedDocument);
        }
        else
        {
            return _projectCodeModelFactory.Value.GetOrCreateFileCodeModel(documentId.ProjectId, document.FilePath);
        }
    }

    internal override bool TryApplyChanges(
        Microsoft.CodeAnalysis.Solution newSolution,
        IProgress<CodeAnalysisProgress> progressTracker)
    {
        if (!_foregroundObject.IsForeground())
        {
            throw new InvalidOperationException(ServicesVSResources.VisualStudioWorkspace_TryApplyChanges_cannot_be_called_from_a_background_thread);
        }

        var currentSolution = this.CurrentSolution;
        var projectChanges = newSolution.GetChanges(currentSolution).GetProjectChanges().ToList();

        // first make sure we can edit the document we will be updating (check them out from source control, etc)
        var changedDocs = projectChanges.SelectMany(pd => pd.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true).Concat(pd.GetChangedAdditionalDocuments()))
                                        .Where(CanApplyChange).ToList();
        if (changedDocs.Count > 0)
        {
            this.EnsureEditableDocuments(changedDocs);
        }

        return base.TryApplyChanges(newSolution, progressTracker);

        bool CanApplyChange(DocumentId documentId)
        {
            var document = newSolution.GetDocument(documentId) ?? currentSolution.GetDocument(documentId);
            if (document == null)
            {
                // we can have null if documentId is for additional files
                return true;
            }

            return document.CanApplyChange();
        }
    }

    public override bool CanOpenDocuments
    {
        get
        {
            return true;
        }
    }

    internal override bool CanChangeActiveContextDocument
    {
        get
        {
            return true;
        }
    }

    internal bool IsCPSProject(CodeAnalysis.Project project)
        => IsCPSProject(project.Id);

    internal bool IsCPSProject(ProjectId projectId)
    {
        _foregroundObject.AssertIsForeground();

        if (this.TryGetHierarchy(projectId, out var hierarchy))
        {
            return hierarchy.IsCapabilityMatch("CPS");
        }

        return false;
    }

    internal bool IsPrimaryProject(ProjectId projectId)
    {
        using (_gate.DisposableWait())
        {
            foreach (var (_, projects) in _projectSystemNameToProjectsMap)
            {
                foreach (var project in projects)
                {
                    if (project.Id == projectId)
                        return project.IsPrimary;
                }
            }
        }

        return true;
    }

    public override bool CanApplyCompilationOptionChange(CompilationOptions oldOptions, CompilationOptions newOptions, CodeAnalysis.Project project)
        => project.Services.GetRequiredService<ICompilationOptionsChangingService>().CanApplyChange(oldOptions, newOptions);

    public override bool CanApplyParseOptionChange(ParseOptions oldOptions, ParseOptions newOptions, CodeAnalysis.Project project)
    {
        var maxSupportLangVersion = ProjectSystemProjectFactory.TryGetMaxSupportedLanguageVersion(project.Id);

        return project.Services.GetRequiredService<IParseOptionsChangingService>().CanApplyChange(oldOptions, newOptions, maxSupportLangVersion);
    }

    private void AddTextBufferCloneServiceToBuffer(object sender, TextBufferCreatedEventArgs e)
        => e.TextBuffer.Properties.AddProperty(typeof(ITextBufferCloneService), _textBufferCloneService);

    public override bool CanApplyChange(ApplyChangesKind feature)
    {
        switch (feature)
        {
            case ApplyChangesKind.AddDocument:
            case ApplyChangesKind.RemoveDocument:
            case ApplyChangesKind.ChangeDocument:
            case ApplyChangesKind.AddMetadataReference:
            case ApplyChangesKind.RemoveMetadataReference:
            case ApplyChangesKind.AddProjectReference:
            case ApplyChangesKind.RemoveProjectReference:
            case ApplyChangesKind.AddAnalyzerReference:
            case ApplyChangesKind.RemoveAnalyzerReference:
            case ApplyChangesKind.AddAdditionalDocument:
            case ApplyChangesKind.RemoveAdditionalDocument:
            case ApplyChangesKind.ChangeAdditionalDocument:
            case ApplyChangesKind.ChangeCompilationOptions:
            case ApplyChangesKind.ChangeParseOptions:
            case ApplyChangesKind.ChangeDocumentInfo:
            case ApplyChangesKind.AddAnalyzerConfigDocument:
            case ApplyChangesKind.RemoveAnalyzerConfigDocument:
            case ApplyChangesKind.ChangeAnalyzerConfigDocument:
            case ApplyChangesKind.AddSolutionAnalyzerReference:
            case ApplyChangesKind.RemoveSolutionAnalyzerReference:
                return true;

            default:
                return false;
        }
    }

    private bool TryGetProjectData(ProjectId projectId, [NotNullWhen(returnValue: true)] out IVsHierarchy? hierarchy, [NotNullWhen(returnValue: true)] out EnvDTE.Project? project)
    {
        project = null;

        return
            this.TryGetHierarchy(projectId, out hierarchy) &&
            hierarchy.TryGetProject(out project);
    }

    internal void GetProjectData(ProjectId projectId, out IVsHierarchy hierarchy, out EnvDTE.Project project)
    {
        if (!TryGetProjectData(projectId, out hierarchy!, out project!))
        {
            throw new ArgumentException(string.Format(ServicesVSResources.Could_not_find_project_0, projectId));
        }
    }

    internal EnvDTE.Project? TryGetDTEProject(ProjectId projectId)
        => TryGetProjectData(projectId, out var _, out var project) ? project : null;

    internal bool TryAddReferenceToProject(ProjectId projectId, string assemblyName)
    {
        if (!TryGetProjectData(projectId, out _, out var project))
        {
            return false;
        }

        var vsProject = (VSProject)project.Object;
        try
        {
            vsProject.References.Add(assemblyName);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    private static string? GetAnalyzerPath(AnalyzerReference analyzerReference)
        => analyzerReference.FullPath;

    protected override void ApplyCompilationOptionsChanged(ProjectId projectId, CompilationOptions options)
    {
        if (projectId == null)
        {
            throw new ArgumentNullException(nameof(projectId));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var originalProject = CurrentSolution.GetRequiredProject(projectId);
        var compilationOptionsService = originalProject.Services.GetRequiredService<ICompilationOptionsChangingService>();
        var storage = ProjectPropertyStorage.Create(TryGetDTEProject(projectId), ServiceProvider.GlobalProvider);
        compilationOptionsService.Apply(originalProject.CompilationOptions!, options, storage);
    }

    protected override void ApplyParseOptionsChanged(ProjectId projectId, ParseOptions options)
    {
        if (projectId == null)
        {
            throw new ArgumentNullException(nameof(projectId));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var parseOptionsService = CurrentSolution.GetRequiredProject(projectId).Services.GetRequiredService<IParseOptionsChangingService>();
        var storage = ProjectPropertyStorage.Create(TryGetDTEProject(projectId), ServiceProvider.GlobalProvider);
        parseOptionsService.Apply(options, storage);
    }

    protected override void ApplyAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference)
    {
        if (projectId == null)
        {
            throw new ArgumentNullException(nameof(projectId));
        }

        if (analyzerReference == null)
        {
            throw new ArgumentNullException(nameof(analyzerReference));
        }

        GetProjectData(projectId, out _, out var project);

        var filePath = GetAnalyzerPath(analyzerReference);
        if (filePath != null)
        {
            var vsProject = (VSProject3)project.Object;
            vsProject.AnalyzerReferences.Add(filePath);
        }
    }

    protected override void ApplyAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference)
    {
        if (projectId == null)
        {
            throw new ArgumentNullException(nameof(projectId));
        }

        if (analyzerReference == null)
        {
            throw new ArgumentNullException(nameof(analyzerReference));
        }

        GetProjectData(projectId, out _, out var project);

        var filePath = GetAnalyzerPath(analyzerReference);
        if (filePath != null)
        {
            var vsProject = (VSProject3)project.Object;
            vsProject.AnalyzerReferences.Remove(filePath);
        }
    }

    private static string? GetMetadataPath(MetadataReference metadataReference)
    {
        if (metadataReference is PortableExecutableReference fileMetadata)
        {
            return fileMetadata.FilePath;
        }

        return null;
    }

    protected override void ApplyMetadataReferenceAdded(
        ProjectId projectId, MetadataReference metadataReference)
    {
        if (projectId == null)
        {
            throw new ArgumentNullException(nameof(projectId));
        }

        if (metadataReference == null)
        {
            throw new ArgumentNullException(nameof(metadataReference));
        }

        GetProjectData(projectId, out _, out var project);

        var filePath = GetMetadataPath(metadataReference);
        if (filePath != null)
        {
            var vsProject = (VSProject)project.Object;
            vsProject.References.Add(filePath);

            var undoManager = TryGetUndoManager();
            undoManager?.Add(new RemoveMetadataReferenceUndoUnit(this, projectId, filePath));
        }
    }

    protected override void ApplyMetadataReferenceRemoved(
        ProjectId projectId, MetadataReference metadataReference)
    {
        if (projectId == null)
        {
            throw new ArgumentNullException(nameof(projectId));
        }

        if (metadataReference == null)
        {
            throw new ArgumentNullException(nameof(metadataReference));
        }

        GetProjectData(projectId, out _, out var project);

        var filePath = GetMetadataPath(metadataReference);
        if (filePath != null)
        {
            var vsProject = (VSProject)project.Object;
            foreach (Reference reference in vsProject.References)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(reference.Path, filePath))
                {
                    reference.Remove();
                    var undoManager = TryGetUndoManager();
                    undoManager?.Add(new AddMetadataReferenceUndoUnit(this, projectId, filePath));
                    break;
                }
            }
        }
    }

    internal override void ApplyMappedFileChanges(SolutionChanges solutionChanges)
    {
        // Get the original text changes from all documents and call the span mapping service to get span mappings for the text changes.
        // Create mapped text changes using the mapped spans and original text changes' text.

        // Mappings for opened razor files are retrieved via the LSP client making a request to the razor server.
        // If we wait for the result on the UI thread, we will hit a bug in the LSP client that brings us to a code path
        // using ConfigureAwait(true).  This deadlocks as it then attempts to return to the UI thread which is already blocked by us.
        // Instead, we invoke this in JTF run which will mitigate deadlocks when the ConfigureAwait(true)
        // tries to switch back to the main thread in the LSP client.
        // Link to LSP client bug for ConfigureAwait(true) - https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1216657
        var mappedChanges = _threadingContext.JoinableTaskFactory.Run(() => GetMappedTextChangesAsync(solutionChanges));

        // Group the mapped text changes by file, then apply all mapped text changes for the file.
        foreach (var changesForFile in mappedChanges)
        {
            // It doesn't matter which of the file's projectIds we pass to the invisible editor, so just pick the first.
            var projectId = changesForFile.Value.First().ProjectId;
            // Make sure we only take distinct changes - we'll have duplicates from different projects for linked files or multi-targeted files.
            var distinctTextChanges = changesForFile.Value.Select(change => change.TextChange).Distinct().ToImmutableArray();
            using var invisibleEditor = new InvisibleEditor(ServiceProvider.GlobalProvider, changesForFile.Key, GetHierarchy(projectId), needsSave: true, needsUndoDisabled: false);
            TextEditApplication.UpdateText(distinctTextChanges, invisibleEditor.TextBuffer, EditOptions.None);
        }

        return;

        async Task<MultiDictionary<string, (TextChange TextChange, ProjectId ProjectId)>> GetMappedTextChangesAsync(SolutionChanges solutionChanges)
        {
            var filePathToMappedTextChanges = new MultiDictionary<string, (TextChange TextChange, ProjectId ProjectId)>();
            foreach (var projectChanges in solutionChanges.GetProjectChanges())
            {
                foreach (var changedDocumentId in projectChanges.GetChangedDocuments())
                {
                    var oldDocument = projectChanges.OldProject.GetRequiredDocument(changedDocumentId);
                    if (!ShouldApplyChangesToMappedDocuments(oldDocument, out var mappingService))
                    {
                        continue;
                    }

                    var newDocument = projectChanges.NewProject.GetRequiredDocument(changedDocumentId);
                    var mappedTextChanges = await mappingService.GetMappedTextChangesAsync(
                        oldDocument, newDocument, CancellationToken.None).ConfigureAwait(false);
                    foreach (var (filePath, textChange) in mappedTextChanges)
                    {
                        filePathToMappedTextChanges.Add(filePath, (textChange, projectChanges.ProjectId));
                    }
                }
            }

            return filePathToMappedTextChanges;
        }

        bool ShouldApplyChangesToMappedDocuments(CodeAnalysis.Document document, [NotNullWhen(true)] out ISpanMappingService? spanMappingService)
        {
            spanMappingService = document.Services.GetService<ISpanMappingService>();
            // Only consider files that are mapped and that we are unable to apply changes to.
            // TODO - refactor how this is determined - https://github.com/dotnet/roslyn/issues/47908
            return spanMappingService != null && document?.CanApplyChange() == false;
        }
    }

    protected override void ApplyProjectReferenceAdded(
        ProjectId projectId, ProjectReference projectReference)
    {
        if (projectId == null)
        {
            throw new ArgumentNullException(nameof(projectId));
        }

        if (projectReference == null)
        {
            throw new ArgumentNullException(nameof(projectReference));
        }

        GetProjectData(projectId, out _, out var project);
        GetProjectData(projectReference.ProjectId, out _, out var refProject);

        var vsProject = (VSProject)project.Object;
        vsProject.References.AddProject(refProject);

        var undoManager = TryGetUndoManager();
        undoManager?.Add(new RemoveProjectReferenceUndoUnit(
            this, projectId, projectReference.ProjectId));
    }

    private OleInterop.IOleUndoManager? TryGetUndoManager()
    {
        var documentTrackingService = this.Services.GetRequiredService<IDocumentTrackingService>();
        var documentId = documentTrackingService.TryGetActiveDocument() ?? documentTrackingService.GetVisibleDocuments().FirstOrDefault();
        if (documentId != null)
        {
            var composition = (IComponentModel)ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel));
            var exportProvider = composition.DefaultExportProvider;
            var editorAdaptersService = exportProvider.GetExportedValue<IVsEditorAdaptersFactoryService>();

            return editorAdaptersService.TryGetUndoManager(this, documentId, CancellationToken.None);
        }

        return null;
    }

    protected override void ApplyProjectReferenceRemoved(
        ProjectId projectId, ProjectReference projectReference)
    {
        if (projectId == null)
        {
            throw new ArgumentNullException(nameof(projectId));
        }

        if (projectReference == null)
        {
            throw new ArgumentNullException(nameof(projectReference));
        }

        GetProjectData(projectId, out _, out var project);
        GetProjectData(projectReference.ProjectId, out _, out var refProject);

        var vsProject = (VSProject)project.Object;
        foreach (Reference reference in vsProject.References)
        {
            if (reference.SourceProject == refProject)
            {
                reference.Remove();
                var undoManager = TryGetUndoManager();
                undoManager?.Add(new AddProjectReferenceUndoUnit(this, projectId, projectReference.ProjectId));
            }
        }
    }

    protected override void ApplyDocumentAdded(DocumentInfo info, SourceText text)
        => AddDocumentCore(info, text, TextDocumentKind.Document);

    protected override void ApplyAdditionalDocumentAdded(DocumentInfo info, SourceText text)
        => AddDocumentCore(info, text, TextDocumentKind.AdditionalDocument);

    protected override void ApplyAnalyzerConfigDocumentAdded(DocumentInfo info, SourceText text)
    {
        if (!TryAddEditorConfigToSolutionItems(info, text))
            AddDocumentCore(info, text, TextDocumentKind.AnalyzerConfigDocument);
    }

    private void AddDocumentCore(DocumentInfo info, SourceText initialText, TextDocumentKind documentKind)
    {
        GetProjectData(info.Id.ProjectId, out _, out var project);

        // If the first namespace name matches the name of the project, then we don't want to
        // generate a folder for that.  The project is implicitly a folder with that name.
        var folders = info.Folders.AsEnumerable();
        if (folders.FirstOrDefault() == project.Name)
        {
            folders = folders.Skip(1);
        }

        folders = FilterFolderForProjectType(project, folders);

        if (IsWebsite(project))
        {
            AddDocumentToFolder(project, info.Id, SpecializedCollections.SingletonEnumerable(AppCodeFolderName), info.Name, documentKind, initialText, info.FilePath);
        }
        else if (folders.Any())
        {
            AddDocumentToFolder(project, info.Id, folders, info.Name, documentKind, initialText, info.FilePath);
        }
        else
        {
            AddDocumentToProject(project, info.Id, info.Name, documentKind, initialText, info.FilePath);
        }

        var undoManager = TryGetUndoManager();

        switch (documentKind)
        {
            case TextDocumentKind.AdditionalDocument:
                undoManager?.Add(new RemoveAdditionalDocumentUndoUnit(this, info.Id));
                break;

            case TextDocumentKind.AnalyzerConfigDocument:
                undoManager?.Add(new RemoveAnalyzerConfigDocumentUndoUnit(this, info.Id));
                break;

            case TextDocumentKind.Document:
                undoManager?.Add(new RemoveDocumentUndoUnit(this, info.Id));
                break;

            default:
                throw ExceptionUtilities.UnexpectedValue(documentKind);
        }
    }

    private static bool IsWebsite(EnvDTE.Project project)
        => project.Kind == VsWebSite.PrjKind.prjKindVenusProject;

    private static IEnumerable<string> FilterFolderForProjectType(EnvDTE.Project project, IEnumerable<string> folders)
    {
        foreach (var folder in folders)
        {
            var items = GetAllItems(project.ProjectItems);
            var folderItem = items.FirstOrDefault(p => StringComparer.OrdinalIgnoreCase.Compare(p.Name, folder) == 0);
            if (folderItem == null || folderItem.Kind != EnvDTE.Constants.vsProjectItemKindPhysicalFile)
            {
                yield return folder;
            }
        }
    }

    private static IEnumerable<ProjectItem> GetAllItems(ProjectItems projectItems)
    {
        if (projectItems == null)
        {
            return SpecializedCollections.EmptyEnumerable<ProjectItem>();
        }

        var items = projectItems.OfType<ProjectItem>();
        return items.Concat(items.SelectMany(i => GetAllItems(i.ProjectItems)));
    }

#if false
    protected override void AddExistingDocument(DocumentId documentId, string filePath, IEnumerable<string> folders)
    {
        IVsHierarchy hierarchy;
        EnvDTE.Project project;
        IVisualStudioHostProject hostProject;
        GetProjectData(documentId.ProjectId, out hostProject, out hierarchy, out project);

        // If the first namespace name matches the name of the project, then we don't want to
        // generate a folder for that.  The project is implicitly a folder with that name.
        if (folders.FirstOrDefault() == project.Name)
        {
            folders = folders.Skip(1);
        }

        var name = Path.GetFileName(filePath);

        if (folders.Any())
        {
            AddDocumentToFolder(hostProject, project, documentId, folders, name, SourceCodeKind.Regular, initialText: null, filePath: filePath);
        }
        else
        {
            AddDocumentToProject(hostProject, project, documentId, name, SourceCodeKind.Regular, initialText: null, filePath: filePath);
        }
    }
#endif

    private void AddDocumentToProject(
        EnvDTE.Project project,
        DocumentId documentId,
        string documentName,
        TextDocumentKind documentKind,
        SourceText? initialText = null,
        string? filePath = null)
    {
        string? folderPath = null;
        if (filePath == null && !project.TryGetFullPath(out folderPath))
        {
            // TODO(cyrusn): Throw an appropriate exception here.
            throw new Exception(ServicesVSResources.Could_not_find_location_of_folder_on_disk);
        }

        AddDocumentToProjectItems(project.ProjectItems, documentId, folderPath, documentName, initialText, filePath, documentKind);
    }

    private void AddDocumentToFolder(
        EnvDTE.Project project,
        DocumentId documentId,
        IEnumerable<string> folders,
        string documentName,
        TextDocumentKind documentKind,
        SourceText? initialText = null,
        string? filePath = null)
    {
        var folder = project.FindOrCreateFolder(folders);

        string? folderPath = null;
        if (filePath == null && !folder.TryGetFullPath(out folderPath))
        {
            // TODO(cyrusn): Throw an appropriate exception here.
            throw new Exception(ServicesVSResources.Could_not_find_location_of_folder_on_disk);
        }

        AddDocumentToProjectItems(folder.ProjectItems, documentId, folderPath, documentName, initialText, filePath, documentKind);
    }

    private bool TryAddEditorConfigToSolutionItems(
        DocumentInfo documentInfo,
        SourceText text)
    {
        // We're going to see if this is an .editorconfig being added at the solution level, and if so process it specially; so verify first that it is one
        if (CurrentSolution.FilePath is null || documentInfo.FilePath is null)
            return false;

        if (PathUtilities.GetFileName(documentInfo.FilePath) != ".editorconfig")
            return false;

        if (IOUtilities.PerformIO(() => File.Exists(documentInfo.FilePath)))
            return false;

        var solutionDirectory = PathUtilities.GetDirectoryName(this.CurrentSolution.FilePath);
        if (solutionDirectory != PathUtilities.GetDirectoryName(documentInfo.FilePath))
            return false;

        // Double check too that this isn't a case of the .csproj being in the same folder of the .sln, at which point it's reasonable
        // just to add this to the project file.
        if (PathUtilities.GetDirectoryName(CurrentSolution.GetProject(documentInfo.Id.ProjectId)?.FilePath) == solutionDirectory)
            return false;

        // All checks pass, so let's treat this special.
        var dte = _threadingContext.JoinableTaskFactory.Run(() => _asyncServiceProvider.GetServiceAsync<SDTE, EnvDTE.DTE>(_threadingContext.JoinableTaskFactory));

        const string SolutionItemsFolderName = "Solution Items";

        var projects = dte.Solution.Projects.OfType<EnvDTE.Project>();
        var solutionItemsFolder = projects.FirstOrDefault(static p => p.Kind == EnvDTE.Constants.vsProjectKindSolutionItems && p.Name == SolutionItemsFolderName);

        if (solutionItemsFolder != null)
        {
            foreach (ProjectItem projectItem in solutionItemsFolder.ProjectItems)
            {
                if (projectItem.Name == documentInfo.Name)
                {
                    // It's already added to the solution folder, we just need to write the text and be done
                    using var writer = new StreamWriter(documentInfo.FilePath, append: false, encoding: text.Encoding ?? Encoding.UTF8);
                    text.Write(writer);
                    return true;
                }
            }
        }
        else
        {
            solutionItemsFolder = ((EnvDTE80.Solution2)dte.Solution).AddSolutionFolder(SolutionItemsFolderName);
        }

        AddDocumentToProjectItems(
            solutionItemsFolder.ProjectItems,
            documentInfo.Id,
            folderPath: null,
            documentInfo.Name,
            text,
            documentInfo.FilePath,
            TextDocumentKind.AnalyzerConfigDocument);
        dte.Solution.SaveAs(dte.Solution.FileName);

        return true;
    }

    private void AddDocumentToProjectItems(
        ProjectItems projectItems,
        DocumentId documentId,
        string? folderPath,
        string documentName,
        SourceText? initialText,
        string? filePath,
        TextDocumentKind documentKind)
    {
        if (filePath == null)
        {
            Contract.ThrowIfNull(folderPath, "If we didn't have a file path, then we expected a folder path to generate the file path from.");
            var baseName = Path.GetFileNameWithoutExtension(documentName);
            var extension = documentKind == TextDocumentKind.Document ? GetPreferredExtension(documentId) : Path.GetExtension(documentName);
            var uniqueName = projectItems.GetUniqueName(baseName, extension);
            filePath = Path.Combine(folderPath, uniqueName);
        }

        if (initialText != null)
        {
            using var writer = new StreamWriter(filePath, append: false, encoding: initialText.Encoding ?? Encoding.UTF8);
            initialText.Write(writer);
        }

        // TODO: restore document ID hinting -- we previously ensured that the AddFromFile will introduce the document ID being used here.
        // (tracked by https://devdiv.visualstudio.com/DevDiv/_workitems/edit/677956)
        projectItems.AddFromFile(filePath);
    }

    private void RemoveDocumentCore(DocumentId documentId, TextDocumentKind documentKind)
    {
        if (documentId == null)
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        var document = this.CurrentSolution.GetTextDocument(documentId);
        if (document != null)
        {
            var hierarchy = this.GetHierarchy(documentId.ProjectId);
            Contract.ThrowIfNull(hierarchy, "Removing files from projects without hierarchies are not supported.");

            var text = document.GetTextSynchronously(CancellationToken.None);

            Contract.ThrowIfNull(document.FilePath, "Removing files from projects that don't have file names are not supported.");
            var itemId = hierarchy.TryGetItemId(document.FilePath);
            if (itemId == (uint)VSConstants.VSITEMID.Nil)
            {
                // it is no longer part of the solution
                return;
            }

            var project = (IVsProject3)hierarchy;
            project.RemoveItem(0, itemId, out _);

            var undoManager = TryGetUndoManager();
            var docInfo = CreateDocumentInfoWithoutText(document);

            switch (documentKind)
            {
                case TextDocumentKind.AdditionalDocument:
                    undoManager?.Add(new AddAdditionalDocumentUndoUnit(this, docInfo, text));
                    break;

                case TextDocumentKind.AnalyzerConfigDocument:
                    undoManager?.Add(new AddAnalyzerConfigDocumentUndoUnit(this, docInfo, text));
                    break;

                case TextDocumentKind.Document:
                    undoManager?.Add(new AddDocumentUndoUnit(this, docInfo, text));
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(documentKind);
            }
        }
    }

    protected override void ApplyDocumentRemoved(DocumentId documentId)
        => RemoveDocumentCore(documentId, TextDocumentKind.Document);

    protected override void ApplyAdditionalDocumentRemoved(DocumentId documentId)
        => RemoveDocumentCore(documentId, TextDocumentKind.AdditionalDocument);

    protected override void ApplyAnalyzerConfigDocumentRemoved(DocumentId documentId)
        => RemoveDocumentCore(documentId, TextDocumentKind.AnalyzerConfigDocument);

    public override void OpenDocument(DocumentId documentId, bool activate = true)
        => OpenDocumentCore(documentId, activate);

    public override void OpenAdditionalDocument(DocumentId documentId, bool activate = true)
        => OpenDocumentCore(documentId, activate);

    public override void OpenAnalyzerConfigDocument(DocumentId documentId, bool activate = true)
        => OpenDocumentCore(documentId, activate);

    public override void CloseDocument(DocumentId documentId)
        => CloseDocumentCore(documentId);

    public override void CloseAdditionalDocument(DocumentId documentId)
        => CloseDocumentCore(documentId);

    public override void CloseAnalyzerConfigDocument(DocumentId documentId)
        => CloseDocumentCore(documentId);

    public void OpenDocumentCore(DocumentId documentId, bool activate = true)
    {
        if (documentId == null)
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        if (!_foregroundObject.IsForeground())
        {
            throw new InvalidOperationException(ServicesVSResources.This_workspace_only_supports_opening_documents_on_the_UI_thread);
        }

        var document = this.CurrentSolution.GetTextDocument(documentId);
        if (document != null)
        {
            OpenDocumentFromPath(document.FilePath, document.Project.Id, activate);
        }
    }

    internal void OpenDocumentFromPath(string? filePath, ProjectId projectId, bool activate = true)
    {
        if (TryGetFrame(filePath, projectId, out var frame))
        {
            if (activate)
            {
                frame.Show();
            }
            else
            {
                frame.ShowNoActivate();
            }
        }
    }

    /// <summary>
    /// Opens a file and retrieves the window frame.
    /// </summary>
    /// <param name="filePath">the file path of the file to open.</param>
    /// <param name="projectId">used to retrieve the IVsHierarchy to ensure the file is opened in a matching context.</param>
    /// <param name="frame">the window frame.</param>
    /// <returns></returns>
    private bool TryGetFrame(string? filePath, ProjectId projectId, [NotNullWhen(returnValue: true)] out IVsWindowFrame? frame)
    {
        frame = null;

        if (filePath == null)
        {
            return false;
        }

        var hierarchy = GetHierarchy(projectId);
        var itemId = hierarchy?.TryGetItemId(filePath) ?? (uint)VSConstants.VSITEMID.Nil;
        if (itemId == (uint)VSConstants.VSITEMID.Nil)
        {
            // If the ItemId is Nil, then IVsProject would not be able to open the
            // document using its ItemId. Thus, we must use OpenDocumentViaProject, which only
            // depends on the file path.

            var openDocumentService = ServiceProvider.GlobalProvider.GetServiceOnMainThread<SVsUIShellOpenDocument, IVsUIShellOpenDocument>();
            return ErrorHandler.Succeeded(openDocumentService.OpenDocumentViaProject(
                filePath,
                VSConstants.LOGVIEWID.TextView_guid,
                out _,
                out _,
                out _,
                out frame));
        }
        else
        {
            // If the ItemId is not Nil, then we should not call IVsUIShellDocument
            // .OpenDocumentViaProject here because that simply takes a file path and opens the
            // file within the context of the first project it finds. That would cause problems
            // if the document we're trying to open is actually a linked file in another
            // project. So, we get the project's hierarchy and open the document using its item
            // ID.

            // It's conceivable that IVsHierarchy might not implement IVsProject. However,
            // OpenDocumentViaProject itself relies upon this QI working, so it should be OK to
            // use here.

            return hierarchy is IVsProject vsProject &&
                ErrorHandler.Succeeded(vsProject.OpenItem(itemId, VSConstants.LOGVIEWID.TextView_guid, s_docDataExisting_Unknown, out frame));
        }
    }

    public void CloseDocumentCore(DocumentId documentId)
    {
        if (documentId == null)
        {
            throw new ArgumentNullException(nameof(documentId));
        }

        if (this.IsDocumentOpen(documentId))
        {
            var filePath = this.GetFilePath(documentId);
            if (filePath != null)
            {
                var openDocumentService = ServiceProvider.GlobalProvider.GetServiceOnMainThread<SVsUIShellOpenDocument, IVsUIShellOpenDocument>();
                if (ErrorHandler.Succeeded(openDocumentService.IsDocumentOpen(null, 0, filePath, Guid.Empty, 0, out _, null, out var frame, out _)))
                {
                    // TODO: do we need save argument for CloseDocument?
                    frame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
                }
            }
        }
    }

    protected override void ApplyDocumentTextChanged(DocumentId documentId, SourceText newText)
        => ApplyTextDocumentChange(documentId, newText);

    protected override void ApplyAdditionalDocumentTextChanged(DocumentId documentId, SourceText newText)
        => ApplyTextDocumentChange(documentId, newText);

    protected override void ApplyAnalyzerConfigDocumentTextChanged(DocumentId documentId, SourceText newText)
        => ApplyTextDocumentChange(documentId, newText);

    private void ApplyTextDocumentChange(DocumentId documentId, SourceText newText)
    {
        var containedDocument = ContainedDocument.TryGetContainedDocument(documentId);

        if (containedDocument != null)
        {
            containedDocument.UpdateText(newText);
        }
        else
        {
            if (IsDocumentOpen(documentId))
            {
                var textBuffer = this.CurrentSolution.GetTextDocument(documentId)!.GetTextSynchronously(CancellationToken.None).Container.TryGetTextBuffer();

                if (textBuffer != null)
                {
                    TextEditApplication.UpdateText(newText, textBuffer, EditOptions.DefaultMinimalChange);
                    return;
                }
            }

            // The document wasn't open in a normal way, so invisible editor time
            using var invisibleEditor = OpenInvisibleEditor(documentId);
            TextEditApplication.UpdateText(newText, invisibleEditor.TextBuffer, EditOptions.None);
        }
    }

    protected override void ApplyDocumentInfoChanged(DocumentId documentId, DocumentInfo updatedInfo)
    {
        var document = CurrentSolution.GetRequiredDocument(documentId);

        FailIfDocumentInfoChangesNotSupported(document, updatedInfo);

        if (document.Name != updatedInfo.Name)
        {
            GetProjectData(updatedInfo.Id.ProjectId, out var _, out var dteProject);

            if (document.FilePath == null)
            {
                FatalError.ReportAndCatch(new Exception("Attempting to change the information of a document without a file path."));
                return;
            }

            var projectItemForDocument = dteProject.FindItemByPath(document.FilePath, StringComparer.OrdinalIgnoreCase);

            if (projectItemForDocument == null)
            {
                // TODO(https://github.com/dotnet/roslyn/issues/34276):
                Debug.Fail("Attempting to change the name of a file in a Shared Project");
                return;
            }

            // Must save the document first for things like Breakpoints to be preserved.
            // WORKAROUND: Check if the document needs to be saved before calling save. 
            // Should remove the if below and just call save() once 
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1163405
            // is fixed
            if (!projectItemForDocument.Saved)
            {
                projectItemForDocument.Save();
            }

            var uniqueName = projectItemForDocument.Collection
                .GetUniqueNameIgnoringProjectItem(
                    projectItemForDocument,
                    Path.GetFileNameWithoutExtension(updatedInfo.Name),
                    Path.GetExtension(updatedInfo.Name));

            // Get the current undoManager before any file renames/documentId changes happen
            var undoManager = TryGetUndoManager();

            // By setting this property, Visual Studio will perform the file rename, which 
            // will cause the workspace's current solution to update and will fire the 
            // necessary workspace changed events.
            projectItemForDocument.Name = uniqueName;

            if (projectItemForDocument.TryGetFullPath(out var newPath))
            {
                undoManager?.Add(new RenameDocumentUndoUnit(this, uniqueName, document.Name, newPath));
            }
        }
    }

    /// <summary>
    /// The <see cref="VisualStudioWorkspace"/> currently supports only a subset of <see cref="DocumentInfo"/> 
    /// changes.
    /// </summary>
    private static void FailIfDocumentInfoChangesNotSupported(CodeAnalysis.Document document, DocumentInfo updatedInfo)
    {
        if (document.SourceCodeKind != updatedInfo.SourceCodeKind)
        {
            throw new InvalidOperationException(
                $"This Workspace does not support changing a document's {nameof(document.SourceCodeKind)}.");
        }

        if (document.FilePath != updatedInfo.FilePath)
        {
            throw new InvalidOperationException(
                $"This Workspace does not support changing a document's {nameof(document.FilePath)}.");
        }

        if (document.Id != updatedInfo.Id)
        {
            throw new InvalidOperationException(
                $"This Workspace does not support changing a document's {nameof(document.Id)}.");
        }

        if (document.Folders != updatedInfo.Folders && !document.Folders.SequenceEqual(updatedInfo.Folders))
        {
            throw new InvalidOperationException(
                $"This Workspace does not support changing a document's {nameof(document.Folders)}.");
        }

        if (document.State.Attributes.IsGenerated != updatedInfo.IsGenerated)
        {
            throw new InvalidOperationException(
                $"This Workspace does not support changing a document's {nameof(document.State.Attributes.IsGenerated)} state.");
        }
    }

    private string GetPreferredExtension(DocumentId documentId)
    {
        // No extension was provided.  Pick a good one based on the type of host project.
        return CurrentSolution.GetRequiredProject(documentId.ProjectId).Language switch
        {
            // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
            //return sourceCodeKind == SourceCodeKind.Regular ? ".cs" : ".csx";
            LanguageNames.CSharp => ".cs",

            // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
            //return sourceCodeKind == SourceCodeKind.Regular ? ".vb" : ".vbx";
            LanguageNames.VisualBasic => ".vb",
            _ => throw new InvalidOperationException(),
        };
    }

    public override IVsHierarchy? GetHierarchy(ProjectId projectId)
    {
        // This doesn't take a lock since _projectToHierarchyMap is immutable
        return _projectToHierarchyMap.GetValueOrDefault(projectId, defaultValue: null);
    }

    internal override Guid GetProjectGuid(ProjectId projectId)
    {
        // This doesn't take a lock since _projectToGuidMap is immutable
        return _projectToGuidMap.GetValueOrDefault(projectId, defaultValue: Guid.Empty);
    }

    internal string? TryGetDependencyNodeTargetIdentifier(ProjectId projectId)
    {
        return ProjectSystemProjectFactory.TryGetDependencyNodeTargetIdentifier(projectId);
    }

    internal override void SetDocumentContext(DocumentId documentId)
    {
        _foregroundObject.AssertIsForeground();

        // Note: this method does not actually call into any workspace code here to change the workspace's context. The assumption is updating the running document table or
        // IVsHierarchies will raise the appropriate events which we are subscribed to.

        var hierarchy = GetHierarchy(documentId.ProjectId);
        if (hierarchy == null)
        {
            // If we don't have a hierarchy then there's nothing we can do
            return;
        }

        // The hierarchy might be supporting multitargeting; in that case, let's update the context. Unfortunately the IVsHierarchies that support this
        // don't necessarily let us read it first, so we have to fire-and-forget here.
        string? projectSystemNameForProjectId = null;

        using (_gate.DisposableWait())
        {
            foreach (var (projectSystemName, projects) in _projectSystemNameToProjectsMap)
            {
                if (projects.Any(p => p.Id == documentId.ProjectId))
                {
                    projectSystemNameForProjectId = projectSystemName;
                }
            }
        }

        if (projectSystemNameForProjectId is null)
        {
            // Project must have been removed asynchronously
            return;
        }

        // The hierarchy might be supporting multitargeting; in that case, let's update the context. Unfortunately the IVsHierarchies that support this
        // don't necessarily let us read it first, so we have to fire-and-forget here.
        hierarchy.SetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID8.VSHPROPID_ActiveIntellisenseProjectContext, projectSystemNameForProjectId);

        var filePath = GetFilePath(documentId);
        if (filePath == null)
        {
            return;
        }

        var itemId = hierarchy.TryGetItemId(filePath);
        if (itemId != VSConstants.VSITEMID_NIL)
        {
            // Is this owned by a shared asset project? If so, we need to put the shared asset project into the running document table, and need to set the
            // current hierarchy as the active context of that shared hierarchy. This is kept as a loop that we do multiple times in the case that you
            // have multiple pointers. This used to be the case for multitargeting projects, but that was now handled by setting the active context property
            // above. Some project systems out there might still be supporting it, so we'll support it too.
            while (SharedProjectUtilities.TryGetItemInSharedAssetsProject(hierarchy, itemId, out var sharedHierarchy, out var sharedItemId) &&
                   hierarchy != sharedHierarchy)
            {
                // Ensure the shared context is set correctly
                if (sharedHierarchy.GetActiveProjectContext() != hierarchy)
                {
                    ErrorHandler.ThrowOnFailure(sharedHierarchy.SetActiveProjectContext(hierarchy));
                }

                // We now need to ensure the outer project is also set up
                hierarchy = sharedHierarchy;
                itemId = sharedItemId;
            }
        }

        // Update the ownership of the file in the Running Document Table
        var project = (IVsProject3)hierarchy;
        project.TransferItem(filePath, filePath, punkWindowFrame: null);
    }

    internal bool TryGetHierarchy(ProjectId projectId, [NotNullWhen(returnValue: true)] out IVsHierarchy? hierarchy)
    {
        hierarchy = this.GetHierarchy(projectId);
        return hierarchy != null;
    }

    protected override void Dispose(bool finalize)
    {
        if (!finalize)
        {
            _textBufferFactoryService.TextBufferCreated -= AddTextBufferCloneServiceToBuffer;
            _projectionBufferFactoryService.ProjectionBufferCreated -= AddTextBufferCloneServiceToBuffer;

            if (_lazyExternalErrorDiagnosticUpdateSource.IsValueCreated)
            {
                _lazyExternalErrorDiagnosticUpdateSource.Value.Dispose();
            }
        }

        base.Dispose(finalize);
    }

    public virtual void EnsureEditableDocuments(IEnumerable<DocumentId> documents)
    {
        var queryEdit = (IVsQueryEditQuerySave2)ServiceProvider.GlobalProvider.GetService(typeof(SVsQueryEditQuerySave));

        // make sure given document id actually exist in current solution and the file is marked as supporting modifications
        // and actually has non null file path
        var fileNames = documents.Select(GetFilePath).ToArray();

        // TODO: meditate about the flags we can pass to this and decide what is most appropriate for Roslyn
        var result = queryEdit.QueryEditFiles(
            rgfQueryEdit: 0,
            cFiles: fileNames.Length,
            rgpszMkDocuments: fileNames,
            rgrgf: new uint[fileNames.Length],
            rgFileInfo: new VSQEQS_FILE_ATTRIBUTE_DATA[fileNames.Length],
            pfEditVerdict: out var editVerdict,
            prgfMoreInfo: out var editResultFlags);

        if (ErrorHandler.Failed(result) ||
            editVerdict != (uint)tagVSQueryEditResult.QER_EditOK)
        {
            throw new Exception("Unable to check out the files from source control.");
        }

        if ((editResultFlags & (uint)(tagVSQueryEditResultFlags2.QER_Changed | tagVSQueryEditResultFlags2.QER_Reloaded)) != 0)
        {
            throw new Exception("A file was reloaded during the source control checkout.");
        }
    }

    internal override bool CanAddProjectReference(ProjectId referencingProject, ProjectId referencedProject)
    {
        _foregroundObject.AssertIsForeground();

        if (!TryGetHierarchy(referencingProject, out var referencingHierarchy) ||
            !TryGetHierarchy(referencedProject, out var referencedHierarchy))
        {
            // Couldn't even get a hierarchy for this project. So we have to assume
            // that adding a reference is disallowed.
            return false;
        }

        // First we have to see if either project disallows the reference being added.
        const int ContextFlags = (int)__VSQUERYFLAVORREFERENCESCONTEXT.VSQUERYFLAVORREFERENCESCONTEXT_RefreshReference;

        var canAddProjectReference = (uint)__VSREFERENCEQUERYRESULT.REFERENCE_UNKNOWN;
        var canBeReferenced = (uint)__VSREFERENCEQUERYRESULT.REFERENCE_UNKNOWN;

        if (referencingHierarchy is IVsProjectFlavorReferences3 referencingProjectFlavor3)
        {
            if (ErrorHandler.Failed(referencingProjectFlavor3.QueryAddProjectReferenceEx(referencedHierarchy, ContextFlags, out canAddProjectReference, out _)))
            {
                // Something went wrong even trying to see if the reference would be allowed.
                // Assume it won't be allowed.
                return false;
            }

            if (canAddProjectReference == (uint)__VSREFERENCEQUERYRESULT.REFERENCE_DENY)
            {
                // Adding this project reference is not allowed.
                return false;
            }
        }

        if (referencedHierarchy is IVsProjectFlavorReferences3 referencedProjectFlavor3)
        {
            if (ErrorHandler.Failed(referencedProjectFlavor3.QueryCanBeReferencedEx(referencingHierarchy, ContextFlags, out canBeReferenced, out _)))
            {
                // Something went wrong even trying to see if the reference would be allowed.
                // Assume it won't be allowed.
                return false;
            }

            if (canBeReferenced == (uint)__VSREFERENCEQUERYRESULT.REFERENCE_DENY)
            {
                // Adding this project reference is not allowed.
                return false;
            }
        }

        // Neither project denied the reference being added.  At this point, if either project
        // allows the reference to be added, and the other doesn't block it, then we can add
        // the reference.
        if (canAddProjectReference == (int)__VSREFERENCEQUERYRESULT.REFERENCE_ALLOW ||
            canBeReferenced == (int)__VSREFERENCEQUERYRESULT.REFERENCE_ALLOW)
        {
            return true;
        }

        // In both directions things are still unknown.  Fallback to the reference manager
        // to make the determination here.
        var referenceManager = (IVsReferenceManager)ServiceProvider.GlobalProvider.GetService(typeof(SVsReferenceManager));
        if (referenceManager == null)
        {
            // Couldn't get the reference manager.  Have to assume it's not allowed.
            return false;
        }

        // As long as the reference manager does not deny things, then we allow the
        // reference to be added.
        var result = referenceManager.QueryCanReferenceProject(referencingHierarchy, referencedHierarchy);
        return result != (uint)__VSREFERENCEQUERYRESULT.REFERENCE_DENY;
    }

    internal void RemoveProjectFromMaps(CodeAnalysis.Project project)
    {
        foreach (var (projectName, projects) in _projectSystemNameToProjectsMap)
        {
            if (projects.RemoveAll(p => p.Id == project.Id) > 0)
            {
                if (projects.Count == 0)
                {
                    _projectSystemNameToProjectsMap.Remove(projectName);
                }

                break;
            }
        }

        _projectToHierarchyMap = _projectToHierarchyMap.Remove(project.Id);
        _projectToGuidMap = _projectToGuidMap.Remove(project.Id);

        ImmutableInterlocked.TryRemove(ref _projectToRuleSetFilePath, project.Id, out _);

        // Try to update the UI context info.  But cancel that work if we're shutting down.
        _threadingContext.RunWithShutdownBlockAsync(async cancellationToken =>
        {
            using var asyncToken = _workspaceListener.BeginAsyncOperation(nameof(RefreshProjectExistsUIContextForLanguageAsync));
            await RefreshProjectExistsUIContextForLanguageAsync(project.Language, cancellationToken).ConfigureAwait(false);
        });
    }

    internal async Task RefreshProjectExistsUIContextForLanguageAsync(string language, CancellationToken cancellationToken)
    {
        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, cancellationToken);

        var uiContext = _languageToProjectExistsUIContext.GetOrAdd(
            language,
            language => Services.GetLanguageServices(language).GetService<IProjectExistsUIContextProviderLanguageService>()?.GetUIContext());

        // UIContexts can be "zombied" if UIContexts aren't supported because we're in a command line build or in
        // other scenarios.
        if (uiContext == null || uiContext.IsZombie)
            return;

        // Note: it's safe to read CurrentSolution here outside of any sort of lock.  We do all work here on the UI
        // thread, so that acts as a natural ordering mechanism here.  If, say, a BG piece of work was mutating this
        // solution (either adding or removing a project) then that work will also have enqueued the next refresh
        // operation on the UI thread.  So we'll always eventually reach a fixed point where the task for that
        // language will check the latest CurrentSolution we have and will set the IsActive bit accordingly.
        uiContext.IsActive = this.CurrentSolution.Projects.Any(p => p.Language == language);
    }
}
