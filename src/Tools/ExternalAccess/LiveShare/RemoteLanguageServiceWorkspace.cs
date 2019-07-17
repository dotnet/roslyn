// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Projects;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LiveShare;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare
{
    /// <summary>
    /// A Roslyn workspace that contains projects that exist on a remote machine.
    /// </summary>
    [Export(typeof(RemoteLanguageServiceWorkspace))]
    internal sealed class RemoteLanguageServiceWorkspace : Workspace, IDisposable, IRunningDocumentTableEventListener
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IThreadingContext _threadingContext;
        private readonly RunningDocumentTableEventTracker _runningDocumentTableEventTracker;

        private const string ExternalProjectName = "ExternalDocuments";

        // A collection of opened documents in RDT, indexed by the moniker of the document.
        private ImmutableDictionary<string, DocumentId> _openedDocs = ImmutableDictionary<string, DocumentId>.Empty;

        private CollaborationSession _session;
        private string _remoteRootPath;
        private string _externalPath;
        private RemoteDiagnosticListTable _remoteDiagnosticListTable;

        public bool IsRemoteSession { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteLanguageServiceWorkspace"/> class.
        /// </summary>
        [ImportingConstructor]
        public RemoteLanguageServiceWorkspace(ExportProvider exportProvider,
                                              IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
                                              SVsServiceProvider serviceProvider,
                                              IDiagnosticService diagnosticService,
                                              ITableManagerProvider tableManagerProvider,
                                              IThreadingContext threadingContext)
            : base(VisualStudioMefHostServices.Create(exportProvider), WorkspaceKind.AnyCodeRoslynWorkspace)

        {
            _serviceProvider = serviceProvider;

            _remoteDiagnosticListTable = new RemoteDiagnosticListTable(serviceProvider, this, diagnosticService, tableManagerProvider);

            var componentModel = _serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            Assumes.Present(componentModel);

            _editorAdaptersFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();

            var runningDocumentTable = (IVsRunningDocumentTable)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            _runningDocumentTableEventTracker = new RunningDocumentTableEventTracker(threadingContext, editorAdaptersFactoryService, runningDocumentTable, this);
            _threadingContext = threadingContext;
        }

        void IRunningDocumentTableEventListener.OnOpenDocument(string moniker, ITextBuffer textBuffer, IVsHierarchy hierarchy) => NotifyOnDocumentOpened(moniker, textBuffer, hierarchy);

        void IRunningDocumentTableEventListener.OnCloseDocument(string moniker) => NotifyOnDocumentClosing(moniker);

        void IRunningDocumentTableEventListener.OnRefreshDocumentContext(string moniker, IVsHierarchy hierarchy)
        {
            // Handled by Add/Remove
        }

        void IRunningDocumentTableEventListener.OnRenameDocument(string newMoniker, string oldMoniker, ITextBuffer textBuffer)
        {
            // Handled by Add/Remove.
        }

        public async Task SetSession(CollaborationSession session)
        {
            _session = session;
            var roots = await session.ListRootsAsync(CancellationToken.None).ConfigureAwait(false);
            _remoteRootPath = session.ConvertSharedUriToLocalPath(roots[0]);
            _remoteRootPath = _remoteRootPath.Substring(0, _remoteRootPath.Length - 1);
            var lastSlash = _remoteRootPath.LastIndexOf('\\');
            _externalPath = _remoteRootPath.Substring(0, lastSlash + 1);
            _externalPath += "~external";
            IsRemoteSession = true;
            session.RemoteServicesChanged += (object sender, RemoteServicesChangedEventArgs e) =>
            {
                _remoteDiagnosticListTable.UpdateWorkspaceDiagnosticsPresent(_session.RemoteServiceNames.Contains("workspaceDiagnostics"));
            };
        }

        public void EndSession()
        {
            IsRemoteSession = false;
        }

        public void Init()
        {
            StartSolutionCrawler();
        }

        /// <inheritdoc />
        public override bool CanOpenDocuments => true;

        /// <inheritdoc />
        public void OnManagedProjectAdded(ProjectInfo projectInfo)
        {
            //Notify AnyCode Roslyn workspace a project is being added.
            OnProjectAdded(projectInfo);
        }

        /// <inheritdoc />
        public void OnManagedProjectReloaded(ProjectInfo projectInfo)
        {
            //Notify AnyCode Roslyn workspace a project is being reloaded.
            OnProjectReloaded(projectInfo);
        }

        /// <inheritdoc />
        public void OnManagedProjectReferenceAdded(ProjectId projectId, ProjectReference reference)
        {
            //Notify AnyCode Roslyn workspace a project reference is being reloaded.
            OnProjectReferenceAdded(projectId, reference);
        }

        /// <inheritdoc />
        public void UpdateProjectReferences(ProjectId projectId, IEnumerable<ProjectReference> references)
        {
            var roslynProject = CurrentSolution.GetProject(projectId);
            roslynProject = roslynProject.WithProjectReferences(references);
        }

        /// <inheritdoc />
        public void OnFileReloaded(object docInfo)
        {
            OnDocumentReloaded(docInfo as DocumentInfo);
        }

        /// <inheritdoc />
        public void NotifyOnDocumentOpened(string moniker, ITextBuffer textBuffer, IVsHierarchy hierarchy)
        {
            if (_openedDocs.ContainsKey(moniker))
            {
                return;
            }

            Document document = GetOrAddDocument(moniker);

            if (document != null)
            {
                SourceTextContainer textContainer = textBuffer.AsTextContainer();
                OnDocumentOpened(document.Id, textContainer);
                _openedDocs = _openedDocs.SetItem(moniker, document.Id);
            }
        }

        private bool IsExternalLocalUri(string localPath)
        {
            return localPath.StartsWith(_externalPath) &&
                localPath.Length > (_externalPath.Length + 1);
        }

        public Document GetOrAddDocument(string filePath)
        {
            DocumentId docId = CurrentSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault();
            if (docId != null)
            {
                return CurrentSolution.GetDocument(docId);
            }

            if (!IsRemoteSession)
            {
                return null;
            }

            // If the document is within the joined folder or it's a registered external file,
            // add it to the workspace, otherwise bail out.
            if (!filePath.StartsWith(_remoteRootPath) &&
                !IsExternalLocalUri(filePath))
            {
                return null;
            }

            var language = GetLanguage(filePath);
            // Unsupported language.
            if (language == null)
            {
                return null;
            }

            var folderName = Path.GetFileNameWithoutExtension(_remoteRootPath);
            return AddDocumentToProject(filePath, language, folderName);
        }

        public Document GetOrAddExternalDocument(string filePath, string language)
        {
            DocumentId docId = CurrentSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault();
            if (docId != null)
            {
                return CurrentSolution.GetDocument(docId);
            }

            return AddDocumentToProject(filePath, language, ExternalProjectName);
        }

        public async Task<DocumentSpan?> GetDocumentSpanFromLocation(LSP.Location location, CancellationToken cancellationToken)
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
                OnManagedProjectAdded(projectInfo);
                project = CurrentSolution.GetProject(projectInfo.Id);
            }

            var docInfo = DocumentInfo.Create(DocumentId.CreateNewId(project.Id),
                                                  name: Path.GetFileName(filePath),
                                                  loader: new FileTextLoader(filePath, null),
                                                  filePath: filePath);
            OnDocumentAdded(docInfo);
            return CurrentSolution.GetDocument(docInfo.Id);
        }

        private string GetLanguage(string filePath)
        {
            var fileExtension = Path.GetExtension(filePath).ToLower();

            if (fileExtension == ".cs")
            {
                return StringConstants.CSharpLspLanguageName;
            }
            else if (fileExtension == ".ts" || fileExtension == ".js")
            {
                return StringConstants.TypeScriptLanguageName;
            }

            return null;
        }

        /// <inheritdoc />
        public void NotifyOnDocumentClosing(string moniker)
        {
            if (_openedDocs.TryGetValue(moniker, out DocumentId id))
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
            var doc = CurrentSolution.GetDocument(documentId);
            if (doc != null)
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

                Guid logicalView = Guid.Empty;
                if (ErrorHandler.Succeeded(svc.OpenDocumentViaProject(doc.FilePath,
                                                                      ref logicalView,
                                                                      out var sp,
                                                                      out IVsUIHierarchy hier,
                                                                      out uint itemid,
                                                                      out IVsWindowFrame frame))
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
                        var hierarchy = _runningDocumentTableEventTracker.GetDocumentHierarchy(doc.FilePath);
                        NotifyOnDocumentOpened(doc.FilePath, buffer, hierarchy);
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
        {
            StopSolutionCrawler();
            base.Dispose(disposing);
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

                    UpdateText(textBuffer, text);
                }
                else
                {
                    // The edits would get sent by the co-authoring service to the owner.
                    // The invisible editor saves the file on being disposed, which should get reflected  on the owner's side.
                    using (var invisibleEditor = new InvisibleEditor(_serviceProvider, document.FilePath, hierarchyOpt: null,
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
        {
            DiagnosticProvider.Enable(this, DiagnosticProvider.Options.Syntax);
        }

        private void StopSolutionCrawler()
        {
            DiagnosticProvider.Disable(this);
        }
    }
}
