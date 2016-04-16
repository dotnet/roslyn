#if !NEWCOMPLETION
// Copyright (c) Microsoft Corporation
// All rights reserved
// REMOVE ONCE WE ACTUALLY REFERENCE THE REAL EDITOR DLLS.
using System;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    internal class IntellisenseFilter : IIntellisenseFilter
    {
        /// <summary>
        /// Create an instance of an IntellisenseFilter with the specified attributes.
        /// </summary>
        public IntellisenseFilter(ImageMoniker moniker, string toolTip, string accessKey, string automationText, bool initialIsChecked = false, bool initialIsEnabled = true)
        {
            if (string.IsNullOrEmpty(accessKey))
            {
                throw new ArgumentException("Must not be null or empty", nameof(accessKey));
            }
            this.Moniker = moniker;
            this.ToolTip = toolTip;
            this.AccessKey = accessKey;
            this.AutomationText = automationText;
            this.IsChecked = initialIsChecked;
            this.IsEnabled = initialIsEnabled;
        }
        /// <summary>
        /// The icon shown on the filter's button.
        /// </summary>
        public ImageMoniker Moniker { get; }
        /// <summary>
        /// The tooltip shown when the mouse hovers over the button.
        /// </summary>
        public string ToolTip { get; }
        /// <summary>
        /// The key used to toggle the filter's state.
        /// </summary>
        public string AccessKey { get; }
        /// <summary>
        /// String used to represent the button for automation.
        /// </summary>
        public string AutomationText { get; }
        /// <summary>
        /// Has the user turned the filter on?
        /// </summary>
        /// <remarks>
        /// The setter will be called when the user toggles the corresponding filter button.
        /// </remarks>
        public virtual bool IsChecked { get; set; }
        /// <summary>
        /// Is the filter enabled?
        /// </summary>
        /// <remarks>
        /// <para>Disabled filters are shown but are grayed out.</para>
        /// <para>Intellisense will never call the setter but the <see cref="CompletionSet"/> owner may and the Intellisense popup will respect the changes.</para>
        /// </remarks>
        public bool IsEnabled { get; set; }
    }
}
#endif