// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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

        // full path or null
        private readonly string _responseFilePath;

        private readonly InteractiveHost _interactiveHost;

        private readonly string _initialWorkingDirectory;
        private string _initialScriptFileOpt;

        private readonly IThreadingContext _threadingContext;
        private readonly IContentType _contentType;
        private readonly InteractiveWorkspace _workspace;
        private IInteractiveWindow _currentWindow;
        private ImmutableArray<MetadataReference> _responseFileReferences;
        private ImmutableArray<string> _responseFileImports;
        private MetadataReferenceResolver _metadataReferenceResolver;
        private SourceReferenceResolver _sourceReferenceResolver;

        private ProjectId _previousSubmissionProjectId;
        private ProjectId _currentSubmissionProjectId;

        private readonly IViewClassifierAggregatorService _classifierAggregator;
        private readonly IInteractiveWindowCommandsFactory _commandsFactory;
        private readonly ImmutableArray<IInteractiveWindowCommand> _commands;
        private IInteractiveWindowCommands _interactiveCommands;
        private ITextBuffer _currentSubmissionBuffer;

        /// <remarks>
        /// This is a set because the same buffer might be re-added when the content type is changed.
        /// </remarks>
        private readonly HashSet<ITextBuffer> _submissionBuffers = new HashSet<ITextBuffer>();

        private int _submissionCount = 0;
        private readonly EventHandler<ContentTypeChangedEventArgs> _contentTypeChangedHandler;

        internal InteractiveEvaluatorResetOptions ResetOptions { get; set; }
            = new InteractiveEvaluatorResetOptions(is64Bit: true);

        public ImmutableArray<string> ReferenceSearchPaths { get; private set; }
        public ImmutableArray<string> SourceSearchPaths { get; private set; }
        public string WorkingDirectory { get; private set; }

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
            _responseFilePath = Path.Combine(GetDesktopHostDirectory(), responseFileName);
            _workspace = new InteractiveWorkspace(hostServices, this);
            _contentTypeChangedHandler = new EventHandler<ContentTypeChangedEventArgs>(LanguageBufferContentTypeChanged);
            _classifierAggregator = classifierAggregator;
            _initialWorkingDirectory = initialWorkingDirectory;
            _commandsFactory = commandsFactory;
            _commands = commands;

            // The following settings will apply when the REPL starts without .rsp file.
            // They are discarded once the REPL is reset.
            ReferenceSearchPaths = ImmutableArray<string>.Empty;
            SourceSearchPaths = ImmutableArray<string>.Empty;
            WorkingDirectory = initialWorkingDirectory;
            var metadataService = _workspace.CurrentSolution.Services.MetadataService;
            _metadataReferenceResolver = CreateMetadataReferenceResolver(metadataService, ReferenceSearchPaths, _initialWorkingDirectory);
            _sourceReferenceResolver = CreateSourceReferenceResolver(SourceSearchPaths, _initialWorkingDirectory);

            _interactiveHost = new InteractiveHost(replType, initialWorkingDirectory);
            _interactiveHost.ProcessStarting += ProcessStarting;
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
                if (value == null)
                {
                    throw new ArgumentNullException();
                }

                if (_currentWindow != null)
                {
                    throw new NotSupportedException(InteractiveEditorFeaturesResources.The_CurrentWindow_property_may_only_be_assigned_once);
                }

                _currentWindow = value;
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
        /// Invoked before the process is reset. The argument is the value of <see cref="InteractiveHostOptions.Is64Bit"/>.
        /// </summary>
        public event Action<bool> OnBeforeReset;

        #region Initialization

        public string GetConfiguration()
        {
            return null;
        }

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
        /// Invoked by <see cref="InteractiveHost"/> when a new process is being started.
        /// </summary>
        private void ProcessStarting(bool initialize)
        {
            var textView = GetCurrentWindowOrThrow().TextView;

            if (!_threadingContext.JoinableTaskContext.IsOnMainThread)
            {
                _threadingContext.JoinableTaskFactory.RunAsync(async () =>
                {
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                    ProcessStarting(initialize);
                });

                return;
            }

            // Freeze all existing classifications and then clear the list of submission buffers we have.
            _submissionBuffers.Remove(_currentSubmissionBuffer); // if present
            foreach (var textBuffer in _submissionBuffers)
            {
                InertClassifierProvider.CaptureExistingClassificationSpans(_classifierAggregator, textView, textBuffer);
            }
            _submissionBuffers.Clear();

            // We always start out empty
            _workspace.ClearSolution();
            _currentSubmissionProjectId = null;
            _previousSubmissionProjectId = null;

            var metadataService = _workspace.CurrentSolution.Services.MetadataService;
            var mscorlibRef = metadataService.GetReference(typeof(object).Assembly.Location, MetadataReferenceProperties.Assembly);
            var interactiveHostObjectRef = metadataService.GetReference(typeof(InteractiveScriptGlobals).Assembly.Location, Script.HostAssemblyReferenceProperties);

            _responseFileReferences = ImmutableArray.Create<MetadataReference>(mscorlibRef, interactiveHostObjectRef);
            _responseFileImports = ImmutableArray<string>.Empty;
            _initialScriptFileOpt = null;
            ReferenceSearchPaths = ImmutableArray<string>.Empty;
            SourceSearchPaths = ImmutableArray<string>.Empty;

            if (initialize && File.Exists(_responseFilePath))
            {
                // The base directory for relative paths is the directory that contains the .rsp file.
                // Note that .rsp files included by this .rsp file will share the base directory (Dev10 behavior of csc/vbc).
                var responseFileDirectory = Path.GetDirectoryName(_responseFilePath);
                var args = this.CommandLineParser.Parse(new[] { "@" + _responseFilePath }, responseFileDirectory, RuntimeEnvironment.GetRuntimeDirectory(), null);

                if (args.Errors.Length == 0)
                {
                    var metadataResolver = CreateMetadataReferenceResolver(metadataService, args.ReferencePaths, responseFileDirectory);
                    var sourceResolver = CreateSourceReferenceResolver(args.SourcePaths, responseFileDirectory);

                    // ignore unresolved references, they will be reported in the interactive window:
                    var responseFileReferences = args.ResolveMetadataReferences(metadataResolver).Where(r => !(r is UnresolvedMetadataReference));

                    _initialScriptFileOpt = args.SourceFiles.IsEmpty ? null : args.SourceFiles[0].Path;

                    ReferenceSearchPaths = args.ReferencePaths;
                    SourceSearchPaths = args.SourcePaths;

                    _responseFileReferences = _responseFileReferences.AddRange(responseFileReferences);
                    _responseFileImports = CommandLineHelpers.GetImports(args);
                }
            }

            _metadataReferenceResolver = CreateMetadataReferenceResolver(metadataService, ReferenceSearchPaths, _initialWorkingDirectory);
            _sourceReferenceResolver = CreateSourceReferenceResolver(SourceSearchPaths, _initialWorkingDirectory);

            // create the first submission project in the workspace after reset:
            if (_currentSubmissionBuffer != null)
            {
                AddSubmission(_currentSubmissionBuffer, this.LanguageName);
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
        {
            return new SourceFileResolver(searchPaths, baseDirectory);
        }

        #endregion

        #region Workspace

        private void SubmissionBufferAdded(object sender, SubmissionBufferAddedEventArgs args)
        {
            AddSubmission(args.NewBuffer, this.LanguageName);
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

            Debug.Assert((afterIsLanguage && beforeIsInteractiveCommand)
                      || (beforeIsLanguage && afterIsInteractiveCommand));

            // We're switching between the target language and the Interactive Command "language".
            // First, remove the current submission from the solution.

            var oldSolution = _workspace.CurrentSolution;
            var newSolution = oldSolution;

            foreach (var documentId in _workspace.GetRelatedDocumentIds(buffer.AsTextContainer()))
            {
                Debug.Assert(documentId != null);

                newSolution = newSolution.RemoveDocument(documentId);

                // TODO (tomat): Is there a better way to remove mapping between buffer and document in REPL? 
                // Perhaps TrackingWorkspace should implement RemoveDocumentAsync?
                _workspace.ClearOpenDocument(documentId);
            }

            // Next, remove the previous submission project and update the workspace.
            newSolution = newSolution.RemoveProject(_currentSubmissionProjectId);
            _workspace.SetCurrentSolution(newSolution);

            // Add a new submission with the correct language for the current buffer.
            var languageName = afterIsLanguage
                ? this.LanguageName
                : InteractiveLanguageNames.InteractiveCommand;

            AddSubmission(buffer, languageName);
        }

        private void AddSubmission(ITextBuffer subjectBuffer, string languageName)
        {
            var solution = _workspace.CurrentSolution;
            Project project;
            ImmutableArray<string> imports;
            ImmutableArray<MetadataReference> references;

            if (_previousSubmissionProjectId != null)
            {
                // only the first project needs imports and references
                imports = ImmutableArray<string>.Empty;
                references = ImmutableArray<MetadataReference>.Empty;
            }
            else if (_initialScriptFileOpt != null)
            {
                // insert a project for initialization script listed in .rsp:
                project = CreateSubmissionProject(solution, languageName, _responseFileImports, _responseFileReferences);
                var documentId = DocumentId.CreateNewId(project.Id, debugName: _initialScriptFileOpt);
                solution = project.Solution.AddDocument(documentId, Path.GetFileName(_initialScriptFileOpt), new FileTextLoader(_initialScriptFileOpt, defaultEncoding: null));
                _previousSubmissionProjectId = project.Id;

                imports = ImmutableArray<string>.Empty;
                references = ImmutableArray<MetadataReference>.Empty;
            }
            else
            {
                imports = _responseFileImports;
                references = _responseFileReferences;
            }

            // project for the new submission:
            project = CreateSubmissionProject(solution, languageName, imports, references);

            // Keep track of this buffer so we can freeze the classifications for it in the future.
            _submissionBuffers.Add(subjectBuffer);

            SetSubmissionDocument(subjectBuffer, project);

            _currentSubmissionProjectId = project.Id;

            if (_currentSubmissionBuffer != null)
            {
                _currentSubmissionBuffer.ContentTypeChanged -= _contentTypeChangedHandler;
            }

            subjectBuffer.ContentTypeChanged += _contentTypeChangedHandler;

            _currentSubmissionBuffer = subjectBuffer;
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

        private void SetSubmissionDocument(ITextBuffer buffer, Project project)
        {
            var documentId = DocumentId.CreateNewId(project.Id, debugName: project.Name);
            var solution = project.Solution
                .AddDocument(documentId, project.Name, buffer.CurrentSnapshot.AsText());

            _workspace.SetCurrentSolution(solution);

            // opening document will start workspace listening to changes in this text container
            _workspace.OpenDocument(documentId, buffer.AsTextContainer());
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

        Task<ExecutionResult> IInteractiveEvaluator.InitializeAsync()
        {
            var window = GetCurrentWindowOrThrow();
            var resetOptions = ResetOptions;

            _interactiveHost.SetOutputs(window.OutputWriter, window.ErrorOutputWriter);

            return ResetAsyncWorker(GetHostOptions(initialize: true, resetOptions.Is64Bit));
        }

        Task<ExecutionResult> IInteractiveEvaluator.ResetAsync(bool initialize)
        {
            var window = GetCurrentWindowOrThrow();

            var resetOptions = ResetOptions;
            Debug.Assert(_interactiveCommands.CommandPrefix == CommandPrefix);
            window.AddInput(CommandPrefix + ResetCommand.GetCommandLine(initialize, resetOptions.Is64Bit));
            window.WriteLine(InteractiveEditorFeaturesResources.Resetting_execution_engine);
            window.FlushOutput();

            return ResetAsyncWorker(GetHostOptions(initialize, resetOptions.Is64Bit));
        }

        private static string GetDesktopHostDirectory()
            => Path.Combine(Path.GetDirectoryName(typeof(InteractiveEvaluator).Assembly.Location), "DesktopHost");

        public InteractiveHostOptions GetHostOptions(bool initialize, bool? is64bit)
            => new InteractiveHostOptions(
                 hostDirectory: _interactiveHost.OptionsOpt?.HostDirectory ?? GetDesktopHostDirectory(),
                 initializationFile: initialize ? _responseFilePath : null,
                 culture: CultureInfo.CurrentUICulture,
                 is64Bit: is64bit ?? _interactiveHost.OptionsOpt?.Is64Bit ?? InteractiveHost.DefaultIs64Bit);

        private async Task<ExecutionResult> ResetAsyncWorker(InteractiveHostOptions options)
        {
            try
            {
                OnBeforeReset(options.Is64Bit);

                var result = await _interactiveHost.ResetAsync(options).ConfigureAwait(false);

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
                if (_interactiveCommands.InCommand)
                {
                    var cmdResult = _interactiveCommands.TryExecuteCommand();
                    if (cmdResult != null)
                    {
                        return await cmdResult.ConfigureAwait(false);
                    }
                }

                var result = await _interactiveHost.ExecuteAsync(text).ConfigureAwait(false);

                if (result.Success)
                {
                    // We are not executing a command (the current content type is not "Interactive Command"),
                    // so the source document should not have been removed.
                    Debug.Assert(_workspace.CurrentSolution.GetProject(_currentSubmissionProjectId).HasDocuments);

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
        {
            UpdateResolvers(result.ChangedReferencePaths.AsImmutableOrNull(), result.ChangedSourcePaths.AsImmutableOrNull(), result.ChangedWorkingDirectory);
        }

        private void UpdateResolvers(ImmutableArray<string> changedReferenceSearchPaths, ImmutableArray<string> changedSourceSearchPaths, string changedWorkingDirectory)
        {
            if (changedReferenceSearchPaths.IsDefault && changedSourceSearchPaths.IsDefault && changedWorkingDirectory == null)
            {
                return;
            }

            var solution = _workspace.CurrentSolution;

            // Maybe called after reset, when no submissions are available.
            var optionsOpt = (_currentSubmissionProjectId != null) ? solution.GetProjectState(_currentSubmissionProjectId).CompilationOptions : null;

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

                if (optionsOpt != null)
                {
                    optionsOpt = optionsOpt.WithMetadataReferenceResolver(_metadataReferenceResolver);
                }
            }

            if (!changedSourceSearchPaths.IsDefault || changedWorkingDirectory != null)
            {
                _sourceReferenceResolver = CreateSourceReferenceResolver(SourceSearchPaths, WorkingDirectory);

                if (optionsOpt != null)
                {
                    optionsOpt = optionsOpt.WithSourceReferenceResolver(_sourceReferenceResolver);
                }
            }

            if (optionsOpt != null)
            {
                _workspace.SetCurrentSolution(solution.WithProjectCompilationOptions(_currentSubmissionProjectId, optionsOpt));
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
