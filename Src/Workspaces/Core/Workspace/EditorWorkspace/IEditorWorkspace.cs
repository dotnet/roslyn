using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services
{
    /// <summary>
    /// A workspace that is associated with an editing environment. 
    /// </summary>
    public interface IEditorWorkspace : IWorkspace
    {
        /// <summary>
        /// An event that is fired when a documents is opened in the editor.
        /// </summary>
        event EventHandler<DocumentEventArgs> DocumentOpened;

        /// <summary>
        /// An event that is fired when a document is closed in the editor.
        /// </summary>
        event EventHandler<DocumentEventArgs> DocumentClosed;

        /// <summary>
        /// True if this workspace supports manually opening and closing documents.
        /// </summary>
        bool CanOpenDocuments { get; }

        /// <summary>
        /// Determines if the document is currently open in the host environment.
        /// </summary>
        bool IsDocumentOpen(DocumentId documentId);

        /// <summary>
        /// Gets a list of the currently opened documents.
        /// </summary>
        IEnumerable<DocumentId> GetOpenedDocuments();

        /// <summary>
        /// Open the specified document.
        /// </summary>
        void OpenDocument(DocumentId documentId);

        /// <summary>
        /// Close the specified document.
        /// </summary>
        void CloseDocument(DocumentId documentId);

        /// <summary>
        /// Tries to get the document ID associated with a text container. 
        /// Returns true if a document ID is found associated with the text container.
        /// </summary>
        bool TryGetDocumentId(ITextContainer textContainer, out DocumentId documentId);
    }
}