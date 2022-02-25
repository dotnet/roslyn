// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;

/// <summary>
/// A default implementation of the background work indicator which simply defers to a threaded-wait-dialog to
/// indicator that background work is happening.
/// </summary>
[ExportWorkspaceService(typeof(IBackgroundWorkIndicatorFactory)), Shared]
internal class DefaultBackgroundWorkIndicatorFactory : IBackgroundWorkIndicatorFactory
{
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultBackgroundWorkIndicatorFactory(
        IUIThreadOperationExecutor uiThreadOperationExecutor)
    {
        _uiThreadOperationExecutor = uiThreadOperationExecutor;
    }

    public IUIThreadOperationContext Create(
        ITextView textView, SnapshotSpan applicableToSpan, string description, bool cancelOnEdit = true, bool cancelOnFocusLost = true)
    {
        return _uiThreadOperationExecutor.BeginExecute(
            description, description, allowCancellation: true, showProgress: true);
    }
}
