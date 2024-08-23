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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Workspaces.ProjectSystem.ProjectSystemProjectFactory;

namespace Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

internal sealed partial class ProjectSystemProject
{
    private static readonly char[] s_directorySeparator = [Path.DirectorySeparatorChar];
    private static readonly ImmutableArray<MetadataReferenceProperties> s_defaultMetadataReferenceProperties = [default(MetadataReferenceProperties)];

    private readonly ProjectSystemProjectFactory _projectSystemProjectFactory;
    private readonly ProjectSystemHostInfo _hostInfo;
    private readonly IAnalyzerAssemblyLoader _analyzerAssemblyLoader;

    /// <summary>
    /// A semaphore taken for all mutation of any mutable field in this type.
    /// </summary>
    /// <remarks>This is, for now, intentionally pessimistic. There are no doubt ways that we could allow more to run in
    /// parallel, but the current tradeoff is for simplicity of code and "obvious correctness" than something that is
    /// subtle, fast, and wrong.</remarks>
    private readonly SemaphoreSlim _gate = new(initialCount: 1);

    /// <summary>
    /// The number of active batch scopes. If this is zero, we are not batching, non-zero means we are batching.
    /// </summary>
    private int _activeBatchScopes = 0;

    private readonly List<(string path, MetadataReferenceProperties properties)> _metadataReferencesAddedInBatch = [];
    private readonly List<(string path, MetadataReferenceProperties properties)> _metadataReferencesRemovedInBatch = [];
    private readonly List<ProjectReference> _projectReferencesAddedInBatch = [];
    private readonly List<ProjectReference> _projectReferencesRemovedInBatch = [];

    private readonly Dictionary<string, AnalyzerFileReference> _analyzerPathsToAnalyzers = [];
    private readonly List<AnalyzerFileReference> _analyzersAddedInBatch = [];

    /// <summary>
    /// The list of <see cref="AnalyzerReference"/>s that will be removed in this batch.
    /// </summary>
    private readonly List<AnalyzerFileReference> _analyzersRemovedInBatch = [];

    private readonly List<Func<SolutionChangeAccumulator, ProjectUpdateState, ProjectUpdateState>> _projectPropertyModificationsInBatch = [];

    private string _assemblyName;
    private string _displayName;
    private string? _filePath;
    private CompilationOptions? _compilationOptions;
    private ParseOptions? _parseOptions;
    private SourceHashAlgorithm _checksumAlgorithm = SourceHashAlgorithms.Default;
    private bool _hasAllInformation = true;
    private string? _compilationOutputAssemblyFilePath;
    private string? _outputFilePath;
    private string? _outputRefFilePath;
    private string? _defaultNamespace;

    /// <summary>
    /// If this project is the 'primary' project the project system cares about for a group of Roslyn projects that
    /// correspond to different configurations of a single project system project. <see langword="true"/> by
    /// default.
    /// </summary>
    internal bool IsPrimary { get; set; } = true;

    // Actual property values for 'RunAnalyzers' and 'RunAnalyzersDuringLiveAnalysis' properties from the project file.
    // Both these properties can be used to configure running analyzers, with RunAnalyzers overriding RunAnalyzersDuringLiveAnalysis.
    private bool? _runAnalyzersPropertyValue;
    private bool? _runAnalyzersDuringLiveAnalysisPropertyValue;

    // Effective boolean value to determine if analyzers should be executed based on _runAnalyzersPropertyValue and _runAnalyzersDuringLiveAnalysisPropertyValue.
    private bool _runAnalyzers = true;

    /// <summary>
    /// The full list of all metadata references this project has. References that have internally been converted to project references
    /// will still be in this.
    /// </summary>
    private readonly Dictionary<string, ImmutableArray<MetadataReferenceProperties>> _allMetadataReferences = [];

    /// <summary>
    /// The file watching tokens for the documents in this project. We get the tokens even when we're in a batch, so the files here
    /// may not be in the actual workspace yet.
    /// </summary>
    private readonly Dictionary<DocumentId, IWatchedFile> _documentWatchedFiles = [];

    /// <summary>
    /// A file change context used to watch source files, additional files, and analyzer config files for this project. It's automatically set to watch the user's project
    /// directory so we avoid file-by-file watching.
    /// </summary>
    private readonly IFileChangeContext _documentFileChangeContext;

    /// <summary>
    /// track whether we have been subscribed to <see cref="IDynamicFileInfoProvider.Updated"/> event
    /// </summary>
    private readonly HashSet<IDynamicFileInfoProvider> _eventSubscriptionTracker = [];

    /// <summary>
    /// Map of the original dynamic file path to the <see cref="DynamicFileInfo.FilePath"/> that was associated with it.
    ///
    /// For example, the key is something like Page.cshtml which is given to us from the project system calling
    /// <see cref="AddDynamicSourceFile(string, ImmutableArray{string})"/>. The value of the map is a generated file that
    /// corresponds to the original path, say Page.g.cs. If we were given a file by the project system but no
    /// <see cref="IDynamicFileInfoProvider"/> provided a file for it, we will record the value as null so we still can track
    /// the addition of the .cshtml file for a later call to <see cref="RemoveDynamicSourceFile(string)"/>.
    ///
    /// The workspace snapshot will only have a document with  <see cref="DynamicFileInfo.FilePath"/> (the value) but not the
    /// original dynamic file path (the key).
    /// </summary>
    /// <remarks>
    /// We use the same string comparer as in the <see cref="BatchingDocumentCollection"/> used by _sourceFiles, below, as these
    /// files are added to that collection too.
    /// </remarks>
    private readonly Dictionary<string, string?> _dynamicFilePathMaps = new(StringComparer.OrdinalIgnoreCase);

    private readonly BatchingDocumentCollection _sourceFiles;
    private readonly BatchingDocumentCollection _additionalFiles;
    private readonly BatchingDocumentCollection _analyzerConfigFiles;

    private readonly AsyncBatchingWorkQueue<string> _fileChangesToProcess;
    private readonly CancellationTokenSource _asynchronousFileChangeProcessingCancellationTokenSource = new();

    public ProjectId Id { get; }
    public string Language { get; }

