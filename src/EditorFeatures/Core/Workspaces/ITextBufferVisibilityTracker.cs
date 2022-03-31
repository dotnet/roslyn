// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Workspaces
{
    /// <summary>
    /// All methods must be called on UI thread.
    /// </summary>
    internal interface ITextBufferVisibilityTracker
    {
        /// <summary>
        /// Whether or not this text buffer is in an actively visible <see cref="ITextView"/>.
        /// </summary>
        bool IsVisible(ITextBuffer subjectBuffer);

        /// <summary>
        /// Registers to hear about visibility changes for this particular buffer.
        /// </summary>
        void RegisterForVisibilityChanges(ITextBuffer subjectBuffer, ITextBufferVisibilityChangedCallback callback);
        void UnregisterForVisibilityChanges(ITextBuffer subjectBuffer, ITextBufferVisibilityChangedCallback callback);
    }

    internal interface ITextBufferVisibilityChangedCallback
    {
        void OnTextBufferVisibilityChanged();
    }
}
