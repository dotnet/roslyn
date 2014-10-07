using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services
{
    /// <summary>
    /// Represents a snapshot of a project at any point in time.
    /// </summary>
    public interface IProject
    {
        /// <summary>
        /// The ID of the project. Multiple IProject instances may share the same ID. However, only
        /// one project may have this ID in any given solution.
        /// </summary>
        ProjectId Id { get; }

        /// <summary>
        /// The name of the project. This may be different than the assembly name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The name of the assembly this project represents.
        /// </summary>
        string AssemblyName { get; }

        /// <summary>
        /// The path to the project file or null if there is no project file.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// The solution this project belongs to.
        /// </summary>
        ISolution Solution { get; }

        /// <summary>
        /// The language service provider associated with this project. You can use this provider to
        /// access language specific services.
        /// </summary>
        ILanguageServiceProvider LanguageServices { get; }

        /// <summary>
        /// The options used when building the compilation for this project.
        /// </summary>
        CommonCompilationOptions CompilationOptions { get; }

        /// <summary>
        /// The options used when parsing documents for this project.
        /// </summary>
        CommonParseOptions ParseOptions { get; }

        /// <summary>
        /// Assembly resolver used to resolve reference names and relative paths.
        /// </summary>
        FileResolver FileResolver { get; }

        /// <summary>
        /// Get the compilation for this project if it is available.
        /// </summary>
        bool TryGetCompilation(out CommonCompilation compilation);

        /// <summary>
        /// Get the compilation corresponding to this project. The first time this is called the
        /// compilation will be built.
        /// </summary>
        CommonCompilation GetCompilation(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get the compilation for this project asynchronously.
        /// </summary>
        Task<CommonCompilation> GetCompilationAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// True if the project has any documents.
        /// </summary>
        bool HasDocuments { get; }

        /// <summary>
        /// True if the project contains a document with the specified ID.
        /// </summary>
        bool ContainsDocument(DocumentId documentId);

        /// <summary>
        /// All the document IDs associated with this project.
        /// </summary>
        IEnumerable<DocumentId> DocumentIds { get; }

        /// <summary>
        /// All the documents associated with this project.
        /// </summary>
        IEnumerable<IDocument> Documents { get; }

        /// <summary>
        /// The list of all other projects within the same solution that this project references.
        /// </summary>
        IEnumerable<ProjectReference> ProjectReferences { get; }

        /// <summary>
        /// The list of all other projects that this project references, including projects that 
        /// are not part of the solution.
        /// </summary>
        IEnumerable<ProjectReference> AllProjectReferences { get; }

        /// <summary>
        /// The list of all other metadata sources (assemblies) that this project references.
        /// </summary>
        IEnumerable<MetadataReference> MetadataReferences { get; }

        /// <summary>
        /// Get the document in this project with the specified document Id.
        /// </summary>
        IDocument GetDocument(DocumentId documentId);

        /// <summary>
        /// Get the document in this project with the specified syntax tree.
        /// </summary>
        IDocument GetDocument(CommonSyntaxTree syntaxTree);

        /// <summary>
        /// Gets an object that lists the added, changed and removed documents between this project and the specified project.
        /// </summary>
        ProjectChanges GetChanges(IProject oldProject);

        /// <summary>
        /// Returns true if this is a submission project.
        /// </summary>
        bool IsSubmission { get; }

        /// <summary>
        /// The project version. This equates to the version of the project file.
        /// </summary>
        VersionStamp Version { get; }

        /// <summary>
        /// The version of the most recently modified document.
        /// </summary>
        Task<VersionStamp> GetLatestDocumentVersionAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// The latest version of the project, its documents and all dependent projects and documents.
        /// </summary>
        Task<VersionStamp> GetDependentVersionAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
