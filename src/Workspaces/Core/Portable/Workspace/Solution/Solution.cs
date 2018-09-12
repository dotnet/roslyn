// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        internal Solution(Workspace workspace, SolutionInfo info)
            : this(new SolutionState(workspace, info))
        {
        }

        internal SolutionState State => _state;

        internal int WorkspaceVersion => _state.WorkspaceVersion;

        internal SolutionServices Services => _state.Services;

        internal BranchId BranchId => _state.BranchId;

        internal ProjectState GetProjectState(ProjectId projectId) => _state.GetProjectState(projectId);

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
        public string FilePath => _state.FilePath;

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
        public IEnumerable<Project> Projects => ProjectIds.Select(id => GetProject(id));

        /// <summary>
        /// The version of the most recently modified project.
        /// </summary>
        public VersionStamp GetLatestProjectVersion() => _state.GetLatestProjectVersion();

        /// <summary>
        /// True if the solution contains a project with the specified project ID.
        /// </summary>
        public bool ContainsProject(ProjectId projectId) => _state.ContainsProject(projectId);

        /// <summary>
        /// Gets the project in this solution with the specified project ID. 
        /// 
        /// If the id is not an id of a project that is part of this solution the method returns null.
        /// </summary>
        public Project GetProject(ProjectId projectId)
        {
            // ContainsProject checks projectId being null
            if (this.ContainsProject(projectId))
            {
                return ImmutableHashMapExtensions.GetOrAdd(ref _projectIdToProjectMap, projectId, s_createProjectFunction, this);
            }

            return null;
        }

        private static readonly Func<ProjectId, Solution, Project> s_createProjectFunction = CreateProject;
        private static Project CreateProject(ProjectId projectId, Solution solution)
        {
            return new Project(solution, solution.State.GetProjectState(projectId));
        }

        /// <summary>
        /// Gets the <see cref="Project"/> associated with an assembly symbol.
        /// </summary>
        public Project GetProject(IAssemblySymbol assemblySymbol, CancellationToken cancellationToken = default)
        {
            var projectState = _state.GetProjectState(assemblySymbol, cancellationToken);

            return projectState == null ? null : GetProject(projectState.Id);
        }

        /// <summary>
        /// True if the solution contains the document in one of its projects
        /// </summary>
        public bool ContainsDocument(DocumentId documentId) => _state.ContainsDocument(documentId);

        /// <summary>
        /// True if the solution contains the additional document in one of its projects
        /// </summary>
        public bool ContainsAdditionalDocument(DocumentId documentId) => _state.ContainsAdditionalDocument(documentId);

        /// <summary>
        /// Gets the documentId in this solution with the specified syntax tree.
        /// </summary>
        public DocumentId GetDocumentId(SyntaxTree syntaxTree) => GetDocumentId(syntaxTree, projectId: null);

        /// <summary>
        /// Gets the documentId in this solution with the specified syntax tree.
        /// </summary>
        public DocumentId GetDocumentId(SyntaxTree syntaxTree, ProjectId projectId)
        {
            if (syntaxTree != null)
            {
                // is this tree known to be associated with a document?
                var documentId = DocumentState.GetDocumentIdForTree(syntaxTree);
                if (documentId != null && (projectId == null || documentId.ProjectId == projectId))
                {
                    // does this solution even have the document?
                    if (this.ContainsDocument(documentId))
                    {
                        return documentId;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the document in this solution with the specified document ID.
        /// </summary>
        public Document GetDocument(DocumentId documentId)
        {
            // ContainsDocument checks documentId being null
            if (this.ContainsDocument(documentId))
            {
                return this.GetProject(documentId.ProjectId).GetDocument(documentId);
            }

            return null;
        }

        /// <summary>
        /// Gets the additional document in this solution with the specified document ID.
        /// </summary>
        public TextDocument GetAdditionalDocument(DocumentId documentId)
        {
            if (documentId != null && this.ContainsAdditionalDocument(documentId))
            {
                return this.GetProject(documentId.ProjectId).GetAdditionalDocument(documentId);
            }

            return null;
        }

        /// <summary>
        /// Gets the document in this solution with the specified syntax tree.
        /// </summary>
        public Document GetDocument(SyntaxTree syntaxTree)
        {
            return this.GetDocument(syntaxTree, projectId: null);
        }

        internal Document GetDocument(SyntaxTree syntaxTree, ProjectId projectId)
        {
            if (syntaxTree != null)
            {
                // is this tree known to be associated with a document?
                var docId = DocumentState.GetDocumentIdForTree(syntaxTree);
                if (docId != null && (projectId == null || docId.ProjectId == projectId))
                {
                    // does this solution even have the document?
                    var document = this.GetDocument(docId);
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

        /// <summary>
        /// Creates a new solution instance that includes a project with the specified language and names.
        /// Returns the new project.
        /// </summary>
        public Project AddProject(string name, string assemblyName, string language)
        {
            var id = ProjectId.CreateNewId(debugName: name);
            return this.AddProject(id, name, assemblyName, language).GetProject(id);
        }

        /// <summary>
        /// Creates a new solution instance that includes a project with the specified language and names.
        /// </summary>
        public Solution AddProject(ProjectId projectId, string name, string assemblyName, string language)
        {
            return this.AddProject(ProjectInfo.Create(projectId, VersionStamp.Create(), name, assemblyName, language));
        }

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
        public Solution WithProjectOutputFilePath(ProjectId projectId, string outputFilePath)
        {
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
        public Solution WithProjectOutputRefFilePath(ProjectId projectId, string outputRefFilePath)
        {
            var newState = _state.WithProjectOutputRefFilePath(projectId, outputRefFilePath);
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
        public Solution WithProjectFilePath(ProjectId projectId, string filePath)
        {
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
            var newState = _state.WithProjectParseOptions(projectId, options);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Update a project as a result of option changes.
        /// 
        /// this is a temporary workaround until editorconfig becomes real part of roslyn solution snapshot.
        /// until then, this will explicitly fork current solution snapshot
        /// </summary>
        internal Solution WithProjectOptionsChanged(ProjectId projectId)
        {
            var newState = _state.WithProjectOptionsChanged(projectId);
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
        // TODO: make it public
        internal Solution WithHasAllInformation(ProjectId projectId, bool hasAllInformation)
        {
            var newState = _state.WithHasAllInformation(projectId, hasAllInformation);
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
        public Solution AddProjectReference(ProjectId projectId, ProjectReference projectReference)
        {
            var newState = _state.AddProjectReference(projectId, projectReference);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include
        /// the specified project references.
        /// </summary>
        public Solution AddProjectReferences(ProjectId projectId, IEnumerable<ProjectReference> projectReferences)
        {
            var newState = _state.AddProjectReferences(projectId, projectReferences);
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
        public Solution RemoveProjectReference(ProjectId projectId, ProjectReference projectReference)
        {
            var newState = _state.RemoveProjectReference(projectId, projectReference);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to contain
        /// the specified list of project references.
        /// </summary>
        public Solution WithProjectReferences(ProjectId projectId, IEnumerable<ProjectReference> projectReferences)
        {
            var newState = _state.WithProjectReferences(projectId, projectReferences);
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
        public Solution AddMetadataReference(ProjectId projectId, MetadataReference metadataReference)
        {
            var newState = _state.AddMetadataReference(projectId, metadataReference);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the
        /// specified metadata references.
        /// </summary>
        public Solution AddMetadataReferences(ProjectId projectId, IEnumerable<MetadataReference> metadataReferences)
        {
            var newState = _state.AddMetadataReferences(projectId, metadataReferences);
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
        public Solution RemoveMetadataReference(ProjectId projectId, MetadataReference metadataReference)
        {
            var newState = _state.RemoveMetadataReference(projectId, metadataReference);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include only the
        /// specified metadata references.
        /// </summary>
        public Solution WithProjectMetadataReferences(ProjectId projectId, IEnumerable<MetadataReference> metadataReferences)
        {
            var newState = _state.WithProjectMetadataReferences(projectId, metadataReferences);
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
        public Solution AddAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            var newState = _state.AddAnalyzerReference(projectId, analyzerReference);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the
        /// specified analyzer references.
        /// </summary>
        public Solution AddAnalyzerReferences(ProjectId projectId, IEnumerable<AnalyzerReference> analyzerReferences)
        {
            var newState = _state.AddAnalyzerReferences(projectId, analyzerReferences);
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
        public Solution RemoveAnalyzerReference(ProjectId projectId, AnalyzerReference analyzerReference)
        {
            var newState = _state.RemoveAnalyzerReference(projectId, analyzerReference);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Create a new solution instance with the project specified updated to include only the
        /// specified analyzer references.
        /// </summary>
        public Solution WithProjectAnalyzerReferences(ProjectId projectId, IEnumerable<AnalyzerReference> analyzerReferences)
        {
            var newState = _state.WithProjectAnalyzerReferences(projectId, analyzerReferences);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        private static SourceCodeKind GetSourceCodeKind(ProjectState project)
        {
            return project.ParseOptions != null ? project.ParseOptions.Kind : SourceCodeKind.Regular;
        }

        /// <summary>
        /// Creates a new solution instance with the corresponding project updated to include a new
        /// document instance defined by its name and text.
        /// </summary>
        public Solution AddDocument(DocumentId documentId, string name, string text, IEnumerable<string> folders = null, string filePath = null)
        {
            return this.AddDocument(documentId, name, SourceText.From(text), folders, filePath);
        }

        /// <summary>
        /// Creates a new solution instance with the corresponding project updated to include a new
        /// document instance defined by its name and text.
        /// </summary>
        public Solution AddDocument(DocumentId documentId, string name, SourceText text, IEnumerable<string> folders = null, string filePath = null, bool isGenerated = false)
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
        public Solution AddDocument(DocumentId documentId, string name, SyntaxNode syntaxRoot, IEnumerable<string> folders = null, string filePath = null, bool isGenerated = false, PreservationMode preservationMode = PreservationMode.PreserveValue)
        {
            return this.AddDocument(documentId, name, SourceText.From(string.Empty), folders, filePath, isGenerated).WithDocumentSyntaxRoot(documentId, syntaxRoot, preservationMode);
        }

        /// <summary>
        /// Creates a new solution instance with the project updated to include a new document with
        /// the arguments specified.
        /// </summary>
        public Solution AddDocument(DocumentId documentId, string name, TextLoader loader, IEnumerable<string> folders = null)
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
        {
            var newState = _state.AddDocument(documentInfo);
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
        public Solution AddAdditionalDocument(DocumentId documentId, string name, string text, IEnumerable<string> folders = null, string filePath = null)
        {
            return this.AddAdditionalDocument(documentId, name, SourceText.From(text), folders, filePath);
        }

        /// <summary>
        /// Creates a new solution instance with the corresponding project updated to include a new
        /// additional document instance defined by its name and text.
        /// </summary>
        public Solution AddAdditionalDocument(DocumentId documentId, string name, SourceText text, IEnumerable<string> folders = null, string filePath = null)
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

            var version = VersionStamp.Create();
            var loader = TextLoader.From(TextAndVersion.Create(text, version, name));

            var info = DocumentInfo.Create(
                documentId,
                name: name,
                folders: folders,
                sourceCodeKind: GetSourceCodeKind(project),
                loader: loader,
                filePath: filePath);

            return this.AddAdditionalDocument(info);
        }

        public Solution AddAdditionalDocument(DocumentInfo documentInfo)
        {
            var newState = _state.AddAdditionalDocument(documentInfo);
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
            var newState = _state.RemoveDocument(documentId);
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
            var newState = _state.RemoveAdditionalDocument(documentId);
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
        public Solution WithDocumentFolders(DocumentId documentId, IEnumerable<string> folders)
        {
            var newState = _state.WithDocumentFolders(documentId, folders);
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
            var newState = _state.WithAdditionalDocumentText(documentId, text, mode);
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
            var newState = _state.WithAdditionalDocumentText(documentId, textAndVersion, mode);
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
            return WithDocumentTextLoader(documentId, loader, textOpt: null, mode: mode);
        }

        internal Solution WithDocumentTextLoader(DocumentId documentId, TextLoader loader, SourceText textOpt, PreservationMode mode)
        {
            var newState = _state.WithDocumentTextLoader(documentId, loader, textOpt, mode);
            if (newState == _state)
            {
                return this;
            }

            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with the additional document specified updated to have the text
        /// supplied by the text loader.
        /// </summary>
        public Solution WithAdditionalDocumentTextLoader(DocumentId documentId, TextLoader loader, PreservationMode mode)
        {
            var newState = _state.WithAdditionalDocumentTextLoader(documentId, loader, mode);
            if (newState == _state)
            {
                return this;
            }

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
            IMergeConflictHandler mergeConflictHandler = null,
            CancellationToken cancellationToken = default)
        {
            // we only log sessioninfo for actual changes committed to workspace which should exclude ones from preview
            var session = new LinkedFileDiffMergingSession(oldSolution, this, solutionChanges ?? this.GetChanges(oldSolution), logSessionInfo: solutionChanges != null);

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

            var documentState = projectState.GetDocumentState(documentId);
            if (documentState == null)
            {
                // this document no longer exist
                return ImmutableArray<DocumentId>.Empty;
            }

            var filePath = documentState.FilePath;
            if (string.IsNullOrEmpty(filePath))
            {
                // this document can't have any related document. only related document is itself.
                return ImmutableArray.Create<DocumentId>(documentId);
            }

            var documentIds = this.GetDocumentIdsWithFilePath(filePath);
            return this.FilterDocumentIdsByLanguage(documentIds, projectState.ProjectInfo.Language).ToImmutableArray();
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
        /// Gets a copy of the solution isolated from the original so that they do not share computed state.
        /// 
        /// Use isolated solutions when doing operations that are likely to access a lot of text,
        /// syntax trees or compilations that are unlikely to be needed again after the operation is done. 
        /// When the isolated solution is reclaimed so will the computed state.
        /// </summary>
        public Solution GetIsolatedSolution()
        {
            var newState = _state.GetIsolatedSolution();
            return new Solution(newState);
        }

        /// <summary>
        /// Creates a new solution instance with all the documents specified updated to have the same specified text.
        /// </summary>
        public Solution WithDocumentText(IEnumerable<DocumentId> documentIds, SourceText text, PreservationMode mode = PreservationMode.PreserveValue)
        {
            var newState = _state.WithDocumentText(documentIds, text, mode);
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
        public ImmutableArray<DocumentId> GetDocumentIdsWithFilePath(string filePath) => _state.GetDocumentIdsWithFilePath(filePath);

        /// <summary>
        /// Gets a <see cref="ProjectDependencyGraph"/> that details the dependencies between projects for this solution.
        /// </summary>
        public ProjectDependencyGraph GetProjectDependencyGraph() => _state.GetProjectDependencyGraph();

        /// <summary>
        /// Returns the options that should be applied to this solution. This is equivalent to <see cref="Workspace.Options" /> when the <see cref="Solution"/> 
        /// instance was created.
        /// </summary>
        public OptionSet Options
        {
            get
            {
                // TODO: actually make this a snapshot
                return this.Workspace.Options;
            }
        }
    }
}
