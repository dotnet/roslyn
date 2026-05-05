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
    /// <summary>
    /// Adds the document to an appropriate workspace. May initiate work to load a project for the document.
    /// Note that the implementation of this method should not depend on anything expensive such as RPC calls.
    /// async is used here to allow taking locks asynchronously and "relatively fast" stuff like that.
    /// </summary>
    ValueTask<TextDocument?> AddDocumentAsync(DocumentUri documentUri, TrackedDocumentInfo trackedDocumentInfo);

    /// <summary>
    /// Removes the document with the given <paramref name="uri"/> from the miscellaneous files workspace.
    /// Used to remove unneeded documents from the miscellaneous files workspace,
    /// when a document is found in a non-miscellaneous files workspace.
    /// </summary>
    /// <returns><see langword="true"/> when a document was found and removed</returns>
    ValueTask<bool> TryRemoveMiscellaneousDocumentAsync(DocumentUri uri);

    /// <summary>
    /// Signals to this provider that the document with the given <paramref name="uri"/> was closed.
    /// Separate from 'TryRemoveMiscellaneousDocumentAsync' because it can either remove documents from the miscellaneous files or host workspace.
    /// For example, for file-based apps, we wouldn't want to unload them just because we found a document in a non-miscellaneous files workspace,
    /// but we may want to unload the file-based app if its entry point file is closed.
    /// </summary>
    ValueTask CloseDocumentAsync(DocumentUri uri);
}
