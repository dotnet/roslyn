﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    /// <summary>
    /// Provides the support for opening files pointing to source generated documents, and keeping the content updated accordingly.
    /// </summary>
    [Export(typeof(SourceGeneratedFileManager))]
    internal sealed class SourceGeneratedFileManager : IRunningDocumentTableEventListener
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IThreadingContext _threadingContext;
        private readonly ForegroundThreadAffinitizedObject _foregroundThreadAffintizedObject;
        private readonly IAsynchronousOperationListener _listener;
        private readonly IVsRunningDocumentTable _runningDocumentTable;
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private readonly VisualStudioDocumentNavigationService _visualStudioDocumentNavigationService;

        private readonly RunningDocumentTableEventTracker _runningDocumentTableEventTracker;

        /// <summary>
        /// The temporary directory that we'll create file names under to act as a prefix we can later recognize and use.
        /// </summary>
        private readonly string _temporaryDirectory;

        /// <summary>
        /// Map of currently open generated files; the key is the generated full file path.
        /// </summary>
        private readonly Dictionary<string, OpenSourceGeneratedFile> _openFiles = new();
        private readonly VisualStudioWorkspace _visualStudioWorkspace;

        private readonly Dictionary<Guid, GeneratedFileDirectoryInfo> _directoryInfoOnDiskByContainingDirectoryId = new();

        /// <summary>
        /// When we have to put a placeholder file on disk, we put it in a directory named by the GUID portion of the DocumentId.
        /// We store the actual DocumentId (which includes the ProjectId) and some other textual information in
        /// <see cref="_directoryInfoOnDiskByContainingDirectoryId"/>, so that way we don't have to pack the information into the path itself.
        /// If we put the GUIDs and string names directly as components of the path, we quickly run into MAX_PATH. If we had a way to do virtual
        /// monikers that don't run into MAX_PATH issues then we absolutely would want to get rid of this.
        /// </summary>
        private class GeneratedFileDirectoryInfo
        {
            public GeneratedFileDirectoryInfo(DocumentId documentId, Type generatorType)
            {
                DocumentId = documentId;
                GeneratorType = generatorType;
            }

            public DocumentId DocumentId { get; }
            public Type GeneratorType { get; }
        }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SourceGeneratedFileManager(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IThreadingContext threadingContext,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            ITextDocumentFactoryService textDocumentFactoryService,
            VisualStudioWorkspace visualStudioWorkspace,
            VisualStudioDocumentNavigationService visualStudioDocumentNavigationService,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _serviceProvider = serviceProvider;
            _threadingContext = threadingContext;
            _foregroundThreadAffintizedObject = new ForegroundThreadAffinitizedObject(threadingContext, assertIsForeground: false);
            _textDocumentFactoryService = textDocumentFactoryService;
            _temporaryDirectory = Path.Combine(Path.GetTempPath(), "VSGeneratedDocuments");
            _visualStudioWorkspace = visualStudioWorkspace;
            _visualStudioDocumentNavigationService = visualStudioDocumentNavigationService;

            Directory.CreateDirectory(_temporaryDirectory);

            _listener = listenerProvider.GetListener(FeatureAttribute.SourceGenerators);

            // The IVsRunningDocumentTable is a free-threaded VS service that allows fetching of the service and advising events
            // to be done without implicitly marshalling to the UI thread.
            _runningDocumentTable = _serviceProvider.GetService<SVsRunningDocumentTable, IVsRunningDocumentTable>();
            _runningDocumentTableEventTracker = new RunningDocumentTableEventTracker(
                threadingContext,
                editorAdaptersFactoryService,
                _runningDocumentTable,
                this);
        }

        public void NavigateToSourceGeneratedFile(SourceGeneratedDocument document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            _foregroundThreadAffintizedObject.AssertIsForeground();

            // We will create an file name to represent this generated file; the Visual Studio shell APIs imply you can use a URI,
            // but most URIs are blocked other than file:// and http://; they also get extra handling to attempt to download the file so
            // those aren't really usable anyways.
            // The file name we create is <temp path>\<document id in GUID form>\<hint name>

            if (!_directoryInfoOnDiskByContainingDirectoryId.ContainsKey(document.Id.Id))
            {
                _directoryInfoOnDiskByContainingDirectoryId.Add(document.Id.Id,
                    new GeneratedFileDirectoryInfo(document.Id, document.SourceGenerator.GetType()));
            }

            // We must always ensure the file name portion of the path is just the hint name, which matches the compiler's choice so
            // debugging works properly.
            var temporaryFilePath = Path.Combine(
                _temporaryDirectory,
                document.Id.Id.ToString(),
                document.HintName);

            Directory.CreateDirectory(Path.GetDirectoryName(temporaryFilePath));

            // Don't write to the file if it's already there, as that potentially triggers a file reload
            if (!File.Exists(temporaryFilePath))
            {
                File.WriteAllText(temporaryFilePath, "");
            }

            var openDocumentService = _serviceProvider.GetService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>();
            var hr = openDocumentService.OpenDocumentViaProject(
                temporaryFilePath,
                VSConstants.LOGVIEWID.TextView_guid,
                out _,
                out _,
                out _,
                out var windowFrame);

            if (ErrorHandler.Succeeded(hr) && windowFrame != null)
            {
                windowFrame.Show();
            }

            // We should have the file now, so navigate to the right span
            if (_openFiles.TryGetValue(temporaryFilePath, out var openFile))
            {
                openFile.NavigateToSpan(sourceSpan, cancellationToken);
            }
        }

        public bool TryGetGeneratedFileInformation(
            string filePath,
            [NotNullWhen(true)] out DocumentId? documentId,
            [NotNullWhen(true)] out Type? generatorType,
            [NotNullWhen(true)] out string? generatedSourceHintName)
        {
            _foregroundThreadAffintizedObject.AssertIsForeground();

            documentId = null;
            generatorType = null;
            generatedSourceHintName = null;

            if (!filePath.StartsWith(_temporaryDirectory))
            {
                return false;
            }

            var fileInfo = new FileInfo(filePath);
            if (!Guid.TryParse(fileInfo.Directory.Name, out var guid) ||
                !_directoryInfoOnDiskByContainingDirectoryId.TryGetValue(guid, out var directoryInfo))
            {
                return false;
            }

            documentId = directoryInfo.DocumentId;
            generatorType = directoryInfo.GeneratorType;
            generatedSourceHintName = fileInfo.Name;

            return true;
        }

        void IRunningDocumentTableEventListener.OnOpenDocument(string moniker, ITextBuffer textBuffer, IVsHierarchy? hierarchy, IVsWindowFrame? windowFrame)
        {
            if (TryGetGeneratedFileInformation(moniker, out var documentId, out var generatorType, out var generatedSourceHintName))
            {
                // Attach to the text buffer if we haven't already
                if (!_openFiles.TryGetValue(moniker, out OpenSourceGeneratedFile openFile))
                {
                    openFile = new OpenSourceGeneratedFile(this, textBuffer, _visualStudioWorkspace, documentId, generatorType, _threadingContext);
                    _openFiles.Add(moniker, openFile);
                    _threadingContext.JoinableTaskFactory.Run(() => openFile.UpdateBufferContentsAsync(CancellationToken.None));

                    // Update the RDT flags to ensure the file can't be saved or appears in any MRUs as it's a temporary generated file name.
                    var cookie = ((IVsRunningDocumentTable4)_runningDocumentTable).GetDocumentCookie(moniker);
                    ErrorHandler.ThrowOnFailure(_runningDocumentTable.ModifyDocumentFlags(cookie, (uint)(_VSRDTFLAGS.RDT_CantSave | _VSRDTFLAGS.RDT_DontAddToMRU), fSet: 1));
                }

                if (windowFrame != null)
                {
                    openFile.SetWindowFrame(windowFrame, generatedSourceHintName);
                }
            }
        }

        void IRunningDocumentTableEventListener.OnCloseDocument(string moniker)
        {
            if (_openFiles.TryGetValue(moniker, out var openFile))
            {
                openFile.Dispose();
                _openFiles.Remove(moniker);
            }
        }

        void IRunningDocumentTableEventListener.OnRefreshDocumentContext(string moniker, IVsHierarchy hierarchy)
        {
        }

        void IRunningDocumentTableEventListener.OnRenameDocument(string newMoniker, string oldMoniker, ITextBuffer textBuffer)
        {
        }

        private class OpenSourceGeneratedFile : ForegroundThreadAffinitizedObject, IDisposable
        {
            private readonly SourceGeneratedFileManager _fileManager;
            private readonly ITextBuffer _textBuffer;
            private readonly Workspace _workspace;
            private readonly DocumentId _documentId;
            private readonly Type _generatorType;

            /// <summary>
            /// A read-only region that we create across the entire file to prevent edits unless we are the one making them.
            /// It's a dynamic read-only region that will allow edits if <see cref="_updatingBuffer"/> is set.
            /// </summary>
            private readonly IReadOnlyRegion _readOnlyRegion;
            private bool _updatingBuffer = false;

            /// <summary>
            /// A cancellation token used for any background updating of this file; this is cancelled on the UI thread
            /// when the file is closed.
            /// </summary>
            private readonly CancellationTokenSource _cancellationTokenSource = new();

            /// <summary>
            /// A queue used to batch updates to the file.
            /// </summary>
            private readonly AsyncBatchingDelay _batchingWorkQueue;

            /// <summary>
            /// The <see cref="IVsWindowFrame"/> of the active window. This may be null if we're in the middle of construction and
            /// we haven't been given it yet.
            /// </summary>
            private IVsWindowFrame? _windowFrame;

            private string? _windowFrameMessageToShow = null;
            private ImageMoniker _windowFrameImageMonikerToShow = default;
            private string? _currentWindowFrameMessage = null;
            private ImageMoniker _currentWindowFrameImageMoniker = default;
            private IVsInfoBarUIElement? _currentWindowFrameInfoBarElement = null;

            public OpenSourceGeneratedFile(SourceGeneratedFileManager fileManager, ITextBuffer textBuffer, Workspace workspace, DocumentId documentId, Type generatorType, IThreadingContext threadingContext)
                : base(threadingContext, assertIsForeground: true)
            {
                _fileManager = fileManager;
                _textBuffer = textBuffer;
                _workspace = workspace;
                _documentId = documentId;
                _generatorType = generatorType;

                // We'll create a read-only region for the file, but it'll be a dynamic region we can temporarily suspend
                // while we're doing edits.
                using (var readOnlyRegionEdit = _textBuffer.CreateReadOnlyRegionEdit())
                {
                    _readOnlyRegion = readOnlyRegionEdit.CreateDynamicReadOnlyRegion(
                        _textBuffer.CurrentSnapshot.GetFullSpan(),
                        SpanTrackingMode.EdgeInclusive,
                        EdgeInsertionMode.Deny,
                        callback: _ => !_updatingBuffer);

                    readOnlyRegionEdit.Apply();
                }

                _workspace.WorkspaceChanged += OnWorkspaceChanged;

                _batchingWorkQueue = new AsyncBatchingDelay(
                    TimeSpan.FromSeconds(1),
                    UpdateBufferContentsAsync,
                    asyncListener: _fileManager._listener,
                    _cancellationTokenSource.Token);
            }

            public void Dispose()
            {
                using (var readOnlyRegionEdit = _textBuffer.CreateReadOnlyRegionEdit())
                {
                    readOnlyRegionEdit.RemoveReadOnlyRegion(_readOnlyRegion);
                    readOnlyRegionEdit.Apply();
                }

                _workspace.WorkspaceChanged -= OnWorkspaceChanged;

                // Cancel any remaining asynchronous work we may have had to update this file
                _cancellationTokenSource.Cancel();
            }

            private string GeneratorDisplayName => _generatorType.FullName;

            public async Task UpdateBufferContentsAsync(CancellationToken cancellationToken)
            {
                SourceText? generatedSource = null;
                var project = _workspace.CurrentSolution.GetProject(_documentId.ProjectId);

                // Locals correspond to the equivalently-named fields; we'll assign these and then assign to the fields while on the
                // UI thread to avoid any potential race where we update the InfoBar while this is running.
                string? windowFrameMessageToShow;
                ImageMoniker windowFrameImageMonikerToShow;

                if (project == null)
                {
                    windowFrameMessageToShow = "The project no longer exists.";
                    windowFrameImageMonikerToShow = KnownMonikers.StatusError;
                }
                else
                {
                    var generatedDocument = await project.GetSourceGeneratedDocumentAsync(_documentId, cancellationToken).ConfigureAwait(false);

                    if (generatedDocument != null)
                    {
                        windowFrameMessageToShow = string.Format(ServicesVSResources.This_file_is_autogenerated_by_0_and_cannot_be_edited, GeneratorDisplayName);
                        windowFrameImageMonikerToShow = default;
                        generatedSource = await generatedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // The file isn't there anymore; do we still have the generator at all?
                        if (project.AnalyzerReferences.Any(a => a.GetGenerators(project.Language).Any(g => g.GetType().Assembly.Equals(_generatorType.Assembly))))
                        {
                            windowFrameMessageToShow = string.Format(ServicesVSResources.The_generator_0_that_generated_this_file_has_stopped_generating_this_file, GeneratorDisplayName);
                            windowFrameImageMonikerToShow = KnownMonikers.StatusError;
                        }
                        else
                        {
                            windowFrameMessageToShow = string.Format(ServicesVSResources.The_generator_0_that_generated_this_file_has_been_removed_from_the_project, GeneratorDisplayName);
                            windowFrameImageMonikerToShow = KnownMonikers.StatusError;
                        }
                    }
                }

                await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                _windowFrameMessageToShow = windowFrameMessageToShow;
                _windowFrameImageMonikerToShow = windowFrameImageMonikerToShow;

                // Update the text if we have new text
                if (generatedSource != null)
                {
                    try
                    {
                        // Allow us to do our own edits
                        _updatingBuffer = true;

                        // Ensure the encoding matches; this is necessary for debugger checksums to match what is in the PDB.
                        if (_fileManager._textDocumentFactoryService.TryGetTextDocument(_textBuffer, out var textDocument))
                        {
                            textDocument.Encoding = generatedSource.Encoding;
                        }

                        // HACK: if we do an edit here, that'll change the dirty state of the document, which
                        // will cause us to think a provisional tab is being edited. If we pass in the textDocument
                        // as an edit tag, the code in Microsoft.VisualStudio.Text.Implementation.TextDocument.TextBufferChangedHandler
                        // will think this is an edit coming from itself, and will skip the dirty update.

                        // We'll ask the editor to do the diffing for us so updates don't refresh the entire buffer
                        using (var edit = _textBuffer.CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber: null, editTag: textDocument))
                        {
                            // TODO: make the edit in some nicer way than creating a massive string
                            edit.Replace(startPosition: 0, _textBuffer.CurrentSnapshot.Length, generatedSource.ToString());
                            edit.Apply();
                        }
                    }
                    finally
                    {
                        _updatingBuffer = false;
                    }
                }

                // Update the InfoBar either way
                EnsureWindowFrameInfoBarUpdated();
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
            {
                var oldProject = e.OldSolution.GetProject(_documentId.ProjectId);
                var newProject = e.NewSolution.GetProject(_documentId.ProjectId);

                if (oldProject != null && newProject != null)
                {
                    // We'll start this work asynchronously to figure out if we need to change; if the file is closed the cancellationToken
                    // is triggered and this will no-op.
                    var asyncToken = _fileManager._listener.BeginAsyncOperation(nameof(OpenSourceGeneratedFile) + "." + nameof(OnWorkspaceChanged));

                    Task.Run(async () =>
                    {
                        if (await oldProject.GetDependentVersionAsync(_cancellationTokenSource.Token).ConfigureAwait(false) !=
                            await newProject.GetDependentVersionAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
                        {
                            _batchingWorkQueue.RequeueWork();
                        }
                    }, _cancellationTokenSource.Token).CompletesAsyncOperation(asyncToken);
                }
            }

            internal void SetWindowFrame(IVsWindowFrame windowFrame, string generatedSourceHintName)
            {
                AssertIsForeground();

                if (_windowFrame != null)
                {
                    // We already have a window frame, and we don't expect to get a second one
                    return;
                }

                _windowFrame = windowFrame;

                // We'll override the window frame and never show it as dirty, even if there's an underlying edit
                windowFrame.SetProperty((int)__VSFPROPID2.VSFPROPID_OverrideDirtyState, false);
                windowFrame.SetProperty((int)__VSFPROPID5.VSFPROPID_OverrideCaption, generatedSourceHintName + " " + ServicesVSResources.generated_suffix);
                windowFrame.SetProperty((int)__VSFPROPID5.VSFPROPID_OverrideToolTip, generatedSourceHintName + " " + string.Format(ServicesVSResources.generated_by_0_suffix, GeneratorDisplayName));

                EnsureWindowFrameInfoBarUpdated();
            }

            private void EnsureWindowFrameInfoBarUpdated()
            {
                AssertIsForeground();

                if (_windowFrameMessageToShow == null ||
                    _windowFrame == null ||
                    _currentWindowFrameMessage == _windowFrameMessageToShow &&
                    !_currentWindowFrameImageMoniker.Equals(_windowFrameImageMonikerToShow))
                {
                    // We don't have anything to do, or anything to do yet.
                    return;
                }

                var infoBarFactory = (IVsInfoBarUIFactory)_fileManager._serviceProvider.GetService(typeof(SVsInfoBarUIFactory));
                Assumes.Present(infoBarFactory);

                if (ErrorHandler.Failed(_windowFrame.GetProperty((int)__VSFPROPID7.VSFPROPID_InfoBarHost, out var infoBarHostObject)) ||
                    infoBarHostObject is not IVsInfoBarHost infoBarHost)
                {
                    return;
                }

                // Remove the existing bar
                if (_currentWindowFrameInfoBarElement != null)
                {
                    infoBarHost.RemoveInfoBar(_currentWindowFrameInfoBarElement);
                }

                var infoBar = new InfoBarModel(_windowFrameMessageToShow, _windowFrameImageMonikerToShow, isCloseButtonVisible: false);
                var infoBarUI = infoBarFactory.CreateInfoBar(infoBar);

                infoBarHost.AddInfoBar(infoBarUI);

                _currentWindowFrameMessage = _windowFrameMessageToShow;
                _currentWindowFrameImageMoniker = _windowFrameImageMonikerToShow;
                _currentWindowFrameInfoBarElement = infoBarUI;
            }

            public void NavigateToSpan(TextSpan sourceSpan, CancellationToken cancellationToken)
            {
                var sourceText = _textBuffer.CurrentSnapshot.AsText();
                _fileManager._visualStudioDocumentNavigationService.NavigateTo(_textBuffer, sourceText.GetVsTextSpanForSpan(sourceSpan), cancellationToken);
            }
        }
    }
}
