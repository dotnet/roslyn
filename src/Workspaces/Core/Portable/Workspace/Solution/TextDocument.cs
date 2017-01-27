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
        internal readonly TextDocumentState State;

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
            State = state;
        }

        /// <summary>
        /// The document's identifier. Many document instances may share the same ID, but only one
        /// document in a solution may have that ID.
        /// </summary>
        public DocumentId Id
        {
            get { return State.Id; }
        }

        /// <summary>
        /// The path to the document file or null if there is no document file.
        /// </summary>
        public string FilePath
        {
            get { return State.FilePath; }
        }

        /// <summary>
        /// The name of the document.
        /// </summary>
        public string Name
        {
            get
            {
                return State.Name;
            }
        }

        /// <summary>
        /// The sequence of logical folders the document is contained in.
        /// </summary>
        public IReadOnlyList<string> Folders
        {
            get
            {
                return State.Folders;
            }
        }

        /// <summary>
        /// Get the current text for the document if it is already loaded and available.
        /// </summary>
        public bool TryGetText(out SourceText text)
        {
            return State.TryGetText(out text);
        }

        /// <summary>
        /// Gets the version of the document's text if it is already loaded and available.
        /// </summary>
        public bool TryGetTextVersion(out VersionStamp version)
        {
            return State.TryGetTextVersion(out version);
        }

        /// <summary>
        /// Gets the current text for the document asynchronously.
        /// </summary>
        public Task<SourceText> GetTextAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return State.GetTextAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the version of the document's text.
        /// </summary>
        public Task<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return State.GetTextVersionAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the version of the document's top level signature.
        /// </summary>
        internal Task<VersionStamp> GetTopLevelChangeTextVersionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return State.GetTopLevelChangeTextVersionAsync(cancellationToken);
        }
    }
}
