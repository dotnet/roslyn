#if !NEWCOMPLETION
// Copyright (c) Microsoft Corporation
// All rights reserved
// REMOVE ONCE WE ACTUALLY REFERENCE THE REAL EDITOR DLLS.
using System;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a filter used to add a row of filter buttons to the bottom
    /// </summary>
    internal interface IIntellisenseFilter
    {
        /// <summary>
        /// The icon shown on the filter's button.
        /// </summary>
        ImageMoniker Moniker { get; }
        /// <summary>
        /// The tooltip shown when the mouse hovers over the button.
        /// </summary>
        string ToolTip { get; }
        /// <summary>
        /// The key used to toggle the filter's state.
        /// </summary>
        string AccessKey { get; }
        /// <summary>
        /// String used to represent the button for automation.
        /// </summary>
        string AutomationText { get; }
        /// <summary>
        /// Has the user turned the filter on?
        /// </summary>
        /// <remarks>
        /// The setter will be called when the user toggles the corresponding filter button.
        /// </remarks>
        bool IsChecked { get; set; }
        /// <summary>
        /// Is the filter enabled?
        /// </summary>
        /// <remarks>
        /// Disabled filters are shown but are grayed out.
        /// </remarks>
        bool IsEnabled { get; set; }
    }
}
#endif