// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
extern alias Scripting;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.FileSystem;
using Microsoft.CodeAnalysis.Editor.Implementation.Interactive;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Editor.Interactive
{
    using RelativePathResolver = Scripting::Microsoft.CodeAnalysis.RelativePathResolver;

    internal abstract class InteractiveEvaluator : IInteractiveEvaluator, ICurrentWorkingDirectoryDiscoveryService
    {
        // full path or null
        private readonly string _responseFilePath;

        private readonly InteractiveHost _interactiveHost;

        private string _initialWorkingDirectory;
        private string _initialScriptFileOpt;

        private readonly IContentType _contentType;
        private readonly InteractiveWorkspace _workspace;
        private IInteractiveWindow _currentWindow;
        private ImmutableArray<MetadataReference> _rspReferences;
        private ImmutableArray<string> _rspImports;
        private MetadataReferenceResolver _metadataReferenceResolver;
        private SourceReferenceResolver _sourceReferenceResolver;

        private ProjectId _previousSubmissionProjectId;
        private ProjectId _currentSubmissionProjectId;

        private readonly IViewClassifierAggregatorService _classifierAggregator;
        private readonly IInteractiveWindowCommandsFactory _commandsFactory;
        private readonly ImmutableArray<IInteractiveWindowCommand> _commands;
        private IInteractiveWindowCommands _interactiveCommands;
        private ITextView _currentTextView;
        private ITextBuffer _currentSubmissionBuffer;

        private readonly ISet<ValueTuple<ITextView, ITextBuffer>> _submissionBuffers = new HashSet<ValueTuple<ITextView, ITextBuffer>>();

        private int _submissionCount = 0;
        private readonly EventHandler<ContentTypeChangedEventArgs> _contentTypeChangedHandler;

        public ImmutableArray<string> ReferenceSearchPaths { get; private set; }
        public ImmutableArray<string> SourceSearchPaths { get; private set; }
        public string WorkingDirectory { get; private set; }

        internal InteractiveEvaluator(
            IContentType contentType,
            HostServices hostServices,
            IViewClassifierAggregatorService classifierAggregator,
            IInteractiveWindowCommandsFactory commandsFactory,
            ImmutableArray<IInteractiveWindowCommand> commands,
            string responseFilePath,
            string initialWorkingDirectory,
            string interactiveHostPath,
            Type replType)
        {
            Debug.Assert(responseFilePath == null || PathUtilities.IsAbsolute(responseFilePath));

            _contentType = contentType;
            _responseFilePath = responseFilePath;
            _workspace = new InteractiveWorkspace(this, hostServices);
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

            _interactiveHost = new InteractiveHost(replType, interactiveHostPath, initialWorkingDirectory);
            _interactiveHost.ProcessStarting += ProcessStarting;
        }

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
                Debug.Assert(_currentWindow == null, "This property is intended to be write-once.");
                if (_currentWindow != value)
                {
                    _interactiveHost.Output = value.OutputWriter;
                    _interactiveHost.ErrorOutput = value.ErrorOutputWriter;
                    if (_currentWindow != null)
                    {
                        _currentWindow.SubmissionBufferAdded -= SubmissionBufferAdded;
                    }
                    _currentWindow = value;

                    _currentWindow.SubmissionBufferAdded += SubmissionBufferAdded;
                    _interactiveCommands = _commandsFactory.CreateInteractiveCommands(_currentWindow, "#", _commands);
                }
            }
        }

        protected IInteractiveWindowCommands InteractiveCommands
        {
            get
            {
                return _interactiveCommands;
            }
        }

        protected abstract string LanguageName { get; }
        protected abstract CompilationOptions GetSubmissionCompilationOptions(string name, MetadataReferenceResolver metadataReferenceResolver, SourceReferenceResolver sourceReferenceResolver, ImmutableArray<string> imports);
        protected abstract ParseOptions ParseOptions { get; }
        protected abstract CommandLineParser CommandLineParser { get; }

        #region Initialization

        public string GetConfiguration()
        {
            return null;
        }

        private IInteractiveWindow GetInteractiveWindow()
        {
            var window = CurrentWindow;
            if (window == null)
            {
                throw new InvalidOperationException(EditorFeaturesResources.EngineMustBeAttachedToAnInteractiveWindow);
            }

            return window;
        }

        public Task<ExecutionResult> InitializeAsync()
        {
            var window = GetInteractiveWindow();
            _interactiveHost.Output = window.OutputWriter;
            _interactiveHost.ErrorOutput = window.ErrorOutputWriter;
            return ResetAsyncWorker();
        }

        public void Dispose()
        {
            _workspace.Dispose();
            _interactiveHost.Dispose();
        }

        /// <summary>
        /// Invoked by <see cref="InteractiveHost"/> when a new process is being started.
        /// </summary>
        private void ProcessStarting(bool initialize)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => ProcessStarting(initialize)));
                return;
            }

            // Freeze all existing classifications and then clear the list of
            // submission buffers we have.
            FreezeClassifications();
            _submissionBuffers.Clear();

            // We always start out empty
            _workspace.ClearSolution();
            _currentSubmissionProjectId = null;
            _previousSubmissionProjectId = null;

            var metadataService = _workspace.CurrentSolution.Services.MetadataService;
            var mscorlibRef = metadataService.GetReference(typeof(object).Assembly.Location, MetadataReferenceProperties.Assembly);
            var interactiveHostObjectRef = metadataService.GetReference(typeof(InteractiveScriptGlobals).Assembly.Location, Script.HostAssemblyReferenceProperties);

            _rspReferences = ImmutableArray.Create<MetadataReference>(mscorlibRef, interactiveHostObjectRef);
            _rspImports = ImmutableArray<string>.Empty;
            _initialScriptFileOpt = null;
            ReferenceSearchPaths = ImmutableArray<string>.Empty;
            SourceSearchPaths = ImmutableArray<string>.Empty;

            if (initialize && File.Exists(_responseFilePath))
            {
                // The base directory for relative paths is the directory that contains the .rsp file.
                // Note that .rsp files included by this .rsp file will share the base directory (Dev10 behavior of csc/vbc).
                var rspDirectory = Path.GetDirectoryName(_responseFilePath);
                var args = this.CommandLineParser.Parse(new[] { "@" + _responseFilePath }, rspDirectory, RuntimeEnvironment.GetRuntimeDirectory(), null);

                if (args.Errors.Length == 0)
                {
                    var metadataResolver = CreateMetadataReferenceResolver(metadataService, args.ReferencePaths, rspDirectory);
                    var sourceResolver = CreateSourceReferenceResolver(args.SourcePaths, rspDirectory);

                    // ignore unresolved references, they will be reported in the interactive window:
                    var rspReferences = args.ResolveMetadataReferences(metadataResolver).Where(r => !(r is UnresolvedMetadataReference));

                    _initialScriptFileOpt = args.SourceFiles.IsEmpty ? null : args.SourceFiles[0].Path;

                    ReferenceSearchPaths = args.ReferencePaths;
                    SourceSearchPaths = args.SourcePaths;

                    _rspReferences = _rspReferences.AddRange(rspReferences);
                    _rspImports = CommandLineHelpers.GetImports(args);
                }
            }

            _metadataReferenceResolver = CreateMetadataReferenceResolver(metadataService, ReferenceSearchPaths, _initialWorkingDirectory);
            _sourceReferenceResolver = CreateSourceReferenceResolver(SourceSearchPaths, _initialWorkingDirectory);

            // create the first submission project in the workspace after reset:
            if (_currentSubmissionBuffer != null)
            {
                AddSubmission(_currentTextView, _currentSubmissionBuffer, this.LanguageName);
            }
        }

        private Dispatcher Dispatcher
        {
            get { return ((FrameworkElement)GetInteractiveWindow().TextView).Dispatcher; }
        }

        private static MetadataReferenceResolver CreateMetadataReferenceResolver(IMetadataService metadataService, ImmutableArray<string> searchPaths, string baseDirectory)
        {
            return new RuntimeMetadataReferenceResolver(
                new RelativePathResolver(searchPaths, baseDirectory),
                null,
                GacFileResolver.IsAvailable ? new GacFileResolver(preferredCulture: CultureInfo.CurrentCulture) : null,
                (path, properties) => metadataService.GetReference(path, properties));
        }

        private static SourceReferenceResolver CreateSourceReferenceResolver(ImmutableArray<string> searchPaths, string baseDirectory)
        {
            return new SourceFileResolver(searchPaths, baseDirectory);
        }

        #endregion

        #region Workspace

        private void SubmissionBufferAdded(object sender, SubmissionBufferAddedEventArgs args)
        {
            AddSubmission(_currentWindow.TextView, args.NewBuffer, this.LanguageName);
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

            AddSubmission(_currentTextView, buffer, languageName);
        }

        private void AddSubmission(ITextView textView, ITextBuffer subjectBuffer, string languageName)
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
                project = CreateSubmissionProject(solution, languageName, _rspImports, _rspReferences);
                var documentId = DocumentId.CreateNewId(project.Id, debugName: _initialScriptFileOpt);
                solution = project.Solution.AddDocument(documentId, Path.GetFileName(_initialScriptFileOpt), new FileTextLoader(_initialScriptFileOpt, defaultEncoding: null));
                _previousSubmissionProjectId = project.Id;

                imports = ImmutableArray<string>.Empty;
                references = ImmutableArray<MetadataReference>.Empty;
            }
            else
            {
                imports = _rspImports;
                references = _rspReferences;
            }

            // project for the new submission:
            project = CreateSubmissionProject(solution, languageName, imports, references);

            // Keep track of this buffer so we can freeze the classifications for it in the future.
            var viewAndBuffer = ValueTuple.Create(textView, subjectBuffer);
            if (!_submissionBuffers.Contains(viewAndBuffer))
            {
                _submissionBuffers.Add(ValueTuple.Create(textView, subjectBuffer));
            }

            SetSubmissionDocument(subjectBuffer, project);

            _currentSubmissionProjectId = project.Id;

            if (_currentSubmissionBuffer != null)
            {
                _currentSubmissionBuffer.ContentTypeChanged -= _contentTypeChangedHandler;
            }

            subjectBuffer.ContentTypeChanged += _contentTypeChangedHandler;
            subjectBuffer.Properties[typeof(ICurrentWorkingDirectoryDiscoveryService)] = this;

            _currentSubmissionBuffer = subjectBuffer;
            _currentTextView = textView;
        }

        private Project CreateSubmissionProject(Solution solution, string languageName, ImmutableArray<string> imports, ImmutableArray<MetadataReference> references)
        {
            var name = "Submission#" + (_submissionCount++);

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

        private void FreezeClassifications()
        {
            foreach (var textViewAndBuffer in _submissionBuffers)
            {
                var textView = textViewAndBuffer.Item1;
                var textBuffer = textViewAndBuffer.Item2;

                if (textBuffer != _currentSubmissionBuffer)
                {
                    InertClassifierProvider.CaptureExistingClassificationSpans(_classifierAggregator, textView, textBuffer);
                }
            }
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

        public Task<ExecutionResult> ResetAsync(bool initialize = true)
        {
            GetInteractiveWindow().AddInput(_interactiveCommands.CommandPrefix + "reset");
            GetInteractiveWindow().WriteLine(InteractiveEditorFeaturesResources.ResettingExecutionEngine);
            GetInteractiveWindow().FlushOutput();

            return ResetAsyncWorker(initialize);
        }

        private async Task<ExecutionResult> ResetAsyncWorker(bool initialize = true)
        {
            try
            {
                var options = new InteractiveHostOptions(
                    initializationFile: initialize ? _responseFilePath : null,
                    culture: CultureInfo.CurrentUICulture);

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
                if (InteractiveCommands.InCommand)
                {
                    var cmdResult = InteractiveCommands.TryExecuteCommand();
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
            // TODO: abort execution
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
            if (CurrentWindow.CurrentLanguageBuffer != null &&
                CurrentWindow.CurrentLanguageBuffer.CurrentSnapshot.LineCount > 1)
            {
                return ". ";
            }
            return "> ";
        }

        #endregion
    }
}
