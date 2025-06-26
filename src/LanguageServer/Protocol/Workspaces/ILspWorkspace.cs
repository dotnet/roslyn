// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Interface to indicate that a workspace wants to customize what happens in the lsp server when lsp contents for a
/// file are updated by an lsp client. In VS, for example, we have a non-mutating workspace.  The VS workspace itself is
/// updated automatically with the real contents of buffers, and by tracking the running-doc-table.  Changes that come
/// in through LSP do not impact this actual workspace.  Instead, the <see cref="LspWorkspaceManager"/> creates an
/// overlay, where it uses the <see cref="Solution"/> snapshot from the VS workspace, but then forks it in any cases
/// where it's view of the world may differ from LSP (which can happen as LSP is async, and so may represent a state of
/// the world that is slightly different from what the VS workspace thinks it is.
/// <para/>
/// For hosts though where there is no external source of truth (for example, a server that might host roslyn directly,
/// where all info comes from LSP), this enables LSP to 'push through' file changes directly to the to the workspace's
/// model. That way, the workspace's solution is always in sync with what LSP thinks it is (at least for open
/// documents).
/// <para/>
/// It is fine for external changes to happen to the contents of documents within the workspace (see <see
/// cref="Workspace.OnDocumentTextChanged(Document)"/>).  However, they will be overwritten by the <see
/// cref="LspWorkspaceManager"/> for any changed documents it knows about (through <see
/// cref="LSP.Methods.TextDocumentDidChange"/>).
/// </summary>
internal interface ILspWorkspace
{
    bool SupportsMutation { get; }

    /// <summary>
    /// If <paramref name="documentId"/> is currently within this workspace, then its text is updated to <paramref
    /// name="sourceText"/>.  Does nothing if the document is not present in the workspace (for example if something
    /// else removed it).
    /// </summary>
    ValueTask UpdateTextIfPresentAsync(DocumentId documentId, SourceText sourceText, CancellationToken cancellationToken);
}
