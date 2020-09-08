// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        public bool HasDocuments => _projectState.HasDocuments;

        /// <summary>
        /// All the document IDs associated with this project.
        /// </summary>
        public IReadOnlyList<DocumentId> DocumentIds => _projectState.DocumentIds;

        /// <summary>
        /// All the additional document IDs associated with this project.
        /// </summary>
        public IReadOnlyList<DocumentId> AdditionalDocumentIds => _projectState.AdditionalDocumentIds;

        /// <summary>
        /// All the documents associated with this project.
        /// </summary>
        public IEnumerable<Document> Documents => _projectState.DocumentIds.Select(GetDocument)!;

        /// <summary>
        /// All the additional documents associated with this project.
        /// </summary>
        public IEnumerable<TextDocument> AdditionalDocuments => _projectState.AdditionalDocumentIds.Select(GetAdditionalDocument)!;

        /// <summary>
        /// All the <see cref="AnalyzerConfigDocument"/>s associated with this project.
        /// </summary>
        public IEnumerable<AnalyzerConfigDocument> AnalyzerConfigDocuments => _projectState.AnalyzerConfigDocumentIds.Select(GetAnalyzerConfigDocument)!;

        /// <summary>
        /// True if the project contains a document with the specified ID.
        /// </summary>
        public bool ContainsDocument(DocumentId documentId)
            => _projectState.ContainsDocument(documentId);

        /// <summary>
        /// True if the project contains an additional document with the specified ID.
        /// </summary>
        public bool ContainsAdditionalDocument(DocumentId documentId)
            => _projectState.ContainsAdditionalDocument(documentId);

        /// <summary>
        /// True if the project contains an <see cref="AnalyzerConfigDocument"/> with the specified ID.
        /// </summary>
        public bool ContainsAnalyzerConfigDocument(DocumentId documentId)
            => _projectState.ContainsAnalyzerConfigDocument(documentId);

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
        {
            if (!ContainsDocument(documentId))
            {
                return null;
            }

            return ImmutableHashMapExtensions.GetOrAdd(ref _idToDocumentMap, documentId, s_createDocumentFunction, this);
        }

        /// <summary>
        /// Get the additional document in this project with the specified document Id.
        /// </summary>
        public TextDocument? GetAdditionalDocument(DocumentId documentId)
        {
            if (!ContainsAdditionalDocument(documentId))
            {
                return null;
            }

            return ImmutableHashMapExtensions.GetOrAdd(ref _idToAdditionalDocumentMap, documentId, s_createAdditionalDocumentFunction, this);
        }

        /// <summary>
        /// Get the analyzer config document in this project with the specified document Id.
        /// </summary>
        public AnalyzerConfigDocument? GetAnalyzerConfigDocument(DocumentId documentId)
        {
            if (!ContainsAnalyzerConfigDocument(documentId))
            {
                return null;
            }

            return ImmutableHashMapExtensions.GetOrAdd(ref _idToAnalyzerConfigDocumentMap, documentId, s_createAnalyzerConfigDocumentFunction, this);
        }

        internal DocumentState? GetDocumentState(DocumentId documentId)
            => _projectState.GetDocumentState(documentId);

        internal TextDocumentState? GetAdditionalDocumentState(DocumentId documentId)
            => _projectState.GetAdditionalDocumentState(documentId);

        internal AnalyzerConfigDocumentState? GetAnalyzerConfigDocumentState(DocumentId documentId)
            => _projectState.GetAnalyzerConfigDocumentState(documentId);

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

        private static readonly Func<DocumentId, Project, Document> s_createDocumentFunction = CreateDocument;
        private static Document CreateDocument(DocumentId documentId, Project project)
        {
            var state = project._projectState.GetDocumentState(documentId);
            Contract.ThrowIfNull(state);
            return new Document(project, state);
        }

        private static readonly Func<DocumentId, Project, AdditionalDocument> s_createAdditionalDocumentFunction = CreateAdditionalDocument;
        private static AdditionalDocument CreateAdditionalDocument(DocumentId documentId, Project project)
        {
            var state = project._projectState.GetAdditionalDocumentState(documentId);
            Contract.ThrowIfNull(state);
            return new AdditionalDocument(project, state);
        }

        private static readonly Func<DocumentId, Project, AnalyzerConfigDocument> s_createAnalyzerConfigDocumentFunction = CreateAnalyzerConfigDocument;
        private static AnalyzerConfigDocument CreateAnalyzerConfigDocument(DocumentId documentId, Project project)
        {
            var state = project._projectState.GetAnalyzerConfigDocumentState(documentId);
            Contract.ThrowIfNull(state);
            return new AnalyzerConfigDocument(project, state);
        }

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
        public async Task<VersionStamp> GetSemanticVersionAsync(CancellationToken cancellationToken = default)
        {
            var projVersion = this.Version;
            var docVersion = await _projectState.GetLatestDocumentTopLevelChangeVersionAsync(cancellationToken).ConfigureAwait(false);
            return docVersion.GetNewerVersion(projVersion);
        }

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

        internal Task<GeneratorDriverRunResult?> GetGeneratorDriverRunResultAsync(CancellationToken cancellationToken)
            => _solution.State.GetGeneratorDriverRunResultAsync(_projectState, cancellationToken);
    }
}
