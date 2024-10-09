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
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Represents a project that is part of a <see cref="Solution"/>.
/// </summary>
[DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
public partial class Project
{
    private ImmutableDictionary<DocumentId, Document?> _idToDocumentMap = ImmutableDictionary<DocumentId, Document?>.Empty;
    private ImmutableDictionary<DocumentId, SourceGeneratedDocument> _idToSourceGeneratedDocumentMap = ImmutableDictionary<DocumentId, SourceGeneratedDocument>.Empty;
    private ImmutableDictionary<DocumentId, AdditionalDocument?> _idToAdditionalDocumentMap = ImmutableDictionary<DocumentId, AdditionalDocument?>.Empty;
    private ImmutableDictionary<DocumentId, AnalyzerConfigDocument?> _idToAnalyzerConfigDocumentMap = ImmutableDictionary<DocumentId, AnalyzerConfigDocument?>.Empty;

    internal Project(Solution solution, ProjectState projectState)
    {
        Contract.ThrowIfNull(solution);
        Contract.ThrowIfNull(projectState);

        Solution = solution;
        State = projectState;
    }

    internal ProjectState State { get; }

    /// <summary>
    /// The solution this project is part of.
    /// </summary>
    public Solution Solution { get; }

    /// <summary>
    /// The ID of the project. Multiple <see cref="Project"/> instances may share the same ID. However, only
    /// one project may have this ID in any given solution.
    /// </summary>
    public ProjectId Id => State.Id;

    /// <summary>
    /// The path to the project file or null if there is no project file.
    /// </summary>
    public string? FilePath => State.FilePath;

    /// <summary>
    /// The path to the output file, or null if it is not known.
    /// </summary>
    public string? OutputFilePath => State.OutputFilePath;

    /// <summary>
    /// The path to the reference assembly output file, or null if it is not known.
    /// </summary>
    public string? OutputRefFilePath => State.OutputRefFilePath;

    /// <summary>
    /// Compilation output file paths.
    /// </summary>
    public CompilationOutputInfo CompilationOutputInfo => State.CompilationOutputInfo;

    /// <summary>
    /// The default namespace of the project ("" if not defined, which means global namespace),
    /// or null if it is unknown or not applicable. 
    /// </summary>
    /// <remarks>
    /// Right now VB doesn't have the concept of "default namespace". But we conjure one in workspace 
    /// by assigning the value of the project's root namespace to it. So various feature can choose to 
    /// use it for their own purpose.
    /// In the future, we might consider officially exposing "default namespace" for VB project 
    /// (e.g. through a "defaultnamespace" msbuild property)
    /// </remarks>
    public string? DefaultNamespace => State.DefaultNamespace;

    /// <summary>
    /// <see langword="true"/> if this <see cref="Project"/> supports providing data through the
    /// <see cref="GetCompilationAsync(CancellationToken)"/> method.
    /// 
    /// If <see langword="false"/> then <see cref="GetCompilationAsync(CancellationToken)"/> method will return <see langword="null"/> instead.
    /// </summary>
    public bool SupportsCompilation => this.Services.GetService<ICompilationFactoryService>() != null;

    /// <summary>
    /// The language services from the host environment associated with this project's language.
    /// </summary>
    [Obsolete($"Use {nameof(Services)} instead.")]
#pragma warning disable CS0618 // Member is obsolete -- shouldn't be reported here https://github.com/dotnet/roslyn/issues/66409
    public HostLanguageServices LanguageServices => State.LanguageServices.HostLanguageServices;
#pragma warning restore

    /// <summary>
    /// Immutable snapshot of language services from the host environment associated with this project's language.
    /// Use this over <see cref="LanguageServices"/> when possible.
    /// </summary>
    public LanguageServices Services => State.LanguageServices;

    /// <summary>
    /// The language associated with the project.
    /// </summary>
    public string Language => State.Language;

    /// <summary>
    /// The name of the assembly this project represents.
    /// </summary>
    public string AssemblyName => State.AssemblyName;

    /// <summary>
    /// The name of the project. This may be different than the assembly name.
    /// </summary>
    public string Name => State.Name;

    /// <summary>
    /// The list of all other metadata sources (assemblies) that this project references.
    /// </summary>
    public IReadOnlyList<MetadataReference> MetadataReferences => State.MetadataReferences;

    /// <summary>
    /// The list of all other projects within the same solution that this project references.
    /// </summary>
    public IEnumerable<ProjectReference> ProjectReferences => State.ProjectReferences.Where(pr => this.Solution.ContainsProject(pr.ProjectId));

    /// <summary>
    /// The list of all other projects that this project references, including projects that 
    /// are not part of the solution.
    /// </summary>
    public IReadOnlyList<ProjectReference> AllProjectReferences => State.ProjectReferences;

    /// <summary>
    /// The list of all the diagnostic analyzer references for this project.
    /// </summary>
    public IReadOnlyList<AnalyzerReference> AnalyzerReferences => State.AnalyzerReferences;

    /// <summary>
    /// The options used by analyzers for this project.
    /// </summary>
    public AnalyzerOptions AnalyzerOptions => State.AnalyzerOptions;

    /// <summary>
    /// The options used when building the compilation for this project.
    /// </summary>
    public CompilationOptions? CompilationOptions => State.CompilationOptions;

    /// <summary>
    /// The options used when parsing documents for this project.
    /// </summary>
    public ParseOptions? ParseOptions => State.ParseOptions;

    /// <summary>
    /// Returns true if this is a submission project.
    /// </summary>
    public bool IsSubmission => State.IsSubmission;

    /// <summary>
    /// True if the project has any documents.
    /// </summary>
    public bool HasDocuments => !State.DocumentStates.IsEmpty;

    /// <summary>
    /// All the document IDs associated with this project.
    /// </summary>
    public IReadOnlyList<DocumentId> DocumentIds => State.DocumentStates.Ids;

    /// <summary>
    /// All the additional document IDs associated with this project.
    /// </summary>
    public IReadOnlyList<DocumentId> AdditionalDocumentIds => State.AdditionalDocumentStates.Ids;

    /// <summary>
    /// All the additional document IDs associated with this project.
    /// </summary>
    internal IReadOnlyList<DocumentId> AnalyzerConfigDocumentIds => State.AnalyzerConfigDocumentStates.Ids;

    /// <summary>
    /// All the regular documents associated with this project. Documents produced from source generators are returned by
    /// <see cref="GetSourceGeneratedDocumentsAsync(CancellationToken)"/>.
    /// </summary>
    public IEnumerable<Document> Documents => DocumentIds.Select(GetDocument)!;

    /// <summary>
    /// All the additional documents associated with this project.
    /// </summary>
    public IEnumerable<TextDocument> AdditionalDocuments => AdditionalDocumentIds.Select(GetAdditionalDocument)!;

    /// <summary>
    /// All the <see cref="AnalyzerConfigDocument"/>s associated with this project.
    /// </summary>
    public IEnumerable<AnalyzerConfigDocument> AnalyzerConfigDocuments => AnalyzerConfigDocumentIds.Select(GetAnalyzerConfigDocument)!;

    /// <summary>
    /// True if the project contains a document with the specified ID.
    /// </summary>
    public bool ContainsDocument(DocumentId documentId)
        => State.DocumentStates.Contains(documentId);

    /// <summary>
    /// True if the project contains an additional document with the specified ID.
    /// </summary>
    public bool ContainsAdditionalDocument(DocumentId documentId)
        => State.AdditionalDocumentStates.Contains(documentId);

    /// <summary>
    /// True if the project contains an <see cref="AnalyzerConfigDocument"/> with the specified ID.
    /// </summary>
    public bool ContainsAnalyzerConfigDocument(DocumentId documentId)
        => State.AnalyzerConfigDocumentStates.Contains(documentId);

    /// <summary>
    /// Get the documentId in this project with the specified syntax tree.
    /// </summary>
    public DocumentId? GetDocumentId(SyntaxTree? syntaxTree)
        => Solution.GetDocumentId(syntaxTree, this.Id);

    /// <summary>
    /// Get the document in this project with the specified syntax tree.
    /// </summary>
    public Document? GetDocument(SyntaxTree? syntaxTree)
        => Solution.GetDocument(syntaxTree, this.Id);

    /// <summary>
    /// Get the document in this project with the specified document Id.
    /// </summary>
    public Document? GetDocument(DocumentId documentId)
        => ImmutableInterlocked.GetOrAdd(ref _idToDocumentMap, documentId, s_tryCreateDocumentFunction, this);

    /// <summary>
    /// Get the additional document in this project with the specified document Id.
    /// </summary>
    public TextDocument? GetAdditionalDocument(DocumentId documentId)
        => ImmutableInterlocked.GetOrAdd(ref _idToAdditionalDocumentMap, documentId, s_tryCreateAdditionalDocumentFunction, this);

    /// <summary>
    /// Get the analyzer config document in this project with the specified document Id.
    /// </summary>
    public AnalyzerConfigDocument? GetAnalyzerConfigDocument(DocumentId documentId)
        => ImmutableInterlocked.GetOrAdd(ref _idToAnalyzerConfigDocumentMap, documentId, s_tryCreateAnalyzerConfigDocumentFunction, this);

    /// <summary>
    /// Gets a document or a source generated document in this solution with the specified document ID.
    /// </summary>
    internal async ValueTask<Document?> GetDocumentAsync(DocumentId documentId, bool includeSourceGenerated = false, CancellationToken cancellationToken = default)
    {
        var document = GetDocument(documentId);
        if (document != null || !includeSourceGenerated)
        {
            return document;
        }

        return await GetSourceGeneratedDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a document, additional document, analyzer config document or a source generated document in this solution with the specified document ID.
    /// </summary>
    internal async ValueTask<TextDocument?> GetTextDocumentAsync(DocumentId documentId, CancellationToken cancellationToken = default)
    {
        var document = GetDocument(documentId) ?? GetAdditionalDocument(documentId) ?? GetAnalyzerConfigDocument(documentId);
        if (document != null)
            return document;

        return await GetSourceGeneratedDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all source generated documents in this project.
    /// </summary>
    public async ValueTask<IEnumerable<SourceGeneratedDocument>> GetSourceGeneratedDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var generatedDocumentStates = await Solution.CompilationState.GetSourceGeneratedDocumentStatesAsync(this.State, cancellationToken).ConfigureAwait(false);

        // return an iterator to avoid eagerly allocating all the document instances
        return generatedDocumentStates.States.Values.Select(state =>
            ImmutableInterlocked.GetOrAdd(ref _idToSourceGeneratedDocumentMap, state.Id, s_createSourceGeneratedDocumentFunction, (state, this)));
    }

    internal async IAsyncEnumerable<Document> GetAllRegularAndSourceGeneratedDocumentsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var document in this.Documents)
            yield return document;

        foreach (var document in await GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
            yield return document;
    }

    public async ValueTask<SourceGeneratedDocument?> GetSourceGeneratedDocumentAsync(DocumentId documentId, CancellationToken cancellationToken = default)
    {
        // Immediately shortcircuit out if we know this is not a doc-id corresponding to an SG document.
        if (!documentId.IsSourceGenerated)
            return null;

        // User incorrect called into us with a doc id for a different project.  Ideally we'd throw here, but we've
        // always been resilient to this misuse since the start of roslyn, so we just quick-bail instead.
        if (this.Id != documentId.ProjectId)
            return null;

        // Quick check first: if we already have created a SourceGeneratedDocument wrapper, we're good
        if (_idToSourceGeneratedDocumentMap.TryGetValue(documentId, out var sourceGeneratedDocument))
            return sourceGeneratedDocument;

        // We'll have to run generators if we haven't already and now try to find it.
        var generatedDocumentStates = await Solution.CompilationState.GetSourceGeneratedDocumentStatesAsync(State, cancellationToken).ConfigureAwait(false);
        var generatedDocumentState = generatedDocumentStates.GetState(documentId);
        if (generatedDocumentState is null)
            return null;

        return GetOrCreateSourceGeneratedDocument(generatedDocumentState);
    }

    internal SourceGeneratedDocument GetOrCreateSourceGeneratedDocument(SourceGeneratedDocumentState state)
        => ImmutableInterlocked.GetOrAdd(ref _idToSourceGeneratedDocumentMap, state.Id, s_createSourceGeneratedDocumentFunction, (state, this));

    /// <summary>
    /// Returns the <see cref="SourceGeneratedDocumentState"/> for a source generated document that has already been generated and observed.
    /// </summary>
    /// <remarks>
    /// This is only safe to call if you already have seen the SyntaxTree or equivalent that indicates the document state has already been
    /// generated. This method exists to implement <see cref="Solution.GetDocument(SyntaxTree?)"/> and is best avoided unless you're doing something
    /// similarly tricky like that.
    /// </remarks>
    internal SourceGeneratedDocument? TryGetSourceGeneratedDocumentForAlreadyGeneratedId(DocumentId documentId)
    {
        // Immediately shortcircuit out if we know this is not a doc-id corresponding to an SG document.
        if (!documentId.IsSourceGenerated)
            return null;

        // User incorrect called into us with a doc id for a different project.  Ideally we'd throw here, but we've
        // always been resilient to this misuse since the start of roslyn, so we just quick-bail instead.
        if (this.Id != documentId.ProjectId)
            return null;

        // Easy case: do we already have the SourceGeneratedDocument created?
        if (_idToSourceGeneratedDocumentMap.TryGetValue(documentId, out var document))
            return document;

        // Trickier case now: it's possible we generated this, but we don't actually have the SourceGeneratedDocument for it, so let's go
        // try to fetch the state.
        var documentState = Solution.CompilationState.TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(documentId);
        if (documentState == null)
            return null;

        return ImmutableInterlocked.GetOrAdd(ref _idToSourceGeneratedDocumentMap, documentId, s_createSourceGeneratedDocumentFunction, (documentState, this));
    }

    internal ValueTask<ImmutableArray<Diagnostic>> GetSourceGeneratorDiagnosticsAsync(CancellationToken cancellationToken)
    {
        return Solution.CompilationState.GetSourceGeneratorDiagnosticsAsync(this.State, cancellationToken);
    }

    internal ValueTask<GeneratorDriverRunResult?> GetSourceGeneratorRunResultAsync(CancellationToken cancellationToken)
    {
        return Solution.CompilationState.GetSourceGeneratorRunResultAsync(this.State, cancellationToken);
    }

    internal Task<bool> ContainsSymbolsWithNameAsync(
        string name, CancellationToken cancellationToken)
    {
        return ContainsSymbolsAsync(
            (index, cancellationToken) => index.ProbablyContainsIdentifier(name) || index.ProbablyContainsEscapedIdentifier(name),
            cancellationToken);
    }

    internal Task<bool> ContainsSymbolsWithNameAsync(
        string name, SymbolFilter filter, CancellationToken cancellationToken)
    {
        return ContainsSymbolsWithNameAsync(
            typeName => name == typeName,
            filter,
            cancellationToken);
    }

    internal Task<bool> ContainsSymbolsWithNameAsync(
        Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken)
    {
        return ContainsDeclarationAsync(
            (index, cancellationToken) =>
            {
                foreach (var info in index.DeclaredSymbolInfos)
                {
                    if (FilterMatches(info, filter) && predicate(info.Name))
                        return true;
                }

                return false;
            },
            cancellationToken);

        static bool FilterMatches(DeclaredSymbolInfo info, SymbolFilter filter)
        {
            switch (info.Kind)
            {
                case DeclaredSymbolInfoKind.Namespace:
                    return (filter & SymbolFilter.Namespace) != 0;
                case DeclaredSymbolInfoKind.Class:
                case DeclaredSymbolInfoKind.Delegate:
                case DeclaredSymbolInfoKind.Enum:
                case DeclaredSymbolInfoKind.Interface:
                case DeclaredSymbolInfoKind.Module:
                case DeclaredSymbolInfoKind.Record:
                case DeclaredSymbolInfoKind.RecordStruct:
                case DeclaredSymbolInfoKind.Struct:
                    return (filter & SymbolFilter.Type) != 0;
                case DeclaredSymbolInfoKind.Constant:
                case DeclaredSymbolInfoKind.Constructor:
                case DeclaredSymbolInfoKind.EnumMember:
                case DeclaredSymbolInfoKind.Event:
                case DeclaredSymbolInfoKind.ExtensionMethod:
                case DeclaredSymbolInfoKind.Field:
                case DeclaredSymbolInfoKind.Indexer:
                case DeclaredSymbolInfoKind.Method:
                case DeclaredSymbolInfoKind.Property:
                    return (filter & SymbolFilter.Member) != 0;
                default:
                    throw ExceptionUtilities.UnexpectedValue(info.Kind);
            }
        }
    }

    private Task<bool> ContainsSymbolsAsync(
        Func<SyntaxTreeIndex, CancellationToken, bool> predicate, CancellationToken cancellationToken)
    {
        return ContainsAsync(async d =>
        {
            var index = await SyntaxTreeIndex.GetRequiredIndexAsync(d, cancellationToken).ConfigureAwait(false);
            return predicate(index, cancellationToken);
        });
    }

    private Task<bool> ContainsDeclarationAsync(
        Func<TopLevelSyntaxTreeIndex, CancellationToken, bool> predicate, CancellationToken cancellationToken)
    {
        return ContainsAsync(async d =>
        {
            var index = await TopLevelSyntaxTreeIndex.GetRequiredIndexAsync(d, cancellationToken).ConfigureAwait(false);
            return predicate(index, cancellationToken);
        });
    }

    private async Task<bool> ContainsAsync(Func<Document, Task<bool>> predicateAsync)
    {
        if (!this.SupportsCompilation)
            return false;

        var results = await Task.WhenAll(this.Documents.Select(predicateAsync)).ConfigureAwait(false);
        return results.Any(b => b);
    }

    private static readonly Func<DocumentId, Project, Document?> s_tryCreateDocumentFunction =
        (documentId, project) => project.State.DocumentStates.TryGetState(documentId, out var state) ? new Document(project, state) : null;

    private static readonly Func<DocumentId, Project, AdditionalDocument?> s_tryCreateAdditionalDocumentFunction =
        (documentId, project) => project.State.AdditionalDocumentStates.TryGetState(documentId, out var state) ? new AdditionalDocument(project, state) : null;

    private static readonly Func<DocumentId, Project, AnalyzerConfigDocument?> s_tryCreateAnalyzerConfigDocumentFunction =
        (documentId, project) => project.State.AnalyzerConfigDocumentStates.TryGetState(documentId, out var state) ? new AnalyzerConfigDocument(project, state) : null;

    private static readonly Func<DocumentId, (SourceGeneratedDocumentState state, Project project), SourceGeneratedDocument> s_createSourceGeneratedDocumentFunction =
        (documentId, stateAndProject) => new SourceGeneratedDocument(stateAndProject.project, stateAndProject.state);

    /// <summary>
    /// Tries to get the cached <see cref="Compilation"/> for this project if it has already been created and is still cached. In almost all
    /// cases you should call <see cref="GetCompilationAsync"/> which will either return the cached <see cref="Compilation"/>
    /// or create a new one otherwise.
    /// </summary>
    public bool TryGetCompilation([NotNullWhen(returnValue: true)] out Compilation? compilation)
        => Solution.CompilationState.TryGetCompilation(this.Id, out compilation);

    /// <summary>
    /// Get the <see cref="Compilation"/> for this project asynchronously.
    /// </summary>
    /// <returns>
    /// Returns the produced <see cref="Compilation"/>, or <see langword="null"/> if <see
    /// cref="SupportsCompilation"/> returns <see langword="false"/>. This function will
    /// return the same value if called multiple times.
    /// </returns>
    public Task<Compilation?> GetCompilationAsync(CancellationToken cancellationToken = default)
        => Solution.CompilationState.GetCompilationAsync(State, cancellationToken);

    /// <summary>
    /// Determines if the compilation returned by <see cref="GetCompilationAsync"/> and all its referenced compilation are from fully loaded projects.
    /// </summary>
    // TODO: make this public
    internal Task<bool> HasSuccessfullyLoadedAsync(CancellationToken cancellationToken)
        => Solution.CompilationState.HasSuccessfullyLoadedAsync(State, cancellationToken);

    /// <summary>
    /// Gets an object that lists the added, changed and removed documents between this project and the specified project.
    /// </summary>
    public ProjectChanges GetChanges(Project oldProject)
    {
        if (oldProject == null)
        {
            throw new ArgumentNullException(nameof(oldProject));
        }

        return new ProjectChanges(this, oldProject);
    }

    /// <summary>
    /// The project version. This equates to the version of the project file.
    /// </summary>
    public VersionStamp Version => State.Version;

    /// <summary>
    /// The version of the most recently modified document.
    /// </summary>
    public Task<VersionStamp> GetLatestDocumentVersionAsync(CancellationToken cancellationToken = default)
        => State.GetLatestDocumentVersionAsync(cancellationToken);

    /// <summary>
    /// The most recent version of the project, its documents and all dependent projects and documents.
    /// </summary>
    public Task<VersionStamp> GetDependentVersionAsync(CancellationToken cancellationToken = default)
        => Solution.CompilationState.GetDependentVersionAsync(this.Id, cancellationToken);

    /// <summary>
    /// The semantic version of this project including the semantics of referenced projects.
    /// This version changes whenever the consumable declarations of this project and/or projects it depends on change.
    /// </summary>
    public Task<VersionStamp> GetDependentSemanticVersionAsync(CancellationToken cancellationToken = default)
        => Solution.CompilationState.GetDependentSemanticVersionAsync(this.Id, cancellationToken);

    /// <summary>
    /// The semantic version of this project not including the semantics of referenced projects.
    /// This version changes only when the consumable declarations of this project change.
    /// </summary>
    public Task<VersionStamp> GetSemanticVersionAsync(CancellationToken cancellationToken = default)
        => State.GetSemanticVersionAsync(cancellationToken);

    /// <summary>
    /// Calculates a checksum that contains a project's checksum along with a checksum for each of the project's 
    /// transitive dependencies.
    /// </summary>
    /// <remarks>
    /// This checksum calculation can be used for cases where a feature needs to know if the semantics in this project
    /// changed.  For example, for diagnostics or caching computed semantic data. The goal is to ensure that changes to
    /// <list type="bullet">
    ///    <item>Files inside the current project</item>
    ///    <item>Project properties of the current project</item>
    ///    <item>Visible files in referenced projects</item>
    ///    <item>Project properties in referenced projects</item>
    /// </list>
    /// are reflected in the metadata we keep so that comparing solutions accurately tells us when we need to recompute
    /// semantic work.   
    /// 
    /// <para>This method of checking for changes has a few important properties that differentiate it from other methods of determining project version.
    /// <list type="bullet">
    ///    <item>Changes to methods inside the current project will be reflected to compute updated diagnostics.
    ///        <see cref="Project.GetDependentSemanticVersionAsync(CancellationToken)"/> does not change as it only returns top level changes.</item>
    ///    <item>Reloading a project without making any changes will re-use cached diagnostics.
    ///        <see cref="Project.GetDependentSemanticVersionAsync(CancellationToken)"/> changes as the project is removed, then added resulting in a version change.</item>
    /// </list>   
    /// </para>
    /// </remarks>
    internal Task<Checksum> GetDependentChecksumAsync(CancellationToken cancellationToken)
        => Solution.CompilationState.GetDependentChecksumAsync(this.Id, cancellationToken);

    /// <summary>
    /// Creates a new instance of this project updated to have the new assembly name.
    /// </summary>
    public Project WithAssemblyName(string assemblyName)
        => this.Solution.WithProjectAssemblyName(this.Id, assemblyName).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to have the new default namespace.
    /// </summary>
    public Project WithDefaultNamespace(string defaultNamespace)
        => this.Solution.WithProjectDefaultNamespace(this.Id, defaultNamespace).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to have the specified compilation options.
    /// </summary>
    public Project WithCompilationOptions(CompilationOptions options)
        => this.Solution.WithProjectCompilationOptions(this.Id, options).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to have the specified parse options.
    /// </summary>
    public Project WithParseOptions(ParseOptions options)
        => this.Solution.WithProjectParseOptions(this.Id, options).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to include the specified project reference
    /// in addition to already existing ones.
    /// </summary>
    public Project AddProjectReference(ProjectReference projectReference)
        => this.Solution.AddProjectReference(this.Id, projectReference).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to include the specified project references
    /// in addition to already existing ones.
    /// </summary>
    public Project AddProjectReferences(IEnumerable<ProjectReference> projectReferences)
        => this.Solution.AddProjectReferences(this.Id, projectReferences).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to no longer include the specified project reference.
    /// </summary>
    public Project RemoveProjectReference(ProjectReference projectReference)
        => this.Solution.RemoveProjectReference(this.Id, projectReference).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to replace existing project references 
    /// with the specified ones.
    /// </summary>
    public Project WithProjectReferences(IEnumerable<ProjectReference> projectReferences)
        => this.Solution.WithProjectReferences(this.Id, projectReferences).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to include the specified metadata reference
    /// in addition to already existing ones.
    /// </summary>
    public Project AddMetadataReference(MetadataReference metadataReference)
        => this.Solution.AddMetadataReference(this.Id, metadataReference).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to include the specified metadata references
    /// in addition to already existing ones.
    /// </summary>
    public Project AddMetadataReferences(IEnumerable<MetadataReference> metadataReferences)
        => this.Solution.AddMetadataReferences(this.Id, metadataReferences).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to no longer include the specified metadata reference.
    /// </summary>
    public Project RemoveMetadataReference(MetadataReference metadataReference)
        => this.Solution.RemoveMetadataReference(this.Id, metadataReference).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to replace existing metadata reference
    /// with the specified ones.
    /// </summary>
    public Project WithMetadataReferences(IEnumerable<MetadataReference> metadataReferences)
        => this.Solution.WithProjectMetadataReferences(this.Id, metadataReferences).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to include the specified analyzer reference 
    /// in addition to already existing ones.
    /// </summary>
    public Project AddAnalyzerReference(AnalyzerReference analyzerReference)
        => this.Solution.AddAnalyzerReference(this.Id, analyzerReference).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to include the specified analyzer references
    /// in addition to already existing ones.
    /// </summary>
    public Project AddAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferences)
        => this.Solution.AddAnalyzerReferences(this.Id, analyzerReferences).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to no longer include the specified analyzer reference.
    /// </summary>
    public Project RemoveAnalyzerReference(AnalyzerReference analyzerReference)
        => this.Solution.RemoveAnalyzerReference(this.Id, analyzerReference).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to replace existing analyzer references 
    /// with the specified ones.
    /// </summary>
    public Project WithAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferencs)
        => this.Solution.WithProjectAnalyzerReferences(this.Id, analyzerReferencs).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to replace existing analyzer references 
    /// with the specified ones.
    /// </summary>
    internal Project WithAttributes(ProjectInfo.ProjectAttributes attributes)
        => Solution.WithProjectAttributes(attributes).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new document in a new instance of this project.
    /// </summary>
    public Document AddDocument(string name, SyntaxNode syntaxRoot, IEnumerable<string>? folders = null, string? filePath = null)
    {
        var id = DocumentId.CreateNewId(this.Id);

        // use preserve identity for forked solution directly from syntax node.
        // this lets us not serialize temporary tree unnecessarily
        return this.Solution.AddDocument(id, name, syntaxRoot, folders, filePath, preservationMode: PreservationMode.PreserveIdentity).GetDocument(id)!;
    }

    /// <summary>
    /// Creates a new document in a new instance of this project.
    /// </summary>
    public Document AddDocument(string name, SourceText text, IEnumerable<string>? folders = null, string? filePath = null)
    {
        var id = DocumentId.CreateNewId(this.Id);
        return this.Solution.AddDocument(id, name, text, folders, filePath).GetDocument(id)!;
    }

    /// <summary>
    /// Creates a new document in a new instance of this project.
    /// </summary>
    public Document AddDocument(string name, string text, IEnumerable<string>? folders = null, string? filePath = null)
    {
        var id = DocumentId.CreateNewId(this.Id, debugName: name);
        return this.Solution.AddDocument(id, name, text, folders, filePath).GetDocument(id)!;
    }

    /// <summary>
    /// Creates a new additional document in a new instance of this project.
    /// </summary>
    public TextDocument AddAdditionalDocument(string name, SourceText text, IEnumerable<string>? folders = null, string? filePath = null)
    {
        var id = DocumentId.CreateNewId(this.Id);
        return this.Solution.AddAdditionalDocument(id, name, text, folders, filePath).GetAdditionalDocument(id)!;
    }

    /// <summary>
    /// Creates a new additional document in a new instance of this project.
    /// </summary>
    public TextDocument AddAdditionalDocument(string name, string text, IEnumerable<string>? folders = null, string? filePath = null)
    {
        var id = DocumentId.CreateNewId(this.Id);
        return this.Solution.AddAdditionalDocument(id, name, text, folders, filePath).GetAdditionalDocument(id)!;
    }

    /// <summary>
    /// Creates a new analyzer config document in a new instance of this project.
    /// </summary>
    public TextDocument AddAnalyzerConfigDocument(string name, SourceText text, IEnumerable<string>? folders = null, string? filePath = null)
    {
        var id = DocumentId.CreateNewId(this.Id);
        return this.Solution.AddAnalyzerConfigDocument(id, name, text, folders, filePath).GetAnalyzerConfigDocument(id)!;
    }

    /// <summary>
    /// Creates a new instance of this project updated to no longer include the specified document.
    /// </summary>
    public Project RemoveDocument(DocumentId documentId)
    {
        // NOTE: the method isn't checking if documentId belongs to the project. This probably should be done, but may be a compat change.
        // https://github.com/dotnet/roslyn/issues/41211 tracks this investigation.
        return this.Solution.RemoveDocument(documentId).GetRequiredProject(Id);
    }

    /// <summary>
    /// Creates a new instance of this project updated to no longer include the specified documents.
    /// </summary>
    public Project RemoveDocuments(ImmutableArray<DocumentId> documentIds)
    {
        CheckIdsContainedInProject(documentIds);

        return this.Solution.RemoveDocuments(documentIds).GetRequiredProject(this.Id);
    }

    /// <summary>
    /// Creates a new instance of this project updated to no longer include the specified additional document.
    /// </summary>
    public Project RemoveAdditionalDocument(DocumentId documentId)
        // NOTE: the method isn't checking if documentId belongs to the project. This probably should be done, but may be a compat change.
        // https://github.com/dotnet/roslyn/issues/41211 tracks this investigation.
        => this.Solution.RemoveAdditionalDocument(documentId).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new instance of this project updated to no longer include the specified additional documents.
    /// </summary>
    public Project RemoveAdditionalDocuments(ImmutableArray<DocumentId> documentIds)
    {
        CheckIdsContainedInProject(documentIds);

        return this.Solution.RemoveAdditionalDocuments(documentIds).GetRequiredProject(this.Id);
    }

    /// <summary>
    /// Creates a new instance of this project updated to no longer include the specified analyzer config document.
    /// </summary>
    public Project RemoveAnalyzerConfigDocument(DocumentId documentId)
        // NOTE: the method isn't checking if documentId belongs to the project. This probably should be done, but may be a compat change.
        // https://github.com/dotnet/roslyn/issues/41211 tracks this investigation.
        => this.Solution.RemoveAnalyzerConfigDocument(documentId).GetRequiredProject(Id);

    /// <summary>
    /// Creates a new solution instance that no longer includes the specified <see cref="AnalyzerConfigDocument"/>s.
    /// </summary>
    public Project RemoveAnalyzerConfigDocuments(ImmutableArray<DocumentId> documentIds)
    {
        CheckIdsContainedInProject(documentIds);

        return this.Solution.RemoveAnalyzerConfigDocuments(documentIds).GetRequiredProject(this.Id);
    }

    private void CheckIdsContainedInProject(ImmutableArray<DocumentId> documentIds)
    {
        foreach (var documentId in documentIds)
        {
            // Dealing with nulls is handled by the caller of this
            if (documentId?.ProjectId != this.Id)
            {
                throw new ArgumentException(string.Format(WorkspacesResources._0_is_in_a_different_project, documentId));
            }
        }
    }

    internal AnalyzerConfigData? GetAnalyzerConfigOptions()
        => State.GetAnalyzerConfigOptions();

    /// <summary>
    /// Retrieves fallback analyzer options for this project's language.
    /// </summary>
    internal StructuredAnalyzerConfigOptions GetFallbackAnalyzerOptions()
        => Solution.FallbackAnalyzerOptions.GetValueOrDefault(Language, StructuredAnalyzerConfigOptions.Empty);

    private string GetDebuggerDisplay()
        => this.Name;

    internal SkippedHostAnalyzersInfo GetSkippedAnalyzersInfo(DiagnosticAnalyzerInfoCache infoCache)
        => Solution.SolutionState.Analyzers.GetSkippedAnalyzersInfo(this, infoCache);

    internal async ValueTask<Document?> GetDocumentAsync(ImmutableArray<byte> contentHash, CancellationToken cancellationToken)
    {
        var documentId = await State.GetDocumentIdAsync(contentHash, cancellationToken).ConfigureAwait(false);
        return documentId is null ? null : GetDocument(documentId);
    }

    // <Metalama> This code is used by Try.Metalama.
    public Task<ImmutableArray<Diagnostic>> GetTransformerDiagnosticsAsync(CancellationToken cancellationToken)
        => Solution.CompilationState.GetTransformerDiagnosticsAsync(State, cancellationToken);
    // </Metalama>
}
