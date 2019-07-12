// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Export(typeof(MiscellaneousFilesWorkspace))]
    internal sealed partial class MiscellaneousFilesWorkspace : Workspace, IRunningDocumentTableEventListener
    {
        private readonly IMetadataAsSourceFileService _fileTrackingMetadataAsSourceService;
        private readonly IVsTextManager _textManager;

        private readonly RunningDocumentTableEventTracker _runningDocumentTableEventTracker;

        private readonly Dictionary<Guid, LanguageInformation> _languageInformationByLanguageGuid = new Dictionary<Guid, LanguageInformation>();

        /// <summary>
        /// <see cref="WorkspaceRegistration"/> instances for all open buffers being tracked by by this object
        /// for possible inclusion into this workspace.
        /// </summary>
        private IBidirectionalMap<string, WorkspaceRegistration> _monikerToWorkspaceRegistration = BidirectionalMap<string, WorkspaceRegistration>.Empty;

        /// <summary>
        /// The mapping of all monikers in the RDT and the <see cref="ProjectId"/> of the project and <see cref="SourceTextContainer"/> of the open
        /// file we have created for that open buffer. An entry should only be in here if it's also already in <see cref="_monikerToWorkspaceRegistration"/>.
        /// </summary>
        private readonly Dictionary<string, (ProjectId projectId, SourceTextContainer textContainer)> _monikersToProjectIdAndContainer = new Dictionary<string, (ProjectId, SourceTextContainer)>();

        private readonly ImmutableArray<MetadataReference> _metadataReferences;

        private readonly ForegroundThreadAffinitizedObject _foregroundThreadAffinitization;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MiscellaneousFilesWorkspace(
            IThreadingContext threadingContext,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IMetadataAsSourceFileService fileTrackingMetadataAsSourceService,
            SaveEventsService saveEventsService,
            VisualStudioWorkspace visualStudioWorkspace,
            SVsServiceProvider serviceProvider)
            : base(visualStudioWorkspace.Services.HostServices, WorkspaceKind.MiscellaneousFiles)
        {
            _foregroundThreadAffinitization = new ForegroundThreadAffinitizedObject(threadingContext, assertIsForeground: true);

            _fileTrackingMetadataAsSourceService = fileTrackingMetadataAsSourceService;
            _textManager = (IVsTextManager)serviceProvider.GetService(typeof(SVsTextManager));

            var runningDocumentTable = (IVsRunningDocumentTable)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            _runningDocumentTableEventTracker = new RunningDocumentTableEventTracker(threadingContext, editorAdaptersFactoryService, runningDocumentTable, this);

            _metadataReferences = ImmutableArray.CreateRange(CreateMetadataReferences());
            saveEventsService.StartSendingSaveEvents();
        }

        void IRunningDocumentTableEventListener.OnOpenDocument(string moniker, ITextBuffer textBuffer, IVsHierarchy _) => TrackOpenedDocument(moniker, textBuffer);

        void IRunningDocumentTableEventListener.OnCloseDocument(string moniker) => TryUntrackClosingDocument(moniker);

        /// <summary>
        /// File hierarchy events are not relevant to the misc workspace.
        /// </summary>
        void IRunningDocumentTableEventListener.OnRefreshDocumentContext(string moniker, IVsHierarchy hierarchy)
        {
        }

        void IRunningDocumentTableEventListener.OnRenameDocument(string newMoniker, string oldMoniker, ITextBuffer buffer)
        {
            // We want to consider this file to be added in one of two situations:
            //
            // 1) the old file already was a misc file, at which point we might just be doing a rename from
            //    one name to another with the same extension
            // 2) the old file was a different extension that we weren't tracking, which may have now changed
            if (TryUntrackClosingDocument(oldMoniker) || TryGetLanguageInformation(oldMoniker) == null)
            {
                // Add the new one, if appropriate.
                TrackOpenedDocument(newMoniker, buffer);
            }
        }

        public void RegisterLanguage(Guid languageGuid, string languageName, string scriptExtension)
        {
            _languageInformationByLanguageGuid.Add(languageGuid, new LanguageInformation(languageName, scriptExtension));
        }

        private LanguageInformation TryGetLanguageInformation(string filename)
        {
            LanguageInformation languageInformation = null;

            if (ErrorHandler.Succeeded(_textManager.MapFilenameToLanguageSID(filename, out var fileLanguageGuid)))
            {
                _languageInformationByLanguageGuid.TryGetValue(fileLanguageGuid, out languageInformation);
            }

            return languageInformation;
        }

        private IEnumerable<MetadataReference> CreateMetadataReferences()
        {
            var manager = this.Services.GetService<VisualStudioMetadataReferenceManager>();
            var searchPaths = VisualStudioMetadataReferenceManager.GetReferencePaths();

            return from fileName in new[] { "mscorlib.dll", "System.dll", "System.Core.dll" }
                   let fullPath = FileUtilities.ResolveRelativePath(fileName, basePath: null, baseDirectory: null, searchPaths: searchPaths, fileExists: File.Exists)
                   where fullPath != null
                   select manager.CreateMetadataReferenceSnapshot(fullPath, MetadataReferenceProperties.Assembly);
        }

        private void TrackOpenedDocument(string moniker, ITextBuffer textBuffer)
        {
            _foregroundThreadAffinitization.AssertIsForeground();

            var languageInformation = TryGetLanguageInformation(moniker);
            if (languageInformation == null)
            {
                // We can never put this document in a workspace, so just bail
                return;
            }

            // We don't want to realize the document here unless it's already initialized. Document initialization is watched in
            // OnAfterAttributeChangeEx and will retrigger this if it wasn't already done.
            if (!_monikerToWorkspaceRegistration.ContainsKey(moniker))
            {
                var registration = Workspace.GetWorkspaceRegistration(textBuffer.AsTextContainer());

                registration.WorkspaceChanged += Registration_WorkspaceChanged;
                _monikerToWorkspaceRegistration = _monikerToWorkspaceRegistration.Add(moniker, registration);

                if (!IsClaimedByAnotherWorkspace(registration))
                {
                    AttachToDocument(moniker, textBuffer);
                }
            }
        }

        private void Registration_WorkspaceChanged(object sender, EventArgs e)
        {
            // We may or may not be getting this notification from the foreground thread if another workspace
            // is raising events on a background. Let's send it back to the UI thread since we can't talk
            // to the RDT in the background thread. Since this is all asynchronous a bit more asynchrony is fine.
            if (!_foregroundThreadAffinitization.IsForeground())
            {
                ScheduleTask(() => Registration_WorkspaceChanged(sender, e));
                return;
            }

            _foregroundThreadAffinitization.AssertIsForeground();

            var workspaceRegistration = (WorkspaceRegistration)sender;

            // Since WorkspaceChanged notifications may be asynchronous and happened on a different thread,
            // we might have already unsubscribed for this synchronously from the RDT while we were in the process of sending this
            // request back to the UI thread.
            if (!_monikerToWorkspaceRegistration.TryGetKey(workspaceRegistration, out var moniker))
            {
                return;
            }

            // It's also theoretically possible that we are getting notified about a workspace change to a document that has
            // been simultaneously removed from the RDT but we haven't gotten the notification. In that case, also bail.
            if (!_runningDocumentTableEventTracker.IsFileOpen(moniker))
            {
                return;
            }

            if (workspaceRegistration.Workspace == null)
            {
                if (_monikersToProjectIdAndContainer.TryGetValue(moniker, out var projectIdAndSourceTextContainer))
                {
                    // The workspace was taken from us and released and we have only asynchronously found out now.
                    // We already have the file open in our workspace, but the global mapping of source text container
                    // to the workspace that owns it needs to be updated once more.
                    RegisterText(projectIdAndSourceTextContainer.textContainer);
                }
                else
                {
                    // We should now try to claim this. The moniker we have here is the moniker after the rename if we're currently processing
                    // a rename. It's possible in that case that this is being closed by the other workspace due to that rename. If the rename
                    // is changing or removing the file extension, we wouldn't want to try attaching, which is why we have to re-check
                    // the moniker. Once we observe the rename later in OnAfterAttributeChangeEx we'll completely disconnect.
                    if (TryGetLanguageInformation(moniker) != null)
                    {
                        if (_runningDocumentTableEventTracker.TryGetBufferFromMoniker(moniker, out var buffer))
                        {
                            AttachToDocument(moniker, buffer);
                        }
                    }
                }
            }
            else if (IsClaimedByAnotherWorkspace(workspaceRegistration))
            {
                // It's now claimed by another workspace, so we should unclaim it
                if (_monikersToProjectIdAndContainer.ContainsKey(moniker))
                {
                    DetachFromDocument(moniker);
                }
            }
        }

        /// <summary>
        /// Stops tracking a document in the RDT for whether we should attach to it.
        /// </summary>
        /// <returns>true if we were previously tracking it.</returns>
        private bool TryUntrackClosingDocument(string moniker)
        {
            _foregroundThreadAffinitization.AssertIsForeground();

            var unregisteredRegistration = false;

            // Remove our registration changing handler before we call DetachFromDocument. Otherwise, calling DetachFromDocument
            // causes us to set the workspace to null, which we then respond to as an indication that we should
            // attach again.
            if (_monikerToWorkspaceRegistration.TryGetValue(moniker, out var registration))
            {
                registration.WorkspaceChanged -= Registration_WorkspaceChanged;
                _monikerToWorkspaceRegistration = _monikerToWorkspaceRegistration.RemoveKey(moniker);
                unregisteredRegistration = true;
            }

            DetachFromDocument(moniker);

            return unregisteredRegistration;
        }

        private bool IsClaimedByAnotherWorkspace(WorkspaceRegistration registration)
        {
            // Currently, we are also responsible for pushing documents to the metadata as source workspace,
            // so we count that here as well
            return registration.Workspace != null && registration.Workspace.Kind != WorkspaceKind.MetadataAsSource && registration.Workspace.Kind != WorkspaceKind.MiscellaneousFiles;
        }

        private void AttachToDocument(string moniker, ITextBuffer textBuffer)
        {
            _foregroundThreadAffinitization.AssertIsForeground();

            if (_fileTrackingMetadataAsSourceService.TryAddDocumentToWorkspace(moniker, textBuffer))
            {
                // We already added it, so we will keep it excluded from the misc files workspace
                return;
            }

            var projectInfo = CreateProjectInfoForDocument(moniker);

            OnProjectAdded(projectInfo);

            var sourceTextContainer = textBuffer.AsTextContainer();
            OnDocumentOpened(projectInfo.Documents.Single().Id, sourceTextContainer);

            _monikersToProjectIdAndContainer.Add(moniker, (projectInfo.Id, sourceTextContainer));
        }

        /// <summary>
        /// Creates the <see cref="ProjectInfo"/> that can be added to the workspace for a newly opened document.
        /// </summary>
        private ProjectInfo CreateProjectInfoForDocument(string filePath)
        {
            // This should always succeed since we only got here if we already confirmed the moniker is acceptable
            var languageInformation = TryGetLanguageInformation(filePath);
            Contract.ThrowIfNull(languageInformation);

            var fileExtension = PathUtilities.GetExtension(filePath);

            var languageServices = Services.GetLanguageServices(languageInformation.LanguageName);
            var compilationOptionsOpt = languageServices.GetService<ICompilationFactoryService>()?.GetDefaultCompilationOptions();

            // Use latest language version which is more permissive, as we cannot find out language version of the project which the file belongs to
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/575761
            var parseOptionsOpt = languageServices.GetService<ISyntaxTreeFactoryService>()?.GetDefaultParseOptionsWithLatestLanguageVersion();

            if (parseOptionsOpt != null &&
                compilationOptionsOpt != null &&
                fileExtension == languageInformation.ScriptExtension)
            {
                parseOptionsOpt = parseOptionsOpt.WithKind(SourceCodeKind.Script);

                var metadataService = Services.GetService<IMetadataService>();
                var scriptEnvironmentService = Services.GetService<IScriptEnvironmentService>();

                // Misc files workspace always provides the service:
                Contract.ThrowIfNull(scriptEnvironmentService);

                var baseDirectory = PathUtilities.GetDirectoryName(filePath);

                // TODO (https://github.com/dotnet/roslyn/issues/5325, https://github.com/dotnet/roslyn/issues/13886):
                // - Need to have a way to specify these somewhere in VS options.
                // - Use RuntimeMetadataReferenceResolver like in InteractiveEvaluator.CreateMetadataReferenceResolver
                // - Add default namespace imports, default metadata references to match csi.rsp
                // - Add default script globals available in 'csi goo.csx' environment: CommandLineScriptGlobals

                var referenceResolver = new WorkspaceMetadataFileReferenceResolver(
                    metadataService,
                    new RelativePathResolver(scriptEnvironmentService.MetadataReferenceSearchPaths, baseDirectory));

                compilationOptionsOpt = compilationOptionsOpt.
                    WithMetadataReferenceResolver(referenceResolver).
                    WithSourceReferenceResolver(new SourceFileResolver(scriptEnvironmentService.SourceReferenceSearchPaths, baseDirectory));
            }

            var projectId = ProjectId.CreateNewId(debugName: "Miscellaneous Files Project for " + filePath);
            var documentId = DocumentId.CreateNewId(projectId, debugName: filePath);

            var sourceCodeKind = GetSourceCodeKind(parseOptionsOpt, fileExtension, languageInformation);
            var documentInfo = DocumentInfo.Create(
                documentId,
                filePath,
                sourceCodeKind: sourceCodeKind,
                loader: new FileTextLoader(filePath, defaultEncoding: null),
                filePath: filePath);

            // The assembly name must be unique for each collection of loose files. Since the name doesn't matter
            // a random GUID can be used.
            var assemblyName = Guid.NewGuid().ToString("N");

            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                name: ServicesVSResources.Miscellaneous_Files,
                assemblyName,
                languageInformation.LanguageName,
                compilationOptions: compilationOptionsOpt,
                parseOptions: parseOptionsOpt,
                documents: SpecializedCollections.SingletonEnumerable(documentInfo),
                metadataReferences: _metadataReferences);

            // Miscellaneous files projects are never fully loaded since, by definition, it won't know
            // what the full set of information is except when the file is script code.
            return projectInfo.WithHasAllInformation(hasAllInformation: sourceCodeKind == SourceCodeKind.Script);
        }

        private SourceCodeKind GetSourceCodeKind(
            ParseOptions parseOptionsOpt,
            string fileExtension,
            LanguageInformation languageInformation)
        {
            if (parseOptionsOpt != null)
            {
                return parseOptionsOpt.Kind;
            }

            return string.Equals(fileExtension, languageInformation.ScriptExtension, StringComparison.OrdinalIgnoreCase) ?
                SourceCodeKind.Script : SourceCodeKind.Regular;
        }

        private void DetachFromDocument(string moniker)
        {
            _foregroundThreadAffinitization.AssertIsForeground();
            if (_fileTrackingMetadataAsSourceService.TryRemoveDocumentFromWorkspace(moniker))
            {
                return;
            }

            if (_monikersToProjectIdAndContainer.TryGetValue(moniker, out var projectIdAndContainer))
            {
                var document = this.CurrentSolution.GetProject(projectIdAndContainer.projectId).Documents.Single();

                // We must close the document prior to deleting the project
                OnDocumentClosed(document.Id, new FileTextLoader(document.FilePath, defaultEncoding: null));
                OnProjectRemoved(document.Project.Id);

                _monikersToProjectIdAndContainer.Remove(moniker);

                return;
            }
        }

        public override bool CanApplyChange(ApplyChangesKind feature)
            => feature == ApplyChangesKind.ChangeDocument;

        protected override void ApplyDocumentTextChanged(DocumentId documentId, SourceText newText)
        {
            foreach (var (projectId, textContainer) in _monikersToProjectIdAndContainer.Values)
            {
                if (projectId == documentId.ProjectId)
                {
                    TextEditApplication.UpdateText(newText, textContainer.GetTextBuffer(), EditOptions.DefaultMinimalChange);
                    break;
                }
            }
        }

        private class LanguageInformation
        {
            public LanguageInformation(string languageName, string scriptExtension)
            {
                this.LanguageName = languageName;
                this.ScriptExtension = scriptExtension;
            }

            public string LanguageName { get; }
            public string ScriptExtension { get; }
        }
    }
}
