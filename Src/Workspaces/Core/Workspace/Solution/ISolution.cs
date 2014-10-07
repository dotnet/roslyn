using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services
{
    /// <summary>
    /// Represents a set of projects, documents and compilations.
    /// </summary>
    public interface ISolution
    {
        /// <summary>
        /// The Workspace this solution is associated with.
        /// </summary>
        IWorkspace Workspace { get; }

        /// <summary>
        /// The Id of the solution. Multiple solution instances may share the same Id.
        /// </summary>
        SolutionId Id { get; }

        /// <summary>
        /// The path to the solution file or null if there is no solution file.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// True if the solution contains a project with the specified project ID.
        /// </summary>
        bool ContainsProject(ProjectId projectId);

        /// <summary>
        /// A list of all the ids for all the projects contained by the solution.
        /// </summary>
        IEnumerable<ProjectId> ProjectIds { get; }

        /// <summary>
        /// A list of all the projects contained by the solution.
        /// </summary>
        IEnumerable<IProject> Projects { get; }

        /// <summary>
        /// Gets the project in this solution with the specified project ID.
        /// </summary>
        IProject GetProject(ProjectId projectId);

        /// <summary>
        /// Gets the project in this solution with the specified name.
        /// </summary>
        IEnumerable<IProject> GetProjectsByName(string name);

        /// <summary>
        /// Gets the project in this solution with the specified assembly name.
        /// </summary>
        IEnumerable<IProject> GetProjectsByAssemblyName(string assemblyName);

        /// <summary>
        /// Provides files for metadata references of projects contained by the solution.
        /// </summary>
        MetadataFileProvider MetadataFileProvider { get; }

        /// <summary>
        /// Creates a new solution instance that includes a project with the specified language and
        /// names.
        /// </summary>
        ISolution AddProject(ProjectId projectId, string projectName, string assemblyName, string languageName);

        /// <summary>
        /// Create a new solution instance that includes a project with the specified project
        /// information.
        /// </summary>
        ISolution AddProject(ProjectInfo projectInfo);

        /// <summary>
        /// Create a new solution instance without the project specified.
        /// </summary>
        ISolution RemoveProject(ProjectId projectId);

        /// <summary>
        /// Creates a new solution instance with the project specified updated to have the new
        /// assembly name.
        /// </summary>
        ISolution WithProjectAssemblyName(ProjectId projectId, string assemblyName);

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified compilation options.
        /// </summary>
        ISolution WithProjectCompilationOptions(ProjectId projectId, CommonCompilationOptions options);

        /// <summary>
        /// Create a new solution instance with the project specified updated to have
        /// the specified parse options.
        /// </summary>
        ISolution WithProjectParseOptions(ProjectId projectId, CommonParseOptions options);

        /// <summary>
        /// Create a new solution instance with the project specified updated to include
        /// the specified project reference.
        /// </summary>
        ISolution AddProjectReference(ProjectId projectId, ProjectReference projectReference);

        /// <summary>
        /// Create a new solution instance with the project specified updated to include
        /// the specified project references.
        /// </summary>
        ISolution AddProjectReferences(ProjectId projectId, IEnumerable<ProjectReference> projectReferences);

        /// <summary>
        /// Create a new solution instance with the project specified updated to no longer
        /// include the specified project reference.
        /// </summary>
        ISolution RemoveProjectReference(ProjectId projectId, ProjectReference projectReference);

        /// <summary>
        /// Create a new solution instance with the project specified updated to contain
        /// the specified list of project references.
        /// </summary>
        ISolution WithProjectReferences(ProjectId projectId, IEnumerable<ProjectReference> projectReferences);

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the 
        /// specified metadata reference.
        /// </summary>
        ISolution AddMetadataReference(ProjectId projectId, MetadataReference metadataReference);

        /// <summary>
        /// Create a new solution instance with the project specified updated to include the
        /// specified metadata references.
        /// </summary>
        ISolution AddMetadataReferences(ProjectId projectId, IEnumerable<MetadataReference> metadataReferences);

        /// <summary>
        /// Create a new solution instance with the project specified updated to no longer include
        /// the specified metadata reference.
        /// </summary>
        ISolution RemoveMetadataReference(ProjectId projectId, MetadataReference metadataReference);

        /// <summary>
        /// Create a new solution instance with the project specified updated to include only the
        /// specified metadata references.
        /// </summary>
        ISolution WithProjectMetadataReferences(ProjectId projectId, IEnumerable<MetadataReference> metadataReferences);

        /// <summary>
        /// True if the solution contains the document in one of its projects
        /// </summary>
        bool ContainsDocument(DocumentId documentId);

        /// <summary>
        /// Gets the document in this solution with the specified document ID.
        /// </summary>
        IDocument GetDocument(DocumentId documentId);

        /// <summary>
        /// Gets the document in this solution with the specified syntax tree.
        /// </summary>
        IDocument GetDocument(CommonSyntaxTree syntaxTree);

        /// <summary>
        /// Create a new solution instance with the corresponding project updated to include a new 
        /// document instanced defined by the document info.
        /// </summary>
        ISolution AddDocument(DocumentInfo documentInfo);

        /// <summary>
        /// Creates a new solution instance with the corresponding project updated to include a new
        /// document instance defined by its name and text.
        /// </summary>
        ISolution AddDocument(DocumentId documentId, string name, IText text, IEnumerable<string> folders = null);

        /// <summary>
        /// Creates a new solution instance with the project updated to include a new document with
        /// the arguments specified.
        /// </summary>
        ISolution AddDocument(DocumentId documentId, string name, TextLoader loader, IEnumerable<string> folders = null);

        /// <summary>
        /// Creates a new solution instance with the project updated to include a new document that
        /// will load its text from the file path.
        /// </summary>
        ISolution AddDocument(DocumentId documentId, string filePath, IEnumerable<string> folders = null);

        /// <summary>
        /// Creates a new solution instance updated to include a document equivalent to the one specified.
        /// </summary>
        ISolution AddDocument(IDocument document);

        /// <summary>
        /// Creates a new solution instance that no longer includes the specified document.
        /// </summary>
        ISolution RemoveDocument(DocumentId documentId);

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the source
        /// code kind specified.
        /// </summary>
        ISolution WithDocumentSourceCodeKind(DocumentId documentId, SourceCodeKind kind);

        /// <summary>
        /// Creates a new solution instance with the document specified updated to be contained in
        /// the sequence of logical folders.
        /// </summary>
        ISolution WithDocumentFolders(DocumentId documentId, IEnumerable<string> folderNames);

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the text
        /// specified.
        /// </summary>
        ISolution WithDocumentText(DocumentId documentId, IText text, PreservationMode mode = PreservationMode.PreserveValue);

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have the text
        /// supplied by the text loader.
        /// </summary>
        ISolution WithDocumentTextLoader(DocumentId documentId, TextLoader loader);

        /// <summary>
        /// Creates a new solution instance with the document specified updated to have a syntax tree
        /// rooted by the specified syntax node.
        /// </summary>
        ISolution WithDocumentSyntaxRoot(DocumentId documentId, CommonSyntaxNode root);

        /// <summary>
        /// Creates a new solution where the specified project uses the given <see cref="FileResolver" />
        /// </summary>
        ISolution WithProjectFileResolver(ProjectId id, FileResolver fileResolver);

        /// <summary>
        /// Gets a copy of the solution isolated from the original so that they do not share computed data.
        /// </summary>
        /// <remarks>
        /// Use isolated solutions when doing operations that are likely to access a lot of text,
        /// syntax trees or compilations. When the isolated solution is reclaimed so will the computed results.
        /// </remarks>
        ISolution GetIsolatedSolution();

        /// <summary>
        /// Gets an objects that lists the added, changed and removed projects between
        /// this solution and the specified solution.
        /// </summary>
        SolutionChanges GetChanges(ISolution oldSolution);

        /// <summary>
        /// The solution version. This equates to the solution file's version.
        /// </summary>
        VersionStamp Version { get; }

        /// <summary>
        /// The version of the most recently modified project.
        /// </summary>
        Task<VersionStamp> GetLatestProjectVersionAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets a solution that contains only the projects that have compilations and documents that have parsed syntax trees.
        /// 
        /// Since the solution represents an immutable deferred data structure many of the expensive to compute elements such as
        /// syntax trees and compilations are not immediately available. They are computed when first accessed. A partial solution
        /// is a consistent subset of a solution with only those projects and documents that already have those expensive
        /// elements computed, reducing the chance that any expensive work will occur when those elements are accessed.
        /// 
        /// This method may return different solution instances each time it is called, representing the consistent partial solution that
        /// is available at that time.
        /// 
        /// For the partial solution to have any content at all this solution must have had compilations accessed at some point.
        /// Some workspaces have background compilers that asynchronously build compilations.
        /// </summary>
        ISolution GetPartialSolution();

        /// <summary>
        /// Gets a document that contains the specified text. If the corresponding document is known but contains different
        /// text then create a new solution with the document updated to have the specified text. Returns true if a document
        /// can be found or created. Returns false if no document corresponding to the text can be identified.
        /// </summary>
        bool TryGetDocumentWithSpecificText(IText text, out IDocument document);
    }
}