using System.Collections.Generic;
using Roslyn.Compilers;

namespace Roslyn.Services
{
    /// <summary>
    /// Used by HostWorkspace to access information about a source document. This data is read once
    /// and is expected to be the state of the document when it is first loaded or declared, except
    /// for Loader which always returns the current source text as stored on disk.
    /// </summary>
    public interface IDocumentInfo
    {
        /// <summary>
        /// The unique Id for the document.
        /// </summary>
        DocumentId Id { get; }

        /// <summary>
        /// The version of the document's text. This may be null if no version is known.
        /// </summary>
        VersionStamp Version { get; }

        /// <summary>
        /// The list of nested folders describing where the document logically sits with respect to
        /// other documents. The folders may not match the filename.
        /// </summary>
        IList<string> Folders { get; }

        /// <summary>
        /// The name of the document. It may be different than the filename.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The source code kind of the document.
        /// </summary>
        SourceCodeKind SourceCodeKind { get; }

        /// <summary>
        /// The path to the document file or null if there is no document file.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// The loader that will load the document from storage.
        /// </summary>
        TextLoader Loader { get; }
    }
}