    internal ProjectSystemProject(
        ProjectSystemProjectFactory projectSystemProjectFactory,
        ProjectSystemHostInfo hostInfo,
        ProjectId id,
        string displayName,
        string language,
        string assemblyName,
        CompilationOptions? compilationOptions,
        string? filePath,
        ParseOptions? parseOptions)
    {
        _projectSystemProjectFactory = projectSystemProjectFactory;
        _hostInfo = hostInfo;

        Id = id;
        Language = language;
        _displayName = displayName;

        var provider = _projectSystemProjectFactory.SolutionServices.GetRequiredService<IAnalyzerAssemblyLoaderProvider>();

        // NOTE: The provider will always return the same singleton, shadow copying, analyzer loader instance, which is
        // important to ensure that analyzer dependencies are correctly loaded.  Note: if we want to support reloading
        // of analyzer references *within* this workspace (and not just in the OOP roslyn server), this would need to
        // change to use a dedicate ALC and an IsolatedAssemblyReferenceSet.
        _analyzerAssemblyLoader = provider.SharedShadowCopyLoader;

        _sourceFiles = new BatchingDocumentCollection(
            this,
            documentAlreadyInWorkspace: (s, d) => s.ContainsDocument(d),
            documentAddAction: (w, d) => w.OnDocumentAdded(d),
            documentRemoveAction: (w, documentId) => w.OnDocumentRemoved(documentId),
            documentTextLoaderChangedAction: (s, d, loader) => s.WithDocumentTextLoader(d, loader, PreservationMode.PreserveValue),
            documentChangedWorkspaceKind: WorkspaceChangeKind.DocumentChanged);

        _additionalFiles = new BatchingDocumentCollection(this,
            (s, d) => s.ContainsAdditionalDocument(d),
            (w, d) => w.OnAdditionalDocumentAdded(d),
            (w, documentId) => w.OnAdditionalDocumentRemoved(documentId),
            documentTextLoaderChangedAction: (s, d, loader) => s.WithAdditionalDocumentTextLoader(d, loader, PreservationMode.PreserveValue),
            documentChangedWorkspaceKind: WorkspaceChangeKind.AdditionalDocumentChanged);

        _analyzerConfigFiles = new BatchingDocumentCollection(this,
            (s, d) => s.ContainsAnalyzerConfigDocument(d),
            (w, d) => w.OnAnalyzerConfigDocumentAdded(d),
            (w, documentId) => w.OnAnalyzerConfigDocumentRemoved(documentId),
            documentTextLoaderChangedAction: (s, d, loader) => s.WithAnalyzerConfigDocumentTextLoader(d, loader, PreservationMode.PreserveValue),
            documentChangedWorkspaceKind: WorkspaceChangeKind.AnalyzerConfigDocumentChanged);

        _fileChangesToProcess = new AsyncBatchingWorkQueue<string>(
            TimeSpan.FromMilliseconds(200), // 200 chosen with absolutely no evidence whatsoever
            ProcessFileChangesAsync,
            StringComparer.Ordinal,
            _projectSystemProjectFactory.WorkspaceListener,
            _asynchronousFileChangeProcessingCancellationTokenSource.Token);

        _assemblyName = assemblyName;
        _compilationOptions = compilationOptions;
        _filePath = filePath;
        _parseOptions = parseOptions;

        var watchedDirectories = GetWatchedDirectories(language, filePath);
        _documentFileChangeContext = _projectSystemProjectFactory.FileChangeWatcher.CreateContext(watchedDirectories);
        _documentFileChangeContext.FileChanged += DocumentFileChangeContext_FileChanged;

        static ImmutableArray<WatchedDirectory> GetWatchedDirectories(string? language, string? filePath)
        {
            if (filePath is null)
                return [];

            var rootPath = Path.GetDirectoryName(filePath);
            if (rootPath is null)
                return [];

            return language switch
            {
                LanguageNames.VisualBasic => [new(rootPath, ".vb")],
                LanguageNames.CSharp => [new(rootPath, ".cs"), new(rootPath, ".razor"), new(rootPath, ".cshtml")],
                _ => []
            };
        }
    }

    private void ChangeProjectProperty<T>(ref T field, T newValue, Func<Solution, Solution> updateSolution, bool logThrowAwayTelemetry = false)
    {
        ChangeProjectProperty(
            ref field,
            newValue,
            (solutionChanges, projectUpdateState, _) =>
            {
                solutionChanges.UpdateSolutionForProjectAction(Id, updateSolution(solutionChanges.Solution));
                return projectUpdateState;
            },
            logThrowAwayTelemetry);
    }

    private void ChangeProjectProperty<T>(
        ref T field,
        T newValue,
        Func<SolutionChangeAccumulator, ProjectUpdateState, T, ProjectUpdateState> updateSolution,
        bool logThrowAwayTelemetry = false)
    {
        using var _ = CreateBatchScope();

        using (_gate.DisposableWait())
        {
            // If nothing is changing, we can skip entirely
            if (object.Equals(field, newValue))
            {
                return;
            }

            var oldValue = field;

            field = newValue;

            if (logThrowAwayTelemetry)
            {
                var telemetryService = _projectSystemProjectFactory.SolutionServices.GetService<IWorkspaceTelemetryService>();

                if (telemetryService?.HasActiveSession == true)
                {
                    var workspaceStatusService = _projectSystemProjectFactory.SolutionServices.GetRequiredService<IWorkspaceStatusService>();

                    // We only log telemetry during solution open

                    // Importantly, we do not await/wait on the fullyLoadedStateTask.  We do not want to ever be waiting on work
                    // that may end up touching the UI thread (As we can deadlock if GetTagsSynchronous waits on us).  Instead,
                    // we only check if the Task is completed.  Prior to that we will assume we are still loading.  Once this
                    // task is completed, we know that the WaitUntilFullyLoadedAsync call will have actually finished and we're
                    // fully loaded.
                    var isFullyLoadedTask = workspaceStatusService.IsFullyLoadedAsync(CancellationToken.None);
                    var isFullyLoaded = isFullyLoadedTask is { IsCompleted: true } && isFullyLoadedTask.GetAwaiter().GetResult();

                    if (!isFullyLoaded)
                    {
                        TryReportCompilationThrownAway(_projectSystemProjectFactory.Workspace.CurrentSolution, Id);
                    }
                }
            }

            _projectPropertyModificationsInBatch.Add(
                (solutionChanges, projectUpdateState) => updateSolution(solutionChanges, projectUpdateState, oldValue));
        }
    }

