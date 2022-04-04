// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
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

    public IBackgroundWorkIndicatorContext Create(
        ITextView textView, SnapshotSpan applicableToSpan, string description, bool cancelOnEdit = true, bool cancelOnFocusLost = true)
    {
        return new DefaultBackgroundWorkIndicatorContext(_uiThreadOperationExecutor.BeginExecute(
            description, description, allowCancellation: true, showProgress: true));
    }

    private class DefaultBackgroundWorkIndicatorContext : IBackgroundWorkIndicatorContext
    {
        private readonly IUIThreadOperationContext _context;

        public DefaultBackgroundWorkIndicatorContext(IUIThreadOperationContext context)
        {
            _context = context;
        }

        public bool CancelOnEdit { get; set; }
        public bool CancelOnFocusLost { get; set; }

        public IUIThreadOperationScope AddScope(bool allowCancellation, string description)
            => _context.AddScope(allowCancellation, description);

        public void TakeOwnership()
            => _context.TakeOwnership();

        public CancellationToken UserCancellationToken
            => _context.UserCancellationToken;

        public bool AllowCancellation
            => _context.AllowCancellation;

        public string Description
            => _context.Description;

        public IEnumerable<IUIThreadOperationScope> Scopes
            => _context.Scopes;

        public PropertyCollection Properties
            => _context.Properties;

        public void Dispose()
            => _context.Dispose();
    }
}
