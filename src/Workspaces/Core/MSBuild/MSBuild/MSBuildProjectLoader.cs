// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MSBuild.Build;
using Microsoft.CodeAnalysis.MSBuild.Logging;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// An API for loading msbuild project files.
    /// </summary>
    public partial class MSBuildProjectLoader
    {
        // the workspace that the projects and solutions are intended to be loaded into.
        private readonly Workspace _workspace;

        // used to protect access to the following mutable state
        private readonly NonReentrantLock _dataGuard = new NonReentrantLock();
        private ImmutableDictionary<string, string> _properties;
        private readonly Dictionary<string, string> _extensionToLanguageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        internal readonly ProjectBuildManager BuildManager;

        /// <summary>
        /// Create a new instance of an <see cref="MSBuildProjectLoader"/>.
        /// </summary>
        public MSBuildProjectLoader(Workspace workspace, ImmutableDictionary<string, string> properties = null)
        {
            _workspace = workspace;
            _properties = properties ?? ImmutableDictionary<string, string>.Empty;
            BuildManager = new ProjectBuildManager();
        }

        /// <summary>
        /// The MSBuild properties used when interpreting project files.
        /// These are the same properties that are passed to msbuild via the /property:&lt;n&gt;=&lt;v&gt; command line argument.
        /// </summary>
        public ImmutableDictionary<string, string> Properties => _properties;

        /// <summary>
        /// Determines if metadata from existing output assemblies is loaded instead of opening referenced projects.
        /// If the referenced project is already opened, the metadata will not be loaded.
        /// If the metadata assembly cannot be found the referenced project will be opened instead.
        /// </summary>
        public bool LoadMetadataForReferencedProjects { get; set; } = false;

        /// <summary>
        /// Determines if unrecognized projects are skipped when solutions or projects are opened.
        /// 
        /// A project is unrecognized if it either has 
        ///   a) an invalid file path, 
        ///   b) a non-existent project file,
        ///   c) has an unrecognized file extension or 
        ///   d) a file extension associated with an unsupported language.
        /// 
        /// If unrecognized projects cannot be skipped a corresponding exception is thrown.
        /// </summary>
        public bool SkipUnrecognizedProjects { get; set; } = true;

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

        private const string SolutionDirProperty = "SolutionDir";

        private void SetSolutionProperties(string solutionFilePath)
        {
            // When MSBuild is building an individual project, it doesn't define $(SolutionDir).
            // However when building an .sln file, or when working inside Visual Studio,
            // $(SolutionDir) is defined to be the directory where the .sln file is located.
            // Some projects out there rely on $(SolutionDir) being set (although the best practice is to
            // use MSBuildProjectDirectory which is always defined).
            if (!string.IsNullOrEmpty(solutionFilePath))
            {
                var solutionDirectory = Path.GetDirectoryName(solutionFilePath);
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

        /// <summary>
        /// Loads the <see cref="SolutionInfo"/> for the specified solution file, including all projects referenced by the solution file and 
        /// all the projects referenced by the project files.
        /// </summary>
        public async Task<SolutionInfo> LoadSolutionInfoAsync(
            string solutionFilePath,
            CancellationToken cancellationToken = default)
        {
            if (solutionFilePath == null)
            {
                throw new ArgumentNullException(nameof(solutionFilePath));
            }

            var absoluteSolutionPath = this.GetAbsoluteSolutionPath(solutionFilePath, Directory.GetCurrentDirectory());
            using (_dataGuard.DisposableWait(cancellationToken))
            {
                this.SetSolutionProperties(absoluteSolutionPath);
            }

            VersionStamp version = default;

            var solutionFile = MSB.Construction.SolutionFile.Parse(absoluteSolutionPath);
            var reportMode = this.SkipUnrecognizedProjects ? ReportMode.Log : ReportMode.Throw;

            var requestedProjectOptions = new ReportingOptions
            {
                OnPathFailure = reportMode,
                OnLoaderFailure = reportMode
            };

            var discoveredProjectOptions = new ReportingOptions
            {
                OnPathFailure = reportMode,
                OnLoaderFailure = reportMode
            };

            var projectPaths = new List<string>(capacity: solutionFile.ProjectsInOrder.Count);

            // load all the projects
            foreach (var project in solutionFile.ProjectsInOrder)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (project.ProjectType != MSB.Construction.SolutionProjectType.SolutionFolder)
                {
                    projectPaths.Add(project.RelativePath);
                }
            }

            var worker = new Worker(this,
                projectPaths.ToImmutableArray(),
                baseDirectory: Path.GetDirectoryName(absoluteSolutionPath),
                _properties,
                projectMap: null,
                requestedProjectOptions,
                discoveredProjectOptions,
                preferMetadataForReferencedProjects: false);

            var projects = await worker.LoadAsync(cancellationToken).ConfigureAwait(false);

            // construct workspace from loaded project infos
            return SolutionInfo.Create(
                SolutionId.CreateNewId(debugName: absoluteSolutionPath),
                version,
                absoluteSolutionPath,
                projects);
        }

        internal string GetAbsoluteSolutionPath(string path, string baseDirectory)
        {
            string absolutePath;

            try
            {
                absolutePath = GetAbsolutePath(path, baseDirectory);
            }
            catch (Exception)
            {
                throw new InvalidOperationException(string.Format(WorkspacesResources.Invalid_solution_file_path_colon_0, path));
            }

            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException(string.Format(WorkspacesResources.Solution_file_not_found_colon_0, absolutePath));
            }

            return absolutePath;
        }

        /// <summary>
        /// Loads the <see cref="ProjectInfo"/> from the specified project file and all referenced projects.
        /// The first <see cref="ProjectInfo"/> in the result corresponds to the specified project file.
        /// </summary>
        public async Task<ImmutableArray<ProjectInfo>> LoadProjectInfoAsync(
            string projectFilePath,
            ProjectMap projectMap = null,
            CancellationToken cancellationToken = default)
        {
            if (projectFilePath == null)
            {
                throw new ArgumentNullException(nameof(projectFilePath));
            }

            var requestedProjectOptions = new ReportingOptions
            {
                OnPathFailure = ReportMode.Throw,
                OnLoaderFailure = ReportMode.Throw
            };

            var discoveredMode = this.SkipUnrecognizedProjects ? ReportMode.Log : ReportMode.Throw;
            var discoveredProjectOptions = new ReportingOptions
            {
                OnPathFailure = discoveredMode,
                OnLoaderFailure = discoveredMode
            };

            var worker = new Worker(this,
                requestedProjectPaths: ImmutableArray.Create(projectFilePath),
                baseDirectory: Directory.GetCurrentDirectory(),
                globalProperties: _properties,
                projectMap,
                requestedProjectOptions,
                discoveredProjectOptions,
                this.LoadMetadataForReferencedProjects);

            return await worker.LoadAsync(cancellationToken).ConfigureAwait(false);
        }

        private static ImmutableArray<DocumentInfo> CreateDocumentInfos(IReadOnlyList<DocumentFileInfo> documentFileInfos, ProjectId projectId, Encoding encoding)
        {
            var results = ImmutableArray.CreateBuilder<DocumentInfo>();

            foreach (var info in documentFileInfos)
            {
                GetDocumentNameAndFolders(info.LogicalPath, out var name, out var folders);

                var documentInfo = DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId, debugName: info.FilePath),
                    name,
                    folders,
                    info.SourceCodeKind,
                    new FileTextLoader(info.FilePath, encoding),
                    info.FilePath,
                    info.IsGenerated);

                results.Add(documentInfo);
            }

            return results.ToImmutable();
        }

        private IEnumerable<AnalyzerReference> ResolveAnalyzerReferences(CommandLineArguments commandLineArgs)
        {
            var analyzerService = _workspace.Services.GetService<IAnalyzerService>();
            var analyzerLoader = analyzerService.GetLoader();

            foreach (var path in commandLineArgs.AnalyzerReferences.Select(r => r.FilePath))
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    analyzerLoader.AddDependencyLocation(fullPath);
                }
            }

            return commandLineArgs.ResolveAnalyzerReferences(analyzerLoader);
        }

        private static string GetMsbuildFailedMessage(string projectFilePath, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Format(WorkspaceMSBuildResources.Msbuild_failed_when_processing_the_file_0, projectFilePath);
            }
            else
            {
                return string.Format(WorkspaceMSBuildResources.Msbuild_failed_when_processing_the_file_0_with_message_1, projectFilePath, message);
            }
        }

        private static VersionStamp GetProjectVersion(string projectFilePath)
        {
            if (!string.IsNullOrEmpty(projectFilePath) && File.Exists(projectFilePath))
            {
                return VersionStamp.Create(File.GetLastWriteTimeUtc(projectFilePath));
            }
            else
            {
                return VersionStamp.Create();
            }
        }

        private static string GetAssemblyNameFromProjectPath(string projectFilePath)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(projectFilePath);

            // if this is still unreasonable, use a fixed name.
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                assemblyName = "assembly";
            }

            return assemblyName;
        }

        private static readonly char[] s_directorySplitChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

        private static void GetDocumentNameAndFolders(string logicalPath, out string name, out ImmutableArray<string> folders)
        {
            var pathNames = logicalPath.Split(s_directorySplitChars, StringSplitOptions.RemoveEmptyEntries);
            if (pathNames.Length > 0)
            {
                if (pathNames.Length > 1)
                {
                    folders = pathNames.Take(pathNames.Length - 1).ToImmutableArray();
                }
                else
                {
                    folders = ImmutableArray.Create<string>();
                }

                name = pathNames[pathNames.Length - 1];
            }
            else
            {
                name = logicalPath;
                folders = ImmutableArray.Create<string>();
            }
        }

        private void CheckForDuplicateDocuments(ImmutableArray<DocumentInfo> documents, ImmutableArray<DocumentInfo> additionalDocuments, string projectFilePath, ProjectId projectId)
        {
            var paths = new HashSet<string>();
            foreach (var doc in documents.Concat(additionalDocuments))
            {
                if (paths.Contains(doc.FilePath))
                {
                    var message = string.Format(WorkspacesResources.Duplicate_source_file_0_in_project_1, doc.FilePath, projectFilePath);
                    var diagnostic = new ProjectDiagnostic(WorkspaceDiagnosticKind.Warning, message, projectId);

                    _workspace.OnWorkspaceFailed(diagnostic);
                }

                paths.Add(doc.FilePath);
            }
        }

        private string TryGetAbsolutePath(string path, ReportMode mode)
        {
            try
            {
                path = Path.GetFullPath(path);
            }
            catch (Exception)
            {
                ReportFailure(mode, string.Format(WorkspacesResources.Invalid_project_file_path_colon_0, path));
                return null;
            }

            if (!File.Exists(path))
            {
                ReportFailure(
                    mode,
                    string.Format(WorkspacesResources.Project_file_not_found_colon_0, path),
                    msg => new FileNotFoundException(msg));
                return null;
            }

            return path;
        }

        internal bool TryGetLoaderFromProjectPath(string projectFilePath, out IProjectFileLoader loader)
        {
            return TryGetLoaderFromProjectPath(projectFilePath, ReportMode.Ignore, out loader);
        }

        private bool TryGetLoaderFromProjectPath(string projectFilePath, ReportMode mode, out IProjectFileLoader loader)
        {
            using (_dataGuard.DisposableWait())
            {
                // otherwise try to figure it out from extension
                var extension = Path.GetExtension(projectFilePath);
                if (extension.Length > 0 && extension[0] == '.')
                {
                    extension = extension.Substring(1);
                }

                if (_extensionToLanguageMap.TryGetValue(extension, out var language))
                {
                    if (_workspace.Services.SupportedLanguages.Contains(language))
                    {
                        loader = _workspace.Services.GetLanguageServices(language).GetService<IProjectFileLoader>();
                    }
                    else
                    {
                        loader = null;
                        this.ReportFailure(mode, string.Format(WorkspacesResources.Cannot_open_project_0_because_the_language_1_is_not_supported, projectFilePath, language));
                        return false;
                    }
                }
                else
                {
                    loader = ProjectFileLoader.GetLoaderForProjectFileExtension(_workspace, extension);

                    if (loader == null)
                    {
                        this.ReportFailure(mode, string.Format(WorkspacesResources.Cannot_open_project_0_because_the_file_extension_1_is_not_associated_with_a_language, projectFilePath, Path.GetExtension(projectFilePath)));
                        return false;
                    }
                }

                // since we have both C# and VB loaders in this same library, it no longer indicates whether we have full language support available.
                if (loader != null)
                {
                    language = loader.Language;

                    // check for command line parser existing... if not then error.
                    var commandLineParser = _workspace.Services.GetLanguageServices(language).GetService<ICommandLineParserService>();
                    if (commandLineParser == null)
                    {
                        loader = null;
                        this.ReportFailure(mode, string.Format(WorkspacesResources.Cannot_open_project_0_because_the_language_1_is_not_supported, projectFilePath, language));
                        return false;
                    }
                }

                return loader != null;
            }
        }

        private bool TryGetAbsoluteProjectPath(string path, string baseDirectory, ReportMode mode, out string absolutePath)
        {
            try
            {
                absolutePath = GetAbsolutePath(path, baseDirectory);
            }
            catch (Exception)
            {
                ReportFailure(mode, string.Format(WorkspacesResources.Invalid_project_file_path_colon_0, path));
                absolutePath = null;
                return false;
            }

            if (!File.Exists(absolutePath))
            {
                ReportFailure(
                    mode,
                    string.Format(WorkspacesResources.Project_file_not_found_colon_0, absolutePath),
                    msg => new FileNotFoundException(msg));
                return false;
            }

            return true;
        }

        private static string GetAbsolutePath(string path, string baseDirectoryPath)
        {
            return Path.GetFullPath(FileUtilities.ResolveRelativePath(path, baseDirectoryPath) ?? path);
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
                    _workspace.OnWorkspaceFailed(new WorkspaceDiagnostic(WorkspaceDiagnosticKind.Failure, message));
                    break;

                case ReportMode.Ignore:
                default:
                    break;
            }
        }

        private void ReportDiagnosticLog(DiagnosticLog log)
        {
            foreach (var logItem in log)
            {
                ReportFailure(ReportMode.Log, GetMsbuildFailedMessage(logItem.ProjectFilePath, logItem.ToString()));
            }
        }

        private bool IsSupportedLanguage(string languageName)
            => _workspace.Services.SupportedLanguages.Contains(languageName);

        private TLanguageService GetLanguageService<TLanguageService>(string languageName)
            where TLanguageService : ILanguageService
            => _workspace.Services
                .GetLanguageServices(languageName)
                .GetService<TLanguageService>();

        private TWorkspaceService GetWorkspaceService<TWorkspaceService>()
            where TWorkspaceService : IWorkspaceService
            => _workspace.Services
                .GetService<TWorkspaceService>();
    }
}
