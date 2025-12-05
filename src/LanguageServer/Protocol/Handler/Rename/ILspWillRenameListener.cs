// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Allows listening for workspace/didRenameFiles notifications.
/// </summary>
/// <remarks>
/// Although the registration for willRename allows specifying a document selector, and that registration is passed
/// along to the client, the LSP server itself does not filter notifications based on that selector. It is up to the
/// the listener to determine if it cares about the rename notification or not. If any listener returns an edit, no
/// further listeners are called.
/// </remarks>
internal interface ILspWillRenameListener
{
    Task<WorkspaceEdit?> HandleWillRenameAsync(RenameFilesParams renameParams, RequestContext context, CancellationToken cancellationToken);
}
