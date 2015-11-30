// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSLangProj;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal abstract partial class AbstractProject : IVisualStudioHostProject
    {
        internal static object RuleSetErrorId = new object();

        private readonly ProjectId _id;
        private readonly string _language;
        private readonly IVsHierarchy _hierarchy;
        private readonly VersionStamp _version;
        private readonly string _projectSystemName;

        /// <summary>
        /// The path to the project file itself. This is intentionally kept private, to avoid having to deal with people who
        /// want the file path without realizing they need to deal with renames. If you need the folder of the project, just
        /// use <see cref="ContainingDirectoryPathOpt" /> which is internal and doesn't change for a project.
        /// </summary>
        private string _filePathOpt;

        private readonly MiscellaneousFilesWorkspace _miscellaneousFilesWorkspaceOpt;
        private readonly VisualStudioWorkspaceImpl _visualStudioWorkspaceOpt;
        private readonly IContentTypeRegistryService _contentTypeRegistryService;
        private readonly IVsReportExternalErrors _externalErrorReporter;
        private readonly HostDiagnosticUpdateSource _hostDiagnosticUpdateSourceOpt;

        internal readonly IServiceProvider ServiceProvider;
        protected readonly VisualStudioProjectTracker ProjectTracker;
        protected readonly IVsRunningDocumentTable4 RunningDocumentTable;

        private string _objOutputPathOpt;
        private string _binOutputPathOpt;

        private string _assemblyName;

        private CompilationOptions _compilationOptions;
        private ParseOptions _parseOptions;
        private readonly List<ProjectReference> _projectReferences = new List<ProjectReference>();
        private readonly List<VisualStudioMetadataReference> _metadataReferences = new List<VisualStudioMetadataReference>();
        private readonly Dictionary<DocumentId, IVisualStudioHostDocument> _documents = new Dictionary<DocumentId, IVisualStudioHostDocument>();
        private readonly Dictionary<string, IVisualStudioHostDocument> _documentMonikers = new Dictionary<string, IVisualStudioHostDocument>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, VisualStudioAnalyzer> _analyzers = new Dictionary<string, VisualStudioAnalyzer>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<DocumentId, IVisualStudioHostDocument> _additionalDocuments = new Dictionary<DocumentId, IVisualStudioHostDocument>();
        protected IRuleSetFile ruleSet = null;

        /// <summary>
        /// The list of files which have been added to the project but we aren't tracking since they
        /// aren't real source files. Sometimes we're asked to add silly things like HTML files or XAML
        /// files, and if those are open in a strange editor we just bail.
        /// </summary>
        private readonly ISet<string> _untrackedDocuments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The path to a metadata reference that was converted to project references.
        /// </summary>
        private readonly Dictionary<string, ProjectReference> _metadataFileNameToConvertedProjectReference = new Dictionary<string, ProjectReference>(StringComparer.OrdinalIgnoreCase);

        private bool _pushingChangesToWorkspaceHosts;

        /// <summary>
        /// Guid of the _hierarchy
        /// 
        /// it is not readonly since it can be changed while loading project
        /// </summary>
        private Guid _guid;

        /// <summary>
        /// string (Guid) of the _hierarchy project type
        /// </summary>
        private readonly string _projectType;

        // PERF: Create these event handlers once to be shared amongst all documents (the sender arg identifies which document and project)
        private static readonly EventHandler<bool> s_documentOpenedEventHandler = OnDocumentOpened;
        private static readonly EventHandler<bool> s_documentClosingEventHandler = OnDocumentClosing;
        private static readonly EventHandler s_documentUpdatedOnDiskEventHandler = OnDocumentUpdatedOnDisk;
        private static readonly EventHandler<bool> s_additionalDocumentOpenedEventHandler = OnAdditionalDocumentOpened;
        private static readonly EventHandler<bool> s_additionalDocumentClosingEventHandler = OnAdditionalDocumentClosing;
        private static readonly EventHandler s_additionalDocumentUpdatedOnDiskEventHandler = OnAdditionalDocumentUpdatedOnDisk;

        public AbstractProject(
            VisualStudioProjectTracker projectTracker,
            Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
            string projectSystemName,
            IVsHierarchy hierarchy,
            string language,
            IServiceProvider serviceProvider,
            MiscellaneousFilesWorkspace miscellaneousFilesWorkspaceOpt,
            VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt)
        {
            Contract.ThrowIfNull(projectSystemName);

            this.ServiceProvider = serviceProvider;

            _language = language;
            _hierarchy = hierarchy;

            // get project id guid
            _guid = GetProjectIDGuid(hierarchy);

            // get project type guid
            _projectType = GetProjectType(hierarchy);

            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            _contentTypeRegistryService = componentModel.GetService<IContentTypeRegistryService>();

            this.RunningDocumentTable = (IVsRunningDocumentTable4)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            this.DisplayName = projectSystemName;
            this.ProjectTracker = projectTracker;

            _projectSystemName = projectSystemName;
            _miscellaneousFilesWorkspaceOpt = miscellaneousFilesWorkspaceOpt;
            _visualStudioWorkspaceOpt = visualStudioWorkspaceOpt;
            _hostDiagnosticUpdateSourceOpt = hostDiagnosticUpdateSourceOpt;

            UpdateProjectDisplayNameAndFilePath();

            if (_filePathOpt != null)
            {
                _version = VersionStamp.Create(File.GetLastWriteTimeUtc(_filePathOpt));
            }
            else
            {
                _version = VersionStamp.Create();
            }

            _id = this.ProjectTracker.GetOrCreateProjectIdForPath(_filePathOpt ?? _projectSystemName, _projectSystemName);
            if (reportExternalErrorCreatorOpt != null)
            {
                _externalErrorReporter = reportExternalErrorCreatorOpt(_id);
            }

            ConnectHierarchyEvents();

            SetIsWebsite(hierarchy);
        }

        private static string GetProjectType(IVsHierarchy hierarchy)
        {
            var aggregatableProject = hierarchy as IVsAggregatableProject;
            if (aggregatableProject == null)
            {
                return string.Empty;
            }

            string projectType;
            if (ErrorHandler.Succeeded(aggregatableProject.GetAggregateProjectTypeGuids(out projectType)))
            {
                return projectType;
            }

            return string.Empty;
        }

        private static Guid GetProjectIDGuid(IVsHierarchy hierarchy)
        {
            Guid guid;
            if (hierarchy.TryGetGuidProperty(__VSHPROPID.VSHPROPID_ProjectIDGuid, out guid))
            {
                return guid;
            }

            return Guid.Empty;
        }

        private void SetIsWebsite(IVsHierarchy hierarchy)
        {
            EnvDTE.Project project;
            try
            {
                if (hierarchy.TryGetProject(out project))
                {
                    this.IsWebSite = project.Kind == VsWebSite.PrjKind.prjKindVenusProject;
                }
            }
            catch (Exception)
            {
                this.IsWebSite = false;
            }
        }

        /// <summary>
        /// Returns a display name for the given project.
        /// </summary>
        private static bool TryGetProjectDisplayName(IVsHierarchy hierarchy, out string name)
        {
            name = null;

            if (!hierarchy.TryGetName(out name))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Indicates whether this project is a website type.
        /// </summary>
        public bool IsWebSite { get; private set; }

        /// <summary>
        /// A full path to the project obj output binary, or null if the project doesn't have an obj output binary.
        /// </summary>
        internal string TryGetObjOutputPath() => _objOutputPathOpt;

        /// <summary>
        /// A full path to the project bin output binary, or null if the project doesn't have an bin output binary.
        /// </summary>
        internal string TryGetBinOutputPath() => _binOutputPathOpt;

        internal VisualStudioWorkspaceImpl VisualStudioWorkspace => _visualStudioWorkspaceOpt;

        internal IRuleSetFile RuleSetFile => this.ruleSet;

        internal HostDiagnosticUpdateSource HostDiagnosticUpdateSource => _hostDiagnosticUpdateSourceOpt;

        public ProjectId Id => _id;

        public string Language => _language;

        public IVsHierarchy Hierarchy => _hierarchy;

        public Guid Guid => _guid;

        public string ProjectType => _projectType;

        public Workspace Workspace => (Workspace)_visualStudioWorkspaceOpt ?? _miscellaneousFilesWorkspaceOpt;

        public VersionStamp Version => _version;

        /// <summary>
        /// The containing directory of the project. Null if none exists (consider Venus.)
        /// </summary>
        protected string ContainingDirectoryPathOpt
        {
            get
            {
                if (_filePathOpt != null)
                {
                    return Path.GetDirectoryName(_filePathOpt);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The public display name of the project. This name is not unique and may be shared
        /// between multiple projects, especially in cases like Venus where the intellisense
        /// projects will match the name of their logical parent project.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// The name of the project according to the project system. In "regular" projects this is
        /// equivalent to <see cref="DisplayName"/>, but in Venus cases these will differ. The
        /// ProjectSystemName is the 2_Default.aspx project name, whereas the regular display name
        /// matches the display name of the project the user actually sees in the solution explorer.
        /// These can be assumed to be unique within the Visual Studio workspace.
        /// </summary>
        public string ProjectSystemName
        {
            get { return _projectSystemName; }
        }

        protected DocumentProvider DocumentProvider
        {
            get { return this.ProjectTracker.DocumentProvider; }
        }

        protected VisualStudioMetadataReferenceManager MetadataReferenceProvider
        {
            get { return this.ProjectTracker.MetadataReferenceProvider; }
        }

        protected IContentTypeRegistryService ContentTypeRegistryService
        {
            get { return _contentTypeRegistryService; }
        }

        public ProjectInfo CreateProjectInfoForCurrentState()
        {
            ValidateReferences();

            return ProjectInfo.Create(
                this.Id,
                this.Version,
                this.DisplayName,
                _assemblyName ?? this.ProjectSystemName,
                this.Language,
                filePath: _filePathOpt,
                outputFilePath: this.TryGetObjOutputPath(),
                compilationOptions: _compilationOptions,
                parseOptions: _parseOptions,
                documents: _documents.Values.Select(d => d.GetInitialState()),
                metadataReferences: _metadataReferences.Select(r => r.CurrentSnapshot),
                projectReferences: _projectReferences,
                analyzerReferences: _analyzers.Values.Select(a => a.GetReference()),
                additionalDocuments: _additionalDocuments.Values.Select(d => d.GetInitialState()));
        }

        protected ImmutableArray<string> GetStrongNameKeyPaths()
        {
            var outputPath = this.TryGetObjOutputPath();

            if (this.ContainingDirectoryPathOpt == null && outputPath == null)
            {
                return ImmutableArray<string>.Empty;
            }

            var builder = ImmutableArray.CreateBuilder<string>();
            if (this.ContainingDirectoryPathOpt != null)
            {
                builder.Add(this.ContainingDirectoryPathOpt);
            }

            if (outputPath != null)
            {
                builder.Add(Path.GetDirectoryName(outputPath));
            }

            return builder.ToImmutable();
        }

        public ImmutableArray<ProjectReference> GetCurrentProjectReferences()
        {
            return ImmutableArray.CreateRange(_projectReferences);
        }

        public IVisualStudioHostDocument GetDocumentOrAdditionalDocument(DocumentId id)
        {
            IVisualStudioHostDocument doc;
            _documents.TryGetValue(id, out doc);

            if (doc == null)
            {
                _additionalDocuments.TryGetValue(id, out doc);
            }

            return doc;
        }

        public IEnumerable<IVisualStudioHostDocument> GetCurrentDocuments()
        {
            return _documents.Values.ToImmutableArrayOrEmpty();
        }

        public bool ContainsFile(string moniker)
        {
            return _documentMonikers.ContainsKey(moniker);
        }

        public IVisualStudioHostDocument GetCurrentDocumentFromPath(string filePath)
        {
            IVisualStudioHostDocument document;
            _documentMonikers.TryGetValue(filePath, out document);
            return document;
        }

        public bool HasMetadataReference(string filename)
        {
            return _metadataReferences.Any(r => StringComparer.OrdinalIgnoreCase.Equals(r.FilePath, filename));
        }

        public VisualStudioMetadataReference TryGetCurrentMetadataReference(string filename)
        {
            // We must normalize the file path, since the paths we're comparing to are always normalized
            filename = FileUtilities.NormalizeAbsolutePath(filename);

            return _metadataReferences.SingleOrDefault(r => StringComparer.OrdinalIgnoreCase.Equals(r.FilePath, filename));
        }

        public bool CurrentProjectReferencesContains(ProjectId projectId)
        {
            return _projectReferences.Any(r => r.ProjectId == projectId);
        }

        public bool CurrentProjectAnalyzersContains(string fullPath)
        {
            return _analyzers.ContainsKey(fullPath);
        }

        private static string GetAssemblyName(string outputPath)
        {
            Contract.Requires(outputPath != null);

            // dev11 sometimes gives us output path w/o extension, so removing extension becomes problematic
            if (outputPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                outputPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                outputPath.EndsWith(".netmodule", StringComparison.OrdinalIgnoreCase) ||
                outputPath.EndsWith(".winmdobj", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFileNameWithoutExtension(outputPath);
            }
            else
            {
                return Path.GetFileName(outputPath);
            }
        }

        protected void SetOptions(CompilationOptions compilationOptions, ParseOptions parseOptions)
        {
            _compilationOptions = compilationOptions;
            _parseOptions = parseOptions;

            if (_pushingChangesToWorkspaceHosts)
            {
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnOptionsChanged(_id, compilationOptions, parseOptions));
            }
        }

        protected int AddMetadataReferenceAndTryConvertingToProjectReferenceIfPossible(string filePath, MetadataReferenceProperties properties, int hResultForMissingFile)
        {
            // If this file is coming from a project, then we should convert it to a project reference instead
            AbstractProject project;
            if (ProjectTracker.TryGetProjectByBinPath(filePath, out project))
            {
                var projectReference = new ProjectReference(project.Id, properties.Aliases, properties.EmbedInteropTypes);
                if (CanAddProjectReference(projectReference))
                {
                    AddProjectReference(projectReference);
                    _metadataFileNameToConvertedProjectReference.Add(filePath, projectReference);
                    return VSConstants.S_OK;
                }
            }

            if (!File.Exists(filePath))
            {
                return hResultForMissingFile;
            }

            AddMetadataReferenceCore(this.MetadataReferenceProvider.CreateMetadataReference(this, filePath, properties));

            return VSConstants.S_OK;
        }

        protected void RemoveMetadataReference(string filePath)
        {
            // Is this a reference we converted to a project reference?
            ProjectReference projectReference;
            if (_metadataFileNameToConvertedProjectReference.TryGetValue(filePath, out projectReference))
            {
                // We converted this, so remove the project reference instead
                RemoveProjectReference(projectReference);

                Contract.ThrowIfFalse(_metadataFileNameToConvertedProjectReference.Remove(filePath));
            }

            // Just a metadata reference, so remove all of those
            var referenceToRemove = TryGetCurrentMetadataReference(filePath);
            if (referenceToRemove != null)
            {
                RemoveMetadataReferenceCore(referenceToRemove, disposeReference: true);
            }
        }

        private void AddMetadataReferenceCore(VisualStudioMetadataReference reference)
        {
            _metadataReferences.Add(reference);

            if (_pushingChangesToWorkspaceHosts)
            {
                var snapshot = reference.CurrentSnapshot;
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnMetadataReferenceAdded(this.Id, snapshot));
            }

            reference.UpdatedOnDisk += OnImportChanged;
        }

        private void RemoveMetadataReferenceCore(VisualStudioMetadataReference reference, bool disposeReference)
        {
            _metadataReferences.Remove(reference);

            if (_pushingChangesToWorkspaceHosts)
            {
                var snapshot = reference.CurrentSnapshot;
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnMetadataReferenceRemoved(this.Id, snapshot));
            }

            reference.UpdatedOnDisk -= OnImportChanged;

            if (disposeReference)
            {
                reference.Dispose();
            }
        }

        /// <summary>
        /// Called when a referenced metadata file changes on disk.
        /// </summary>
        private void OnImportChanged(object sender, EventArgs e)
        {
            VisualStudioMetadataReference reference = (VisualStudioMetadataReference)sender;

            // Ensure that we are still referencing this binary
            if (_metadataReferences.Contains(reference))
            {
                // remove the old metadata reference
                this.RemoveMetadataReferenceCore(reference, disposeReference: false);

                // Signal to update the underlying reference snapshot
                reference.UpdateSnapshot();

                // add it back (it will now be based on the new file contents)
                this.AddMetadataReferenceCore(reference);
            }
        }

        private void OnAnalyzerChanged(object sender, EventArgs e)
        {
            VisualStudioAnalyzer analyzer = (VisualStudioAnalyzer)sender;

            RemoveAnalyzerAssembly(analyzer.FullPath);
            AddAnalyzerAssembly(analyzer.FullPath);
        }

        protected void AddProjectReference(ProjectReference projectReference)
        {
            // dev11 is sometimes calling us multiple times for the same data
            if (!CanAddProjectReference(projectReference))
            {
                return;
            }

            // always manipulate current state after workspace is told so it will correctly observe the initial state
            _projectReferences.Add(projectReference);

            if (_pushingChangesToWorkspaceHosts)
            {
                // This project is already pushed to listening workspace hosts, but it's possible that our target
                // project hasn't been yet. Get the dependent project into the workspace as well.
                var targetProject = this.ProjectTracker.GetProject(projectReference.ProjectId);
                this.ProjectTracker.StartPushingToWorkspaceAndNotifyOfOpenDocuments(SpecializedCollections.SingletonEnumerable(targetProject));

                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnProjectReferenceAdded(this.Id, projectReference));
            }
        }

        protected bool CanAddProjectReference(ProjectReference projectReference)
        {
            if (projectReference.ProjectId == this.Id)
            {
                // cannot self reference
                return false;
            }

            if (_projectReferences.Contains(projectReference))
            {
                // already have this reference
                return false;
            }

            var project = this.ProjectTracker.GetProject(projectReference.ProjectId);
            if (project != null)
            {
                // cannot add a reference to a project that references us (it would make a cycle)
                return !project.TransitivelyReferences(this.Id);
            }

            return true;
        }

        private bool TransitivelyReferences(ProjectId projectId)
        {
            return TransitivelyReferencesWorker(projectId, new HashSet<ProjectId>());
        }

        private bool TransitivelyReferencesWorker(ProjectId projectId, HashSet<ProjectId> visited)
        {
            visited.Add(this.Id);

            foreach (var pr in _projectReferences)
            {
                if (projectId == pr.ProjectId)
                {
                    return true;
                }

                if (!visited.Contains(pr.ProjectId))
                {
                    var project = this.ProjectTracker.GetProject(pr.ProjectId);
                    if (project != null)
                    {
                        if (project.TransitivelyReferencesWorker(projectId, visited))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        protected void RemoveProjectReference(ProjectReference projectReference)
        {
            Contract.ThrowIfFalse(_projectReferences.Remove(projectReference));

            if (_pushingChangesToWorkspaceHosts)
            {
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnProjectReferenceRemoved(this.Id, projectReference));
            }
        }

        private static void OnDocumentOpened(object sender, bool isCurrentContext)
        {
            IVisualStudioHostDocument document = (IVisualStudioHostDocument)sender;
            AbstractProject project = (AbstractProject)document.Project;

            if (project._pushingChangesToWorkspaceHosts)
            {
                project.ProjectTracker.NotifyWorkspaceHosts(host => host.OnDocumentOpened(document.Id, document.GetOpenTextBuffer(), isCurrentContext));
            }
            else
            {
                StartPushingToWorkspaceAndNotifyOfOpenDocuments(project);
            }
        }

        private static void OnDocumentClosing(object sender, bool updateActiveContext)
        {
            IVisualStudioHostDocument document = (IVisualStudioHostDocument)sender;
            AbstractProject project = (AbstractProject)document.Project;
            var projectTracker = project.ProjectTracker;

            if (project._pushingChangesToWorkspaceHosts)
            {
                projectTracker.NotifyWorkspaceHosts(host => host.OnDocumentClosed(document.Id, document.GetOpenTextBuffer(), document.Loader, updateActiveContext));
            }
        }

        private static void OnDocumentUpdatedOnDisk(object sender, EventArgs e)
        {
            IVisualStudioHostDocument document = (IVisualStudioHostDocument)sender;
            AbstractProject project = (AbstractProject)document.Project;

            if (project._pushingChangesToWorkspaceHosts)
            {
                project.ProjectTracker.NotifyWorkspaceHosts(host => host.OnDocumentTextUpdatedOnDisk(document.Id));
            }
        }

        private static void OnAdditionalDocumentOpened(object sender, bool isCurrentContext)
        {
            IVisualStudioHostDocument document = (IVisualStudioHostDocument)sender;
            AbstractProject project = (AbstractProject)document.Project;

            if (project._pushingChangesToWorkspaceHosts)
            {
                project.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAdditionalDocumentOpened(document.Id, document.GetOpenTextBuffer(), isCurrentContext));
            }
            else
            {
                StartPushingToWorkspaceAndNotifyOfOpenDocuments(project);
            }
        }

        private static void OnAdditionalDocumentClosing(object sender, bool notUsed)
        {
            IVisualStudioHostDocument document = (IVisualStudioHostDocument)sender;
            AbstractProject project = (AbstractProject)document.Project;
            var projectTracker = project.ProjectTracker;

            if (project._pushingChangesToWorkspaceHosts)
            {
                projectTracker.NotifyWorkspaceHosts(host => host.OnAdditionalDocumentClosed(document.Id, document.GetOpenTextBuffer(), document.Loader));
            }
        }

        private static void OnAdditionalDocumentUpdatedOnDisk(object sender, EventArgs e)
        {
            IVisualStudioHostDocument document = (IVisualStudioHostDocument)sender;
            AbstractProject project = (AbstractProject)document.Project;

            if (project._pushingChangesToWorkspaceHosts)
            {
                project.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAdditionalDocumentTextUpdatedOnDisk(document.Id));
            }
        }

        protected void AddFile(string filename, SourceCodeKind sourceCodeKind, uint itemId, Func<ITextBuffer, bool> canUseTextBuffer)
        {
            var document = this.DocumentProvider.TryGetDocumentForFile(this, itemId, filePath: filename, sourceCodeKind: sourceCodeKind, canUseTextBuffer: canUseTextBuffer);

            if (document == null)
            {
                // It's possible this file is open in some very strange editor. In that case, we'll just ignore it.
                // This might happen if somebody decides to mark a non-source-file as something to compile.

                // TODO: Venus does this for .aspx/.cshtml files which is completely unnecessary for Roslyn. We should remove that code.
                AddUntrackedFile(filename);
                return;
            }

            AddDocument(
                document,
                isCurrentContext: document.Project.Hierarchy == LinkedFileUtilities.GetContextHierarchy(document, RunningDocumentTable));
        }

        protected void AddUntrackedFile(string filename)
        {
            _untrackedDocuments.Add(filename);
        }

        protected void RemoveFile(string filename)
        {
            // Remove this as an untracked file, if it is
            if (_untrackedDocuments.Remove(filename))
            {
                return;
            }

            IVisualStudioHostDocument document = this.GetCurrentDocumentFromPath(filename);
            if (document == null)
            {
                throw new InvalidOperationException("The document is not a part of the finalProject.");
            }

            RemoveDocument(document);
        }

        internal void AddDocument(IVisualStudioHostDocument document, bool isCurrentContext)
        {
            // We do not want to allow message pumping/reentrancy when processing project system changes.
            using (Dispatcher.CurrentDispatcher.DisableProcessing())
            {
                if (_miscellaneousFilesWorkspaceOpt != null)
                {
                    _miscellaneousFilesWorkspaceOpt.OnFileIncludedInProject(document);
                }

                _documents.Add(document.Id, document);
                _documentMonikers.Add(document.Key.Moniker, document);

                if (_pushingChangesToWorkspaceHosts)
                {
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnDocumentAdded(document.GetInitialState()));

                    if (document.IsOpen)
                    {
                        this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnDocumentOpened(document.Id, document.GetOpenTextBuffer(), isCurrentContext));
                    }
                }

                document.Opened += s_documentOpenedEventHandler;
                document.Closing += s_documentClosingEventHandler;
                document.UpdatedOnDisk += s_documentUpdatedOnDiskEventHandler;

                DocumentProvider.NotifyDocumentRegisteredToProject(document);

                if (!_pushingChangesToWorkspaceHosts && document.IsOpen)
                {
                    StartPushingToWorkspaceAndNotifyOfOpenDocuments();
                }
            }
        }

        internal void RemoveDocument(IVisualStudioHostDocument document)
        {
            // We do not want to allow message pumping/reentrancy when processing project system changes.
            using (Dispatcher.CurrentDispatcher.DisableProcessing())
            {
                _documents.Remove(document.Id);
                _documentMonikers.Remove(document.Key.Moniker);

                UninitializeDocument(document);
                OnDocumentRemoved(document.Key.Moniker);
            }
        }

        internal void AddAdditionalDocument(IVisualStudioHostDocument document, bool isCurrentContext)
        {
            if (_miscellaneousFilesWorkspaceOpt != null)
            {
                _miscellaneousFilesWorkspaceOpt.OnFileIncludedInProject(document);
            }

            _additionalDocuments.Add(document.Id, document);
            _documentMonikers.Add(document.Key.Moniker, document);

            if (_pushingChangesToWorkspaceHosts)
            {
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAdditionalDocumentAdded(document.GetInitialState()));

                if (document.IsOpen)
                {
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAdditionalDocumentOpened(document.Id, document.GetOpenTextBuffer(), isCurrentContext));
                }
            }

            document.Opened += s_additionalDocumentOpenedEventHandler;
            document.Closing += s_additionalDocumentClosingEventHandler;
            document.UpdatedOnDisk += s_additionalDocumentUpdatedOnDiskEventHandler;

            DocumentProvider.NotifyDocumentRegisteredToProject(document);

            if (!_pushingChangesToWorkspaceHosts && document.IsOpen)
            {
                StartPushingToWorkspaceAndNotifyOfOpenDocuments();
            }
        }

        internal void RemoveAdditionalDocument(IVisualStudioHostDocument document)
        {
            _additionalDocuments.Remove(document.Id);
            _documentMonikers.Remove(document.Key.Moniker);

            UninitializeAdditionalDocument(document);
        }

        public virtual void Disconnect()
        {
            using (_visualStudioWorkspaceOpt?.Services.GetService<IGlobalOperationNotificationService>()?.Start("Disconnect Project"))
            {
                // Unsubscribe IVsHierarchyEvents
                DisconnectHierarchyEvents();

                // The project is going away, so let's remove ourselves from the host. First, we
                // close and dispose of any remaining documents
                foreach (var document in this.GetCurrentDocuments())
                {
                    UninitializeDocument(document);
                }

                // Dispose metadata references.
                foreach (var reference in _metadataReferences)
                {
                    reference.Dispose();
                }

                foreach (var analyzer in _analyzers.Values)
                {
                    analyzer.Dispose();
                }

                // Make sure we clear out any external errors left when closing the project.
                if (_externalErrorReporter != null)
                {
                    _externalErrorReporter.ClearAllErrors();
                }

                // Make sure we clear out any host errors left when closing the project.
                if (_hostDiagnosticUpdateSourceOpt != null)
                {
                    _hostDiagnosticUpdateSourceOpt.ClearAllDiagnosticsForProject(this.Id);
                }

                ClearAnalyzerRuleSet();

                this.ProjectTracker.RemoveProject(this);
            }
        }

        internal void TryProjectConversionForIntroducedOutputPath(string binPath, AbstractProject projectToReference)
        {
            // We should not already have references for this, since we're only introducing the path for the first time
            Contract.ThrowIfTrue(_metadataFileNameToConvertedProjectReference.ContainsKey(binPath));

            var metadataReference = TryGetCurrentMetadataReference(binPath);
            if (metadataReference != null)
            {
                var projectReference = new ProjectReference(
                    projectToReference.Id,
                    metadataReference.Properties.Aliases,
                    metadataReference.Properties.EmbedInteropTypes);

                if (CanAddProjectReference(projectReference))
                {
                    RemoveMetadataReferenceCore(metadataReference, disposeReference: true);
                    AddProjectReference(projectReference);

                    _metadataFileNameToConvertedProjectReference.Add(binPath, projectReference);
                }
            }
        }

        internal void UndoProjectReferenceConversionForDisappearingOutputPath(string binPath)
        {
            ProjectReference projectReference;
            if (_metadataFileNameToConvertedProjectReference.TryGetValue(binPath, out projectReference))
            {
                // We converted this, so convert it back to a metadata reference
                RemoveProjectReference(projectReference);

                var metadataReferenceProperties = new MetadataReferenceProperties(
                    MetadataImageKind.Assembly,
                    projectReference.Aliases,
                    projectReference.EmbedInteropTypes);

                AddMetadataReferenceCore(MetadataReferenceProvider.CreateMetadataReference(this, binPath, metadataReferenceProperties));

                Contract.ThrowIfFalse(_metadataFileNameToConvertedProjectReference.Remove(binPath));
            }
        }

        protected void UpdateMetadataReferenceAliases(string file, ImmutableArray<string> aliases)
        {
            file = FileUtilities.NormalizeAbsolutePath(file);

            // Have we converted these to project references?
            ProjectReference convertedProjectReference;

            if (_metadataFileNameToConvertedProjectReference.TryGetValue(file, out convertedProjectReference))
            {
                var project = ProjectTracker.GetProject(convertedProjectReference.ProjectId);
                UpdateProjectReferenceAliases(project, aliases);
            }
            else
            {
                var existingReference = TryGetCurrentMetadataReference(file);
                Contract.ThrowIfNull(existingReference);

                var newProperties = existingReference.Properties.WithAliases(aliases);

                RemoveMetadataReferenceCore(existingReference, disposeReference: true);

                AddMetadataReferenceCore(this.MetadataReferenceProvider.CreateMetadataReference(this, file, newProperties));
            }
        }

        protected void UpdateProjectReferenceAliases(AbstractProject referencedProject, ImmutableArray<string> aliases)
        {
            var projectReference = GetCurrentProjectReferences().Single(r => r.ProjectId == referencedProject.Id);

            var newProjectReference = new ProjectReference(referencedProject.Id, aliases, projectReference.EmbedInteropTypes);

            // Is this a project with converted references? If so, make sure we track it
            string referenceBinPath = referencedProject.TryGetBinOutputPath();
            if (referenceBinPath != null && _metadataFileNameToConvertedProjectReference.ContainsKey(referenceBinPath))
            {
                _metadataFileNameToConvertedProjectReference[referenceBinPath] = newProjectReference;
            }

            // Remove the existing reference first
            RemoveProjectReference(projectReference);

            AddProjectReference(newProjectReference);
        }

        private void UninitializeDocument(IVisualStudioHostDocument document)
        {
            if (_pushingChangesToWorkspaceHosts)
            {
                if (document.IsOpen)
                {
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnDocumentClosed(document.Id, document.GetOpenTextBuffer(), document.Loader, updateActiveContext: true));
                }

                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnDocumentRemoved(document.Id));
            }

            if (_miscellaneousFilesWorkspaceOpt != null)
            {
                _miscellaneousFilesWorkspaceOpt.OnFileRemovedFromProject(document);
            }

            document.Opened -= s_documentOpenedEventHandler;
            document.Closing -= s_documentClosingEventHandler;
            document.UpdatedOnDisk -= s_documentUpdatedOnDiskEventHandler;

            document.Dispose();
        }

        private void UninitializeAdditionalDocument(IVisualStudioHostDocument document)
        {
            if (_pushingChangesToWorkspaceHosts)
            {
                if (document.IsOpen)
                {
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAdditionalDocumentClosed(document.Id, document.GetOpenTextBuffer(), document.Loader));
                }

                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAdditionalDocumentRemoved(document.Id));
            }

            if (_miscellaneousFilesWorkspaceOpt != null)
            {
                _miscellaneousFilesWorkspaceOpt.OnFileRemovedFromProject(document);
            }

            document.Opened -= s_additionalDocumentOpenedEventHandler;
            document.Closing -= s_additionalDocumentClosingEventHandler;
            document.UpdatedOnDisk -= s_additionalDocumentUpdatedOnDiskEventHandler;

            document.Dispose();
        }

        protected virtual void OnDocumentRemoved(string filePath)
        {
        }

        protected virtual void UpdateAnalyzerRules()
        {
        }

        private readonly Dictionary<uint, IReadOnlyList<string>> _folderNameMap = new Dictionary<uint, IReadOnlyList<string>>();

        public IReadOnlyList<string> GetFolderNames(uint documentItemID)
        {
            object parentObj;
            if (documentItemID != (uint)VSConstants.VSITEMID.Nil && _hierarchy.GetProperty(documentItemID, (int)VsHierarchyPropID.Parent, out parentObj) == VSConstants.S_OK)
            {
                var parentID = this.UnboxVSItemId(parentObj);
                if (parentID != (uint)VSConstants.VSITEMID.Nil && parentID != (uint)VSConstants.VSITEMID.Root)
                {
                    return this.GetFolderNamesForFolder(parentID);
                }
            }

            return SpecializedCollections.EmptyReadOnlyList<string>();
        }

        private readonly List<string> _tmpFolders = new List<string>();

        private IReadOnlyList<string> GetFolderNamesForFolder(uint folderItemID)
        {
            // note: use of tmpFolders is assuming this API is called on UI thread only.
            _tmpFolders.Clear();

            IReadOnlyList<string> names;
            if (!_folderNameMap.TryGetValue(folderItemID, out names))
            {
                this.ComputeFolderNames(folderItemID, _tmpFolders);
                names = _tmpFolders.ToImmutableArray();
                _folderNameMap.Add(folderItemID, names);
            }
            else
            {
                // verify names, and change map if we get a different set.
                // this is necessary because we only get document adds/removes from the project system
                // when a document name or folder name changes.
                this.ComputeFolderNames(folderItemID, _tmpFolders);
                if (!Enumerable.SequenceEqual(names, _tmpFolders))
                {
                    names = _tmpFolders.ToImmutableArray();
                    _folderNameMap[folderItemID] = names;
                }
            }

            return names;
        }

        // Different hierarchies are inconsistent on whether they return ints or uints for VSItemIds.
        // Technically it should be a uint.  However, there's no enforcement of this, and marshalling
        // from native to managed can end up resulting in boxed ints instead.  Handle both here so 
        // we're resilient to however the IVsHierarchy was actually implemented.
        private uint UnboxVSItemId(object id)
        {
            return id is uint ? (uint)id : unchecked((uint)(int)id);
        }

        private void ComputeFolderNames(uint folderItemID, List<string> names)
        {
            object nameObj;
            if (_hierarchy.GetProperty((uint)folderItemID, (int)VsHierarchyPropID.Name, out nameObj) == VSConstants.S_OK)
            {
                // For 'Shared' projects, IVSHierarchy returns a hierarchy item with < character in its name (i.e. <SharedProjectName>)
                // as a child of the root item. There is no such item in the 'visual' hierarchy in solution explorer and no such folder
                // is present on disk either. Since this is not a real 'folder', we exclude it from the contents of Document.Folders.
                // Note: The parent of the hierarchy item that contains < character in its name is VSITEMID.Root. So we don't need to
                // worry about accidental propagation out of the Shared project to any containing 'Solution' folders - the check for
                // VSITEMID.Root below already takes care of that.
                var name = (string)nameObj;
                if (!name.StartsWith("<", StringComparison.OrdinalIgnoreCase))
                {
                    names.Insert(0, name);
                }
            }

            object parentObj;
            if (_hierarchy.GetProperty((uint)folderItemID, (int)VsHierarchyPropID.Parent, out parentObj) == VSConstants.S_OK)
            {
                var parentID = this.UnboxVSItemId(parentObj);
                if (parentID != (uint)VSConstants.VSITEMID.Nil && parentID != (uint)VSConstants.VSITEMID.Root)
                {
                    ComputeFolderNames(parentID, names);
                }
            }
        }

        internal void StartPushingToWorkspaceHosts()
        {
            _pushingChangesToWorkspaceHosts = true;
        }

        internal void StopPushingToWorkspaceHosts()
        {
            _pushingChangesToWorkspaceHosts = false;
        }

        internal void StartPushingToWorkspaceAndNotifyOfOpenDocuments()
        {
            StartPushingToWorkspaceAndNotifyOfOpenDocuments(this);
        }

        internal bool PushingChangesToWorkspaceHosts
        {
            get
            {
                return _pushingChangesToWorkspaceHosts;
            }
        }

        protected void UpdateRuleSetError(IRuleSetFile ruleSetFile)
        {
            if (this.HostDiagnosticUpdateSource == null)
            {
                return;
            }

            if (ruleSetFile == null ||
                ruleSetFile.GetException() == null)
            {
                this.HostDiagnosticUpdateSource.ClearDiagnosticsForProject(this.Id, RuleSetErrorId);
            }
            else
            {
                string message = string.Format(ServicesVSResources.ERR_CantReadRulesetFileMessage, ruleSetFile.FilePath, ruleSetFile.GetException().Message);
                var data = new DiagnosticData(
                    id: IDEDiagnosticIds.ErrorReadingRulesetId,
                    category: FeaturesResources.ErrorCategory,
                    message: message,
                    enuMessageForBingSearch: ServicesVSResources.ERR_CantReadRulesetFileMessage,
                    severity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true,
                    warningLevel: 0,
                    workspace: this.Workspace,
                    projectId: this.Id,
                    title: ServicesVSResources.ERR_CantReadRulesetFileTitle);

                this.HostDiagnosticUpdateSource.UpdateDiagnosticsForProject(this.Id, RuleSetErrorId, SpecializedCollections.SingletonEnumerable(data));
            }
        }

        protected void SetOutputPathAndRelatedData(string objOutputPath)
        {
            if (this.Workspace == null)
            {
                // can only happen in tests
                return;
            }

            if (PathUtilities.IsAbsolute(objOutputPath) && !string.Equals(_objOutputPathOpt, objOutputPath, StringComparison.OrdinalIgnoreCase))
            {
                // set obj output path if changed
                _objOutputPathOpt = objOutputPath;

                _compilationOptions = _compilationOptions.WithMetadataReferenceResolver(CreateMetadataReferenceResolver(
                    metadataService: this.Workspace.Services.GetService<IMetadataService>(),
                    projectDirectory: this.ContainingDirectoryPathOpt,
                    outputDirectory: Path.GetDirectoryName(_objOutputPathOpt)));

                if (_pushingChangesToWorkspaceHosts)
                {
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnOptionsChanged(this.Id, _compilationOptions, _parseOptions));
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnOutputFilePathChanged(this.Id, _objOutputPathOpt));
                }
            }

            // set assembly name if changed
            // we use designTimeOutputPath to get assembly name since it is more reliable way to get the assembly name.
            // otherwise, friend assembly all get messed up.
            var newAssemblyName = GetAssemblyName(_objOutputPathOpt ?? this.ProjectSystemName);
            if (!string.Equals(_assemblyName, newAssemblyName, StringComparison.Ordinal))
            {
                _assemblyName = newAssemblyName;

                if (_pushingChangesToWorkspaceHosts)
                {
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAssemblyNameChanged(this.Id, _assemblyName));
                }
            }

            // refresh final output path
            string newBinOutputPath;
            if (TryGetOutputPathFromBuildManager(out newBinOutputPath) && newBinOutputPath != null)
            {
                if (!string.Equals(_binOutputPathOpt, newBinOutputPath, StringComparison.OrdinalIgnoreCase))
                {
                    string oldBinOutputPath = _binOutputPathOpt;

                    // set obj output path if changed
                    _binOutputPathOpt = newBinOutputPath;

                    this.ProjectTracker.UpdateProjectBinPath(this, oldBinOutputPath, _binOutputPathOpt);
                }
            }
        }

        private void UpdateProjectDisplayNameAndFilePath()
        {
            bool updateMade = false;
            string newDisplayName;
            if (TryGetProjectDisplayName(_hierarchy, out newDisplayName) && this.DisplayName != newDisplayName)
            {
                this.DisplayName = newDisplayName;
                updateMade = true;
            }

            string newPath;
            if (ErrorHandler.Succeeded(((IVsProject3)_hierarchy).GetMkDocument((uint)VSConstants.VSITEMID.Root, out newPath)) &&
                File.Exists(newPath) && _filePathOpt != newPath)
            {
                Debug.Assert(PathUtilities.IsAbsolute(newPath));
                _filePathOpt = newPath;
                updateMade = true;
            }

            if (updateMade && _pushingChangesToWorkspaceHosts)
            {
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnProjectNameChanged(_id, this.DisplayName, _filePathOpt));
            }
        }

        private static void StartPushingToWorkspaceAndNotifyOfOpenDocuments(AbstractProject project)
        {
            // If a document is opened in a project but we haven't started pushing yet, we want to stop doing lazy
            // loading for this project and get it up to date so the user gets a fast experience there. If the file
            // was presented as open to us right away, then we'll never do this in OnDocumentOpened, so we should do
            // it here. It's important to do this after everything else happens in this method, so we don't get
            // strange ordering issues. It's still possible that this won't actually push changes if the workspace
            // host isn't ready to receive events yet.
            project.ProjectTracker.StartPushingToWorkspaceAndNotifyOfOpenDocuments(SpecializedCollections.SingletonEnumerable(project));
        }

        private static MetadataReferenceResolver CreateMetadataReferenceResolver(IMetadataService metadataService, string projectDirectory, string outputDirectory)
        {
            ImmutableArray<string> assemblySearchPaths;
            if (projectDirectory != null && outputDirectory != null)
            {
                assemblySearchPaths = ImmutableArray.Create(projectDirectory, outputDirectory);
            }
            else if (projectDirectory != null)
            {
                assemblySearchPaths = ImmutableArray.Create(projectDirectory);
            }
            else if (outputDirectory != null)
            {
                assemblySearchPaths = ImmutableArray.Create(outputDirectory);
            }
            else
            {
                assemblySearchPaths = ImmutableArray<string>.Empty;
            }

            return new WorkspaceMetadataFileReferenceResolver(metadataService, new RelativePathResolver(assemblySearchPaths, baseDirectory: projectDirectory));
        }

        private bool TryGetOutputPathFromBuildManager(out string binOutputPath)
        {
            binOutputPath = null;

            string outputDirectory;
            string targetFileName;

            var storage = _hierarchy as IVsBuildPropertyStorage;
            if (storage == null)
            {
                return false;
            }

            if (ErrorHandler.Failed(storage.GetPropertyValue("OutDir", null, (uint)_PersistStorageType.PST_PROJECT_FILE, out outputDirectory)) ||
                ErrorHandler.Failed(storage.GetPropertyValue("TargetFileName", null, (uint)_PersistStorageType.PST_PROJECT_FILE, out targetFileName)))
            {
                return false;
            }

            // web app case
            if (!PathUtilities.IsAbsolute(outputDirectory))
            {
                if (this.ContainingDirectoryPathOpt == null)
                {
                    return false;
                }

                outputDirectory = FileUtilities.ResolveRelativePath(outputDirectory, this.ContainingDirectoryPathOpt);
            }

            binOutputPath = FileUtilities.NormalizeAbsolutePath(Path.Combine(outputDirectory, targetFileName));
            return true;
        }

