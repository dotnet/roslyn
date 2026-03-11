// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents a set of projects and their source code documents. 
/// </summary>
public partial class Solution
{
    // Values for all these are created on demand. Only access when holding the dictionary as a lock.
    // Intentionally a simple dictionary rather than a ConcurrentDictionary or ImmutableDictionary
    // for performance reasons.
    private readonly Dictionary<ProjectId, Project> _projectIdToProjectMap = [];

    /// <summary>
    /// Result of calling <see cref="WithFrozenPartialCompilationsAsync"/>.
    /// </summary>
    private readonly AsyncLazy<Solution> _cachedFrozenSolution;

    /// <summary>
    /// Mapping of DocumentId to the frozen solution we produced for it the last time we were queried.  This
    /// instance should be used as its own lock when reading or writing to it.
    /// </summary>
    private readonly Dictionary<DocumentId, AsyncLazy<Solution>> _documentIdToFrozenSolution = [];

    private Solution(
        SolutionCompilationState compilationState,
        AsyncLazy<Solution>? cachedFrozenSolution = null)
    {
        CompilationState = compilationState;

        _cachedFrozenSolution = cachedFrozenSolution ??
            AsyncLazy.Create(synchronousComputeFunction: static (self, c) =>
                self.ComputeFrozenSolution(c),
                this);
    }

    internal Solution(
        Workspace workspace,
        SolutionInfo.SolutionAttributes solutionAttributes,
        SolutionOptionSet options,
        IReadOnlyList<AnalyzerReference> analyzerReferences,
        ImmutableDictionary<string, StructuredAnalyzerConfigOptions> fallbackAnalyzerOptions)
        : this(new SolutionCompilationState(
            new SolutionState(workspace.Kind, workspace.Services.SolutionServices, solutionAttributes, options, analyzerReferences, fallbackAnalyzerOptions),
            workspace.PartialSemanticsEnabled))
    {
    }

    internal SolutionState SolutionState => CompilationState.SolutionState;

    internal SolutionCompilationState CompilationState { get; }

    internal int SolutionStateContentVersion => this.SolutionState.ContentVersion;

    internal bool PartialSemanticsEnabled => CompilationState.PartialSemanticsEnabled;

    /// <summary>
    /// Per solution services provided by the host environment.  Use this instead of <see
    /// cref="Workspace.Services"/> when possible.
    /// </summary>
    public SolutionServices Services => this.SolutionState.Services;

    internal string? WorkspaceKind => this.SolutionState.WorkspaceKind;

    internal ProjectState? GetProjectState(ProjectId projectId) => this.SolutionState.GetProjectState(projectId);

    /// <summary>
    /// The Workspace this solution is associated with.
    /// </summary>
    public Workspace Workspace
    {
        get
        {
            Contract.ThrowIfTrue(this.WorkspaceKind == CodeAnalysis.WorkspaceKind.RemoteWorkspace, "Access .Workspace off of a RemoteWorkspace Solution is not supported.");
#pragma warning disable CS0618 // Type or member is obsolete (TODO: obsolete the property)
            return this.SolutionState.Services.WorkspaceServices.Workspace;
#pragma warning restore
        }
    }

    /// <summary>
    /// The Id of the solution. Multiple solution instances may share the same Id.
    /// </summary>
    public SolutionId Id => this.SolutionState.Id;

    /// <summary>
    /// The path to the solution file or null if there is no solution file.
    /// </summary>
    public string? FilePath => this.SolutionState.FilePath;

    /// <summary>
    /// The solution version. This equates to the solution file's version.
    /// </summary>
    public VersionStamp Version => this.SolutionState.Version;

    /// <summary>
    /// A list of all the ids for all the projects contained by the solution.
    /// Ordering determined by the order the projects were added to the solution.
    /// </summary>
    public IReadOnlyList<ProjectId> ProjectIds => this.SolutionState.ProjectIds;

    /// <summary>
    /// A list of all the project states contained by the solution.
    /// Ordered by <see cref="ProjectState.Id"/>'s <see cref="ProjectId.Id"/> value.
    /// </summary>
    internal ImmutableArray<ProjectState> SortedProjectStates => this.SolutionState.SortedProjectStates;

    /// <summary>
    /// A list of all the projects contained by the solution.
    /// Ordering determined by the order the projects were added to the solution.
    /// </summary>
    public IEnumerable<Project> Projects => ProjectIds.Select(id => GetProject(id)!);

    /// <summary>
    /// The version of the most recently modified project.
    /// </summary>
    public VersionStamp GetLatestProjectVersion() => this.SolutionState.GetLatestProjectVersion();

    /// <summary>
    /// True if the solution contains a project with the specified project ID.
    /// </summary>
    public bool ContainsProject([NotNullWhen(returnValue: true)] ProjectId? projectId)
        => this.SolutionState.ContainsProject(projectId);

    /// <summary>
    /// Gets the project in this solution with the specified project ID. 
    /// 
    /// If the id is not an id of a project that is part of this solution the method returns null.
    /// </summary>
    public Project? GetProject(ProjectId? projectId)
    {
        if (this.ContainsProject(projectId))
        {
            lock (_projectIdToProjectMap)
            {
                return _projectIdToProjectMap.GetOrAdd(projectId, s_createProjectFunction, this);
            }
        }

        return null;
    }

