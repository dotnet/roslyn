// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;

/// <summary>
/// Factory for creating lightweight <see cref="IUIThreadOperationContext"/>s that can sit in the editor in a
/// unobtrusive fashion unlike the Threaded-Wait-Dialog.  Features can use this to indicate to users that work
/// is happening in the background while not blocking the user from continuing to work with their code.
/// </summary>
/// <remarks>
/// Only one background work indicator can be active at a time.  Any attempt to make a new indicator will cancel any
/// existing outstanding item.
/// </remarks>
internal interface IBackgroundWorkIndicatorFactory : IWorkspaceService
{
    /// <summary>
    /// Creates a new background work indicator that appears as a tooltip at the requested location to notify the
    /// user that background work is happening.  The work is always cancellable the user hitting the 'escape' key.
    /// Any attempt to set <see cref="IUIThreadOperationScope.AllowCancellation"/> to <see langword="false"/> is
    /// simply ignored.
    /// </summary>
    /// <remarks>
    /// Default cancellation behavior can also be specified through <paramref name="cancelOnEdit"/> and <paramref
    /// name="cancelOnFocusLost"/>.
    /// </remarks>
    IBackgroundWorkIndicatorContext Create(
        ITextView textView, SnapshotSpan applicableToSpan,
        string description, bool cancelOnEdit = true, bool cancelOnFocusLost = true);
}