#if DEBUG
        public virtual bool Debug_VBEmbeddedCoreOptionOn
        {
            get
            {
                return false;
            }
        }
#endif

        [Conditional("DEBUG")]
        private void ValidateReferences()
        {
            // can happen when project is unloaded and reloaded or in Venus (aspx) case
            if (_filePathOpt == null || _binOutputPathOpt == null || _objOutputPathOpt == null)
            {
                return;
            }

            object property = null;
            if (ErrorHandler.Failed(_hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out property)))
            {
                return;
            }

            var dteProject = property as EnvDTE.Project;
            if (dteProject == null)
            {
                return;
            }

            var vsproject = dteProject.Object as VSProject;
            if (vsproject == null)
            {
                return;
            }

            var noReferenceOutputAssemblies = new List<string>();
            var factory = this.ServiceProvider.GetService(typeof(SVsEnumHierarchyItemsFactory)) as IVsEnumHierarchyItemsFactory;

            IEnumHierarchyItems items;
            if (ErrorHandler.Failed(factory.EnumHierarchyItems(_hierarchy, (uint)__VSEHI.VSEHI_Leaf, (uint)VSConstants.VSITEMID.Root, out items)))
            {
                return;
            }

            uint fetched;
            VSITEMSELECTION[] item = new VSITEMSELECTION[1];
            while (ErrorHandler.Succeeded(items.Next(1, item, out fetched)) && fetched == 1)
            {
                // ignore ReferenceOutputAssembly=false references since those will not be added to us in design time.
                var storage = _hierarchy as IVsBuildPropertyStorage;
                string value;
                storage.GetItemAttribute(item[0].itemid, "ReferenceOutputAssembly", out value);

                object caption;
                _hierarchy.GetProperty(item[0].itemid, (int)__VSHPROPID.VSHPROPID_Caption, out caption);

                if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "off", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "0", StringComparison.OrdinalIgnoreCase))
                {
                    noReferenceOutputAssemblies.Add((string)caption);
                }
            }

            var set = new HashSet<string>(vsproject.References.OfType<Reference>().Select(r => PathUtilities.IsAbsolute(r.Name) ? Path.GetFileNameWithoutExtension(r.Name) : r.Name), StringComparer.OrdinalIgnoreCase);
            var delta = set.Count - noReferenceOutputAssemblies.Count - (_projectReferences.Count + _metadataReferences.Count);
            if (delta == 0)
            {
                return;
            }

            // okay, two has different set of dlls referenced. check special Microsoft.VisualBasic case.
            if (delta != 1)
            {
                //// Contract.Requires(false, "different set of references!!!");
                return;
            }

            set.ExceptWith(noReferenceOutputAssemblies);
            set.ExceptWith(_projectReferences.Select(r => ProjectTracker.GetProject(r.ProjectId).DisplayName));
            set.ExceptWith(_metadataReferences.Select(m => Path.GetFileNameWithoutExtension(m.FilePath)));

            //// Contract.Requires(set.Count == 1);

            var reference = set.First();
            if (!string.Equals(reference, "Microsoft.VisualBasic", StringComparison.OrdinalIgnoreCase))
            {
                //// Contract.Requires(false, "unknown new reference " + reference);
                return;
            }

#if DEBUG
            // when we are missing microsoft.visualbasic reference, make sure we have embedded vb core option on.
            Contract.Requires(Debug_VBEmbeddedCoreOptionOn);
#endif
        }

        /// <summary>
        /// Used for unit testing: don't crash the process if something bad happens.
        /// </summary>
        internal static bool CrashOnException = true;

        protected static bool FilterException(Exception e)
        {
            if (CrashOnException)
            {
                FatalError.Report(e);
            }

            // Nothing fancy, so don't catch
            return false;
        }
    }
}