    private static readonly Func<ProjectId, Solution, Project> s_createProjectFunction = CreateProject;
    private static Project CreateProject(ProjectId projectId, Solution solution)
    {
        var state = solution.SolutionState.GetProjectState(projectId);
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
        var projectId = SolutionCompilationState.GetProjectId(assemblySymbol);
        return GetProject(projectId);
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
        => CompilationState.GetOriginatingProjectInfo(symbol)?.ProjectId;

    /// <inheritdoc cref="GetOriginatingProjectId"/>
    internal Project? GetOriginatingProject(ISymbol symbol)
        => GetProject(GetOriginatingProjectId(symbol));

    /// <inheritdoc cref="GetOriginatingProjectId"/>
    /// <remarks>
    /// Returns the <see cref="Compilation"/> that produced the symbol.  In the case of a symbol that was retargetted
    /// this will be the compilation it was retargtted into, not the original compilation that it was retargetted from.
    /// </remarks>
    internal Compilation? GetOriginatingCompilation(ISymbol symbol)
        => CompilationState.GetOriginatingProjectInfo(symbol)?.Compilation;

    /// <summary>
    /// True if the solution contains the document in one of its projects
    /// </summary>
    public bool ContainsDocument([NotNullWhen(returnValue: true)] DocumentId? documentId) => this.SolutionState.ContainsDocument(documentId);

    /// <summary>
    /// True if the solution contains the additional document in one of its projects
    /// </summary>
    public bool ContainsAdditionalDocument([NotNullWhen(returnValue: true)] DocumentId? documentId) => this.SolutionState.ContainsAdditionalDocument(documentId);

    /// <summary>
    /// True if the solution contains the analyzer config document in one of its projects
    /// </summary>
    public bool ContainsAnalyzerConfigDocument([NotNullWhen(returnValue: true)] DocumentId? documentId) => this.SolutionState.ContainsAnalyzerConfigDocument(documentId);

    /// <summary>
    /// Gets the documentId in this solution with the specified syntax tree.
    /// </summary>
    public DocumentId? GetDocumentId(SyntaxTree? syntaxTree) => GetDocumentId(syntaxTree, projectId: null);

    /// <summary>
    /// Gets the documentId in this solution with the specified syntax tree.
    /// </summary>
    public DocumentId? GetDocumentId(SyntaxTree? syntaxTree, ProjectId? projectId)
        => CompilationState.GetDocumentState(syntaxTree, projectId)?.Id;

    /// <summary>
    /// Gets the document in this solution with the specified document ID.
    /// </summary>
    public Document? GetDocument(DocumentId? documentId)
        => GetProject(documentId?.ProjectId)?.GetDocument(documentId!);

    /// <summary>
    /// Gets a document or a source generated document in this solution with the specified document ID.
    /// </summary>
    internal async ValueTask<Document?> GetDocumentAsync(DocumentId? documentId, bool includeSourceGenerated = false, CancellationToken cancellationToken = default)
    {
        var project = GetProject(documentId?.ProjectId);
        if (project == null)
        {
            return null;
        }

        Contract.ThrowIfNull(documentId);
        return await project.GetDocumentAsync(documentId, includeSourceGenerated, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a document, additional document, analyzer config document or a source generated document in this solution with the specified document ID.
    /// </summary>
    internal async ValueTask<TextDocument?> GetTextDocumentAsync(DocumentId? documentId, CancellationToken cancellationToken = default)
    {
        var project = GetProject(documentId?.ProjectId);
        if (project == null)
        {
            return null;
        }

        Contract.ThrowIfNull(documentId);
        return await project.GetTextDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
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

    public async ValueTask<SourceGeneratedDocument?> GetSourceGeneratedDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
    {
        var project = GetProject(documentId.ProjectId);

        if (project == null)
        {
            return null;
        }
        else
        {
            return await project.GetSourceGeneratedDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
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
            var documentState = CompilationState.GetDocumentState(syntaxTree, projectId);

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
                return GetDocument(documentState.Id);
            }
        }

        return null;
    }

    private Solution WithCompilationState(SolutionCompilationState compilationState)
        => compilationState == CompilationState ? this : new Solution(compilationState);

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

    /// <inheritdoc cref="SolutionCompilationState.AddProjects"/>
    public Solution AddProject(ProjectInfo projectInfo)
    {
        using var _ = ArrayBuilder<ProjectInfo>.GetInstance(1, out var projectInfos);
        projectInfos.Add(projectInfo);
        return AddProjects(projectInfos);
    }

    /// <inheritdoc cref="SolutionCompilationState.AddProjects"/>
    internal Solution AddProjects(ArrayBuilder<ProjectInfo> projectInfos)
        => WithCompilationState(CompilationState.AddProjects(projectInfos));

    /// <inheritdoc cref="SolutionCompilationState.RemoveProjects"/>
    public Solution RemoveProject(ProjectId projectId)
    {
        using var _ = ArrayBuilder<ProjectId>.GetInstance(1, out var projectIds);
        projectIds.Add(projectId);
        return RemoveProjects(projectIds);
    }

    /// <inheritdoc cref="SolutionCompilationState.RemoveProjects"/>
    internal Solution RemoveProjects(ArrayBuilder<ProjectId> projectIds)
        => WithCompilationState(CompilationState.RemoveProjects(projectIds));

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

        return WithCompilationState(CompilationState.WithProjectAssemblyName(projectId, assemblyName));
    }

    /// <summary>
    /// Creates a new solution instance with the project specified updated to have the output file path.
    /// </summary>
    public Solution WithProjectOutputFilePath(ProjectId projectId, string? outputFilePath)
    {
        CheckContainsProject(projectId);

        return WithCompilationState(CompilationState.WithProjectOutputFilePath(projectId, outputFilePath));
    }

    /// <summary>
    /// Creates a new solution instance with the project specified updated to have the reference assembly output file path.
    /// </summary>
    public Solution WithProjectOutputRefFilePath(ProjectId projectId, string? outputRefFilePath)
    {
        CheckContainsProject(projectId);

        return WithCompilationState(CompilationState.WithProjectOutputRefFilePath(projectId, outputRefFilePath));
    }

    /// <summary>
    /// Creates a new solution instance with the project specified updated to have the compiler output file path.
    /// </summary>
    public Solution WithProjectCompilationOutputInfo(ProjectId projectId, in CompilationOutputInfo info)
    {
        CheckContainsProject(projectId);

        return WithCompilationState(CompilationState.WithProjectCompilationOutputInfo(projectId, info));
    }

    /// <summary>
    /// Creates a new solution instance with the project specified updated to have the default namespace.
    /// </summary>
    public Solution WithProjectDefaultNamespace(ProjectId projectId, string? defaultNamespace)
    {
        CheckContainsProject(projectId);

        return WithCompilationState(CompilationState.WithProjectDefaultNamespace(projectId, defaultNamespace));
    }

    /// <summary>
    /// Creates a new solution instance with the project specified updated to have the specified attributes.
    /// </summary>
    internal Solution WithProjectChecksumAlgorithm(ProjectId projectId, SourceHashAlgorithm checksumAlgorithm)
    {
        CheckContainsProject(projectId);

        return WithCompilationState(CompilationState.WithProjectChecksumAlgorithm(projectId, checksumAlgorithm));
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

        return WithCompilationState(CompilationState.WithProjectName(projectId, name));
    }

    /// <summary>
    /// Creates a new solution instance with the project specified updated to have the project file path.
    /// </summary>
    public Solution WithProjectFilePath(ProjectId projectId, string? filePath)
    {
        CheckContainsProject(projectId);

        return WithCompilationState(CompilationState.WithProjectFilePath(projectId, filePath));
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

        return WithCompilationState(CompilationState.WithProjectCompilationOptions(projectId, options));
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

        return WithCompilationState(CompilationState.WithProjectParseOptions(projectId, options));
    }

    /// <summary>
    /// Create a new solution instance updated to use the specified <see cref="FallbackAnalyzerOptions"/>.
    /// </summary>
    internal Solution WithFallbackAnalyzerOptions(ImmutableDictionary<string, StructuredAnalyzerConfigOptions> options)
        => WithCompilationState(CompilationState.WithFallbackAnalyzerOptions(options));

    /// <summary>
    /// Forks this solution to ensure that its <see cref="FallbackAnalyzerOptions"/> are updated with the latest values
    /// from the host, provided via <see cref="IFallbackAnalyzerConfigOptionsProvider"/>, using <paramref
    /// name="oldSolution"/> as the baseline solution that this solution was forked from.  Specifically, this will
    /// ensure that if this solution no longer contains certain project in certain languages that those languages are
    /// removed from <see cref="FallbackAnalyzerOptions"/>.  Similarly, if there are new languages in this solution not
    /// present in the <paramref name="oldSolution"/>, those languages will be added to <see
    /// cref="FallbackAnalyzerOptions"/>.
    /// </summary>
    internal Solution WithFallbackAnalyzerOptionValuesFromHost(Solution oldSolution)
    {
        var newFallbackOptions = this.FallbackAnalyzerOptions;

        // Clear out languages that are no longer present in the solution.
        // If we didn't, the workspace might clear the solution (which removes the fallback options)
        // and we would never re-initialize them from global options.
        foreach (var (language, _) in oldSolution.SolutionState.ProjectCountByLanguage)
        {
            if (!this.SolutionState.ProjectCountByLanguage.ContainsKey(language))
                newFallbackOptions = newFallbackOptions.Remove(language);
        }

        // Update solution snapshot to include options for newly added languages:
        foreach (var (language, _) in this.SolutionState.ProjectCountByLanguage)
        {
            if (oldSolution.SolutionState.ProjectCountByLanguage.ContainsKey(language))
                continue;

            if (newFallbackOptions.ContainsKey(language))
                continue;

            var provider = oldSolution.Services.GetRequiredService<IFallbackAnalyzerConfigOptionsProvider>();
            newFallbackOptions = newFallbackOptions.Add(language, provider.GetOptions(language));
        }

        return this.WithFallbackAnalyzerOptions(newFallbackOptions);
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to have
    /// the specified hasAllInformation.
    /// </summary>
    // TODO: https://github.com/dotnet/roslyn/issues/42449 make it public
    internal Solution WithHasAllInformation(ProjectId projectId, bool hasAllInformation)
    {
        CheckContainsProject(projectId);

        return WithCompilationState(CompilationState.WithHasAllInformation(projectId, hasAllInformation));
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to have
    /// the specified runAnalyzers.
    /// </summary>
    // TODO: https://github.com/dotnet/roslyn/issues/42449 make it public
    internal Solution WithRunAnalyzers(ProjectId projectId, bool runAnalyzers)
    {
        CheckContainsProject(projectId);

        return WithCompilationState(CompilationState.WithRunAnalyzers(projectId, runAnalyzers));
    }

    /// <summary>
    /// Create a new solution instance with the project specified updated to have
    /// the specified hasSdkCodeStyleAnalyzers.
    /// </summary>
    internal Solution WithHasSdkCodeStyleAnalyzers(ProjectId projectId, bool hasSdkCodeStyleAnalyzers)
    {
        CheckContainsProject(projectId);

        return WithCompilationState(CompilationState.WithHasSdkCodeStyleAnalyzers(projectId, hasSdkCodeStyleAnalyzers));
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

        return WithCompilationState(CompilationState.WithProjectDocumentsOrder(projectId, documentIds));
    }

    /// <summary>
    /// Updates the solution with project information stored in <paramref name="attributes"/>.
    /// </summary>
    internal Solution WithProjectAttributes(ProjectInfo.ProjectAttributes attributes)
    {
        CheckContainsProject(attributes.Id);
        return WithCompilationState(CompilationState.WithProjectAttributes(attributes));
    }

    /// <summary>
    /// Updates the solution with project information stored in <paramref name="info"/>.
    /// </summary>
    internal Solution WithProjectInfo(ProjectInfo info)
    {
        CheckContainsProject(info.Id);
        return WithCompilationState(CompilationState.WithProjectInfo(info));
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
            [projectReference ?? throw new ArgumentNullException(nameof(projectReference))]);
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
            if (this.SolutionState.ContainsProjectReference(projectId, projectReference))
            {
                throw new InvalidOperationException(WorkspacesResources.The_project_already_references_the_target_project);
            }
        }

        CheckCircularProjectReferences(projectId, collection);
        CheckSubmissionProjectReferences(projectId, collection, ignoreExistingReferences: false);

        return WithCompilationState(CompilationState.AddProjectReferences(projectId, collection));
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
        try
        {
            if (projectReference == null)
                throw new ArgumentNullException(nameof(projectReference));

            CheckContainsProject(projectId);

            var oldProject = GetRequiredProjectState(projectId);
            if (!oldProject.ProjectReferences.Contains(projectReference))
                throw new ArgumentException(WorkspacesResources.Project_does_not_contain_specified_reference, nameof(projectReference));

            return WithCompilationState(CompilationState.RemoveProjectReference(projectId, projectReference));
        }
        catch (Exception ex) when (FatalError.ReportAndPropagate(ex))
        {
            throw ExceptionUtilities.Unreachable();
        }
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

        return WithCompilationState(CompilationState.WithProjectReferences(projectId, collection));
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
            [metadataReference ?? throw new ArgumentNullException(nameof(metadataReference))]);
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
            if (this.SolutionState.ContainsMetadataReference(projectId, metadataReference))
            {
                throw new InvalidOperationException(WorkspacesResources.The_project_already_contains_the_specified_reference);
            }
        }

        return WithCompilationState(CompilationState.AddMetadataReferences(projectId, collection));
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
            throw new ArgumentNullException(nameof(metadataReference));

        var oldProject = GetRequiredProjectState(projectId);
        if (!oldProject.MetadataReferences.Contains(metadataReference))
            throw new InvalidOperationException(WorkspacesResources.Project_does_not_contain_specified_reference);

        return WithCompilationState(CompilationState.RemoveMetadataReference(projectId, metadataReference));
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

        var collection = PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(metadataReferences, nameof(metadataReferences));

        return WithCompilationState(CompilationState.WithProjectMetadataReferences(projectId, collection));
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
            [analyzerReference ?? throw new ArgumentNullException(nameof(analyzerReference))]);
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
            throw new ArgumentNullException(nameof(analyzerReferences));

