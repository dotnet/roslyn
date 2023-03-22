// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Marker interface to indicate that a workspace wants to be updated with document changes when the LSP server is told
/// about them.  In VS, for example, we have a non-mutating workspace.  The VS workspace itself is updated automatically
/// with the real contents of buffers, and by tracking the running-doc-table.  Changes that come in through LSP do not
/// impact this actual workspace.  Instead, the <see cref="LspWorkspaceManager"/> creates an overlay, where it uses the
/// <see cref="Solution"/> snapshot from the VS workspace, but then forks it in any cases where it's view of the world
/// may differ from LSP (which can happen as LSP is async, and so may represent a state of the world that is slightly
/// different from what the VS workspace thinks it is.
/// <para/>
/// 
/// For hosts though where there is no external source of truth (for example, a server that might host roslyn directly,
/// where all info comes from LSP), we want the changes that LSP knows about to 'push through' to the workspace's model.
/// That way, the workspace's solution is always in sync with what LSP thinks it is (at least for open documents).
/// </summary>
interface IMutatingLspWorkspace
{
}
