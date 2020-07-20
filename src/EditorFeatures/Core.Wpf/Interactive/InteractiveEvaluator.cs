// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

extern alias InteractiveHost;
extern alias Scripting;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Interactive;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Interactive
{
    using InteractiveHost::Microsoft.CodeAnalysis.Interactive;
    using RelativePathResolver = Scripting::Microsoft.CodeAnalysis.RelativePathResolver;

    internal abstract class InteractiveEvaluator : IResettableInteractiveEvaluator
    {
        private const string CommandPrefix = "#";

        private readonly string _responseFileName;

        private readonly InteractiveHost _interactiveHost;

        private readonly string _hostDirectory;

        private readonly IThreadingContext _threadingContext;
        private readonly IContentType _contentType;
        private readonly IViewClassifierAggregatorService _classifierAggregator;
        private readonly IInteractiveWindowCommandsFactory _commandsFactory;
        private readonly ImmutableArray<IInteractiveWindowCommand> _commands;
        private readonly CancellationTokenSource _shutdownCancellationSource;

        private IInteractiveWindow? _lazyInteractiveWindow;
        private IInteractiveWindowCommands? _lazyInteractiveCommands;

        #region UI Thread only

        /// <remarks>
        /// Submission buffers in the order they were submitted. 
        /// Includes both command buffers as well as language buffers.
        /// Does not include the current buffer unless it has been submitted.
        /// </remarks>
        private readonly List<ITextBuffer> _submittedBuffers = new List<ITextBuffer>();

        #endregion

        #region State only accessible by queued tasks

        // Use to serialize InteractiveHost process initialization and code execution.
        // The process may restart any time and we need to react to that by clearing 
        // the current solution and setting up the first submission project. 
        // At the same time a code submission might be in progress.
        // If we left these operations run in parallel we might get into a state
        // inconsistent with the state of the host.
        private readonly TaskQueue _taskQueue;

        private readonly InteractiveWorkspace _workspace;
        private ProjectId? _lastSuccessfulSubmissionProjectId;
        private ProjectId? _currentSubmissionProjectId;
        public int SubmissionCount { get; private set; }

        private RemoteInitializationResult? _initializationResult;
        private InteractiveHostPlatformInfo _platformInfo;
        public ImmutableArray<string> ReferenceSearchPaths { get; private set; }
        public ImmutableArray<string> SourceSearchPaths { get; private set; }
        public string WorkingDirectory { get; private set; }

        /// <summary>
        /// Buffers that need to be associated with a submission project once the process initialization completes.
        /// </summary>
        private readonly List<(ITextBuffer buffer, string name)> _pendingBuffers = new List<(ITextBuffer, string)>();

        #endregion

        internal InteractiveEvaluatorResetOptions ResetOptions { get; set; }
            = new InteractiveEvaluatorResetOptions(InteractiveHostPlatform.Desktop64);

        InteractiveEvaluatorResetOptions IResettableInteractiveEvaluator.ResetOptions { get => ResetOptions; set => ResetOptions = value; }

        internal InteractiveEvaluator(
            IThreadingContext threadingContext,
            IAsynchronousOperationListener listener,
            IContentType contentType,
            HostServices hostServices,
            IViewClassifierAggregatorService classifierAggregator,
            IInteractiveWindowCommandsFactory commandsFactory,
            ImmutableArray<IInteractiveWindowCommand> commands,
            string responseFileName,
            string initialWorkingDirectory,
            Type replType)
        {
            Debug.Assert(responseFileName.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) == -1);

            _threadingContext = threadingContext;
            _taskQueue = new TaskQueue(listener, TaskScheduler.Default);
            _shutdownCancellationSource = new CancellationTokenSource();
            _contentType = contentType;
            _responseFileName = responseFileName;
            _workspace = new InteractiveWorkspace(hostServices, this);
            _classifierAggregator = classifierAggregator;
            _commandsFactory = commandsFactory;
            _commands = commands;
            _hostDirectory = Path.Combine(Path.GetDirectoryName(typeof(InteractiveEvaluator).Assembly.Location), "InteractiveHost");

            // The following settings will apply when the REPL starts without .rsp file.
            // They are discarded once the REPL is reset.
            ReferenceSearchPaths = ImmutableArray<string>.Empty;
            SourceSearchPaths = ImmutableArray<string>.Empty;
            WorkingDirectory = initialWorkingDirectory;

            _interactiveHost = new InteractiveHost(replType, initialWorkingDirectory);
            _interactiveHost.ProcessInitialized += ProcessInitialized;
        }

        public IContentType ContentType => _contentType;

        public IInteractiveWindow? CurrentWindow
        {
            get => _lazyInteractiveWindow;

            set
            {
                _threadingContext.ThrowIfNotOnUIThread();

                if (_lazyInteractiveWindow != null)
                {
                    throw new NotSupportedException(InteractiveEditorFeaturesResources.The_CurrentWindow_property_may_only_be_assigned_once);
                }

                _lazyInteractiveWindow = value ?? throw new ArgumentNullException(nameof(value));
                _workspace.Window = value;

                Task.Run(() => _interactiveHost.SetOutputs(value.OutputWriter, value.ErrorOutputWriter));

                value.SubmissionBufferAdded += SubmissionBufferAdded;
                _lazyInteractiveCommands = _commandsFactory.CreateInteractiveCommands(value, CommandPrefix, _commands);
            }
        }

        protected abstract string LanguageName { get; }
        protected abstract CompilationOptions GetSubmissionCompilationOptions(string name, MetadataReferenceResolver metadataReferenceResolver, SourceReferenceResolver sourceReferenceResolver, ImmutableArray<string> imports);
        protected abstract ParseOptions ParseOptions { get; }
        protected abstract CommandLineParser CommandLineParser { get; }

        /// <summary>
        /// Invoked before the process is reset. The argument is the value of <see cref="InteractiveHostOptions.Platform"/>.
        /// </summary>
        public event Action<InteractiveHostPlatform>? OnBeforeReset;

        #region Initialization

        private IInteractiveWindow GetInteractiveWindow()
            => _lazyInteractiveWindow ?? throw new InvalidOperationException(EditorFeaturesResources.Engine_must_be_attached_to_an_Interactive_Window);

        private IInteractiveWindowCommands GetInteractiveCommands()
            => _lazyInteractiveCommands ?? throw new InvalidOperationException(EditorFeaturesResources.Engine_must_be_attached_to_an_Interactive_Window);

        public void Dispose()
        {
            _shutdownCancellationSource.Cancel();
            _shutdownCancellationSource.Dispose();

            _workspace.Dispose();
            _interactiveHost.Dispose();

            var interactiveWindow = _lazyInteractiveWindow;
            if (interactiveWindow != null)
            {
                interactiveWindow.SubmissionBufferAdded -= SubmissionBufferAdded;
            }
        }

        private void CaptureClassificationSpans()
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var textView = GetInteractiveWindow().TextView;

            // Freeze all existing classifications and then clear the list of submission buffers we have.
            foreach (var textBuffer in _submittedBuffers)
            {
                InertClassifierProvider.CaptureExistingClassificationSpans(_classifierAggregator, textView, textBuffer);
            }

            _submittedBuffers.Clear();
        }

        /// <summary>
        /// Invoked by <see cref="InteractiveHost"/> when a new process initialization completes.
        /// </summary>
        private void ProcessInitialized(InteractiveHostPlatformInfo platformInfo, InteractiveHostOptions options, RemoteExecutionResult result)
        {
            Contract.ThrowIfFalse(result.InitializationResult != null);

            _ = _threadingContext.JoinableTaskFactory.RunAsync(async () =>
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                CaptureClassificationSpans();
            });

            _ = _taskQueue.ScheduleTask(nameof(ProcessInitialized), () =>
            {
                // clear workspace state:
                _workspace.ClearSolution();
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

        #endregion

        #region Workspace

        /// <summary>
        /// Invoked on UI thread when a new language buffer is created and before it is added to the projection.
        /// </summary>
        private void SubmissionBufferAdded(object sender, SubmissionBufferAddedEventArgs args)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            _taskQueue.ScheduleTask(nameof(SubmissionBufferAdded), () => AddSubmissionProjectNoLock(args.NewBuffer, LanguageName), _shutdownCancellationSource.Token);
        }

        private void AddSubmissionProjectNoLock(ITextBuffer submissionBuffer, string languageName)
        {
            var solution = _workspace.CurrentSolution;
            Project project;
            var imports = ImmutableArray<string>.Empty;
            var references = ImmutableArray<MetadataReference>.Empty;

            if (_currentSubmissionProjectId == null)
            {
                Debug.Assert(_lastSuccessfulSubmissionProjectId == null);

                // The Interactive Window may have added the first language buffer before 
                // the host initialization has completed. Do not create a submission project 
                // for the buffer in such case. It will be created when the initialization completes.
                if (_initializationResult == null)
                {
                    _pendingBuffers.Add((submissionBuffer, languageName));
                    return;
                }

                var initResult = _initializationResult;

                imports = initResult.Imports.ToImmutableArrayOrEmpty();

                var metadataService = _workspace.Services.GetRequiredService<IMetadataService>();
                references = initResult.MetadataReferencePaths.ToImmutableArrayOrEmpty().SelectAsArray(
                    (path, metadataService) => (MetadataReference)metadataService.GetReference(path, MetadataReferenceProperties.Assembly),
                    metadataService);

                // if a script was specified in .rps file insert a project with a document that represents it:
                var scriptPath = initResult.ScriptPath;
                if (scriptPath != null)
                {
                    project = CreateSubmissionProjectNoLock(solution, previousSubmissionProjectId: null, languageName, imports, references);

                    var initDocumentId = DocumentId.CreateNewId(project.Id, debugName: scriptPath);
                    solution = project.Solution.AddDocument(initDocumentId, Path.GetFileName(scriptPath), new FileTextLoader(scriptPath, defaultEncoding: null));
                    _lastSuccessfulSubmissionProjectId = project.Id;

                    // imports and references will be inherited:
                    imports = ImmutableArray<string>.Empty;
                    references = ImmutableArray<MetadataReference>.Empty;
                }
            }

            // Project for the new submission - chain to the last submission that successfully executed.
            project = CreateSubmissionProjectNoLock(solution, _lastSuccessfulSubmissionProjectId, languageName, imports, references);

            var documentId = DocumentId.CreateNewId(project.Id, debugName: project.Name);
            solution = project.Solution.AddDocument(documentId, project.Name, submissionBuffer.CurrentSnapshot.AsText());

            _workspace.SetCurrentSolution(solution);

            // opening document will start workspace listening to changes in this text container
            _workspace.OpenDocument(documentId, submissionBuffer.AsTextContainer());

            _currentSubmissionProjectId = project.Id;
        }

        private Project CreateSubmissionProjectNoLock(Solution solution, ProjectId? previousSubmissionProjectId, string languageName, ImmutableArray<string> imports, ImmutableArray<MetadataReference> references)
        {
            var name = "Submission#" + SubmissionCount++;

            CompilationOptions compilationOptions;
            if (previousSubmissionProjectId != null)
            {
                compilationOptions = solution.GetRequiredProject(previousSubmissionProjectId).CompilationOptions!;

                var metadataResolver = (RuntimeMetadataReferenceResolver)compilationOptions.MetadataReferenceResolver!;
                if (metadataResolver.PathResolver.BaseDirectory != WorkingDirectory ||
                    !metadataResolver.PathResolver.SearchPaths.SequenceEqual(ReferenceSearchPaths))
                {
                    compilationOptions = compilationOptions.WithMetadataReferenceResolver(metadataResolver.WithRelativePathResolver(new RelativePathResolver(ReferenceSearchPaths, WorkingDirectory)));
                }

                var sourceResolver = (SourceFileResolver)compilationOptions.SourceReferenceResolver!;
                if (sourceResolver.BaseDirectory != WorkingDirectory ||
                    !sourceResolver.SearchPaths.SequenceEqual(SourceSearchPaths))
                {
                    compilationOptions = compilationOptions.WithSourceReferenceResolver(CreateSourceReferenceResolver(sourceResolver.SearchPaths, WorkingDirectory));
                }
            }
            else
            {
                var metadataService = _workspace.Services.GetRequiredService<IMetadataService>();
                compilationOptions = GetSubmissionCompilationOptions(
                    name,
                    CreateMetadataReferenceResolver(metadataService, _platformInfo, ReferenceSearchPaths, WorkingDirectory),
                    CreateSourceReferenceResolver(SourceSearchPaths, WorkingDirectory),
                    imports);
            }

            var projectId = ProjectId.CreateNewId(debugName: name);

            solution = solution.AddProject(
                ProjectInfo.Create(
                    projectId,
                    VersionStamp.Create(),
                    name: name,
                    assemblyName: name,
                    language: languageName,
                    compilationOptions: compilationOptions,
                    parseOptions: ParseOptions,
                    documents: null,
                    projectReferences: null,
                    metadataReferences: references,
                    hostObjectType: typeof(InteractiveScriptGlobals),
                    isSubmission: true));

            if (previousSubmissionProjectId != null)
            {
                solution = solution.AddProjectReference(projectId, new ProjectReference(previousSubmissionProjectId));
            }

            return solution.GetProject(projectId)!;
        }

        #endregion

        #region IInteractiveEngine

        public virtual bool CanExecuteCode(string text)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            return _lazyInteractiveCommands?.InCommand == true;
        }

        /// <summary>
        /// Invoked when the Interactive Window is created.
        /// </summary>
        Task<ExecutionResult> IInteractiveEvaluator.InitializeAsync()
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var window = GetInteractiveWindow();
            var resetOptions = ResetOptions;

            _interactiveHost.SetOutputs(window.OutputWriter, window.ErrorOutputWriter);

            return ResetCoreAsync(GetHostOptions(initialize: true, resetOptions.Platform));
        }

        /// <summary>
        /// Invoked by the reset toolbar button.
        /// </summary>
        Task<ExecutionResult> IInteractiveEvaluator.ResetAsync(bool initialize)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var window = GetInteractiveWindow();

            var resetOptions = ResetOptions;
            Debug.Assert(GetInteractiveCommands().CommandPrefix == CommandPrefix);
            window.AddInput(CommandPrefix + ResetCommand.GetCommandLine(initialize, resetOptions.Platform));
            window.WriteLine(InteractiveEditorFeaturesResources.Resetting_execution_engine);
            window.FlushOutput();

            return ResetCoreAsync(GetHostOptions(initialize, resetOptions.Platform));
        }

        public InteractiveHostOptions GetHostOptions(bool initialize, InteractiveHostPlatform? platform)
            => InteractiveHostOptions.CreateFromDirectory(
                _hostDirectory,
                initialize ? _responseFileName : null,
                CultureInfo.CurrentUICulture,
                 platform ?? _interactiveHost.OptionsOpt?.Platform ?? InteractiveHost.DefaultPlatform);

        private async Task<ExecutionResult> ResetCoreAsync(InteractiveHostOptions options)
        {
            try
            {
                _threadingContext.ThrowIfNotOnUIThread();

                OnBeforeReset?.Invoke(options.Platform);

                // Do not queue reset operation - invoke it directly.
                // Code execution might be in progress when the user requests reset (via a reset button, or process terminating on its own).
                // We need the execution to be interrupted and the process restarted, not wait for it to complete.

                var result = await _interactiveHost.ResetAsync(options).ConfigureAwait(false);

                // Note: Not calling UpdatePathsNoLock here. The paths will be updated by ProcessInitialized 
                // which is executed once the new host process finishes its initialization.

                return new ExecutionResult(result.Success);
            }
            catch (Exception e) when (FatalError.Report(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Called on UI thread by the Interactive Window once a code snippet is submitted.
        /// Followed on UI thread by creation of a new language buffer and call to <see cref="SubmissionBufferAdded"/>.
        /// </summary>
        public async Task<ExecutionResult> ExecuteCodeAsync(string text)
        {
            try
            {
                _threadingContext.ThrowIfNotOnUIThread();

                var window = GetInteractiveWindow();
                var commands = GetInteractiveCommands();

                var currentSubmissionBuffer = window.CurrentLanguageBuffer;
                Contract.ThrowIfNull(currentSubmissionBuffer);
                _submittedBuffers.Add(currentSubmissionBuffer);

                if (commands.InCommand)
                {
                    // Takes the content of the current language buffer, parses it as a command
                    // and returns a task that execute the command, or null if the text doesn't parse.
                    var commandTask = commands.TryExecuteCommand();
                    if (commandTask != null)
                    {
                        return await commandTask.ConfigureAwait(false);
                    }
                }

                // If process initialization is in progress we will wait with code 
                // execution after the initialization is completed.

                return await _taskQueue.ScheduleTask(nameof(ExecuteCodeAsync), async () =>
                {
                    var result = await _interactiveHost.ExecuteAsync(text).ConfigureAwait(false);
                    if (result.Success)
                    {
                        _lastSuccessfulSubmissionProjectId = _currentSubmissionProjectId;

                        // update local search paths - remote paths has already been updated
                        UpdatePathsNoLock(result);
                    }

                    return new ExecutionResult(result.Success);
                }, _shutdownCancellationSource.Token).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.Report(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public void AbortExecution()
        {
            // TODO (https://github.com/dotnet/roslyn/issues/4725)
        }

        public string? FormatClipboard()
        {
            // keep the clipboard content as is
            return null;
        }

        #endregion

        private void UpdatePathsNoLock(RemoteExecutionResult result)
        {
            WorkingDirectory = result.WorkingDirectory;
            ReferenceSearchPaths = result.ReferencePaths;
            SourceSearchPaths = result.SourcePaths;
        }

        public async Task SetPathsAsync(ImmutableArray<string> referenceSearchPaths, ImmutableArray<string> sourceSearchPaths, string workingDirectory)
        {
            try
            {
                var result = await _interactiveHost.SetPathsAsync(referenceSearchPaths.ToArray(), sourceSearchPaths.ToArray(), workingDirectory).ConfigureAwait(false);
                UpdatePathsNoLock(result);
            }
            catch (Exception e) when (FatalError.Report(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public string GetPrompt()
        {
            var buffer = GetInteractiveWindow().CurrentLanguageBuffer;
            return buffer != null && buffer.CurrentSnapshot.LineCount > 1
                ? ". "
                : "> ";
        }
    }
}
