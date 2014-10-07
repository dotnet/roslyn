using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services
{
    /// <summary>
    /// Represents a document that is part of a solution and project. Equivalent to a single .cs or
    /// .vb file, but abstracted away from the notion of a file system.
    /// </summary>
    public interface IDocument
    {
        /// <summary>
        /// The document's identifier. Many document instances may share the same ID, but only one
        /// document in a solution may have that ID.
        /// </summary>
        DocumentId Id { get; }

        /// <summary>
        /// The path to the document file or null if there is no document file.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// The project this document belongs to.
        /// </summary>
        IProject Project { get; }

        /// <summary>
        /// The sequence of logical folders the document is contained in.
        /// </summary>
        IList<string> Folders { get; }

        /// <summary>
        /// The name of the document.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The kind of source code this document contains.
        /// </summary>
        SourceCodeKind SourceCodeKind { get; }

        /// <summary>
        /// The language services provider you should use to get access to language specific services.
        /// </summary>
        ILanguageServiceProvider LanguageServices { get; }

        /// <summary>
        /// Get the current text for the document if it is already loaded and available.
        /// </summary>
        bool TryGetText(out IText text);

        /// <summary>
        /// Get the current text for the document. This method may do work to load the text from
        /// disk (or other source) the first time it is called.
        /// </summary>
        IText GetText(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the current text for the document asynchronously.
        /// </summary>
        Task<IText> GetTextAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Get the current syntax tree for the document if the text is already loaded and the tree
        /// is already parsed.
        /// </summary>
        bool TryGetSyntaxTree(out CommonSyntaxTree syntaxTree);

        /// <summary>
        /// Gets the SyntaxTree corresponding to this document. This method may do work to load the
        /// text from disk and parse it into a syntax tree.
        /// </summary>
        CommonSyntaxTree GetSyntaxTree(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the SyntaxTree for this document asynchronously.
        /// </summary>
        Task<CommonSyntaxTree> GetSyntaxTreeAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the root node of the current syntax tree if it is available.
        /// </summary>
        bool TryGetSyntaxRoot(out CommonSyntaxNode root);

        /// <summary>
        /// Gets the root node of the current syntax tree.
        /// </summary>
        CommonSyntaxNode GetSyntaxRoot(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the root node of the syntax tree asynchronously.
        /// </summary>
        Task<CommonSyntaxNode> GetSyntaxRootAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the current semantic model for this document if the model is already computed.
        /// </summary>
        bool TryGetSemanticModel(out ISemanticModel semanticModel);

        /// <summary>
        /// Get the semantic model corresponding to this document. This method may do work to build the 
        /// compilation for the project.
        /// </summary>
        ISemanticModel GetSemanticModel(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the semantic model for this document asynchronously.
        /// </summary>
        Task<ISemanticModel> GetSemanticModelAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the version of the document's text.
        /// </summary>
        Task<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the version of the syntax tree. This is generally the newer of the text version and the project's version.
        /// </summary>
        Task<VersionStamp> GetSyntaxVersionAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Returns <c>true</c> if the provided position is in a hidden region inaccessible to the
        /// user.
        /// </summary>
        bool IsHiddenPosition(int position, CancellationToken cancellationToken = default(CancellationToken));
    }
}