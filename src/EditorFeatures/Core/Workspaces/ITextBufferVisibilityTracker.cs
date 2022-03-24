// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Workspaces
{
    internal interface ITextBufferVisibilityTracker
    {
        /// <summary>
        /// Can be called on any thread.
        /// </summary>
        bool IsVisible(ITextBuffer subjectBuffer);

        /// <summary>
        /// Will always fire on UI thread.
        /// </summary>
        event EventHandler? DocumentsChanged;
    }
}
