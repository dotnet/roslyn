// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    using InteractiveHost::Microsoft.CodeAnalysis.Interactive;
    using RelativePathResolver = Scripting::Microsoft.CodeAnalysis.RelativePathResolver;

    internal sealed class InteractiveSession : IDisposable
    {
        public InteractiveHost Host { get; }

        private readonly IThreadingContext _threadingContext;
        private readonly InteractiveEvaluatorLanguageInfoProvider _languageInfo;
        private readonly InteractiveWorkspace _workspace;
        private readonly CancellationTokenSource _shutdownCancellationSource;
        private readonly string _hostDirectory;

        #region State only accessible by queued tasks

        // Use to serialize InteractiveHost process initialization and code execution.
        // The process may restart any time and we need to react to that by clearing 
        // the current solution and setting up the first submission project. 
        // At the same time a code submission might be in progress.
        // If we left these operations run in parallel we might get into a state
        // inconsistent with the state of the host.
        private readonly TaskQueue _taskQueue;

        private ProjectId? _lastSuccessfulSubmissionProjectId;
        private ProjectId? _currentSubmissionProjectId;
        public int SubmissionCount { get; private set; }

        private RemoteInitializationResult? _initializationResult;
        private InteractiveHostPlatformInfo _platformInfo;
        private ImmutableArray<string> _referenceSearchPaths;
        private ImmutableArray<string> _sourceSearchPaths;
        private string _workingDirectory;

        /// <summary>
        /// Buffers that need to be associated with a submission project once the process initialization completes.
        /// </summary>
        private readonly List<(ITextBuffer buffer, string name)> _pendingBuffers = new();

        #endregion

        public InteractiveSession(
            InteractiveWorkspace workspace,
            IThreadingContext threadingContext,
            IAsynchronousOperationListener listener,
            InteractiveEvaluatorLanguageInfoProvider languageInfo,
            string initialWorkingDirectory)
        {
            _workspace = workspace;
            _threadingContext = threadingContext;
            _languageInfo = languageInfo;
            _taskQueue = new TaskQueue(listener, TaskScheduler.Default);
            _shutdownCancellationSource = new CancellationTokenSource();

            // The following settings will apply when the REPL starts without .rsp file.
            // They are discarded once the REPL is reset.
            _referenceSearchPaths = ImmutableArray<string>.Empty;
            _sourceSearchPaths = ImmutableArray<string>.Empty;
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

        /// <summary>
        /// Invoked by <see cref="InteractiveHost"/> when a new process initialization completes.
        /// </summary>
        private void ProcessInitialized(InteractiveHostPlatformInfo platformInfo, InteractiveHostOptions options, RemoteExecutionResult result)
        {
            Contract.ThrowIfFalse(result.InitializationResult != null);

            _ = _taskQueue.ScheduleTask(nameof(ProcessInitialized), () =>
            {
                _workspace.ResetSolution();

                _currentSubmissionProjectId = null;
                _lastSuccessfulSubmissionProjectId = null;

                // update host state:
                _platformInfo = platformInfo;
                _initializationResult = result.InitializationResult;
                UpdatePathsNoLock(result);

                // Create submission projects for buffers that were added by the Interactive Window 
                // before the process initialization completed.
                foreach (var (buffer, languageName) in _pendingBuffers)
                {
                    AddSubmissionProjectNoLock(buffer, languageName);
                }

                _pendingBuffers.Clear();
            }, _shutdownCancellationSource.Token);
        }

        /// <summary>
        /// Invoked on UI thread when a new language buffer is created and before it is added to the projection.
        /// </summary>
        internal void AddSubmissionProject(ITextBuffer submissionBuffer)
        {
            _taskQueue.ScheduleTask(
                nameof(AddSubmissionProject),
                () => AddSubmissionProjectNoLock(submissionBuffer, _languageInfo.LanguageName),
                _shutdownCancellationSource.Token);
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
                    imports = ImmutableArray<string>.Empty;
                    references = ImmutableArray<MetadataReference>.Empty;
                }
            }

            var newSubmissionProjectName = CreateNewSubmissionName();
            var newSubmissionText = submissionBuffer.CurrentSnapshot.AsText();
            _currentSubmissionProjectId = ProjectId.CreateNewId(newSubmissionProjectName);
            var newSubmissionDocumentId = DocumentId.CreateNewId(_currentSubmissionProjectId, newSubmissionProjectName);

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
                        new FileTextLoader(initializationScriptPath, defaultEncoding: null));
                }

                var newSubmissionProject = CreateSubmissionProjectNoLock(solution, _currentSubmissionProjectId, _lastSuccessfulSubmissionProjectId, languageName, imports, references);
                solution = newSubmissionProject.Solution.AddDocument(
                    newSubmissionDocumentId,
                    newSubmissionProjectName,
                    newSubmissionText);

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
                    newSubmissionProjectId,
                    VersionStamp.Create(),
                    name: name,
                    assemblyName: name,
                    language: languageName,
                    compilationOptions: compilationOptions,
                    parseOptions: _languageInfo.ParseOptions,
                    documents: null,
                    projectReferences: null,
                    metadataReferences: references,
                    hostObjectType: typeof(InteractiveScriptGlobals),
                    isSubmission: true));

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
        internal Task<bool> ExecuteCodeAsync(string text)
        {
            return _taskQueue.ScheduleTask(nameof(ExecuteCodeAsync), async () =>
            {
                var result = await Host.ExecuteAsync(text).ConfigureAwait(false);
                if (result.Success)
                {
                    _lastSuccessfulSubmissionProjectId = _currentSubmissionProjectId;

                    // update local search paths - remote paths has already been updated
                    UpdatePathsNoLock(result);
                }

                return result.Success;
            }, _shutdownCancellationSource.Token);
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
                throw ExceptionUtilities.Unreachable;
            }
        }

        public InteractiveHostOptions GetHostOptions(bool initialize, InteractiveHostPlatform? platform)
            => InteractiveHostOptions.CreateFromDirectory(
                _hostDirectory,
                initialize ? _languageInfo.InteractiveResponseFileName : null,
                CultureInfo.CurrentUICulture,
                 platform ?? Host.OptionsOpt?.Platform ?? InteractiveHost.DefaultPlatform);

        private static RuntimeMetadataReferenceResolver CreateMetadataReferenceResolver(IMetadataService metadataService, InteractiveHostPlatformInfo platformInfo, ImmutableArray<string> searchPaths, string baseDirectory)
        {
            return new RuntimeMetadataReferenceResolver(
                searchPaths,
                baseDirectory,
                gacFileResolver: platformInfo.HasGlobalAssemblyCache ? new GacFileResolver(preferredCulture: CultureInfo.CurrentCulture) : null,
                platformAssemblyPaths: platformInfo.PlatformAssemblyPaths,
                fileReferenceProvider: (path, properties) => metadataService.GetReference(path, properties));
        }

        private static SourceReferenceResolver CreateSourceReferenceResolver(ImmutableArray<string> searchPaths, string baseDirectory)
            => new SourceFileResolver(searchPaths, baseDirectory);

        public Task SetPathsAsync(ImmutableArray<string> referenceSearchPaths, ImmutableArray<string> sourceSearchPaths, string workingDirectory)
        {
            return _taskQueue.ScheduleTask(nameof(ExecuteCodeAsync), async () =>
            {
                var result = await Host.SetPathsAsync(referenceSearchPaths, sourceSearchPaths, workingDirectory).ConfigureAwait(false);
                UpdatePathsNoLock(result);
            }, _shutdownCancellationSource.Token);
        }

        private void UpdatePathsNoLock(RemoteExecutionResult result)
        {
            _workingDirectory = result.WorkingDirectory;
            _referenceSearchPaths = result.ReferencePaths;
            _sourceSearchPaths = result.SourcePaths;
        }
    }
}