        var collection = analyzerReferences.ToImmutableArray();

        PublicContract.RequireUniqueNonNullItems(collection, nameof(analyzerReferences));

        foreach (var analyzerReference in collection)
        {
            if (this.SolutionState.ContainsAnalyzerReference(projectId, analyzerReference))
                throw new InvalidOperationException(WorkspacesResources.The_project_already_contains_the_specified_reference);
        }

        var boxedReferences = Roslyn.Utilities.EnumerableExtensions.ToBoxedImmutableArray([
            // Note: we guaranteed that analyzerReferences has no duplicates, and has no overlap with the existing
            // analyzer references above, so we can just concatenate them here safely.
            .. this.GetRequiredProjectState(projectId).AnalyzerReferences,
            .. collection,
        ]);
        return WithCompilationState(CompilationState.WithProjectAnalyzerReferences(projectId, boxedReferences));
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
            throw new ArgumentNullException(nameof(analyzerReference));

        var oldProject = GetRequiredProjectState(projectId);
        if (!oldProject.AnalyzerReferences.Contains(analyzerReference))
            throw new InvalidOperationException(WorkspacesResources.Project_does_not_contain_specified_reference);

        var builder = new FixedSizeArrayBuilder<AnalyzerReference>(oldProject.AnalyzerReferences.Count - 1);
        foreach (var reference in oldProject.AnalyzerReferences)
        {
            if (!reference.Equals(analyzerReference))
                builder.Add(reference);
        }

