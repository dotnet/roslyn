// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Logging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis
{
    using SolutionStatePair = StrongBox<(SolutionState, SolutionCompilationState)>;

    /// <summary>
    /// Represents a set of projects and their source code documents.
    ///
    /// this is a green node of Solution like ProjectState/DocumentState are for
    /// Project and Document.
    /// </summary>
    internal partial class SolutionState
    {
        // the version of the workspace this solution is from
        public int WorkspaceVersion { get; }
        public string? WorkspaceKind { get; }
        public SolutionServices Services { get; }
        public SolutionOptionSet Options { get; }
        public IReadOnlyList<AnalyzerReference> AnalyzerReferences { get; }

        private readonly SolutionInfo.SolutionAttributes _solutionAttributes;
        private readonly ImmutableDictionary<ProjectId, ProjectState> _projectIdToProjectStateMap;
        private readonly ImmutableDictionary<string, ImmutableArray<DocumentId>> _filePathToDocumentIdsMap;
        private readonly ProjectDependencyGraph _dependencyGraph;

        // holds on data calculated based on the AnalyzerReferences list
        private readonly Lazy<HostDiagnosticAnalyzers> _lazyAnalyzers;

        private SolutionState(
            string? workspaceKind,
            int workspaceVersion,
            SolutionServices services,
            SolutionInfo.SolutionAttributes solutionAttributes,
            IReadOnlyList<ProjectId> projectIds,
            SolutionOptionSet options,
            IReadOnlyList<AnalyzerReference> analyzerReferences,
            ImmutableDictionary<ProjectId, ProjectState> idToProjectStateMap,
            ImmutableDictionary<string, ImmutableArray<DocumentId>> filePathToDocumentIdsMap,
            ProjectDependencyGraph dependencyGraph,
            Lazy<HostDiagnosticAnalyzers>? lazyAnalyzers)
        {
            WorkspaceKind = workspaceKind;
            WorkspaceVersion = workspaceVersion;
            _solutionAttributes = solutionAttributes;
            Services = services;
            ProjectIds = projectIds;
            Options = options;
            AnalyzerReferences = analyzerReferences;
            _projectIdToProjectStateMap = idToProjectStateMap;
            _filePathToDocumentIdsMap = filePathToDocumentIdsMap;
            _dependencyGraph = dependencyGraph;
            _lazyAnalyzers = lazyAnalyzers ?? CreateLazyHostDiagnosticAnalyzers(analyzerReferences);

            CheckInvariants();

            // make sure we don't accidentally capture any state but the list of references:
            static Lazy<HostDiagnosticAnalyzers> CreateLazyHostDiagnosticAnalyzers(IReadOnlyList<AnalyzerReference> analyzerReferences)
                => new(() => new HostDiagnosticAnalyzers(analyzerReferences));
        }

        public SolutionState(
            string? workspaceKind,
            SolutionServices services,
            SolutionInfo.SolutionAttributes solutionAttributes,
            SolutionOptionSet options,
            IReadOnlyList<AnalyzerReference> analyzerReferences)
            : this(
                workspaceKind,
                workspaceVersion: 0,
                services,
                solutionAttributes,
                projectIds: SpecializedCollections.EmptyBoxedImmutableArray<ProjectId>(),
                options,
                analyzerReferences,
                idToProjectStateMap: ImmutableDictionary<ProjectId, ProjectState>.Empty,
                filePathToDocumentIdsMap: ImmutableDictionary.Create<string, ImmutableArray<DocumentId>>(StringComparer.OrdinalIgnoreCase),
                dependencyGraph: ProjectDependencyGraph.Empty,
                lazyAnalyzers: null)
        {
        }

        public HostDiagnosticAnalyzers Analyzers => _lazyAnalyzers.Value;

        public SolutionInfo.SolutionAttributes SolutionAttributes => _solutionAttributes;

        public ImmutableDictionary<ProjectId, ProjectState> ProjectStates => _projectIdToProjectStateMap;

        /// <summary>
        /// The Id of the solution. Multiple solution instances may share the same Id.
        /// </summary>
        public SolutionId Id => _solutionAttributes.Id;

        /// <summary>
        /// The path to the solution file or null if there is no solution file.
        /// </summary>
        public string? FilePath => _solutionAttributes.FilePath;

        /// <summary>
        /// The solution version. This equates to the solution file's version.
        /// </summary>
        public VersionStamp Version => _solutionAttributes.Version;

        /// <summary>
        /// A list of all the ids for all the projects contained by the solution.
        /// </summary>
        public IReadOnlyList<ProjectId> ProjectIds { get; }

        private void CheckInvariants()
        {
            // Run these quick checks all the time.  We need to know immediately if we violate these.
            Contract.ThrowIfFalse(_projectIdToProjectStateMap.Count == ProjectIds.Count);
            Contract.ThrowIfFalse(_projectIdToProjectStateMap.Count == _dependencyGraph.ProjectIds.Count);

            // Only run this in debug builds; even the .SetEquals() call across all projects can be expensive when there's a lot of them.
#if DEBUG
            // project ids must be the same:
            Debug.Assert(_projectIdToProjectStateMap.Keys.SetEquals(ProjectIds));
            Debug.Assert(_projectIdToProjectStateMap.Keys.SetEquals(_dependencyGraph.ProjectIds));
#endif
        }

        private SolutionState Branch(
            SolutionInfo.SolutionAttributes? solutionAttributes = null,
            IReadOnlyList<ProjectId>? projectIds = null,
            SolutionOptionSet? options = null,
            IReadOnlyList<AnalyzerReference>? analyzerReferences = null,
            ImmutableDictionary<ProjectId, ProjectState>? idToProjectStateMap = null,
            ImmutableDictionary<string, ImmutableArray<DocumentId>>? filePathToDocumentIdsMap = null,
            ProjectDependencyGraph? dependencyGraph = null)
        {
            solutionAttributes ??= _solutionAttributes;
            projectIds ??= ProjectIds;
            idToProjectStateMap ??= _projectIdToProjectStateMap;
            options ??= Options;
            analyzerReferences ??= AnalyzerReferences;
            filePathToDocumentIdsMap ??= _filePathToDocumentIdsMap;
            dependencyGraph ??= _dependencyGraph;

            var analyzerReferencesEqual = AnalyzerReferences.SequenceEqual(analyzerReferences);

            if (solutionAttributes == _solutionAttributes &&
                projectIds == ProjectIds &&
                options == Options &&
                analyzerReferencesEqual &&
                idToProjectStateMap == _projectIdToProjectStateMap &&
                filePathToDocumentIdsMap == _filePathToDocumentIdsMap &&
                dependencyGraph == _dependencyGraph)
            {
                return this;
            }

            return new SolutionState(
                WorkspaceKind,
                WorkspaceVersion,
                Services,
                solutionAttributes,
                projectIds,
                options,
                analyzerReferences,
                idToProjectStateMap,
                filePathToDocumentIdsMap,
                dependencyGraph,
                analyzerReferencesEqual ? _lazyAnalyzers : null);
        }

        /// <summary>
        /// Updates the solution with specified workspace kind, workspace version and services.
        /// This implicitly also changes the value of <see cref="Solution.Workspace"/> for this solution,
        /// since that is extracted from <see cref="SolutionServices"/> for backwards compatibility.
        /// </summary>
        public SolutionState WithNewWorkspace(
            string? workspaceKind,
            int workspaceVersion,
            SolutionServices services)
        {
            if (workspaceKind == WorkspaceKind &&
                workspaceVersion == WorkspaceVersion &&
                services == Services)
            {
                return this;
            }

            // Note: this will potentially have problems if the workspace services are different, as some services
            // get locked-in by document states and project states when first constructed.
            return new SolutionState(
                workspaceKind,
                workspaceVersion,
                services,
                _solutionAttributes,
                ProjectIds,
                Options,
                AnalyzerReferences,
                _projectIdToProjectStateMap,
                _filePathToDocumentIdsMap,
                _dependencyGraph,
                _lazyAnalyzers);
        }

        /// <summary>
        /// The version of the most recently modified project.
        /// </summary>
        public VersionStamp GetLatestProjectVersion()
        {
            // this may produce a version that is out of sync with the actual Document versions.
            var latestVersion = VersionStamp.Default;
            foreach (var project in this.ProjectStates.Values)
            {
                latestVersion = project.Version.GetNewerVersion(latestVersion);
            }

            return latestVersion;
        }

        /// <summary>
        /// True if the solution contains a project with the specified project ID.
        /// </summary>
        public bool ContainsProject([NotNullWhen(returnValue: true)] ProjectId? projectId)
            => projectId != null && _projectIdToProjectStateMap.ContainsKey(projectId);

        /// <summary>
        /// True if the solution contains the document in one of its projects
        /// </summary>
        public bool ContainsDocument([NotNullWhen(returnValue: true)] DocumentId? documentId)
        {
            return
                documentId != null &&
                this.ContainsProject(documentId.ProjectId) &&
                this.GetProjectState(documentId.ProjectId)!.DocumentStates.Contains(documentId);
        }

        /// <summary>
        /// True if the solution contains the additional document in one of its projects
        /// </summary>
        public bool ContainsAdditionalDocument([NotNullWhen(returnValue: true)] DocumentId? documentId)
        {
            return
                documentId != null &&
                this.ContainsProject(documentId.ProjectId) &&
                this.GetProjectState(documentId.ProjectId)!.AdditionalDocumentStates.Contains(documentId);
        }

        /// <summary>
        /// True if the solution contains the analyzer config document in one of its projects
        /// </summary>
        public bool ContainsAnalyzerConfigDocument([NotNullWhen(returnValue: true)] DocumentId? documentId)
        {
            return
                documentId != null &&
                this.ContainsProject(documentId.ProjectId) &&
                this.GetProjectState(documentId.ProjectId)!.AnalyzerConfigDocumentStates.Contains(documentId);
        }

        private DocumentState GetRequiredDocumentState(DocumentId documentId)
            => GetRequiredProjectState(documentId.ProjectId).DocumentStates.GetRequiredState(documentId);

        private AdditionalDocumentState GetRequiredAdditionalDocumentState(DocumentId documentId)
            => GetRequiredProjectState(documentId.ProjectId).AdditionalDocumentStates.GetRequiredState(documentId);

        private AnalyzerConfigDocumentState GetRequiredAnalyzerConfigDocumentState(DocumentId documentId)
            => GetRequiredProjectState(documentId.ProjectId).AnalyzerConfigDocumentStates.GetRequiredState(documentId);

        public ProjectState? GetProjectState(ProjectId projectId)
        {
            _projectIdToProjectStateMap.TryGetValue(projectId, out var state);
            return state;
        }

        public ProjectState GetRequiredProjectState(ProjectId projectId)
        {
            var result = GetProjectState(projectId);
            Contract.ThrowIfNull(result);
            return result;
        }

        private SolutionState AddProject(ProjectState projectState)
        {
            var projectId = projectState.Id;

            // changed project list so, increment version.
            var newSolutionAttributes = _solutionAttributes.With(version: Version.GetNewerVersion());

            var newProjectIds = ProjectIds.ToImmutableArray().Add(projectId);
            var newStateMap = _projectIdToProjectStateMap.Add(projectId, projectState);

            var newDependencyGraph = _dependencyGraph
                .WithAdditionalProject(projectId)
                .WithAdditionalProjectReferences(projectId, projectState.ProjectReferences);

            // It's possible that another project already in newStateMap has a reference to this project that we're adding, since we allow
            // dangling references like that. If so, we'll need to link those in too.
            foreach (var newState in newStateMap)
            {
                foreach (var projectReference in newState.Value.ProjectReferences)
                {
                    if (projectReference.ProjectId == projectId)
                    {
                        newDependencyGraph = newDependencyGraph.WithAdditionalProjectReferences(
                            newState.Key,
                            SpecializedCollections.SingletonReadOnlyList(projectReference));

                        break;
                    }
                }
            }

            var newFilePathToDocumentIdsMap = CreateFilePathToDocumentIdsMapWithAddedDocuments(GetDocumentStates(newStateMap[projectId]));

            return Branch(
                solutionAttributes: newSolutionAttributes,
                projectIds: newProjectIds,
                idToProjectStateMap: newStateMap,
                filePathToDocumentIdsMap: newFilePathToDocumentIdsMap,
                dependencyGraph: newDependencyGraph);
        }

        /// <summary>
        /// Create a new solution instance that includes a project with the specified project information.
        /// </summary>
        public SolutionState AddProject(ProjectInfo projectInfo)
        {
            if (projectInfo == null)
            {
                throw new ArgumentNullException(nameof(projectInfo));
            }

            var projectId = projectInfo.Id;

            var language = projectInfo.Language;
            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            var displayName = projectInfo.Name;
            if (displayName == null)
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            CheckNotContainsProject(projectId);

            var languageServices = Services.GetLanguageServices(language);
            if (languageServices == null)
            {
                throw new ArgumentException(string.Format(WorkspacesResources.The_language_0_is_not_supported, language));
            }

            var newProject = new ProjectState(languageServices, projectInfo);

            return this.AddProject(newProject);
        }

        public ImmutableDictionary<string, ImmutableArray<DocumentId>> CreateFilePathToDocumentIdsMapWithAddedDocuments(IEnumerable<TextDocumentState> documentStates)
        {
            var builder = _filePathToDocumentIdsMap.ToBuilder();

            foreach (var documentState in documentStates)
            {
                var filePath = documentState.FilePath;

                if (RoslynString.IsNullOrEmpty(filePath))
                {
                    continue;
                }

                builder.MultiAdd(filePath, documentState.Id);
            }

            return builder.ToImmutable();
        }

        private static IEnumerable<TextDocumentState> GetDocumentStates(ProjectState projectState)
            => projectState.DocumentStates.States.Values
                   .Concat<TextDocumentState>(projectState.AdditionalDocumentStates.States.Values)
                   .Concat(projectState.AnalyzerConfigDocumentStates.States.Values);

        /// <summary>
        /// Create a new solution instance without the project specified.
        /// </summary>
        public SolutionState RemoveProject(ProjectId projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            CheckContainsProject(projectId);

            // changed project list so, increment version.
            var newSolutionAttributes = _solutionAttributes.With(version: this.Version.GetNewerVersion());

            var newProjectIds = ProjectIds.ToImmutableArray().Remove(projectId);
            var newStateMap = _projectIdToProjectStateMap.Remove(projectId);
            var newDependencyGraph = _dependencyGraph.WithProjectRemoved(projectId);
            var newFilePathToDocumentIdsMap = CreateFilePathToDocumentIdsMapWithRemovedDocuments(GetDocumentStates(_projectIdToProjectStateMap[projectId]));

            return this.Branch(
                solutionAttributes: newSolutionAttributes,
                projectIds: newProjectIds,
                idToProjectStateMap: newStateMap,
                filePathToDocumentIdsMap: newFilePathToDocumentIdsMap,
                dependencyGraph: newDependencyGraph);
        }

        public ImmutableDictionary<string, ImmutableArray<DocumentId>> CreateFilePathToDocumentIdsMapWithRemovedDocuments(IEnumerable<TextDocumentState> documentStates)
        {
            var builder = _filePathToDocumentIdsMap.ToBuilder();

            foreach (var documentState in documentStates)
            {
                var filePath = documentState.FilePath;

                if (RoslynString.IsNullOrEmpty(filePath))
                {
                    continue;
                }

                if (!builder.TryGetValue(filePath, out var documentIdsWithPath) || !documentIdsWithPath.Contains(documentState.Id))
                {
                    throw new ArgumentException($"The given documentId was not found in '{nameof(_filePathToDocumentIdsMap)}'.");
                }

                builder.MultiRemove(filePath, documentState.Id);
            }

            return builder.ToImmutable();
        }

        private ImmutableDictionary<string, ImmutableArray<DocumentId>> CreateFilePathToDocumentIdsMapWithFilePath(DocumentId documentId, string? oldFilePath, string? newFilePath)
        {
            if (oldFilePath == newFilePath)
            {
                return _filePathToDocumentIdsMap;
            }

            var builder = _filePathToDocumentIdsMap.ToBuilder();

            if (!RoslynString.IsNullOrEmpty(oldFilePath))
            {
                builder.MultiRemove(oldFilePath, documentId);
            }

            if (!RoslynString.IsNullOrEmpty(newFilePath))
            {
                builder.MultiAdd(newFilePath, documentId);
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the new
        /// assembly name.
        /// </summary>
        public (SolutionState, ProjectState) WithProjectAssemblyName(ProjectId projectId, string assemblyName)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithAssemblyName(assemblyName);

            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the output file path.
        /// </summary>
        public (SolutionState, ProjectState) WithProjectOutputFilePath(ProjectId projectId, string? outputFilePath)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithOutputFilePath(outputFilePath);

            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the output file path.
        /// </summary>
        public (SolutionState, ProjectState) WithProjectOutputRefFilePath(ProjectId projectId, string? outputRefFilePath)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithOutputRefFilePath(outputRefFilePath);

            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the compiler output file path.
        /// </summary>
        public (SolutionState, ProjectState) WithProjectCompilationOutputInfo(ProjectId projectId, in CompilationOutputInfo info)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithCompilationOutputInfo(info);

            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the default namespace.
        /// </summary>
        public (SolutionState, ProjectState) WithProjectDefaultNamespace(ProjectId projectId, string? defaultNamespace)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithDefaultNamespace(defaultNamespace);

            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the name.
        /// </summary>
        public (SolutionState, ProjectState) WithProjectChecksumAlgorithm(ProjectId projectId, SourceHashAlgorithm checksumAlgorithm)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithChecksumAlgorithm(checksumAlgorithm);

            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the name.
        /// </summary>
        public (SolutionState, ProjectState) WithProjectName(ProjectId projectId, string name)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithName(name);

            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the project file path.
        /// </summary>
        public (SolutionState, ProjectState) WithProjectFilePath(ProjectId projectId, string? filePath)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithFilePath(filePath);

            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified compilation options.
        /// </summary>
        public (SolutionState, ProjectState) WithProjectCompilationOptions(ProjectId projectId, CompilationOptions options)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithCompilationOptions(options);

            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified parse options.
        /// </summary>
        public (SolutionState, ProjectState) WithProjectParseOptions(ProjectId projectId, ParseOptions options)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithParseOptions(options);

            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified hasAllInformation.
        /// </summary>
        public (SolutionState, ProjectState) WithHasAllInformation(ProjectId projectId, bool hasAllInformation)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithHasAllInformation(hasAllInformation);

            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            // fork without any change on compilation.
            return ForkProject(newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified runAnalyzers.
        /// </summary>
        public (SolutionState, ProjectState) WithRunAnalyzers(ProjectId projectId, bool runAnalyzers)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithRunAnalyzers(runAnalyzers);

            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            // fork without any change on compilation.
            return ForkProject(newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include
        /// the specified project references.
        /// </summary>
        public (SolutionState, ProjectState) AddProjectReferences(ProjectId projectId, IReadOnlyCollection<ProjectReference> projectReferences)
        {
            var oldProject = GetRequiredProjectState(projectId);
            if (projectReferences.Count == 0)
            {
                return (this, oldProject);
            }

            var oldReferences = oldProject.ProjectReferences.ToImmutableArray();
            var newReferences = oldReferences.AddRange(projectReferences);

            var newProject = oldProject.WithProjectReferences(newReferences);
            var newDependencyGraph = _dependencyGraph.WithAdditionalProjectReferences(projectId, projectReferences);

            return ForkProject(newProject, newDependencyGraph: newDependencyGraph);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to no longer
        /// include the specified project reference.
        /// </summary>
        public (SolutionState, ProjectState) RemoveProjectReference(ProjectId projectId, ProjectReference projectReference)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var oldReferences = oldProject.ProjectReferences.ToImmutableArray();

            // Note: uses ProjectReference equality to compare references.
            var newReferences = oldReferences.Remove(projectReference);

            if (oldReferences == newReferences)
            {
                return (this, oldProject);
            }

            var newProject = oldProject.WithProjectReferences(newReferences);

            ProjectDependencyGraph newDependencyGraph;
            if (newProject.ContainsReferenceToProject(projectReference.ProjectId) ||
                !_projectIdToProjectStateMap.ContainsKey(projectReference.ProjectId))
            {
                // Two cases:
                // 1) The project contained multiple non-equivalent references to the project,
                // and not all of them were removed. The dependency graph doesn't change.
                // Note that there might be two references to the same project, one with
                // extern alias and the other without. These are not considered duplicates.
                // 2) The referenced project is not part of the solution and hence not included
                // in the dependency graph.
                newDependencyGraph = _dependencyGraph;
            }
            else
            {
                newDependencyGraph = _dependencyGraph.WithProjectReferenceRemoved(projectId, projectReference.ProjectId);
            }

            return ForkProject(newProject, newDependencyGraph: newDependencyGraph);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to contain
        /// the specified list of project references.
        /// </summary>
        public (SolutionState, ProjectState) WithProjectReferences(ProjectId projectId, IReadOnlyList<ProjectReference> projectReferences)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithProjectReferences(projectReferences);
            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            var newDependencyGraph = _dependencyGraph.WithProjectReferences(projectId, projectReferences);
            return ForkProject(newProject, newDependencyGraph: newDependencyGraph);
        }

        /// <summary>
        /// Creates a new solution instance with the project documents in the order by the specified document ids.
        /// The specified document ids must be the same as what is already in the project; no adding or removing is allowed.
        /// </summary>
        public (SolutionState, ProjectState) WithProjectDocumentsOrder(ProjectId projectId, ImmutableList<DocumentId> documentIds)
        {
            var oldProject = GetRequiredProjectState(projectId);

            if (documentIds.Count != oldProject.DocumentStates.Count)
            {
                throw new ArgumentException($"The specified documents do not equal the project document count.", nameof(documentIds));
            }

            foreach (var id in documentIds)
            {
                if (!oldProject.DocumentStates.Contains(id))
                {
                    throw new InvalidOperationException($"The document '{id}' does not exist in the project.");
                }
            }

            var newProject = oldProject.UpdateDocumentsOrder(documentIds);

            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the
        /// specified metadata references.
        /// </summary>
        public (SolutionState, ProjectState) AddMetadataReferences(ProjectId projectId, IReadOnlyCollection<MetadataReference> metadataReferences)
        {
            var oldProject = GetRequiredProjectState(projectId);
            if (metadataReferences.Count == 0)
            {
                return (this, oldProject);
            }

            var oldReferences = oldProject.MetadataReferences.ToImmutableArray();
            var newReferences = oldReferences.AddRange(metadataReferences);

            return ForkProject(oldProject.WithMetadataReferences(newReferences));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to no longer include
        /// the specified metadata reference.
        /// </summary>
        public (SolutionState, ProjectState) RemoveMetadataReference(ProjectId projectId, MetadataReference metadataReference)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var oldReferences = oldProject.MetadataReferences.ToImmutableArray();
            var newReferences = oldReferences.Remove(metadataReference);
            if (oldReferences == newReferences)
            {
                return (this, oldProject);
            }

            return ForkProject(oldProject.WithMetadataReferences(newReferences));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include only the
        /// specified metadata references.
        /// </summary>
        public (SolutionState, ProjectState) WithProjectMetadataReferences(ProjectId projectId, IReadOnlyList<MetadataReference> metadataReferences)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithMetadataReferences(metadataReferences);
            if (oldProject == newProject)
            {
                return (this, oldProject);
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the
        /// specified analyzer references.
        /// </summary>
        public (SolutionState, ProjectState oldProject, ProjectState newProject) AddAnalyzerReferences(ProjectId projectId, ImmutableArray<AnalyzerReference> analyzerReferences)
        {
            var oldProject = GetRequiredProjectState(projectId);
            if (analyzerReferences.Length == 0)
            {
                return (this, oldProject, oldProject);
            }

            var oldReferences = oldProject.AnalyzerReferences.ToImmutableArray();
            var newReferences = oldReferences.AddRange(analyzerReferences);

            var (newState, newProject) = ForkProject(oldProject.WithAnalyzerReferences(newReferences));
            return (newState, oldProject, newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to no longer include
        /// the specified analyzer reference.
        /// </summary>
        public (SolutionState, ProjectState oldProject, ProjectState newProject) RemoveAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var oldReferences = oldProject.AnalyzerReferences.ToImmutableArray();
            var newReferences = oldReferences.Remove(analyzerReference);
            if (oldReferences == newReferences)
            {
                return (this, oldProject, oldProject);
            }

            var (newState, newProject) = ForkProject(oldProject.WithAnalyzerReferences(newReferences));
            return (newState, oldProject, newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include only the
        /// specified analyzer references.
        /// </summary>
        public (SolutionState, ProjectState oldProjectState, ProjectState newProjectState) WithProjectAnalyzerReferences(ProjectId projectId, IReadOnlyList<AnalyzerReference> analyzerReferences)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithAnalyzerReferences(analyzerReferences);
            if (oldProject == newProject)
            {
                return (this, oldProject, newProject);
            }

            (var newState, newProject) = ForkProject(newProject);
            return (newState, oldProject, newProject);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the specified name.
        /// </summary>
        public (SolutionState, ProjectState oldProjectState, ProjectState newProjectState) WithDocumentName(DocumentId documentId, string name)
        {
            var oldDocument = GetRequiredDocumentState(documentId);
            if (oldDocument.Attributes.Name == name)
            {
                var oldProject = GetRequiredProjectState(documentId.ProjectId);
                return (this, oldProject, oldProject);
            }

            return UpdateDocumentState(oldDocument.UpdateName(name), contentChanged: false);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to be contained in
        /// the sequence of logical folders.
        /// </summary>
        public (SolutionState, ProjectState oldProjectState, ProjectState newProjectState) WithDocumentFolders(DocumentId documentId, IReadOnlyList<string> folders)
        {
            var oldDocument = GetRequiredDocumentState(documentId);
            if (oldDocument.Folders.SequenceEqual(folders))
            {
                var oldProject = GetRequiredProjectState(documentId.ProjectId);
                return (this, oldProject, oldProject);
            }

            return UpdateDocumentState(oldDocument.UpdateFolders(folders), contentChanged: false);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the specified file path.
        /// </summary>
        public (SolutionState, ProjectState oldProjectState, ProjectState newProjectState) WithDocumentFilePath(DocumentId documentId, string? filePath)
        {
            var oldDocument = GetRequiredDocumentState(documentId);
            if (oldDocument.FilePath == filePath)
            {
                var oldProject = GetRequiredProjectState(documentId.ProjectId);
                return (this, oldProject, oldProject);
            }

            return UpdateDocumentState(oldDocument.UpdateFilePath(filePath), contentChanged: false);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the text
        /// specified.
        /// </summary>
        public (SolutionState, ProjectState oldProjectState, ProjectState newProjectState) WithDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var oldDocument = GetRequiredDocumentState(documentId);
            if (oldDocument.TryGetText(out var oldText) && text == oldText)
            {
                var oldProject = GetRequiredProjectState(documentId.ProjectId);
                return (this, oldProject, oldProject);
            }

            return UpdateDocumentState(oldDocument.UpdateText(text, mode), contentChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the additional document specified updated to have the text
        /// specified.
        /// </summary>
        public (SolutionState, ProjectState oldProjectState, ProjectState newProjectState) WithAdditionalDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var oldDocument = GetRequiredAdditionalDocumentState(documentId);
            if (oldDocument.TryGetText(out var oldText) && text == oldText)
            {
                var oldProject = GetRequiredProjectState(documentId.ProjectId);
                return (this, oldProject, oldProject);
            }

            return UpdateAdditionalDocumentState(oldDocument.UpdateText(text, mode), contentChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the text
        /// specified.
        /// </summary>
        public (SolutionState, ProjectState newProjectState) WithAnalyzerConfigDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var oldDocument = GetRequiredAnalyzerConfigDocumentState(documentId);
            if (oldDocument.TryGetText(out var oldText) && text == oldText)
            {
                var oldProject = GetRequiredProjectState(documentId.ProjectId);
                return (this, oldProject);
            }

            return UpdateAnalyzerConfigDocumentState(oldDocument.UpdateText(text, mode));
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the text
        /// and version specified.
        /// </summary>
        public (SolutionState, ProjectState oldProjectState, ProjectState newProjectState) WithDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var oldDocument = GetRequiredDocumentState(documentId);
            if (oldDocument.TryGetTextAndVersion(out var oldTextAndVersion) && textAndVersion == oldTextAndVersion)
            {
                var oldProject = GetRequiredProjectState(documentId.ProjectId);
                return (this, oldProject, oldProject);
            }

            return UpdateDocumentState(oldDocument.UpdateText(textAndVersion, mode), contentChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the additional document specified updated to have the text
        /// and version specified.
        /// </summary>
        public (SolutionState, ProjectState oldProjectState, ProjectState newProjectState) WithAdditionalDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var oldDocument = GetRequiredAdditionalDocumentState(documentId);
            if (oldDocument.TryGetTextAndVersion(out var oldTextAndVersion) && textAndVersion == oldTextAndVersion)
            {
                var oldProject = GetRequiredProjectState(documentId.ProjectId);
                return (this, oldProject, oldProject);
            }

            return UpdateAdditionalDocumentState(oldDocument.UpdateText(textAndVersion, mode), contentChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the analyzer config document specified updated to have the text
        /// and version specified.
        /// </summary>
        public (SolutionState, ProjectState newProjectState) WithAnalyzerConfigDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var oldDocument = GetRequiredAnalyzerConfigDocumentState(documentId);
            if (oldDocument.TryGetTextAndVersion(out var oldTextAndVersion) && textAndVersion == oldTextAndVersion)
            {
                var oldProject = GetRequiredProjectState(documentId.ProjectId);
                return (this, oldProject);
            }

            return UpdateAnalyzerConfigDocumentState(oldDocument.UpdateText(textAndVersion, mode));
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have a syntax tree
        /// rooted by the specified syntax node.
        /// </summary>
        public (SolutionState, ProjectState oldProjectState, ProjectState newProjectState) WithDocumentSyntaxRoot(DocumentId documentId, SyntaxNode root, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var oldDocument = GetRequiredDocumentState(documentId);
            if (oldDocument.TryGetSyntaxTree(out var oldTree) &&
                oldTree.TryGetRoot(out var oldRoot) &&
                oldRoot == root)
            {
                var oldProject = GetRequiredProjectState(documentId.ProjectId);
                return (this, oldProject, oldProject);
            }

            return UpdateDocumentState(oldDocument.UpdateTree(root, mode), contentChanged: true);
        }

        public (SolutionState, ProjectState oldProjectState, ProjectState newProjectState) WithDocumentContentsFrom(DocumentId documentId, DocumentState documentState)
        {
            var oldDocument = GetRequiredDocumentState(documentId);
            var oldProject = GetRequiredProjectState(documentId.ProjectId);
            if (oldDocument == documentState)
                return (this, oldProject, oldProject);

            if (oldDocument.TextAndVersionSource == documentState.TextAndVersionSource &&
                oldDocument.TreeSource == documentState.TreeSource)
            {
                return (this, oldProject, oldProject);
            }

            return UpdateDocumentState(
                oldDocument.UpdateTextAndTreeContents(documentState.TextAndVersionSource, documentState.TreeSource),
                contentChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the source
        /// code kind specified.
        /// </summary>
        public (SolutionState, ProjectState oldProjectState, ProjectState newProjectState) WithDocumentSourceCodeKind(DocumentId documentId, SourceCodeKind sourceCodeKind)
        {
            var oldDocument = GetRequiredDocumentState(documentId);
            if (oldDocument.SourceCodeKind == sourceCodeKind)
            {
                var oldProject = GetRequiredProjectState(documentId.ProjectId);
                return (this, oldProject, oldProject);
            }

            return UpdateDocumentState(oldDocument.UpdateSourceCodeKind(sourceCodeKind), contentChanged: true);
        }

        public (SolutionState, ProjectState oldProjectState, ProjectState newProjectState) UpdateDocumentTextLoader(DocumentId documentId, TextLoader loader, PreservationMode mode)
        {
            var oldDocument = GetRequiredDocumentState(documentId);

            // Assumes that content has changed. User could have closed a doc without saving and we are loading text
            // from closed file with old content.
            return UpdateDocumentState(oldDocument.UpdateText(loader, mode), contentChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the additional document specified updated to have the text
        /// supplied by the text loader.
        /// </summary>
        public (SolutionState, ProjectState oldProjectState, ProjectState newProjectState) UpdateAdditionalDocumentTextLoader(DocumentId documentId, TextLoader loader, PreservationMode mode)
        {
            var oldDocument = GetRequiredAdditionalDocumentState(documentId);

            // Assumes that content has changed. User could have closed a doc without saving and we are loading text
            // from closed file with old content.
            return UpdateAdditionalDocumentState(oldDocument.UpdateText(loader, mode), contentChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the analyzer config document specified updated to have the text
        /// supplied by the text loader.
        /// </summary>
        public (SolutionState, ProjectState newProjectState) UpdateAnalyzerConfigDocumentTextLoader(DocumentId documentId, TextLoader loader, PreservationMode mode)
        {
            var oldDocument = GetRequiredAnalyzerConfigDocumentState(documentId);

            // Assumes that text has changed. User could have closed a doc without saving and we are loading text from closed file with
            // old content. Also this should make sure we don't re-use latest doc version with data associated with opened document.
            return UpdateAnalyzerConfigDocumentState(oldDocument.UpdateText(loader, mode));
        }

        private (SolutionState, ProjectState oldState, ProjectState newState) UpdateDocumentState(DocumentState newDocument, bool contentChanged)
        {
            var oldProject = GetProjectState(newDocument.Id.ProjectId)!;
            var newProject = oldProject.UpdateDocument(newDocument, contentChanged);

            // This method shouldn't have been called if the document has not changed.
            Debug.Assert(oldProject != newProject);

            var oldDocument = oldProject.DocumentStates.GetRequiredState(newDocument.Id);
            var newFilePathToDocumentIdsMap = CreateFilePathToDocumentIdsMapWithFilePath(newDocument.Id, oldDocument.FilePath, newDocument.FilePath);

            (var newState, newProject) = ForkProject(
                newProject,
                newFilePathToDocumentIdsMap: newFilePathToDocumentIdsMap);
            return (newState, oldProject, newProject);
        }

        private (SolutionState, ProjectState oldProjectState, ProjectState newProjectState) UpdateAdditionalDocumentState(AdditionalDocumentState newDocument, bool contentChanged)
        {
            var oldProject = GetProjectState(newDocument.Id.ProjectId)!;
            var newProject = oldProject.UpdateAdditionalDocument(newDocument, contentChanged);

            // This method shouldn't have been called if the document has not changed.
            Debug.Assert(oldProject != newProject);

            (var newState, newProject) = ForkProject(newProject);
            return (newState, oldProject, newProject);
        }

        private (SolutionState, ProjectState newProjectState) UpdateAnalyzerConfigDocumentState(AnalyzerConfigDocumentState newDocument)
        {
            var oldProject = GetProjectState(newDocument.Id.ProjectId)!;
            var newProject = oldProject.UpdateAnalyzerConfigDocument(newDocument);

            // This method shouldn't have been called if the document has not changed.
            Debug.Assert(oldProject != newProject);

            return ForkProject(newProject);
        }

        /// <summary>
        /// Creates a new snapshot with an updated project and an action that will produce a new
        /// compilation matching the new project out of an old compilation. All dependent projects
        /// are fixed-up if the change to the new project affects its public metadata, and old
        /// dependent compilations are forgotten.
        /// </summary>
        public (SolutionState solutionState, ProjectState newProjectState) ForkProject(
            ProjectState newProjectState,
            ProjectDependencyGraph? newDependencyGraph = null,
            ImmutableDictionary<string, ImmutableArray<DocumentId>>? newFilePathToDocumentIdsMap = null)
        {
            var projectId = newProjectState.Id;

            Contract.ThrowIfFalse(_projectIdToProjectStateMap.ContainsKey(projectId));
            var newStateMap = _projectIdToProjectStateMap.SetItem(projectId, newProjectState);

            newDependencyGraph ??= _dependencyGraph;

            var newSolutionState = this.Branch(
                idToProjectStateMap: newStateMap,
                dependencyGraph: newDependencyGraph,
                filePathToDocumentIdsMap: newFilePathToDocumentIdsMap ?? _filePathToDocumentIdsMap);

            return (newSolutionState, newProjectState);
        }

        /// <summary>
        /// Gets the set of <see cref="DocumentId"/>s in this <see cref="Solution"/> with a
        /// <see cref="TextDocument.FilePath"/> that matches the given file path.
        /// </summary>
        public ImmutableArray<DocumentId> GetDocumentIdsWithFilePath(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return ImmutableArray<DocumentId>.Empty;
            }

            return _filePathToDocumentIdsMap.TryGetValue(filePath!, out var documentIds)
                ? documentIds
                : ImmutableArray<DocumentId>.Empty;
        }

        private static ProjectDependencyGraph CreateDependencyGraph(
            IReadOnlyList<ProjectId> projectIds,
            ImmutableDictionary<ProjectId, ProjectState> projectStates)
        {
            var map = projectStates.Values.Select(state => new KeyValuePair<ProjectId, ImmutableHashSet<ProjectId>>(
                    state.Id,
                    state.ProjectReferences.Where(pr => projectStates.ContainsKey(pr.ProjectId)).Select(pr => pr.ProjectId).ToImmutableHashSet()))
                    .ToImmutableDictionary();

            return new ProjectDependencyGraph(projectIds.ToImmutableHashSet(), map);
        }

        public SolutionState WithOptions(SolutionOptionSet options)
            => Branch(options: options);

        public SolutionState AddAnalyzerReferences(IReadOnlyCollection<AnalyzerReference> analyzerReferences)
        {
            if (analyzerReferences.Count == 0)
            {
                return this;
            }

            var oldReferences = AnalyzerReferences.ToImmutableArray();
            var newReferences = oldReferences.AddRange(analyzerReferences);
            return Branch(analyzerReferences: newReferences);
        }

        public SolutionState RemoveAnalyzerReference(AnalyzerReference analyzerReference)
        {
            var oldReferences = AnalyzerReferences.ToImmutableArray();
            var newReferences = oldReferences.Remove(analyzerReference);
            if (oldReferences == newReferences)
            {
                return this;
            }

            return Branch(analyzerReferences: newReferences);
        }

        public SolutionState WithAnalyzerReferences(IReadOnlyList<AnalyzerReference> analyzerReferences)
        {
            if (analyzerReferences == AnalyzerReferences)
            {
                return this;
            }

            return Branch(analyzerReferences: analyzerReferences);
        }

        // this lock guards all the mutable fields (do not share lock with derived classes)
        private NonReentrantLock? _stateLockBackingField;
        private NonReentrantLock StateLock
        {
            get
            {
                // TODO: why did I need to do a nullable suppression here?
                return LazyInitializer.EnsureInitialized(ref _stateLockBackingField, NonReentrantLock.Factory)!;
            }
        }

        private WeakReference<SolutionStatePair>? _latestSolutionWithPartialCompilation;
        private DateTime _timeOfLatestSolutionWithPartialCompilation;
        private DocumentId? _documentIdOfLatestSolutionWithPartialCompilation;

        /// <summary>
        /// Creates a branch of the solution that has its compilations frozen in whatever state they are in at the time, assuming a background compiler is
        /// busy building this compilations.
        ///
        /// A compilation for the project containing the specified document id will be guaranteed to exist with at least the syntax tree for the document.
        ///
        /// This not intended to be the public API, use Document.WithFrozenPartialSemantics() instead.
        /// </summary>
        public (SolutionState, SolutionCompilationState) WithFrozenPartialCompilationIncludingSpecificDocument(
            SolutionCompilationState compilationState, DocumentId documentId, CancellationToken cancellationToken)
        {
            try
            {
                var allDocumentIds = GetRelatedDocumentIds(documentId);
                using var _ = ArrayBuilder<(DocumentState, SyntaxTree)>.GetInstance(allDocumentIds.Length, out var builder);

                foreach (var currentDocumentId in allDocumentIds)
                {
                    var document = this.GetRequiredDocumentState(currentDocumentId);
                    builder.Add((document, document.GetSyntaxTree(cancellationToken)));
                }

                using (this.StateLock.DisposableWait(cancellationToken))
                {
                    // in progress solutions are disabled for some testing
                    if (Services.GetService<IWorkspacePartialSolutionsTestHook>()?.IsPartialSolutionDisabled == true)
                    {
                        return (this, compilationState);
                    }

                    SolutionStatePair? currentPartialSolution = null;
                    _latestSolutionWithPartialCompilation?.TryGetTarget(out currentPartialSolution);

                    var reuseExistingPartialSolution =
                        (DateTime.UtcNow - _timeOfLatestSolutionWithPartialCompilation).TotalSeconds < 0.1 &&
                        _documentIdOfLatestSolutionWithPartialCompilation == documentId;

                    if (reuseExistingPartialSolution && currentPartialSolution != null)
                    {
                        SolutionLogger.UseExistingPartialSolution();
                        return (currentPartialSolution.Value.Item1, currentPartialSolution.Value.Item2);
                    }

                    var newIdToProjectStateMap = _projectIdToProjectStateMap;
                    var newIdToTrackerMap = compilationState.ProjectIdToTrackerMap;

                    foreach (var (doc, tree) in builder)
                    {
                        // if we don't have one or it is stale, create a new partial solution
                        var tracker = compilationState.GetCompilationTracker(this, doc.Id.ProjectId);
                        var newTracker = tracker.FreezePartialStateWithTree(this, compilationState, doc, tree, cancellationToken);

                        Contract.ThrowIfFalse(newIdToProjectStateMap.ContainsKey(doc.Id.ProjectId));
                        newIdToProjectStateMap = newIdToProjectStateMap.SetItem(doc.Id.ProjectId, newTracker.ProjectState);
                        newIdToTrackerMap = newIdToTrackerMap.SetItem(doc.Id.ProjectId, newTracker);
                    }

                    var newState = this.Branch(
                        idToProjectStateMap: newIdToProjectStateMap,
                        dependencyGraph: CreateDependencyGraph(ProjectIds, newIdToProjectStateMap));
                    var newCompilationState = compilationState.Branch(
                        newIdToTrackerMap);

                    _latestSolutionWithPartialCompilation = new WeakReference<SolutionStatePair>(new SolutionStatePair((newState, newCompilationState)));
                    _timeOfLatestSolutionWithPartialCompilation = DateTime.UtcNow;
                    _documentIdOfLatestSolutionWithPartialCompilation = documentId;

                    SolutionLogger.CreatePartialSolution();
                    return (newState, newCompilationState);
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        public ImmutableArray<DocumentId> GetRelatedDocumentIds(DocumentId documentId)
        {
            var projectState = this.GetProjectState(documentId.ProjectId);
            if (projectState == null)
            {
                // this document no longer exist
                return ImmutableArray<DocumentId>.Empty;
            }

            var documentState = projectState.DocumentStates.GetState(documentId);
            if (documentState == null)
            {
                // this document no longer exist
                return ImmutableArray<DocumentId>.Empty;
            }

            var filePath = documentState.FilePath;
            if (string.IsNullOrEmpty(filePath))
            {
                // this document can't have any related document. only related document is itself.
                return ImmutableArray.Create(documentId);
            }

            var documentIds = GetDocumentIdsWithFilePath(filePath);
            return documentIds.WhereAsArray(
                static (documentId, args) =>
                {
                    var projectState = args.solution.GetProjectState(documentId.ProjectId);
                    if (projectState == null)
                    {
                        // this document no longer exist
                        // I'm adding this ReportAndCatch to see if this does happen in the wild; it's not clear to me under what scenario that could happen since all the IDs of all document types
                        // should be removed when a project is removed.
                        FatalError.ReportAndCatch(new Exception("GetDocumentIdsWithFilePath returned a document in a project that does not exist."));
                        return false;
                    }

                    if (projectState.ProjectInfo.Language != args.Language)
                        return false;

                    // GetDocumentIdsWithFilePath may return DocumentIds for other types of documents (like additional files), so filter to normal documents
                    return projectState.DocumentStates.Contains(documentId);
                },
                (solution: this, projectState.Language));
        }

        /// <summary>
        /// Gets a <see cref="ProjectDependencyGraph"/> that details the dependencies between projects for this solution.
        /// </summary>
        public ProjectDependencyGraph GetProjectDependencyGraph()
            => _dependencyGraph;

        private void CheckNotContainsProject(ProjectId projectId)
        {
            if (this.ContainsProject(projectId))
            {
                throw new InvalidOperationException(WorkspacesResources.The_solution_already_contains_the_specified_project);
            }
        }

        internal void CheckContainsProject(ProjectId projectId)
        {
            if (!this.ContainsProject(projectId))
            {
                throw new InvalidOperationException(WorkspacesResources.The_solution_does_not_contain_the_specified_project);
            }
        }

        internal bool ContainsProjectReference(ProjectId projectId, ProjectReference projectReference)
            => GetRequiredProjectState(projectId).ProjectReferences.Contains(projectReference);

        internal bool ContainsMetadataReference(ProjectId projectId, MetadataReference metadataReference)
            => GetRequiredProjectState(projectId).MetadataReferences.Contains(metadataReference);

        internal bool ContainsAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
            => GetRequiredProjectState(projectId).AnalyzerReferences.Contains(analyzerReference);

        internal bool ContainsTransitiveReference(ProjectId fromProjectId, ProjectId toProjectId)
            => _dependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(fromProjectId).Contains(toProjectId);
    }
}