    /// <summary>
    /// Reports a telemetry event if compilation information is being thrown away after being previously computed
    /// </summary>
    private static void TryReportCompilationThrownAway(
        Solution solution, ProjectId projectId)
    {
        // We log the number of syntax trees that have been parsed even if there was no compilation created yet
        var projectState = solution.SolutionState.GetRequiredProjectState(projectId);
        var parsedTrees = 0;
        foreach (var (_, documentState) in projectState.DocumentStates.States)
        {
            if (documentState.TryGetSyntaxTree(out _))
            {
                parsedTrees++;
            }
        }

        // But we also want to know if a compilation was created
        var hadCompilation = solution.CompilationState.TryGetCompilation(projectId, out _);

        if (parsedTrees > 0 || hadCompilation)
        {
            Logger.Log(FunctionId.Workspace_Project_CompilationThrownAway, KeyValueLogMessage.Create(m =>
            {
                // Note: Not using our project Id. This is the same ProjectGuid that the project system uses
                // so data can be correlated
                m["ProjectGuid"] = projectState.ProjectInfo.Attributes.TelemetryId.ToString("B");
                m["SyntaxTreesParsed"] = parsedTrees;
                m["HadCompilation"] = hadCompilation;
            }));
        }
    }

    private void ChangeProjectOutputPath(ref string? field, string? newValue, Func<Solution, Solution> withNewValue)
    {
        ChangeProjectProperty(ref field, newValue, (solutionChanges, projectUpdateState, oldValue) =>
        {
            // First, update the property itself that's exposed on the Project.
            solutionChanges.UpdateSolutionForProjectAction(Id, withNewValue(solutionChanges.Solution));

            if (oldValue != null)
            {
                projectUpdateState = RemoveProjectOutputPath_NoLock(solutionChanges, Id, oldValue, projectUpdateState,
                    _projectSystemProjectFactory.SolutionClosing, _projectSystemProjectFactory.SolutionServices);
            }

            if (newValue != null)
            {
                projectUpdateState = AddProjectOutputPath_NoLock(solutionChanges, Id, newValue, projectUpdateState, _projectSystemProjectFactory.SolutionServices);
            }

            return projectUpdateState;
        });
    }

    public string AssemblyName
    {
        get => _assemblyName;
        set => ChangeProjectProperty(ref _assemblyName, value, s => s.WithProjectAssemblyName(Id, value), logThrowAwayTelemetry: true);
    }

    // The property could be null if this is a non-C#/VB language and we don't have one for it. But we disallow assigning null, because C#/VB cannot end up null
    // again once they already had one.
    [DisallowNull]
    public CompilationOptions? CompilationOptions
    {
        get => _compilationOptions;
        set => ChangeProjectProperty(ref _compilationOptions, value, s => s.WithProjectCompilationOptions(Id, value), logThrowAwayTelemetry: true);
    }

    // The property could be null if this is a non-C#/VB language and we don't have one for it. But we disallow assigning null, because C#/VB cannot end up null
    // again once they already had one.
    [DisallowNull]
    public ParseOptions? ParseOptions
    {
        get => _parseOptions;
        set => ChangeProjectProperty(ref _parseOptions, value, s => s.WithProjectParseOptions(Id, value), logThrowAwayTelemetry: true);
    }

    /// <summary>
    /// The path to the output in obj.
    /// </summary>
    internal string? CompilationOutputAssemblyFilePath
    {
        get => _compilationOutputAssemblyFilePath;
        set => ChangeProjectOutputPath(
            ref _compilationOutputAssemblyFilePath,
            value,
            s => s.WithProjectCompilationOutputInfo(Id, s.GetRequiredProject(Id).CompilationOutputInfo.WithAssemblyPath(value)));
    }

    public string? OutputFilePath
    {
        get => _outputFilePath;
        set => ChangeProjectOutputPath(ref _outputFilePath, value, s => s.WithProjectOutputFilePath(Id, value));
    }

    public string? OutputRefFilePath
    {
        get => _outputRefFilePath;
        set => ChangeProjectOutputPath(ref _outputRefFilePath, value, s => s.WithProjectOutputRefFilePath(Id, value));
    }

    public string? FilePath
    {
        get => _filePath;
        set => ChangeProjectProperty(ref _filePath, value, s => s.WithProjectFilePath(Id, value));
    }

    public string DisplayName
    {
        get => _displayName;
        set => ChangeProjectProperty(ref _displayName, value, s => s.WithProjectName(Id, value));
    }

    public SourceHashAlgorithm ChecksumAlgorithm
    {
        get => _checksumAlgorithm;
        set => ChangeProjectProperty(ref _checksumAlgorithm, value, s => s.WithProjectChecksumAlgorithm(Id, value));
    }

    // internal to match the visibility of the Workspace-level API -- this is something
    // we use but we haven't made officially public yet.
    internal bool HasAllInformation
    {
        get => _hasAllInformation;
        set => ChangeProjectProperty(ref _hasAllInformation, value, s => s.WithHasAllInformation(Id, value));
    }

    internal bool? RunAnalyzers
    {
        get => _runAnalyzersPropertyValue;
        set
        {
            _runAnalyzersPropertyValue = value;
            UpdateRunAnalyzers();
        }
    }

    internal bool? RunAnalyzersDuringLiveAnalysis
    {
        get => _runAnalyzersDuringLiveAnalysisPropertyValue;
        set
        {
            _runAnalyzersDuringLiveAnalysisPropertyValue = value;
            UpdateRunAnalyzers();
        }
    }

    private void UpdateRunAnalyzers()
    {
        // Property RunAnalyzers overrides RunAnalyzersDuringLiveAnalysis, and default when both properties are not set is 'true'.
        var runAnalyzers = _runAnalyzersPropertyValue ?? _runAnalyzersDuringLiveAnalysisPropertyValue ?? true;
        ChangeProjectProperty(ref _runAnalyzers, runAnalyzers, s => s.WithRunAnalyzers(Id, runAnalyzers));
    }

    /// <summary>
    /// The default namespace of the project.
    /// </summary>
    /// <remarks>
    /// In C#, this is defined as the value of "rootnamespace" msbuild property. Right now VB doesn't 
    /// have the concept of "default namespace", but we conjure one in workspace by assigning the value
    /// of the project's root namespace to it. So various features can choose to use it for their own purpose.
    /// 
    /// In the future, we might consider officially exposing "default namespace" for VB project
    /// (e.g.through a "defaultnamespace" msbuild property)
    /// </remarks>
    internal string? DefaultNamespace
    {
        get => _defaultNamespace;
        set => ChangeProjectProperty(ref _defaultNamespace, value, s => s.WithProjectDefaultNamespace(Id, value));
    }

