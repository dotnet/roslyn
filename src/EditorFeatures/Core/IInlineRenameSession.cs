// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor;

internal sealed class InlineRenameSessionInfo
{
    /// <summary>
    /// Whether or not the entity at the selected location can be renamed.
    /// </summary>
    public bool CanRename { get; }

    /// <summary>
    /// Provides the reason that can be displayed to the user if the entity at the selected 
    /// location cannot be renamed.
    /// </summary>
    public string LocalizedErrorMessage { get; }

    /// <summary>
    /// The session created if it was possible to rename the entity.
    /// </summary>
    public IInlineRenameSession Session { get; }

    internal InlineRenameSessionInfo(string localizedErrorMessage)
    {
        this.CanRename = false;
        this.LocalizedErrorMessage = localizedErrorMessage;
    }

    internal InlineRenameSessionInfo(IInlineRenameSession session)
    {
        this.CanRename = true;
        this.Session = session;
    }
}

internal interface IInlineRenameSession
{
    /// <summary>
    /// Cancels the rename session, and undoes any edits that had been performed by the session.
    /// </summary>
    void Cancel();

    /// <summary>
    /// Dismisses the rename session, completing the rename operation across all files.
    /// </summary>
    Task<bool> CommitAsync(bool previewChanges);
}
