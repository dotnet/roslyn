﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Projects;
using Microsoft.VisualStudio.LiveShare;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Workspace.VSIntegration.Contracts;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    /// <summary>
    /// A Roslyn workspace that contains projects that exist on a remote machine.
    /// </summary>
    [Export(typeof(RemoteLanguageServiceWorkspace))]
    internal sealed class RemoteLanguageServiceWorkspace : CodeAnalysis.Workspace, IDisposable, IRunningDocumentTableEventListener
    {
        /// <summary>
        /// Gate to make sure we only update the paths and trigger RDT one at a time.
        /// Guards <see cref="_remoteWorkspaceRootPaths"/> and <see cref="_registeredExternalPaths"/>
        /// </summary>
        private static readonly SemaphoreSlim s_RemotePathsGate = new SemaphoreSlim(initialCount: 1);

        private readonly IServiceProvider _serviceProvider;
        private readonly IThreadingContext _threadingContext;
        private readonly RunningDocumentTableEventTracker _runningDocumentTableEventTracker;
        private readonly IVsFolderWorkspaceService _vsFolderWorkspaceService;

        private const string ExternalProjectName = "ExternalDocuments";

        // A collection of opened documents in RDT, indexed by the moniker of the document.
        private ImmutableDictionary<string, DocumentId> _openedDocs = ImmutableDictionary<string, DocumentId>.Empty;

        private CollaborationSession? _session;

        /// <summary>
        /// Stores the current base folder path(s) on the client that hold files retrieved from the host workspace(s).
        /// </summary>
        private ImmutableHashSet<string> _remoteWorkspaceRootPaths;

        /// <summary>
        /// Stores the current base folder path(s) on the client that holds registered external files.
        /// </summary>
        private ImmutableHashSet<string> _registeredExternalPaths;

        private readonly RemoteDiagnosticListTable _remoteDiagnosticListTable;

        public bool IsRemoteSession => _session != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteLanguageServiceWorkspace"/> class.
        /// </summary>
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteLanguageServiceWorkspace(ExportProvider exportProvider,
                                              IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
                                              IVsFolderWorkspaceService vsFolderWorkspaceService,
                                              SVsServiceProvider serviceProvider,
                                              IDiagnosticService diagnosticService,
                                              ITableManagerProvider tableManagerProvider,
                                              IThreadingContext threadingContext)
            : base(VisualStudioMefHostServices.Create(exportProvider), WorkspaceKind.CloudEnvironmentClientWorkspace)

        {
            _serviceProvider = serviceProvider;

            _remoteDiagnosticListTable = new RemoteDiagnosticListTable(serviceProvider, this, diagnosticService, tableManagerProvider);

            var runningDocumentTable = (IVsRunningDocumentTable)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            _runningDocumentTableEventTracker = new RunningDocumentTableEventTracker(threadingContext, editorAdaptersFactoryService, runningDocumentTable, this);
            _threadingContext = threadingContext;

            _vsFolderWorkspaceService = vsFolderWorkspaceService;

            _remoteWorkspaceRootPaths = ImmutableHashSet<string>.Empty;
            _registeredExternalPaths = ImmutableHashSet<string>.Empty;
        }

        void IRunningDocumentTableEventListener.OnOpenDocument(string moniker, ITextBuffer textBuffer, IVsHierarchy? hierarchy, IVsWindowFrame? windowFrame) => NotifyOnDocumentOpened(moniker, textBuffer);

        void IRunningDocumentTableEventListener.OnCloseDocument(string moniker) => NotifyOnDocumentClosing(moniker);

        void IRunningDocumentTableEventListener.OnRefreshDocumentContext(string moniker, IVsHierarchy hierarchy)
        {
            // Handled by Add/Remove
        }

        void IRunningDocumentTableEventListener.OnRenameDocument(string newMoniker, string oldMoniker, ITextBuffer textBuffer)
        {
            // Handled by Add/Remove.
        }

        public async Task SetSessionAsync(CollaborationSession session)
        {
            _session = session;

            StartSolutionCrawler();

            // Get the initial workspace roots and update any files that have been opened.
            await UpdatePathsToRemoteFilesAsync(session).ConfigureAwait(false);

            _vsFolderWorkspaceService.OnActiveWorkspaceChanged += OnActiveWorkspaceChangedAsync;
            session.RemoteServicesChanged += (object sender, RemoteServicesChangedEventArgs e) =>
            {
                _remoteDiagnosticListTable.UpdateWorkspaceDiagnosticsPresent(_session.RemoteServiceNames.Contains("workspaceDiagnostics"));
            };
        }

        public string? GetRemoteExternalRoot(string filePath)
            => _registeredExternalPaths.SingleOrDefault(externalPath => filePath.StartsWith(externalPath));

        public string? GetRemoteWorkspaceRoot(string filePath)
            => _remoteWorkspaceRootPaths.SingleOrDefault(remoteWorkspaceRoot => filePath.StartsWith(remoteWorkspaceRoot));

        /// <summary>
        /// Event that gets triggered whenever the active workspace changes.  If we're in a live share session
        /// this means that the remote workpace roots have also changed and need to be updated.
        /// This will not be called concurrently.
        /// </summary>
        private async Task OnActiveWorkspaceChangedAsync(object sender, EventArgs args)
        {
            if (IsRemoteSession)
            {
                await UpdatePathsToRemoteFilesAsync(_session!).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Retrieves the base folder paths for files on the client that have been retrieved from the remote host.
        /// Triggers a refresh of all open files so we make sure they are in the correct workspace.
        /// </summary>
        private async Task UpdatePathsToRemoteFilesAsync(CollaborationSession session)
        {
            var (remoteRootPaths, externalPaths) = await GetLocalPathsOfRemoteRootsAsync(session).ConfigureAwait(false);

            // Make sure we update our references to the remote roots and iterate RDT only one at a time.
            using (await s_RemotePathsGate.DisposableWaitAsync(CancellationToken.None).ConfigureAwait(false))
            {
                if (IsRemoteSession && (!_remoteWorkspaceRootPaths.Equals(remoteRootPaths) || !_registeredExternalPaths.Equals(externalPaths)))
                {
                    _remoteWorkspaceRootPaths = remoteRootPaths;
                    _registeredExternalPaths = externalPaths;
                    await RefreshAllFilesAsync().ConfigureAwait(false);
                }
            }
        }

        private static async Task<(ImmutableHashSet<string> remoteRootPaths, ImmutableHashSet<string> externalPaths)> GetLocalPathsOfRemoteRootsAsync(CollaborationSession session)
        {
            var roots = await session.ListRootsAsync(CancellationToken.None).ConfigureAwait(false);
            var localPathsOfRemoteRoots = roots.Select(root => session.ConvertSharedUriToLocalPath(root)).ToImmutableArray();

            var remoteRootPaths = ImmutableHashSet.CreateBuilder<string>();
            var externalPaths = ImmutableHashSet.CreateBuilder<string>();

            foreach (var localRoot in localPathsOfRemoteRoots)
            {
                // The local root is something like tmp\\xxx\\<workspace name>
                // The external root should be tmp\\xxx\\~external, so replace the workspace name with ~external.
                var splitRoot = localRoot.TrimEnd('\\').Split('\\');
                splitRoot[splitRoot.Length - 1] = "~external";
                var externalPath = string.Join("\\", splitRoot) + "\\";

                remoteRootPaths.Add(localRoot);
                externalPaths.Add(externalPath);
            }

            return (remoteRootPaths.ToImmutable(), externalPaths.ToImmutable());
        }

        public void EndSession()
        {
            _session = null;
            _vsFolderWorkspaceService.OnActiveWorkspaceChanged -= OnActiveWorkspaceChangedAsync;
            StopSolutionCrawler();

            // Clear the remote paths on end of session.  Live share handles closing all the files.
            using (s_RemotePathsGate.DisposableWait())
            {
                _remoteWorkspaceRootPaths = ImmutableHashSet<string>.Empty;
                _registeredExternalPaths = ImmutableHashSet<string>.Empty;
            }
        }

        /// <inheritdoc />
        public override bool CanOpenDocuments => true;

        /// <inheritdoc />
        public void NotifyOnDocumentOpened(string moniker, ITextBuffer textBuffer)
        {
            if (_openedDocs.ContainsKey(moniker))
            {
                return;
            }

            var document = GetOrAddDocument(moniker);

            if (document != null)
            {
                var textContainer = textBuffer.AsTextContainer();
                OnDocumentOpened(document.Id, textContainer);
                _openedDocs = _openedDocs.SetItem(moniker, document.Id);
            }
        }

        /// <summary>
        /// Iterates through the RDT and re-opens any files that are present.
        /// Used to update opened files after remote workspace roots change.
        /// </summary>
        public async Task RefreshAllFilesAsync()
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
            var documents = _runningDocumentTableEventTracker.EnumerateDocumentSet();
            foreach (var (moniker, textBuffer, _) in documents)
            {
                NotifyOnDocumentOpened(moniker, textBuffer);
            }
        }

        public Document? GetOrAddDocument(string filePath)
        {
            var docId = CurrentSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault();
            if (docId != null)
            {
                return CurrentSolution.GetDocument(docId);
            }

            if (!IsRemoteSession)
            {
                return null;
            }

            var language = GetLanguage(filePath);
            // Unsupported language.
            if (language == null)
            {
                return null;
            }

            // If the document is within the joined folder or it's a registered external file,
            // add it to the workspace, otherwise bail out.
            var remoteWorkspaceRoot = GetRemoteWorkspaceRoot(filePath);
            var remoteExternalRoot = GetRemoteExternalRoot(filePath);
            if (!string.IsNullOrEmpty(remoteWorkspaceRoot))
            {
                return AddDocumentToProject(filePath, language, Path.GetFileName(Path.GetDirectoryName(remoteWorkspaceRoot)));
            }
            else if (!string.IsNullOrEmpty(remoteExternalRoot))
            {
                return AddDocumentToProject(filePath, language, Path.GetFileName(Path.GetDirectoryName(remoteExternalRoot)));
            }
            else
            {
                return null;
            }
        }

        public Document? GetOrAddExternalDocument(string filePath, string language)
        {
            var docId = CurrentSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault();
            if (docId != null)
            {
                return CurrentSolution.GetDocument(docId);
            }

            return AddDocumentToProject(filePath, language, ExternalProjectName);
        }

        public async Task<DocumentSpan?> GetDocumentSpanFromLocationAsync(LSP.Location location, CancellationToken cancellationToken)
        {
            var document = GetOrAddDocument(location.Uri.LocalPath);
            if (document == null)
            {
                return null;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // The protocol converter would have synced the file to disk but we the document snapshot that was in the workspace before the sync would have empty text.
            // So we need to read from disk in order to map from line\column to a textspan.
            if (string.IsNullOrEmpty(text.ToString()))
            {
                text = SourceText.From(File.ReadAllText(document.FilePath));

                // Some features like the FindRefs window try to get the text at the span without opening the document (for eg to classify the span).
                // So fork the document to get one with the text. Note that this new document will not be in the CurrentSolution and we don't intend to
                // apply it back. By fetching the file, the workspace will get updated anyway. The assumption here is that this document that we are
                // handing out is only used for simple inspection and it's version is never compared with the Workspace.CurrentSolution.
                document = document.WithText(text);
            }

            var textSpan = ProtocolConversions.RangeToTextSpan(location.Range, text);
            return new DocumentSpan(document, textSpan);
        }

        private Document AddDocumentToProject(string filePath, string language, string projectName)
        {
            var project = CurrentSolution.Projects.FirstOrDefault(p => p.Name == projectName && p.Language == language);
            if (project == null)
            {
                var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), projectName, projectName, language);
                OnProjectAdded(projectInfo);
                project = CurrentSolution.GetRequiredProject(projectInfo.Id);
            }

            var docInfo = DocumentInfo.Create(DocumentId.CreateNewId(project.Id),
                                                  name: Path.GetFileName(filePath),
                                                  loader: new FileTextLoader(filePath, null),
                                                  filePath: filePath);
            OnDocumentAdded(docInfo);
            return CurrentSolution.GetDocument(docInfo.Id)!;
        }

        private string? GetLanguage(string filePath)
        {
            var fileExtension = Path.GetExtension(filePath).ToLower();

            if (fileExtension == ".cs")
            {
                return LanguageNames.CSharp;
            }
            else if (fileExtension == ".ts" || fileExtension == ".js")
            {
                return StringConstants.TypeScriptLanguageName;
            }
            else if (fileExtension == ".vb")
            {
                return LanguageNames.VisualBasic;
            }

            return null;
        }

        /// <inheritdoc />
        public void NotifyOnDocumentClosing(string moniker)
        {
            if (_openedDocs.TryGetValue(moniker, out var id))
            {
                // check if the doc is part of the current Roslyn workspace before notifying Roslyn.
                if (CurrentSolution.ContainsProject(id.ProjectId))
                {
                    OnDocumentClosed(id, new FileTextLoaderNoException(moniker, null));
                    _openedDocs = _openedDocs.Remove(moniker);
                }
            }
        }

        /// <inheritdoc />
        public override void OpenDocument(DocumentId documentId, bool activate = true)
        {
            if (_session == null)
            {
                return;
            }

            var doc = CurrentSolution.GetDocument(documentId);
            if (doc != null && doc.FilePath != null)
            {
                var svc = _serviceProvider.GetService(typeof(SVsUIShellOpenDocument)) as IVsUIShellOpenDocument;
                Report.IfNotPresent(svc);
                if (svc == null)
                {
                    return;
                }
                _threadingContext.JoinableTaskFactory.Run(async () =>
                {
                    await _session.DownloadFileAsync(_session.ConvertLocalPathToSharedUri(doc.FilePath), CancellationToken.None).ConfigureAwait(true);
                });

                var logicalView = Guid.Empty;
                if (ErrorHandler.Succeeded(svc.OpenDocumentViaProject(doc.FilePath,
                                                                      ref logicalView,
                                                                      out var sp,
                                                                      out var hier,
                                                                      out var itemid,
                                                                      out var frame))
                    && frame != null)
                {
                    if (activate)
                    {
                        frame.Show();
                    }
                    else
                    {
                        frame.ShowNoActivate();
                    }

                    if (_runningDocumentTableEventTracker.IsFileOpen(doc.FilePath) && _runningDocumentTableEventTracker.TryGetBufferFromMoniker(doc.FilePath, out var buffer))
                    {
                        NotifyOnDocumentOpened(doc.FilePath, buffer);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override bool CanApplyChange(ApplyChangesKind feature)
        {
            switch (feature)
            {
                case ApplyChangesKind.ChangeDocument:
                case ApplyChangesKind.AddDocument:
                case ApplyChangesKind.RemoveDocument:
                    return true;

                default:
                    return false;
            }
        }

        /// <inheritdoc />
        public void AddOpenedDocument(string filePath, DocumentId docId)
        {
            if (_runningDocumentTableEventTracker.IsFileOpen(filePath))
            {
                _openedDocs = _openedDocs.SetItem(filePath, docId);
            }
        }

        /// <inheritdoc />
        public void RemoveOpenedDocument(string filePath)
        {
            if (_runningDocumentTableEventTracker.IsFileOpen(filePath))
            {
                _openedDocs = _openedDocs.Remove(filePath);
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
            => base.Dispose(disposing);

        /// <summary>
        /// Marker class to easily group error reporting for missing live share text buffers.
        /// </summary>
        private class LiveShareTextBufferMissingException : Exception
        {
        }

        /// <inheritdoc />
        protected override void ApplyDocumentTextChanged(DocumentId documentId, SourceText text)
        {
            var document = CurrentSolution.GetDocument(documentId);
            if (document != null)
            {
                if (_openedDocs.Values.Contains(documentId) || IsDocumentOpen(documentId))
                {
                    var textBuffer = _threadingContext.JoinableTaskFactory.Run(async () =>
                    {
                        var sourceText = await document.GetTextAsync().ConfigureAwait(false);
                        var textContainer = sourceText.Container;
                        return textContainer.TryGetTextBuffer();
                    });

                    if (textBuffer == null)
                    {
                        // Text buffer is missing for opened Live Share document.
                        FatalError.ReportAndCatch(new LiveShareTextBufferMissingException());
                        return;
                    }

                    UpdateText(textBuffer, text);
                }
                else
                {
                    // The edits would get sent by the co-authoring service to the owner.
                    // The invisible editor saves the file on being disposed, which should get reflected  on the owner's side.
                    using (var invisibleEditor = new InvisibleEditor(_serviceProvider, document.FilePath!, hierarchy: null,
                                                 needsSave: true, needsUndoDisabled: false))
                    {
                        UpdateText(invisibleEditor.TextBuffer, text);
                    }
                }
            }
        }

        private static void UpdateText(ITextBuffer textBuffer, SourceText text)
        {
            using (var edit = textBuffer.CreateEdit(EditOptions.DefaultMinimalChange, reiteratedVersionNumber: null, editTag: null))
            {
                var oldSnapshot = textBuffer.CurrentSnapshot;
                var oldText = oldSnapshot.AsText();
                var changes = text.GetTextChanges(oldText);

                foreach (var change in changes)
                {
                    edit.Replace(change.Span.Start, change.Span.Length, change.NewText);
                }

                edit.Apply();
            }
        }

        private void StartSolutionCrawler()
            => DiagnosticProvider.Enable(this, DiagnosticProvider.Options.Syntax);

        private void StopSolutionCrawler()
            => DiagnosticProvider.Disable(this);
    }
}
