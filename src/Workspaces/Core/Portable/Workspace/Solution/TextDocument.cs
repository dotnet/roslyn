// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public class TextDocument
    {
        private readonly TextDocumentState _state;

        internal virtual TextDocumentState GetDocumentState()
        {
            return _state;
        }

        /// <summary>
        /// The project this document belongs to.
        /// </summary>
        public Project Project { get; protected set; }

        protected TextDocument()
        {
        }

        internal TextDocument(Project project, TextDocumentState state)
        {
            Contract.ThrowIfNull(project);
            Contract.ThrowIfNull(state);

            this.Project = project;
            _state = state;
        }

        /// <summary>
        /// The document's identifier. Many document instances may share the same ID, but only one
        /// document in a solution may have that ID.
        /// </summary>
        public DocumentId Id
        {
            get { return this.GetDocumentState().Id; }
        }

        /// <summary>
        /// The path to the document file or null if there is no document file.
        /// </summary>
        public string FilePath
        {
            get { return this.GetDocumentState().FilePath; }
        }

        /// <summary>
        /// The name of the document.
        /// </summary>
        public string Name
        {
            get
            {
                return this.GetDocumentState().Name;
            }
        }

        /// <summary>
        /// The sequence of logical folders the document is contained in.
        /// </summary>
        public IReadOnlyList<string> Folders
        {
            get
            {
                return this.GetDocumentState().Folders;
            }
        }

        /// <summary>
        /// Get the current text for the document if it is already loaded and available.
        /// </summary>
        public bool TryGetText(out SourceText text)
        {
            return this.GetDocumentState().TryGetText(out text);
        }

        /// <summary>
        /// Gets the version of the document's text if it is already loaded and available.
        /// </summary>
        public bool TryGetTextVersion(out VersionStamp version)
        {
            return this.GetDocumentState().TryGetTextVersion(out version);
        }

        /// <summary>
        /// Gets the current text for the document asynchronously.
        /// </summary>
        public Task<SourceText> GetTextAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.GetDocumentState().GetTextAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the version of the document's text.
        /// </summary>
        public Task<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.GetDocumentState().GetTextVersionAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the version of the document's top level signature.
        /// </summary>
        internal Task<VersionStamp> GetTopLevelChangeTextVersionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.GetDocumentState().GetTopLevelChangeTextVersionAsync(cancellationToken);
        }
    }
}
