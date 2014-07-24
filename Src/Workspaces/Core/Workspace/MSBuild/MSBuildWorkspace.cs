// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// A workspace that can be populated by opening MSBuild solution and project files.
    /// </summary>
    public sealed class MSBuildWorkspace : Workspace
    {
        // used to serialize access to public methods
        private readonly NonReentrantLock serializationLock = new NonReentrantLock();

        // used to protect access to mutable state
        private readonly NonReentrantLock dataGuard = new NonReentrantLock();

        private readonly Dictionary<string, string> extensionToLanguageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ProjectId> projectPathToProjectIdMap = new Dictionary<string, ProjectId>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IProjectFileLoader> projectPathToLoaderMap = new Dictionary<string, IProjectFileLoader>(StringComparer.OrdinalIgnoreCase);

        private string solutionFilePath;
        private ImmutableDictionary<string, string> properties;

        private MSBuildWorkspace(
            HostServices hostServices,
            ImmutableDictionary<string, string> properties)
            : base(hostServices, "MSBuildWorkspace")
        {
            // always make a copy of these build properties (no mutation please!)
            this.properties = properties ?? ImmutableDictionary<string, string>.Empty;
            this.SetSolutionProperties(solutionFilePath: null);
            this.LoadMetadataForReferencedProjects = false;
            this.SkipUnrecognizedProjects = true;
        }

        /// <summary>
        /// Create a new instance of a workspace that can be populated by opening solution and project files.
        /// </summary>
        public static MSBuildWorkspace Create()
        {
            return Create(ImmutableDictionary<string, string>.Empty);
        }

        /// <summary>
        /// Create a new instance of a workspace that can be populated by opening solution and project files.
        /// </summary>
        /// <param name="properties">An optional set of MSBuild properties used when interpretting project files.
        /// These are the same properties that are passed to msbuild via the /property:&lt;n&gt;=&lt;v&gt; command line argument.</param>
        public static MSBuildWorkspace Create(IDictionary<string, string> properties)
        {
            return Create(properties, Host.Mef.MefHostServices.DefaultHost);
        }

        /// <summary>
        /// Create a new instance of a workspace that can be populated by opening solution and project files.
        /// </summary>
        /// <param name="properties">The MSBuild properties used when interpretting project files.
        /// These are the same properties that are passed to msbuild via the /property:&lt;n&gt;=&lt;v&gt; command line argument.</param>
        /// <param name="hostServices">The <see cref="HostServices"/> used to configure this workspace.</param>
        public static MSBuildWorkspace Create(IDictionary<string, string> properties, HostServices hostServices)
        {
            if (properties == null)
            {
                throw new ArgumentNullException("properties");
            }

            if (hostServices == null)
            {
                throw new ArgumentNullException("hostServices");
            }

            return new MSBuildWorkspace(hostServices, properties.ToImmutableDictionary());
        }

        /// <summary>
        /// The MSBuild properties used when interpretting project files.
        /// These are the same properties that are passed to msbuild via the /property:&lt;n&gt;=&lt;v&gt; command line argument.
        /// </summary>
        public ImmutableDictionary<string, string> Properties
        {
            get { return this.properties; }
        }

        /// <summary>
        /// Determines if metadata from existing output assemblies is loaded instead of opening referenced projects.
        /// If the referenced project is already opened, the metadata will not be loaded.
        /// If the metadata assembly cannot be found the referenced project will be opened instead.
        /// </summary>
        public bool LoadMetadataForReferencedProjects { get; set; }

        /// <summary>
        /// Determines if unrecognized projects are skipped when solutions or projects are opened.
        /// 
        /// An project is unrecognized if it either has 
        ///   a) an invalid file path, 
        ///   b) a non-existent project file,
        ///   c) has an unrecognized file extension or 
        ///   d) a file extension associated with an unsupported language.
        /// 
        /// If unrecognized projects cannot be skipped a corresponding exception is thrown.
        /// </summary>
        public bool SkipUnrecognizedProjects { get; set; }

        /// <summary>
        /// Associates a project file extension with a language name.
        /// </summary>
        public void AssociateFileExtensionWithLanguage(string projectFileExtension, string language)
        {
            if (language == null)
            {
                throw new ArgumentNullException("language");
            }

            if (projectFileExtension == null)
            {
                throw new ArgumentNullException("projectFileExtension");
            }

            using (this.dataGuard.DisposableWait())
            {
                this.extensionToLanguageMap[projectFileExtension] = language;
            }
        }

        /// <summary>
        /// Close the open solution, and reset the workspace to a new empty solution.
        /// </summary>
        public void CloseSolution()
        {
            using (this.serializationLock.DisposableWait())
            {
                this.ClearSolution();
            }
        }

        protected override void ClearSolutionData()
        {
            base.ClearSolutionData();

            using (this.dataGuard.DisposableWait())
            {
                this.SetSolutionProperties(solutionFilePath: null);

                // clear project related data
                this.projectPathToProjectIdMap.Clear();
                this.projectPathToLoaderMap.Clear();
            }
        }

        private const string SolutionDirProperty = "SolutionDir";

        private void SetSolutionProperties(string solutionFilePath)
        {
            this.solutionFilePath = solutionFilePath;

            if (!string.IsNullOrEmpty(solutionFilePath))
            {
                // When MSBuild is building an individual project, it doesn't define $(SolutionDir).
                // However when building an .sln file, or when working inside Visual Studio,
                // $(SolutionDir) is defined to be the directory where the .sln file is located.
                // Some projects out there rely on $(SolutionDir) being set (although the best practice is to
                // use MSBuildProjectDirectory which is always defined).
                if (!string.IsNullOrEmpty(solutionFilePath))
                {
                    string solutionDirectory = Path.GetDirectoryName(solutionFilePath);
                    if (!solutionDirectory.EndsWith("\\"))
                    {
                        solutionDirectory += "\\";
                    }

                    if (Directory.Exists(solutionDirectory))
                    {
                        this.properties = this.properties.SetItem(SolutionDirProperty, solutionDirectory);
                    }
                }
            }
        }

        private ProjectId GetProjectId(string fullProjectPath)
        {
            using (this.dataGuard.DisposableWait())
            {
                ProjectId id;
                this.projectPathToProjectIdMap.TryGetValue(fullProjectPath, out id);
                return id;
            }
        }

        private ProjectId GetOrCreateProjectId(string fullProjectPath)
        {
            using (this.dataGuard.DisposableWait())
            {
                ProjectId id;
                if (!this.projectPathToProjectIdMap.TryGetValue(fullProjectPath, out id))
                {
                    id = ProjectId.CreateNewId(debugName: fullProjectPath);
                    this.projectPathToProjectIdMap.Add(fullProjectPath, id);
                }

                return id;
            }
        }

        private bool TryGetLoaderFromProjectPath(string projectFilePath, ReportMode mode, out IProjectFileLoader loader)
        {
            using (this.dataGuard.DisposableWait())
            {
                // check to see if we already know the loader
                if (!this.projectPathToLoaderMap.TryGetValue(projectFilePath, out loader))
                {
                    // otherwise try to figure it out from extension
                    var extension = Path.GetExtension(projectFilePath);
                    if (extension.StartsWith("."))
                    {
                        extension = extension.Substring(1);
                    }

                    string language;
                    if (this.extensionToLanguageMap.TryGetValue(extension, out language))
                    {
                        if (this.Services.SupportedLanguages.Contains(language))
                        {
                            loader = this.Services.GetLanguageServices(language).GetService<IProjectFileLoader>();
                        }
                        else
                        {
                            this.ReportFailure(mode, string.Format(WorkspacesResources.CannotOpenProjectUnsupportedLanguage, projectFilePath, language));
                            return false;
                        }
                    }
                    else 
                    {
                        loader = ProjectFileLoader.GetLoaderForProjectFileExtension(this, extension);

                        if (loader == null)
                        {
                            this.ReportFailure(mode, string.Format(WorkspacesResources.CannotOpenProjectUnrecognizedFileExtension, projectFilePath, Path.GetExtension(projectFilePath)));
                            return false;
                        }
                    }

                    if (loader != null)
                    {
                        this.projectPathToLoaderMap[projectFilePath] = loader;
                    }
                }

                return loader != null;
            }
        }

        private bool TryGetAbsoluteProjectPath(string path, string baseDirectory, ReportMode mode, out string absolutePath)
        {
            try
            {
                absolutePath = this.GetAbsolutePath(path, baseDirectory);
            }
            catch (Exception)
            {
                ReportFailure(mode, string.Format(WorkspacesResources.InvalidProjectFilePath, path));
                absolutePath = null;
                return false;
            }

            if (!File.Exists(absolutePath))
            {
                ReportFailure(
                    mode,
                    string.Format(WorkspacesResources.ProjectFileNotFound, absolutePath),
                    msg => new FileNotFoundException(msg));
                return false;
            }

            return true;
        }

        private string GetAbsoluteSolutionPath(string path, string baseDirectory)
        {
            string absolutePath;

            try
            {
                absolutePath = GetAbsolutePath(path, baseDirectory);
            }
            catch (Exception)
            {
                throw new InvalidOperationException(string.Format(WorkspacesResources.InvalidSolutionFilePath, path));
            }

            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(string.Format(WorkspacesResources.SolutionFileNotFound, absolutePath));
            }

            return absolutePath;
        }

        private enum ReportMode
        {
            Throw,
            Log,
            Ignore
        }

        private void ReportFailure(ReportMode mode, string message, Func<string, Exception> createException = null)
        {
            switch (mode)
            {
                case ReportMode.Throw:
                    if (createException != null)
                    {
                        throw createException(message);
                    }
                    else
                    {
                        throw new InvalidOperationException(message);
                    }

                case ReportMode.Log:
                    this.OnWorkspaceFailed(new WorkspaceDiagnostic(WorkspaceDiagnosticKind.Failure, message));
                    break;

                case ReportMode.Ignore:
                default:
                    break;
            }
        }

        private string GetAbsolutePath(string path, string baseDirectoryPath)
        {
            return Path.GetFullPath(FileUtilities.ResolveRelativePath(path, baseDirectoryPath) ?? path);
        }

        #region Open Solution & Project
        /// <summary>
        /// Open a solution file and all referenced projects.
        /// </summary>
        public async Task<Solution> OpenSolutionAsync(string solutionFilePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (solutionFilePath == null)
            {
                throw new ArgumentNullException("solutionFilePath");
            }

            this.ClearSolution();

            var absoluteSolutionPath = this.GetAbsoluteSolutionPath(solutionFilePath, Environment.CurrentDirectory);

            using (this.dataGuard.DisposableWait())
            {
                this.SetSolutionProperties(absoluteSolutionPath);
            }

            VersionStamp version = default(VersionStamp);
            SolutionFile solutionFile = null;

            using (var reader = new StreamReader(absoluteSolutionPath))
            {
                version = VersionStamp.Create(File.GetLastWriteTimeUtc(absoluteSolutionPath));
                var text = await reader.ReadToEndAsync().ConfigureAwait(false);
                solutionFile = SolutionFile.Parse(new StringReader(text));
            }

            var solutionFolder = Path.GetDirectoryName(absoluteSolutionPath);

            // seed loaders from known project types
            using (this.dataGuard.DisposableWait())
            {
                foreach (var projectBlock in solutionFile.ProjectBlocks)
                {
                    string absoluteProjectPath;
                    if (TryGetAbsoluteProjectPath(projectBlock.ProjectPath, solutionFolder, ReportMode.Ignore, out absoluteProjectPath))
                    {
                        var loader = ProjectFileLoader.GetLoaderForProjectTypeGuid(this, projectBlock.ProjectTypeGuid);
                        if (loader != null)
                        {
                            this.projectPathToLoaderMap[absoluteProjectPath] = loader;
                        }
                    }
                }
            }

            // a list to accumulate all the loaded projects
            var loadedProjects = new List<ProjectInfo>();

            var reportMode = this.SkipUnrecognizedProjects ? ReportMode.Log : ReportMode.Throw;

            // load all the projects
            foreach (var projectBlock in solutionFile.ProjectBlocks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string absoluteProjectPath;
                if (TryGetAbsoluteProjectPath(projectBlock.ProjectPath, solutionFolder, reportMode, out absoluteProjectPath))
                {
                    IProjectFileLoader loader;
                    if (TryGetLoaderFromProjectPath(absoluteProjectPath, reportMode, out loader))
                    { 
                        // projects get added to 'loadedProjects' as side-effect
                        // never perfer metadata when loading solution, all projects get loaded if they can.
                        var tmp = await GetOrLoadProjectAsync(absoluteProjectPath, loader, preferMetadata: false, loadedProjects: loadedProjects, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            // construct workspace from loaded project infos
            this.OnSolutionAdded(SolutionInfo.Create(SolutionId.CreateNewId(debugName: absoluteSolutionPath), version, absoluteSolutionPath, loadedProjects));

            this.UpdateReferencesAfterAdd();

            return this.CurrentSolution;
        }

        /// <summary>
        /// Open a project file and all referenced projects.
        /// </summary>
        public async Task<Project> OpenProjectAsync(string projectFilePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException("projectFilePath");
            }

            string fullPath;
            if (this.TryGetAbsoluteProjectPath(projectFilePath, Environment.CurrentDirectory, ReportMode.Throw, out fullPath))
            {
                IProjectFileLoader loader;
                if (this.TryGetLoaderFromProjectPath(projectFilePath, ReportMode.Throw, out loader))
                {
                    var loadedProjects = new List<ProjectInfo>();
                    var projectId = await GetOrLoadProjectAsync(fullPath, loader, this.LoadMetadataForReferencedProjects, loadedProjects, cancellationToken).ConfigureAwait(false);

                    // add projects to solution
                    foreach (var project in loadedProjects)
                    {
                        this.OnProjectAdded(project);
                    }

                    this.UpdateReferencesAfterAdd();

                    return this.CurrentSolution.GetProject(projectId);
                }
            }

            // unreachable
            return null;
        }

        private void UpdateReferencesAfterAdd()
        {
            using (this.serializationLock.DisposableWait())
            {
                var oldSolution = this.CurrentSolution;
                var newSolution = this.UpdateReferencesAfterAdd(oldSolution);

                if (newSolution != oldSolution)
                {
                    newSolution = this.SetCurrentSolution(newSolution);
                    var ignore = this.RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind.SolutionChanged, oldSolution, newSolution);
                }
            }
        }

        // Updates all projects to properly reference other existing projects via project references instead of using references to built metadata.
        private Solution UpdateReferencesAfterAdd(Solution solution)
        {
            // Build map from output assembly path to ProjectId
            // Use explicit loop instead of ToDictionary so we don't throw if multiple projects have same output assembly path.
            var outputAssemblyToProjectIdMap = new Dictionary<string, ProjectId>();
            foreach (var p in solution.Projects)
            {
                if (!string.IsNullOrEmpty(p.OutputFilePath))
                {
                    outputAssemblyToProjectIdMap[p.OutputFilePath] = p.Id;
                }
            }

            // now fix each project if necessary
            foreach (var pid in solution.ProjectIds)
            {
                var project = solution.GetProject(pid);

                // convert metadata references to project references if the metadata reference matches some project's output assembly.
                foreach (var meta in project.MetadataReferences)
                {
                    var pemeta = meta as PortableExecutableReference;
                    if (pemeta != null)
                    {
                        ProjectId matchingProjectId;

                        // check both Display and FilePath. FilePath points to the actually bits, but Display should match output path if 
                        // the metadata reference is shadow copied.
                        if ((!string.IsNullOrEmpty(pemeta.Display) && outputAssemblyToProjectIdMap.TryGetValue(pemeta.Display, out matchingProjectId)) ||
                            (!string.IsNullOrEmpty(pemeta.FilePath) && outputAssemblyToProjectIdMap.TryGetValue(pemeta.FilePath, out matchingProjectId)))
                        {
                            var newProjRef = new ProjectReference(matchingProjectId, pemeta.Properties.Aliases, pemeta.Properties.EmbedInteropTypes);

                            if (!project.ProjectReferences.Contains(newProjRef))
                            {
                                project = project.WithProjectReferences(project.ProjectReferences.Concat(newProjRef));
                            }

                            project = project.WithMetadataReferences(project.MetadataReferences.Where(mr => mr != meta));
                        }
                    }
                }

                solution = project.Solution;
            }

            return solution;
        }

        private async Task<ProjectId> GetOrLoadProjectAsync(string projectFilePath, IProjectFileLoader loader, bool preferMetadata, List<ProjectInfo> loadedProjects, CancellationToken cancellationToken)
        {
            var projectId = GetProjectId(projectFilePath);
            if (projectId == null)
            {
                projectId = await this.LoadProjectAsync(projectFilePath, loader, preferMetadata, loadedProjects, cancellationToken).ConfigureAwait(false);
            }

            return projectId;
        }

        private async Task<ProjectId> LoadProjectAsync(string projectFilePath, IProjectFileLoader loader, bool preferMetadata, List<ProjectInfo> loadedProjects, CancellationToken cancellationToken)
        {
            System.Diagnostics.Debug.Assert(projectFilePath != null);
            System.Diagnostics.Debug.Assert(loader != null);

            var projectId = this.GetOrCreateProjectId(projectFilePath);

            var name = Path.GetFileNameWithoutExtension(projectFilePath);

            var projectFile = await loader.LoadProjectFileAsync(projectFilePath, this.properties, cancellationToken).ConfigureAwait(false);
            var projectFileInfo = await projectFile.GetProjectFileInfoAsync(cancellationToken).ConfigureAwait(false);

            VersionStamp version;
            if (!string.IsNullOrEmpty(projectFilePath) && File.Exists(projectFilePath))
            {
                version = VersionStamp.Create(File.GetLastWriteTimeUtc(projectFilePath));
            }
            else
            {
                version = VersionStamp.Create();
            }

            // Documents
            var docFileInfos = projectFileInfo.Documents.ToImmutableArrayOrEmpty();
            CheckDocuments(docFileInfos, projectFilePath, projectId);

            // TODO: is there a way how to specify encoding to msbuild? csc.exe has /codepage command line option.
            // For now use auto-detection. (bug 941489).
            Encoding defaultEncoding = null;

            var docs = new List<DocumentInfo>();
            foreach (var docFileInfo in docFileInfos)
            {
                docs.Add(DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId, debugName: docFileInfo.FilePath),
                    Path.GetFileName(docFileInfo.LogicalPath),
                    GetDocumentFolders(docFileInfo.LogicalPath),
                    projectFile.GetSourceCodeKind(docFileInfo.FilePath),
                    new FileTextLoader(docFileInfo.FilePath, defaultEncoding),
                    docFileInfo.FilePath,
                    defaultEncoding,
                    docFileInfo.IsGenerated));
            }

            // project references
            var resolvedReferences = await this.ResolveProjectReferencesAsync(
                projectFilePath, projectFileInfo.ProjectReferences, preferMetadata, loadedProjects, cancellationToken).ConfigureAwait(false);

            var metadataReferences = projectFileInfo.MetadataReferences
                .Concat(resolvedReferences.MetadataReferences);

            var outputFilePath = projectFileInfo.OutputFilePath;
            var assemblyName = projectFileInfo.AssemblyName;

            // if the project file loader couldn't figure out an assembly name, make one using the project's file path.
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                assemblyName = Path.GetFileNameWithoutExtension(projectFilePath);

                // if this is still unreasonable, use a fixed name.
                if (string.IsNullOrWhiteSpace(assemblyName))
                {
                    assemblyName = "assembly";
                }
            }

            loadedProjects.Add(
                ProjectInfo.Create(
                    projectId,
                    version,
                    name,
                    assemblyName,
                    loader.Language,
                    projectFilePath,
                    outputFilePath,
                    projectFileInfo.CompilationOptions,
                    projectFileInfo.ParseOptions,
                    docs,
                    resolvedReferences.ProjectReferences,
                    metadataReferences,
                    analyzerReferences: projectFileInfo.AnalyzerReferences,
                    isSubmission: false,
                    hostObjectType: null));

            return projectId;
        }

        private static readonly char[] DirectorySplitChars = new char[] { Path.DirectorySeparatorChar };

        private static ImmutableArray<string> GetDocumentFolders(string logicalPath)
        {
            var logicalDirectory = Path.GetDirectoryName(logicalPath);

            if (!string.IsNullOrEmpty(logicalDirectory))
            {
                return logicalDirectory.Split(DirectorySplitChars, StringSplitOptions.None).ToImmutableArray();
            }

            return ImmutableArray.Create<string>();
        }

        private void CheckDocuments(IEnumerable<DocumentFileInfo> docs, string projectFilePath, ProjectId projectId)
        {
            var paths = new HashSet<string>();
            foreach (var doc in docs)
            {
                if (paths.Contains(doc.FilePath))
                {
                    this.OnWorkspaceFailed(new ProjectDiagnostic(WorkspaceDiagnosticKind.Warning, string.Format(WorkspacesResources.DuplicateSourceFileInProject, doc.FilePath, projectFilePath), projectId));
                }

                paths.Add(doc.FilePath);
            }
        }

        private class ResolvedReferences
        {
            public readonly List<ProjectReference> ProjectReferences = new List<ProjectReference>();
            public readonly List<MetadataReference> MetadataReferences = new List<MetadataReference>();
        }

        private async Task<ResolvedReferences> ResolveProjectReferencesAsync(
            string thisProjectPath,
            IReadOnlyList<ProjectFileReference> projectFileReferences, 
            bool preferMetadata,
            List<ProjectInfo> loadedProjects,
            CancellationToken cancellationToken)
        {
            var resolvedReferences = new ResolvedReferences();
            var reportMode = this.SkipUnrecognizedProjects ? ReportMode.Log : ReportMode.Throw;

            foreach (var projectFileReference in projectFileReferences)
            {
                string fullPath;

                if (TryGetAbsoluteProjectPath(projectFileReference.Path, Path.GetDirectoryName(thisProjectPath), reportMode, out fullPath))
                {
                    // if the project is already loaded, then just reference the one we have
                    var existingProjectId = this.GetProjectId(fullPath);
                    if (existingProjectId != null)
                    {
                        resolvedReferences.ProjectReferences.Add(new ProjectReference(existingProjectId, projectFileReference.Aliases));
                        continue;
                    }

                    IProjectFileLoader loader;
                    TryGetLoaderFromProjectPath(fullPath, ReportMode.Ignore, out loader);

                    // get metadata if prefered or if loader is unknown
                    if (preferMetadata || loader == null)
                    {
                        var projectMetadata = await this.GetProjectMetadata(fullPath, projectFileReference.Aliases, this.properties, cancellationToken).ConfigureAwait(false);
                        if (projectMetadata != null)
                        {
                            resolvedReferences.MetadataReferences.Add(projectMetadata);
                            continue;
                        }
                    }

                    // must load, so we really need loader
                    if (TryGetLoaderFromProjectPath(fullPath, reportMode, out loader))
                    {
                        // load the project
                        var projectId = await this.GetOrLoadProjectAsync(fullPath, loader, preferMetadata, loadedProjects, cancellationToken).ConfigureAwait(false);
                        resolvedReferences.ProjectReferences.Add(new ProjectReference(projectId, projectFileReference.Aliases));
                        continue;
                    }
                }
                else
                {
                    fullPath = projectFileReference.Path;
                }

                // cannot find metadata and project cannot be loaded, so leave a project reference to a non-existent project.
                var id = this.GetOrCreateProjectId(fullPath);
                resolvedReferences.ProjectReferences.Add(new ProjectReference(id, projectFileReference.Aliases));
            }

            return resolvedReferences;
        }

        /// <summary>
        /// Gets a MetadataReference to a project's output assembly.
        /// </summary>
        private async Task<MetadataReference> GetProjectMetadata(string projectFilePath, ImmutableArray<string> aliases, IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
        {
            // use loader service to determine output file for project if possible
            var outputFilePath = await ProjectFileLoader.GetOutputFilePathAsync(projectFilePath, globalProperties, cancellationToken).ConfigureAwait(false);

            if (outputFilePath != null && File.Exists(outputFilePath))
            {
                if (Workspace.TestHookStandaloneProjectsDoNotHoldReferences)
                {
                    var documentationService = this.Services.GetService<IDocumentationProviderService>();
                    var docProvider = documentationService.GetDocumentationProvider(outputFilePath);

                    return new MetadataImageReference(
                        AssemblyMetadata.CreateFromImage(ImmutableArray.Create(File.ReadAllBytes(outputFilePath))),
                        documentation: docProvider,
                        aliases: aliases,
                        display: outputFilePath);
                }
                else
                {
                    var metadataService = this.Services.GetService<IMetadataReferenceProviderService>();
                    var provider = metadataService.GetProvider();
                    return provider.GetReference(outputFilePath, new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases));
                }
            }

            return null;
        }
    #endregion

    #region Apply Changes
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

        private IProjectFile applyChangesProjectFile;

        public override bool TryApplyChanges(Solution newSolution)
        {
            using (this.serializationLock.DisposableWait())
            {
                return base.TryApplyChanges(newSolution);
            }
        }

        protected override void ApplyProjectChanges(ProjectChanges projectChanges)
        {
            System.Diagnostics.Debug.Assert(this.applyChangesProjectFile == null);

            var project = projectChanges.OldProject ?? projectChanges.NewProject;

            try
            {
                // if we need to modify the project file, load it first.
                if (projectChanges.GetAddedDocuments().Any() ||
                    projectChanges.GetRemovedDocuments().Any())
                {
                    var projectPath = project.FilePath;
                    IProjectFileLoader loader;
                    if (this.TryGetLoaderFromProjectPath(projectPath, ReportMode.Ignore, out loader))
                    { 
                        try
                        {
                            this.applyChangesProjectFile = loader.LoadProjectFileAsync(projectPath, this.properties, CancellationToken.None).Result;
                        }
                        catch (System.IO.IOException exception)
                        {
                            this.OnWorkspaceFailed(new ProjectDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, projectChanges.ProjectId));
                        }
                    }
                }

                // do normal apply operations
                base.ApplyProjectChanges(projectChanges);

                // save project file
                if (this.applyChangesProjectFile != null)
                {
                    try
                    {
                        this.applyChangesProjectFile.Save();
                    }
                    catch (System.IO.IOException exception)
                    {
                        this.OnWorkspaceFailed(new ProjectDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, projectChanges.ProjectId));
                    }
                }
            }
            finally
            {
                this.applyChangesProjectFile = null;
            }
        }

        protected override void ChangedDocumentText(DocumentId documentId, SourceText text)
        {
            var document = this.CurrentSolution.GetDocument(documentId);
            if (document != null)
            {
                this.SaveDocumentText(documentId, document.FilePath, text);
                this.OnDocumentTextChanged(documentId, text, PreservationMode.PreserveValue);
            }
        }

        protected override void ChangedAdditionalDocumentText(DocumentId documentId, SourceText text)
        {
            var document = this.CurrentSolution.GetAdditionalDocument(documentId);
            if (document != null)
            {
                this.SaveDocumentText(documentId, document.FilePath, text);
                this.OnAdditionalDocumentTextChanged(documentId, text, PreservationMode.PreserveValue);
            }
        }

        private void AddDocumentCore(DocumentId documentId, IEnumerable<string> folders, string name, SourceText text = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular, bool isAdditionalDocument = false)
        {
            System.Diagnostics.Debug.Assert(this.applyChangesProjectFile != null);

            var project = this.CurrentSolution.GetProject(documentId.ProjectId);

            IProjectFileLoader loader;
            if (this.TryGetLoaderFromProjectPath(project.FilePath, ReportMode.Ignore, out loader))
            {
                var extension = isAdditionalDocument ? Path.GetExtension(name) : this.applyChangesProjectFile.GetDocumentExtension(sourceCodeKind);
                var fileName = Path.ChangeExtension(name, extension);

                var relativePath = folders != null ? Path.Combine(Path.Combine(folders.ToArray()), fileName) : fileName;

                var fullPath = GetAbsolutePath(relativePath, Path.GetDirectoryName(project.FilePath));
                var encoding = (text != null) ? text.Encoding : Encoding.UTF8;

                var documentInfo = DocumentInfo.Create(documentId, fileName, folders, sourceCodeKind, new FileTextLoader(fullPath, encoding), fullPath, encoding, isGenerated: false);

                // add document to project file
                this.applyChangesProjectFile.AddDocument(relativePath);

                // add to solution
                if (isAdditionalDocument)
                {
                    this.OnAdditionalDocumentAdded(documentInfo);
                }
                else
                {
                    this.OnDocumentAdded(documentInfo);
                }

                // save text to disk
                if (text != null)
                {
                    this.SaveDocumentText(documentId, fullPath, text);
                }
            }
        }

        protected override void AddDocument(DocumentId documentId, IEnumerable<string> folders, string name, SourceText text = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
        {
            AddDocumentCore(documentId, folders, name, text, sourceCodeKind, isAdditionalDocument: false);
        }

        protected override void AddAdditionalDocument(DocumentId documentId, IEnumerable<string> folders, string name, SourceText text = null)
        {
            AddDocumentCore(documentId, folders, name, text, SourceCodeKind.Regular, isAdditionalDocument: true);
        }

        private void SaveDocumentText(DocumentId id, string fullPath, SourceText newText)
        {
            try
            {
                var dir = Path.GetDirectoryName(fullPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                using (var writer = new StreamWriter(fullPath, append: false, encoding: newText.Encoding ?? Encoding.UTF8))
                {
                    newText.Write(writer);
                }
            }
            catch (IOException exception)
            {
                this.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, id));
            }
        }

        protected override void RemoveDocument(DocumentId documentId)
        {
            System.Diagnostics.Debug.Assert(this.applyChangesProjectFile != null);

            var document = this.CurrentSolution.GetDocument(documentId);
            if (document != null)
            {
                this.applyChangesProjectFile.RemoveDocument(document.FilePath);
                this.DeleteDocumentFile(document.Id, document.FilePath);
                this.OnDocumentRemoved(documentId);
            }
        }

        protected override void RemoveAdditionalDocument(DocumentId documentId)
        {
            System.Diagnostics.Debug.Assert(this.applyChangesProjectFile != null);

            var document = this.CurrentSolution.GetAdditionalDocument(documentId);
            if (document != null)
            {
                this.applyChangesProjectFile.RemoveDocument(document.FilePath);
                this.DeleteDocumentFile(document.Id, document.FilePath);
                this.OnAdditionalDocumentRemoved(documentId);
            }
        }

        private void DeleteDocumentFile(DocumentId documentId, string fullPath)
        {
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (System.IO.IOException exception)
            {
                this.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, documentId));
            }
        }
    }
    #endregion
}