﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias InteractiveHost;
extern alias Scripting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive;

using InteractiveHost::Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Threading;
using RelativePathResolver = Scripting::Microsoft.CodeAnalysis.RelativePathResolver;

internal sealed class InteractiveSession : IDisposable
{
    public InteractiveHost Host { get; }

    private readonly InteractiveEvaluatorLanguageInfoProvider _languageInfo;
    private readonly InteractiveWorkspace _workspace;
    private readonly ITextDocumentFactoryService _textDocumentFactoryService;

    private readonly CancellationTokenSource _shutdownCancellationSource;

    /// <summary>
    /// The top level directory where all the interactive host extensions are installed (both Core and Desktop).
    /// </summary>
    private readonly string _hostDirectory;

    #region State only accessible by queued tasks

    // Use to serialize InteractiveHost process initialization and code execution.
    // The process may restart any time and we need to react to that by clearing 
    // the current solution and setting up the first submission project. 
    // At the same time a code submission might be in progress.
    // If we left these operations run in parallel we might get into a state
    // inconsistent with the state of the host.
    private readonly AsyncBatchingWorkQueue<Func<Task>> _workQueue;

    private ProjectId? _lastSuccessfulSubmissionProjectId;
    private ProjectId? _currentSubmissionProjectId;
    public int SubmissionCount { get; private set; }

    private RemoteInitializationResult? _initializationResult;
    private InteractiveHostPlatformInfo _platformInfo;
    private ImmutableArray<string> _referenceSearchPaths;
    private ImmutableArray<string> _sourceSearchPaths;
    private string _workingDirectory;
    private InteractiveHostOptions? _hostOptions;

    /// <summary>
    /// Buffers that need to be associated with a submission project once the process initialization completes.
    /// </summary>
    private readonly List<(ITextBuffer buffer, string name)> _pendingBuffers = [];

    #endregion

    public InteractiveSession(
        InteractiveWorkspace workspace,
        IAsynchronousOperationListener listener,
        ITextDocumentFactoryService documentFactory,
        InteractiveEvaluatorLanguageInfoProvider languageInfo,
        string initialWorkingDirectory)
    {
        _workspace = workspace;
        _languageInfo = languageInfo;
        _textDocumentFactoryService = documentFactory;

        _shutdownCancellationSource = new CancellationTokenSource();
        _workQueue = new(
            TimeSpan.Zero,
            ProcessWorkQueueAsync,
            listener,
            _shutdownCancellationSource.Token);

        // The following settings will apply when the REPL starts without .rsp file.
        // They are discarded once the REPL is reset.
        _referenceSearchPaths = [];
        _sourceSearchPaths = [];
        _workingDirectory = initialWorkingDirectory;

        _hostDirectory = Path.Combine(Path.GetDirectoryName(typeof(InteractiveSession).Assembly.Location)!, "InteractiveHost");

        Host = new InteractiveHost(languageInfo.ReplServiceProviderType, initialWorkingDirectory);
        Host.ProcessInitialized += ProcessInitialized;
    }

    public void Dispose()
    {
        _shutdownCancellationSource.Cancel();
        _shutdownCancellationSource.Dispose();

        Host.Dispose();
    }

