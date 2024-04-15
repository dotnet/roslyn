// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
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

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

using InfoBarInfo = (ImageMoniker imageMoniker, string message);

/// <summary>
/// Provides the support for opening files pointing to source generated documents, and keeping the content updated accordingly.
/// </summary>
[Export(typeof(SourceGeneratedFileManager))]
internal sealed class SourceGeneratedFileManager : IOpenTextBufferEventListener
{
    private readonly SVsServiceProvider _serviceProvider;
    private readonly IVsService<SVsInfoBarUIFactory, IVsInfoBarUIFactory> _vsInfoBarUIFactory;
    private readonly IVsService<SVsShell, IVsShell> _vsShell;
    private readonly IThreadingContext _threadingContext;
    private readonly ITextDocumentFactoryService _textDocumentFactoryService;
    private readonly VisualStudioDocumentNavigationService _visualStudioDocumentNavigationService;

    private readonly IAsynchronousOperationListener _listener;
    private readonly IAsynchronousOperationListenerProvider _listenerProvider;

    /// <summary>
    /// The temporary directory that we'll create file names under to act as a prefix we can later recognize and use.
    /// </summary>
    private readonly string _temporaryDirectory;

    /// <summary>
    /// Map of currently open generated files; the key is the generated full file path.
    /// </summary>
    private readonly Dictionary<string, OpenSourceGeneratedFile> _openFiles = [];
    private readonly VisualStudioWorkspace _visualStudioWorkspace;

    /// <summary>
    /// When we have to put a placeholder file on disk, we put it in a directory named by the GUID portion of the DocumentId.
    /// We store the actual DocumentId (which includes the ProjectId) and some other textual information in
    /// <see cref="_directoryInfoOnDiskByContainingDirectoryId"/>, so that way we don't have to pack the information into the path itself.
    /// If we put the GUIDs and string names directly as components of the path, we quickly run into MAX_PATH. If we had a way to do virtual
    /// monikers that don't run into MAX_PATH issues then we absolutely would want to get rid of this.
    /// </summary>
    /// <remarks>All accesses should be on the UI thread.</remarks>
    private readonly Dictionary<Guid, SourceGeneratedDocumentIdentity> _directoryInfoOnDiskByContainingDirectoryId = [];

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SourceGeneratedFileManager(
        SVsServiceProvider serviceProvider,
        IVsService<SVsInfoBarUIFactory, IVsInfoBarUIFactory> vsInfoBarUIFactory,
        IVsService<SVsShell, IVsShell> vsShell,
        IThreadingContext threadingContext,
        OpenTextBufferProvider openTextBufferProvider,
        IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
        ITextDocumentFactoryService textDocumentFactoryService,
        VisualStudioWorkspace visualStudioWorkspace,
        VisualStudioDocumentNavigationService visualStudioDocumentNavigationService,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _serviceProvider = serviceProvider;
        _vsInfoBarUIFactory = vsInfoBarUIFactory;
        _vsShell = vsShell;
        _threadingContext = threadingContext;
        _textDocumentFactoryService = textDocumentFactoryService;
        _temporaryDirectory = PathUtilities.EnsureTrailingSeparator(Path.Combine(Path.GetTempPath(), "VSGeneratedDocuments"));
        _visualStudioWorkspace = visualStudioWorkspace;
        _visualStudioDocumentNavigationService = visualStudioDocumentNavigationService;

        _listenerProvider = listenerProvider;
        _listener = listenerProvider.GetListener(FeatureAttribute.SourceGenerators);

        Directory.CreateDirectory(_temporaryDirectory);

        openTextBufferProvider.AddListener(this);
    }

    public Func<CancellationToken, Task<bool>> GetNavigationCallback(SourceGeneratedDocument document, TextSpan sourceSpan)
    {
        // We will create an file name to represent this generated file; the Visual Studio shell APIs imply you can use a URI,
        // but most URIs are blocked other than file:// and http://; they also get extra handling to attempt to download the file so
        // those aren't really usable anyways.
        // The file name we create is <temp path>\<document id in GUID form>\<hint name>

        if (!_directoryInfoOnDiskByContainingDirectoryId.ContainsKey(document.Id.Id))
        {
            _directoryInfoOnDiskByContainingDirectoryId.Add(document.Id.Id, document.Identity);
        }

        // We must always ensure the file name portion of the path is just the hint name, which matches the compiler's choice so
        // debugging works properly.
        var temporaryFilePath = Path.Combine(
            _temporaryDirectory,
            document.Id.Id.ToString(),
            // Normalize hint name (it always contains forward slashes).
            document.HintName.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(Path.GetDirectoryName(temporaryFilePath));

        // Don't write to the file if it's already there, as that potentially triggers a file reload
        if (!File.Exists(temporaryFilePath))
        {
            File.WriteAllText(temporaryFilePath, "");
        }

        return async cancellationToken =>
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var openDocumentService = _serviceProvider.GetService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>(_threadingContext.JoinableTaskFactory);
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
            return _openFiles.TryGetValue(temporaryFilePath, out var openFile) &&
                await openFile.NavigateToSpanAsync(sourceSpan, cancellationToken).ConfigureAwait(false);
        };
    }