        return WithCompilationState(CompilationState.WithProjectAnalyzerReferences(projectId, builder.MoveToImmutable()));
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

        var collection = PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(analyzerReferences, nameof(analyzerReferences));

        return WithCompilationState(CompilationState.WithProjectAnalyzerReferences(projectId, collection));
    }

    /// <summary>
    /// Create a new solution instance updated to include the specified analyzer reference.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="analyzerReference"/> is <see langword="null"/>.</exception>
    public Solution AddAnalyzerReference(AnalyzerReference analyzerReference)
    {
        return AddAnalyzerReferences(
            [analyzerReference ?? throw new ArgumentNullException(nameof(analyzerReference))]);
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
            if (this.SolutionState.AnalyzerReferences.Contains(analyzerReference))
            {
                throw new InvalidOperationException(WorkspacesResources.The_solution_already_contains_the_specified_reference);
            }
        }

        return WithCompilationState(CompilationState.AddAnalyzerReferences(collection));
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
            throw new ArgumentNullException(nameof(analyzerReference));

        if (!this.SolutionState.AnalyzerReferences.Contains(analyzerReference))
            throw new InvalidOperationException(WorkspacesResources.Solution_does_not_contain_specified_reference);

        return WithCompilationState(CompilationState.RemoveAnalyzerReference(analyzerReference));
    }

    /// <summary>
    /// Creates a new solution instance with the specified analyzer references.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="analyzerReferences"/> contains <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="analyzerReferences"/> contains duplicate items.</exception>
    public Solution WithAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferences)
    {
        var collection = PublicContract.ToBoxedImmutableArrayWithDistinctNonNullItems(analyzerReferences, nameof(analyzerReferences));

        return WithCompilationState(CompilationState.WithAnalyzerReferences(collection));
    }

    private static SourceCodeKind GetSourceCodeKind(ProjectState project)
        => project.ParseOptions != null ? project.ParseOptions.Kind : SourceCodeKind.Regular;

    /// <summary>
    /// Creates a new solution instance with the corresponding project updated to include a new
    /// document instance defined by its name and text.
    /// </summary>
    public Solution AddDocument(DocumentId documentId, string name, string text, IEnumerable<string>? folders = null, string? filePath = null)
    {
        if (documentId == null)
            throw new ArgumentNullException(nameof(documentId));

        if (name == null)
            throw new ArgumentNullException(nameof(name));

        var project = GetRequiredProjectState(documentId.ProjectId);
        var sourceText = SourceText.From(text, encoding: null, checksumAlgorithm: project.ChecksumAlgorithm);

        return AddDocumentImpl(project, documentId, name, sourceText, PublicContract.ToBoxedImmutableArrayWithNonNullItems(folders, nameof(folders)), filePath, isGenerated: false);
    }

    /// <summary>
    /// Creates a new solution instance with the corresponding project updated to include a new
    /// document instance defined by its name and text.
    /// </summary>
    public Solution AddDocument(DocumentId documentId, string name, SourceText text, IEnumerable<string>? folders = null, string? filePath = null, bool isGenerated = false)
    {
        if (documentId == null)
            throw new ArgumentNullException(nameof(documentId));

        if (name == null)
            throw new ArgumentNullException(nameof(name));

        if (text == null)
            throw new ArgumentNullException(nameof(text));

        var project = GetRequiredProjectState(documentId.ProjectId);
        return AddDocumentImpl(project, documentId, name, text, PublicContract.ToBoxedImmutableArrayWithNonNullItems(folders, nameof(folders)), filePath, isGenerated);
    }

    /// <summary>
    /// Creates a new solution instance with the corresponding project updated to include a new
    /// document instance defined by its name and root <see cref="SyntaxNode"/>.
    /// </summary>
    public Solution AddDocument(DocumentId documentId, string name, SyntaxNode syntaxRoot, IEnumerable<string>? folders = null, string? filePath = null, bool isGenerated = false, PreservationMode preservationMode = PreservationMode.PreserveValue)
    {
        if (documentId == null)
            throw new ArgumentNullException(nameof(documentId));

        if (name == null)
            throw new ArgumentNullException(nameof(name));

        if (syntaxRoot == null)
            throw new ArgumentNullException(nameof(syntaxRoot));

        var project = GetRequiredProjectState(documentId.ProjectId);

        // The empty text is replaced in WithDocumentSyntaxRoot with the actual text that matches the syntax tree.
        var sourceText = SourceText.From(string.Empty, encoding: null, project.ChecksumAlgorithm);

        return AddDocumentImpl(project, documentId, name, sourceText, PublicContract.ToBoxedImmutableArrayWithNonNullItems(folders, nameof(folders)), filePath, isGenerated).
            WithDocumentSyntaxRoot(documentId, syntaxRoot, preservationMode);
    }

    private Solution AddDocumentImpl(ProjectState project, DocumentId documentId, string name, SourceText text, IReadOnlyList<string>? folders, string? filePath, bool isGenerated)
        => AddDocument(DocumentInfo.Create(
            documentId,
            name: name,
            folders: folders,
            sourceCodeKind: GetSourceCodeKind(project),
            loader: TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create(), name)),
            filePath: filePath,
            isGenerated: isGenerated));

    /// <summary>
    /// Creates a new solution instance with the project updated to include a new document with
    /// the arguments specified.
    /// </summary>
    public Solution AddDocument(DocumentId documentId, string name, TextLoader loader, IEnumerable<string>? folders = null)
    {
        if (documentId == null)
            throw new ArgumentNullException(nameof(documentId));

        if (name == null)
            throw new ArgumentNullException(nameof(name));

        if (loader == null)
            throw new ArgumentNullException(nameof(loader));

        var project = GetRequiredProjectState(documentId.ProjectId);

        return AddDocument(DocumentInfo.Create(
            documentId,
            name,
            folders,
            GetSourceCodeKind(project),
            loader));
    }

    /// <summary>
    /// Create a new solution instance with the corresponding project updated to include a new 
    /// document instanced defined by the document info.
    /// </summary>
    public Solution AddDocument(DocumentInfo documentInfo)
        => AddDocuments([documentInfo]);

    /// <summary>
    /// Create a new <see cref="Solution"/> instance with the corresponding <see cref="Project"/>s updated to include
    /// the documents specified by <paramref name="documentInfos"/>.
    /// </summary>
    /// <returns>A new <see cref="Solution"/> with the documents added.</returns>
    public Solution AddDocuments(ImmutableArray<DocumentInfo> documentInfos)
        => WithCompilationState(CompilationState.AddDocumentsToMultipleProjects<DocumentState>(documentInfos));

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
        => AddAdditionalDocuments([documentInfo]);

    public Solution AddAdditionalDocuments(ImmutableArray<DocumentInfo> documentInfos)
        => WithCompilationState(CompilationState.AddDocumentsToMultipleProjects<AdditionalDocumentState>(documentInfos));

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
        return this.AddAnalyzerConfigDocuments([info]);
    }

    private DocumentInfo CreateDocumentInfo(DocumentId documentId, string name, SourceText text, IEnumerable<string>? folders, string? filePath)
    {
        var project = GetRequiredProjectState(documentId.ProjectId);
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

    internal ProjectState GetRequiredProjectState(ProjectId projectId)
        => this.SolutionState.GetProjectState(projectId) ?? throw new InvalidOperationException(string.Format(WorkspacesResources._0_is_not_part_of_the_workspace, projectId));

    /// <summary>
    /// Creates a new Solution instance that contains a new compiler configuration document like a .editorconfig file.
    /// </summary>
    public Solution AddAnalyzerConfigDocuments(ImmutableArray<DocumentInfo> documentInfos)
        => WithCompilationState(CompilationState.AddDocumentsToMultipleProjects<AnalyzerConfigDocumentState>(documentInfos));

    /// <summary>
    /// Creates a new solution instance that no longer includes the specified document.
    /// </summary>
    public Solution RemoveDocument(DocumentId documentId)
    {
        CheckContainsDocument(documentId);
        return RemoveDocumentsImpl([documentId]);
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
        => WithCompilationState(CompilationState.RemoveDocumentsFromMultipleProjects<DocumentState>(documentIds));

    /// <summary>
    /// Creates a new solution instance that no longer includes the specified additional document.
    /// </summary>
    public Solution RemoveAdditionalDocument(DocumentId documentId)
    {
        CheckContainsAdditionalDocument(documentId);
        return RemoveAdditionalDocumentsImpl([documentId]);
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
        => WithCompilationState(CompilationState.RemoveDocumentsFromMultipleProjects<AdditionalDocumentState>(documentIds));

    /// <summary>
    /// Creates a new solution instance that no longer includes the specified <see cref="AnalyzerConfigDocument"/>.
    /// </summary>
    public Solution RemoveAnalyzerConfigDocument(DocumentId documentId)
    {
        CheckContainsAnalyzerConfigDocument(documentId);
        return RemoveAnalyzerConfigDocumentsImpl([documentId]);
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
        => WithCompilationState(CompilationState.RemoveDocumentsFromMultipleProjects<AnalyzerConfigDocumentState>(documentIds));

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

        return WithCompilationState(CompilationState.WithDocumentAttributes(
            documentId,
            name,
            static (attributes, value) => attributes.With(name: value)));
    }

    /// <summary>
    /// Creates a new solution instance with the document specified updated to be contained in
    /// the sequence of logical folders.
    /// </summary>
    public Solution WithDocumentFolders(DocumentId documentId, IEnumerable<string>? folders)
    {
        CheckContainsDocument(documentId);

        var collection = PublicContract.ToBoxedImmutableArrayWithNonNullItems(folders, nameof(folders));

        return WithCompilationState(CompilationState.WithDocumentAttributes(
            documentId,
            collection,
            static (attributes, value) => attributes.With(folders: value)));
    }

    /// <summary>
    /// Creates a new solution instance with the document specified updated to have the specified file path.
    /// </summary>
    public Solution WithDocumentFilePath(DocumentId documentId, string? filePath)
    {
        CheckContainsDocument(documentId);

        return WithCompilationState(CompilationState.WithDocumentAttributes(
            documentId,
            filePath,
            static (attributes, value) => attributes.With(filePath: value)));
    }

    /// <summary>
    /// Creates a new solution instance with the document specified updated to have the text
    /// specified.
    /// </summary>
    public Solution WithDocumentText(DocumentId documentId, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        => WithDocumentTexts([(documentId, text)], mode);

    internal Solution WithDocumentTexts(ImmutableArray<(DocumentId documentId, SourceText text)> texts, PreservationMode mode = PreservationMode.PreserveValue)
    {
        foreach (var (documentId, text) in texts)
        {
            CheckContainsDocument(documentId);

            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (!mode.IsValid())
                throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return WithCompilationState(CompilationState.WithDocumentTexts(texts, mode));
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

        return WithCompilationState(CompilationState.WithAdditionalDocumentText(documentId, text, mode));
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

        return WithCompilationState(CompilationState.WithAnalyzerConfigDocumentText(documentId, text, mode));
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

        return WithCompilationState(CompilationState.WithDocumentText(documentId, textAndVersion, mode));
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

        return WithCompilationState(CompilationState.WithAdditionalDocumentText(documentId, textAndVersion, mode));
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

        return WithCompilationState(CompilationState.WithAnalyzerConfigDocumentText(documentId, textAndVersion, mode));
    }

    /// <summary>
    /// Creates a new solution instance with the document specified updated to have a syntax tree
    /// rooted by the specified syntax node.
    /// </summary>
    public Solution WithDocumentSyntaxRoot(DocumentId documentId, SyntaxNode root, PreservationMode mode = PreservationMode.PreserveValue)
        => WithDocumentSyntaxRoots([(documentId, root)], mode);

    /// <inheritdoc cref="WithDocumentSyntaxRoot"/>.
    internal Solution WithDocumentSyntaxRoots(ImmutableArray<(DocumentId documentId, SyntaxNode root)> syntaxRoots, PreservationMode mode = PreservationMode.PreserveValue)
    {
        if (!mode.IsValid())
            throw new ArgumentOutOfRangeException(nameof(mode));

        foreach (var (documentId, root) in syntaxRoots)
        {
            CheckContainsDocument(documentId);

            if (root == null)
                throw new ArgumentNullException(nameof(root));
        }

        return WithCompilationState(CompilationState.WithDocumentSyntaxRoots(syntaxRoots, mode));
    }

    internal Solution WithDocumentContentsFrom(DocumentId documentId, DocumentState documentState)
        => WithDocumentContentsFrom([(documentId, documentState)]);

    internal Solution WithDocumentContentsFrom(ImmutableArray<(DocumentId documentId, DocumentState documentState)> documentIdsAndStates)
        // This code path is all about updating linked files to match the contents of the document they are linked to.
        // We always want to try to allow the linked files to reuse the root from the linked document if possible, or
        // reparse if it is not.  Hence why we pass `forceEvenIfTreesWouldDiffer: false` as we don't want reuse in the
        // cases like when PP directives change and the docs contain a #if directive.
        => WithCompilationState(CompilationState.WithDocumentContentsFrom(documentIdsAndStates, forceEvenIfTreesWouldDiffer: false));

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

        return WithCompilationState(CompilationState.WithDocumentSourceCodeKind(documentId, sourceCodeKind));
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

        return WithCompilationState(CompilationState.UpdateDocumentTextLoader(documentId, loader, mode));
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

        return WithCompilationState(CompilationState.UpdateAdditionalDocumentTextLoader(documentId, loader, mode));
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

        return WithCompilationState(CompilationState.UpdateAnalyzerConfigDocumentTextLoader(documentId, loader, mode));
    }

    /// <summary>
    /// Returns a solution instance where every project is frozen at whatever current state it is in
    /// </summary>
    /// <param name="cancellationToken"></param>
    internal Solution WithFrozenPartialCompilations(CancellationToken cancellationToken)
        => _cachedFrozenSolution.GetValue(cancellationToken);

    /// <inheritdoc cref="WithFrozenPartialCompilations"/>
    internal Task<Solution> WithFrozenPartialCompilationsAsync(CancellationToken cancellationToken)
        => _cachedFrozenSolution.GetValueAsync(cancellationToken);

    private Solution ComputeFrozenSolution(CancellationToken cancellationToken)
    {
        // in progress solutions are disabled for some testing
        if (this.Services.GetService<IWorkspacePartialSolutionsTestHook>()?.IsPartialSolutionDisabled == true)
            return this;

        var newCompilationState = this.CompilationState.WithFrozenPartialCompilations(cancellationToken);

        var frozenSolution = new Solution(
            newCompilationState,
            // Set the frozen solution to be its own frozen solution.  Freezing multiple times is a no-op.
            cachedFrozenSolution: _cachedFrozenSolution);

        return frozenSolution;
    }

    /// <summary>
    /// Creates a branch of the solution that has its compilations frozen in whatever state they are in at the time,
    /// assuming a background compiler is busy building this compilations.
    /// <para/> A compilation for the project containing the specified document id will be guaranteed to exist with
    /// at least the syntax tree for the document.
    /// <para/> This not intended to be the public API, use Document.WithFrozenPartialSemantics() instead.
    /// </summary>
    internal Solution WithFrozenPartialCompilationIncludingSpecificDocument(DocumentId documentId, CancellationToken cancellationToken)
    {
        return GetLazySolution().GetValue(cancellationToken);

        AsyncLazy<Solution> GetLazySolution()
        {
            lock (_documentIdToFrozenSolution)
            {
                if (!_documentIdToFrozenSolution.TryGetValue(documentId, out var lazySolution))
                {
                    // in a local function to prevent lambda allocations when not needed.
                    lazySolution = CreateLazyFrozenSolution(this.CompilationState, documentId);
                    _documentIdToFrozenSolution.Add(documentId, lazySolution);
                }

                return lazySolution;
            }
        }

        static AsyncLazy<Solution> CreateLazyFrozenSolution(SolutionCompilationState compilationState, DocumentId documentId)
            => AsyncLazy.Create(synchronousComputeFunction: static (arg, cancellationToken) =>
                ComputeFrozenSolution(arg.compilationState, arg.documentId, cancellationToken),
                arg: (compilationState, documentId));

        static Solution ComputeFrozenSolution(SolutionCompilationState compilationState, DocumentId documentId, CancellationToken cancellationToken)
        {
            var newCompilationState = compilationState.WithFrozenPartialCompilationIncludingSpecificDocument(documentId, cancellationToken);
            var solution = new Solution(newCompilationState);

            // ensure that this document is within the frozen-partial-document for the solution we're creating.  That
            // way, if we ask to freeze it again, we'll just the same document back.
            Contract.ThrowIfTrue(solution._documentIdToFrozenSolution.Count != 0);
            solution._documentIdToFrozenSolution.Add(documentId, AsyncLazy.Create(solution));

            return solution;
        }
    }

    internal async Task<Solution> WithMergedLinkedFileChangesAsync(
        Solution oldSolution,
        SolutionChanges? solutionChanges = null,
        CancellationToken cancellationToken = default)
    {
        // we only log sessioninfo for actual changes committed to workspace which should exclude ones from preview
        var session = new LinkedFileDiffMergingSession(oldSolution, this, solutionChanges ?? this.GetChanges(oldSolution));

        return (await session.MergeDiffsAsync(cancellationToken).ConfigureAwait(false)).MergedSolution;
    }

    internal ImmutableArray<DocumentId> GetRelatedDocumentIds(DocumentId documentId, bool includeDifferentLanguages = false)
        => this.SolutionState.GetRelatedDocumentIds(documentId, includeDifferentLanguages);

    /// <summary>
    /// Returns one of any of the related documents of <paramref name="documentId"/>.  Importantly, this will never
    /// return <paramref name="documentId"/> (unlike <see cref="GetRelatedDocumentIds"/> which includes the original
    /// file in the result).
    /// </summary>
    /// <param name="relatedProjectIdHint">A hint on the first project to search when looking for related
    /// documents.  Must not be the project that <paramref name="documentId"/> is from.</param>
    internal DocumentId? GetFirstRelatedDocumentId(DocumentId documentId, ProjectId? relatedProjectIdHint)
        => this.SolutionState.GetFirstRelatedDocumentId(documentId, relatedProjectIdHint);

    internal Solution WithNewWorkspaceFrom(Solution oldSolution)
        => WithCompilationState(CompilationState.WithNewWorkspaceFrom(oldSolution));

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

        return WithCompilationState(CompilationState.WithDocumentText(documentIds, text, mode));
    }

    /// <summary>
    /// Returns a new Solution that will always produce a specific output for a generated file. This is used only in the
    /// implementation of <see cref="TextExtensions.GetOpenDocumentInCurrentContextWithChanges"/> where if a user has a source
    /// generated file open, we need to make sure everything lines up.
    /// </summary>
    internal Document WithFrozenSourceGeneratedDocument(
        SourceGeneratedDocumentIdentity documentIdentity, DateTime generationDateTime, SourceText text)
    {
        // SyntaxNode is null here because it will be computed on demand. Other APIs, like Document.WithSyntaxRoot, specify it.
        var newCompilationState = CompilationState.WithFrozenSourceGeneratedDocuments([(documentIdentity, generationDateTime, text, syntaxNode: null)]);
        var newSolution = WithCompilationState(newCompilationState);

        var newDocumentState = newCompilationState.TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(documentIdentity.DocumentId);
        Contract.ThrowIfNull(newDocumentState, "Because we just froze this document, it should always exist.");

        var newProject = newSolution.GetRequiredProject(newDocumentState.Id.ProjectId);
        return newProject.GetOrCreateSourceGeneratedDocument(newDocumentState);
    }

    internal Solution WithFrozenSourceGeneratedDocuments(ImmutableArray<(SourceGeneratedDocumentIdentity documentIdentity, DateTime generationDateTime, SourceText text)> documents)
        => WithCompilationState(CompilationState.WithFrozenSourceGeneratedDocuments(documents.SelectAsArray(d => (d.documentIdentity, d.generationDateTime, (SourceText?)d.text, (SyntaxNode?)null))));

    /// <inheritdoc cref="SolutionCompilationState.UpdateSpecificSourceGeneratorExecutionVersions"/>
    internal Solution UpdateSpecificSourceGeneratorExecutionVersions(SourceGeneratorExecutionVersionMap sourceGeneratorExecutionVersionMap)
        => WithCompilationState(CompilationState.UpdateSpecificSourceGeneratorExecutionVersions(sourceGeneratorExecutionVersionMap));

    /// <summary>
    /// Undoes the operation of <see cref="WithFrozenSourceGeneratedDocument"/>; any frozen source generated document is allowed
    /// to have it's real output again.
    /// </summary>
    internal Solution WithoutFrozenSourceGeneratedDocuments()
        => WithCompilationState(CompilationState.WithoutFrozenSourceGeneratedDocuments());

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
    /// <see cref="TextDocument.FilePath"/> that matches the given file path. This may return IDs for any type of document
    /// including <see cref="AdditionalDocument"/>s or <see cref="AnalyzerConfigDocument" />s.
    /// </summary>
    /// <remarks>
    /// It's possible (but unlikely) that the same file may exist as more than one type of document in the same solution. If this
    /// were to return more than one <see cref="DocumentId"/>, you should not assume that just because one is a regular source file means
    /// that all of them would be.
    /// </remarks>
    public ImmutableArray<DocumentId> GetDocumentIdsWithFilePath(string? filePath) => this.SolutionState.GetDocumentIdsWithFilePath(filePath);

    /// <summary>
    /// Gets a <see cref="ProjectDependencyGraph"/> that details the dependencies between projects for this solution.
    /// </summary>
    public ProjectDependencyGraph GetProjectDependencyGraph() => this.SolutionState.GetProjectDependencyGraph();

    /// <summary>
    /// Returns the options that should be applied to this solution. This is equivalent to <see cref="Workspace.Options" /> when the <see cref="Solution"/> 
    /// instance was created.
    /// </summary>
    public OptionSet Options => this.SolutionState.Options;

    /// <summary>
    /// Analyzer references associated with the solution.
    /// </summary>
    public IReadOnlyList<AnalyzerReference> AnalyzerReferences => this.SolutionState.AnalyzerReferences;

    /// <summary>
    /// Fallback analyzer config options by language. The set of languages does not need to match the set of languages of projects included in the current solution snapshot
    /// since these options can be updated independently of the projects contained in the solution.
    /// Generally, the host is responsible for keeping these options up-to-date with whatever option store it maintains
    /// and for making sure fallback options are available in the solution for all languages the host supports.
    /// </summary>
    internal ImmutableDictionary<string, StructuredAnalyzerConfigOptions> FallbackAnalyzerOptions => SolutionState.FallbackAnalyzerOptions;

    /// <summary>
    /// Creates a new solution instance with the specified <paramref name="options"/>.
    /// </summary>
    public Solution WithOptions(OptionSet options)
    {
        return options switch
        {
            SolutionOptionSet serializableOptions => WithOptions(serializableOptions),
            null => throw new ArgumentNullException(nameof(options)),
            _ => throw new ArgumentException(WorkspacesResources.Options_did_not_come_from_specified_Solution, paramName: nameof(options))
        };
    }

    /// <summary>
    /// Creates a new solution instance with the specified serializable <paramref name="options"/>.
    /// </summary>
    internal Solution WithOptions(SolutionOptionSet options)
        => WithCompilationState(CompilationState.WithOptions(options));

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

        // For source generated documents we expect them to be already generated to use any of the APIs that call this
        if (documentId.IsSourceGenerated && ContainsSourceGeneratedDocument(documentId))
        {
            return;
        }

        if (ContainsDocument(documentId))
        {
            return;
        }

        throw ISolutionExtensions.CreateDocumentNotFoundException(documentId);

        bool ContainsSourceGeneratedDocument(DocumentId documentId)
        {
            var project = this.GetProject(documentId.ProjectId);
            if (project is null)
                return false;

            return project.TryGetSourceGeneratedDocumentForAlreadyGeneratedId(documentId) is not null;
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
            throw ISolutionExtensions.CreateDocumentNotFoundException(documentId);
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
            throw ISolutionExtensions.CreateDocumentNotFoundException(documentId);
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

            if (this.SolutionState.ContainsTransitiveReference(projectReference.ProjectId, projectId))
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
        var projectState = this.SolutionState.GetRequiredProjectState(projectId);

        var isSubmission = projectState.IsSubmission;
        var hasSubmissionReference = !ignoreExistingReferences && projectState.ProjectReferences.Any(p => this.SolutionState.GetRequiredProjectState(p.ProjectId).IsSubmission);

        foreach (var projectReference in projectReferences)
        {
            // Note: need to handle reference to a project that's not included in the solution:
            var referencedProjectState = this.SolutionState.GetProjectState(projectReference.ProjectId);
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

    internal SourceGeneratorExecutionVersion GetSourceGeneratorExecutionVersion(ProjectId projectId)
        => this.CompilationState.SourceGeneratorExecutionVersionMap[projectId];
}
