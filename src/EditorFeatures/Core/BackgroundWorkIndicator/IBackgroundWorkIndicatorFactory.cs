// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator
{
    /// <summary>
    /// Factory for creating lightweight <see cref="IUIThreadOperationContext"/>s that can sit in the editor in a
    /// unobtrusive fashion unlike the Threaded-Wait-Dialog.  Features can use this to indicate to users that work
    /// is happening in the background while not blocking the user from continuing to work with their code.
    /// </summary>
    internal interface IBackgroundWorkIndicatorFactory : IWorkspaceService
    {
        /// <summary>
        /// Creates a new background work indicator that appears as a tooltip at the requested location to notify the
        /// user that background work is happening.  The work always starts initially cancellable by the user hitting
        /// the 'escape' key, but this can be overridden by calling <see cref="IUIThreadOperationContext.AddScope"/> and
        /// passing in <see langword="false"/> for <c>allowCancellation</c>.
        /// </summary>
        /// <remarks>
        /// Default cancellation behavior can also be specified through <paramref name="cancelOnEdit"/> and <paramref
        /// name="cancelOnFocusLost"/>. However, this cancellation will only happen if the context is cancellable at the
        /// time those respective events happen.
        /// </remarks>
        IUIThreadOperationContext Create(
            ITextView textView, SnapshotSpan applicableToSpan,
            string description, bool cancelOnEdit = true, bool cancelOnFocusLost = true);
    }

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
}