    public bool TryGetGeneratedFileInformation(
        string filePath,
        out SourceGeneratedDocumentIdentity identity)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        identity = default;

        // Find GUID in "temporary directory/GUID/path/to/file.cs".
        if (!filePath.StartsWith(_temporaryDirectory))
        {
            return false;
        }

        // Remove temporary directory prefix (note that it always has trailing separator).
        var slice = filePath.AsSpan()[_temporaryDirectory.Length..];
        if (slice.IsEmpty)
        {
            return false;
        }

        var separatorIndex = slice.IndexOf(Path.DirectorySeparatorChar);
        if (separatorIndex < 0)
        {
            return false;
        }

        var guidDirName = slice[..separatorIndex];
        return Guid.TryParse(guidDirName.ToString(), out var guid) &&
            _directoryInfoOnDiskByContainingDirectoryId.TryGetValue(guid, out identity);
    }

    void IOpenTextBufferEventListener.OnOpenDocument(string moniker, ITextBuffer textBuffer, IVsHierarchy? hierarchy)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        if (TryGetGeneratedFileInformation(moniker, out var documentIdentity))
        {
            // Attach to the text buffer if we haven't already
            if (!_openFiles.TryGetValue(moniker, out var openFile))
            {
                openFile = new OpenSourceGeneratedFile(this, textBuffer, documentIdentity);
                _openFiles.Add(moniker, openFile);

                _threadingContext.JoinableTaskFactory.Run(() => openFile.RefreshFileAsync(CancellationToken.None).AsTask());

                // Update the RDT flags to ensure the file can't be saved or appears in any MRUs as it's a temporary generated file name.
                var runningDocumentTable = _serviceProvider.GetService<SVsRunningDocumentTable, IVsRunningDocumentTable4>(_threadingContext.JoinableTaskFactory);
                var cookie = runningDocumentTable.GetDocumentCookie(moniker);
                ErrorHandler.ThrowOnFailure(((IVsRunningDocumentTable)runningDocumentTable).ModifyDocumentFlags(cookie, (uint)(_VSRDTFLAGS.RDT_CantSave | _VSRDTFLAGS.RDT_DontAddToMRU), fSet: 1));
            }
        }
    }

    void IOpenTextBufferEventListener.OnDocumentOpenedIntoWindowFrame(string moniker, IVsWindowFrame windowFrame)
    {
        if (_openFiles.TryGetValue(moniker, out var openFile))
            _threadingContext.JoinableTaskFactory.Run(() => openFile.SetWindowFrameAsync(windowFrame));
    }

    void IOpenTextBufferEventListener.OnCloseDocument(string moniker)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        if (_openFiles.TryGetValue(moniker, out var openFile))
        {
            openFile.Dispose();
            _openFiles.Remove(moniker);
        }
    }

    void IOpenTextBufferEventListener.OnRefreshDocumentContext(string moniker, IVsHierarchy hierarchy)
    {
    }

    void IOpenTextBufferEventListener.OnRenameDocument(string newMoniker, string oldMoniker, ITextBuffer textBuffer)
    {
    }

    private sealed class OpenSourceGeneratedFile : IDisposable
    {
        private readonly SourceGeneratedFileManager _fileManager;
        private readonly ITextBuffer _textBuffer;
        private readonly SourceGeneratedDocumentIdentity _documentIdentity;
        private readonly IWorkspaceConfigurationService? _workspaceConfigurationService;

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
        private readonly AsyncBatchingWorkQueue _batchingWorkQueue;

        /// <summary>
        /// The info bar of the active window. This may be null if we're in the middle of construction and we haven't
        /// created it yet
        /// </summary>
        private VisualStudioInfoBar? _infoBar;
        private VisualStudioInfoBar.InfoBarMessage? _currentInfoBarMessage;

        private InfoBarInfo? _infoToShow = null;

        public OpenSourceGeneratedFile(SourceGeneratedFileManager fileManager, ITextBuffer textBuffer, SourceGeneratedDocumentIdentity documentIdentity)
        {
            fileManager._threadingContext.ThrowIfNotOnUIThread();
            _fileManager = fileManager;
            _textBuffer = textBuffer;
            _documentIdentity = documentIdentity;
            _workspaceConfigurationService = this.Workspace.Services.GetService<IWorkspaceConfigurationService>();

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

            this.Workspace.WorkspaceChanged += OnWorkspaceChanged;

            _batchingWorkQueue = new AsyncBatchingWorkQueue(
                TimeSpan.FromSeconds(1),
                RefreshFileAsync,
                asyncListener: _fileManager._listener,
                _cancellationTokenSource.Token);
        }

        private Workspace Workspace => _fileManager._visualStudioWorkspace;

        private void DisconnectFromWorkspaceIfOpen()
        {
            _fileManager._threadingContext.ThrowIfNotOnUIThread();

            if (this.Workspace.IsDocumentOpen(_documentIdentity.DocumentId))
            {
                var sourceGeneratedDocument = (SourceGeneratedDocument?)_textBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                Contract.ThrowIfNull(sourceGeneratedDocument);
                this.Workspace.OnSourceGeneratedDocumentClosed(sourceGeneratedDocument);
            }
        }

        public void Dispose()
        {
            _fileManager._threadingContext.ThrowIfNotOnUIThread();

            this.Workspace.WorkspaceChanged -= OnWorkspaceChanged;

            // Disconnect the buffer from the workspace before making it eligible for edits
            DisconnectFromWorkspaceIfOpen();

            using (var readOnlyRegionEdit = _textBuffer.CreateReadOnlyRegionEdit())
            {
                readOnlyRegionEdit.RemoveReadOnlyRegion(_readOnlyRegion);
                readOnlyRegionEdit.Apply();
            }

            // Cancel any remaining asynchronous work we may have had to update this file
            _cancellationTokenSource.Cancel();
        }

        private string GeneratorDisplayName => _documentIdentity.Generator.TypeName;

        public async ValueTask RefreshFileAsync(CancellationToken cancellationToken)
        {
            SourceGeneratedDocument? generatedDocument = null;
            SourceText? generatedSource = null;
            var project = this.Workspace.CurrentSolution.GetProject(_documentIdentity.DocumentId.ProjectId);

            // Locals correspond to the equivalently-named fields; we'll assign these and then assign to the fields while on the
            // UI thread to avoid any potential race where we update the InfoBar while this is running.
            InfoBarInfo infoToShow;

            if (project == null)
            {
                infoToShow = (KnownMonikers.StatusError, ServicesVSResources.The_project_no_longer_exists);
            }
            else
            {
                generatedDocument = await project.GetSourceGeneratedDocumentAsync(_documentIdentity.DocumentId, cancellationToken).ConfigureAwait(false);
                if (generatedDocument != null)
                {
                    infoToShow = (imageMoniker: default, string.Format(
                        ServicesVSResources.This_file_was_generated_by_0_at_1_and_cannot_be_edited,
                        GeneratorDisplayName,
                        generatedDocument.GenerationDateTime.ToLocalTime()));
                    generatedSource = await generatedDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // The file isn't there anymore; do we still have the generator at all?
                    if (project.AnalyzerReferences.Any(a => a.FullPath == _documentIdentity.Generator.AssemblyPath))
                    {
                        infoToShow = (KnownMonikers.StatusError, string.Format(ServicesVSResources.The_generator_0_that_generated_this_file_has_stopped_generating_this_file, GeneratorDisplayName));
                    }
                    else
                    {
                        infoToShow = (KnownMonikers.StatusError, string.Format(ServicesVSResources.The_generator_0_that_generated_this_file_has_been_removed_from_the_project, GeneratorDisplayName));
                    }
                }
            }

            await _fileManager._threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _infoToShow = infoToShow;

            // Update the text if we have new text
            if (generatedSource != null)
            {
                RoslynDebug.AssertNotNull(generatedDocument);

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

                    // If the file isn't already open, open it now. We may transition between opening and closing
                    // if the file is repeatedly appearing and disappearing.
                    var connectToWorkspace = _workspaceConfigurationService?.Options.EnableOpeningSourceGeneratedFiles != false;

                    if (connectToWorkspace && !this.Workspace.IsDocumentOpen(_documentIdentity.DocumentId))
                    {
                        this.Workspace.OnSourceGeneratedDocumentOpened(_textBuffer.AsTextContainer(), generatedDocument);
                    }
                }
                finally
                {
                    _updatingBuffer = false;
                }
            }
            else
            {
                // The user made an edit that meant the source generator that generated this file is no longer generating this file.
                // We can't update buffer contents anymore. We'll remove the connection between this buffer and the workspace,
                // so this file now appears in Miscellaneous Files.
                DisconnectFromWorkspaceIfOpen();
            }

            // Update the InfoBar either way
            await EnsureWindowFrameInfoBarUpdatedAsync(cancellationToken).ConfigureAwait(true);
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            var projectId = _documentIdentity.DocumentId.ProjectId;

            // Trivial check.  see if the SG version of these projects changed.  If so, we definitely want to update
            // this generated file.
            if (e.OldSolution.GetSourceGeneratorExecutionVersion(projectId) !=
                e.NewSolution.GetSourceGeneratorExecutionVersion(projectId))
            {
                _batchingWorkQueue.AddWork();
                return;
            }

            var oldProject = e.OldSolution.GetProject(projectId);
            var newProject = e.NewSolution.GetProject(projectId);

            if (oldProject != null && newProject != null)
            {
                // We'll start this work asynchronously to figure out if we need to change; if the file is closed the cancellationToken
                // is triggered and this will no-op.
                var asyncToken = _fileManager._listener.BeginAsyncOperation($"{nameof(OpenSourceGeneratedFile)}.{nameof(OnWorkspaceChanged)}");
                CheckDependentVersionsAsync().CompletesAsyncOperation(asyncToken);
            }

            async Task CheckDependentVersionsAsync()
            {
                // Ensure we do this off the thread that is telling us about workspace changes.
                await Task.Yield();

                if (await oldProject.GetDependentVersionAsync(_cancellationTokenSource.Token).ConfigureAwait(false) !=
                    await newProject.GetDependentVersionAsync(_cancellationTokenSource.Token).ConfigureAwait(false))
                {
                    _batchingWorkQueue.AddWork();
                }
            }
        }

        internal async Task SetWindowFrameAsync(IVsWindowFrame windowFrame)
        {
            var cancellationToken = _cancellationTokenSource.Token;
            await _fileManager._threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Only need to do this once.  Can use the presence of the info bar to check this.
            if (_infoBar != null)
                return;

            _infoBar = new VisualStudioInfoBar(
                _fileManager._threadingContext, _fileManager._vsInfoBarUIFactory, _fileManager._vsShell, _fileManager._listenerProvider, windowFrame);

            // We'll override the window frame and never show it as dirty, even if there's an underlying edit
            windowFrame.SetProperty((int)__VSFPROPID2.VSFPROPID_OverrideDirtyState, false);
            windowFrame.SetProperty((int)__VSFPROPID5.VSFPROPID_OverrideCaption, _documentIdentity.HintName + " " + ServicesVSResources.generated_suffix);
            windowFrame.SetProperty((int)__VSFPROPID5.VSFPROPID_OverrideToolTip, _documentIdentity.HintName + " " + string.Format(ServicesVSResources.generated_by_0_suffix, GeneratorDisplayName));

            await EnsureWindowFrameInfoBarUpdatedAsync(cancellationToken).ConfigureAwait(true);
        }

        private async Task EnsureWindowFrameInfoBarUpdatedAsync(CancellationToken cancellationToken)
        {
            await _fileManager._threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // If we don't have a frame, or even a message, we can't do anything yet.
            if (_infoToShow is null || _infoBar is null)
                return;

            // bail out if no change is needed
            var (imageMoniker, message) = _infoToShow.Value;
            if (_currentInfoBarMessage != null &&
                _currentInfoBarMessage.Message == message &&
                _currentInfoBarMessage.ImageMoniker.Equals(imageMoniker))
            {
                return;
            }

            // Remove the current message so it can be replaced with the new one.
            _currentInfoBarMessage?.Remove();

            // Capture the newly created message into a local.  That way the "rerun generator" button callback can
            // reference *exactly this* instance in order to remove it when it is clicked.
            VisualStudioInfoBar.InfoBarMessage? infoBarMessage = null;
            InfoBarUI[] infoBarItems = [new InfoBarUI(ServicesVSResources.Rerun_generator, InfoBarUI.UIKind.Button, () =>
            {
                _fileManager._threadingContext.ThrowIfNotOnUIThread();
                Contract.ThrowIfNull(infoBarMessage);
                infoBarMessage.Remove();

                _currentInfoBarMessage = _fileManager._threadingContext.JoinableTaskFactory.Run(() =>
                    _infoBar.ShowInfoBarMessageAsync(
                        ServicesVSResources.Generator_running, isCloseButtonVisible: false, KnownMonikers.StatusInformation));

                // Force regeneration here.  Nothing has actually changed, so the incremental generator architecture
                // would normally just return the same values all over again.  By forcing things, we drop the
                // generator driver, which will force new files to actually be created.
                this.Workspace.EnqueueUpdateSourceGeneratorVersion(
                    this._documentIdentity.DocumentId.ProjectId,
                    forceRegeneration: true);
            })];

            infoBarMessage = await _infoBar.ShowInfoBarMessageAsync(
                message, isCloseButtonVisible: false, imageMoniker, infoBarItems).ConfigureAwait(true);
            _currentInfoBarMessage = infoBarMessage;
        }

        public Task<bool> NavigateToSpanAsync(TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var sourceText = _textBuffer.CurrentSnapshot.AsText();
            return _fileManager._visualStudioDocumentNavigationService.NavigateToTextBufferAsync(
                _textBuffer, sourceText.GetVsTextSpanForSpan(sourceSpan), cancellationToken);
        }
    }
}