    /// <summary>
    /// The max language version supported for this project, if applicable. Useful to help indicate what 
    /// language version features should be suggested to a user, as well as if they can be upgraded. 
    /// </summary>
    internal string? MaxLangVersion
    {
        set => _projectSystemProjectFactory.SetMaxLanguageVersion(Id, value);
    }

    internal string DependencyNodeTargetIdentifier
    {
        set => _projectSystemProjectFactory.SetDependencyNodeTargetIdentifier(Id, value);
    }

    private bool HasBeenRemoved => !_projectSystemProjectFactory.Workspace.CurrentSolution.ContainsProject(Id);

    #region Batching

    public BatchScope CreateBatchScope()
    {
        using (_gate.DisposableWait())
        {
            _activeBatchScopes++;
            return new BatchScope(this);
        }
    }

    public async ValueTask<BatchScope> CreateBatchScopeAsync(CancellationToken cancellationToken)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            _activeBatchScopes++;
            return new BatchScope(this);
        }
    }

    public sealed class BatchScope : IDisposable, IAsyncDisposable
    {
        private readonly ProjectSystemProject _project;

        /// <summary>
        /// Flag to control if this has already been disposed. Not a boolean only so it can be used with Interlocked.CompareExchange.
        /// </summary>
        private volatile int _disposed = 0;

        internal BatchScope(ProjectSystemProject visualStudioProject)
            => _project = visualStudioProject;

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                _project.OnBatchScopeDisposedMaybeAsync(useAsync: false).VerifyCompleted();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                await _project.OnBatchScopeDisposedMaybeAsync(useAsync: true).ConfigureAwait(false);
            }
        }
    }

    private async Task OnBatchScopeDisposedMaybeAsync(bool useAsync)
    {
        using (useAsync ? await _gate.DisposableWaitAsync().ConfigureAwait(false) : _gate.DisposableWait())
        {
            _activeBatchScopes--;

            if (_activeBatchScopes > 0)
            {
                return;
            }

            // If the project was already removed, we'll just ignore any further requests to complete batches.
            if (HasBeenRemoved)
            {
                return;
            }

            // The transformation function will set these variables, but we need to use them after the transformation is applied
            // so we must instantiate them here.
            ImmutableArray<string> documentFileNamesAdded = [];
            ImmutableArray<(DocumentId documentId, SourceTextContainer textContainer)> documentsToOpen = [];
            ImmutableArray<(DocumentId documentId, SourceTextContainer textContainer)> additionalDocumentsToOpen = [];
            ImmutableArray<(DocumentId documentId, SourceTextContainer textContainer)> analyzerConfigDocumentsToOpen = [];

            var hasAnalyzerChanges = _analyzersAddedInBatch.Count > 0 || _analyzersRemovedInBatch.Count > 0;

            await _projectSystemProjectFactory.ApplyBatchChangeToWorkspaceMaybeAsync(useAsync, (solutionChanges, projectUpdateState) =>
            {
                // Changes made inside this transformation must be idemopotent in case it is attempted multiple times.

                var documentFileNamesAddedBuilder = ImmutableArray.CreateBuilder<string>();
                documentsToOpen = _sourceFiles.UpdateSolutionForBatch(
                    solutionChanges,
                    documentFileNamesAddedBuilder,
                    static (s, documents) => s.AddDocuments(documents),
                    WorkspaceChangeKind.DocumentAdded,
                    static (s, ids) => s.RemoveDocuments(ids),
                    WorkspaceChangeKind.DocumentRemoved);

                additionalDocumentsToOpen = _additionalFiles.UpdateSolutionForBatch(
                    solutionChanges,
                    documentFileNamesAddedBuilder,
                    static (s, documents) => s.AddAdditionalDocuments(documents),
                    WorkspaceChangeKind.AdditionalDocumentAdded,
                    static (s, ids) => s.RemoveAdditionalDocuments(ids),
                    WorkspaceChangeKind.AdditionalDocumentRemoved);

                analyzerConfigDocumentsToOpen = _analyzerConfigFiles.UpdateSolutionForBatch(
                    solutionChanges,
                    documentFileNamesAddedBuilder,
                    static (s, documents) => s.AddAnalyzerConfigDocuments(documents),
                    WorkspaceChangeKind.AnalyzerConfigDocumentAdded,
                    static (s, ids) => s.RemoveAnalyzerConfigDocuments(ids),
                    WorkspaceChangeKind.AnalyzerConfigDocumentRemoved);

                documentFileNamesAdded = documentFileNamesAddedBuilder.ToImmutable();

                // Metadata reference removing. Do this before adding in case this removes a project reference that
                // we are also going to add in the same batch. This could happen if case is changing, or we're targeting
                // a different output path (say bin vs. obj vs. ref).
                foreach (var (path, properties) in _metadataReferencesRemovedInBatch)
                {
                    projectUpdateState = TryRemoveConvertedProjectReference_NoLock(Id, path, properties, projectUpdateState, out var projectReference);

                    if (projectReference != null)
                    {
                        solutionChanges.UpdateSolutionForProjectAction(
                            Id,
                            solutionChanges.Solution.RemoveProjectReference(Id, projectReference));
                    }
                    else
                    {
                        // TODO: find a cleaner way to fetch this
                        var metadataReference = _projectSystemProjectFactory.Workspace.CurrentSolution.GetRequiredProject(Id).MetadataReferences
                            .Cast<PortableExecutableReference>()
                            .Single(m => m.FilePath == path && m.Properties == properties);

                        projectUpdateState = projectUpdateState.WithIncrementalMetadataReferenceRemoved(metadataReference);

                        solutionChanges.UpdateSolutionForProjectAction(
                            Id,
                            newSolution: solutionChanges.Solution.RemoveMetadataReference(Id, metadataReference));
                    }
                }

                // Metadata reference adding...
                if (_metadataReferencesAddedInBatch.Count > 0)
                {
                    var projectReferencesCreated = new List<ProjectReference>();

                    foreach (var (path, properties) in _metadataReferencesAddedInBatch)
                    {
                        projectUpdateState = TryCreateConvertedProjectReference_NoLock(
                            Id, path, properties, projectUpdateState, solutionChanges.Solution, out var projectReference);

                        if (projectReference != null)
                        {
                            projectReferencesCreated.Add(projectReference);
                        }
                        else
                        {
                            var metadataReference = CreateMetadataReference_NoLock(path, properties, _projectSystemProjectFactory.SolutionServices);
                            projectUpdateState = projectUpdateState.WithIncrementalMetadataReferenceAdded(metadataReference);
                        }
                    }

                    solutionChanges.UpdateSolutionForProjectAction(
                        Id,
                        solutionChanges.Solution
                            .AddProjectReferences(Id, projectReferencesCreated)
                            .AddMetadataReferences(Id, projectUpdateState.AddedMetadataReferences));
                }

                // Project reference adding...
                solutionChanges.UpdateSolutionForProjectAction(
                    Id,
                    newSolution: solutionChanges.Solution.AddProjectReferences(Id, _projectReferencesAddedInBatch));

                // Project reference removing...
                foreach (var projectReference in _projectReferencesRemovedInBatch)
                {
                    solutionChanges.UpdateSolutionForProjectAction(
                        Id,
                        newSolution: solutionChanges.Solution.RemoveProjectReference(Id, projectReference));
                }

                // Analyzer reference removing...
                if (_analyzersRemovedInBatch.Count > 0)
                {
                    projectUpdateState = projectUpdateState.WithIncrementalAnalyzerReferencesRemoved(_analyzersRemovedInBatch);

                    foreach (var analyzerReference in _analyzersRemovedInBatch)
                        solutionChanges.UpdateSolutionForProjectAction(Id, solutionChanges.Solution.RemoveAnalyzerReference(Id, analyzerReference));
                }

                // Analyzer reference adding...
                if (_analyzersAddedInBatch.Count > 0)
                {
                    projectUpdateState = projectUpdateState.WithIncrementalAnalyzerReferencesAdded(_analyzersAddedInBatch);

                    solutionChanges.UpdateSolutionForProjectAction(
                        Id, solutionChanges.Solution.AddAnalyzerReferences(Id, _analyzersAddedInBatch));
                }

                // Other property modifications...
                foreach (var propertyModification in _projectPropertyModificationsInBatch)
                    projectUpdateState = propertyModification(solutionChanges, projectUpdateState);

                return projectUpdateState;
            },
            onAfterUpdateAlways: projectUpdateState =>
            {
                // It is very important that these are cleared in the onAfterUpdateAlways action passed to ApplyBatchChangeToWorkspaceMaybeAsync
                // This is because the transformation may be run multiple times (if the workspace current solution is changed underneath us),
                // whereas onAfterUpdate runs a single time once the transformation has been applied.
                _sourceFiles.ClearBatchState();
                _additionalFiles.ClearBatchState();
                _analyzerConfigFiles.ClearBatchState();

                ClearAndZeroCapacity(_metadataReferencesRemovedInBatch);
                ClearAndZeroCapacity(_metadataReferencesAddedInBatch);

                ClearAndZeroCapacity(_projectReferencesAddedInBatch);
                ClearAndZeroCapacity(_projectReferencesRemovedInBatch);
                ClearAndZeroCapacity(_analyzersAddedInBatch);
                ClearAndZeroCapacity(_analyzersRemovedInBatch);

                ClearAndZeroCapacity(_projectPropertyModificationsInBatch);

            }).ConfigureAwait(false);

            foreach (var (documentId, textContainer) in documentsToOpen)
                await _projectSystemProjectFactory.ApplyChangeToWorkspaceMaybeAsync(useAsync, w => w.OnDocumentOpened(documentId, textContainer)).ConfigureAwait(false);

            foreach (var (documentId, textContainer) in additionalDocumentsToOpen)
                await _projectSystemProjectFactory.ApplyChangeToWorkspaceMaybeAsync(useAsync, w => w.OnAdditionalDocumentOpened(documentId, textContainer)).ConfigureAwait(false);

            foreach (var (documentId, textContainer) in analyzerConfigDocumentsToOpen)
                await _projectSystemProjectFactory.ApplyChangeToWorkspaceMaybeAsync(useAsync, w => w.OnAnalyzerConfigDocumentOpened(documentId, textContainer)).ConfigureAwait(false);

            // Give the host the opportunity to check if those files are open
            if (documentFileNamesAdded.Count() > 0)
                await _projectSystemProjectFactory.RaiseOnDocumentsAddedMaybeAsync(useAsync, documentFileNamesAdded).ConfigureAwait(false);

            // If we added or removed analyzers, then re-run all generators to bring them up to date.
            if (hasAnalyzerChanges)
                _projectSystemProjectFactory.Workspace.EnqueueUpdateSourceGeneratorVersion(projectId: null, forceRegeneration: true);
        }
    }

    #endregion

    #region Source File Addition/Removal

    public void AddSourceFile(string fullPath, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular, ImmutableArray<string> folders = default)
        => _sourceFiles.AddFile(fullPath, sourceCodeKind, folders);

    /// <summary>
    /// Adds a source file to the project from a text container (eg, a Visual Studio Text buffer)
    /// </summary>
    /// <param name="textContainer">The text container that contains this file.</param>
    /// <param name="fullPath">The file path of the document.</param>
    /// <param name="sourceCodeKind">The kind of the source code.</param>
    /// <param name="folders">The names of the logical nested folders the document is contained in.</param>
    /// <param name="designTimeOnly">Whether the document is used only for design time (eg. completion) or also included in a compilation.</param>
    /// <param name="documentServiceProvider">A <see cref="IDocumentServiceProvider"/> associated with this document</param>
    public DocumentId AddSourceTextContainer(
        SourceTextContainer textContainer,
        string fullPath,
        SourceCodeKind sourceCodeKind = SourceCodeKind.Regular,
        ImmutableArray<string> folders = default,
        bool designTimeOnly = false,
        IDocumentServiceProvider? documentServiceProvider = null)
    {
        return _sourceFiles.AddTextContainer(textContainer, fullPath, sourceCodeKind, folders, designTimeOnly, documentServiceProvider);
    }

    public bool ContainsSourceFile(string fullPath)
        => _sourceFiles.ContainsFile(fullPath);

    public void RemoveSourceFile(string fullPath)
        => _sourceFiles.RemoveFile(fullPath);

    public void RemoveSourceTextContainer(SourceTextContainer textContainer)
        => _sourceFiles.RemoveTextContainer(textContainer);

    #endregion

    #region Additional File Addition/Removal

    // TODO: should AdditionalFiles have source code kinds?
    public void AddAdditionalFile(string fullPath, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular, ImmutableArray<string> folders = default)
        => _additionalFiles.AddFile(fullPath, sourceCodeKind, folders);

    public bool ContainsAdditionalFile(string fullPath)
        => _additionalFiles.ContainsFile(fullPath);

    public void RemoveAdditionalFile(string fullPath)
        => _additionalFiles.RemoveFile(fullPath);

    #endregion

    #region Analyzer Config File Addition/Removal

    public void AddAnalyzerConfigFile(string fullPath)
    {
        // TODO: do we need folders for analyzer config files?
        _analyzerConfigFiles.AddFile(fullPath, SourceCodeKind.Regular, folders: default);
    }

    public bool ContainsAnalyzerConfigFile(string fullPath)
        => _analyzerConfigFiles.ContainsFile(fullPath);

    public void RemoveAnalyzerConfigFile(string fullPath)
        => _analyzerConfigFiles.RemoveFile(fullPath);

    #endregion

    #region Non Source File Addition/Removal

    public void AddDynamicSourceFile(string dynamicFilePath, ImmutableArray<string> folders)
    {
        DynamicFileInfo? fileInfo = null;
        IDynamicFileInfoProvider? providerForFileInfo = null;

        var extension = FileNameUtilities.GetExtension(dynamicFilePath)?.TrimStart('.');
        if (extension?.Length == 0)
        {
            fileInfo = null;
        }
        else
        {
            foreach (var provider in _hostInfo.DynamicFileInfoProviders)
            {
                // skip unrelated providers
                if (!provider.Metadata.Extensions.Any(e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                // Don't get confused by _filePath and filePath.
                // VisualStudioProject._filePath points to csproj/vbproj of the project
                // and the parameter filePath points to dynamic file such as ASP.NET .g.cs files.
                // 
                // Also, provider is free-threaded. so fine to call Wait rather than JTF.
                fileInfo = provider.Value.GetDynamicFileInfoAsync(
                    projectId: Id, projectFilePath: _filePath, filePath: dynamicFilePath, CancellationToken.None).WaitAndGetResult_CanCallOnBackground(CancellationToken.None);

                if (fileInfo != null)
                {
                    fileInfo = FixUpDynamicFileInfo(fileInfo, dynamicFilePath);
                    providerForFileInfo = provider.Value;
                    break;
                }
            }
        }

        using (_gate.DisposableWait())
        {
            if (_dynamicFilePathMaps.ContainsKey(dynamicFilePath))
            {
                // TODO: if we have a duplicate, we are not calling RemoveDynamicFileInfoAsync since we
                // don't want to call with that under a lock. If we are getting duplicates we have bigger problems
                // at that point since our workspace is generally out of sync with the project system.
                // Given we're taking this as a late fix prior to a release, I don't think it's worth the added
                // risk to handle a case that wasn't handled before either.
                throw new ArgumentException($"{dynamicFilePath} has already been added to this project.");
            }

            // Record the mapping from the dynamic file path to the source file it generated. We will record
            // 'null' if no provider was able to produce a source file for this input file. That could happen
            // if the provider (say ASP.NET Razor) doesn't recognize the file, or the wrong type of file
            // got passed through the system. That's not a failure from the project system's perspective:
            // adding dynamic files is a hint at best that doesn't impact it.
            _dynamicFilePathMaps.Add(dynamicFilePath, fileInfo?.FilePath);

            if (fileInfo != null)
            {
                // If fileInfo is not null, that means we found a provider so this should be not-null as well
                // since we had to go through the earlier assignment.
                Contract.ThrowIfNull(providerForFileInfo);
                _sourceFiles.AddDynamicFile_NoLock(providerForFileInfo, fileInfo, folders);
            }
        }
    }

    private static DynamicFileInfo FixUpDynamicFileInfo(DynamicFileInfo fileInfo, string filePath)
    {
        // we might change contract and just throw here. but for now, we keep existing contract where one can return null for DynamicFileInfo.FilePath.
        // In this case we substitute the file being generated from so we still have some path.
        if (string.IsNullOrEmpty(fileInfo.FilePath))
        {
            return new DynamicFileInfo(filePath, fileInfo.SourceCodeKind, fileInfo.TextLoader, fileInfo.DesignTimeOnly, fileInfo.DocumentServiceProvider);
        }

        return fileInfo;
    }

    public void RemoveDynamicSourceFile(string dynamicFilePath)
    {
        IDynamicFileInfoProvider provider;

        using (_gate.DisposableWait())
        {
            if (!_dynamicFilePathMaps.TryGetValue(dynamicFilePath, out var sourceFilePath))
            {
                throw new ArgumentException($"{dynamicFilePath} wasn't added by a previous call to {nameof(AddDynamicSourceFile)}");
            }

            _dynamicFilePathMaps.Remove(dynamicFilePath);

            // If we got a null path back, it means we never had a source file to add. In that case,
            // we're done
            if (sourceFilePath == null)
            {
                return;
            }

            provider = _sourceFiles.RemoveDynamicFile_NoLock(sourceFilePath);
        }

        // provider is free-threaded. so fine to call Wait rather than JTF
        provider.RemoveDynamicFileInfoAsync(
            projectId: Id, projectFilePath: _filePath, filePath: dynamicFilePath, CancellationToken.None).Wait(CancellationToken.None);
    }

    private void OnDynamicFileInfoUpdated(object? sender, string dynamicFilePath)
    {
        string? fileInfoPath;

        using (_gate.DisposableWait())
        {
            if (!_dynamicFilePathMaps.TryGetValue(dynamicFilePath, out fileInfoPath))
            {
                // given file doesn't belong to this project. 
                // this happen since the event this is handling is shared between all projects
                return;
            }
        }

        if (fileInfoPath != null)
        {
            _sourceFiles.ProcessDynamicFileChange(dynamicFilePath, fileInfoPath);
        }
    }

    #endregion

    #region Analyzer Addition/Removal

    public void AddAnalyzerReference(string fullPath)
    {
        CompilerPathUtilities.RequireAbsolutePath(fullPath, nameof(fullPath));

        var mappedPaths = GetMappedAnalyzerPaths(fullPath);

        using var _ = CreateBatchScope();

        using (_gate.DisposableWait())
        {
            // check all mapped paths first, so that all analyzers are either added or not
            foreach (var mappedFullPath in mappedPaths)
            {
                if (_analyzerPathsToAnalyzers.ContainsKey(mappedFullPath))
                    throw new ArgumentException($"'{fullPath}' has already been added to this project.", nameof(fullPath));
            }

            foreach (var mappedFullPath in mappedPaths)
            {
                // Are we adding one we just recently removed? If so, we can just keep using that one, and avoid removing
                // it once we apply the batch
                var analyzerPendingRemoval = _analyzersRemovedInBatch.FirstOrDefault(a => a.FullPath == mappedFullPath);
                if (analyzerPendingRemoval != null)
                {
                    _analyzersRemovedInBatch.Remove(analyzerPendingRemoval);
                    _analyzerPathsToAnalyzers.Add(mappedFullPath, analyzerPendingRemoval);
                }
                else
                {
                    // Nope, we actually need to make a new one.
                    var analyzerReference = new AnalyzerFileReference(mappedFullPath, _analyzerAssemblyLoader);

                    _analyzersAddedInBatch.Add(analyzerReference);
                    _analyzerPathsToAnalyzers.Add(mappedFullPath, analyzerReference);
                }
            }
        }
    }

    public void RemoveAnalyzerReference(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            throw new ArgumentException("message", nameof(fullPath));

        var mappedPaths = GetMappedAnalyzerPaths(fullPath);

        using var _ = CreateBatchScope();

        using (_gate.DisposableWait())
        {
            // check all mapped paths first, so that all analyzers are either removed or not
            foreach (var mappedFullPath in mappedPaths)
            {
                if (!_analyzerPathsToAnalyzers.ContainsKey(mappedFullPath))
                    throw new ArgumentException($"'{fullPath}' is not an analyzer of this project.", nameof(fullPath));
            }

            foreach (var mappedFullPath in mappedPaths)
            {
                var analyzerReference = _analyzerPathsToAnalyzers[mappedFullPath];

                _analyzerPathsToAnalyzers.Remove(mappedFullPath);

                // This analyzer may be one we've just added in the same batch; in that case, just don't add it in
                // the first place.
                if (!_analyzersAddedInBatch.Remove(analyzerReference))
                    _analyzersRemovedInBatch.Add(analyzerReference);
            }
        }
    }

    internal const string RazorVsixExtensionId = "Microsoft.VisualStudio.RazorExtension";
    private static readonly string s_razorSourceGeneratorSdkDirectory = Path.Combine("Sdks", "Microsoft.NET.Sdk.Razor", "source-generators") + PathUtilities.DirectorySeparatorStr;
    private static readonly ImmutableArray<string> s_razorSourceGeneratorAssemblyNames =
    [
        "Microsoft.NET.Sdk.Razor.SourceGenerators",
        "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators",
        "Microsoft.CodeAnalysis.Razor.Compiler",
    ];
    private static readonly ImmutableArray<string> s_razorSourceGeneratorAssemblyRootedFileNames = s_razorSourceGeneratorAssemblyNames.SelectAsArray(
        assemblyName => PathUtilities.DirectorySeparatorStr + assemblyName + ".dll");

    private OneOrMany<string> GetMappedAnalyzerPaths(string fullPath)
    {
        fullPath = Path.GetFullPath(fullPath);
        // Map all files in the SDK directory that contains the Razor source generator to source generator files loaded from VSIX.
        // Include the generator and all its dependencies shipped in VSIX, discard the generator and all dependencies in the SDK
        if (fullPath.LastIndexOf(s_razorSourceGeneratorSdkDirectory, StringComparison.OrdinalIgnoreCase) + s_razorSourceGeneratorSdkDirectory.Length - 1 ==
            fullPath.LastIndexOf(Path.DirectorySeparatorChar))
        {
            var vsixRazorAnalyzers = _hostInfo.HostDiagnosticAnalyzerProvider.GetAnalyzerReferencesInExtensions().SelectAsArray(
                predicate: item => item.extensionId == RazorVsixExtensionId,
                selector: item => item.reference.FullPath);

            if (!vsixRazorAnalyzers.IsEmpty)
            {
                if (s_razorSourceGeneratorAssemblyRootedFileNames.Any(
                    static (fileName, fullPath) => fullPath.EndsWith(fileName, StringComparison.OrdinalIgnoreCase), fullPath))
                {
                    return OneOrMany.Create(vsixRazorAnalyzers);
                }

                return OneOrMany.Create(ImmutableArray<string>.Empty);
            }
        }

        return OneOrMany.Create(fullPath);
    }

    #endregion

    private void DocumentFileChangeContext_FileChanged(object? sender, string fullFilePath)
    {
        _fileChangesToProcess.AddWork(fullFilePath);
    }

    private async ValueTask ProcessFileChangesAsync(ImmutableSegmentedList<string> filePaths, CancellationToken cancellationToken)
    {
        await _sourceFiles.ProcessRegularFileChangesAsync(filePaths).ConfigureAwait(false);
        await _additionalFiles.ProcessRegularFileChangesAsync(filePaths).ConfigureAwait(false);
        await _analyzerConfigFiles.ProcessRegularFileChangesAsync(filePaths).ConfigureAwait(false);
    }

    #region Metadata Reference Addition/Removal

    public void AddMetadataReference(string fullPath, MetadataReferenceProperties properties)
    {
        if (string.IsNullOrEmpty(fullPath))
            throw new ArgumentException($"{nameof(fullPath)} isn't a valid path.", nameof(fullPath));

        using var _ = CreateBatchScope();

        using (_gate.DisposableWait())
        {
            if (ContainsMetadataReference_NoLock(fullPath, properties))
                throw new InvalidOperationException("The metadata reference has already been added to the project.");

            _allMetadataReferences.MultiAdd(fullPath, properties, s_defaultMetadataReferenceProperties);

            if (!_metadataReferencesRemovedInBatch.Remove((fullPath, properties)))
                _metadataReferencesAddedInBatch.Add((fullPath, properties));
        }
    }

    public bool ContainsMetadataReference(string fullPath, MetadataReferenceProperties properties)
    {
        using (_gate.DisposableWait())
        {
            return ContainsMetadataReference_NoLock(fullPath, properties);
        }
    }

    private bool ContainsMetadataReference_NoLock(string fullPath, MetadataReferenceProperties properties)
    {
        Debug.Assert(_gate.CurrentCount == 0);

        return _allMetadataReferences.TryGetValue(fullPath, out var propertiesList) && propertiesList.Contains(properties);
    }

    /// <summary>
    /// Returns the properties being used for the current metadata reference added to this project. May return multiple properties if
    /// the reference has been added multiple times with different properties.
    /// </summary>
    public ImmutableArray<MetadataReferenceProperties> GetPropertiesForMetadataReference(string fullPath)
    {
        using (_gate.DisposableWait())
        {
            return _allMetadataReferences.TryGetValue(fullPath, out var list) ? list : [];
        }
    }

    public void RemoveMetadataReference(string fullPath, MetadataReferenceProperties properties)
    {
        if (string.IsNullOrEmpty(fullPath))
            throw new ArgumentException($"{nameof(fullPath)} isn't a valid path.", nameof(fullPath));

        using var _ = CreateBatchScope();

        using (_gate.DisposableWait())
        {
            if (!ContainsMetadataReference_NoLock(fullPath, properties))
                throw new InvalidOperationException("The metadata reference does not exist in this project.");

            _allMetadataReferences.MultiRemove(fullPath, properties);

            if (!_metadataReferencesAddedInBatch.Remove((fullPath, properties)))
                _metadataReferencesRemovedInBatch.Add((fullPath, properties));
        }
    }

    #endregion

    #region Project Reference Addition/Removal

    public void AddProjectReference(ProjectReference projectReference)
    {
        if (projectReference == null)
            throw new ArgumentNullException(nameof(projectReference));

        using var _ = CreateBatchScope();

        using (_gate.DisposableWait())
        {
            if (ContainsProjectReference_NoLock(projectReference))
                throw new ArgumentException("The project reference has already been added to the project.");

            if (!_projectReferencesRemovedInBatch.Remove(projectReference))
                _projectReferencesAddedInBatch.Add(projectReference);
        }
    }

    public bool ContainsProjectReference(ProjectReference projectReference)
    {
        if (projectReference == null)
            throw new ArgumentNullException(nameof(projectReference));

        using (_gate.DisposableWait())
        {
            return ContainsProjectReference_NoLock(projectReference);
        }
    }

    private bool ContainsProjectReference_NoLock(ProjectReference projectReference)
    {
        Debug.Assert(_gate.CurrentCount == 0);

        if (_projectReferencesRemovedInBatch.Contains(projectReference))
            return false;

        if (_projectReferencesAddedInBatch.Contains(projectReference))
            return true;

        return _projectSystemProjectFactory.Workspace.CurrentSolution.GetRequiredProject(Id).AllProjectReferences.Contains(projectReference);
    }

    public IReadOnlyList<ProjectReference> GetProjectReferences()
    {
        using (_gate.DisposableWait())
        {
            // If we're not batching, then this is cheap: just fetch from the workspace and we're done
            var projectReferencesInWorkspace = _projectSystemProjectFactory.Workspace.CurrentSolution.GetRequiredProject(Id).AllProjectReferences;

            if (_activeBatchScopes == 0)
            {
                return projectReferencesInWorkspace;
            }

            // Not, so we get to compute a new list instead
            var newList = projectReferencesInWorkspace.ToList();
            newList.AddRange(_projectReferencesAddedInBatch);
            newList.RemoveAll(p => _projectReferencesRemovedInBatch.Contains(p));

            return newList;
        }
    }

    public void RemoveProjectReference(ProjectReference projectReference)
    {
        if (projectReference == null)
            throw new ArgumentNullException(nameof(projectReference));

        using var _ = CreateBatchScope();

        using (_gate.DisposableWait())
        {
            if (!ContainsProjectReference_NoLock(projectReference))
                throw new ArgumentException("The project does not contain that project reference.");

            if (!_projectReferencesAddedInBatch.Remove(projectReference))
                _projectReferencesRemovedInBatch.Add(projectReference);
        }
    }

    #endregion

    public void RemoveFromWorkspace()
    {
        using (_gate.DisposableWait())
        {
            if (!_projectSystemProjectFactory.Workspace.CurrentSolution.ContainsProject(Id))
            {
                throw new InvalidOperationException("The project has already been removed.");
            }

            _asynchronousFileChangeProcessingCancellationTokenSource.Cancel();

            // clear tracking to external components
            foreach (var provider in _eventSubscriptionTracker)
            {
                provider.Updated -= OnDynamicFileInfoUpdated;
            }

            _eventSubscriptionTracker.Clear();
        }

        _documentFileChangeContext.Dispose();

        IReadOnlyList<MetadataReference>? remainingMetadataReferences = null;

        _projectSystemProjectFactory.ApplyChangeToWorkspace(w =>
        {
            // Acquire the remaining metadata references inside the workspace lock. This is critical
            // as another project being removed at the same time could result in project to project
            // references being converted to metadata references (or vice versa) and we might either
            // miss stopping a file watcher or might end up double-stopping a file watcher.
            remainingMetadataReferences = w.CurrentSolution.GetRequiredProject(Id).MetadataReferences;
            _projectSystemProjectFactory.RemoveProjectFromTrackingMaps_NoLock(Id);

            // If this is our last project, clear the entire solution.
            if (w.CurrentSolution.ProjectIds.Count == 1)
            {
                _projectSystemProjectFactory.RemoveSolution_NoLock();
            }
            else
            {
                _projectSystemProjectFactory.Workspace.OnProjectRemoved(Id);
            }
        });

        Contract.ThrowIfNull(remainingMetadataReferences);

        foreach (var reference in remainingMetadataReferences.OfType<PortableExecutableReference>())
            _projectSystemProjectFactory.FileWatchedPortableExecutableReferenceFactory.StopWatchingReference(reference);
    }

    public void ReorderSourceFiles(ImmutableArray<string> filePaths)
        => _sourceFiles.ReorderFiles(filePaths);

    /// <summary>
    /// Clears a list and zeros out the capacity. The lists we use for batching are likely to get large during an initial load, but after
    /// that point should never get that large again.
    /// </summary>
    private static void ClearAndZeroCapacity<T>(List<T> list)
    {
        list.Clear();
        list.Capacity = 0;
    }

    /// <summary>
    /// Clears a list and zeros out the capacity. The lists we use for batching are likely to get large during an initial load, but after
    /// that point should never get that large again.
    /// </summary>
    private static void ClearAndZeroCapacity<T>(ImmutableArray<T>.Builder list)
    {
        list.Clear();
        list.Capacity = 0;
    }
}
