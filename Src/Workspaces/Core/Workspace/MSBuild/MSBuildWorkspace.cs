// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
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
        // used at serialize access to public methods
        private readonly NonReentrantLock serializationLock = new NonReentrantLock();

        // used to protect access to mutable state
        private readonly NonReentrantLock dataGuard = new NonReentrantLock();

        private readonly Dictionary<string, string> extensionToLanguageMap = new Dictionary<string, string>();
        private readonly Dictionary<string, ProjectId> projectPathToProjectIdMap = new Dictionary<string, ProjectId>();
        private readonly Dictionary<string, IProjectFileLoader> projectPathToLoaderMap = new Dictionary<string, IProjectFileLoader>();
        private readonly Dictionary<string, DocumentationProvider> assemblyPathToDocumentationProviderMap = new Dictionary<string, DocumentationProvider>();

        private string solutionFilePath;
        private bool currentPreferMetadata;
        private ImmutableDictionary<string, string> properties;

        private MSBuildWorkspace(
            HostServices hostServices,
            ImmutableDictionary<string, string> properties)
            : base(hostServices, "MSBuildWorkspace")
        {
            // always make a copy of these build properties (no mutation please!)
            this.properties = properties ?? ImmutableDictionary<string, string>.Empty;
            this.SetSolutionProperties(solutionFilePath: null);
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
        /// <param name="hostServices">The feature pack used to configure this workspace.</param>
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
        public bool LoadMetadataForReferencedProjects
        {
            get { return this.currentPreferMetadata; }
            set { this.currentPreferMetadata = value; }
        }

        /// <summary>
        /// Associates a language name with a project file extension.
        /// Projects with the specified file extension will be opened using the specified language.
        /// </summary>
        public void AssociateLanguageWithExtension(string projectFileExtension, string language)
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
                this.assemblyPathToDocumentationProviderMap.Clear();
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

        private IProjectFileLoader GetLoaderFromProjectPath(string projectFilePath)
        {
            using (this.dataGuard.DisposableWait())
            {
                // check to see if we already know the loader
                IProjectFileLoader loader;
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
                    }
                    else 
                    {
                        // note: this forces all language dlls to load
                        loader = this.Services.SupportedLanguages.Select(lang => this.Services.GetLanguageServices(lang).GetService<IProjectFileLoader>())
                                    .FirstOrDefault(ld => ld.IsProjectFileExtension(extension));
                    }

                    this.projectPathToLoaderMap[projectFilePath] = loader;
                }

                return loader;
            }
        }

        private IProjectFileLoader GetLoaderFromProjectType(Guid projectTypeGuid)
        {
            return this.Services.SupportedLanguages.Select(lang => this.Services.GetLanguageServices(lang).GetService<IProjectFileLoader>()).FirstOrDefault(p => p.IsProjectTypeGuid(projectTypeGuid));
        }

        private static string GetAbsolutePath(string path, string baseDirectoryPath)
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

            solutionFilePath = Path.GetFullPath(solutionFilePath);

            using (this.dataGuard.DisposableWait())
            {
                this.SetSolutionProperties(solutionFilePath);
            }

            VersionStamp version = default(VersionStamp);
            SolutionFile solutionFile = null;

            using (var reader = new StreamReader(solutionFilePath))
            {
                version = VersionStamp.Create(File.GetLastWriteTimeUtc(solutionFilePath));
                var text = await reader.ReadToEndAsync().ConfigureAwait(false);
                solutionFile = SolutionFile.Parse(new StringReader(text));
            }

            var solutionFolder = Path.GetDirectoryName(solutionFilePath);

            // seed loaders from known project types
            using (this.dataGuard.DisposableWait())
            {
                foreach (var projectBlock in solutionFile.ProjectBlocks)
                {
                    var absoluteProjectPath = GetAbsolutePath(projectBlock.ProjectPath, solutionFolder);
                    var loader = GetLoaderFromProjectType(projectBlock.ProjectTypeGuid);
                    this.projectPathToLoaderMap[absoluteProjectPath] = loader;
                }
            }

            // a list to accumulate all the loaded projects
            var loadedProjects = new List<ProjectInfo>();

            // load all the projects
            foreach (var projectBlock in solutionFile.ProjectBlocks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var absoluteProjectPath = GetAbsolutePath(projectBlock.ProjectPath, solutionFolder);

                // don't even try to load project if there is no file
                if (!File.Exists(absoluteProjectPath))
                {
                    continue;
                }

                var loader = GetLoaderFromProjectPath(absoluteProjectPath);

                // only attempt to load project if it can be loaded.
                if (loader != null)
                {
                    // projects get added to pending projects as side-effect
                    // never perfer metadata when loading solution, all projects get loaded if they can.
                    var tmp = await GetOrLoadProjectAsync(absoluteProjectPath, loader, preferMetadata: false, loadedProjects: loadedProjects, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }

            // have the base workspace construct the solution from this info
            this.OnSolutionAdded(SolutionInfo.Create(SolutionId.CreateNewId(debugName: solutionFilePath), version, solutionFilePath, loadedProjects));

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

            projectFilePath = Path.GetFullPath(projectFilePath);

            var loader = this.GetLoaderFromProjectPath(projectFilePath);
            if (loader == null)
            {
                throw new InvalidOperationException(WorkspacesResources.UnrecognizedProjectType);
            }

            projectFilePath = Path.GetFullPath(projectFilePath);

            var loadedProjects = new List<ProjectInfo>();
            var projectId = await GetOrLoadProjectAsync(projectFilePath, loader, this.currentPreferMetadata, loadedProjects, cancellationToken).ConfigureAwait(false);

            // add projects to solution
            foreach (var project in loadedProjects)
            {
                this.OnProjectAdded(project);
            }

            return this.CurrentSolution.GetProject(projectId);
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

            try
            {
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
                var docFileInfos = projectFileInfo.Documents.ToImmutableListOrEmpty();
                CheckDocuments(docFileInfos, projectFilePath, projectId);

                var docs = new List<DocumentInfo>();
                foreach (var docFileInfo in docFileInfos)
                {
                    docs.Add(DocumentInfo.Create(
                        DocumentId.CreateNewId(projectId, debugName: docFileInfo.FilePath),
                        Path.GetFileName(docFileInfo.LogicalPath),
                        GetDocumentFolders(docFileInfo.LogicalPath),
                        projectFile.GetSourceCodeKind(docFileInfo.FilePath),
                        new FileTextLoader(docFileInfo.FilePath),
                        docFileInfo.FilePath,
                        docFileInfo.IsGenerated));
                }

                // project references
                var resolvedReferences = await this.ResolveProjectReferencesAsync(
                    projectFilePath, projectFileInfo.ProjectReferences, preferMetadata, loadedProjects, cancellationToken).ConfigureAwait(false);

                // metadata references
                var metadataReferences = projectFileInfo.MetadataReferences
                    .Select(mi => new MetadataFileReference(mi.Path, mi.Properties, this.GetDocumentationProvider(mi.Path)))
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
                        docs.ToImmutableListOrEmpty(),
                        resolvedReferences.ProjectReferences.ToImmutableListOrEmpty(),
                        metadataReferences.ToImmutableListOrEmpty(),
                        analyzerReferences: projectFileInfo.AnalyzerReferences,
                        isSubmission: false,
                        hostObjectType: null));

                return projectId;
            }
            catch (System.IO.IOException exception)
            {
                this.OnWorkspaceFailed(new ProjectDiagnostic(WorkspaceDiagnosticKind.FileAccessFailure, exception.Message, projectId));
                loadedProjects.Add(ProjectInfo.Create(projectId, VersionStamp.Default, name, name, loader.Language, filePath: projectFilePath));
                return projectId;
            }
        }

        private static readonly char[] DirectorySplitChars = new char[] { Path.DirectorySeparatorChar };

        private static ImmutableList<string> GetDocumentFolders(string logicalPath)
        {
            var logicalDirectory = Path.GetDirectoryName(logicalPath);

            if (!string.IsNullOrEmpty(logicalDirectory))
            {
                return logicalDirectory.Split(DirectorySplitChars, StringSplitOptions.None).ToImmutableList();
            }

            return ImmutableList.Create<string>();
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

            foreach (var projectFileReference in projectFileReferences)
            {
                var fullPath = GetAbsolutePath(projectFileReference.Path, Path.GetDirectoryName(thisProjectPath));

                // if the project is already loaded, then just reference the one we have
                var existingProjectId = this.GetProjectId(fullPath);
                if (existingProjectId != null)
                { 
                    resolvedReferences.ProjectReferences.Add(new ProjectReference(existingProjectId, projectFileReference.Aliases));
                    continue;
                }

                var loader = this.GetLoaderFromProjectPath(fullPath);
                if (preferMetadata || loader == null)
                {
                    // attempt to find project's metadata
                    var projectMetadata = await this.GetProjectMetadata(fullPath, projectFileReference.Aliases, this.properties, cancellationToken).ConfigureAwait(false);
                    if (projectMetadata != null)
                    {
                        resolvedReferences.MetadataReferences.Add(projectMetadata);
                        continue;
                    }
                    else if (loader == null)
                    {
                        // cannot find metadata and project cannot be loaded, so leave a project reference to a non-existent project.
                        var id = this.GetOrCreateProjectId(fullPath);
                        resolvedReferences.ProjectReferences.Add(new ProjectReference(id, projectFileReference.Aliases));
                        continue;
                    }
                }

                // load the project
                var projectId = await this.GetOrLoadProjectAsync(fullPath, loader, preferMetadata, loadedProjects, cancellationToken).ConfigureAwait(false);
                resolvedReferences.ProjectReferences.Add(new ProjectReference(projectId, projectFileReference.Aliases));
            }

            return resolvedReferences;
        }

        /// <summary>
        /// Gets a MetadataReference to a project's output assembly.
        /// </summary>
        private async Task<MetadataReference> GetProjectMetadata(string projectFilePath, ImmutableArray<string> aliases, IDictionary<string, string> globalProperties, CancellationToken cancellationToken)
        {
            try
            {
                // use loader service to determine output file for project if possible
                var outputFilePath = await ProjectFileLoader.GetOutputFilePathAsync(projectFilePath, globalProperties, cancellationToken).ConfigureAwait(false);

                if (outputFilePath != null && File.Exists(outputFilePath))
                {
                    var docProvider = this.GetDocumentationProvider(outputFilePath);

                    if (Workspace.TestHookStandaloneProjectsDoNotHoldReferences)
                    {
                        return new MetadataImageReference(
                            AssemblyMetadata.CreateFromImage(ImmutableArray.Create(File.ReadAllBytes(outputFilePath))),
                            documentation: docProvider,
                            aliases: aliases,
                            display: outputFilePath);
                    }
                    else
                    {
                        return new MetadataFileReference(outputFilePath, new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases), docProvider);
                    }
                }
            }
            catch (System.IO.IOException exception)
            {
                this.OnWorkspaceFailed(new ProjectDiagnostic(WorkspaceDiagnosticKind.FileAccessFailure, exception.Message, this.GetOrCreateProjectId(projectFilePath)));
            }

            return null;
        }

        private DocumentationProvider GetDocumentationProvider(string assemblyPath)
        {
            using (this.dataGuard.DisposableWait())
            {
                assemblyPath = Path.ChangeExtension(assemblyPath, "xml");

                DocumentationProvider provider;
                if (!this.assemblyPathToDocumentationProviderMap.TryGetValue(assemblyPath, out provider))
                {
                    provider = XmlDocumentationProvider.Create(assemblyPath);
                    this.assemblyPathToDocumentationProviderMap.Add(assemblyPath, provider);
                }

                return provider;
            }
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
                    var loader = this.GetLoaderFromProjectPath(projectPath);
                    if (loader != null)
                    {
                        try
                        {
                            this.applyChangesProjectFile = loader.LoadProjectFileAsync(projectPath, this.properties, CancellationToken.None).Result;
                        }
                        catch (System.IO.IOException exception)
                        {
                            this.OnWorkspaceFailed(new ProjectDiagnostic(WorkspaceDiagnosticKind.FileAccessFailure, exception.Message, projectChanges.ProjectId));
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
                        this.OnWorkspaceFailed(new ProjectDiagnostic(WorkspaceDiagnosticKind.FileAccessFailure, exception.Message, projectChanges.ProjectId));
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

        protected override void AddDocument(DocumentId documentId, IEnumerable<string> folders, string name, SourceText text = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
        {
            System.Diagnostics.Debug.Assert(this.applyChangesProjectFile != null);

            var project = this.CurrentSolution.GetProject(documentId.ProjectId);
            var loader = this.GetLoaderFromProjectPath(project.FilePath);

            var extension = this.applyChangesProjectFile.GetDocumentExtension(sourceCodeKind);
            var fileName = Path.ChangeExtension(name, extension);

            var relativePath = folders != null ? Path.Combine(Path.Combine(folders.ToArray()), fileName) : fileName;
            var fullPath = GetAbsolutePath(relativePath, Path.GetDirectoryName(project.FilePath));

            var documentInfo = DocumentInfo.Create(documentId, fileName, folders, sourceCodeKind, new FileTextLoader(fullPath), fullPath, isGenerated: false);

            // add document to project file
            this.applyChangesProjectFile.AddDocument(relativePath);

            // add to solution
            this.OnDocumentAdded(documentInfo);

            // save text to disk
            this.SaveDocumentText(documentId, fullPath, text);
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

                using (var writer = new StreamWriter(fullPath))
                {
                    newText.Write(writer);
                }
            }
            catch (System.IO.IOException exception)
            {
                this.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.FileAccessFailure, exception.Message, id));
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
                this.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.FileAccessFailure, exception.Message, documentId));
            }
        }
    }
    #endregion
}