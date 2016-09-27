// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.EditAndContinue;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    // NOTE: Microsoft.VisualStudio.LanguageServices.TypeScript.TypeScriptProject derives from AbstractProject.
    internal abstract partial class AbstractProject : ForegroundThreadAffinitizedObject, IVisualStudioHostProject
    {
        internal static object RuleSetErrorId = new object();
        private readonly object _gate = new object();

        #region Mutable fields accessed from foreground or background threads - need locking for access.
        private readonly List<ProjectReference> _projectReferences = new List<ProjectReference>();
        private readonly List<VisualStudioMetadataReference> _metadataReferences = new List<VisualStudioMetadataReference>();
        private readonly Dictionary<DocumentId, IVisualStudioHostDocument> _documents = new Dictionary<DocumentId, IVisualStudioHostDocument>();
        private readonly Dictionary<string, IVisualStudioHostDocument> _documentMonikers = new Dictionary<string, IVisualStudioHostDocument>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, VisualStudioAnalyzer> _analyzers = new Dictionary<string, VisualStudioAnalyzer>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<DocumentId, IVisualStudioHostDocument> _additionalDocuments = new Dictionary<DocumentId, IVisualStudioHostDocument>();

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

        #endregion

        #region Mutable fields accessed only from the foreground thread - does not need locking for access.
        /// <summary>
        /// When a reference changes on disk we start a delayed task to update the <see cref="Workspace"/>.
        /// It is delayed for two reasons: first, there are often a bunch of change notifications in quick succession
        /// as the file is written.  Second, we often get the first notification while something is still writing the
        /// file, so we're unable to actually load it.  To avoid both of these issues, we wait five seconds before
        /// reloading the metadata.  This <see cref="Dictionary{TKey, TValue}"/> holds on to
        /// <see cref="CancellationTokenSource"/>s that allow us to cancel the existing reload task if another file
        /// change comes in before we process it.
        /// </summary>
        private readonly Dictionary<VisualStudioMetadataReference, CancellationTokenSource> _donotAccessDirectlyChangedReferencesPendingUpdate
            = new Dictionary<VisualStudioMetadataReference, CancellationTokenSource>();
        private Dictionary<VisualStudioMetadataReference, CancellationTokenSource> ChangedReferencesPendingUpdate
        {
            get
            {
                AssertIsForeground();
                return _donotAccessDirectlyChangedReferencesPendingUpdate;
            }
        }

        #endregion

        // PERF: Create these event handlers once to be shared amongst all documents (the sender arg identifies which document and project)
        private static readonly EventHandler<bool> s_documentOpenedEventHandler = OnDocumentOpened;
        private static readonly EventHandler<bool> s_documentClosingEventHandler = OnDocumentClosing;
        private static readonly EventHandler s_documentUpdatedOnDiskEventHandler = OnDocumentUpdatedOnDisk;
        private static readonly EventHandler<bool> s_additionalDocumentOpenedEventHandler = OnAdditionalDocumentOpened;
        private static readonly EventHandler<bool> s_additionalDocumentClosingEventHandler = OnAdditionalDocumentClosing;
        private static readonly EventHandler s_additionalDocumentUpdatedOnDiskEventHandler = OnAdditionalDocumentUpdatedOnDisk;

        private readonly DiagnosticDescriptor _errorReadingRulesetRule = new DiagnosticDescriptor(
            id: IDEDiagnosticIds.ErrorReadingRulesetId,
            title: ServicesVSResources.ErrorReadingRuleset,
            messageFormat: ServicesVSResources.Error_reading_ruleset_file_0_1,
            category: FeaturesResources.Roslyn_HostError,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public AbstractProject(
            VisualStudioProjectTracker projectTracker,
            Func<ProjectId, IVsReportExternalErrors> reportExternalErrorCreatorOpt,
            string projectSystemName,
            string projectFilePath,
            IVsHierarchy hierarchy,
            string language,
            Guid projectGuid,
            IServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl visualStudioWorkspaceOpt,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSourceOpt,
            ICommandLineParserService commandLineParserServiceOpt = null)
        {
            Contract.ThrowIfNull(projectSystemName);

            ServiceProvider = serviceProvider;
            Language = language;
            Hierarchy = hierarchy;
            Guid = projectGuid;

            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            ContentTypeRegistryService = componentModel.GetService<IContentTypeRegistryService>();

            this.RunningDocumentTable = (IVsRunningDocumentTable4)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
            this.DisplayName = projectSystemName;
            this.ProjectTracker = projectTracker;

            ProjectSystemName = projectSystemName;
            Workspace = visualStudioWorkspaceOpt;
            CommandLineParserService = commandLineParserServiceOpt;
            HostDiagnosticUpdateSource = hostDiagnosticUpdateSourceOpt;

            UpdateProjectDisplayNameAndFilePath(projectSystemName, projectFilePath);

            if (ProjectFilePath != null)
            {
                Version = VersionStamp.Create(File.GetLastWriteTimeUtc(ProjectFilePath));
            }
            else
            {
                Version = VersionStamp.Create();
            }

            Id = this.ProjectTracker.GetOrCreateProjectIdForPath(ProjectFilePath ?? ProjectSystemName, ProjectSystemName);
            if (reportExternalErrorCreatorOpt != null)
            {
                ExternalErrorReporter = reportExternalErrorCreatorOpt(Id);
            }

            if (visualStudioWorkspaceOpt != null)
            {
                this.EditAndContinueImplOpt = new VsENCRebuildableProjectImpl(this);
                this.MetadataService = visualStudioWorkspaceOpt.Services.GetService<IMetadataService>();
            }

            UpdateAssemblyName();
        }

        internal IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Indicates whether this project is a website type.
        /// </summary>
        public bool IsWebSite { get; protected set; }

        /// <summary>
        /// A full path to the project obj output binary, or null if the project doesn't have an obj output binary.
        /// </summary>
        internal string ObjOutputPath { get; private set; }

        /// <summary>
        /// A full path to the project bin output binary, or null if the project doesn't have an bin output binary.
        /// </summary>
        internal string BinOutputPath { get; private set; }

        public IRuleSetFile RuleSetFile { get; private set; }

        protected VisualStudioProjectTracker ProjectTracker { get; }

        protected IVsRunningDocumentTable4 RunningDocumentTable { get; }

        protected IVsReportExternalErrors ExternalErrorReporter { get; }

        internal HostDiagnosticUpdateSource HostDiagnosticUpdateSource { get; }

        public ProjectId Id { get; }

        public string Language { get; }

        private ICommandLineParserService CommandLineParserService { get; }

        /// <summary>
        /// The <see cref="IVsHierarchy"/> for this project.  NOTE: May be null in Deferred Project Load cases.
        /// </summary>
        public IVsHierarchy Hierarchy { get; }

        /// <summary>
        /// Guid of the project
        /// 
        /// it is not readonly since it can be changed while loading project
        /// </summary>
        public Guid Guid { get; protected set; }

        public Workspace Workspace { get; }

        public VersionStamp Version { get; }

        public IMetadataService MetadataService { get; }

        /// <summary>
        /// The containing directory of the project. Null if none exists (consider Venus.)
        /// </summary>
        protected string ContainingDirectoryPathOpt
        {
            get
            {
                var projectFilePath = this.ProjectFilePath;
                if (projectFilePath != null)
                {
                    return Path.GetDirectoryName(projectFilePath);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The full path of the project file. Null if none exists (consider Venus.)
        /// Note that the project file path might change with project file rename.
        /// If you need the folder of the project, just use <see cref="ContainingDirectoryPathOpt" /> which doesn't change for a project.
        /// </summary>
        public string ProjectFilePath { get; private set; }

        /// <summary>
        /// The public display name of the project. This name is not unique and may be shared
        /// between multiple projects, especially in cases like Venus where the intellisense
        /// projects will match the name of their logical parent project.
        /// </summary>
        public string DisplayName { get; private set; }

        internal string AssemblyName { get; private set; }

        /// <summary>
        /// The name of the project according to the project system. In "regular" projects this is
        /// equivalent to <see cref="DisplayName"/>, but in Venus cases these will differ. The
        /// ProjectSystemName is the 2_Default.aspx project name, whereas the regular display name
        /// matches the display name of the project the user actually sees in the solution explorer.
        /// These can be assumed to be unique within the Visual Studio workspace.
        /// </summary>
        public string ProjectSystemName { get; }

        protected DocumentProvider DocumentProvider => this.ProjectTracker.DocumentProvider;

        protected VisualStudioMetadataReferenceManager MetadataReferenceProvider => this.ProjectTracker.MetadataReferenceProvider;

        protected IContentTypeRegistryService ContentTypeRegistryService { get; }

        /// <summary>
        /// Flag indicating if the latest design time build has succeeded for current project state.
        /// </summary>
        protected abstract bool LastDesignTimeBuildSucceeded { get; }

        internal VsENCRebuildableProjectImpl EditAndContinueImplOpt { get; private set; }

        /// <summary>
        /// Override this method to validate references when creating <see cref="ProjectInfo"/> for current state.
        /// By default, this method does nothing.
        /// </summary>
        protected virtual void ValidateReferences()
        {
        }

        public ProjectInfo CreateProjectInfoForCurrentState()
        {
            ValidateReferences();

            lock (_gate)
            {
                var info = ProjectInfo.Create(
                    this.Id,
                    this.Version,
                    this.DisplayName,
                    this.AssemblyName ?? this.ProjectSystemName,
                    this.Language,
                    filePath: this.ProjectFilePath,
                    outputFilePath: this.ObjOutputPath,
                    compilationOptions: this.CurrentCompilationOptions,
                    parseOptions: this.CurrentParseOptions,
                    documents: _documents.Values.Select(d => d.GetInitialState()),
                    metadataReferences: _metadataReferences.Select(r => r.CurrentSnapshot),
                    projectReferences: _projectReferences,
                    analyzerReferences: _analyzers.Values.Select(a => a.GetReference()),
                    additionalDocuments: _additionalDocuments.Values.Select(d => d.GetInitialState()));

                return info.WithHasAllInformation(hasAllInformation: this.LastDesignTimeBuildSucceeded);
            }
        }

        protected ImmutableArray<string> GetStrongNameKeyPaths()
        {
            var outputPath = this.ObjOutputPath;

            if (this.ContainingDirectoryPathOpt == null && outputPath == null)
            {
                return ImmutableArray<string>.Empty;
            }

            var builder = ArrayBuilder<string>.GetInstance();
            if (this.ContainingDirectoryPathOpt != null)
            {
                builder.Add(this.ContainingDirectoryPathOpt);
            }

            if (outputPath != null)
            {
                builder.Add(Path.GetDirectoryName(outputPath));
            }

            return builder.ToImmutableAndFree();
        }

        public ImmutableArray<ProjectReference> GetCurrentProjectReferences()
        {
            lock (_gate)
            {
                return ImmutableArray.CreateRange(_projectReferences);
            }
        }

        public ImmutableArray<VisualStudioMetadataReference> GetCurrentMetadataReferences()
        {
            lock (_gate)
            {
                return ImmutableArray.CreateRange(_metadataReferences);
            }
        }

        public ImmutableArray<VisualStudioAnalyzer> GetCurrentAnalyzers()
        {
            lock (_gate)
            {
                return ImmutableArray.CreateRange(_analyzers.Values);
            }
        }

        public IVisualStudioHostDocument GetDocumentOrAdditionalDocument(DocumentId id)
        {
            IVisualStudioHostDocument doc;
            lock (_gate)
            {
                _documents.TryGetValue(id, out doc);
                if (doc == null)
                {
                    _additionalDocuments.TryGetValue(id, out doc);
                }

                return doc;
            }
        }

        public ImmutableArray<IVisualStudioHostDocument> GetCurrentDocuments()
        {
            lock (_gate)
            {
                return _documents.Values.ToImmutableArrayOrEmpty();
            }
        }

        public ImmutableArray<IVisualStudioHostDocument> GetCurrentAdditionalDocuments()
        {
            lock (_gate)
            {
                return _additionalDocuments.Values.ToImmutableArrayOrEmpty();
            }
        }

        public bool ContainsFile(string moniker)
        {
            lock (_gate)
            {
                return _documentMonikers.ContainsKey(moniker);
            }
        }

        public IVisualStudioHostDocument GetCurrentDocumentFromPath(string filePath)
        {
            lock (_gate)
            {
                IVisualStudioHostDocument document;
                _documentMonikers.TryGetValue(filePath, out document);
                return document;
            }
        }

        public bool HasMetadataReference(string filename)
        {
            lock (_gate)
            {
                return _metadataReferences.Any(r => StringComparer.OrdinalIgnoreCase.Equals(r.FilePath, filename));
            }
        }

        public VisualStudioMetadataReference TryGetCurrentMetadataReference(string filename)
        {
            // We must normalize the file path, since the paths we're comparing to are always normalized
            filename = FileUtilities.NormalizeAbsolutePath(filename);

            lock (_gate)
            {
                return _metadataReferences.SingleOrDefault(r => StringComparer.OrdinalIgnoreCase.Equals(r.FilePath, filename));
            }
        }

        private void AddMetadataFileNameToConvertedProjectReference(string filePath, ProjectReference projectReference)
        {
            lock (_gate)
            {
                _metadataFileNameToConvertedProjectReference.Add(filePath, projectReference);
            }
        }

        private void UpdateMetadataFileNameToConvertedProjectReference(string filePath, ProjectReference projectReference)
        {
            lock (_gate)
            {
                _metadataFileNameToConvertedProjectReference[filePath] = projectReference;
            }
        }

        private bool RemoveMetadataFileNameToConvertedProjectReference(string filePath)
        {
            lock (_gate)
            {
                return _metadataFileNameToConvertedProjectReference.Remove(filePath);
            }
        }

        private bool TryGetMetadataFileNameToConvertedProjectReference(string filePath, out ProjectReference projectReference)
        {
            lock (_gate)
            {
                return _metadataFileNameToConvertedProjectReference.TryGetValue(filePath, out projectReference);
            }
        }

        private bool HasMetadataFileNameToConvertedProjectReference(string filePath)
        {
            lock (_gate)
            {
                return _metadataFileNameToConvertedProjectReference.ContainsKey(filePath);
            }
        }

        public bool CurrentProjectReferencesContains(ProjectId projectId)
        {
            lock (_gate)
            {
                return _projectReferences.Any(r => r.ProjectId == projectId);
            }
        }

        private bool TryGetAnalyzer(string analyzerAssemblyFullPath, out VisualStudioAnalyzer analyzer)
        {
            lock (_gate)
            {
                return _analyzers.TryGetValue(analyzerAssemblyFullPath, out analyzer);
            }
        }

        private void AddOrUpdateAnalyzer(string analyzerAssemblyFullPath, VisualStudioAnalyzer analyzer)
        {
            lock (_gate)
            {
                _analyzers[analyzerAssemblyFullPath] = analyzer;
            }
        }

        private void RemoveAnalyzer(string analyzerAssemblyFullPath)
        {
            lock (_gate)
            {
                _analyzers.Remove(analyzerAssemblyFullPath);
            }
        }

        public bool CurrentProjectAnalyzersContains(string fullPath)
        {
            lock (_gate)
            {
                return _analyzers.ContainsKey(fullPath);
            }
        }

        /// <summary>
        /// Returns a map from full path to <see cref="VisualStudioAnalyzer"/>.
        /// </summary>
        public ImmutableDictionary<string, VisualStudioAnalyzer> GetProjectAnalyzersMap()
        {
            lock (_gate)
            {
                return _analyzers.ToImmutableDictionary();
            }
        }

        private static string GetAssemblyNameFromPath(string outputPath)
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

        protected bool CanConvertToProjectReferences
        {
            get
            {
                if (this.Workspace != null)
                {
                    return this.Workspace.Options.GetOption(InternalFeatureOnOffOptions.ProjectReferenceConversion);
                }
                else
                {
                    return InternalFeatureOnOffOptions.ProjectReferenceConversion.DefaultValue;
                }
            }
        }

        protected int AddMetadataReferenceAndTryConvertingToProjectReferenceIfPossible(string filePath, MetadataReferenceProperties properties)
        {
            // If this file is coming from a project, then we should convert it to a project reference instead
            AbstractProject project;
            if (this.CanConvertToProjectReferences && ProjectTracker.TryGetProjectByBinPath(filePath, out project))
            {
                var projectReference = new ProjectReference(project.Id, properties.Aliases, properties.EmbedInteropTypes);
                if (CanAddProjectReference(projectReference))
                {
                    AddProjectReference(projectReference);
                    AddMetadataFileNameToConvertedProjectReference(filePath, projectReference);
                    return VSConstants.S_OK;
                }
            }

            // regardless whether the file exists or not, we still record it. one of reason 
            // we do that is some cross language p2p references might be resolved
            // after they are already reported as metadata references. since we use bin path 
            // as a way to discover them, if we don't previously record the reference ourselves, 
            // cross p2p references won't be resolved as p2p references when we finally have 
            // all required information.
            //
            // it looks like 
            //    1. project system sometimes won't guarantee build dependency for intellisense build 
            //       if it is cross language dependency
            //    2. output path of referenced cross language project might be changed to right one 
            //       once it is already added as a metadata reference.
            //
            // but this has one consequence. even if a user adds a project in the solution as 
            // a metadata reference explicitly, that dll will be automatically converted back to p2p 
            // reference.
            // 
            // unfortunately there is no way to prevent this using information we have since, 
            // at this point, we don't know whether it is a metadata reference added because 
            // we don't have enough information yet for p2p reference or user explicitly added it 
            // as a metadata reference.
            AddMetadataReferenceCore(this.MetadataReferenceProvider.CreateMetadataReference(this, filePath, properties));

            // here, we change behavior compared to old C# language service. regardless of file being exist or not, 
            // we will always return S_OK. this is to support cross language p2p reference better. 
            // 
            // this should make project system to cache all cross language p2p references regardless 
            // whether it actually exist in disk or not. 
            // (see Roslyn bug 7315 for history - http://vstfdevdiv:8080/DevDiv_Projects/Roslyn/_workitems?_a=edit&id=7315)
            //
            // after this point, Roslyn will take care of non-exist metadata reference.
            //
            // But, this doesn't sovle the issue where actual metadata reference 
            // (not cross language p2p reference) is missing at the time project is opened.
            //
            // in that case, msbuild filter those actual metadata references out, so project system doesn't know 
            // path to the reference. since it doesn't know where dll is, it can't (or currently doesn't) 
            // setup file change notification either to find out when dll becomes available. 
            //
            // at this point, user has 2 ways to recover missing metadata reference once it becomes available.
            //
            // one way is explicitly clicking that missing reference from solution explorer reference node.
            // the other is building the project. at that point, project system will refresh references 
            // which will discover new dll and connect to us. once it is connected, we will take care of it.
            return VSConstants.S_OK;
        }

        protected void RemoveMetadataReference(string filePath)
        {
            // Is this a reference we converted to a project reference?
            ProjectReference projectReference;
            if (TryGetMetadataFileNameToConvertedProjectReference(filePath, out projectReference))
            {
                // We converted this, so remove the project reference instead
                RemoveProjectReference(projectReference);

                Contract.ThrowIfFalse(RemoveMetadataFileNameToConvertedProjectReference(filePath));
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
            lock (_gate)
            {
                _metadataReferences.Add(reference);
            }

            if (_pushingChangesToWorkspaceHosts)
            {
                var snapshot = reference.CurrentSnapshot;
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnMetadataReferenceAdded(this.Id, snapshot));
            }

            reference.UpdatedOnDisk += OnImportChanged;
        }

        private void RemoveMetadataReferenceCore(VisualStudioMetadataReference reference, bool disposeReference)
        {
            lock (_gate)
            {
                _metadataReferences.Remove(reference);
            }

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
            AssertIsForeground();

            VisualStudioMetadataReference reference = (VisualStudioMetadataReference)sender;

            CancellationTokenSource delayTaskCancellationTokenSource;
            if (ChangedReferencesPendingUpdate.TryGetValue(reference, out delayTaskCancellationTokenSource))
            {
                delayTaskCancellationTokenSource.Cancel();
            }

            delayTaskCancellationTokenSource = new CancellationTokenSource();
            ChangedReferencesPendingUpdate[reference] = delayTaskCancellationTokenSource;

            var task = Task.Delay(TimeSpan.FromSeconds(5), delayTaskCancellationTokenSource.Token)
                .ContinueWith(
                    OnImportChangedAfterDelay,
                    reference,
                    delayTaskCancellationTokenSource.Token,
                    TaskContinuationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void OnImportChangedAfterDelay(Task previous, object state)
        {
            AssertIsForeground();

            var reference = (VisualStudioMetadataReference)state;
            ChangedReferencesPendingUpdate.Remove(reference);

            lock (_gate)
            {
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
        }

        private void OnAnalyzerChanged(object sender, EventArgs e)
        {
            // Postpone handler's actions to prevent deadlock. This AnalyzeChanged event can
            // be invoked while the FileChangeService lock is held, and VisualStudioAnalyzer's 
            // efforts to listen to file changes can lead to a deadlock situation.
            // Postponing the VisualStudioAnalyzer operations gives this thread the opportunity
            // to release the lock.
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                VisualStudioAnalyzer analyzer = (VisualStudioAnalyzer)sender;

                RemoveAnalyzerReference(analyzer.FullPath);
                AddAnalyzerReference(analyzer.FullPath);
            }));
        }

        // Internal for unit testing
        internal void AddProjectReference(ProjectReference projectReference)
        {
            // dev11 is sometimes calling us multiple times for the same data
            if (!CanAddProjectReference(projectReference))
            {
                return;
            }

            lock (_gate)
            {
                // always manipulate current state after workspace is told so it will correctly observe the initial state
                _projectReferences.Add(projectReference);
            }

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

            lock (_gate)
            {
                if (_projectReferences.Contains(projectReference))
                {
                    // already have this reference
                    return false;
                }
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

            foreach (var pr in GetCurrentProjectReferences())
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
            lock (_gate)
            {
                Contract.ThrowIfFalse(_projectReferences.Remove(projectReference));
            }

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

        protected void AddFile(
            string filename,
            SourceCodeKind sourceCodeKind,
            Func<IVisualStudioHostDocument, bool> getIsCurrentContext,
            Func<uint, IReadOnlyList<string>> getFolderNames)
        {
            // We can currently be on a background thread.
            // So, hookup the handlers when creating the standard text document, as we might receive these handler notifications on the UI thread.
            var document = this.DocumentProvider.TryGetDocumentForFile(
                this,
                filePath: filename,
                sourceCodeKind: sourceCodeKind,
                getFolderNames: getFolderNames,
                canUseTextBuffer: CanUseTextBuffer,
                updatedOnDiskHandler: s_documentUpdatedOnDiskEventHandler,
                openedHandler: s_documentOpenedEventHandler,
                closingHandler: s_documentClosingEventHandler);

            if (document == null)
            {
                // It's possible this file is open in some very strange editor. In that case, we'll just ignore it.
                // This might happen if somebody decides to mark a non-source-file as something to compile.

                // TODO: Venus does this for .aspx/.cshtml files which is completely unnecessary for Roslyn. We should remove that code.
                AddUntrackedFile(filename);
                return;
            }

            AddDocument(document, getIsCurrentContext(document), hookupHandlers: false);
        }

        protected virtual bool CanUseTextBuffer(ITextBuffer textBuffer)
        {
            return true;
        }

        protected void AddUntrackedFile(string filename)
        {
            lock (_gate)
            {
                _untrackedDocuments.Add(filename);
            }
        }

        protected void RemoveFile(string filename)
        {
            lock (_gate)
            {
                // Remove this as an untracked file, if it is
                if (_untrackedDocuments.Remove(filename))
                {
                    return;
                }
            }

            IVisualStudioHostDocument document = this.GetCurrentDocumentFromPath(filename);
            if (document == null)
            {
                throw new InvalidOperationException("The document is not a part of the finalProject.");
            }

            RemoveDocument(document);
        }

        internal void AddDocument(IVisualStudioHostDocument document, bool isCurrentContext, bool hookupHandlers)
        {
            // We do not want to allow message pumping/reentrancy when processing project system changes.
            using (Dispatcher.CurrentDispatcher.DisableProcessing())
            {
                lock (_gate)
                {
                    _documents.Add(document.Id, document);
                    _documentMonikers.Add(document.Key.Moniker, document);
                }

                if (_pushingChangesToWorkspaceHosts)
                {
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnDocumentAdded(document.GetInitialState()));

                    if (document.IsOpen)
                    {
                        this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnDocumentOpened(document.Id, document.GetOpenTextBuffer(), isCurrentContext));
                    }
                }

                if (hookupHandlers)
                {
                    document.Opened += s_documentOpenedEventHandler;
                    document.Closing += s_documentClosingEventHandler;
                    document.UpdatedOnDisk += s_documentUpdatedOnDiskEventHandler;
                }

                DocumentProvider.NotifyDocumentRegisteredToProjectAndStartToRaiseEvents(document);

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
                lock (_gate)
                {
                    _documents.Remove(document.Id);
                    _documentMonikers.Remove(document.Key.Moniker);
                }

                UninitializeDocument(document);
                OnDocumentRemoved(document.Key.Moniker);
            }
        }

        internal void AddAdditionalDocument(IVisualStudioHostDocument document, bool isCurrentContext)
        {
            lock (_gate)
            {
                _additionalDocuments.Add(document.Id, document);
                _documentMonikers.Add(document.Key.Moniker, document);
            }

            if (_pushingChangesToWorkspaceHosts)
            {
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAdditionalDocumentAdded(document.GetInitialState()));

                if (document.IsOpen)
                {
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAdditionalDocumentOpened(document.Id, document.GetOpenTextBuffer(), isCurrentContext));
                }
            }

            DocumentProvider.NotifyDocumentRegisteredToProjectAndStartToRaiseEvents(document);

            if (!_pushingChangesToWorkspaceHosts && document.IsOpen)
            {
                StartPushingToWorkspaceAndNotifyOfOpenDocuments();
            }
        }

        internal void RemoveAdditionalDocument(IVisualStudioHostDocument document)
        {
            lock (_gate)
            {
                _additionalDocuments.Remove(document.Id);
                _documentMonikers.Remove(document.Key.Moniker);
            }

            UninitializeAdditionalDocument(document);
        }

        public virtual void Disconnect()
        {
            AssertIsForeground();

            using (Workspace?.Services.GetService<IGlobalOperationNotificationService>()?.Start("Disconnect Project"))
            {
                lock (_gate)
                {
                    // No sense in reloading any metadata references anymore.
                    foreach (var cancellationTokenSource in ChangedReferencesPendingUpdate.Values)
                    {
                        cancellationTokenSource.Cancel();
                    }

                    ChangedReferencesPendingUpdate.Clear();

                    var wasPushing = _pushingChangesToWorkspaceHosts;

                    // disable pushing down to workspaces, so we don't get redundant workspace document removed events
                    _pushingChangesToWorkspaceHosts = false;

                    // The project is going away, so let's remove ourselves from the host. First, we
                    // close and dispose of any remaining documents
                    foreach (var document in _documents.Values)
                    {
                        UninitializeDocument(document);
                    }

                    foreach (var document in _additionalDocuments.Values)
                    {
                        UninitializeAdditionalDocument(document);
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
                    ExternalErrorReporter?.ClearAllErrors();

                    // Make sure we clear out any host errors left when closing the project.
                    HostDiagnosticUpdateSource?.ClearAllDiagnosticsForProject(this.Id);

                    ClearAnalyzerRuleSet();

                    // reinstate pushing down to workspace, so the workspace project remove event fires
                    _pushingChangesToWorkspaceHosts = wasPushing;

                    this.ProjectTracker.RemoveProject(this);

                    _pushingChangesToWorkspaceHosts = false;

                    this.EditAndContinueImplOpt = null;
                }
            }
        }

        internal void TryProjectConversionForIntroducedOutputPath(string binPath, AbstractProject projectToReference)
        {
            if (this.CanConvertToProjectReferences)
            {
                // We should not already have references for this, since we're only introducing the path for the first time
                Contract.ThrowIfTrue(HasMetadataFileNameToConvertedProjectReference(binPath));

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

                        AddMetadataFileNameToConvertedProjectReference(binPath, projectReference);
                    }
                }
            }
        }

        internal void UndoProjectReferenceConversionForDisappearingOutputPath(string binPath)
        {
            ProjectReference projectReference;
            if (TryGetMetadataFileNameToConvertedProjectReference(binPath, out projectReference))
            {
                // We converted this, so convert it back to a metadata reference
                RemoveProjectReference(projectReference);

                var metadataReferenceProperties = new MetadataReferenceProperties(
                    MetadataImageKind.Assembly,
                    projectReference.Aliases,
                    projectReference.EmbedInteropTypes);

                AddMetadataReferenceCore(MetadataReferenceProvider.CreateMetadataReference(this, binPath, metadataReferenceProperties));

                Contract.ThrowIfFalse(RemoveMetadataFileNameToConvertedProjectReference(binPath));
            }
        }

        protected void UpdateMetadataReferenceAliases(string file, ImmutableArray<string> aliases)
        {
            file = FileUtilities.NormalizeAbsolutePath(file);

            // Have we converted these to project references?
            ProjectReference convertedProjectReference;

            if (TryGetMetadataFileNameToConvertedProjectReference(file, out convertedProjectReference))
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
            string referenceBinPath = referencedProject.BinOutputPath;
            if (referenceBinPath != null && HasMetadataFileNameToConvertedProjectReference(referenceBinPath))
            {
                UpdateMetadataFileNameToConvertedProjectReference(referenceBinPath, newProjectReference);
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

            document.Opened -= s_additionalDocumentOpenedEventHandler;
            document.Closing -= s_additionalDocumentClosingEventHandler;
            document.UpdatedOnDisk -= s_additionalDocumentUpdatedOnDiskEventHandler;

            document.Dispose();
        }

        protected virtual void OnDocumentRemoved(string filePath)
        {
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
                var messageArguments = new string[] { ruleSetFile.FilePath, ruleSetFile.GetException().Message };
                DiagnosticData diagnostic;
                if (DiagnosticData.TryCreate(_errorReadingRulesetRule, messageArguments, this.Id, this.Workspace, out diagnostic))
                {
                    this.HostDiagnosticUpdateSource.UpdateDiagnosticsForProject(this.Id, RuleSetErrorId, SpecializedCollections.SingletonEnumerable(diagnostic));
                }
            }
        }

        protected void SetObjOutputPathAndRelatedData(string objOutputPath)
        {
            var currentObjOutputPath = this.ObjOutputPath;
            if (PathUtilities.IsAbsolute(objOutputPath) && !string.Equals(currentObjOutputPath, objOutputPath, StringComparison.OrdinalIgnoreCase))
            {
                // set obj output path
                this.ObjOutputPath = objOutputPath;

                // Workspace/services can be null for tests.
                if (this.MetadataService != null)
                {
                    var newCompilationOptions = CurrentCompilationOptions.WithMetadataReferenceResolver(CreateMetadataReferenceResolver(
                        metadataService: this.MetadataService,
                        projectDirectory: this.ContainingDirectoryPathOpt,
                        outputDirectory: Path.GetDirectoryName(objOutputPath)));
                    SetOptionsCore(newCompilationOptions);
                }

                if (_pushingChangesToWorkspaceHosts)
                {
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnOptionsChanged(this.Id, CurrentCompilationOptions, CurrentParseOptions));
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnOutputFilePathChanged(this.Id, objOutputPath));
                }

                UpdateAssemblyName();
            }
        }

        private void UpdateAssemblyName()
        {
            // set assembly name if changed
            // we use designTimeOutputPath to get assembly name since it is more reliable way to get the assembly name.
            // otherwise, friend assembly all get messed up.
            var newAssemblyName = GetAssemblyNameFromPath(this.ObjOutputPath ?? this.ProjectSystemName);
            if (!string.Equals(AssemblyName, newAssemblyName, StringComparison.Ordinal))
            {
                AssemblyName = newAssemblyName;

                if (_pushingChangesToWorkspaceHosts)
                {
                    this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnAssemblyNameChanged(this.Id, newAssemblyName));
                }
            }
        }

        protected void SetBinOutputPathAndRelatedData(string binOutputPath)
        {
            // refresh final output path
            var currentBinOutputPath = this.BinOutputPath;
            if (binOutputPath != null && !string.Equals(currentBinOutputPath, binOutputPath, StringComparison.OrdinalIgnoreCase))
            {
                this.BinOutputPath = binOutputPath;

                // If the project has been hooked up with the project tracker, then update the bin path with the tracker.
                if (this.ProjectTracker.GetProject(Id) != null)
                {
                    this.ProjectTracker.UpdateProjectBinPath(this, currentBinOutputPath, binOutputPath);
                }
            }
        }

        protected void UpdateProjectDisplayName(string newDisplayName)
        {
            UpdateProjectDisplayNameAndFilePath(newDisplayName, newFilePath: null);
        }

        protected void UpdateProjectFilePath(string newFilePath)
        {
            UpdateProjectDisplayNameAndFilePath(newDisplayName: null, newFilePath: newFilePath);
        }

        protected void UpdateProjectDisplayNameAndFilePath(string newDisplayName, string newFilePath)
        {
            bool updateMade = false;

            if (newDisplayName != null && this.DisplayName != newDisplayName)
            {
                this.DisplayName = newDisplayName;
                updateMade = true;
            }

            if (newFilePath != null && File.Exists(newFilePath) && this.ProjectFilePath != newFilePath)
            {
                Debug.Assert(PathUtilities.IsAbsolute(newFilePath));
                this.ProjectFilePath = newFilePath;
                updateMade = true;
            }

            if (updateMade && _pushingChangesToWorkspaceHosts)
            {
                this.ProjectTracker.NotifyWorkspaceHosts(host => host.OnProjectNameChanged(Id, this.DisplayName, this.ProjectFilePath));
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

#if DEBUG
        public virtual bool Debug_VBEmbeddedCoreOptionOn
        {
            get
            {
                return false;
            }
        }
#endif

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

        #region FolderNames
        private readonly List<string> _tmpFolders = new List<string>();
        private readonly Dictionary<uint, IReadOnlyList<string>> _folderNameMap = new Dictionary<uint, IReadOnlyList<string>>();

        public IReadOnlyList<string> GetFolderNamesFromHierarchy(uint documentItemID)
        {
            object parentObj;
            if (documentItemID != (uint)VSConstants.VSITEMID.Nil && Hierarchy.GetProperty(documentItemID, (int)VsHierarchyPropID.Parent, out parentObj) == VSConstants.S_OK)
            {
                var parentID = UnboxVSItemId(parentObj);
                if (parentID != (uint)VSConstants.VSITEMID.Nil && parentID != (uint)VSConstants.VSITEMID.Root)
                {
                    return GetFolderNamesForFolder(parentID);
                }
            }

            return SpecializedCollections.EmptyReadOnlyList<string>();
        }

        private IReadOnlyList<string> GetFolderNamesForFolder(uint folderItemID)
        {
            // note: use of tmpFolders is assuming this API is called on UI thread only.
            _tmpFolders.Clear();

            IReadOnlyList<string> names;
            if (!_folderNameMap.TryGetValue(folderItemID, out names))
            {
                ComputeFolderNames(folderItemID, _tmpFolders, Hierarchy);
                names = _tmpFolders.ToImmutableArray();
                _folderNameMap.Add(folderItemID, names);
            }
            else
            {
                // verify names, and change map if we get a different set.
                // this is necessary because we only get document adds/removes from the project system
                // when a document name or folder name changes.
                ComputeFolderNames(folderItemID, _tmpFolders, Hierarchy);
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
        private static uint UnboxVSItemId(object id)
        {
            return id is uint ? (uint)id : unchecked((uint)(int)id);
        }

        private static void ComputeFolderNames(uint folderItemID, List<string> names, IVsHierarchy hierarchy)
        {
            object nameObj;
            if (hierarchy.GetProperty((uint)folderItemID, (int)VsHierarchyPropID.Name, out nameObj) == VSConstants.S_OK)
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
            if (hierarchy.GetProperty((uint)folderItemID, (int)VsHierarchyPropID.Parent, out parentObj) == VSConstants.S_OK)
            {
                var parentID = UnboxVSItemId(parentObj);
                if (parentID != (uint)VSConstants.VSITEMID.Nil && parentID != (uint)VSConstants.VSITEMID.Root)
                {
                    ComputeFolderNames(parentID, names, hierarchy);
                }
            }
        }
        #endregion
    }
}
