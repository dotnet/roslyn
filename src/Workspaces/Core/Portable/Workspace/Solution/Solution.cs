// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a set of projects and their source code documents. 
    /// </summary>
    public partial class Solution
    {
        // SolutionState that doesn't hold onto Project/Document
        private readonly SolutionState _state;

        // Values for all these are created on demand.
        private ImmutableHashMap<ProjectId, Project> _projectIdToProjectMap;

        private Solution(SolutionState state)
        {
            _projectIdToProjectMap = ImmutableHashMap<ProjectId, Project>.Empty;
            _state = state;
        }

        internal Solution(Workspace workspace, SolutionInfo.SolutionAttributes solutionAttributes, SerializableOptionSet options, IReadOnlyList<AnalyzerReference> analyzerReferences)
            : this(new SolutionState(workspace.PrimaryBranchId, new SolutionServices(workspace), solutionAttributes, options, analyzerReferences))
        {
        }

        internal SolutionState State => _state;

        internal int WorkspaceVersion => _state.WorkspaceVersion;

        internal SolutionServices Services => _state.Services;

        internal BranchId BranchId => _state.BranchId;

        internal ProjectState? GetProjectState(ProjectId projectId) => _state.GetProjectState(projectId);

        /// <summary>
        /// The Workspace this solution is associated with.
        /// </summary>
        public Workspace Workspace => _state.Workspace;

        /// <summary>
        /// The Id of the solution. Multiple solution instances may share the same Id.
        /// </summary>
        public SolutionId Id => _state.Id;

        /// <summary>
        /// The path to the solution file or null if there is no solution file.
        /// </summary>
        public string? FilePath => _state.FilePath;

        /// <summary>
        /// The solution version. This equates to the solution file's version.
        /// </summary>
        public VersionStamp Version => _state.Version;

        /// <summary>
        /// A list of all the ids for all the projects contained by the solution.
        /// </summary>
        public IReadOnlyList<ProjectId> ProjectIds => _state.ProjectIds;

        /// <summary>
        /// A list of all the projects contained by the solution.
        /// </summary>
        public IEnumerable<Project> Projects => ProjectIds.Select(id => GetProject(id)!);

        /// <summary>
        /// The version of the most recently modified project.
        /// </summary>
        public VersionStamp GetLatestProjectVersion() => _state.GetLatestProjectVersion();

        /// <summary>
        /// True if the solution contains a project with the specified project ID.
        /// </summary>
        public bool ContainsProject([NotNullWhen(returnValue: true)] ProjectId? projectId) => _state.ContainsProject(projectId);

        /// <summary>
        /// Gets the project in this solution with the specified project ID. 
        /// 
        /// If the id is not an id of a project that is part of this solution the method returns null.
        /// </summary>
        public Project? GetProject(ProjectId? projectId)
        {
            if (this.ContainsProject(projectId))
            {
                return ImmutableHashMapExtensions.GetOrAdd(ref _projectIdToProjectMap, projectId, s_createProjectFunction, this);
            }

            return null;
        }

        private static readonly Func<ProjectId, Solution, Project> s_createProjectFunction = CreateProject;
        private static Project CreateProject(ProjectId projectId, Solution solution)
        {
            var state = solution.State.GetProjectState(projectId);
            Contract.ThrowIfNull(state);
            return new Project(solution, state);
        }

#pragma warning disable IDE0060 // Remove unused parameter 'cancellationToken' - shipped public API
        /// <summary>
        /// Gets the <see cref="Project"/> associated with an assembly symbol.
        /// </summary>
        public Project? GetProject(IAssemblySymbol assemblySymbol,
            CancellationToken cancellationToken = default)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            var projectState = _state.GetProjectState(assemblySymbol);

            return projectState == null ? null : GetProject(projectState.Id);
        }

        /// <summary>
        /// Given a <paramref name="symbol"/> returns the <see cref="ProjectId"/> of the <see cref="Project"/> it came
        /// from.  Returns <see langword="null"/> if <paramref name="symbol"/> does not come from any project in this solution.
        /// </summary>
        /// <remarks>
        /// This function differs from <see cref="GetProject(IAssemblySymbol, CancellationToken)"/> in terms of how it
        /// treats <see cref="IAssemblySymbol"/>s.  Specifically, say there is the following:
        ///
        /// <c>
        /// Project-A, containing Symbol-A.<para/>
        /// Project-B, with a reference to Project-A, and usage of Symbol-A.
        /// </c>
        ///
        /// It is possible (with retargeting, and other complex cases) that Symbol-A from Project-B will be a different
        /// symbol than Symbol-A from Project-A.  However, <see cref="GetProject(IAssemblySymbol, CancellationToken)"/>
        /// will always try to return Project-A for either of the Symbol-A's, as it prefers to return the original
        /// Source-Project of the original definition, not the project that actually produced the symbol.  For many
        /// features this is an acceptable abstraction.  However, for some cases (Find-References in particular) it is
        /// necessary to resolve symbols back to the actual project/compilation that produced them for correctness.
        /// </remarks>
        internal ProjectId? GetOriginatingProjectId(ISymbol symbol)
            => _state.GetOriginatingProjectId(symbol);

        /// <inheritdoc cref="GetOriginatingProjectId"/>
        internal Project? GetOriginatingProject(ISymbol symbol)
            => GetProject(GetOriginatingProjectId(symbol));

        /// <summary>
        /// True if the solution contains the document in one of its projects
        /// </summary>
        public bool ContainsDocument([NotNullWhen(returnValue: true)] DocumentId? documentId) => _state.ContainsDocument(documentId);

        /// <summary>
        /// True if the solution contains the additional document in one of its projects
        /// </summary>
        public bool ContainsAdditionalDocument([NotNullWhen(returnValue: true)] DocumentId? documentId) => _state.ContainsAdditionalDocument(documentId);

        /// <summary>
        /// True if the solution contains the analyzer config document in one of its projects
        /// </summary>
        public bool ContainsAnalyzerConfigDocument([NotNullWhen(returnValue: true)] DocumentId? documentId) => _state.ContainsAnalyzerConfigDocument(documentId);

        /// <summary>
        /// Gets the documentId in this solution with the specified syntax tree.
        /// </summary>
        public DocumentId? GetDocumentId(SyntaxTree? syntaxTree) => GetDocumentId(syntaxTree, projectId: null);

        /// <summary>
        /// Gets the documentId in this solution with the specified syntax tree.
        /// </summary>
        public DocumentId? GetDocumentId(SyntaxTree? syntaxTree, ProjectId? projectId)
        {
            return State.GetDocumentState(syntaxTree, projectId)?.Id;
        }

        /// <summary>
        /// Gets the document in this solution with the specified document ID.
        /// </summary>
        public Document? GetDocument(DocumentId? documentId)
            => GetProject(documentId?.ProjectId)?.GetDocument(documentId!);

        /// <summary>
        /// Gets a document or a source generated document in this solution with the specified document ID.
        /// </summary>
        internal ValueTask<Document?> GetDocumentAsync(DocumentId? documentId, bool includeSourceGenerated = false, CancellationToken cancellationToken = default)
        {
            var project = GetProject(documentId?.ProjectId);
            if (project == null)
            {
                return default;
            }

            Contract.ThrowIfNull(documentId);
            return project.GetDocumentAsync(documentId, includeSourceGenerated, cancellationToken);
        }

        /// <summary>
        /// Gets a document, additional document, analyzer config document or a source generated document in this solution with the specified document ID.
        /// </summary>
        internal ValueTask<TextDocument?> GetTextDocumentAsync(DocumentId? documentId, CancellationToken cancellationToken = default)
        {
            var project = GetProject(documentId?.ProjectId);
            if (project == null)
            {
                return default;
            }

            Contract.ThrowIfNull(documentId);
            return project.GetTextDocumentAsync(documentId, cancellationToken);
        }

        /// <summary>
        /// Gets the additional document in this solution with the specified document ID.
        /// </summary>
        public TextDocument? GetAdditionalDocument(DocumentId? documentId)
        {
            if (this.ContainsAdditionalDocument(documentId))
            {
                return this.GetProject(documentId.ProjectId)!.GetAdditionalDocument(documentId);
            }

            return null;
        }

        /// <summary>
        /// Gets the analyzer config document in this solution with the specified document ID.
        /// </summary>
        public AnalyzerConfigDocument? GetAnalyzerConfigDocument(DocumentId? documentId)
        {
            if (this.ContainsAnalyzerConfigDocument(documentId))
            {
                return this.GetProject(documentId.ProjectId)!.GetAnalyzerConfigDocument(documentId);
            }

            return null;
        }

        public ValueTask<SourceGeneratedDocument?> GetSourceGeneratedDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            var project = GetProject(documentId.ProjectId);

            if (project == null)
            {
                return new(result: null);
            }
            else
            {
                return project.GetSourceGeneratedDocumentAsync(documentId, cancellationToken);
            }
        }

        /// <summary>
        /// Gets the document in this solution with the specified syntax tree.
        /// </summary>
        public Document? GetDocument(SyntaxTree? syntaxTree)
            => this.GetDocument(syntaxTree, projectId: null);

        internal Document? GetDocument(SyntaxTree? syntaxTree, ProjectId? projectId)
        {
            if (syntaxTree != null)
            {
                var documentState = State.GetDocumentState(syntaxTree, projectId);

                if (documentState is SourceGeneratedDocumentState)
                {
                    // We have the underlying state, but we need to get the wrapper SourceGeneratedDocument object. The wrapping is maintained by
                    // the Project object, so we'll now fetch the project and ask it to get the SourceGeneratedDocument wrapper. Under the covers this
                    // implicity may call to fetch the SourceGeneratedDocumentState a second time but that's not expensive.
                    var generatedDocument = this.GetRequiredProject(documentState.Id.ProjectId).TryGetSourceGeneratedDocumentForAlreadyGeneratedId(documentState.Id);
                    Contract.ThrowIfNull(generatedDocument, "The call to GetDocumentState found a SourceGeneratedDocumentState, so we should have found it now.");
                    return generatedDocument;
                }
                else if (documentState is DocumentState)
                {
                    return GetDocument(documentState.Id)!;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a new solution instance that includes a project with the specified language and names.
        /// Returns the new project.
        /// </summary>
        public Project AddProject(string name, string assemblyName, string language)
        {
            var id = ProjectId.CreateNewId(debugName: name);
            return this.AddProject(id, name, assemblyName, language).GetProject(id)!;
        }

        /// <summary>
        /// Creates a new solution instance that includes a project with the specified language and names.
        /// </summary>
        public Solution AddProject(ProjectId projectId, string name, string assemblyName, string language)
            => this.AddProject(ProjectInfo.Create(projectId, VersionStamp.Create(), name, assemblyName, language));

        /// <summary>
        /// Create a new solution instance that includes a project with the specified project information.
        /// </summary>
        public Solution AddProject(ProjectInfo projectInfo)
        {
            var newState = _state.AddProject(projectInfo);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance without the project specified.
        /// </summary>
        public Solution RemoveProject(ProjectId projectId)
        {
            var newState = _state.RemoveProject(projectId);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the new
        /// assembly name.
        /// </summary>
        public Solution WithProjectAssemblyName(ProjectId projectId, string assemblyName)
        {
            CheckContainsProject(projectId);

            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            var newState = _state.WithProjectAssemblyName(projectId, assemblyName);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the output file path.
        /// </summary>
        public Solution WithProjectOutputFilePath(ProjectId projectId, string? outputFilePath)
        {
            CheckContainsProject(projectId);

            var newState = _state.WithProjectOutputFilePath(projectId, outputFilePath);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the reference assembly output file path.
        /// </summary>
        public Solution WithProjectOutputRefFilePath(ProjectId projectId, string? outputRefFilePath)
        {
            CheckContainsProject(projectId);

            var newState = _state.WithProjectOutputRefFilePath(projectId, outputRefFilePath);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the compiler output file path.
        /// </summary>
        public Solution WithProjectCompilationOutputInfo(ProjectId projectId, in CompilationOutputInfo info)
        {
            CheckContainsProject(projectId);

            var newState = _state.WithProjectCompilationOutputInfo(projectId, info);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the default namespace.
        /// </summary>
        public Solution WithProjectDefaultNamespace(ProjectId projectId, string? defaultNamespace)
        {
            CheckContainsProject(projectId);

            var newState = _state.WithProjectDefaultNamespace(projectId, defaultNamespace);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the name.
        /// </summary>
        public Solution WithProjectName(ProjectId projectId, string name)
        {
            CheckContainsProject(projectId);

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var newState = _state.WithProjectName(projectId, name);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the project file path.
        /// </summary>
        public Solution WithProjectFilePath(ProjectId projectId, string? filePath)
        {
            CheckContainsProject(projectId);

            var newState = _state.WithProjectFilePath(projectId, filePath);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified compilation options.
        /// </summary>
        public Solution WithProjectCompilationOptions(ProjectId projectId, CompilationOptions options)
        {
            CheckContainsProject(projectId);

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var newState = _state.WithProjectCompilationOptions(projectId, options);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified parse options.
        /// </summary>
        public Solution WithProjectParseOptions(ProjectId projectId, ParseOptions options)
        {
            CheckContainsProject(projectId);

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var newState = _state.WithProjectParseOptions(projectId, options);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified hasAllInformation.
        /// </summary>
        // TODO: https://github.com/dotnet/roslyn/issues/42449 make it public
        internal Solution WithHasAllInformation(ProjectId projectId, bool hasAllInformation)
        {
            CheckContainsProject(projectId);

            var newState = _state.WithHasAllInformation(projectId, hasAllInformation);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified runAnalyzers.
        /// </summary>
        // TODO: https://github.com/dotnet/roslyn/issues/42449 make it public
        internal Solution WithRunAnalyzers(ProjectId projectId, bool runAnalyzers)
        {
            CheckContainsProject(projectId);

            var newState = _state.WithRunAnalyzers(projectId, runAnalyzers);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the project documents in the order by the specified document ids.
        /// The specified document ids must be the same as what is already in the project; no adding or removing is allowed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="documentIds"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The solution does not contain <paramref name="projectId"/>.</exception>
        /// <exception cref="ArgumentException">The number of documents specified in <paramref name="documentIds"/> is not equal to the number of documents in project <paramref name="projectId"/>.</exception>
        /// <exception cref="InvalidOperationException">Document specified in <paramref name="documentIds"/> does not exist in project <paramref name="projectId"/>.</exception>
        public Solution WithProjectDocumentsOrder(ProjectId projectId, ImmutableList<DocumentId> documentIds)
        {
            CheckContainsProject(projectId);

            if (documentIds == null)
            {
                throw new ArgumentNullException(nameof(documentIds));
            }

            var newState = _state.WithProjectDocumentsOrder(projectId, documentIds);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include
        /// the specified project reference.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="projectReference"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The solution does not contain <paramref name="projectId"/>.</exception>
        /// <exception cref="InvalidOperationException">The project already references the target project.</exception>
        public Solution AddProjectReference(ProjectId projectId, ProjectReference projectReference)
        {
            return AddProjectReferences(projectId,
                SpecializedCollections.SingletonEnumerable(
                    projectReference ?? throw new ArgumentNullException(nameof(projectReference))));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include
        /// the specified project references.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="projectReferences"/> contains <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="projectReferences"/> contains duplicate items.</exception>
        /// <exception cref="InvalidOperationException">The solution does not contain <paramref name="projectId"/>.</exception>
        /// <exception cref="InvalidOperationException">The project already references the target project.</exception>
        /// <exception cref="InvalidOperationException">Adding the project reference would create a circular dependency.</exception>
        public Solution AddProjectReferences(ProjectId projectId, IEnumerable<ProjectReference> projectReferences)
        {
            CheckContainsProject(projectId);

            // avoid enumerating multiple times:
            var collection = projectReferences?.ToCollection();

            PublicContract.RequireUniqueNonNullItems(collection, nameof(projectReferences));

            foreach (var projectReference in collection)
            {
                if (_state.ContainsProjectReference(projectId, projectReference))
                {
                    throw new InvalidOperationException(WorkspacesResources.The_project_already_references_the_target_project);
                }
            }

            CheckCircularProjectReferences(projectId, collection);
            CheckSubmissionProjectReferences(projectId, collection, ignoreExistingReferences: false);

            var newState = _state.AddProjectReferences(projectId, collection);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to no longer
        /// include the specified project reference.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="projectReference"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The solution does not contain <paramref name="projectId"/>.</exception>
        public Solution RemoveProjectReference(ProjectId projectId, ProjectReference projectReference)
        {
            if (projectReference == null)
            {
                throw new ArgumentNullException(nameof(projectReference));
            }

            CheckContainsProject(projectId);

            var newState = _state.RemoveProjectReference(projectId, projectReference);
            if (newState == _state)
            {
                throw new ArgumentException(WorkspacesResources.Project_does_not_contain_specified_reference, nameof(projectReference));
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to contain
        /// the specified list of project references.
        /// </summary>
        /// <param name="projectId">Id of the project whose references to replace with <paramref name="projectReferences"/>.</param>
        /// <param name="projectReferences">New project references.</param>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="projectReferences"/> contains <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="projectReferences"/> contains duplicate items.</exception>
        /// <exception cref="InvalidOperationException">The solution does not contain <paramref name="projectId"/>.</exception>
        public Solution WithProjectReferences(ProjectId projectId, IEnumerable<ProjectReference>? projectReferences)
        {
            CheckContainsProject(projectId);

            // avoid enumerating multiple times:
            var collection = PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(projectReferences, nameof(projectReferences));

            CheckCircularProjectReferences(projectId, collection);
            CheckSubmissionProjectReferences(projectId, collection, ignoreExistingReferences: true);

            var newState = _state.WithProjectReferences(projectId, collection);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the 
        /// specified metadata reference.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="metadataReference"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The solution does not contain <paramref name="projectId"/>.</exception>
        /// <exception cref="InvalidOperationException">The project already contains the specified reference.</exception>
        public Solution AddMetadataReference(ProjectId projectId, MetadataReference metadataReference)
        {
            return AddMetadataReferences(projectId,
                SpecializedCollections.SingletonEnumerable(
                    metadataReference ?? throw new ArgumentNullException(nameof(metadataReference))));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the
        /// specified metadata references.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="metadataReferences"/> contains <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="metadataReferences"/> contains duplicate items.</exception>
        /// <exception cref="InvalidOperationException">The solution does not contain <paramref name="projectId"/>.</exception>
        /// <exception cref="InvalidOperationException">The project already contains the specified reference.</exception>
        public Solution AddMetadataReferences(ProjectId projectId, IEnumerable<MetadataReference> metadataReferences)
        {
            CheckContainsProject(projectId);

            // avoid enumerating multiple times:
            var collection = metadataReferences?.ToCollection();

            PublicContract.RequireUniqueNonNullItems(collection, nameof(metadataReferences));
            foreach (var metadataReference in collection)
            {
                if (_state.ContainsMetadataReference(projectId, metadataReference))
                {
                    throw new InvalidOperationException(WorkspacesResources.The_project_already_contains_the_specified_reference);
                }
            }

            var newState = _state.AddMetadataReferences(projectId, collection);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to no longer include
        /// the specified metadata reference.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="metadataReference"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The solution does not contain <paramref name="projectId"/>.</exception>
        /// <exception cref="InvalidOperationException">The project does not contain the specified reference.</exception>
        public Solution RemoveMetadataReference(ProjectId projectId, MetadataReference metadataReference)
        {
            CheckContainsProject(projectId);

            if (metadataReference == null)
            {
                throw new ArgumentNullException(nameof(metadataReference));
            }

            var newState = _state.RemoveMetadataReference(projectId, metadataReference);
            if (newState == _state)
            {
                throw new InvalidOperationException(WorkspacesResources.Project_does_not_contain_specified_reference);
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include only the
        /// specified metadata references.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="metadataReferences"/> contains <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="metadataReferences"/> contains duplicate items.</exception>
        /// <exception cref="InvalidOperationException">The solution does not contain <paramref name="projectId"/>.</exception>
        public Solution WithProjectMetadataReferences(ProjectId projectId, IEnumerable<MetadataReference> metadataReferences)
        {
            CheckContainsProject(projectId);

            var newState = _state.WithProjectMetadataReferences(
                projectId,
                PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(metadataReferences, nameof(metadataReferences)));

            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the 
        /// specified analyzer reference.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="analyzerReference"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The solution does not contain <paramref name="projectId"/>.</exception>
        public Solution AddAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            return AddAnalyzerReferences(projectId,
                SpecializedCollections.SingletonEnumerable(
                    analyzerReference ?? throw new ArgumentNullException(nameof(analyzerReference))));
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the
        /// specified analyzer references.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="analyzerReferences"/> contains <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="analyzerReferences"/> contains duplicate items.</exception>
        /// <exception cref="InvalidOperationException">The solution does not contain <paramref name="projectId"/>.</exception>
        /// <exception cref="InvalidOperationException">The project already contains the specified reference.</exception>
        public Solution AddAnalyzerReferences(ProjectId projectId, IEnumerable<AnalyzerReference> analyzerReferences)
        {
            CheckContainsProject(projectId);

            if (analyzerReferences is null)
            {
                throw new ArgumentNullException(nameof(analyzerReferences));
            }

            var collection = analyzerReferences.ToImmutableArray();

            PublicContract.RequireUniqueNonNullItems(collection, nameof(analyzerReferences));

            foreach (var analyzerReference in collection)
            {
                if (_state.ContainsAnalyzerReference(projectId, analyzerReference))
                {
                    throw new InvalidOperationException(WorkspacesResources.The_project_already_contains_the_specified_reference);
                }
            }

            var newState = _state.AddAnalyzerReferences(projectId, collection);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to no longer include
        /// the specified analyzer reference.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="analyzerReference"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The solution does not contain <paramref name="projectId"/>.</exception>
        /// <exception cref="InvalidOperationException">The project does not contain the specified reference.</exception>
        public Solution RemoveAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            CheckContainsProject(projectId);

            if (analyzerReference == null)
            {
                throw new ArgumentNullException(nameof(analyzerReference));
            }

            var newState = _state.RemoveAnalyzerReference(projectId, analyzerReference);
            if (newState == _state)
            {
                throw new InvalidOperationException(WorkspacesResources.Project_does_not_contain_specified_reference);
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include only the
        /// specified analyzer references.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="projectId"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="analyzerReferences"/> contains <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="analyzerReferences"/> contains duplicate items.</exception>
        /// <exception cref="InvalidOperationException">The solution does not contain <paramref name="projectId"/>.</exception>
        public Solution WithProjectAnalyzerReferences(ProjectId projectId, IEnumerable<AnalyzerReference> analyzerReferences)
        {
            CheckContainsProject(projectId);

            var newState = _state.WithProjectAnalyzerReferences(
                projectId,
                PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(analyzerReferences, nameof(analyzerReferences)));

            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance updated to include the specified analyzer reference.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="analyzerReference"/> is <see langword="null"/>.</exception>
        public Solution AddAnalyzerReference(AnalyzerReference analyzerReference)
        {
            return AddAnalyzerReferences(
                SpecializedCollections.SingletonEnumerable(
                    analyzerReference ?? throw new ArgumentNullException(nameof(analyzerReference))));
        }

        /// <summary>
        /// Create a new solution instance updated to include the specified analyzer references.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="analyzerReferences"/> contains <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="analyzerReferences"/> contains duplicate items.</exception>
        /// <exception cref="InvalidOperationException">The solution already contains the specified reference.</exception>
        public Solution AddAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferences)
        {
            // avoid enumerating multiple times:
            var collection = analyzerReferences?.ToCollection();

            PublicContract.RequireUniqueNonNullItems(collection, nameof(analyzerReferences));

            foreach (var analyzerReference in collection)
            {
                if (_state.AnalyzerReferences.Contains(analyzerReference))
                {
                    throw new InvalidOperationException(WorkspacesResources.The_solution_already_contains_the_specified_reference);
                }
            }

            var newState = _state.AddAnalyzerReferences(collection);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to no longer include
        /// the specified analyzer reference.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="analyzerReference"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The solution does not contain the specified reference.</exception>
        public Solution RemoveAnalyzerReference(AnalyzerReference analyzerReference)
        {
            if (analyzerReference == null)
            {
                throw new ArgumentNullException(nameof(analyzerReference));
            }

            var newState = _state.RemoveAnalyzerReference(analyzerReference);
            if (newState == _state)
            {
                throw new InvalidOperationException(WorkspacesResources.Solution_does_not_contain_specified_reference);
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the specified analyzer references.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="analyzerReferences"/> contains <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="analyzerReferences"/> contains duplicate items.</exception>
        public Solution WithAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferences)
        {
            var newState = _state.WithAnalyzerReferences(
                PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(analyzerReferences, nameof(analyzerReferences)));

            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        private static SourceCodeKind GetSourceCodeKind(ProjectState project)
            => project.ParseOptions != null ? project.ParseOptions.Kind : SourceCodeKind.Regular;

        /// <summary>
        /// Creates a new solution instance with the corresponding project updated to include a new
        /// document instance defined by its name and text.
        /// </summary>
        public Solution AddDocument(DocumentId documentId, string name, string text, IEnumerable<string>? folders = null, string? filePath = null)
            => this.AddDocument(documentId, name, SourceText.From(text), folders, filePath);

        /// <summary>
        /// Creates a new solution instance with the corresponding project updated to include a new
        /// document instance defined by its name and text.
        /// </summary>
        public Solution AddDocument(DocumentId documentId, string name, SourceText text, IEnumerable<string>? folders = null, string? filePath = null, bool isGenerated = false)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var project = _state.GetProjectState(documentId.ProjectId);

            if (project == null)
            {
                throw new InvalidOperationException(string.Format(WorkspacesResources._0_is_not_part_of_the_workspace, documentId.ProjectId));
            }

            var version = VersionStamp.Create();
            var loader = TextLoader.From(TextAndVersion.Create(text, version, name));

            var info = DocumentInfo.Create(
                documentId,
                name: name,
                folders: folders,
                sourceCodeKind: GetSourceCodeKind(project),
                loader: loader,
                filePath: filePath,
                isGenerated: isGenerated);

            return this.AddDocument(info);
        }

        /// <summary>
        /// Creates a new solution instance with the corresponding project updated to include a new
        /// document instance defined by its name and root <see cref="SyntaxNode"/>.
        /// </summary>
        public Solution AddDocument(DocumentId documentId, string name, SyntaxNode syntaxRoot, IEnumerable<string>? folders = null, string? filePath = null, bool isGenerated = false, PreservationMode preservationMode = PreservationMode.PreserveValue)
            => this.AddDocument(documentId, name, SourceText.From(string.Empty), folders, filePath, isGenerated).WithDocumentSyntaxRoot(documentId, syntaxRoot, preservationMode);

        /// <summary>
        /// Creates a new solution instance with the project updated to include a new document with
        /// the arguments specified.
        /// </summary>
        public Solution AddDocument(DocumentId documentId, string name, TextLoader loader, IEnumerable<string>? folders = null)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            var project = _state.GetProjectState(documentId.ProjectId);

            if (project == null)
            {
                throw new InvalidOperationException(string.Format(WorkspacesResources._0_is_not_part_of_the_workspace, documentId.ProjectId));
            }

            var info = DocumentInfo.Create(
                documentId,
                name: name,
                folders: folders,
                sourceCodeKind: GetSourceCodeKind(project),
                loader: loader);

            return this.AddDocument(info);
        }

        /// <summary>
        /// Create a new solution instance with the corresponding project updated to include a new 
        /// document instanced defined by the document info.
        /// </summary>
        public Solution AddDocument(DocumentInfo documentInfo)
            => AddDocuments(ImmutableArray.Create(documentInfo));

        /// <summary>
        /// Create a new <see cref="Solution"/> instance with the corresponding <see cref="Project"/>s updated to include
        /// the documents specified by <paramref name="documentInfos"/>.
        /// </summary>
        /// <returns>A new <see cref="Solution"/> with the documents added.</returns>
        public Solution AddDocuments(ImmutableArray<DocumentInfo> documentInfos)
        {
            var newState = _state.AddDocuments(documentInfos);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the corresponding project updated to include a new
        /// additional document instance defined by its name and text.
        /// </summary>
        public Solution AddAdditionalDocument(DocumentId documentId, string name, string text, IEnumerable<string>? folders = null, string? filePath = null)
            => this.AddAdditionalDocument(documentId, name, SourceText.From(text), folders, filePath);

        /// <summary>
        /// Creates a new solution instance with the corresponding project updated to include a new
        /// additional document instance defined by its name and text.
        /// </summary>
        public Solution AddAdditionalDocument(DocumentId documentId, string name, SourceText text, IEnumerable<string>? folders = null, string? filePath = null)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var info = CreateDocumentInfo(documentId, name, text, folders, filePath);
            return this.AddAdditionalDocument(info);
        }

        public Solution AddAdditionalDocument(DocumentInfo documentInfo)
            => AddAdditionalDocuments(ImmutableArray.Create(documentInfo));

        public Solution AddAdditionalDocuments(ImmutableArray<DocumentInfo> documentInfos)
        {
            var newState = _state.AddAdditionalDocuments(documentInfos);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the corresponding project updated to include a new
        /// analyzer config document instance defined by its name and text.
        /// </summary>
        public Solution AddAnalyzerConfigDocument(DocumentId documentId, string name, SourceText text, IEnumerable<string>? folders = null, string? filePath = null)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            // TODO: should we validate file path?
            // https://github.com/dotnet/roslyn/issues/41940

            var info = CreateDocumentInfo(documentId, name, text, folders, filePath);
            return this.AddAnalyzerConfigDocuments(ImmutableArray.Create(info));
        }

        private DocumentInfo CreateDocumentInfo(DocumentId documentId, string name, SourceText text, IEnumerable<string>? folders, string? filePath)
        {
            var project = _state.GetProjectState(documentId.ProjectId);

            if (project is null)
            {
                throw new InvalidOperationException(string.Format(WorkspacesResources._0_is_not_part_of_the_workspace, documentId.ProjectId));
            }

            var version = VersionStamp.Create();
            var loader = TextLoader.From(TextAndVersion.Create(text, version, name));

            return DocumentInfo.Create(
                documentId,
                name: name,
                folders: folders,
                sourceCodeKind: GetSourceCodeKind(project),
                loader: loader,
                filePath: filePath);
        }

        /// <summary>
        /// Creates a new Solution instance that contains a new compiler configuration document like a .editorconfig file.
        /// </summary>
        public Solution AddAnalyzerConfigDocuments(ImmutableArray<DocumentInfo> documentInfos)
        {
            var newState = _state.AddAnalyzerConfigDocuments(documentInfos);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance that no longer includes the specified document.
        /// </summary>
        public Solution RemoveDocument(DocumentId documentId)
        {
            CheckContainsDocument(documentId);
            return RemoveDocumentsImpl(ImmutableArray.Create(documentId));
        }

        /// <summary>
        /// Creates a new solution instance that no longer includes the specified documents.
        /// </summary>
        public Solution RemoveDocuments(ImmutableArray<DocumentId> documentIds)
        {
            CheckContainsDocuments(documentIds);
            return RemoveDocumentsImpl(documentIds);
        }

        private Solution RemoveDocumentsImpl(ImmutableArray<DocumentId> documentIds)
        {
            var newState = _state.RemoveDocuments(documentIds);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance that no longer includes the specified additional document.
        /// </summary>
        public Solution RemoveAdditionalDocument(DocumentId documentId)
        {
            CheckContainsAdditionalDocument(documentId);
            return RemoveAdditionalDocumentsImpl(ImmutableArray.Create(documentId));
        }

        /// <summary>
        /// Creates a new solution instance that no longer includes the specified additional documents.
        /// </summary>
        public Solution RemoveAdditionalDocuments(ImmutableArray<DocumentId> documentIds)
        {
            CheckContainsAdditionalDocuments(documentIds);
            return RemoveAdditionalDocumentsImpl(documentIds);
        }

        private Solution RemoveAdditionalDocumentsImpl(ImmutableArray<DocumentId> documentIds)
        {
            var newState = _state.RemoveAdditionalDocuments(documentIds);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance that no longer includes the specified <see cref="AnalyzerConfigDocument"/>.
        /// </summary>
        public Solution RemoveAnalyzerConfigDocument(DocumentId documentId)
        {
            CheckContainsAnalyzerConfigDocument(documentId);
            return RemoveAnalyzerConfigDocumentsImpl(ImmutableArray.Create(documentId));
        }

        /// <summary>
        /// Creates a new solution instance that no longer includes the specified <see cref="AnalyzerConfigDocument"/>s.
        /// </summary>
        public Solution RemoveAnalyzerConfigDocuments(ImmutableArray<DocumentId> documentIds)
        {
            CheckContainsAnalyzerConfigDocuments(documentIds);
            return RemoveAnalyzerConfigDocumentsImpl(documentIds);
        }

        private Solution RemoveAnalyzerConfigDocumentsImpl(ImmutableArray<DocumentId> documentIds)
        {
            var newState = _state.RemoveAnalyzerConfigDocuments(documentIds);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the new name.
        /// </summary>
        public Solution WithDocumentName(DocumentId documentId, string name)
        {
            CheckContainsDocument(documentId);

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var newState = _state.WithDocumentName(documentId, name);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to be contained in
        /// the sequence of logical folders.
        /// </summary>
        public Solution WithDocumentFolders(DocumentId documentId, IEnumerable<string>? folders)
        {
            CheckContainsDocument(documentId);

            var newState = _state.WithDocumentFolders(documentId,
                PublicContract.ToBoxedImmutableArrayWithNonNullItems(folders, nameof(folders)));

            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the specified file path.
        /// </summary>
        public Solution WithDocumentFilePath(DocumentId documentId, string filePath)
        {
            CheckContainsDocument(documentId);

            // TODO (https://github.com/dotnet/roslyn/issues/37125): 
            // We *do* support null file paths. Why can't you switch a document back to null?
            // See DocumentState.GetSyntaxTreeFilePath
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var newState = _state.WithDocumentFilePath(documentId, filePath);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the text
        /// specified.
        /// </summary>
        public Solution WithDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        {
            CheckContainsDocument(documentId);

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (!mode.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            var newState = _state.WithDocumentText(documentId, text, mode);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the additional document specified updated to have the text
        /// specified.
        /// </summary>
        public Solution WithAdditionalDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        {
            CheckContainsAdditionalDocument(documentId);

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (!mode.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            var newState = _state.WithAdditionalDocumentText(documentId, text, mode);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the analyzer config document specified updated to have the text
        /// supplied by the text loader.
        /// </summary>
        public Solution WithAnalyzerConfigDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        {
            CheckContainsAnalyzerConfigDocument(documentId);

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (!mode.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            var newState = _state.WithAnalyzerConfigDocumentText(documentId, text, mode);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the text
        /// and version specified.
        /// </summary>
        public Solution WithDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
        {
            CheckContainsDocument(documentId);

            if (textAndVersion == null)
            {
                throw new ArgumentNullException(nameof(textAndVersion));
            }

            if (!mode.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            var newState = _state.WithDocumentText(documentId, textAndVersion, mode);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the additional document specified updated to have the text
        /// and version specified.
        /// </summary>
        public Solution WithAdditionalDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
        {
            CheckContainsAdditionalDocument(documentId);

            if (textAndVersion == null)
            {
                throw new ArgumentNullException(nameof(textAndVersion));
            }

            if (!mode.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            var newState = _state.WithAdditionalDocumentText(documentId, textAndVersion, mode);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the analyzer config document specified updated to have the text
        /// and version specified.
        /// </summary>
        public Solution WithAnalyzerConfigDocumentText(DocumentId documentId, TextAndVersion textAndVersion, PreservationMode mode = PreservationMode.PreserveValue)
        {
            CheckContainsAnalyzerConfigDocument(documentId);

            if (textAndVersion == null)
            {
                throw new ArgumentNullException(nameof(textAndVersion));
            }

            if (!mode.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            var newState = _state.WithAnalyzerConfigDocumentText(documentId, textAndVersion, mode);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have a syntax tree
        /// rooted by the specified syntax node.
        /// </summary>
        public Solution WithDocumentSyntaxRoot(DocumentId documentId, SyntaxNode root, PreservationMode mode = PreservationMode.PreserveValue)
        {
            CheckContainsDocument(documentId);

            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (!mode.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            var newState = _state.WithDocumentSyntaxRoot(documentId, root, mode);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the source
        /// code kind specified.
        /// </summary>
        public Solution WithDocumentSourceCodeKind(DocumentId documentId, SourceCodeKind sourceCodeKind)
        {
            CheckContainsDocument(documentId);

#pragma warning disable CS0618 // Interactive is obsolete but this method accepts it for backward compatibility.
            if (sourceCodeKind == SourceCodeKind.Interactive)
            {
                sourceCodeKind = SourceCodeKind.Script;
            }
#pragma warning restore CS0618

            if (!sourceCodeKind.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(sourceCodeKind));
            }

            var newState = _state.WithDocumentSourceCodeKind(documentId, sourceCodeKind);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the text
        /// supplied by the text loader.
        /// </summary>
        public Solution WithDocumentTextLoader(DocumentId documentId, TextLoader loader, PreservationMode mode)
        {
            CheckContainsDocument(documentId);

            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            if (!mode.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            return UpdateDocumentTextLoader(documentId, loader, text: null, mode: mode);
        }

        internal Solution UpdateDocumentTextLoader(DocumentId documentId, TextLoader loader, SourceText? text, PreservationMode mode)
        {
            var newState = _state.UpdateDocumentTextLoader(documentId, loader, text, mode);

            // Note: state is currently not reused.
            // If UpdateDocumentTextLoader is changed to reuse the state replace this assert with Solution instance reusal.
            Debug.Assert(newState != _state);

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the additional document specified updated to have the text
        /// supplied by the text loader.
        /// </summary>
        public Solution WithAdditionalDocumentTextLoader(DocumentId documentId, TextLoader loader, PreservationMode mode)
        {
            CheckContainsAdditionalDocument(documentId);

            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            if (!mode.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            var newState = _state.UpdateAdditionalDocumentTextLoader(documentId, loader, mode);

            // Note: state is currently not reused.
            // If UpdateAdditionalDocumentTextLoader is changed to reuse the state replace this assert with Solution instance reusal.
            Debug.Assert(newState != _state);

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the analyzer config document specified updated to have the text
        /// supplied by the text loader.
        /// </summary>
        public Solution WithAnalyzerConfigDocumentTextLoader(DocumentId documentId, TextLoader loader, PreservationMode mode)
        {
            CheckContainsAnalyzerConfigDocument(documentId);

            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            if (!mode.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            var newState = _state.UpdateAnalyzerConfigDocumentTextLoader(documentId, loader, mode);

            // Note: state is currently not reused.
            // If UpdateAnalyzerConfigDocumentTextLoader is changed to reuse the state replace this assert with Solution instance reusal.
            Debug.Assert(newState != _state);

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a branch of the solution that has its compilations frozen in whatever state they are in at the time, assuming a background compiler is
        /// busy building this compilations.
        /// 
        /// A compilation for the project containing the specified document id will be guaranteed to exist with at least the syntax tree for the document.
        /// 
        /// This not intended to be the public API, use Document.WithFrozenPartialSemantics() instead.
        /// </summary>
        internal Solution WithFrozenPartialCompilationIncludingSpecificDocument(DocumentId documentId, CancellationToken cancellationToken)
        {
            var newState = _state.WithFrozenPartialCompilationIncludingSpecificDocument(documentId, cancellationToken);
            return new Solution(newState);
        }

        internal async Task<Solution> WithMergedLinkedFileChangesAsync(
            Solution oldSolution,
            SolutionChanges? solutionChanges = null,
            IMergeConflictHandler? mergeConflictHandler = null,
            CancellationToken cancellationToken = default)
        {
            // we only log sessioninfo for actual changes committed to workspace which should exclude ones from preview
            var session = new LinkedFileDiffMergingSession(oldSolution, this, solutionChanges ?? this.GetChanges(oldSolution));

            return (await session.MergeDiffsAsync(mergeConflictHandler, cancellationToken).ConfigureAwait(false)).MergedSolution;
        }

        internal ImmutableArray<DocumentId> GetRelatedDocumentIds(DocumentId documentId)
        {
            var projectState = _state.GetProjectState(documentId.ProjectId);
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
            return this.FilterDocumentIdsByLanguage(documentIds, projectState.ProjectInfo.Language);
        }

        internal Solution WithNewWorkspace(Workspace workspace, int workspaceVersion)
        {
            var newState = _state.WithNewWorkspace(workspace, workspaceVersion);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Formerly, returned a copy of the solution isolated from the original so that they do not share computed state. It now does nothing.
        /// </summary>
        [Obsolete("This method no longer produces a Solution that does not share state and is no longer necessary to call.", error: false)]
        [EditorBrowsable(EditorBrowsableState.Never)] // hide this since it is obsolete and only leads to confusion
        public Solution GetIsolatedSolution()
        {
            // To maintain compat, just return ourself, which will be functionally identical.
            return this;
        }

        /// <summary>
        /// Creates a new solution instance with all the documents specified updated to have the same specified text.
        /// </summary>
        public Solution WithDocumentText(IEnumerable<DocumentId?> documentIds, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        {
            if (documentIds == null)
            {
                throw new ArgumentNullException(nameof(documentIds));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (!mode.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(mode));
            }

            var newState = _state.WithDocumentText(documentIds, text, mode);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Returns a new Solution that will always produce a specific output for a generated file. This is used only in the
        /// implementation of <see cref="TextExtensions.GetOpenDocumentInCurrentContextWithChanges"/> where if a user has a source
        /// generated file open, we need to make sure everything lines up.
        /// </summary>
        internal Document WithFrozenSourceGeneratedDocument(SourceGeneratedDocumentIdentity documentIdentity, SourceText text)
        {
            var newState = _state.WithFrozenSourceGeneratedDocument(documentIdentity, text);
            var newSolution = newState != _state ? new Solution(newState) : this;
            var newDocumentState = newState.TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(documentIdentity.DocumentId);
            Contract.ThrowIfNull(newDocumentState, "Because we just froze this document, it should always exist.");
            var newProject = newSolution.GetRequiredProject(newDocumentState.Id.ProjectId);
            return newProject.GetOrCreateSourceGeneratedDocument(newDocumentState);
        }

        /// <summary>
        /// Undoes the operation of <see cref="WithFrozenSourceGeneratedDocument"/>; any frozen source generated document is allowed
        /// to have it's real output again.
        /// </summary>
        internal Solution WithoutFrozenSourceGeneratedDocuments()
        {
            var newState = _state.WithoutFrozenSourceGeneratedDocuments();
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Gets an objects that lists the added, changed and removed projects between
        /// this solution and the specified solution.
        /// </summary>
        public SolutionChanges GetChanges(Solution oldSolution)
        {
            if (oldSolution == null)
            {
                throw new ArgumentNullException(nameof(oldSolution));
            }

            return new SolutionChanges(this, oldSolution);
        }

        /// <summary>
        /// Gets the set of <see cref="DocumentId"/>s in this <see cref="Solution"/> with a
        /// <see cref="TextDocument.FilePath"/> that matches the given file path.
        /// </summary>
        public ImmutableArray<DocumentId> GetDocumentIdsWithFilePath(string? filePath) => _state.GetDocumentIdsWithFilePath(filePath);

        /// <summary>
        /// Gets a <see cref="ProjectDependencyGraph"/> that details the dependencies between projects for this solution.
        /// </summary>
        public ProjectDependencyGraph GetProjectDependencyGraph() => _state.GetProjectDependencyGraph();

        /// <summary>
        /// Returns the options that should be applied to this solution. This is equivalent to <see cref="Workspace.Options" /> when the <see cref="Solution"/> 
        /// instance was created.
        /// </summary>
        public OptionSet Options => _state.Options;

        /// <summary>
        /// Analyzer references associated with the solution.
        /// </summary>
        public IReadOnlyList<AnalyzerReference> AnalyzerReferences => _state.AnalyzerReferences;

        /// <summary>
        /// Creates a new solution instance with the specified <paramref name="options"/>.
        /// </summary>
        public Solution WithOptions(OptionSet options)
        {
            return options switch
            {
                SerializableOptionSet serializableOptions => WithOptions(serializableOptions),
                null => throw new ArgumentNullException(nameof(options)),
                _ => throw new ArgumentException(WorkspacesResources.Options_did_not_come_from_specified_Solution, paramName: nameof(options))
            };
        }

        /// <summary>
        /// Creates a new solution instance with the specified serializable <paramref name="options"/>.
        /// </summary>
        internal Solution WithOptions(SerializableOptionSet options)
        {
            var newState = _state.WithOptions(options: options);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        private void CheckContainsProject(ProjectId projectId)
        {
            if (projectId == null)
            {
                throw new ArgumentNullException(nameof(projectId));
            }

            if (!ContainsProject(projectId))
            {
                throw new InvalidOperationException(WorkspacesResources.The_solution_does_not_contain_the_specified_project);
            }
        }

        private void CheckContainsDocument(DocumentId documentId)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (!ContainsDocument(documentId))
            {
                throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);
            }
        }

        private void CheckContainsDocuments(ImmutableArray<DocumentId> documentIds)
        {
            if (documentIds.IsDefault)
            {
                throw new ArgumentNullException(nameof(documentIds));
            }

            foreach (var documentId in documentIds)
            {
                CheckContainsDocument(documentId);
            }
        }

        private void CheckContainsAdditionalDocument(DocumentId documentId)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (!ContainsAdditionalDocument(documentId))
            {
                throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);
            }
        }

        private void CheckContainsAdditionalDocuments(ImmutableArray<DocumentId> documentIds)
        {
            if (documentIds.IsDefault)
            {
                throw new ArgumentNullException(nameof(documentIds));
            }

            foreach (var documentId in documentIds)
            {
                CheckContainsAdditionalDocument(documentId);
            }
        }

        private void CheckContainsAnalyzerConfigDocument(DocumentId documentId)
        {
            if (documentId == null)
            {
                throw new ArgumentNullException(nameof(documentId));
            }

            if (!ContainsAnalyzerConfigDocument(documentId))
            {
                throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);
            }
        }

        private void CheckContainsAnalyzerConfigDocuments(ImmutableArray<DocumentId> documentIds)
        {
            if (documentIds.IsDefault)
            {
                throw new ArgumentNullException(nameof(documentIds));
            }

            foreach (var documentId in documentIds)
            {
                CheckContainsAnalyzerConfigDocument(documentId);
            }
        }

        /// <summary>
        /// Throws if setting the project references of project <paramref name="projectId"/> to specified <paramref name="projectReferences"/>
        /// would form a cycle in project dependency graph.
        /// </summary>
        private void CheckCircularProjectReferences(ProjectId projectId, IReadOnlyCollection<ProjectReference> projectReferences)
        {
            foreach (var projectReference in projectReferences)
            {
                if (projectId == projectReference.ProjectId)
                {
                    throw new InvalidOperationException(WorkspacesResources.A_project_may_not_reference_itself);
                }

                if (_state.ContainsTransitiveReference(projectReference.ProjectId, projectId))
                {
                    throw new InvalidOperationException(
                        string.Format(WorkspacesResources.Adding_project_reference_from_0_to_1_will_cause_a_circular_reference,
                            projectId,
                            projectReference.ProjectId));
                }
            }
        }

        /// <summary>
        /// Throws if setting the project references of project <paramref name="projectId"/> to specified <paramref name="projectReferences"/>
        /// would form an invalid submission project chain.
        /// 
        /// Submission projects can reference at most one other submission project. Regular projects can't reference any.
        /// </summary>
        private void CheckSubmissionProjectReferences(ProjectId projectId, IEnumerable<ProjectReference> projectReferences, bool ignoreExistingReferences)
        {
            var projectState = _state.GetRequiredProjectState(projectId);

            var isSubmission = projectState.IsSubmission;
            var hasSubmissionReference = !ignoreExistingReferences && projectState.ProjectReferences.Any(p => _state.GetRequiredProjectState(p.ProjectId).IsSubmission);

            foreach (var projectReference in projectReferences)
            {
                // Note: need to handle reference to a project that's not included in the solution:
                var referencedProjectState = _state.GetProjectState(projectReference.ProjectId);
                if (referencedProjectState != null && referencedProjectState.IsSubmission)
                {
                    if (!isSubmission)
                    {
                        throw new InvalidOperationException(WorkspacesResources.Only_submission_project_can_reference_submission_projects);
                    }

                    if (hasSubmissionReference)
                    {
                        throw new InvalidOperationException(WorkspacesResources.This_submission_already_references_another_submission_project);
                    }

                    hasSubmissionReference = true;
                }
            }
        }
    }
}
