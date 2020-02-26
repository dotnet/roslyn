﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
using Microsoft.CodeAnalysis.Logging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a set of projects and their source code documents. 
    /// 
    /// this is a green node of Solution like ProjectState/DocumentState are for 
    /// Project and Document.
    /// </summary>
    internal partial class SolutionState
    {
        // branch id for this solution
        private readonly BranchId _branchId;

        // the version of the workspace this solution is from
        private readonly int _workspaceVersion;

        private readonly SolutionInfo.SolutionAttributes _solutionAttributes;
        private readonly SolutionServices _solutionServices;
        private readonly ImmutableDictionary<ProjectId, ProjectState> _projectIdToProjectStateMap;
        private readonly ImmutableDictionary<string, ImmutableArray<DocumentId>> _filePathToDocumentIdsMap;
        private readonly ProjectDependencyGraph _dependencyGraph;

        // Values for all these are created on demand.
        private ImmutableDictionary<ProjectId, CompilationTracker> _projectIdToTrackerMap;

        // Checksums for this solution state
        private readonly ValueSource<SolutionStateChecksums> _lazyChecksums;

        private SolutionState(
            BranchId branchId,
            int workspaceVersion,
            SolutionServices solutionServices,
            SolutionInfo.SolutionAttributes solutionAttributes,
            IReadOnlyList<ProjectId> projectIds,
            SerializableOptionSet options,
            ImmutableDictionary<ProjectId, ProjectState> idToProjectStateMap,
            ImmutableDictionary<ProjectId, CompilationTracker> projectIdToTrackerMap,
            ImmutableDictionary<string, ImmutableArray<DocumentId>> filePathToDocumentIdsMap,
            ProjectDependencyGraph dependencyGraph)
        {
            _branchId = branchId;
            _workspaceVersion = workspaceVersion;
            _solutionAttributes = solutionAttributes;
            _solutionServices = solutionServices;
            ProjectIds = projectIds;
            Options = options;
            _projectIdToProjectStateMap = idToProjectStateMap;
            _projectIdToTrackerMap = projectIdToTrackerMap;
            _filePathToDocumentIdsMap = filePathToDocumentIdsMap;
            _dependencyGraph = dependencyGraph;

            // when solution state is changed, we re-calcuate its checksum
            _lazyChecksums = new AsyncLazy<SolutionStateChecksums>(ComputeChecksumsAsync, cacheResult: true);

            CheckInvariants();
        }

        public SolutionState(
            BranchId primaryBranchId,
            SolutionServices solutionServices,
            SolutionInfo.SolutionAttributes solutionAttributes,
            SerializableOptionSet options)
            : this(
                primaryBranchId,
                workspaceVersion: 0,
                solutionServices,
                solutionAttributes,
                projectIds: SpecializedCollections.EmptyBoxedImmutableArray<ProjectId>(),
                options,
                idToProjectStateMap: ImmutableDictionary<ProjectId, ProjectState>.Empty,
                projectIdToTrackerMap: ImmutableDictionary<ProjectId, CompilationTracker>.Empty,
                filePathToDocumentIdsMap: ImmutableDictionary.Create<string, ImmutableArray<DocumentId>>(StringComparer.OrdinalIgnoreCase),
                dependencyGraph: ProjectDependencyGraph.Empty)
        {
        }

        public SolutionState WithNewWorkspace(Workspace workspace, int workspaceVersion)
        {
            var services = workspace != _solutionServices.Workspace
                ? new SolutionServices(workspace)
                : _solutionServices;

            // Note: this will potentially have problems if the workspace services are different, as some services
            // get locked-in by document states and project states when first constructed.
            return CreatePrimarySolution(branchId: workspace.PrimaryBranchId, workspaceVersion: workspaceVersion, services: services);
        }

        public SolutionInfo.SolutionAttributes SolutionAttributes => _solutionAttributes;

        public ImmutableDictionary<ProjectId, ProjectState> ProjectStates => _projectIdToProjectStateMap;

        public int WorkspaceVersion => _workspaceVersion;

        public SolutionServices Services => _solutionServices;

        public SerializableOptionSet Options { get; }

        /// <summary>
        /// branch id of this solution
        /// 
        /// currently, it only supports one level of branching. there is a primary branch of a workspace and all other
        /// branches that are branched from the primary branch.
        /// 
        /// one still can create multiple forked solutions from an already branched solution, but versions among those
        /// can't be reliably used and compared. 
        /// 
        /// version only has a meaning between primary solution and branched one or between solutions from same branch.
        /// </summary>
        public BranchId BranchId => _branchId;

        /// <summary>
        /// The Workspace this solution is associated with.
        /// </summary>
        public Workspace Workspace => _solutionServices.Workspace;

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

        // [Conditional("DEBUG")]
        private void CheckInvariants()
        {
            Contract.ThrowIfTrue(ProjectIds.Count != _projectIdToProjectStateMap.Count);

            // An id shouldn't point at a tracker for a different project.
            Contract.ThrowIfTrue(_projectIdToTrackerMap.Any(kvp => kvp.Key != kvp.Value.ProjectState.Id));
        }

        private SolutionState Branch(
            SolutionInfo.SolutionAttributes? solutionAttributes = null,
            IReadOnlyList<ProjectId>? projectIds = null,
            SerializableOptionSet? options = null,
            ImmutableDictionary<ProjectId, ProjectState>? idToProjectStateMap = null,
            ImmutableDictionary<ProjectId, CompilationTracker>? projectIdToTrackerMap = null,
            ImmutableDictionary<string, ImmutableArray<DocumentId>>? filePathToDocumentIdsMap = null,
            ProjectDependencyGraph? dependencyGraph = null)
        {
            var branchId = GetBranchId();

            solutionAttributes ??= _solutionAttributes;
            projectIds ??= ProjectIds;
            idToProjectStateMap ??= _projectIdToProjectStateMap;
            options ??= Options.WithLanguages(GetProjectLanguages(idToProjectStateMap));
            projectIdToTrackerMap ??= _projectIdToTrackerMap;
            filePathToDocumentIdsMap ??= _filePathToDocumentIdsMap;
            dependencyGraph ??= _dependencyGraph;

            if (branchId == _branchId &&
                solutionAttributes == _solutionAttributes &&
                projectIds == ProjectIds &&
                options == Options &&
                idToProjectStateMap == _projectIdToProjectStateMap &&
                projectIdToTrackerMap == _projectIdToTrackerMap &&
                filePathToDocumentIdsMap == _filePathToDocumentIdsMap &&
                dependencyGraph == _dependencyGraph)
            {
                return this;
            }


            return new SolutionState(
                branchId,
                _workspaceVersion,
                _solutionServices,
                solutionAttributes,
                projectIds,
                options,
                idToProjectStateMap,
                projectIdToTrackerMap,
                filePathToDocumentIdsMap,
                dependencyGraph);
        }

        private SolutionState CreatePrimarySolution(
            BranchId branchId,
            int workspaceVersion,
            SolutionServices services)
        {
            if (branchId == _branchId &&
                workspaceVersion == _workspaceVersion &&
                services == _solutionServices)
            {
                return this;
            }

            return new SolutionState(
                branchId,
                workspaceVersion,
                services,
                _solutionAttributes,
                ProjectIds,
                Options,
                _projectIdToProjectStateMap,
                _projectIdToTrackerMap,
                _filePathToDocumentIdsMap,
                _dependencyGraph);
        }

        private BranchId GetBranchId()
        {
            // currently we only support one level branching. 
            // my reasonings are
            // 1. it seems there is no-one who needs sub branches.
            // 2. this lets us to branch without explicit branch API
            return _branchId == Workspace.PrimaryBranchId ? BranchId.GetNextId() : _branchId;
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
        {
            return projectId != null && _projectIdToProjectStateMap.ContainsKey(projectId);
        }

        /// <summary>
        /// True if the solution contains the document in one of its projects
        /// </summary>
        public bool ContainsDocument([NotNullWhen(returnValue: true)] DocumentId? documentId)
        {
            return
                documentId != null &&
                this.ContainsProject(documentId.ProjectId) &&
                this.GetProjectState(documentId.ProjectId)!.ContainsDocument(documentId);
        }

        /// <summary>
        /// True if the solution contains the additional document in one of its projects
        /// </summary>
        public bool ContainsAdditionalDocument([NotNullWhen(returnValue: true)] DocumentId? documentId)
        {
            return
                documentId != null &&
                this.ContainsProject(documentId.ProjectId) &&
                this.GetProjectState(documentId.ProjectId)!.ContainsAdditionalDocument(documentId);
        }

        /// <summary>
        /// True if the solution contains the analyzer config document in one of its projects
        /// </summary>
        public bool ContainsAnalyzerConfigDocument([NotNullWhen(returnValue: true)] DocumentId? documentId)
        {
            return
                documentId != null &&
                this.ContainsProject(documentId.ProjectId) &&
                this.GetProjectState(documentId.ProjectId)!.ContainsAnalyzerConfigDocument(documentId);
        }

        private DocumentState? GetDocumentState(DocumentId? documentId)
        {
            if (documentId != null)
            {
                var projectState = this.GetProjectState(documentId.ProjectId);
                if (projectState != null)
                {
                    return projectState.GetDocumentState(documentId);
                }
            }

            return null;
        }

        private DocumentState? GetDocumentState(SyntaxTree? syntaxTree, ProjectId? projectId)
        {
            if (syntaxTree != null)
            {
                // is this tree known to be associated with a document?
                var docId = DocumentState.GetDocumentIdForTree(syntaxTree);
                if (docId != null && (projectId == null || docId.ProjectId == projectId))
                {
                    // does this solution even have the document?
                    var document = this.GetDocumentState(docId);
                    if (document != null)
                    {
                        // does this document really have the syntax tree?
                        if (document.TryGetSyntaxTree(out var documentTree) && documentTree == syntaxTree)
                        {
                            return document;
                        }
                    }
                }
            }

            return null;
        }

        private TextDocumentState? GetAdditionalDocumentState(DocumentId? documentId)
        {
            if (documentId != null)
            {
                var projectState = this.GetProjectState(documentId.ProjectId);
                if (projectState != null)
                {
                    return projectState.GetAdditionalDocumentState(documentId);
                }
            }

            return null;
        }

        private AnalyzerConfigDocumentState? GetAnalyzerConfigDocumentState(DocumentId? documentId)
        {
            if (documentId != null)
            {
                var projectState = this.GetProjectState(documentId.ProjectId);
                if (projectState != null)
                {
                    return projectState.GetAnalyzerConfigDocumentState(documentId);
                }
            }

            return null;
        }

        public Task<VersionStamp> GetDependentVersionAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            return this.GetCompilationTracker(projectId).GetDependentVersionAsync(this, cancellationToken);
        }

        public Task<VersionStamp> GetDependentSemanticVersionAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            return this.GetCompilationTracker(projectId).GetDependentSemanticVersionAsync(this, cancellationToken);
        }

        public ProjectState? GetProjectState(ProjectId projectId)
        {
            _projectIdToProjectStateMap.TryGetValue(projectId, out var state);
            return state;
        }

        /// <summary>
        /// Gets the <see cref="Project"/> associated with an assembly symbol.
        /// </summary>
        public ProjectState? GetProjectState(IAssemblySymbol? assemblySymbol)
        {
            if (assemblySymbol == null)
            {
                return null;
            }

            // TODO: Remove this loop when we add source assembly symbols to s_assemblyOrModuleSymbolToProjectMap
            foreach (var (_, state) in _projectIdToProjectStateMap)
            {
                if (this.TryGetCompilation(state.Id, out var compilation))
                {
                    // if the symbol is the compilation's assembly symbol, we are done
                    if (Equals(compilation.Assembly, assemblySymbol))
                    {
                        return state;
                    }
                }
            }

            s_assemblyOrModuleSymbolToProjectMap.TryGetValue(assemblySymbol, out var id);
            return id == null ? null : this.GetProjectState(id);
        }

        private bool TryGetCompilationTracker(ProjectId projectId, [NotNullWhen(returnValue: true)] out CompilationTracker? tracker)
        {
            return _projectIdToTrackerMap.TryGetValue(projectId, out tracker);
        }

        private static readonly Func<ProjectId, SolutionState, CompilationTracker> s_createCompilationTrackerFunction = CreateCompilationTracker;

        private static CompilationTracker CreateCompilationTracker(ProjectId projectId, SolutionState solution)
        {
            var projectState = solution.GetProjectState(projectId);
            Contract.ThrowIfNull(projectState);
            return new CompilationTracker(projectState);
        }

        private CompilationTracker GetCompilationTracker(ProjectId projectId)
        {
            if (!_projectIdToTrackerMap.TryGetValue(projectId, out var tracker))
            {
                tracker = ImmutableInterlocked.GetOrAdd(ref _projectIdToTrackerMap, projectId, s_createCompilationTrackerFunction, this);
            }

            return tracker;
        }

        private SolutionState AddProject(ProjectId projectId, ProjectState projectState)
        {
            // changed project list so, increment version.
            var newSolutionAttributes = _solutionAttributes.WithVersion(this.Version.GetNewerVersion());

            var newProjectIds = ProjectIds.ToImmutableArray().Add(projectId);
            var newStateMap = _projectIdToProjectStateMap.Add(projectId, projectState);
            var newDependencyGraph = _dependencyGraph
                                        .WithAdditionalProjects(SpecializedCollections.SingletonEnumerable(projectId))
                                        .WithAdditionalProjectReferences(projectId,
                                            projectState.ProjectReferences.Where(r => _projectIdToProjectStateMap.ContainsKey(r.ProjectId)).Select(r => r.ProjectId).ToList());

            // It's possible that another project already in newStateMap has a reference to this project that we're adding, since we allow
            // dangling references like that. If so, we'll need to link those in too.
            foreach (var newState in newStateMap)
            {
                foreach (var projectReference in newState.Value.ProjectReferences)
                {
                    if (projectReference.ProjectId == projectId)
                    {
                        newDependencyGraph = newDependencyGraph.WithAdditionalProjectReferences(newState.Key, ImmutableArray.Create(projectId));
                        break;
                    }
                }
            }

            var newTrackerMap = CreateCompilationTrackerMap(projectId, newDependencyGraph);
            var newFilePathToDocumentIdsMap = CreateFilePathToDocumentIdsMapWithAddedProject(newStateMap[projectId]);

            return this.Branch(
                solutionAttributes: newSolutionAttributes,
                projectIds: newProjectIds,
                idToProjectStateMap: newStateMap,
                projectIdToTrackerMap: newTrackerMap,
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

            var languageServices = this.Workspace.Services.GetLanguageServices(language);
            if (languageServices == null)
            {
                throw new ArgumentException(string.Format(WorkspacesResources.The_language_0_is_not_supported, language));
            }

            var newProject = new ProjectState(projectInfo, languageServices, _solutionServices);

            return this.AddProject(newProject.Id, newProject);
        }

        private ImmutableDictionary<string, ImmutableArray<DocumentId>> CreateFilePathToDocumentIdsMapWithAddedProject(ProjectState projectState)
        {
            return CreateFilePathToDocumentIdsMapWithAddedDocuments(GetDocumentStates(projectState));
        }

        private ImmutableDictionary<string, ImmutableArray<DocumentId>> CreateFilePathToDocumentIdsMapWithAddedDocuments(IEnumerable<TextDocumentState> documentStates)
        {
            var builder = _filePathToDocumentIdsMap.ToBuilder();

            foreach (var documentState in documentStates)
            {
                if (string.IsNullOrEmpty(documentState.FilePath))
                {
                    continue;
                }

                builder[documentState.FilePath!] = builder.TryGetValue(documentState.FilePath!, out var documentIdsWithPath)
                    ? documentIdsWithPath.Add(documentState.Id)
                    : ImmutableArray.Create(documentState.Id);
            }

            return builder.ToImmutable();
        }

        private static IEnumerable<TextDocumentState> GetDocumentStates(ProjectState projectState)
            => projectState.DocumentStates.Values
                   .Concat(projectState.AdditionalDocumentStates.Values)
                   .Concat(projectState.AnalyzerConfigDocumentStates.Values);

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
            var newSolutionAttributes = _solutionAttributes.WithVersion(this.Version.GetNewerVersion());

            var newProjectIds = ProjectIds.ToImmutableArray().Remove(projectId);
            var newStateMap = _projectIdToProjectStateMap.Remove(projectId);
            var newDependencyGraph = _dependencyGraph.WithProjectRemoved(projectId);
            var newTrackerMap = CreateCompilationTrackerMap(projectId, newDependencyGraph);
            var newFilePathToDocumentIdsMap = CreateFilePathToDocumentIdsMapWithRemovedProject(_projectIdToProjectStateMap[projectId]);

            return this.Branch(
                solutionAttributes: newSolutionAttributes,
                projectIds: newProjectIds,
                idToProjectStateMap: newStateMap,
                projectIdToTrackerMap: newTrackerMap.Remove(projectId),
                filePathToDocumentIdsMap: newFilePathToDocumentIdsMap,
                dependencyGraph: newDependencyGraph);
        }

        private ImmutableDictionary<string, ImmutableArray<DocumentId>> CreateFilePathToDocumentIdsMapWithRemovedProject(ProjectState projectState)
        {
            return CreateFilePathToDocumentIdsMapWithRemovedDocuments(GetDocumentStates(projectState));
        }

        private ImmutableDictionary<string, ImmutableArray<DocumentId>> CreateFilePathToDocumentIdsMapWithRemovedDocuments(IEnumerable<TextDocumentState> documentStates)
        {
            var builder = _filePathToDocumentIdsMap.ToBuilder();

            foreach (var documentState in documentStates)
            {
                if (string.IsNullOrEmpty(documentState.FilePath))
                {
                    continue;
                }

                if (!builder.TryGetValue(documentState.FilePath!, out var documentIdsWithPath) || !documentIdsWithPath.Contains(documentState.Id))
                {
                    throw new ArgumentException($"The given documentId was not found in '{nameof(_filePathToDocumentIdsMap)}'.");
                }

                if (documentIdsWithPath.Length == 1)
                {
                    builder.Remove(documentState.FilePath!);
                }
                else
                {
                    builder[documentState.FilePath!] = documentIdsWithPath.Remove(documentState.Id);
                }
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the new
        /// assembly name.
        /// </summary>
        public SolutionState WithProjectAssemblyName(ProjectId projectId, string assemblyName)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            CheckContainsProject(projectId);

            var oldProject = this.GetProjectState(projectId)!;
            var newProject = oldProject.UpdateAssemblyName(assemblyName);

            if (oldProject == newProject)
            {
                return this;
            }

            return this.ForkProject(newProject, new CompilationTranslationAction.ProjectAssemblyNameAction(assemblyName));
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the output file path.
        /// </summary>
        public SolutionState WithProjectOutputFilePath(ProjectId projectId, string? outputFilePath)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            CheckContainsProject(projectId);

            var oldProjectState = this.GetProjectState(projectId)!;
            var newProjectState = oldProjectState.UpdateOutputFilePath(outputFilePath);

            if (oldProjectState == newProjectState)
            {
                return this;
            }

            return this.ForkProject(newProjectState);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the output file path.
        /// </summary>
        public SolutionState WithProjectOutputRefFilePath(ProjectId projectId, string? outputRefFilePath)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            CheckContainsProject(projectId);

            var oldProjectState = this.GetProjectState(projectId)!;
            var newProjectState = oldProjectState.UpdateOutputRefFilePath(outputRefFilePath);

            if (oldProjectState == newProjectState)
            {
                return this;
            }

            return this.ForkProject(newProjectState);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the default namespace.
        /// </summary>
        public SolutionState WithProjectDefaultNamespace(ProjectId projectId, string? defaultNamespace)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            CheckContainsProject(projectId);

            var oldProjectState = this.GetProjectState(projectId)!;
            var newProjectState = oldProjectState.UpdateDefaultNamespace(defaultNamespace);

            if (oldProjectState == newProjectState)
            {
                return this;
            }

            return this.ForkProject(newProjectState);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the name.
        /// </summary>
        public SolutionState WithProjectName(ProjectId projectId, string name)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            CheckContainsProject(projectId);

            var oldProjectState = this.GetProjectState(projectId)!;
            var newProjectState = oldProjectState.UpdateName(name);

            if (oldProjectState == newProjectState)
            {
                return this;
            }

            return this.ForkProject(newProjectState);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the project file path.
        /// </summary>
        public SolutionState WithProjectFilePath(ProjectId projectId, string? filePath)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            CheckContainsProject(projectId);

            var oldProjectState = this.GetProjectState(projectId)!;
            var newProjectState = oldProjectState.UpdateFilePath(filePath);

            if (oldProjectState == newProjectState)
            {
                return this;
            }

            return this.ForkProject(newProjectState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified compilation options.
        /// </summary>
        public SolutionState WithProjectCompilationOptions(ProjectId projectId, CompilationOptions options)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            CheckContainsProject(projectId);

            var oldProject = this.GetProjectState(projectId)!;
            var newProject = oldProject.UpdateCompilationOptions(options);

            if (oldProject == newProject)
            {
                return this;
            }

            return this.ForkProject(newProject, new CompilationTranslationAction.ProjectCompilationOptionsAction(options));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified parse options.
        /// </summary>
        public SolutionState WithProjectParseOptions(ProjectId projectId, ParseOptions options)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            Debug.Assert(this.ContainsProject(projectId));

            var oldProject = this.GetProjectState(projectId)!;
            var newProject = oldProject.UpdateParseOptions(options);

            if (oldProject == newProject)
            {
                return this;
            }

            if (this.Workspace.PartialSemanticsEnabled)
            {
                // don't fork tracker with queued action since access via partial semantics can become inconsistent (throw).
                // Since changing options is rare event, it is okay to start compilation building from scratch.
                return this.ForkProject(newProject, forkTracker: false);
            }
            else
            {
                return this.ForkProject(newProject, new CompilationTranslationAction.ReplaceAllSyntaxTreesAction(newProject));
            }
        }

        /// <summary>
        /// Update a new solution instance with a fork of the specified project.
        /// 
        /// this is a temporary workaround until editorconfig becomes real part of roslyn solution snapshot.
        /// until then, this will explicitly fork current solution snapshot
        /// </summary>
        internal SolutionState WithProjectOptionsChanged(ProjectId projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            Debug.Assert(this.ContainsProject(projectId));

            return ForkProject(GetProjectState(projectId)!);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified hasAllInformation.
        /// </summary>
        public SolutionState WithHasAllInformation(ProjectId projectId, bool hasAllInformation)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            Debug.Assert(this.ContainsProject(projectId));

            var oldProject = this.GetProjectState(projectId)!;
            var newProject = oldProject.UpdateHasAllInformation(hasAllInformation);

            if (oldProject == newProject)
            {
                return this;
            }

            // fork without any change on compilation.
            return this.ForkProject(newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified runAnalyzers.
        /// </summary>
        public SolutionState WithRunAnalyzers(ProjectId projectId, bool runAnalyzers)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            Debug.Assert(this.ContainsProject(projectId));

            var oldProject = this.GetProjectState(projectId)!;
            var newProject = oldProject.UpdateRunAnalyzers(runAnalyzers);

            if (oldProject == newProject)
            {
                return this;
            }

            // fork without any change on compilation.
            return this.ForkProject(newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include
        /// the specified project references.
        /// </summary>
        public SolutionState AddProjectReferences(ProjectId projectId, IEnumerable<ProjectReference> projectReferences)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (projectReferences == null)
            {
                throw new ArgumentNullException(nameof(projectReferences));
            }

            if (!projectReferences.Any())
            {
                return this;
            }

            CheckContainsProject(projectId);

            foreach (var referencedProject in projectReferences)
            {
                CheckContainsProject(referencedProject.ProjectId);
                CheckNotContainsProjectReference(projectId, referencedProject);
                CheckNotContainsTransitiveReference(referencedProject.ProjectId, projectId);
                CheckNotSecondSubmissionReference(projectId, referencedProject.ProjectId);
            }

            var oldProject = this.GetProjectState(projectId)!;
            var newProject = oldProject.AddProjectReferences(projectReferences);
            var newDependencyGraph = _dependencyGraph.WithAdditionalProjectReferences(projectId, projectReferences.Select(r => r.ProjectId).ToList());

            return this.ForkProject(newProject, newDependencyGraph: newDependencyGraph);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to no longer
        /// include the specified project reference.
        /// </summary>
        public SolutionState RemoveProjectReference(ProjectId projectId, ProjectReference projectReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (projectReference == null)
            {
                throw new ArgumentNullException(nameof(projectReference));
            }

            CheckContainsProject(projectId);
            CheckContainsProject(projectReference.ProjectId);

            var oldProject = this.GetProjectState(projectId)!;
            var newProject = oldProject.RemoveProjectReference(projectReference);

            ProjectDependencyGraph newDependencyGraph;
            if (newProject.ContainsReferenceToProject(projectReference.ProjectId))
            {
                // The project contained multiple references to the project, and not all of them were removed. The
                // dependency graph doesn't change.
                newDependencyGraph = _dependencyGraph;
            }
            else
            {
                newDependencyGraph = _dependencyGraph.WithProjectReferenceRemoved(projectId, projectReference.ProjectId);
            }

            return this.ForkProject(newProject, newDependencyGraph: newDependencyGraph);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to contain
        /// the specified list of project references.
        /// </summary>
        public SolutionState WithProjectReferences(ProjectId projectId, IEnumerable<ProjectReference> projectReferences)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (projectReferences == null)
            {
                throw new ArgumentNullException(nameof(projectReferences));
            }

            CheckContainsProject(projectId);

            var oldProject = this.GetProjectState(projectId)!;
            var newProject = oldProject.WithProjectReferences(projectReferences);

            return this.ForkProject(newProject, newDependencyGraph: _dependencyGraph.WithProjectReferences(projectId, projectReferences.Select(p => p.ProjectId)));
        }

        /// <summary>
        /// Creates a new solution instance with the project documents in the order by the specified document ids.
        /// The specified document ids must be the same as what is already in the project; no adding or removing is allowed.
        /// </summary>
        public SolutionState WithProjectDocumentsOrder(ProjectId projectId, ImmutableList<DocumentId> documentIds)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (documentIds == null)
            {
                throw new ArgumentNullException(nameof(documentIds));
            }

            CheckContainsProject(projectId);

            var oldProject = this.GetProjectState(projectId)!;
            var newProject = oldProject.UpdateDocumentsOrder(documentIds);

            if (oldProject == newProject)
            {
                return this;
            }

            return this.ForkProject(newProject, new CompilationTranslationAction.ReplaceAllSyntaxTreesAction(newProject));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the 
        /// specified metadata reference.
        /// </summary>
        public SolutionState AddMetadataReference(ProjectId projectId, MetadataReference metadataReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (metadataReference == null)
            {
                throw new ArgumentNullException(nameof(metadataReference));
            }

            CheckContainsProject(projectId);

            return this.ForkProject(
                this.GetProjectState(projectId)!.AddMetadataReference(metadataReference));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the
        /// specified metadata references.
        /// </summary>
        public SolutionState AddMetadataReferences(ProjectId projectId, IEnumerable<MetadataReference> metadataReferences)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (metadataReferences == null)
            {
                throw new ArgumentNullException(nameof(metadataReferences));
            }

            CheckContainsProject(projectId);
            return this.ForkProject(this.GetProjectState(projectId)!.AddMetadataReferences(metadataReferences));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to no longer include
        /// the specified metadata reference.
        /// </summary>
        public SolutionState RemoveMetadataReference(ProjectId projectId, MetadataReference metadataReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (metadataReference == null)
            {
                throw new ArgumentNullException(nameof(metadataReference));
            }

            CheckContainsProject(projectId);

            return this.ForkProject(
                this.GetProjectState(projectId)!.RemoveMetadataReference(metadataReference));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include only the
        /// specified metadata references.
        /// </summary>
        public SolutionState WithProjectMetadataReferences(ProjectId projectId, IEnumerable<MetadataReference> metadataReferences)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (metadataReferences == null)
            {
                throw new ArgumentNullException(nameof(metadataReferences));
            }

            CheckContainsProject(projectId);
            return this.ForkProject(this.GetProjectState(projectId)!.WithMetadataReferences(metadataReferences));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the 
        /// specified analyzer reference.
        /// </summary>
        public SolutionState AddAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (analyzerReference == null)
            {
                throw new ArgumentNullException(nameof(analyzerReference));
            }

            CheckContainsProject(projectId);
            return this.ForkProject(this.GetProjectState(projectId)!.AddAnalyzerReference(analyzerReference));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the
        /// specified analyzer references.
        /// </summary>
        public SolutionState AddAnalyzerReferences(ProjectId projectId, IEnumerable<AnalyzerReference> analyzerReferences)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (analyzerReferences == null)
            {
                throw new ArgumentNullException(nameof(analyzerReferences));
            }

            if (!analyzerReferences.Any())
            {
                return this;
            }

            CheckContainsProject(projectId);
            return this.ForkProject(this.GetProjectState(projectId)!.AddAnalyzerReferences(analyzerReferences));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to no longer include
        /// the specified analyzer reference.
        /// </summary>
        public SolutionState RemoveAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (analyzerReference == null)
            {
                throw new ArgumentNullException(nameof(analyzerReference));
            }

            CheckContainsProject(projectId);
            return this.ForkProject(this.GetProjectState(projectId)!.RemoveAnalyzerReference(analyzerReference));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include only the
        /// specified analyzer references.
        /// </summary>
        public SolutionState WithProjectAnalyzerReferences(ProjectId projectId, IEnumerable<AnalyzerReference> analyzerReferences)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (analyzerReferences == null)
            {
                throw new ArgumentNullException(nameof(analyzerReferences));
            }

            CheckContainsProject(projectId);
            return this.ForkProject(this.GetProjectState(projectId)!.WithAnalyzerReferences(analyzerReferences));
        }

        /// <summary>
        /// Create a new solution instance with the corresponding projects updated to include new 
        /// documents defined by the document info.
        /// </summary>
        public SolutionState AddDocuments(ImmutableArray<DocumentInfo> documentInfos)
        {
            return AddDocumentsToMultipleProjects(documentInfos,
                (documentInfo, project) => project.CreateDocument(documentInfo, project.ParseOptions),
                (oldProject, documents) => (oldProject.AddDocuments(documents), new CompilationTranslationAction.AddDocumentsAction(documents)));
        }

        /// <summary>
        /// Core helper that takes a set of <see cref="DocumentInfo" />s and does the application of the appropriate documents to each project.
        /// </summary>
        /// <param name="documentInfos">The set of documents to add.</param>
        /// <param name="addDocumentsToProjectState">Returns the new <see cref="ProjectState"/> with the documents added, and the <see cref="CompilationTranslationAction"/> needed as well.</param>
        /// <returns></returns>
        private SolutionState AddDocumentsToMultipleProjects<T>(
            ImmutableArray<DocumentInfo> documentInfos,
            Func<DocumentInfo, ProjectState, T> createDocumentState,
            Func<ProjectState, ImmutableArray<T>, (ProjectState newState, CompilationTranslationAction? translationAction)> addDocumentsToProjectState)
            where T : TextDocumentState
        {
            if (documentInfos.IsDefault)
            {
                throw new ArgumentNullException(nameof(documentInfos));
            }

            if (documentInfos.IsEmpty)
            {
                return this;
            }

            // The documents might be contributing to multiple different projects; split them by project and then we'll process
            // project-at-a-time.
            var documentInfosByProjectId = documentInfos.ToLookup(d => d.Id.ProjectId);

            var newSolutionState = this;

            foreach (var documentInfosInProject in documentInfosByProjectId)
            {
                CheckContainsProject(documentInfosInProject.Key);
                var oldProjectState = this.GetProjectState(documentInfosInProject.Key)!;

                var newDocumentStatesForProjectBuilder = ArrayBuilder<T>.GetInstance();

                foreach (var documentInfo in documentInfosInProject)
                {
                    newDocumentStatesForProjectBuilder.Add(createDocumentState(documentInfo, oldProjectState));
                }

                var newDocumentStatesForProject = newDocumentStatesForProjectBuilder.ToImmutableAndFree();

                var (newProjectState, compilationTranslationAction) = addDocumentsToProjectState(oldProjectState, newDocumentStatesForProject);

                newSolutionState = newSolutionState.ForkProject(newProjectState,
                    compilationTranslationAction,
                    newFilePathToDocumentIdsMap: CreateFilePathToDocumentIdsMapWithAddedDocuments(newDocumentStatesForProject));
            }

            return newSolutionState;
        }

        public SolutionState AddAdditionalDocuments(ImmutableArray<DocumentInfo> documentInfos)
        {
            return AddDocumentsToMultipleProjects(documentInfos,
                (documentInfo, project) => new TextDocumentState(documentInfo, _solutionServices),
                (projectState, documents) => (projectState.AddAdditionalDocuments(documents), translationAction: null));
        }

        public SolutionState AddAnalyzerConfigDocuments(ImmutableArray<DocumentInfo> documentInfos)
        {
            // Adding a new analyzer config potentially impacts all syntax trees and the diagnostic reporting information
            // attached to them, so we'll just replace all syntax trees in that case.
            return AddDocumentsToMultipleProjects(documentInfos,
                (documentInfo, project) => new AnalyzerConfigDocumentState(documentInfo, _solutionServices),
                (oldProject, documents) =>
                {
                    var newProject = oldProject.AddAnalyzerConfigDocuments(documents);
                    return (newProject, new CompilationTranslationAction.ReplaceAllSyntaxTreesAction(newProject));
                });
        }

        public SolutionState RemoveAnalyzerConfigDocuments(ImmutableArray<DocumentId> documentIds)
        {
            return RemoveDocumentsFromMultipleProjects(documentIds,
                (projectState, documentId) => { CheckContainsAnalyzerConfigDocument(documentId); return projectState.GetAnalyzerConfigDocumentState(documentId)!; },
                (oldProject, documentIds, _) =>
                {
                    var newProject = oldProject.RemoveAnalyzerConfigDocuments(documentIds);
                    return (newProject, new CompilationTranslationAction.ReplaceAllSyntaxTreesAction(newProject));
                });
        }

        /// <summary>
        /// Creates a new solution instance that no longer includes the specified document.
        /// </summary>
        public SolutionState RemoveDocuments(ImmutableArray<DocumentId> documentIds)
        {
            return RemoveDocumentsFromMultipleProjects(documentIds,
                (projectState, documentId) => { CheckContainsDocument(documentId); return projectState.GetDocumentState(documentId)!; },
                (projectState, documentIds, documentStates) => (projectState.RemoveDocuments(documentIds), new CompilationTranslationAction.RemoveDocumentsAction(documentStates)));
        }

        private SolutionState RemoveDocumentsFromMultipleProjects<T>(
            ImmutableArray<DocumentId> documentIds,
            Func<ProjectState, DocumentId, T> getExistingTextDocumentState,
            Func<ProjectState, ImmutableArray<DocumentId>, ImmutableArray<T>, (ProjectState newState, CompilationTranslationAction? translationAction)> removeDocumentsFromProjectState)
            where T : TextDocumentState
        {
            if (documentIds.IsDefault)
            {
                throw new ArgumentNullException(nameof(documentIds));
            }

            if (documentIds.IsEmpty)
            {
                return this;
            }

            // The documents might be contributing to multiple different projects; split them by project and then we'll process
            // project-at-a-time.
            var documentIdsByProjectId = documentIds.ToLookup(id => id.ProjectId);

            var newSolutionState = this;

            foreach (var documentIdsInProject in documentIdsByProjectId)
            {
                var oldProjectState = this.GetProjectState(documentIdsInProject.Key);

                if (oldProjectState == null)
                {
                    throw new InvalidOperationException(string.Format(WorkspacesResources._0_is_not_part_of_the_workspace, documentIdsInProject.Key));
                }

                var removedDocumentStatesBuilder = ArrayBuilder<T>.GetInstance();

                foreach (var documentId in documentIdsInProject)
                {
                    removedDocumentStatesBuilder.Add(getExistingTextDocumentState(oldProjectState, documentId));
                }

                var removedDocumentStatesForProject = removedDocumentStatesBuilder.ToImmutableAndFree();

                var (newProjectState, compilationTranslationAction) = removeDocumentsFromProjectState(oldProjectState, documentIdsInProject.ToImmutableArray(), removedDocumentStatesForProject);

                newSolutionState = newSolutionState.ForkProject(newProjectState,
                    compilationTranslationAction,
                    newFilePathToDocumentIdsMap: CreateFilePathToDocumentIdsMapWithRemovedDocuments(removedDocumentStatesForProject));
            }

            return newSolutionState;
        }

        /// <summary>
        /// Creates a new solution instance that no longer includes the specified additional documents.
        /// </summary>
        public SolutionState RemoveAdditionalDocuments(ImmutableArray<DocumentId> documentIds)
        {
            return RemoveDocumentsFromMultipleProjects(documentIds,
                (projectState, documentId) => { CheckContainsAdditionalDocument(documentId); return projectState.GetAdditionalDocumentState(documentId)!; },
                (projectState, documentIds, documentStates) => (projectState.RemoveAdditionalDocuments(documentIds), translationAction: null));
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the specified name.
        /// </summary>
        public SolutionState WithDocumentName(DocumentId documentId, string name)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var oldDocument = this.GetDocumentState(documentId)!;
            var newDocument = oldDocument.UpdateName(name);

            return this.WithDocumentState(newDocument);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to be contained in
        /// the sequence of logical folders.
        /// </summary>
        public SolutionState WithDocumentFolders(DocumentId documentId, IEnumerable<string?> folders)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (folders == null)
            {
                throw new ArgumentNullException(nameof(folders));
            }

            var oldDocument = GetDocumentState(documentId)!;
            var newDocument = oldDocument.UpdateFolders(folders.WhereNotNull().ToImmutableArray());

            return WithDocumentState(newDocument);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the specified file path.
        /// </summary>
        public SolutionState WithDocumentFilePath(DocumentId documentId, string filePath)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            // TODO: why? we support nullable file paths
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var oldDocument = this.GetDocumentState(documentId)!;
            var newDocument = oldDocument.UpdateFilePath(filePath);

            return this.WithDocumentState(newDocument);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the text
        /// specified.
        /// </summary>
        public SolutionState WithDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            CheckContainsDocument(documentId);

            var oldDocument = this.GetDocumentState(documentId)!;
            if (oldDocument.TryGetText(out var oldText) && text == oldText)
            {
                return this;
            }

            return this.WithDocumentState(oldDocument.UpdateText(text, mode), textChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the additional document specified updated to have the text
        /// specified.
        /// </summary>
        public SolutionState WithAdditionalDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            CheckContainsAdditionalDocument(documentId);

            var oldDocument = this.GetAdditionalDocumentState(documentId)!;
            if (oldDocument.TryGetText(out var oldText) && text == oldText)
            {
                return this;
            }

            var newSolution = this.WithAdditionalDocumentState(oldDocument.UpdateText(text, mode), textChanged: true);
            return newSolution;
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the text
        /// specified.
        /// </summary>
        public SolutionState WithAnalyzerConfigDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            CheckContainsAnalyzerConfigDocument(documentId);

            var oldDocument = this.GetAnalyzerConfigDocumentState(documentId)!;
            if (oldDocument.TryGetText(out var oldText) && text == oldText)
            {
                return this;
            }

            return this.WithAnalyzerConfigDocumentState(oldDocument.UpdateText(text, mode), textChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the text
        /// and version specified.
        /// </summary>
        public SolutionState WithDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (textAndVersion == null)
            {
                throw new ArgumentNullException(nameof(textAndVersion));
            }

            CheckContainsDocument(documentId);

            var oldDocument = this.GetDocumentState(documentId)!;

            return WithDocumentState(oldDocument.UpdateText(textAndVersion, mode), textChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the additional document specified updated to have the text
        /// and version specified.
        /// </summary>
        public SolutionState WithAdditionalDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (textAndVersion == null)
            {
                throw new ArgumentNullException(nameof(textAndVersion));
            }

            CheckContainsAdditionalDocument(documentId);

            var oldDocument = this.GetAdditionalDocumentState(documentId)!;

            return WithAdditionalDocumentState(oldDocument.UpdateText(textAndVersion, mode), textChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the analyzer config document specified updated to have the text
        /// and version specified.
        /// </summary>
        public SolutionState WithAnalyzerConfigDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (textAndVersion == null)
            {
                throw new ArgumentNullException(nameof(textAndVersion));
            }

            CheckContainsAnalyzerConfigDocument(documentId);

            var oldDocument = this.GetAnalyzerConfigDocumentState(documentId)!;

            return WithAnalyzerConfigDocumentState(oldDocument.UpdateText(textAndVersion, mode), textChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have a syntax tree
        /// rooted by the specified syntax node.
        /// </summary>
        public SolutionState WithDocumentSyntaxRoot(DocumentId documentId, SyntaxNode root, PreservationMode mode = PreservationMode.PreserveValue)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            CheckContainsDocument(documentId);

            var oldDocument = this.GetDocumentState(documentId)!;
            if (oldDocument.TryGetSyntaxTree(out var oldTree) &&
                oldTree.TryGetRoot(out var oldRoot) &&
                oldRoot == root)
            {
                return this;
            }

            return WithDocumentState(oldDocument.UpdateTree(root, mode), textChanged: true);
        }

        private static async Task<Compilation> UpdateDocumentInCompilationAsync(
            Compilation compilation,
            DocumentState oldDocument,
            DocumentState newDocument,
            CancellationToken cancellationToken)
        {
            return compilation.ReplaceSyntaxTree(
                await oldDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false),
                await newDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the source
        /// code kind specified.
        /// </summary>
        public SolutionState WithDocumentSourceCodeKind(DocumentId documentId, SourceCodeKind sourceCodeKind)
        {
            if (!Enum.IsDefined(typeof(SourceCodeKind), sourceCodeKind))
            {
                throw new ArgumentOutOfRangeException(nameof(sourceCodeKind));
            }

            CheckContainsDocument(documentId);

            var oldDocument = this.GetDocumentState(documentId)!;

            if (oldDocument.SourceCodeKind == sourceCodeKind)
            {
                return this;
            }

            return WithDocumentState(oldDocument.UpdateSourceCodeKind(sourceCodeKind), textChanged: true);
        }

        public SolutionState WithDocumentTextLoader(DocumentId documentId, TextLoader loader, SourceText? text, PreservationMode mode)
        {
            CheckContainsDocument(documentId);

            var oldDocument = this.GetDocumentState(documentId)!;

            // assumes that text has changed. user could have closed a doc without saving and we are loading text from closed file with
            // old content. also this should make sure we don't re-use latest doc version with data associated with opened document.
            return this.WithDocumentState(oldDocument.UpdateText(loader, text, mode), textChanged: true, recalculateDependentVersions: true);
        }

        /// <summary>
        /// Creates a new solution instance with the additional document specified updated to have the text
        /// supplied by the text loader.
        /// </summary>
        public SolutionState WithAdditionalDocumentTextLoader(DocumentId documentId, TextLoader loader, PreservationMode mode)
        {
            CheckContainsAdditionalDocument(documentId);

            var oldDocument = this.GetAdditionalDocumentState(documentId)!;

            // assumes that text has changed. user could have closed a doc without saving and we are loading text from closed file with
            // old content. also this should make sure we don't re-use latest doc version with data associated with opened document.
            return this.WithAdditionalDocumentState(oldDocument.UpdateText(loader, mode), textChanged: true, recalculateDependentVersions: true);
        }

        /// <summary>
        /// Creates a new solution instance with the analyzer config document specified updated to have the text
        /// supplied by the text loader.
        /// </summary>
        public SolutionState WithAnalyzerConfigDocumentTextLoader(DocumentId documentId, TextLoader loader, PreservationMode mode)
        {
            CheckContainsAnalyzerConfigDocument(documentId);

            var oldDocument = this.GetAnalyzerConfigDocumentState(documentId)!;

            // assumes that text has changed. user could have closed a doc without saving and we are loading text from closed file with
            // old content. also this should make sure we don't re-use latest doc version with data associated with opened document.
            return this.WithAnalyzerConfigDocumentState(oldDocument.UpdateText(loader, mode), textChanged: true, recalculateDependentVersions: true);
        }

        private SolutionState WithDocumentState(DocumentState newDocument, bool textChanged = false, bool recalculateDependentVersions = false)
        {
            CheckContainsDocument(newDocument.Id);

            if (newDocument == this.GetDocumentState(newDocument.Id))
            {
                // old and new documents are the same instance
                return this;
            }

            return this.TouchDocument(newDocument.Id, p => p.UpdateDocument(newDocument, textChanged, recalculateDependentVersions));
        }

        private SolutionState TouchDocument(DocumentId documentId, Func<ProjectState, ProjectState> touchProject)
        {
            Debug.Assert(this.ContainsDocument(documentId));

            var oldProject = this.GetProjectState(documentId.ProjectId)!;
            var newProject = touchProject(oldProject);

            if (oldProject == newProject)
            {
                // old and new projects are the same instance
                return this;
            }

            var oldDocument = oldProject.GetDocumentState(documentId);
            var newDocument = newProject.GetDocumentState(documentId);

            return this.ForkProject(newProject, new CompilationTranslationAction.TouchDocumentAction(oldDocument, newDocument));
        }

        private SolutionState WithAdditionalDocumentState(TextDocumentState newDocument, bool textChanged = false, bool recalculateDependentVersions = false)
        {
            CheckContainsAdditionalDocument(newDocument.Id);

            if (newDocument == this.GetAdditionalDocumentState(newDocument.Id))
            {
                // old and new documents are the same instance
                return this;
            }

            var oldProject = this.GetProjectState(newDocument.Id.ProjectId)!;
            var newProject = oldProject.UpdateAdditionalDocument(newDocument, textChanged, recalculateDependentVersions);

            if (oldProject == newProject)
            {
                // old and new projects are the same instance
                return this;
            }

            return this.ForkProject(newProject);
        }

        private SolutionState WithAnalyzerConfigDocumentState(AnalyzerConfigDocumentState newDocument, bool textChanged = false, bool recalculateDependentVersions = false)
        {
            CheckContainsAnalyzerConfigDocument(newDocument.Id);

            if (newDocument == this.GetAnalyzerConfigDocumentState(newDocument.Id))
            {
                // old and new documents are the same instance
                return this;
            }

            var oldProject = this.GetProjectState(newDocument.Id.ProjectId)!;
            var newProject = oldProject.UpdateAnalyzerConfigDocument(newDocument, textChanged, recalculateDependentVersions);

            if (oldProject == newProject)
            {
                // old and new projects are the same instance
                return this;
            }

            return this.ForkProject(newProject, new CompilationTranslationAction.ReplaceAllSyntaxTreesAction(newProject));
        }

        /// <summary>
        /// Creates a new snapshot with an updated project and an action that will produce a new
        /// compilation matching the new project out of an old compilation. All dependent projects
        /// are fixed-up if the change to the new project affects its public metadata, and old
        /// dependent compilations are forgotten.
        /// </summary>
        private SolutionState ForkProject(
            ProjectState newProjectState,
            CompilationTranslationAction? translate = null,
            ProjectDependencyGraph? newDependencyGraph = null,
            ImmutableDictionary<string, ImmutableArray<DocumentId>>? newFilePathToDocumentIdsMap = null,
            bool forkTracker = true)
        {
            var projectId = newProjectState.Id;

            var newStateMap = _projectIdToProjectStateMap.SetItem(projectId, newProjectState);
            newDependencyGraph ??= _dependencyGraph;
            var newTrackerMap = CreateCompilationTrackerMap(projectId, newDependencyGraph);
            // If we have a tracker for this project, then fork it as well (along with the
            // translation action and store it in the tracker map.
            if (newTrackerMap.TryGetValue(projectId, out var tracker))
            {
                newTrackerMap = newTrackerMap.Remove(projectId);

                if (forkTracker)
                {
                    newTrackerMap = newTrackerMap.Add(projectId, tracker.Fork(newProjectState, translate));
                }
            }

            return this.Branch(
                idToProjectStateMap: newStateMap,
                projectIdToTrackerMap: newTrackerMap,
                dependencyGraph: newDependencyGraph,
                filePathToDocumentIdsMap: newFilePathToDocumentIdsMap ?? _filePathToDocumentIdsMap);
        }

        /// <summary>
        /// Gets the set of <see cref="DocumentId"/>s in this <see cref="Solution"/> with a
        /// <see cref="TextDocument.FilePath"/> that matches the given file path.
        /// </summary>
        public ImmutableArray<DocumentId> GetDocumentIdsWithFilePath(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return ImmutableArray.Create<DocumentId>();
            }

            return _filePathToDocumentIdsMap.TryGetValue(filePath!, out var documentIds)
                ? documentIds
                : ImmutableArray.Create<DocumentId>();
        }

        private static ProjectDependencyGraph CreateDependencyGraph(
            IReadOnlyList<ProjectId> projectIds,
            ImmutableDictionary<ProjectId, ProjectState> projectStates)
        {
            var map = projectStates.Values.Select(state => new KeyValuePair<ProjectId, ImmutableHashSet<ProjectId>>(
                    state.Id,
                    state.ProjectReferences.Where(pr => projectStates.ContainsKey(pr.ProjectId)).Select(pr => pr.ProjectId).ToImmutableHashSet()))
                    .Where(pair => !pair.Value.IsEmpty)
                    .ToImmutableDictionary();

            return new ProjectDependencyGraph(projectIds.ToImmutableHashSet(), map);
        }

        private ImmutableDictionary<ProjectId, CompilationTracker> CreateCompilationTrackerMap(ProjectId changedProjectId, ProjectDependencyGraph dependencyGraph)
        {
            var builder = ImmutableDictionary.CreateBuilder<ProjectId, CompilationTracker>();
            IEnumerable<ProjectId>? dependencies = null;

            foreach (var (id, tracker) in _projectIdToTrackerMap)
            {
                if (!tracker.HasCompilation)
                {
                    continue;
                }

                builder.Add(id, CanReuse(id) ? tracker : tracker.Fork(tracker.ProjectState));
            }

            return builder.ToImmutable();

            // Returns true if 'tracker' can be reused for project 'id'
            bool CanReuse(ProjectId id)
            {
                if (id == changedProjectId)
                    return true;

                // Check the dependency graph to see if project 'id' directly or transitively depends on 'projectId'.
                // If the information is not available, do not compute it.
                var forwardDependencies = dependencyGraph.TryGetProjectsThatThisProjectTransitivelyDependsOn(id);
                if (forwardDependencies is object && !forwardDependencies.Contains(changedProjectId))
                {
                    return true;
                }

                // Compute the set of all projects that depend on 'projectId'. This information answers the same
                // question as the previous check, but involves at most one transitive computation within the
                // dependency graph.
                dependencies ??= dependencyGraph.GetProjectsThatTransitivelyDependOnThisProject(changedProjectId);
                return !dependencies.Contains(id);
            }
        }

        /// <summary>
        /// Gets a copy of the solution isolated from the original so that they do not share computed state.
        /// 
        /// Use isolated solutions when doing operations that are likely to access a lot of text,
        /// syntax trees or compilations that are unlikely to be needed again after the operation is done. 
        /// When the isolated solution is reclaimed so will the computed state.
        /// </summary>
        public SolutionState GetIsolatedSolution()
        {
            var forkedMap = ImmutableDictionary.CreateRange<ProjectId, CompilationTracker>(
                _projectIdToTrackerMap.Where(kvp => kvp.Value.HasCompilation)
                                     .Select(kvp => new KeyValuePair<ProjectId, CompilationTracker>(kvp.Key, kvp.Value.Clone())));

            return this.Branch(projectIdToTrackerMap: forkedMap);
        }

        public SolutionState WithOptions(SerializableOptionSet options) => this.Branch(options: options);

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

        private WeakReference<SolutionState>? _latestSolutionWithPartialCompilation;
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
        public SolutionState WithFrozenPartialCompilationIncludingSpecificDocument(DocumentId documentId, CancellationToken cancellationToken)
        {
            try
            {
                var doc = this.GetDocumentState(documentId)!;
                var tree = doc.GetSyntaxTree(cancellationToken);

                using (this.StateLock.DisposableWait(cancellationToken))
                {
                    // in progress solutions are disabled for some testing
                    if (this.Workspace is Workspace ws && ws.TestHookPartialSolutionsDisabled)
                    {
                        return this;
                    }

                    SolutionState? currentPartialSolution = null;
                    if (_latestSolutionWithPartialCompilation != null)
                    {
                        _latestSolutionWithPartialCompilation.TryGetTarget(out currentPartialSolution);
                    }

                    var reuseExistingPartialSolution =
                        currentPartialSolution != null &&
                        (DateTime.UtcNow - _timeOfLatestSolutionWithPartialCompilation).TotalSeconds < 0.1 &&
                        _documentIdOfLatestSolutionWithPartialCompilation == documentId;

                    if (reuseExistingPartialSolution)
                    {
                        SolutionLogger.UseExistingPartialSolution();
                        return currentPartialSolution!;
                    }

                    // if we don't have one or it is stale, create a new partial solution
                    var tracker = this.GetCompilationTracker(documentId.ProjectId);
                    var newTracker = tracker.FreezePartialStateWithTree(this, doc, tree, cancellationToken);

                    var newIdToProjectStateMap = _projectIdToProjectStateMap.SetItem(documentId.ProjectId, newTracker.ProjectState);
                    var newIdToTrackerMap = _projectIdToTrackerMap.SetItem(documentId.ProjectId, newTracker);

                    currentPartialSolution = this.Branch(
                        idToProjectStateMap: newIdToProjectStateMap,
                        projectIdToTrackerMap: newIdToTrackerMap,
                        dependencyGraph: CreateDependencyGraph(ProjectIds, newIdToProjectStateMap));

                    _latestSolutionWithPartialCompilation = new WeakReference<SolutionState>(currentPartialSolution);
                    _timeOfLatestSolutionWithPartialCompilation = DateTime.UtcNow;
                    _documentIdOfLatestSolutionWithPartialCompilation = documentId;

                    SolutionLogger.CreatePartialSolution();
                    return currentPartialSolution;
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Creates a new solution instance with all the documents specified updated to have the same specified text.
        /// </summary>
        public SolutionState WithDocumentText(IEnumerable<DocumentId> documentIds, SourceText text, PreservationMode mode)
        {
            if (documentIds == null)
            {
                throw new ArgumentNullException(nameof(documentIds));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var solution = this;

            foreach (var documentId in documentIds)
            {
                var doc = solution.GetDocumentState(documentId);
                if (doc != null)
                {
                    if (!doc.TryGetText(out var existingText) || existingText != text)
                    {
                        solution = solution.WithDocumentText(documentId, text, mode);
                    }
                }
            }

            return solution;
        }

        public bool TryGetCompilation(ProjectId projectId, [NotNullWhen(returnValue: true)] out Compilation? compilation)
        {
            CheckContainsProject(projectId);
            compilation = null;

            return this.TryGetCompilationTracker(projectId, out var tracker)
                && tracker.TryGetCompilation(out compilation);
        }

        /// <summary>
        /// Returns the compilation for the specified <see cref="ProjectId"/>.  Can return <see langword="null"/> when the project
        /// does not support compilations.
        /// </summary>
        /// <remarks>
        /// The compilation is guaranteed to have a syntax tree for each document of the project.
        /// </remarks>
        private Task<Compilation?> GetCompilationAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            // TODO: figure out where this is called and why the nullable suppression is required
            return GetCompilationAsync(GetProjectState(projectId)!, cancellationToken);
        }

        /// <summary>
        /// Returns the compilation for the specified <see cref="ProjectState"/>.  Can return <see langword="null"/> when the project
        /// does not support compilations.
        /// </summary>
        /// <remarks>
        /// The compilation is guaranteed to have a syntax tree for each document of the project.
        /// </remarks>
        public Task<Compilation?> GetCompilationAsync(ProjectState project, CancellationToken cancellationToken)
        {
            return project.SupportsCompilation
                ? GetCompilationTracker(project.Id).GetCompilationAsync(this, cancellationToken).AsNullable()
                : SpecializedTasks.Null<Compilation>();
        }

        /// <summary>
        /// Return reference completeness for the given project and all projects this references.
        /// </summary>
        public Task<bool> HasSuccessfullyLoadedAsync(ProjectState project, CancellationToken cancellationToken)
        {
            // return HasAllInformation when compilation is not supported. 
            // regardless whether project support compilation or not, if projectInfo is not complete, we can't guarantee its reference completeness
            return project.SupportsCompilation
                ? this.GetCompilationTracker(project.Id).HasSuccessfullyLoadedAsync(this, cancellationToken)
                : project.HasAllInformation ? SpecializedTasks.True : SpecializedTasks.False;
        }

        /// <summary>
        /// Symbols need to be either <see cref="IAssemblySymbol"/> or <see cref="IModuleSymbol"/>.
        /// </summary>
        private static readonly ConditionalWeakTable<ISymbol, ProjectId> s_assemblyOrModuleSymbolToProjectMap =
            new ConditionalWeakTable<ISymbol, ProjectId>();

        private static void RecordSourceOfAssemblySymbol(ISymbol? assemblyOrModuleSymbol, ProjectId projectId)
        {
            // TODO: how would we ever get a null here?
            if (assemblyOrModuleSymbol == null)
            {
                return;
            }

            Contract.ThrowIfNull(projectId);
            // remember which project is associated with this assembly
            if (!s_assemblyOrModuleSymbolToProjectMap.TryGetValue(assemblyOrModuleSymbol, out var tmp))
            {
                // use GetValue to avoid race condition exceptions from Add.
                // the first one to set the value wins.
                s_assemblyOrModuleSymbolToProjectMap.GetValue(assemblyOrModuleSymbol, _ => projectId);
            }
            else
            {
                // sanity check: this should always be true, no matter how many times
                // we attempt to record the association.
                Debug.Assert(tmp == projectId);
            }
        }

        /// <summary>
        /// Get a metadata reference for the project's compilation
        /// </summary>
        public Task<MetadataReference> GetMetadataReferenceAsync(ProjectReference projectReference, ProjectState fromProject, CancellationToken cancellationToken)
        {
            try
            {
                // Get the compilation state for this project.  If it's not already created, then this
                // will create it.  Then force that state to completion and get a metadata reference to it.
                var tracker = this.GetCompilationTracker(projectReference.ProjectId);
                return tracker.GetMetadataReferenceAsync(this, fromProject, projectReference, cancellationToken);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Attempt to get the best readily available compilation for the project. It may be a
        /// partially built compilation.
        /// </summary>
        private MetadataReference? GetPartialMetadataReference(
            ProjectReference projectReference,
            ProjectState fromProject)
        {
            // Try to get the compilation state for this project.  If it doesn't exist, don't do any
            // more work.  
            if (!_projectIdToTrackerMap.TryGetValue(projectReference.ProjectId, out var state))
            {
                return null;
            }

            return state.GetPartialMetadataReference(fromProject, projectReference);
        }

        public async Task<bool> ContainsSymbolsWithNameAsync(ProjectId id, string name, SymbolFilter filter, CancellationToken cancellationToken)
        {
            var result = GetCompilationTracker(id).ContainsSymbolsWithNameFromDeclarationOnlyCompilation(name, filter, cancellationToken);
            if (result.HasValue)
            {
                return result.Value;
            }

            // it looks like declaration compilation doesn't exist yet. we have to build full compilation
            var compilation = await GetCompilationAsync(id, cancellationToken).ConfigureAwait(false);
            if (compilation == null)
            {
                // some projects don't support compilations (e.g., TypeScript) so there's nothing to check
                return false;
            }

            return compilation.ContainsSymbolsWithName(name, filter, cancellationToken);
        }

        public async Task<bool> ContainsSymbolsWithNameAsync(ProjectId id, Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken)
        {
            var result = GetCompilationTracker(id).ContainsSymbolsWithNameFromDeclarationOnlyCompilation(predicate, filter, cancellationToken);
            if (result.HasValue)
            {
                return result.Value;
            }

            // it looks like declaration compilation doesn't exist yet. we have to build full compilation
            var compilation = await GetCompilationAsync(id, cancellationToken).ConfigureAwait(false);
            if (compilation == null)
            {
                // some projects don't support compilations (e.g., TypeScript) so there's nothing to check
                return false;
            }

            return compilation.ContainsSymbolsWithName(predicate, filter, cancellationToken);
        }

        public async Task<ImmutableArray<DocumentState>> GetDocumentsWithNameAsync(
            ProjectId id, Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken)
        {
            // this will be used to find documents that contain declaration information in IDE cache such as DeclarationSyntaxTreeInfo for "NavigateTo"
            var trees = GetCompilationTracker(id).GetSyntaxTreesWithNameFromDeclarationOnlyCompilation(predicate, filter, cancellationToken);
            if (trees != null)
            {
                return ConvertTreesToDocuments(id, trees);
            }

            // it looks like declaration compilation doesn't exist yet. we have to build full compilation
            var compilation = await GetCompilationAsync(id, cancellationToken).ConfigureAwait(false);
            if (compilation == null)
            {
                // some projects don't support compilations (e.g., TypeScript) so there's nothing to check
                return ImmutableArray<DocumentState>.Empty;
            }

            return ConvertTreesToDocuments(
                id, compilation.GetSymbolsWithName(predicate, filter, cancellationToken).SelectMany(s => s.DeclaringSyntaxReferences.Select(r => r.SyntaxTree)));
        }

        private ImmutableArray<DocumentState> ConvertTreesToDocuments(ProjectId id, IEnumerable<SyntaxTree> trees)
        {
            var result = ArrayBuilder<DocumentState>.GetInstance();
            foreach (var tree in trees)
            {
                var document = GetDocumentState(tree, id);
                if (document == null)
                {
                    // ignore trees that are not known to solution such as VB synthesized trees made by compilation.
                    continue;
                }

                result.Add(document);
            }

            return result.ToImmutableAndFree();
        }

        /// <summary>
        /// Gets a <see cref="ProjectDependencyGraph"/> that details the dependencies between projects for this solution.
        /// </summary>
        public ProjectDependencyGraph GetProjectDependencyGraph()
        {
            return _dependencyGraph;
        }

        private void CheckNotContainsProject(ProjectId projectId)
        {
            if (this.ContainsProject(projectId))
            {
                throw new InvalidOperationException(WorkspacesResources.The_solution_already_contains_the_specified_project);
            }
        }

        private void CheckContainsProject(ProjectId projectId)
        {
            if (!this.ContainsProject(projectId))
            {
                throw new InvalidOperationException(WorkspacesResources.The_solution_does_not_contain_the_specified_project);
            }
        }

        private void CheckNotContainsProjectReference(ProjectId projectId, ProjectReference referencedProject)
        {
            if (this.GetProjectState(projectId)!.ProjectReferences.Contains(referencedProject))
            {
                throw new InvalidOperationException(WorkspacesResources.The_project_already_references_the_target_project);
            }
        }

        private void CheckNotContainsTransitiveReference(ProjectId fromProjectId, ProjectId toProjectId)
        {
            var dependents = _dependencyGraph.GetProjectsThatThisProjectTransitivelyDependsOn(fromProjectId);
            if (dependents.Contains(toProjectId))
            {
                throw new InvalidOperationException(WorkspacesResources.The_project_already_transitively_references_the_target_project);
            }
        }

        private void CheckNotSecondSubmissionReference(ProjectId projectId, ProjectId toProjectId)
        {
            var projectState = GetProjectState(projectId);

            if (projectState!.IsSubmission && GetProjectState(toProjectId)!.IsSubmission)
            {
                if (projectState.ProjectReferences.Any(p => GetProjectState(p.ProjectId)!.IsSubmission))
                {
                    throw new InvalidOperationException(WorkspacesResources.This_submission_already_references_another_submission_project);
                }
            }
        }

        private void CheckContainsDocument(DocumentId documentId)
        {
            Debug.Assert(this.ContainsDocument(documentId));

            if (!this.ContainsDocument(documentId))
            {
                throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);
            }
        }

        private void CheckContainsAdditionalDocument(DocumentId documentId)
        {
            Debug.Assert(this.ContainsAdditionalDocument(documentId));

            if (!this.ContainsAdditionalDocument(documentId))
            {
                throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);
            }
        }

        private void CheckContainsAnalyzerConfigDocument(DocumentId documentId)
        {
            Debug.Assert(this.ContainsAnalyzerConfigDocument(documentId));

            if (!this.ContainsAnalyzerConfigDocument(documentId))
            {
                throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);
            }
        }

        internal ImmutableHashSet<string> GetProjectLanguages()
            => GetProjectLanguages(ProjectStates);

        private static ImmutableHashSet<string> GetProjectLanguages(ImmutableDictionary<ProjectId, ProjectState> projectStates)
            => projectStates.Select(p => p.Value.Language).ToImmutableHashSet();
    }
}
