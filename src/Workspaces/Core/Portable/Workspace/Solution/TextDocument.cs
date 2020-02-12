﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public class TextDocument
    {
        internal TextDocumentState State { get; }
        internal TextDocumentKind Kind { get; }

        /// <summary>
        /// The project this document belongs to.
        /// </summary>
        public Project Project { get; }

        internal TextDocument(Project project, TextDocumentState state, TextDocumentKind kind)
        {
            Contract.ThrowIfNull(project);
            Contract.ThrowIfNull(state);

            this.Project = project;
            State = state;
            Kind = kind;
        }

        /// <summary>
        /// The document's identifier. Many document instances may share the same ID, but only one
        /// document in a solution may have that ID.
        /// </summary>
        public DocumentId Id => State.Id;

        /// <summary>
        /// The path to the document file or null if there is no document file.
        /// </summary>
        public string? FilePath => State.FilePath;

        /// <summary>
        /// The name of the document.
        /// </summary>
        public string Name => State.Name;

        /// <summary>
        /// The sequence of logical folders the document is contained in.
        /// </summary>
        public IReadOnlyList<string> Folders => State.Folders;

        /// <summary>
        /// A <see cref="IDocumentServiceProvider"/> associated with this document
        /// </summary>
        internal IDocumentServiceProvider Services => State.Services;

        /// <summary>
        /// Get the current text for the document if it is already loaded and available.
        /// </summary>
        public bool TryGetText([NotNullWhen(returnValue: true)] out SourceText? text)
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
        public Task<SourceText> GetTextAsync(CancellationToken cancellationToken = default)
        {
            return State.GetTextAsync(cancellationToken).AsTask();
        }

        /// <summary>
        /// Fetches the current text for the document synchronously.
        /// </summary>
        /// <remarks>This is internal for the same reason <see cref="Document.GetSyntaxTreeSynchronously(CancellationToken)"/> is internal:
        /// we have specialized cases where we need it, but we worry that making it public will do more harm than good.</remarks>
        internal SourceText GetTextSynchronously(CancellationToken cancellationToken)
        {
            return State.GetTextSynchronously(cancellationToken);
        }

        /// <summary>
        /// Gets the version of the document's text.
        /// </summary>
        public Task<VersionStamp> GetTextVersionAsync(CancellationToken cancellationToken = default)
        {
            return State.GetTextVersionAsync(cancellationToken);
        }

        /// <summary>
        /// Fetches the current version for the document synchronously.
        /// </summary>
        /// <remarks>This is internal for the same reason <see cref="Document.GetSyntaxTreeSynchronously(CancellationToken)"/> is internal:
        /// we have specialized cases where we need it, but we worry that making it public will do more harm than good.</remarks>
        internal VersionStamp GetTextVersionSynchronously(CancellationToken cancellationToken)
        {
            return State.GetTextVersionSynchronously(cancellationToken);
        }

        /// <summary>
        /// Gets the version of the document's top level signature.
        /// </summary>
        internal Task<VersionStamp> GetTopLevelChangeTextVersionAsync(CancellationToken cancellationToken = default)
        {
            return State.GetTopLevelChangeTextVersionAsync(cancellationToken);
        }

        /// <summary>
        /// True if the info of the document change (name, folders, file path; not the content).
        /// </summary>
        internal virtual bool HasInfoChanged(TextDocument otherTextDocument)
        {
            return State.HasInfoChanged(otherTextDocument.State);
        }

        /// <summary>
        /// Only checks if the source of the text has changed, no content check is done.
        /// </summary>
        internal bool HasTextChanged(TextDocument otherTextDocument, bool ignoreUnchangeableDocument)
        {
            return State.HasTextChanged(otherTextDocument.State, ignoreUnchangeableDocument);
        }
    }
}