    private async ValueTask ProcessWorkQueueAsync(ImmutableSegmentedList<Func<Task>> list, CancellationToken cancellationToken)
    {
        foreach (var taskCreator in list)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Kick off the task to run.  This also ensures that if the taskCreator func throws synchronously, that we
            // appropriately handle reporting it in ReportNonFatalErrorAsync.  Also,  ensure we always process the next
            // piece of work, even if the current work fails.
            var task = Task.Run(taskCreator, cancellationToken);
            _ = task.ReportNonFatalErrorAsync();
            await task.NoThrowAwaitableInternal(captureContext: false);
        }
    }

    /// <summary>
    /// Invoked by <see cref="InteractiveHost"/> when a new process initialization completes.
    /// </summary>
    private void ProcessInitialized(InteractiveHostPlatformInfo platformInfo, InteractiveHostOptions options, RemoteExecutionResult result)
    {
        Contract.ThrowIfFalse(result.InitializationResult != null);

        _workQueue.AddWork(() =>
        {
            _workspace.ResetSolution();

            _currentSubmissionProjectId = null;
            _lastSuccessfulSubmissionProjectId = null;

            // update host state:
            _platformInfo = platformInfo;
            _initializationResult = result.InitializationResult;
            _hostOptions = options;
            UpdatePathsNoLock(result);

            // Create submission projects for buffers that were added by the Interactive Window 
            // before the process initialization completed.
            foreach (var (buffer, languageName) in _pendingBuffers)
            {
                AddSubmissionProjectNoLock(buffer, languageName);
            }

            _pendingBuffers.Clear();
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Invoked on UI thread when a new language buffer is created and before it is added to the projection.
    /// </summary>
    internal void AddSubmissionProject(ITextBuffer submissionBuffer)
    {
        _workQueue.AddWork(
            () =>
            {
                AddSubmissionProjectNoLock(submissionBuffer, _languageInfo.LanguageName);
                return Task.CompletedTask;
            });
    }

    private void AddSubmissionProjectNoLock(ITextBuffer submissionBuffer, string languageName)
    {
        var initResult = _initializationResult;

        var imports = ImmutableArray<string>.Empty;
        var references = ImmutableArray<MetadataReference>.Empty;

        var initializationScriptImports = ImmutableArray<string>.Empty;
        var initializationScriptReferences = ImmutableArray<MetadataReference>.Empty;

        ProjectId? initializationScriptProjectId = null;
        string? initializationScriptPath = null;

        if (_currentSubmissionProjectId == null)
        {
            Debug.Assert(_lastSuccessfulSubmissionProjectId == null);

            // The Interactive Window may have added the first language buffer before 
            // the host initialization has completed. Do not create a submission project 
            // for the buffer in such case. It will be created when the initialization completes.
            if (initResult == null)
            {
                _pendingBuffers.Add((submissionBuffer, languageName));
                return;
            }

            imports = initResult.Imports.ToImmutableArrayOrEmpty();

            var metadataService = _workspace.Services.GetRequiredService<IMetadataService>();
            references = initResult.MetadataReferencePaths.ToImmutableArrayOrEmpty().SelectAsArray(
                (path, metadataService) => (MetadataReference)metadataService.GetReference(path, MetadataReferenceProperties.Assembly),
                metadataService);

            // if a script was specified in .rps file insert a project with a document that represents it:
            initializationScriptPath = initResult!.ScriptPath;
            if (initializationScriptPath != null)
            {
                initializationScriptProjectId = ProjectId.CreateNewId(CreateNewSubmissionName());

                _lastSuccessfulSubmissionProjectId = initializationScriptProjectId;

                // imports and references will be inherited:
                initializationScriptImports = imports;
                initializationScriptReferences = references;
                imports = [];
                references = [];
            }
        }

        var newSubmissionProjectName = CreateNewSubmissionName();
        var newSubmissionText = submissionBuffer.CurrentSnapshot.AsText();
        _currentSubmissionProjectId = ProjectId.CreateNewId(newSubmissionProjectName);
        var newSubmissionDocumentId = DocumentId.CreateNewId(_currentSubmissionProjectId, newSubmissionProjectName);

        // If the _initializationResult is not null we must also have the host options.
        RoslynDebug.AssertNotNull(_hostOptions);
        // Retrieve the directory that the host path exe is located in.
        var hostPathDirectory = Path.GetDirectoryName(_hostOptions.HostPath);
        RoslynDebug.AssertNotNull(hostPathDirectory);

        // Create a file path for the submission located in the interactive host directory.
        var newSubmissionFilePath = Path.Combine(hostPathDirectory, $"Submission{SubmissionCount}{_languageInfo.Extension}");

        // Associate the path with both the editor document and our roslyn document so LSP can make requests on it.
        _textDocumentFactoryService.TryGetTextDocument(submissionBuffer, out var textDocument);
        textDocument.Rename(newSubmissionFilePath);

        // Chain projects to the the last submission that successfully executed.
        _workspace.SetCurrentSolution(solution =>
        {
            if (initializationScriptProjectId != null)
            {
                RoslynDebug.AssertNotNull(initializationScriptPath);

                var initProject = CreateSubmissionProjectNoLock(solution, initializationScriptProjectId, previousSubmissionProjectId: null, languageName, initializationScriptImports, initializationScriptReferences);
                solution = initProject.Solution.AddDocument(
                    DocumentId.CreateNewId(initializationScriptProjectId, debugName: initializationScriptPath),
                    Path.GetFileName(initializationScriptPath),
                    new WorkspaceFileTextLoader(solution.Services, initializationScriptPath, defaultEncoding: null));
            }

            var newSubmissionProject = CreateSubmissionProjectNoLock(solution, _currentSubmissionProjectId, _lastSuccessfulSubmissionProjectId, languageName, imports, references);
            solution = newSubmissionProject.Solution.AddDocument(
                newSubmissionDocumentId,
                newSubmissionProjectName,
                newSubmissionText,
                filePath: newSubmissionFilePath);

            return solution;
        }, WorkspaceChangeKind.SolutionChanged);

        // opening document will start workspace listening to changes in this text container
        _workspace.OpenDocument(newSubmissionDocumentId, submissionBuffer.AsTextContainer());
    }

    private string CreateNewSubmissionName()
        => "Submission#" + SubmissionCount++;

    private Project CreateSubmissionProjectNoLock(Solution solution, ProjectId newSubmissionProjectId, ProjectId? previousSubmissionProjectId, string languageName, ImmutableArray<string> imports, ImmutableArray<MetadataReference> references)
    {
        var name = newSubmissionProjectId.DebugName;
        RoslynDebug.AssertNotNull(name);

        CompilationOptions compilationOptions;
        if (previousSubmissionProjectId != null)
        {
            compilationOptions = solution.GetRequiredProject(previousSubmissionProjectId).CompilationOptions!;

            var metadataResolver = (RuntimeMetadataReferenceResolver)compilationOptions.MetadataReferenceResolver!;
            if (metadataResolver.PathResolver.BaseDirectory != _workingDirectory ||
                !metadataResolver.PathResolver.SearchPaths.SequenceEqual(_referenceSearchPaths))
            {
                compilationOptions = compilationOptions.WithMetadataReferenceResolver(metadataResolver.WithRelativePathResolver(new RelativePathResolver(_referenceSearchPaths, _workingDirectory)));
            }

            var sourceResolver = (SourceFileResolver)compilationOptions.SourceReferenceResolver!;
            if (sourceResolver.BaseDirectory != _workingDirectory ||
                !sourceResolver.SearchPaths.SequenceEqual(_sourceSearchPaths))
            {
                compilationOptions = compilationOptions.WithSourceReferenceResolver(CreateSourceReferenceResolver(sourceResolver.SearchPaths, _workingDirectory));
            }
        }
        else
        {
            var metadataService = _workspace.Services.GetRequiredService<IMetadataService>();
            compilationOptions = _languageInfo.GetSubmissionCompilationOptions(
                name,
                CreateMetadataReferenceResolver(metadataService, _platformInfo, _referenceSearchPaths, _workingDirectory),
                CreateSourceReferenceResolver(_sourceSearchPaths, _workingDirectory),
                imports);
        }

        solution = solution.AddProject(
            ProjectInfo.Create(
                new ProjectInfo.ProjectAttributes(
                    id: newSubmissionProjectId,
                    version: VersionStamp.Create(),
                    name: name,
                    assemblyName: name,
                    language: languageName,
                    compilationOutputInfo: default,
                    checksumAlgorithm: SourceHashAlgorithms.Default,
                    isSubmission: true),
                compilationOptions: compilationOptions,
                parseOptions: _languageInfo.ParseOptions,
                documents: null,
                projectReferences: null,
                metadataReferences: references,
                hostObjectType: typeof(InteractiveScriptGlobals)));

        if (previousSubmissionProjectId != null)
        {
            solution = solution.AddProjectReference(newSubmissionProjectId, new ProjectReference(previousSubmissionProjectId));
        }

        return solution.GetRequiredProject(newSubmissionProjectId);
    }

    /// <summary>
    /// Called once a code snippet is submitted.
    /// Followed by creation of a new language buffer and call to <see cref="AddSubmissionProject(ITextBuffer)"/>.
    /// </summary>
    internal async Task<bool> ExecuteCodeAsync(string text)
    {
        var returnValue = false;
        _workQueue.AddWork(async () =>
        {
            var result = await Host.ExecuteAsync(text).ConfigureAwait(false);
            if (result.Success)
            {
                _lastSuccessfulSubmissionProjectId = _currentSubmissionProjectId;

                // update local search paths - remote paths has already been updated
                UpdatePathsNoLock(result);
            }

            returnValue = result.Success;
        });

        await _workQueue.WaitUntilCurrentBatchCompletesAsync().ConfigureAwait(false);
        return returnValue;
    }

    internal async Task<bool> ResetAsync(InteractiveHostOptions options)
    {
        try
        {
            // Do not queue reset operation - invoke it directly.
            // Code execution might be in progress when the user requests reset (via a reset button, or process terminating on its own).
            // We need the execution to be interrupted and the process restarted, not wait for it to complete.

            var result = await Host.ResetAsync(options).ConfigureAwait(false);

            // Note: Not calling UpdatePathsNoLock here. The paths will be updated by ProcessInitialized 
            // which is executed once the new host process finishes its initialization.

            return result.Success;
        }
        catch (Exception e) when (FatalError.ReportAndPropagate(e))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    public InteractiveHostOptions GetHostOptions(bool initialize, InteractiveHostPlatform? platform)
        => InteractiveHostOptions.CreateFromDirectory(
            _hostDirectory,
            initialize ? _languageInfo.InteractiveResponseFileName : null,
            CultureInfo.CurrentCulture,
            CultureInfo.CurrentUICulture,
             platform ?? Host.OptionsOpt?.Platform ?? InteractiveHost.DefaultPlatform);

    private static RuntimeMetadataReferenceResolver CreateMetadataReferenceResolver(IMetadataService metadataService, InteractiveHostPlatformInfo platformInfo, ImmutableArray<string> searchPaths, string baseDirectory)
    {
        return new RuntimeMetadataReferenceResolver(
            searchPaths,
            baseDirectory,
            gacFileResolver: platformInfo.HasGlobalAssemblyCache ? new GacFileResolver(preferredCulture: CultureInfo.CurrentCulture) : null,
            platformAssemblyPaths: platformInfo.PlatformAssemblyPaths,
            createFromFileFunc: metadataService.GetReference);
    }

    private static SourceReferenceResolver CreateSourceReferenceResolver(ImmutableArray<string> searchPaths, string baseDirectory)
        => new SourceFileResolver(searchPaths, baseDirectory);

    public Task SetPathsAsync(ImmutableArray<string> referenceSearchPaths, ImmutableArray<string> sourceSearchPaths, string workingDirectory)
    {
        _workQueue.AddWork(async () =>
        {
            var result = await Host.SetPathsAsync(referenceSearchPaths, sourceSearchPaths, workingDirectory).ConfigureAwait(false);
            UpdatePathsNoLock(result);
        });

        return _workQueue.WaitUntilCurrentBatchCompletesAsync();
    }

    private void UpdatePathsNoLock(RemoteExecutionResult result)
    {
        _workingDirectory = result.WorkingDirectory;
        _referenceSearchPaths = result.ReferencePaths;
        _sourceSearchPaths = result.SourcePaths;
    }
}
