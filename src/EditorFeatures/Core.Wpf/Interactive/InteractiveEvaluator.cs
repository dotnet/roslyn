// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias Scripting;
extern alias InteractiveHost;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.Interactive;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Interactive
{
    using RelativePathResolver = Scripting::Microsoft.CodeAnalysis.RelativePathResolver;
    using InteractiveHost::Microsoft.CodeAnalysis.Interactive;

    internal abstract class InteractiveEvaluator : IResettableInteractiveEvaluator
    {
        private const string CommandPrefix = "#";

        private readonly string _responseFileName;

        private readonly InteractiveHost _interactiveHost;

        private readonly string _hostDirectory;
        private readonly string _initialWorkingDirectory;

        private readonly IThreadingContext _threadingContext;
        private readonly IContentType _contentType;
        private readonly InteractiveWorkspace _workspace;
        private readonly IViewClassifierAggregatorService _classifierAggregator;
        private readonly IInteractiveWindowCommandsFactory _commandsFactory;
        private readonly ImmutableArray<IInteractiveWindowCommand> _commands;
        private readonly EventHandler<ContentTypeChangedEventArgs> _contentTypeChangedHandler;

        // InteractiveHost state:
        private MetadataReferenceResolver _metadataReferenceResolver;
        private SourceReferenceResolver _sourceReferenceResolver;
        private RemoteInitializationResult _initializationResult;
        public ImmutableArray<string> ReferenceSearchPaths { get; private set; }
        public ImmutableArray<string> SourceSearchPaths { get; private set; }
        public string WorkingDirectory { get; private set; }

        // InteractiveWorkspace state:
        private ProjectId _previousSubmissionProjectId;
        private ProjectId _currentSubmissionProjectId;

        // InteractiveWindow state:
        private IInteractiveWindow _currentWindow;
        private IInteractiveWindowCommands _interactiveCommands;

        /// <remarks>
        /// Submission buffers in the order they were submitted. 
        /// Includes both command buffers as well as language buffers.
        /// Does not include the current buffer unless it has been submitted.
        /// </remarks>
        private readonly List<ITextBuffer> _submittedBuffers = new List<ITextBuffer>();

        private int _submissionCount = 0;

        internal InteractiveEvaluatorResetOptions ResetOptions { get; set; }
            = new InteractiveEvaluatorResetOptions(InteractiveHostPlatform.Desktop64);

        InteractiveEvaluatorResetOptions IResettableInteractiveEvaluator.ResetOptions { get => ResetOptions; set => ResetOptions = value; }

        internal InteractiveEvaluator(
            IThreadingContext threadingContext,
            IContentType contentType,
            HostServices hostServices,
            IViewClassifierAggregatorService classifierAggregator,
            IInteractiveWindowCommandsFactory commandsFactory,
            ImmutableArray<IInteractiveWindowCommand> commands,
            string responseFileName,
            string initialWorkingDirectory,
            Type replType)
        {
            Debug.Assert(responseFileName == null || responseFileName.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }) == -1);

            _threadingContext = threadingContext;
            _contentType = contentType;
            _responseFileName = responseFileName;
            _workspace = new InteractiveWorkspace(hostServices, this);
            _contentTypeChangedHandler = new EventHandler<ContentTypeChangedEventArgs>(LanguageBufferContentTypeChanged);
            _classifierAggregator = classifierAggregator;
            _initialWorkingDirectory = initialWorkingDirectory;
            _commandsFactory = commandsFactory;
            _commands = commands;
            _hostDirectory = Path.Combine(Path.GetDirectoryName(typeof(InteractiveEvaluator).Assembly.Location), "InteractiveHost");

            // The following settings will apply when the REPL starts without .rsp file.
            // They are discarded once the REPL is reset.
            ReferenceSearchPaths = ImmutableArray<string>.Empty;
            SourceSearchPaths = ImmutableArray<string>.Empty;
            WorkingDirectory = initialWorkingDirectory;
            var metadataService = _workspace.CurrentSolution.Services.MetadataService;
            _metadataReferenceResolver = CreateMetadataReferenceResolver(metadataService, ReferenceSearchPaths, _initialWorkingDirectory);
            _sourceReferenceResolver = CreateSourceReferenceResolver(SourceSearchPaths, _initialWorkingDirectory);

            _interactiveHost = new InteractiveHost(replType, initialWorkingDirectory);
            _interactiveHost.ProcessInitialized += ProcessInitialized;
        }

        public int SubmissionCount => _submissionCount;

        public IContentType ContentType
        {
            get
            {
                return _contentType;
            }
        }

        public IInteractiveWindow CurrentWindow
        {
            get
            {
                return _currentWindow;
            }

            set
            {
                if (_currentWindow != null)
                {
                    throw new NotSupportedException(InteractiveEditorFeaturesResources.The_CurrentWindow_property_may_only_be_assigned_once);
                }

                _currentWindow = value ?? throw new ArgumentNullException();
                _workspace.Window = value;

                Task.Run(() => _interactiveHost.SetOutputs(_currentWindow.OutputWriter, _currentWindow.ErrorOutputWriter));

                _currentWindow.SubmissionBufferAdded += SubmissionBufferAdded;
                _interactiveCommands = _commandsFactory.CreateInteractiveCommands(_currentWindow, CommandPrefix, _commands);
            }
        }

        protected abstract string LanguageName { get; }
        protected abstract CompilationOptions GetSubmissionCompilationOptions(string name, MetadataReferenceResolver metadataReferenceResolver, SourceReferenceResolver sourceReferenceResolver, ImmutableArray<string> imports);
        protected abstract ParseOptions ParseOptions { get; }
        protected abstract CommandLineParser CommandLineParser { get; }

        /// <summary>
        /// Invoked before the process is reset. The argument is the value of <see cref="InteractiveHostOptions.Platform"/>.
        /// </summary>
        public event Action<InteractiveHostPlatform> OnBeforeReset;

        #region Initialization

        private IInteractiveWindow GetCurrentWindowOrThrow()
        {
            var window = _currentWindow;
            if (window == null)
            {
                throw new InvalidOperationException(EditorFeaturesResources.Engine_must_be_attached_to_an_Interactive_Window);
            }

            return window;
        }

        public void Dispose()
        {
            _workspace.Dispose();
            _interactiveHost.Dispose();

            if (_currentWindow != null)
            {
                _currentWindow.SubmissionBufferAdded -= SubmissionBufferAdded;
            }
        }

        /// <summary>
        /// Invoked by <see cref="InteractiveHost"/> when a new process initialization completes.
        /// </summary>
        private void ProcessInitialized(InteractiveHostOptions options, RemoteExecutionResult result)
        {
            Contract.ThrowIfFalse(result.InitializationResult != null);

            if (!_threadingContext.JoinableTaskContext.IsOnMainThread)
            {
                _threadingContext.JoinableTaskFactory.RunAsync(async () =>
                {
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                    ProcessInitialized(options, result);
                });

                return;
            }

            var textView = GetCurrentWindowOrThrow().TextView;

            var lastSubmittedBuffer = _submittedBuffers.LastOrDefault();

            // Freeze all existing classifications and then clear the list of submission buffers we have.
            foreach (var textBuffer in _submittedBuffers)
            {
                InertClassifierProvider.CaptureExistingClassificationSpans(_classifierAggregator, textView, textBuffer);
            }

            _submittedBuffers.Clear();

            // clear workspace state:
            _workspace.ClearSolution();
            _currentSubmissionProjectId = null;
            _previousSubmissionProjectId = null;

            // update host state:
            _initializationResult = result.InitializationResult;
            ReferenceSearchPaths = result.ChangedReferencePaths.ToImmutableArrayOrEmpty();
            SourceSearchPaths = result.ChangedSourcePaths.ToImmutableArrayOrEmpty();
            WorkingDirectory = result.ChangedWorkingDirectory ?? _initialWorkingDirectory;

            var metadataService = _workspace.Services.GetRequiredService<IMetadataService>();
            _metadataReferenceResolver = CreateMetadataReferenceResolver(metadataService, ReferenceSearchPaths, WorkingDirectory);
            _sourceReferenceResolver = CreateSourceReferenceResolver(SourceSearchPaths, WorkingDirectory);

            // If the current buffer has not been submitted, create and add a submission project for it to the workspace.
            // It it has been submitted then a new buffer and its corresponding submission project will be added later.
            var currentSubmissionBuffer = _currentWindow.CurrentLanguageBuffer;
            if (currentSubmissionBuffer != lastSubmittedBuffer)
            {
                AddSubmissionProject(currentSubmissionBuffer, LanguageName);
            }
        }

        private static MetadataReferenceResolver CreateMetadataReferenceResolver(IMetadataService metadataService, ImmutableArray<string> searchPaths, string baseDirectory)
        {
            // TODO: To support CoreCLR we need to query the remote process for TPA list and pass it to the resolver.
            // https://github.com/dotnet/roslyn/issues/4788
            return new RuntimeMetadataReferenceResolver(
                new RelativePathResolver(searchPaths, baseDirectory),
                packageResolver: null,
                gacFileResolver: GacFileResolver.IsAvailable ? new GacFileResolver(preferredCulture: CultureInfo.CurrentCulture) : null,
                useCoreResolver: false,
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

            args.NewBuffer.ContentTypeChanged += _contentTypeChangedHandler;

            AddSubmissionProject(args.NewBuffer, LanguageName);
        }

        // The REPL window might change content type to host command content type (when a host command is typed at the beginning of the buffer).
        private void LanguageBufferContentTypeChanged(object sender, ContentTypeChangedEventArgs e)
        {
            // It's not clear whether this situation will ever happen, but just in case.
            if (e.BeforeContentType == e.AfterContentType)
            {
                return;
            }

            var buffer = e.Before.TextBuffer;
            var contentTypeName = this.ContentType.TypeName;

            var afterIsLanguage = e.AfterContentType.IsOfType(contentTypeName);
            var afterIsInteractiveCommand = e.AfterContentType.IsOfType(PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName);
            var beforeIsLanguage = e.BeforeContentType.IsOfType(contentTypeName);
            var beforeIsInteractiveCommand = e.BeforeContentType.IsOfType(PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName);

            // Workaround for https://github.com/dotnet/interactive-window/issues/156
            if ((beforeIsLanguage || beforeIsInteractiveCommand) && e.AfterContentType.TypeName == "inert" ||
                (afterIsLanguage || afterIsInteractiveCommand) && e.BeforeContentType.TypeName == "inert")
            {
                return;
            }

            Debug.Assert(afterIsLanguage && beforeIsInteractiveCommand
                      || beforeIsLanguage && afterIsInteractiveCommand);

            // We're switching between the target language and the Interactive Command "language".
            // First, remove the current submission from the solution.

            var documentId = _workspace.GetDocumentIdInCurrentContext(buffer.AsTextContainer());
            var oldSolution = _workspace.CurrentSolution;
            var relatedDocumentIds = oldSolution.GetRelatedDocumentIds(documentId);

            var newSolution = oldSolution;

            foreach (var relatedDocumentId in relatedDocumentIds)
            {
                Debug.Assert(relatedDocumentId != null);

                newSolution = newSolution.RemoveDocument(relatedDocumentId);

                // TODO (tomat): Is there a better way to remove mapping between buffer and document in REPL? 
                // Perhaps TrackingWorkspace should implement RemoveDocumentAsync?
                _workspace.ClearOpenDocument(relatedDocumentId);
            }

            // Next, remove the previous submission project and update the workspace.
            newSolution = newSolution.RemoveProject(_currentSubmissionProjectId);
            _workspace.SetCurrentSolution(newSolution);

            // Add a new submission with the correct language for the current buffer.
            var languageName = afterIsLanguage ? LanguageName : InteractiveLanguageNames.InteractiveCommand;

            AddSubmissionProject(buffer, languageName);
        }

        private void AddSubmissionProject(ITextBuffer submissionBuffer, string languageName)
        {
            var solution = _workspace.CurrentSolution;
            Project project;
            var imports = ImmutableArray<string>.Empty;
            var references = ImmutableArray<MetadataReference>.Empty;

            if (_previousSubmissionProjectId == null)
            {
                // The Interactive Window may have added the first language buffer before 
                // the host initialization has completed. Do not create a submission project 
                // for the buffer in such case. It will be created when the initialization completes.
                if (_initializationResult == null)
                {
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
                    project = CreateSubmissionProject(solution, languageName, imports, references);

                    var initDocumentId = DocumentId.CreateNewId(project.Id, debugName: scriptPath);
                    solution = project.Solution.AddDocument(initDocumentId, Path.GetFileName(scriptPath), new FileTextLoader(scriptPath, defaultEncoding: null));
                    _previousSubmissionProjectId = project.Id;

                    // imports and references will be inherited:
                    imports = ImmutableArray<string>.Empty;
                    references = ImmutableArray<MetadataReference>.Empty;
                }
            }

            // project for the new submission:
            project = CreateSubmissionProject(solution, languageName, imports, references);

            var documentId = DocumentId.CreateNewId(project.Id, debugName: project.Name);
            solution = project.Solution.AddDocument(documentId, project.Name, submissionBuffer.CurrentSnapshot.AsText());

            _workspace.SetCurrentSolution(solution);

            // opening document will start workspace listening to changes in this text container
            _workspace.OpenDocument(documentId, submissionBuffer.AsTextContainer());

            _currentSubmissionProjectId = project.Id;
        }

        private Project CreateSubmissionProject(Solution solution, string languageName, ImmutableArray<string> imports, ImmutableArray<MetadataReference> references)
        {
            var name = "Submission#" + _submissionCount++;

            // Grab a local copy so we aren't closing over the field that might change. The
            // collection itself is an immutable collection.
            var localCompilationOptions = GetSubmissionCompilationOptions(name, _metadataReferenceResolver, _sourceReferenceResolver, imports);

            var localParseOptions = ParseOptions;

            var projectId = ProjectId.CreateNewId(debugName: name);

            solution = solution.AddProject(
                ProjectInfo.Create(
                    projectId,
                    VersionStamp.Create(),
                    name: name,
                    assemblyName: name,
                    language: languageName,
                    compilationOptions: localCompilationOptions,
                    parseOptions: localParseOptions,
                    documents: null,
                    projectReferences: null,
                    metadataReferences: references,
                    hostObjectType: typeof(InteractiveScriptGlobals),
                    isSubmission: true));

            if (_previousSubmissionProjectId != null)
            {
                solution = solution.AddProjectReference(projectId, new ProjectReference(_previousSubmissionProjectId));
            }

            return solution.GetProject(projectId);
        }

        #endregion

        #region IInteractiveEngine

        public virtual bool CanExecuteCode(string text)
        {
            if (_interactiveCommands != null && _interactiveCommands.InCommand)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Invoked when the Interactive Window is created.
        /// </summary>
        Task<ExecutionResult> IInteractiveEvaluator.InitializeAsync()
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var window = GetCurrentWindowOrThrow();
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

            var window = GetCurrentWindowOrThrow();

            var resetOptions = ResetOptions;
            Debug.Assert(_interactiveCommands.CommandPrefix == CommandPrefix);
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

                OnBeforeReset(options.Platform);

                // Initiate reset and return to the UI thread to update state.
                var result = await _interactiveHost.ResetAsync(options).ConfigureAwait(true);
                if (result.Success)
                {
                    UpdateResolvers(result);
                }

                return new ExecutionResult(result.Success);
            }
            catch (Exception e) when (FatalError.Report(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public async Task<ExecutionResult> ExecuteCodeAsync(string text)
        {
            try
            {
                _threadingContext.ThrowIfNotOnUIThread();

                var currentSubmissionBuffer = _currentWindow.CurrentLanguageBuffer;
                Contract.ThrowIfNull(currentSubmissionBuffer);
                currentSubmissionBuffer.ContentTypeChanged -= _contentTypeChangedHandler;
                _submittedBuffers.Add(currentSubmissionBuffer);

                if (_interactiveCommands.InCommand)
                {
                    // Takes the content of the current language buffer, parses it as a command
                    // and returns a task that execute the command, or null if the text doesn't parse.
                    var commandTask = _interactiveCommands.TryExecuteCommand();
                    if (commandTask != null)
                    {
                        return await commandTask.ConfigureAwait(false);
                    }
                }

                // Execute code and return to the UI thread to update state.
                var result = await _interactiveHost.ExecuteAsync(text).ConfigureAwait(true);
                if (result.Success)
                {
                    // We are not executing a command (the current content type is not "Interactive Command"),
                    // so the source document should not have been removed.
                    Contract.ThrowIfFalse(_workspace.CurrentSolution.GetProject(_currentSubmissionProjectId).HasDocuments);

                    // only remember the submission if we compiled successfully, otherwise we
                    // ignore it's id so we don't reference it in the next submission.
                    _previousSubmissionProjectId = _currentSubmissionProjectId;

                    // update local search paths - remote paths has already been updated
                    UpdateResolvers(result);
                }

                return new ExecutionResult(result.Success);
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

        public string FormatClipboard()
        {
            // keep the clipboard content as is
            return null;
        }

        #endregion

        #region Paths, Resolvers

        private void UpdateResolvers(RemoteExecutionResult result)
            => UpdateResolvers(result.ChangedReferencePaths, result.ChangedSourcePaths, result.ChangedWorkingDirectory);

        private void UpdateResolvers(ImmutableArray<string> changedReferenceSearchPaths, ImmutableArray<string> changedSourceSearchPaths, string changedWorkingDirectory)
        {
            if (changedReferenceSearchPaths.IsDefault && changedSourceSearchPaths.IsDefault && changedWorkingDirectory == null)
            {
                return;
            }

            var solution = _workspace.CurrentSolution;

            // Maybe called after reset, when no submissions are available.
            var options = (_currentSubmissionProjectId != null) ? solution.GetProjectState(_currentSubmissionProjectId).CompilationOptions : null;

            if (changedWorkingDirectory != null)
            {
                WorkingDirectory = changedWorkingDirectory;
            }

            if (!changedReferenceSearchPaths.IsDefault)
            {
                ReferenceSearchPaths = changedReferenceSearchPaths;
            }

            if (!changedSourceSearchPaths.IsDefault)
            {
                SourceSearchPaths = changedSourceSearchPaths;
            }

            if (!changedReferenceSearchPaths.IsDefault || changedWorkingDirectory != null)
            {
                _metadataReferenceResolver = CreateMetadataReferenceResolver(_workspace.CurrentSolution.Services.MetadataService, ReferenceSearchPaths, WorkingDirectory);
                options = options?.WithMetadataReferenceResolver(_metadataReferenceResolver);
            }

            if (!changedSourceSearchPaths.IsDefault || changedWorkingDirectory != null)
            {
                _sourceReferenceResolver = CreateSourceReferenceResolver(SourceSearchPaths, WorkingDirectory);
                options = options?.WithSourceReferenceResolver(_sourceReferenceResolver);
            }

            if (options != null)
            {
                _workspace.SetCurrentSolution(solution.WithProjectCompilationOptions(_currentSubmissionProjectId, options));
            }
        }

        public async Task SetPathsAsync(ImmutableArray<string> referenceSearchPaths, ImmutableArray<string> sourceSearchPaths, string workingDirectory)
        {
            try
            {
                var result = await _interactiveHost.SetPathsAsync(referenceSearchPaths.ToArray(), sourceSearchPaths.ToArray(), workingDirectory).ConfigureAwait(false);
                UpdateResolvers(result);
            }
            catch (Exception e) when (FatalError.Report(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public string GetPrompt()
        {
            var buffer = GetCurrentWindowOrThrow().CurrentLanguageBuffer;
            return buffer != null && buffer.CurrentSnapshot.LineCount > 1
                ? ". "
                : "> ";
        }

        #endregion
    }
}
