// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    /// <summary>
    /// A CommandFilter used for "normal" files, as opposed to Venus files which are special.
    /// </summary>
    internal sealed class StandaloneCommandFilter<TPackage, TLanguageService> : AbstractVsTextViewFilter<TPackage, TLanguageService>
        where TPackage : AbstractPackage<TPackage, TLanguageService>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
    {
        /// <summary>
        /// Creates a new command handler that is attached to an IVsTextView.
        /// </summary>
        /// <param name="wpfTextView">The IWpfTextView of the view.</param>
        /// <param name="languageService">The language service</param>
        internal StandaloneCommandFilter(
            TLanguageService languageService,
            IWpfTextView wpfTextView,
            IComponentModel componentModel)
            : base(languageService, wpfTextView, componentModel)
        {
        }
    }
}
