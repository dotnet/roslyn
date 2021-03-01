﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a project that is part of a <see cref="Solution"/>.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public partial class Project
    {
        private readonly Solution _solution;
        private readonly ProjectState _projectState;
        private ImmutableHashMap<DocumentId, Document> _idToDocumentMap = ImmutableHashMap<DocumentId, Document>.Empty;
        private ImmutableHashMap<DocumentId, SourceGeneratedDocument> _idToSourceGeneratedDocumentMap = ImmutableHashMap<DocumentId, SourceGeneratedDocument>.Empty;
        private ImmutableHashMap<DocumentId, AdditionalDocument> _idToAdditionalDocumentMap = ImmutableHashMap<DocumentId, AdditionalDocument>.Empty;
        private ImmutableHashMap<DocumentId, AnalyzerConfigDocument> _idToAnalyzerConfigDocumentMap = ImmutableHashMap<DocumentId, AnalyzerConfigDocument>.Empty;

        internal Project(Solution solution, ProjectState projectState)
        {
            Contract.ThrowIfNull(solution);
            Contract.ThrowIfNull(projectState);

            _solution = solution;
            _projectState = projectState;
        }

        internal ProjectState State => _projectState;

        /// <summary>
        /// The solution this project is part of.
        /// </summary>
        public Solution Solution => _solution;

        /// <summary>
        /// The ID of the project. Multiple <see cref="Project"/> instances may share the same ID. However, only
        /// one project may have this ID in any given solution.
        /// </summary>
        public ProjectId Id => _projectState.Id;

        /// <summary>
        /// The path to the project file or null if there is no project file.
        /// </summary>
        public string? FilePath => _projectState.FilePath;

        /// <summary>
        /// The path to the output file, or null if it is not known.
        /// </summary>
        public string? OutputFilePath => _projectState.OutputFilePath;

        /// <summary>
        /// The path to the reference assembly output file, or null if it is not known.
        /// </summary>
        public string? OutputRefFilePath => _projectState.OutputRefFilePath;

        /// <summary>
        /// Compilation output file paths.
        /// </summary>
        public CompilationOutputInfo CompilationOutputInfo => _projectState.CompilationOutputInfo;

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
        public string? DefaultNamespace => _projectState.DefaultNamespace;

        /// <summary>
        /// <see langword="true"/> if this <see cref="Project"/> supports providing data through the
        /// <see cref="GetCompilationAsync(CancellationToken)"/> method.
        /// 
        /// If <see langword="false"/> then <see cref="GetCompilationAsync(CancellationToken)"/> method will return <see langword="null"/> instead.
        /// </summary>
        public bool SupportsCompilation => this.LanguageServices.GetService<ICompilationFactoryService>() != null;

        /// <summary>
        /// The language services from the host environment associated with this project's language.
        /// </summary>
        public HostLanguageServices LanguageServices => _projectState.LanguageServices;

        /// <summary>
        /// The language associated with the project.
        /// </summary>
        public string Language => _projectState.Language;

        /// <summary>
        /// The name of the assembly this project represents.
        /// </summary>
        public string AssemblyName => _projectState.AssemblyName;

        /// <summary>
        /// The name of the project. This may be different than the assembly name.
        /// </summary>
        public string Name => _projectState.Name;

        /// <summary>
        /// The list of all other metadata sources (assemblies) that this project references.
        /// </summary>
        public IReadOnlyList<MetadataReference> MetadataReferences => _projectState.MetadataReferences;

        /// <summary>
        /// The list of all other projects within the same solution that this project references.
        /// </summary>
        public IEnumerable<ProjectReference> ProjectReferences => _projectState.ProjectReferences.Where(pr => this.Solution.ContainsProject(pr.ProjectId));

        /// <summary>
        /// The list of all other projects that this project references, including projects that 
        /// are not part of the solution.
        /// </summary>
        public IReadOnlyList<ProjectReference> AllProjectReferences => _projectState.ProjectReferences;

        /// <summary>
        /// The list of all the diagnostic analyzer references for this project.
        /// </summary>
        public IReadOnlyList<AnalyzerReference> AnalyzerReferences => _projectState.AnalyzerReferences;

        /// <summary>
        /// The options used by analyzers for this project.
        /// </summary>
        public AnalyzerOptions AnalyzerOptions => _projectState.AnalyzerOptions;

        /// <summary>
        /// The options used when building the compilation for this project.
        /// </summary>
        public CompilationOptions? CompilationOptions => _projectState.CompilationOptions;

        /// <summary>
        /// The options used when parsing documents for this project.
        /// </summary>
        public ParseOptions? ParseOptions => _projectState.ParseOptions;

        /// <summary>
        /// Returns true if this is a submission project.
        /// </summary>
        public bool IsSubmission => _projectState.IsSubmission;

        /// <summary>
        /// True if the project has any documents.
        /// </summary>
        public bool HasDocuments => !_projectState.DocumentStates.IsEmpty;

        /// <summary>
        /// All the document IDs associated with this project.
        /// </summary>
        public IReadOnlyList<DocumentId> DocumentIds => _projectState.DocumentStates.Ids;

        /// <summary>
        /// All the additional document IDs associated with this project.
        /// </summary>
        public IReadOnlyList<DocumentId> AdditionalDocumentIds => _projectState.AdditionalDocumentStates.Ids;

        /// <summary>
        /// All the additional document IDs associated with this project.
        /// </summary>
        internal IReadOnlyList<DocumentId> AnalyzerConfigDocumentIds => _projectState.AnalyzerConfigDocumentStates.Ids;

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
            => _projectState.DocumentStates.Contains(documentId);

        /// <summary>
        /// True if the project contains an additional document with the specified ID.
        /// </summary>
        public bool ContainsAdditionalDocument(DocumentId documentId)
            => _projectState.AdditionalDocumentStates.Contains(documentId);

        /// <summary>
        /// True if the project contains an <see cref="AnalyzerConfigDocument"/> with the specified ID.
        /// </summary>
        public bool ContainsAnalyzerConfigDocument(DocumentId documentId)
            => _projectState.AnalyzerConfigDocumentStates.Contains(documentId);

        /// <summary>
        /// Get the documentId in this project with the specified syntax tree.
        /// </summary>
        public DocumentId? GetDocumentId(SyntaxTree? syntaxTree)
            => _solution.GetDocumentId(syntaxTree, this.Id);

        /// <summary>
        /// Get the document in this project with the specified syntax tree.
        /// </summary>
        public Document? GetDocument(SyntaxTree? syntaxTree)
            => _solution.GetDocument(syntaxTree, this.Id);

        /// <summary>
        /// Get the document in this project with the specified document Id.
        /// </summary>
        public Document? GetDocument(DocumentId documentId)
            => ImmutableHashMapExtensions.GetOrAdd(ref _idToDocumentMap, documentId, s_tryCreateDocumentFunction, this);

        /// <summary>
        /// Get the additional document in this project with the specified document Id.
        /// </summary>
        public TextDocument? GetAdditionalDocument(DocumentId documentId)
            => ImmutableHashMapExtensions.GetOrAdd(ref _idToAdditionalDocumentMap, documentId, s_tryCreateAdditionalDocumentFunction, this);

        /// <summary>
        /// Get the analyzer config document in this project with the specified document Id.
        /// </summary>
        public AnalyzerConfigDocument? GetAnalyzerConfigDocument(DocumentId documentId)
            => ImmutableHashMapExtensions.GetOrAdd(ref _idToAnalyzerConfigDocumentMap, documentId, s_tryCreateAnalyzerConfigDocumentFunction, this);

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
        /// Gets all source generated documents in this project.
        /// </summary>
        public async ValueTask<IEnumerable<SourceGeneratedDocument>> GetSourceGeneratedDocumentsAsync(CancellationToken cancellationToken = default)
        {
            var generatedDocumentStates = await _solution.State.GetSourceGeneratedDocumentStatesAsync(this.State, cancellationToken).ConfigureAwait(false);

            // return an iterator to avoid eagerly allocating all the document instances
            return generatedDocumentStates.States.Select(state =>
                ImmutableHashMapExtensions.GetOrAdd(ref _idToSourceGeneratedDocumentMap, state.Id, s_createSourceGeneratedDocumentFunction, (state, this)))!;
        }

        internal async ValueTask<IEnumerable<Document>> GetAllRegularAndSourceGeneratedDocumentsAsync(CancellationToken cancellationToken = default)
        {
            return Documents.Concat(await GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false));
        }

        public async ValueTask<SourceGeneratedDocument?> GetSourceGeneratedDocumentAsync(DocumentId documentId, CancellationToken cancellationToken = default)
        {
            // Quick check first: if we already have created a SourceGeneratedDocument wrapper, we're good
            if (_idToSourceGeneratedDocumentMap.TryGetValue(documentId, out var sourceGeneratedDocument))
            {
                return sourceGeneratedDocument;
            }

            // We'll have to run generators if we haven't already and now try to find it.
            var generatedDocumentStates = await _solution.State.GetSourceGeneratedDocumentStatesAsync(State, cancellationToken).ConfigureAwait(false);
            var generatedDocumentState = generatedDocumentStates.GetState(documentId);
            if (generatedDocumentState != null)
            {
                return GetOrCreateSourceGeneratedDocument(generatedDocumentState);
            }

            return null;
        }

        internal SourceGeneratedDocument GetOrCreateSourceGeneratedDocument(SourceGeneratedDocumentState state)
            => ImmutableHashMapExtensions.GetOrAdd(ref _idToSourceGeneratedDocumentMap, state.Id, s_createSourceGeneratedDocumentFunction, (state, this))!;

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
            // Easy case: do we already have the SourceGeneratedDocument created?
            if (_idToSourceGeneratedDocumentMap.TryGetValue(documentId, out var document))
            {
                return document;
            }

            // Trickier case now: it's possible we generated this, but we don't actually have the SourceGeneratedDocument for it, so let's go
            // try to fetch the state.
            var documentState = _solution.State.TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(documentId);

            if (documentState == null)
            {
                return null;
            }

            return ImmutableHashMapExtensions.GetOrAdd(ref _idToSourceGeneratedDocumentMap, documentId, s_createSourceGeneratedDocumentFunction, (documentState, this));
        }

        internal async Task<bool> ContainsSymbolsWithNameAsync(string name, SymbolFilter filter, CancellationToken cancellationToken)
        {
            return this.SupportsCompilation &&
                   await _solution.State.ContainsSymbolsWithNameAsync(Id, name, filter, cancellationToken).ConfigureAwait(false);
        }

        internal async Task<bool> ContainsSymbolsWithNameAsync(Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken)
        {
            return this.SupportsCompilation &&
                   await _solution.State.ContainsSymbolsWithNameAsync(Id, predicate, filter, cancellationToken).ConfigureAwait(false);
        }

        internal async Task<IEnumerable<Document>> GetDocumentsWithNameAsync(Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken)
            => (await _solution.State.GetDocumentsWithNameAsync(Id, predicate, filter, cancellationToken).ConfigureAwait(false)).Select(s => _solution.GetDocument(s.Id)!);

        private static readonly Func<DocumentId, Project, Document?> s_tryCreateDocumentFunction =
            (documentId, project) => project._projectState.DocumentStates.TryGetState(documentId, out var state) ? new Document(project, state) : null;

        private static readonly Func<DocumentId, Project, AdditionalDocument?> s_tryCreateAdditionalDocumentFunction =
            (documentId, project) => project._projectState.AdditionalDocumentStates.TryGetState(documentId, out var state) ? new AdditionalDocument(project, state) : null;

        private static readonly Func<DocumentId, Project, AnalyzerConfigDocument?> s_tryCreateAnalyzerConfigDocumentFunction =
            (documentId, project) => project._projectState.AnalyzerConfigDocumentStates.TryGetState(documentId, out var state) ? new AnalyzerConfigDocument(project, state) : null;

        private static readonly Func<DocumentId, (SourceGeneratedDocumentState state, Project project), SourceGeneratedDocument> s_createSourceGeneratedDocumentFunction =
            (documentId, stateAndProject) => new SourceGeneratedDocument(stateAndProject.project, stateAndProject.state);

        /// <summary>
        /// Tries to get the cached <see cref="Compilation"/> for this project if it has already been created and is still cached. In almost all
        /// cases you should call <see cref="GetCompilationAsync"/> which will either return the cached <see cref="Compilation"/>
        /// or create a new one otherwise.
        /// </summary>
        public bool TryGetCompilation([NotNullWhen(returnValue: true)] out Compilation? compilation)
            => _solution.State.TryGetCompilation(this.Id, out compilation);

        /// <summary>
        /// Get the <see cref="Compilation"/> for this project asynchronously.
        /// </summary>
        /// <returns>
        /// Returns the produced <see cref="Compilation"/>, or <see langword="null"/> if <see
        /// cref="SupportsCompilation"/> returns <see langword="false"/>. This function will
        /// return the same value if called multiple times.
        /// </returns>
        public Task<Compilation?> GetCompilationAsync(CancellationToken cancellationToken = default)
            => _solution.State.GetCompilationAsync(_projectState, cancellationToken);

        /// <summary>
        /// Determines if the compilation returned by <see cref="GetCompilationAsync"/> and all its referenced compilation are from fully loaded projects.
        /// </summary>
        // TODO: make this public
        internal Task<bool> HasSuccessfullyLoadedAsync(CancellationToken cancellationToken = default)
            => _solution.State.HasSuccessfullyLoadedAsync(_projectState, cancellationToken);

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
        public VersionStamp Version => _projectState.Version;

        /// <summary>
        /// The version of the most recently modified document.
        /// </summary>
        public Task<VersionStamp> GetLatestDocumentVersionAsync(CancellationToken cancellationToken = default)
            => _projectState.GetLatestDocumentVersionAsync(cancellationToken);

        /// <summary>
        /// The most recent version of the project, its documents and all dependent projects and documents.
        /// </summary>
        public Task<VersionStamp> GetDependentVersionAsync(CancellationToken cancellationToken = default)
            => _solution.State.GetDependentVersionAsync(this.Id, cancellationToken);

        /// <summary>
        /// The semantic version of this project including the semantics of referenced projects.
        /// This version changes whenever the consumable declarations of this project and/or projects it depends on change.
        /// </summary>
        public Task<VersionStamp> GetDependentSemanticVersionAsync(CancellationToken cancellationToken = default)
            => _solution.State.GetDependentSemanticVersionAsync(this.Id, cancellationToken);

        /// <summary>
        /// The semantic version of this project not including the semantics of referenced projects.
        /// This version changes only when the consumable declarations of this project change.
        /// </summary>
        public Task<VersionStamp> GetSemanticVersionAsync(CancellationToken cancellationToken = default)
            => _projectState.GetSemanticVersionAsync(cancellationToken);

        /// <summary>
        /// Creates a new instance of this project updated to have the new assembly name.
        /// </summary>
        public Project WithAssemblyName(string assemblyName)
            => this.Solution.WithProjectAssemblyName(this.Id, assemblyName).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to have the new default namespace.
        /// </summary>
        public Project WithDefaultNamespace(string defaultNamespace)
            => this.Solution.WithProjectDefaultNamespace(this.Id, defaultNamespace).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to have the specified compilation options.
        /// </summary>
        public Project WithCompilationOptions(CompilationOptions options)
            => this.Solution.WithProjectCompilationOptions(this.Id, options).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to have the specified parse options.
        /// </summary>
        public Project WithParseOptions(ParseOptions options)
            => this.Solution.WithProjectParseOptions(this.Id, options).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to include the specified project reference
        /// in addition to already existing ones.
        /// </summary>
        public Project AddProjectReference(ProjectReference projectReference)
            => this.Solution.AddProjectReference(this.Id, projectReference).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to include the specified project references
        /// in addition to already existing ones.
        /// </summary>
        public Project AddProjectReferences(IEnumerable<ProjectReference> projectReferences)
            => this.Solution.AddProjectReferences(this.Id, projectReferences).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to no longer include the specified project reference.
        /// </summary>
        public Project RemoveProjectReference(ProjectReference projectReference)
            => this.Solution.RemoveProjectReference(this.Id, projectReference).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to replace existing project references 
        /// with the specified ones.
        /// </summary>
        public Project WithProjectReferences(IEnumerable<ProjectReference> projectReferences)
            => this.Solution.WithProjectReferences(this.Id, projectReferences).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to include the specified metadata reference
        /// in addition to already existing ones.
        /// </summary>
        public Project AddMetadataReference(MetadataReference metadataReference)
            => this.Solution.AddMetadataReference(this.Id, metadataReference).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to include the specified metadata references
        /// in addition to already existing ones.
        /// </summary>
        public Project AddMetadataReferences(IEnumerable<MetadataReference> metadataReferences)
            => this.Solution.AddMetadataReferences(this.Id, metadataReferences).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to no longer include the specified metadata reference.
        /// </summary>
        public Project RemoveMetadataReference(MetadataReference metadataReference)
            => this.Solution.RemoveMetadataReference(this.Id, metadataReference).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to replace existing metadata reference
        /// with the specified ones.
        /// </summary>
        public Project WithMetadataReferences(IEnumerable<MetadataReference> metadataReferences)
            => this.Solution.WithProjectMetadataReferences(this.Id, metadataReferences).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to include the specified analyzer reference 
        /// in addition to already existing ones.
        /// </summary>
        public Project AddAnalyzerReference(AnalyzerReference analyzerReference)
            => this.Solution.AddAnalyzerReference(this.Id, analyzerReference).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to include the specified analyzer references
        /// in addition to already existing ones.
        /// </summary>
        public Project AddAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferences)
            => this.Solution.AddAnalyzerReferences(this.Id, analyzerReferences).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to no longer include the specified analyzer reference.
        /// </summary>
        public Project RemoveAnalyzerReference(AnalyzerReference analyzerReference)
            => this.Solution.RemoveAnalyzerReference(this.Id, analyzerReference).GetProject(this.Id)!;

        /// <summary>
        /// Creates a new instance of this project updated to replace existing analyzer references 
        /// with the specified ones.
        /// </summary>
        public Project WithAnalyzerReferences(IEnumerable<AnalyzerReference> analyzerReferencs)
            => this.Solution.WithProjectAnalyzerReferences(this.Id, analyzerReferencs).GetProject(this.Id)!;

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
            return this.Solution.RemoveDocument(documentId).GetProject(this.Id)!;
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
            => this.Solution.RemoveAdditionalDocument(documentId).GetProject(this.Id)!;

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
            => this.Solution.RemoveAnalyzerConfigDocument(documentId).GetProject(this.Id)!;

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

        internal AnalyzerConfigOptionsResult? GetAnalyzerConfigOptions()
            => _projectState.GetAnalyzerConfigOptions();

        private string GetDebuggerDisplay()
            => this.Name;

        internal SkippedHostAnalyzersInfo GetSkippedAnalyzersInfo(DiagnosticAnalyzerInfoCache infoCache)
            => Solution.State.Analyzers.GetSkippedAnalyzersInfo(this, infoCache);
    }
}
