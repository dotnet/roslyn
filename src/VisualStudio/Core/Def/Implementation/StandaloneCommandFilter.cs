// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    /// <summary>
    /// A CommandFilter used for "normal" files, as opposed to Venus files which are special.
    /// </summary>
    internal sealed class StandaloneCommandFilter : AbstractVsTextViewFilter
    {
        /// <summary>
        /// Creates a new command handler that is attached to an IVsTextView.
        /// </summary>
        /// <param name="wpfTextView">The IWpfTextView of the view.</param>
        internal StandaloneCommandFilter(
            IWpfTextView wpfTextView,
            IComponentModel componentModel)
            : base(wpfTextView, componentModel)
        {
        }
    }
}
