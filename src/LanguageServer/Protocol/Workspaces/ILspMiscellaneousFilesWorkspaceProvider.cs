// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Allows the LSP server to create miscellaneous files documents.
/// </summary>
/// <remarks>
/// No methods will be called concurrently, since we are dispatching LSP requests one at a time in the core dispatching loop.
/// This does mean methods should be reasonably fast since they will block the LSP server from processing other requests while they are running.
/// </remarks>
internal interface ILspMiscellaneousFilesWorkspaceProvider : ILspService
{
    bool ManagesWorkspace(Workspace workspace);

    /// <summary>
    /// Gets or adds a document to the appropriate workspace potentially based on the document's contents.
    /// Note that the implementation of this method should not depend on anything expensive such as RPC calls.
    /// async is used here to allow taking locks asynchronously and "relatively fast" stuff like that.
    /// </summary>
    ValueTask<(TextDocument document, bool alreadyExists)?> GetOrAddDocumentAsync(DocumentUri documentUri, TrackedDocumentInfo trackedDocumentInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the document with the given <paramref name="uri"/> from the miscellaneous files workspace.
    /// If the miscellaneous files workspace already does not contain such a document, does nothing.
    /// Note that the implementation of this method should not depend on anything expensive such as RPC calls.
    /// async is used here to allow taking locks asynchronously and "relatively fast" stuff like that.
    /// </summary>
    /// <returns><see langword="true"/> when a document was found and removed</returns>
    ValueTask<bool> TryRemoveMiscellaneousDocumentAsync(DocumentUri uri);

    /// <summary>
    /// Notify this provider that a document was closed.
    /// This may result in unloading the document from the miscellaneous files workspace or from the host workspace.
    /// </summary>
    ValueTask CloseDocumentAsync(DocumentUri uri);
}
