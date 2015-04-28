// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if !MSBUILD12
using Microsoft.Build.Construction;
#endif

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
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
        private readonly NonReentrantLock _serializationLock = new NonReentrantLock();

        // used to protect access to mutable state
        private readonly NonReentrantLock _dataGuard = new NonReentrantLock();

        private readonly Dictionary<string, string> _extensionToLanguageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ProjectId> _projectPathToProjectIdMap = new Dictionary<string, ProjectId>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, IProjectFileLoader> _projectPathToLoaderMap = new Dictionary<string, IProjectFileLoader>(StringComparer.OrdinalIgnoreCase);

        private string _solutionFilePath;
        private ImmutableDictionary<string, string> _properties;

        private MSBuildWorkspace(
            HostServices hostServices,
            ImmutableDictionary<string, string> properties)
            : base(hostServices, "MSBuildWorkspace")
        {
            // always make a copy of these build properties (no mutation please!)
            _properties = properties ?? ImmutableDictionary<string, string>.Empty;
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
        /// <param name="properties">An optional set of MSBuild properties used when interpreting project files.
        /// These are the same properties that are passed to msbuild via the /property:&lt;n&gt;=&lt;v&gt; command line argument.</param>
        public static MSBuildWorkspace Create(IDictionary<string, string> properties)
        {
            return Create(properties, DesktopMefHostServices.DefaultServices);
        }

        /// <summary>
        /// Create a new instance of a workspace that can be populated by opening solution and project files.
        /// </summary>
        /// <param name="properties">The MSBuild properties used when interpreting project files.
        /// These are the same properties that are passed to msbuild via the /property:&lt;n&gt;=&lt;v&gt; command line argument.</param>
        /// <param name="hostServices">The <see cref="HostServices"/> used to configure this workspace.</param>
        public static MSBuildWorkspace Create(IDictionary<string, string> properties, HostServices hostServices)
        {
            if (properties == null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            if (hostServices == null)
            {
                throw new ArgumentNullException(nameof(hostServices));
            }

            return new MSBuildWorkspace(hostServices, properties.ToImmutableDictionary());
        }

        /// <summary>
        /// The MSBuild properties used when interpreting project files.
        /// These are the same properties that are passed to msbuild via the /property:&lt;n&gt;=&lt;v&gt; command line argument.
        /// </summary>
        public ImmutableDictionary<string, string> Properties
        {
            get { return _properties; }
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
                throw new ArgumentNullException(nameof(language));
            }

            if (projectFileExtension == null)
            {
                throw new ArgumentNullException(nameof(projectFileExtension));
            }

            using (_dataGuard.DisposableWait())
            {
                _extensionToLanguageMap[projectFileExtension] = language;
            }
        }

        /// <summary>
        /// Close the open solution, and reset the workspace to a new empty solution.
        /// </summary>
        public void CloseSolution()
        {
            using (_serializationLock.DisposableWait())
            {
                this.ClearSolution();
            }
        }

        protected override void ClearSolutionData()
        {
            base.ClearSolutionData();

            using (_dataGuard.DisposableWait())
            {
                this.SetSolutionProperties(solutionFilePath: null);

                // clear project related data
                _projectPathToProjectIdMap.Clear();
                _projectPathToLoaderMap.Clear();
            }
        }

        private const string SolutionDirProperty = "SolutionDir";

        private void SetSolutionProperties(string solutionFilePath)
        {
            _solutionFilePath = solutionFilePath;

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
                    if (!solutionDirectory.EndsWith(@"\", StringComparison.Ordinal))
                    {
                        solutionDirectory += @"\";
                    }

                    if (Directory.Exists(solutionDirectory))
                    {
                        _properties = _properties.SetItem(SolutionDirProperty, solutionDirectory);
                    }
                }
            }
        }

        private ProjectId GetProjectId(string fullProjectPath)
        {
            using (_dataGuard.DisposableWait())
            {
                ProjectId id;
                _projectPathToProjectIdMap.TryGetValue(fullProjectPath, out id);
                return id;
            }
        }

        private ProjectId GetOrCreateProjectId(string fullProjectPath)
        {
            using (_dataGuard.DisposableWait())
            {
                ProjectId id;
                if (!_projectPathToProjectIdMap.TryGetValue(fullProjectPath, out id))
                {
                    id = ProjectId.CreateNewId(debugName: fullProjectPath);
                    _projectPathToProjectIdMap.Add(fullProjectPath, id);
                }

                return id;
            }
        }

        private bool TryGetLoaderFromProjectPath(string projectFilePath, ReportMode mode, out IProjectFileLoader loader)
        {
            using (_dataGuard.DisposableWait())
            {
                // check to see if we already know the loader
                if (!_projectPathToLoaderMap.TryGetValue(projectFilePath, out loader))
                {
                    // otherwise try to figure it out from extension
                    var extension = Path.GetExtension(projectFilePath);
                    if (extension.Length > 0 && extension[0] == '.')
                    {
                        extension = extension.Substring(1);
                    }

                    string language;
                    if (_extensionToLanguageMap.TryGetValue(extension, out language))
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
                        _projectPathToLoaderMap[projectFilePath] = loader;
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
                throw new ArgumentNullException(nameof(solutionFilePath));
            }

            this.ClearSolution();

            var absoluteSolutionPath = this.GetAbsoluteSolutionPath(solutionFilePath, Directory.GetCurrentDirectory());

            using (_dataGuard.DisposableWait(cancellationToken))
            {
                this.SetSolutionProperties(absoluteSolutionPath);
            }

            VersionStamp version = default(VersionStamp);

#if !MSBUILD12
            Microsoft.Build.Construction.SolutionFile solutionFile = Microsoft.Build.Construction.SolutionFile.Parse(absoluteSolutionPath);
            var reportMode = this.SkipUnrecognizedProjects ? ReportMode.Log : ReportMode.Throw;
            var invalidProjects = new List<ProjectInSolution>();

            // seed loaders from known project types
            using (_dataGuard.DisposableWait(cancellationToken))
            {
                foreach (var project in solutionFile.ProjectsInOrder)
                {
                    if (project.ProjectType == SolutionProjectType.SolutionFolder)
                    {
                        continue;
                    }

                    var projectAbsolutePath = TryGetAbsolutePath(project.AbsolutePath, reportMode);
                    if (projectAbsolutePath != null)
                    {
                        var extension = Path.GetExtension(projectAbsolutePath);
                        if (extension.Length > 0 && extension[0] == '.')
                        {
                            extension = extension.Substring(1);
                        }

                        var loader = ProjectFileLoader.GetLoaderForProjectFileExtension(this, extension);
                        if (loader != null)
                        {
                            _projectPathToLoaderMap[projectAbsolutePath] = loader;
                        }
                    }
                    else
                    {
                        invalidProjects.Add(project);
                    }
                }
            }

            // a list to accumulate all the loaded projects
            var loadedProjects = new List<ProjectInfo>();

            // load all the projects
            foreach (var project in solutionFile.ProjectsInOrder)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (project.ProjectType != SolutionProjectType.SolutionFolder && !invalidProjects.Contains(project))
                {
                    var projectAbsolutePath = TryGetAbsolutePath(project.AbsolutePath, reportMode);
                    if (projectAbsolutePath != null)
                    {
                        IProjectFileLoader loader;
                        if (TryGetLoaderFromProjectPath(projectAbsolutePath, reportMode, out loader))
                        {
                            // projects get added to 'loadedProjects' as side-effect
                            // never perfer metadata when loading solution, all projects get loaded if they can.
                            var tmp = await GetOrLoadProjectAsync(projectAbsolutePath, loader, preferMetadata: false, loadedProjects: loadedProjects, cancellationToken: cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }
#else
            SolutionFile solutionFile = null;

            using (var reader = new StreamReader(absoluteSolutionPath))
            {
                version = VersionStamp.Create(File.GetLastWriteTimeUtc(absoluteSolutionPath));
                var text = await reader.ReadToEndAsync().ConfigureAwait(false);
                solutionFile = SolutionFile.Parse(new StringReader(text));
            }

            var solutionFolder = Path.GetDirectoryName(absoluteSolutionPath);

            // seed loaders from known project types
            using (_dataGuard.DisposableWait())
            {
                foreach (var projectBlock in solutionFile.ProjectBlocks)
                {
                    string absoluteProjectPath;
                    if (TryGetAbsoluteProjectPath(projectBlock.ProjectPath, solutionFolder, ReportMode.Ignore, out absoluteProjectPath))
                    {
                        var loader = ProjectFileLoader.GetLoaderForProjectTypeGuid(this, projectBlock.ProjectTypeGuid);
                        if (loader != null)
                        {
                            _projectPathToLoaderMap[absoluteProjectPath] = loader;
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
#endif

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
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            string fullPath;
            if (this.TryGetAbsoluteProjectPath(projectFilePath, Directory.GetCurrentDirectory(), ReportMode.Throw, out fullPath))
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

        private string TryGetAbsolutePath(string path, ReportMode mode)
        {
            try
            {
                path = Path.GetFullPath(path);
            }
            catch (Exception)
            {
                ReportFailure(mode, string.Format(WorkspacesResources.InvalidProjectFilePath, path));
                return null;
            }

            if (!File.Exists(path))
            {
                ReportFailure(
                    mode,
                    string.Format(WorkspacesResources.ProjectFileNotFound, path),
                    msg => new FileNotFoundException(msg));
                return null;
            }

            return path;
        }

        private void UpdateReferencesAfterAdd()
        {
            using (_serializationLock.DisposableWait())
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

            var projectFile = await loader.LoadProjectFileAsync(projectFilePath, _properties, cancellationToken).ConfigureAwait(false);
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

            Encoding defaultEncoding = GetDefaultEncoding(projectFileInfo.CodePage);

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
                    docFileInfo.IsGenerated));
            }

            var additonalDocs = new List<DocumentInfo>();
            foreach (var docFileInfo in projectFileInfo.AdditionalDocuments)
            {
                additonalDocs.Add(DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId, debugName: docFileInfo.FilePath),
                    Path.GetFileName(docFileInfo.LogicalPath),
                    GetDocumentFolders(docFileInfo.LogicalPath),
                    SourceCodeKind.Regular,
                    new FileTextLoader(docFileInfo.FilePath, defaultEncoding),
                    docFileInfo.FilePath,
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
                    additionalDocuments: additonalDocs,
                    isSubmission: false,
                    hostObjectType: null));

            return projectId;
        }

        private static Encoding GetDefaultEncoding(int codePage)
        {
            // If no CodePage was specified in the project file, then the FileTextLoader will 
            // attempt to use UTF8 before falling back on Encoding.Default.
            if (codePage == 0)
            {
                return null;
            }

            try
            {
                return Encoding.GetEncoding(codePage);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static readonly char[] s_directorySplitChars = new char[] { Path.DirectorySeparatorChar };

        private static ImmutableArray<string> GetDocumentFolders(string logicalPath)
        {
            var logicalDirectory = Path.GetDirectoryName(logicalPath);

            if (!string.IsNullOrEmpty(logicalDirectory))
            {
                return logicalDirectory.Split(s_directorySplitChars, StringSplitOptions.None).ToImmutableArray();
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

                    // get metadata if preferred or if loader is unknown
                    if (preferMetadata || loader == null)
                    {
                        var projectMetadata = await this.GetProjectMetadata(fullPath, projectFileReference.Aliases, _properties, cancellationToken).ConfigureAwait(false);
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
            string outputFilePath = null;

            try
            {
                outputFilePath = await ProjectFileLoader.GetOutputFilePathAsync(projectFilePath, globalProperties, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                this.OnWorkspaceFailed(new WorkspaceDiagnostic(WorkspaceDiagnosticKind.Failure, e.Message));
            }

            if (outputFilePath != null && File.Exists(outputFilePath))
            {
                if (Workspace.TestHookStandaloneProjectsDoNotHoldReferences)
                {
                    var documentationService = this.Services.GetService<IDocumentationProviderService>();
                    var docProvider = documentationService.GetDocumentationProvider(outputFilePath);
                    var metadata = AssemblyMetadata.CreateFromImage(File.ReadAllBytes(outputFilePath));

                    return metadata.GetReference(
                        documentation: docProvider,
                        aliases: aliases,
                        display: outputFilePath);
                }
                else
                {
                    var metadataService = this.Services.GetService<IMetadataService>();
                    return metadataService.GetReference(outputFilePath, new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases));
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
                case ApplyChangesKind.AddMetadataReference:
                case ApplyChangesKind.RemoveMetadataReference:
                case ApplyChangesKind.AddProjectReference:
                case ApplyChangesKind.RemoveProjectReference:
                case ApplyChangesKind.AddAnalyzerReference:
                case ApplyChangesKind.RemoveAnalyzerReference:
                    return true;
                default:
                    return false;
            }
        }

        private bool HasProjectFileChanges(ProjectChanges changes)
        {
            return changes.GetAddedDocuments().Any() ||
                   changes.GetRemovedDocuments().Any() ||
                   changes.GetAddedMetadataReferences().Any() ||
                   changes.GetRemovedMetadataReferences().Any() ||
                   changes.GetAddedProjectReferences().Any() ||
                   changes.GetRemovedProjectReferences().Any() ||
                   changes.GetAddedAnalyzerReferences().Any() ||
                   changes.GetRemovedAnalyzerReferences().Any();
        }

        private IProjectFile _applyChangesProjectFile;

        public override bool TryApplyChanges(Solution newSolution)
        {
            using (_serializationLock.DisposableWait())
            {
                return base.TryApplyChanges(newSolution);
            }
        }

        protected override void ApplyProjectChanges(ProjectChanges projectChanges)
        {
            System.Diagnostics.Debug.Assert(_applyChangesProjectFile == null);

            var project = projectChanges.OldProject ?? projectChanges.NewProject;

            try
            {
                // if we need to modify the project file, load it first.
                if (this.HasProjectFileChanges(projectChanges))
                {
                    var projectPath = project.FilePath;
                    IProjectFileLoader loader;
                    if (this.TryGetLoaderFromProjectPath(projectPath, ReportMode.Ignore, out loader))
                    {
                        try
                        {
                            _applyChangesProjectFile = loader.LoadProjectFileAsync(projectPath, _properties, CancellationToken.None).Result;
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
                if (_applyChangesProjectFile != null)
                {
                    try
                    {
                        _applyChangesProjectFile.Save();
                    }
                    catch (System.IO.IOException exception)
                    {
                        this.OnWorkspaceFailed(new ProjectDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, projectChanges.ProjectId));
                    }
                }
            }
            finally
            {
                _applyChangesProjectFile = null;
            }
        }

        protected override void ApplyDocumentTextChanged(DocumentId documentId, SourceText text)
        {
            var document = this.CurrentSolution.GetDocument(documentId);
            if (document != null)
            {
                Encoding encoding = DetermineEncoding(text, document);

                this.SaveDocumentText(documentId, document.FilePath, text, encoding ?? Encoding.UTF8);
                this.OnDocumentTextChanged(documentId, text, PreservationMode.PreserveValue);
            }
        }

        private static Encoding DetermineEncoding(SourceText text, Document document)
        {
            if (text.Encoding != null)
            {
                return text.Encoding;
            }

            try
            {
                using (ExceptionHelpers.SuppressFailFast())
                {
                    using (var stream = new FileStream(document.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var onDiskText = EncodedStringText.Create(stream);
                        return onDiskText.Encoding;
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (InvalidDataException)
            {
            }

            return null;
        }

        protected override void ApplyDocumentAdded(DocumentInfo info, SourceText text)
        {
            System.Diagnostics.Debug.Assert(_applyChangesProjectFile != null);

            var project = this.CurrentSolution.GetProject(info.Id.ProjectId);

            IProjectFileLoader loader;
            if (this.TryGetLoaderFromProjectPath(project.FilePath, ReportMode.Ignore, out loader))
            {
                var extension = _applyChangesProjectFile.GetDocumentExtension(info.SourceCodeKind);
                var fileName = Path.ChangeExtension(info.Name, extension);

                var relativePath = (info.Folders != null && info.Folders.Count > 0)
                    ? Path.Combine(Path.Combine(info.Folders.ToArray()), fileName)
                    : fileName;

                var fullPath = GetAbsolutePath(relativePath, Path.GetDirectoryName(project.FilePath));

                var newDocumentInfo = info.WithName(fileName)
                    .WithFilePath(fullPath)
                    .WithTextLoader(new FileTextLoader(fullPath, text.Encoding));

                // add document to project file
                _applyChangesProjectFile.AddDocument(relativePath);

                // add to solution
                this.OnDocumentAdded(newDocumentInfo);

                // save text to disk
                if (text != null)
                {
                    this.SaveDocumentText(info.Id, fullPath, text, text.Encoding ?? Encoding.UTF8);
                }
            }
        }

        private void SaveDocumentText(DocumentId id, string fullPath, SourceText newText, Encoding encoding)
        {
            try
            {
                using (ExceptionHelpers.SuppressFailFast())
                {
                    var dir = Path.GetDirectoryName(fullPath);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    Debug.Assert(encoding != null);
                    using (var writer = new StreamWriter(fullPath, append: false, encoding: encoding))
                    {
                        newText.Write(writer);
                    }
                }
            }
            catch (IOException exception)
            {
                this.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, id));
            }
        }

        protected override void ApplyDocumentRemoved(DocumentId documentId)
        {
            Debug.Assert(_applyChangesProjectFile != null);

            var document = this.CurrentSolution.GetDocument(documentId);
            if (document != null)
            {
                _applyChangesProjectFile.RemoveDocument(document.FilePath);
                this.DeleteDocumentFile(document.Id, document.FilePath);
                this.OnDocumentRemoved(documentId);
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
            catch (IOException exception)
            {
                this.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, documentId));
            }
            catch (NotSupportedException exception)
            {
                this.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, documentId));
            }
            catch (UnauthorizedAccessException exception)
            {
                this.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, exception.Message, documentId));
            }
        }

        protected override void ApplyMetadataReferenceAdded(ProjectId projectId, MetadataReference metadataReference)
        {
            Debug.Assert(_applyChangesProjectFile != null);
            var identity = GetAssemblyIdentity(projectId, metadataReference);
            _applyChangesProjectFile.AddMetadataReference(metadataReference, identity);
            this.OnMetadataReferenceAdded(projectId, metadataReference);
        }

        protected override void ApplyMetadataReferenceRemoved(ProjectId projectId, MetadataReference metadataReference)
        {
            Debug.Assert(_applyChangesProjectFile != null);
            var identity = GetAssemblyIdentity(projectId, metadataReference);
            _applyChangesProjectFile.RemoveMetadataReference(metadataReference, identity);
            this.OnMetadataReferenceRemoved(projectId, metadataReference);
        }

        private AssemblyIdentity GetAssemblyIdentity(ProjectId projectId, MetadataReference metadataReference)
        {
            var project = this.CurrentSolution.GetProject(projectId);
            if (!project.MetadataReferences.Contains(metadataReference))
            {
                project = project.AddMetadataReference(metadataReference);
            }

            var compilation = project.GetCompilationAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            var symbol = compilation.GetAssemblyOrModuleSymbol(metadataReference) as IAssemblySymbol;
            return symbol != null ? symbol.Identity : null;
        }

        protected override void ApplyProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference)
        {
            Debug.Assert(_applyChangesProjectFile != null);

            var project = this.CurrentSolution.GetProject(projectReference.ProjectId);
            if (project != null)
            {
                _applyChangesProjectFile.AddProjectReference(project.Name, new ProjectFileReference(project.FilePath, projectReference.Aliases));
            }

            this.OnProjectReferenceAdded(projectId, projectReference);
        }

        protected override void ApplyProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference)
        {
            Debug.Assert(_applyChangesProjectFile != null);

            var project = this.CurrentSolution.GetProject(projectReference.ProjectId);
            if (project != null)
            {
                _applyChangesProjectFile.RemoveProjectReference(project.Name, project.FilePath);
            }

            this.OnProjectReferenceRemoved(projectId, projectReference);
        }

        protected override void ApplyAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            Debug.Assert(_applyChangesProjectFile != null);
            _applyChangesProjectFile.AddAnalyzerReference(analyzerReference);
            this.OnAnalyzerReferenceAdded(projectId, analyzerReference);
        }

        protected override void ApplyAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            Debug.Assert(_applyChangesProjectFile != null);
            _applyChangesProjectFile.RemoveAnalyzerReference(analyzerReference);
            this.OnAnalyzerReferenceRemoved(projectId, analyzerReference);
        }
    }
    #endregion
}
