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
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Logging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
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
        private readonly ImmutableHashSet<string> _remoteSupportedLanguages;
        private readonly ImmutableDictionary<string, ImmutableArray<DocumentId>> _filePathToDocumentIdsMap;
        private readonly ProjectDependencyGraph _dependencyGraph;

        public readonly IReadOnlyList<AnalyzerReference> AnalyzerReferences;

        // Values for all these are created on demand.
        private ImmutableDictionary<ProjectId, ICompilationTracker> _projectIdToTrackerMap;

        // Checksums for this solution state
        private readonly ValueSource<SolutionStateChecksums> _lazyChecksums;

        /// <summary>
        /// Mapping from project-id to the checksums needed to synchronize it (and the projects it depends on) over 
        /// to an OOP host.  Lock this specific field before reading/writing to it.
        /// </summary>
        private readonly Dictionary<ProjectId, ValueSource<SolutionStateChecksums>> _lazyProjectChecksums = new();

        // holds on data calculated based on the AnalyzerReferences list
        private readonly Lazy<HostDiagnosticAnalyzers> _lazyAnalyzers;

        /// <summary>
        /// Cache we use to map between unrooted symbols (i.e. assembly, module and dynamic symbols) and the project
        /// they came from.  That way if we are asked about many symbols from the same assembly/module we can answer the
        /// question quickly after computing for the first one.  Created on demand.
        /// </summary>
        private ConditionalWeakTable<ISymbol, ProjectId?>? _unrootedSymbolToProjectId;
        private static readonly Func<ConditionalWeakTable<ISymbol, ProjectId?>> s_createTable = () => new ConditionalWeakTable<ISymbol, ProjectId?>();

        private readonly SourceGeneratedDocumentState? _frozenSourceGeneratedDocumentState;

        private SolutionState(
            BranchId branchId,
            int workspaceVersion,
            SolutionServices solutionServices,
            SolutionInfo.SolutionAttributes solutionAttributes,
            IReadOnlyList<ProjectId> projectIds,
            SerializableOptionSet options,
            IReadOnlyList<AnalyzerReference> analyzerReferences,
            ImmutableDictionary<ProjectId, ProjectState> idToProjectStateMap,
            ImmutableHashSet<string> remoteSupportedLanguages,
            ImmutableDictionary<ProjectId, ICompilationTracker> projectIdToTrackerMap,
            ImmutableDictionary<string, ImmutableArray<DocumentId>> filePathToDocumentIdsMap,
            ProjectDependencyGraph dependencyGraph,
            Lazy<HostDiagnosticAnalyzers>? lazyAnalyzers,
            SourceGeneratedDocumentState? frozenSourceGeneratedDocument)
        {
            _branchId = branchId;
            _workspaceVersion = workspaceVersion;
            _solutionAttributes = solutionAttributes;
            _solutionServices = solutionServices;
            ProjectIds = projectIds;
            Options = options;
            AnalyzerReferences = analyzerReferences;
            _projectIdToProjectStateMap = idToProjectStateMap;
            _remoteSupportedLanguages = remoteSupportedLanguages;
            _projectIdToTrackerMap = projectIdToTrackerMap;
            _filePathToDocumentIdsMap = filePathToDocumentIdsMap;
            _dependencyGraph = dependencyGraph;
            _lazyAnalyzers = lazyAnalyzers ?? CreateLazyHostDiagnosticAnalyzers(analyzerReferences);
            _frozenSourceGeneratedDocumentState = frozenSourceGeneratedDocument;

            // when solution state is changed, we recalculate its checksum
            _lazyChecksums = new AsyncLazy<SolutionStateChecksums>(
                c => ComputeChecksumsAsync(projectsToInclude: null, Options, c), cacheResult: true);

            CheckInvariants();

            // make sure we don't accidentally capture any state but the list of references:
            static Lazy<HostDiagnosticAnalyzers> CreateLazyHostDiagnosticAnalyzers(IReadOnlyList<AnalyzerReference> analyzerReferences)
                => new(() => new HostDiagnosticAnalyzers(analyzerReferences));
        }

        public SolutionState(
            BranchId primaryBranchId,
            SolutionServices solutionServices,
            SolutionInfo.SolutionAttributes solutionAttributes,
            SerializableOptionSet options,
            IReadOnlyList<AnalyzerReference> analyzerReferences)
            : this(
                primaryBranchId,
                workspaceVersion: 0,
                solutionServices,
                solutionAttributes,
                projectIds: SpecializedCollections.EmptyBoxedImmutableArray<ProjectId>(),
                options,
                analyzerReferences,
                idToProjectStateMap: ImmutableDictionary<ProjectId, ProjectState>.Empty,
                remoteSupportedLanguages: ImmutableHashSet<string>.Empty,
                projectIdToTrackerMap: ImmutableDictionary<ProjectId, ICompilationTracker>.Empty,
                filePathToDocumentIdsMap: ImmutableDictionary.Create<string, ImmutableArray<DocumentId>>(StringComparer.OrdinalIgnoreCase),
                dependencyGraph: ProjectDependencyGraph.Empty,
                lazyAnalyzers: null,
                frozenSourceGeneratedDocument: null)
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

        public HostDiagnosticAnalyzers Analyzers => _lazyAnalyzers.Value;

        public SolutionInfo.SolutionAttributes SolutionAttributes => _solutionAttributes;

        public SourceGeneratedDocumentState? FrozenSourceGeneratedDocumentState => _frozenSourceGeneratedDocumentState;

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

        private void CheckInvariants()
        {
            // Run these quick checks all the time.  We need to know immediately if we violate these.
            Contract.ThrowIfFalse(_projectIdToProjectStateMap.Count == ProjectIds.Count);
            Contract.ThrowIfFalse(_projectIdToProjectStateMap.Count == _dependencyGraph.ProjectIds.Count);

            // Only run this in debug builds; even the .Any() call across all projects can be expensive when there's a lot of them.

#if DEBUG

            // An id shouldn't point at a tracker for a different project.
            Contract.ThrowIfTrue(_projectIdToTrackerMap.Any(kvp => kvp.Key != kvp.Value.ProjectState.Id));

            // project ids must be the same:
            Debug.Assert(_projectIdToProjectStateMap.Keys.SetEquals(ProjectIds));
            Debug.Assert(_projectIdToProjectStateMap.Keys.SetEquals(_dependencyGraph.ProjectIds));

            Debug.Assert(_remoteSupportedLanguages.SetEquals(GetRemoteSupportedProjectLanguages(_projectIdToProjectStateMap)));

#endif
        }

        private SolutionState Branch(
            SolutionInfo.SolutionAttributes? solutionAttributes = null,
            IReadOnlyList<ProjectId>? projectIds = null,
            SerializableOptionSet? options = null,
            IReadOnlyList<AnalyzerReference>? analyzerReferences = null,
            ImmutableDictionary<ProjectId, ProjectState>? idToProjectStateMap = null,
            ImmutableHashSet<string>? remoteSupportedProjectLanguages = null,
            ImmutableDictionary<ProjectId, ICompilationTracker>? projectIdToTrackerMap = null,
            ImmutableDictionary<string, ImmutableArray<DocumentId>>? filePathToDocumentIdsMap = null,
            ProjectDependencyGraph? dependencyGraph = null,
            Optional<SourceGeneratedDocumentState?> frozenSourceGeneratedDocument = default)
        {
            var branchId = GetBranchId();

            if (idToProjectStateMap is not null)
            {
                Contract.ThrowIfNull(remoteSupportedProjectLanguages);
            }

            solutionAttributes ??= _solutionAttributes;
            projectIds ??= ProjectIds;
            idToProjectStateMap ??= _projectIdToProjectStateMap;
            remoteSupportedProjectLanguages ??= _remoteSupportedLanguages;
            Debug.Assert(remoteSupportedProjectLanguages.SetEquals(GetRemoteSupportedProjectLanguages(idToProjectStateMap)));
            options ??= Options.UnionWithLanguages(remoteSupportedProjectLanguages);
            analyzerReferences ??= AnalyzerReferences;
            projectIdToTrackerMap ??= _projectIdToTrackerMap;
            filePathToDocumentIdsMap ??= _filePathToDocumentIdsMap;
            dependencyGraph ??= _dependencyGraph;
            var newFrozenSourceGeneratedDocumentState = frozenSourceGeneratedDocument.HasValue ? frozenSourceGeneratedDocument.Value : _frozenSourceGeneratedDocumentState;

            var analyzerReferencesEqual = AnalyzerReferences.SequenceEqual(analyzerReferences);

            if (branchId == _branchId &&
                solutionAttributes == _solutionAttributes &&
                projectIds == ProjectIds &&
                options == Options &&
                analyzerReferencesEqual &&
                idToProjectStateMap == _projectIdToProjectStateMap &&
                projectIdToTrackerMap == _projectIdToTrackerMap &&
                filePathToDocumentIdsMap == _filePathToDocumentIdsMap &&
                dependencyGraph == _dependencyGraph &&
                newFrozenSourceGeneratedDocumentState == _frozenSourceGeneratedDocumentState)
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
                analyzerReferences,
                idToProjectStateMap,
                remoteSupportedProjectLanguages,
                projectIdToTrackerMap,
                filePathToDocumentIdsMap,
                dependencyGraph,
                analyzerReferencesEqual ? _lazyAnalyzers : null,
                newFrozenSourceGeneratedDocumentState);
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
                AnalyzerReferences,
                _projectIdToProjectStateMap,
                _remoteSupportedLanguages,
                _projectIdToTrackerMap,
                _filePathToDocumentIdsMap,
                _dependencyGraph,
                _lazyAnalyzers,
                _frozenSourceGeneratedDocumentState);
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

        internal DocumentState? GetDocumentState(SyntaxTree? syntaxTree, ProjectId? projectId)
        {
            if (syntaxTree != null)
            {
                // is this tree known to be associated with a document?
                var documentId = DocumentState.GetDocumentIdForTree(syntaxTree);
                if (documentId != null && (projectId == null || documentId.ProjectId == projectId))
                {
                    // does this solution even have the document?
                    var projectState = GetProjectState(documentId.ProjectId);
                    if (projectState != null)
                    {
                        var document = projectState.DocumentStates.GetState(documentId);
                        if (document != null)
                        {
                            // does this document really have the syntax tree?
                            if (document.TryGetSyntaxTree(out var documentTree) && documentTree == syntaxTree)
                            {
                                return document;
                            }
                        }
                        else
                        {
                            var generatedDocument = TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(documentId);

                            if (generatedDocument != null)
                            {
                                // does this document really have the syntax tree?
                                if (generatedDocument.TryGetSyntaxTree(out var documentTree) && documentTree == syntaxTree)
                                {
                                    return generatedDocument;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        public Task<VersionStamp> GetDependentVersionAsync(ProjectId projectId, CancellationToken cancellationToken)
            => this.GetCompilationTracker(projectId).GetDependentVersionAsync(this, cancellationToken);

        public Task<VersionStamp> GetDependentSemanticVersionAsync(ProjectId projectId, CancellationToken cancellationToken)
            => this.GetCompilationTracker(projectId).GetDependentSemanticVersionAsync(this, cancellationToken);

        public Task<Checksum> GetDependentChecksumAsync(ProjectId projectId, CancellationToken cancellationToken)
            => this.GetCompilationTracker(projectId).GetDependentChecksumAsync(this, cancellationToken);

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

        /// <summary>
        /// Gets the <see cref="Project"/> associated with an assembly symbol.
        /// </summary>
        public ProjectState? GetProjectState(IAssemblySymbol? assemblySymbol)
        {
            if (assemblySymbol == null)
                return null;

            s_assemblyOrModuleSymbolToProjectMap.TryGetValue(assemblySymbol, out var id);
            return id == null ? null : this.GetProjectState(id);
        }

        private bool TryGetCompilationTracker(ProjectId projectId, [NotNullWhen(returnValue: true)] out ICompilationTracker? tracker)
            => _projectIdToTrackerMap.TryGetValue(projectId, out tracker);

        private static readonly Func<ProjectId, SolutionState, CompilationTracker> s_createCompilationTrackerFunction = CreateCompilationTracker;

        private static CompilationTracker CreateCompilationTracker(ProjectId projectId, SolutionState solution)
        {
            var projectState = solution.GetProjectState(projectId);
            Contract.ThrowIfNull(projectState);
            return new CompilationTracker(projectState);
        }

        private ICompilationTracker GetCompilationTracker(ProjectId projectId)
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
            var newSolutionAttributes = _solutionAttributes.With(version: Version.GetNewerVersion());

            var newProjectIds = ProjectIds.ToImmutableArray().Add(projectId);
            var newStateMap = _projectIdToProjectStateMap.Add(projectId, projectState);
            var newLanguages = RemoteSupportedLanguages.IsSupported(projectState.Language)
                ? _remoteSupportedLanguages.Add(projectState.Language)
                : _remoteSupportedLanguages;

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

            var newTrackerMap = CreateCompilationTrackerMap(projectId, newDependencyGraph);
            var newFilePathToDocumentIdsMap = CreateFilePathToDocumentIdsMapWithAddedDocuments(GetDocumentStates(newStateMap[projectId]));

            return Branch(
                solutionAttributes: newSolutionAttributes,
                projectIds: newProjectIds,
                idToProjectStateMap: newStateMap,
                remoteSupportedProjectLanguages: newLanguages,
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

        private ImmutableDictionary<string, ImmutableArray<DocumentId>> CreateFilePathToDocumentIdsMapWithAddedDocuments(IEnumerable<TextDocumentState> documentStates)
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

            // Remote supported languages only changes if the removed project is the last project of a supported language.
            var newLanguages = _remoteSupportedLanguages;
            if (_projectIdToProjectStateMap.TryGetValue(projectId, out var projectState)
                && RemoteSupportedLanguages.IsSupported(projectState.Language))
            {
                var stillSupportsLanguage = false;
                foreach (var (id, state) in _projectIdToProjectStateMap)
                {
                    if (id == projectId)
                        continue;

                    if (state.Language == projectState.Language)
                    {
                        stillSupportsLanguage = true;
                        break;
                    }
                }

                if (!stillSupportsLanguage)
                {
                    newLanguages = newLanguages.Remove(projectState.Language);
                }
            }

            var newDependencyGraph = _dependencyGraph.WithProjectRemoved(projectId);
            var newTrackerMap = CreateCompilationTrackerMap(projectId, newDependencyGraph);
            var newFilePathToDocumentIdsMap = CreateFilePathToDocumentIdsMapWithRemovedDocuments(GetDocumentStates(_projectIdToProjectStateMap[projectId]));

            return this.Branch(
                solutionAttributes: newSolutionAttributes,
                projectIds: newProjectIds,
                idToProjectStateMap: newStateMap,
                remoteSupportedProjectLanguages: newLanguages,
                projectIdToTrackerMap: newTrackerMap.Remove(projectId),
                filePathToDocumentIdsMap: newFilePathToDocumentIdsMap,
                dependencyGraph: newDependencyGraph);
        }

        private ImmutableDictionary<string, ImmutableArray<DocumentId>> CreateFilePathToDocumentIdsMapWithRemovedDocuments(IEnumerable<TextDocumentState> documentStates)
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
        public SolutionState WithProjectAssemblyName(ProjectId projectId, string assemblyName)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithAssemblyName(assemblyName);

            if (oldProject == newProject)
            {
                return this;
            }

            return ForkProject(newProject, new CompilationAndGeneratorDriverTranslationAction.ProjectAssemblyNameAction(assemblyName));
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the output file path.
        /// </summary>
        public SolutionState WithProjectOutputFilePath(ProjectId projectId, string? outputFilePath)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithOutputFilePath(outputFilePath);

            if (oldProject == newProject)
            {
                return this;
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the output file path.
        /// </summary>
        public SolutionState WithProjectOutputRefFilePath(ProjectId projectId, string? outputRefFilePath)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithOutputRefFilePath(outputRefFilePath);

            if (oldProject == newProject)
            {
                return this;
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the compiler output file path.
        /// </summary>
        public SolutionState WithProjectCompilationOutputInfo(ProjectId projectId, in CompilationOutputInfo info)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithCompilationOutputInfo(info);

            if (oldProject == newProject)
            {
                return this;
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the default namespace.
        /// </summary>
        public SolutionState WithProjectDefaultNamespace(ProjectId projectId, string? defaultNamespace)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithDefaultNamespace(defaultNamespace);

            if (oldProject == newProject)
            {
                return this;
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the name.
        /// </summary>
        public SolutionState WithProjectName(ProjectId projectId, string name)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithName(name);

            if (oldProject == newProject)
            {
                return this;
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the project file path.
        /// </summary>
        public SolutionState WithProjectFilePath(ProjectId projectId, string? filePath)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithFilePath(filePath);

            if (oldProject == newProject)
            {
                return this;
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified compilation options.
        /// </summary>
        public SolutionState WithProjectCompilationOptions(ProjectId projectId, CompilationOptions options)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithCompilationOptions(options);

            if (oldProject == newProject)
            {
                return this;
            }

            return ForkProject(newProject, new CompilationAndGeneratorDriverTranslationAction.ProjectCompilationOptionsAction(newProject, isAnalyzerConfigChange: false));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified parse options.
        /// </summary>
        public SolutionState WithProjectParseOptions(ProjectId projectId, ParseOptions options)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithParseOptions(options);

            if (oldProject == newProject)
            {
                return this;
            }

            if (Workspace.PartialSemanticsEnabled)
            {
                // don't fork tracker with queued action since access via partial semantics can become inconsistent (throw).
                // Since changing options is rare event, it is okay to start compilation building from scratch.
                return ForkProject(newProject, forkTracker: false);
            }
            else
            {
                return ForkProject(newProject, new CompilationAndGeneratorDriverTranslationAction.ReplaceAllSyntaxTreesAction(newProject, isParseOptionChange: true));
            }
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified hasAllInformation.
        /// </summary>
        public SolutionState WithHasAllInformation(ProjectId projectId, bool hasAllInformation)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithHasAllInformation(hasAllInformation);

            if (oldProject == newProject)
            {
                return this;
            }

            // fork without any change on compilation.
            return ForkProject(newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified runAnalyzers.
        /// </summary>
        public SolutionState WithRunAnalyzers(ProjectId projectId, bool runAnalyzers)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithRunAnalyzers(runAnalyzers);

            if (oldProject == newProject)
            {
                return this;
            }

            // fork without any change on compilation.
            return ForkProject(newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include
        /// the specified project references.
        /// </summary>
        public SolutionState AddProjectReferences(ProjectId projectId, IReadOnlyCollection<ProjectReference> projectReferences)
        {
            if (projectReferences.Count == 0)
            {
                return this;
            }

            var oldProject = GetRequiredProjectState(projectId);
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
        public SolutionState RemoveProjectReference(ProjectId projectId, ProjectReference projectReference)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var oldReferences = oldProject.ProjectReferences.ToImmutableArray();

            // Note: uses ProjectReference equality to compare references.
            var newReferences = oldReferences.Remove(projectReference);

            if (oldReferences == newReferences)
            {
                return this;
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
        public SolutionState WithProjectReferences(ProjectId projectId, IReadOnlyList<ProjectReference> projectReferences)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithProjectReferences(projectReferences);
            if (oldProject == newProject)
            {
                return this;
            }

            var newDependencyGraph = _dependencyGraph.WithProjectReferences(projectId, projectReferences);
            return ForkProject(newProject, newDependencyGraph: newDependencyGraph);
        }

        /// <summary>
        /// Creates a new solution instance with the project documents in the order by the specified document ids.
        /// The specified document ids must be the same as what is already in the project; no adding or removing is allowed.
        /// </summary>
        public SolutionState WithProjectDocumentsOrder(ProjectId projectId, ImmutableList<DocumentId> documentIds)
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
                return this;
            }

            return ForkProject(newProject, new CompilationAndGeneratorDriverTranslationAction.ReplaceAllSyntaxTreesAction(newProject, isParseOptionChange: false));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the
        /// specified metadata references.
        /// </summary>
        public SolutionState AddMetadataReferences(ProjectId projectId, IReadOnlyCollection<MetadataReference> metadataReferences)
        {
            if (metadataReferences.Count == 0)
            {
                return this;
            }

            var oldProject = GetRequiredProjectState(projectId);
            var oldReferences = oldProject.MetadataReferences.ToImmutableArray();
            var newReferences = oldReferences.AddRange(metadataReferences);

            return ForkProject(oldProject.WithMetadataReferences(newReferences));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to no longer include
        /// the specified metadata reference.
        /// </summary>
        public SolutionState RemoveMetadataReference(ProjectId projectId, MetadataReference metadataReference)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var oldReferences = oldProject.MetadataReferences.ToImmutableArray();
            var newReferences = oldReferences.Remove(metadataReference);
            if (oldReferences == newReferences)
            {
                return this;
            }

            return ForkProject(oldProject.WithMetadataReferences(newReferences));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include only the
        /// specified metadata references.
        /// </summary>
        public SolutionState WithProjectMetadataReferences(ProjectId projectId, IReadOnlyList<MetadataReference> metadataReferences)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithMetadataReferences(metadataReferences);
            if (oldProject == newProject)
            {
                return this;
            }

            return ForkProject(newProject);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the
        /// specified analyzer references.
        /// </summary>
        public SolutionState AddAnalyzerReferences(ProjectId projectId, ImmutableArray<AnalyzerReference> analyzerReferences)
        {
            if (analyzerReferences.Length == 0)
            {
                return this;
            }

            var oldProject = GetRequiredProjectState(projectId);
            var oldReferences = oldProject.AnalyzerReferences.ToImmutableArray();
            var newReferences = oldReferences.AddRange(analyzerReferences);

            return ForkProject(
                oldProject.WithAnalyzerReferences(newReferences),
                new CompilationAndGeneratorDriverTranslationAction.AddOrRemoveAnalyzerReferencesAction(oldProject.Language, referencesToAdd: analyzerReferences));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to no longer include
        /// the specified analyzer reference.
        /// </summary>
        public SolutionState RemoveAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var oldReferences = oldProject.AnalyzerReferences.ToImmutableArray();
            var newReferences = oldReferences.Remove(analyzerReference);
            if (oldReferences == newReferences)
            {
                return this;
            }

            return ForkProject(
                oldProject.WithAnalyzerReferences(newReferences),
                new CompilationAndGeneratorDriverTranslationAction.AddOrRemoveAnalyzerReferencesAction(oldProject.Language, referencesToRemove: ImmutableArray.Create(analyzerReference)));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include only the
        /// specified analyzer references.
        /// </summary>
        public SolutionState WithProjectAnalyzerReferences(ProjectId projectId, IEnumerable<AnalyzerReference> analyzerReferences)
        {
            var oldProject = GetRequiredProjectState(projectId);
            var newProject = oldProject.WithAnalyzerReferences(analyzerReferences);
            if (oldProject == newProject)
            {
                return this;
            }

            // The .Except() methods here aren't going to terribly cheap, but the assumption is adding or removing just the generators
            // we changed, rather than creating an entire new generator driver from scratch and rerunning all generators, is cheaper
            // in the end. This was written without data backing up that assumption, so if a profile indicates to the contrary,
            // this could be changed.
            var addedReferences = newProject.AnalyzerReferences.Except(oldProject.AnalyzerReferences).ToImmutableArray();
            var removedReferences = oldProject.AnalyzerReferences.Except(newProject.AnalyzerReferences).ToImmutableArray();

            return ForkProject(
                newProject,
                new CompilationAndGeneratorDriverTranslationAction.AddOrRemoveAnalyzerReferencesAction(oldProject.Language, referencesToAdd: addedReferences, referencesToRemove: removedReferences));
        }

        /// <summary>
        /// Create a new solution instance with the corresponding projects updated to include new
        /// documents defined by the document info.
        /// </summary>
        public SolutionState AddDocuments(ImmutableArray<DocumentInfo> documentInfos)
        {
            return AddDocumentsToMultipleProjects(documentInfos,
                (documentInfo, project) => project.CreateDocument(documentInfo, project.ParseOptions),
                (oldProject, documents) => (oldProject.AddDocuments(documents), new CompilationAndGeneratorDriverTranslationAction.AddDocumentsAction(documents)));
        }

        /// <summary>
        /// Core helper that takes a set of <see cref="DocumentInfo" />s and does the application of the appropriate documents to each project.
        /// </summary>
        /// <param name="documentInfos">The set of documents to add.</param>
        /// <param name="addDocumentsToProjectState">Returns the new <see cref="ProjectState"/> with the documents added, and the <see cref="CompilationAndGeneratorDriverTranslationAction"/> needed as well.</param>
        /// <returns></returns>
        private SolutionState AddDocumentsToMultipleProjects<T>(
            ImmutableArray<DocumentInfo> documentInfos,
            Func<DocumentInfo, ProjectState, T> createDocumentState,
            Func<ProjectState, ImmutableArray<T>, (ProjectState newState, CompilationAndGeneratorDriverTranslationAction translationAction)> addDocumentsToProjectState)
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
                (documentInfo, project) => new AdditionalDocumentState(documentInfo, _solutionServices),
                (projectState, documents) => (projectState.AddAdditionalDocuments(documents), new CompilationAndGeneratorDriverTranslationAction.AddAdditionalDocumentsAction(documents)));
        }

        public SolutionState AddAnalyzerConfigDocuments(ImmutableArray<DocumentInfo> documentInfos)
        {
            // Adding a new analyzer config potentially modifies the compilation options
            return AddDocumentsToMultipleProjects(documentInfos,
                (documentInfo, project) => new AnalyzerConfigDocumentState(documentInfo, _solutionServices),
                (oldProject, documents) =>
                {
                    var newProject = oldProject.AddAnalyzerConfigDocuments(documents);
                    return (newProject, new CompilationAndGeneratorDriverTranslationAction.ProjectCompilationOptionsAction(newProject, isAnalyzerConfigChange: true));
                });
        }

        public SolutionState RemoveAnalyzerConfigDocuments(ImmutableArray<DocumentId> documentIds)
        {
            return RemoveDocumentsFromMultipleProjects(documentIds,
                (projectState, documentId) => projectState.AnalyzerConfigDocumentStates.GetRequiredState(documentId),
                (oldProject, documentIds, _) =>
                {
                    var newProject = oldProject.RemoveAnalyzerConfigDocuments(documentIds);
                    return (newProject, new CompilationAndGeneratorDriverTranslationAction.ProjectCompilationOptionsAction(newProject, isAnalyzerConfigChange: true));
                });
        }

        /// <summary>
        /// Creates a new solution instance that no longer includes the specified document.
        /// </summary>
        public SolutionState RemoveDocuments(ImmutableArray<DocumentId> documentIds)
        {
            return RemoveDocumentsFromMultipleProjects(documentIds,
                (projectState, documentId) => projectState.DocumentStates.GetRequiredState(documentId),
                (projectState, documentIds, documentStates) => (projectState.RemoveDocuments(documentIds), new CompilationAndGeneratorDriverTranslationAction.RemoveDocumentsAction(documentStates)));
        }

        private SolutionState RemoveDocumentsFromMultipleProjects<T>(
            ImmutableArray<DocumentId> documentIds,
            Func<ProjectState, DocumentId, T> getExistingTextDocumentState,
            Func<ProjectState, ImmutableArray<DocumentId>, ImmutableArray<T>, (ProjectState newState, CompilationAndGeneratorDriverTranslationAction translationAction)> removeDocumentsFromProjectState)
            where T : TextDocumentState
        {
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
                (projectState, documentId) => projectState.AdditionalDocumentStates.GetRequiredState(documentId),
                (projectState, documentIds, documentStates) => (projectState.RemoveAdditionalDocuments(documentIds), new CompilationAndGeneratorDriverTranslationAction.RemoveAdditionalDocumentsAction(documentStates)));
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the specified name.
        /// </summary>
        public SolutionState WithDocumentName(DocumentId documentId, string name)
        {
            var oldDocument = GetRequiredDocumentState(documentId);
            if (oldDocument.Attributes.Name == name)
            {
                return this;
            }

            return UpdateDocumentState(oldDocument.UpdateName(name));
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to be contained in
        /// the sequence of logical folders.
        /// </summary>
        public SolutionState WithDocumentFolders(DocumentId documentId, IReadOnlyList<string> folders)
        {
            var oldDocument = GetRequiredDocumentState(documentId);
            if (oldDocument.Folders.SequenceEqual(folders))
            {
                return this;
            }

            return UpdateDocumentState(oldDocument.UpdateFolders(folders));
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the specified file path.
        /// </summary>
        public SolutionState WithDocumentFilePath(DocumentId documentId, string? filePath)
        {
            var oldDocument = GetRequiredDocumentState(documentId);
            if (oldDocument.FilePath == filePath)
            {
                return this;
            }

            return UpdateDocumentState(oldDocument.UpdateFilePath(filePath));
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the text
        /// specified.
        /// </summary>
        public SolutionState WithDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var oldDocument = GetRequiredDocumentState(documentId);
            if (oldDocument.TryGetText(out var oldText) && text == oldText)
            {
                return this;
            }

            return UpdateDocumentState(oldDocument.UpdateText(text, mode), textChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the additional document specified updated to have the text
        /// specified.
        /// </summary>
        public SolutionState WithAdditionalDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var oldDocument = GetRequiredAdditionalDocumentState(documentId);
            if (oldDocument.TryGetText(out var oldText) && text == oldText)
            {
                return this;
            }

            return UpdateAdditionalDocumentState(oldDocument.UpdateText(text, mode), textChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the text
        /// specified.
        /// </summary>
        public SolutionState WithAnalyzerConfigDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var oldDocument = GetRequiredAnalyzerConfigDocumentState(documentId);
            if (oldDocument.TryGetText(out var oldText) && text == oldText)
            {
                return this;
            }

            return UpdateAnalyzerConfigDocumentState(oldDocument.UpdateText(text, mode));
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the text
        /// and version specified.
        /// </summary>
        public SolutionState WithDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var oldDocument = GetRequiredDocumentState(documentId);
            if (oldDocument.TryGetTextAndVersion(out var oldTextAndVersion) && textAndVersion == oldTextAndVersion)
            {
                return this;
            }

            return UpdateDocumentState(oldDocument.UpdateText(textAndVersion, mode), textChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the additional document specified updated to have the text
        /// and version specified.
        /// </summary>
        public SolutionState WithAdditionalDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var oldDocument = GetRequiredAdditionalDocumentState(documentId);
            if (oldDocument.TryGetTextAndVersion(out var oldTextAndVersion) && textAndVersion == oldTextAndVersion)
            {
                return this;
            }

            return UpdateAdditionalDocumentState(oldDocument.UpdateText(textAndVersion, mode), textChanged: true);
        }

        /// <summary>
        /// Creates a new solution instance with the analyzer config document specified updated to have the text
        /// and version specified.
        /// </summary>
        public SolutionState WithAnalyzerConfigDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var oldDocument = GetRequiredAnalyzerConfigDocumentState(documentId);
            if (oldDocument.TryGetTextAndVersion(out var oldTextAndVersion) && textAndVersion == oldTextAndVersion)
            {
                return this;
            }

            return UpdateAnalyzerConfigDocumentState(oldDocument.UpdateText(textAndVersion, mode));
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have a syntax tree
        /// rooted by the specified syntax node.
        /// </summary>
        public SolutionState WithDocumentSyntaxRoot(DocumentId documentId, SyntaxNode root, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var oldDocument = GetRequiredDocumentState(documentId);
            if (oldDocument.TryGetSyntaxTree(out var oldTree) &&
                oldTree.TryGetRoot(out var oldRoot) &&
                oldRoot == root)
            {
                return this;
            }

            return UpdateDocumentState(oldDocument.UpdateTree(root, mode), textChanged: true);
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
            var oldDocument = GetRequiredDocumentState(documentId);
            if (oldDocument.SourceCodeKind == sourceCodeKind)
            {
                return this;
            }

            return UpdateDocumentState(oldDocument.UpdateSourceCodeKind(sourceCodeKind), textChanged: true);
        }

        public SolutionState UpdateDocumentTextLoader(DocumentId documentId, TextLoader loader, SourceText? text, PreservationMode mode)
        {
            var oldDocument = GetRequiredDocumentState(documentId);

            // Assumes that text has changed. User could have closed a doc without saving and we are loading text from closed file with
            // old content. Also this should make sure we don't re-use latest doc version with data associated with opened document.
            return UpdateDocumentState(oldDocument.UpdateText(loader, text, mode), textChanged: true, recalculateDependentVersions: true);
        }

        /// <summary>
        /// Creates a new solution instance with the additional document specified updated to have the text
        /// supplied by the text loader.
        /// </summary>
        public SolutionState UpdateAdditionalDocumentTextLoader(DocumentId documentId, TextLoader loader, PreservationMode mode)
        {
            var oldDocument = GetRequiredAdditionalDocumentState(documentId);

            // Assumes that text has changed. User could have closed a doc without saving and we are loading text from closed file with
            // old content. Also this should make sure we don't re-use latest doc version with data associated with opened document.
            return UpdateAdditionalDocumentState(oldDocument.UpdateText(loader, mode), textChanged: true, recalculateDependentVersions: true);
        }

        /// <summary>
        /// Creates a new solution instance with the analyzer config document specified updated to have the text
        /// supplied by the text loader.
        /// </summary>
        public SolutionState UpdateAnalyzerConfigDocumentTextLoader(DocumentId documentId, TextLoader loader, PreservationMode mode)
        {
            var oldDocument = GetRequiredAnalyzerConfigDocumentState(documentId);

            // Assumes that text has changed. User could have closed a doc without saving and we are loading text from closed file with
            // old content. Also this should make sure we don't re-use latest doc version with data associated with opened document.
            return UpdateAnalyzerConfigDocumentState(oldDocument.UpdateText(loader, mode));
        }

        private SolutionState UpdateDocumentState(DocumentState newDocument, bool textChanged = false, bool recalculateDependentVersions = false)
        {
            var oldProject = GetProjectState(newDocument.Id.ProjectId)!;
            var newProject = oldProject.UpdateDocument(newDocument, textChanged, recalculateDependentVersions);

            // This method shouldn't have been called if the document has not changed.
            Debug.Assert(oldProject != newProject);

            var oldDocument = oldProject.DocumentStates.GetRequiredState(newDocument.Id);
            var newFilePathToDocumentIdsMap = CreateFilePathToDocumentIdsMapWithFilePath(newDocument.Id, oldDocument.FilePath, newDocument.FilePath);

            return ForkProject(
                newProject,
                new CompilationAndGeneratorDriverTranslationAction.TouchDocumentAction(oldDocument, newDocument),
                newFilePathToDocumentIdsMap: newFilePathToDocumentIdsMap);
        }

        private SolutionState UpdateAdditionalDocumentState(AdditionalDocumentState newDocument, bool textChanged = false, bool recalculateDependentVersions = false)
        {
            var oldProject = GetProjectState(newDocument.Id.ProjectId)!;
            var newProject = oldProject.UpdateAdditionalDocument(newDocument, textChanged, recalculateDependentVersions);

            // This method shouldn't have been called if the document has not changed.
            Debug.Assert(oldProject != newProject);

            var oldDocument = oldProject.AdditionalDocumentStates.GetRequiredState(newDocument.Id);

            return ForkProject(
                newProject,
                translate: new CompilationAndGeneratorDriverTranslationAction.TouchAdditionalDocumentAction(oldDocument, newDocument));
        }

        private SolutionState UpdateAnalyzerConfigDocumentState(AnalyzerConfigDocumentState newDocument)
        {
            var oldProject = GetProjectState(newDocument.Id.ProjectId)!;
            var newProject = oldProject.UpdateAnalyzerConfigDocument(newDocument);

            // This method shouldn't have been called if the document has not changed.
            Debug.Assert(oldProject != newProject);

            return ForkProject(newProject,
                newProject.CompilationOptions != null ? new CompilationAndGeneratorDriverTranslationAction.ProjectCompilationOptionsAction(newProject, isAnalyzerConfigChange: true) : null);
        }

        /// <summary>
        /// Creates a new snapshot with an updated project and an action that will produce a new
        /// compilation matching the new project out of an old compilation. All dependent projects
        /// are fixed-up if the change to the new project affects its public metadata, and old
        /// dependent compilations are forgotten.
        /// </summary>
        private SolutionState ForkProject(
            ProjectState newProjectState,
            CompilationAndGeneratorDriverTranslationAction? translate = null,
            ProjectDependencyGraph? newDependencyGraph = null,
            ImmutableDictionary<string, ImmutableArray<DocumentId>>? newFilePathToDocumentIdsMap = null,
            bool forkTracker = true)
        {
            var projectId = newProjectState.Id;

            Contract.ThrowIfFalse(_projectIdToProjectStateMap.ContainsKey(projectId));
            var newStateMap = _projectIdToProjectStateMap.SetItem(projectId, newProjectState);

            // Remote supported languages can only change if the project changes language. This is an unexpected edge
            // case, so it's not optimized for incremental updates.
            var newLanguages = !_projectIdToProjectStateMap.TryGetValue(projectId, out var projectState) || projectState.Language != newProjectState.Language
                ? GetRemoteSupportedProjectLanguages(newStateMap)
                : _remoteSupportedLanguages;

            newDependencyGraph ??= _dependencyGraph;
            var newTrackerMap = CreateCompilationTrackerMap(projectId, newDependencyGraph);
            // If we have a tracker for this project, then fork it as well (along with the
            // translation action and store it in the tracker map.
            if (newTrackerMap.TryGetValue(projectId, out var tracker))
            {
                newTrackerMap = newTrackerMap.Remove(projectId);

                if (forkTracker)
                {
                    newTrackerMap = newTrackerMap.Add(projectId, tracker.Fork(_solutionServices, newProjectState, translate));
                }
            }

            return this.Branch(
                idToProjectStateMap: newStateMap,
                remoteSupportedProjectLanguages: newLanguages,
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

        private ImmutableDictionary<ProjectId, ICompilationTracker> CreateCompilationTrackerMap(ProjectId changedProjectId, ProjectDependencyGraph dependencyGraph)
        {
            var builder = ImmutableDictionary.CreateBuilder<ProjectId, ICompilationTracker>();
            IEnumerable<ProjectId>? dependencies = null;

            foreach (var (id, tracker) in _projectIdToTrackerMap)
                builder.Add(id, CanReuse(id) ? tracker : tracker.Fork(_solutionServices, tracker.ProjectState));

            return builder.ToImmutable();

            // Returns true if 'tracker' can be reused for project 'id'
            bool CanReuse(ProjectId id)
            {
                if (id == changedProjectId)
                {
                    return true;
                }

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

        public SolutionState WithOptions(SerializableOptionSet options)
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
                var doc = this.GetRequiredDocumentState(documentId);
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

                    Contract.ThrowIfFalse(_projectIdToProjectStateMap.ContainsKey(documentId.ProjectId));
                    var newIdToProjectStateMap = _projectIdToProjectStateMap.SetItem(documentId.ProjectId, newTracker.ProjectState);
                    var newLanguages = _remoteSupportedLanguages;
                    var newIdToTrackerMap = _projectIdToTrackerMap.SetItem(documentId.ProjectId, newTracker);

                    currentPartialSolution = this.Branch(
                        idToProjectStateMap: newIdToProjectStateMap,
                        remoteSupportedProjectLanguages: newLanguages,
                        projectIdToTrackerMap: newIdToTrackerMap,
                        dependencyGraph: CreateDependencyGraph(ProjectIds, newIdToProjectStateMap));

                    _latestSolutionWithPartialCompilation = new WeakReference<SolutionState>(currentPartialSolution);
                    _timeOfLatestSolutionWithPartialCompilation = DateTime.UtcNow;
                    _documentIdOfLatestSolutionWithPartialCompilation = documentId;

                    SolutionLogger.CreatePartialSolution();
                    return currentPartialSolution;
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Creates a new solution instance with all the documents specified updated to have the same specified text.
        /// </summary>
        public SolutionState WithDocumentText(IEnumerable<DocumentId?> documentIds, SourceText text, PreservationMode mode)
        {
            var solution = this;

            foreach (var documentId in documentIds)
            {
                if (documentId == null)
                {
                    continue;
                }

                var doc = GetProjectState(documentId.ProjectId)?.DocumentStates.GetState(documentId);
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
        /// Returns the generated document states for source generated documents.
        /// </summary>
        public ValueTask<TextDocumentStates<SourceGeneratedDocumentState>> GetSourceGeneratedDocumentStatesAsync(ProjectState project, CancellationToken cancellationToken)
        {
            return project.SupportsCompilation
                ? GetCompilationTracker(project.Id).GetSourceGeneratedDocumentStatesAsync(this, cancellationToken)
                : new(TextDocumentStates<SourceGeneratedDocumentState>.Empty);
        }

        /// <summary>
        /// Returns the <see cref="SourceGeneratedDocumentState"/> for a source generated document that has already been generated and observed.
        /// </summary>
        /// <remarks>
        /// This is only safe to call if you already have seen the SyntaxTree or equivalent that indicates the document state has already been
        /// generated. This method exists to implement <see cref="Solution.GetDocument(SyntaxTree?)"/> and is best avoided unless you're doing something
        /// similarly tricky like that.
        /// </remarks>
        public SourceGeneratedDocumentState? TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(DocumentId documentId)
        {
            return GetCompilationTracker(documentId.ProjectId).TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(documentId);
        }

        /// <summary>
        /// Returns a new SolutionState that will always produce a specific output for a generated file. This is used only in the
        /// implementation of <see cref="TextExtensions.GetOpenDocumentInCurrentContextWithChanges"/> where if a user has a source
        /// generated file open, we need to make sure everything lines up.
        /// </summary>
        public SolutionState WithFrozenSourceGeneratedDocument(SourceGeneratedDocumentIdentity documentIdentity, SourceText sourceText)
        {
            // We won't support freezing multiple source generated documents at once. Although nothing in the implementation
            // of this method would have problems, this simplifies the handling of serializing this solution to out-of-proc.
            // Since we only produce these snapshots from an open document, there should be no way to observe this, so this assertion
            // also serves as a good check on the system. If down the road we need to support this, we can remove this check and
            // update the out-of-process serialization logic accordingly.
            Contract.ThrowIfTrue(_frozenSourceGeneratedDocumentState != null, "We shouldn't be calling WithFrozenSourceGeneratedDocument on a solution with a frozen source generated document.");

            var existingGeneratedState = TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(documentIdentity.DocumentId);
            SourceGeneratedDocumentState newGeneratedState;

            if (existingGeneratedState != null)
            {
                newGeneratedState = existingGeneratedState.WithUpdatedGeneratedContent(sourceText, existingGeneratedState.ParseOptions);

                // If the content already matched, we can just reuse the existing state
                if (newGeneratedState == existingGeneratedState)
                {
                    return this;
                }
            }
            else
            {
                var projectState = GetRequiredProjectState(documentIdentity.DocumentId.ProjectId);
                newGeneratedState = SourceGeneratedDocumentState.Create(
                    documentIdentity,
                    sourceText,
                    projectState.ParseOptions!,
                    projectState.LanguageServices,
                    _solutionServices);
            }

            var projectId = documentIdentity.DocumentId.ProjectId;
            var newTrackerMap = CreateCompilationTrackerMap(projectId, _dependencyGraph);

            // We want to create a new snapshot with a new compilation tracker that will do this replacement.
            // If we already have an existing tracker we'll just wrap that (so we also are reusing any underlying
            // computations). If we don't have one, we'll create one and then wrap it.
            if (!newTrackerMap.TryGetValue(projectId, out var existingTracker))
            {
                existingTracker = CreateCompilationTracker(projectId, this);
            }

            newTrackerMap = newTrackerMap.SetItem(
                projectId,
                new GeneratedFileReplacingCompilationTracker(existingTracker, newGeneratedState));

            return this.Branch(
                projectIdToTrackerMap: newTrackerMap,
                frozenSourceGeneratedDocument: newGeneratedState);
        }

        /// <summary>
        /// Undoes the operation of <see cref="WithFrozenSourceGeneratedDocument"/>; any frozen source generated document is allowed
        /// to have it's real output again.
        /// </summary>
        public SolutionState WithoutFrozenSourceGeneratedDocuments()
        {
            // If there's nothing frozen, there's nothing to do.
            if (_frozenSourceGeneratedDocumentState == null)
                return this;

            var projectId = _frozenSourceGeneratedDocumentState.Id.ProjectId;

            // Since we previously froze this document, we should have a CompilationTracker entry for it, and it should be a
            // GeneratedFileReplacingCompilationTracker. To undo the operation, we'll just restore the original CompilationTracker.
            var newTrackerMap = CreateCompilationTrackerMap(projectId, _dependencyGraph);
            Contract.ThrowIfFalse(newTrackerMap.TryGetValue(projectId, out var existingTracker));
            var replacingItemTracker = existingTracker as GeneratedFileReplacingCompilationTracker;
            Contract.ThrowIfNull(replacingItemTracker);
            newTrackerMap = newTrackerMap.SetItem(projectId, replacingItemTracker.UnderlyingTracker);

            return this.Branch(
                projectIdToTrackerMap: newTrackerMap,
                frozenSourceGeneratedDocument: null);
        }

        /// <summary>
        /// Symbols need to be either <see cref="IAssemblySymbol"/> or <see cref="IModuleSymbol"/>.
        /// </summary>
        private static readonly ConditionalWeakTable<ISymbol, ProjectId> s_assemblyOrModuleSymbolToProjectMap = new();

        /// <summary>
        /// Get a metadata reference for the project's compilation.  Returns <see langword="null"/> upon failure, which 
        /// can happen when trying to build a skeleton reference that fails to build.
        /// </summary>
        public Task<MetadataReference?> GetMetadataReferenceAsync(ProjectReference projectReference, ProjectState fromProject, CancellationToken cancellationToken)
        {
            try
            {
                // Get the compilation state for this project.  If it's not already created, then this
                // will create it.  Then force that state to completion and get a metadata reference to it.
                var tracker = this.GetCompilationTracker(projectReference.ProjectId);
                return GetMetadataReferenceAsync(tracker, fromProject, projectReference, cancellationToken);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Get a metadata reference to this compilation info's compilation with respect to
        /// another project. For cross language references produce a skeletal assembly. If the
        /// compilation is not available, it is built. If a skeletal assembly reference is
        /// needed and does not exist, it is also built.
        /// </summary>
        private async Task<MetadataReference?> GetMetadataReferenceAsync(
            ICompilationTracker tracker, ProjectState fromProject, ProjectReference projectReference, CancellationToken cancellationToken)
        {
            try
            {
                // If same language then we can wrap the other project's compilation into a compilation reference
                if (tracker.ProjectState.LanguageServices == fromProject.LanguageServices)
                {
                    // otherwise, base it off the compilation by building it first.
                    var compilation = await tracker.GetCompilationAsync(this, cancellationToken).ConfigureAwait(false);
                    return compilation.ToMetadataReference(projectReference.Aliases, projectReference.EmbedInteropTypes);
                }

                // otherwise get a metadata only image reference that is built by emitting the metadata from the
                // referenced project's compilation and re-importing it.
                using (Logger.LogBlock(FunctionId.Workspace_SkeletonAssembly_GetMetadataOnlyImage, cancellationToken))
                {
                    var properties = new MetadataReferenceProperties(aliases: projectReference.Aliases, embedInteropTypes: projectReference.EmbedInteropTypes);
                    return await tracker.SkeletonReferenceCache.GetOrBuildReferenceAsync(
                        tracker, this, properties, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.Critical))
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

        private void CheckContainsProject(ProjectId projectId)
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

        internal ImmutableHashSet<string> GetRemoteSupportedProjectLanguages()
            => _remoteSupportedLanguages;

        private static ImmutableHashSet<string> GetRemoteSupportedProjectLanguages(ImmutableDictionary<ProjectId, ProjectState> projectStates)
        {
            var builder = ImmutableHashSet.CreateBuilder<string>();
            foreach (var projectState in projectStates)
            {
                if (RemoteSupportedLanguages.IsSupported(projectState.Value.Language))
                {
                    builder.Add(projectState.Value.Language);
                }
            }

            return builder.ToImmutable();
        }

        internal TestAccessor GetTestAccessor() => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly SolutionState _solutionState;

            public TestAccessor(SolutionState solutionState)
            {
                _solutionState = solutionState;
            }

            public GeneratorDriver? GetGeneratorDriver(Project project)
            {
                return project.SupportsCompilation ? _solutionState.GetCompilationTracker(project.Id).GeneratorDriver : null;
            }
        }
    }
}
