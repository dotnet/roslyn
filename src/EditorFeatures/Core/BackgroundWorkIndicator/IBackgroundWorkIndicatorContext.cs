// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.BackgroundWorkIndicator;

internal interface IBackgroundWorkIndicatorContext : IUIThreadOperationContext
{
    /// <summary>
    /// Whether or not this context should cancel work if a navigation happens. Clients that use this indicator can 
    /// have this behavior set to true (so that they cancel if the user edits themselves), but then set this to
    /// false right before they edit themselves so that they do not self-cancel.
    /// </summary>
    bool CancelOnEdit { get; set; }

    /// <summary>
    /// Whether or not this context should cancel work if the text view it is attached to loses focus. Clients that use
    /// this indicator can have this behavior set to true (so that they cancel if the user navigates themselves), but
    /// then set this to false right before they navigate themselves so that they do not self-cancel.
    /// </summary>
    bool CancelOnFocusLost { get; set; }
}
