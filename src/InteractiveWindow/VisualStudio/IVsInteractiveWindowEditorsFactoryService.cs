// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.InteractiveWindow;

namespace Microsoft.VisualStudio
{
    /// <summary>
    /// Provides access to information and settings for an interactive window created inside of Visual Studio.
    /// </summary>
    public interface IVsInteractiveWindowEditorsFactoryService
    {
        /// <summary>
        /// Gets the text view host for the given IInteractiveWindow instance.
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        IWpfTextViewHost GetTextViewHost(IInteractiveWindow window);

        /// <summary>
        /// Configures the window for the specified VS language service guid language preferences.
        /// 
        /// Also installs a language appropriate command filter if one is exported via IVsInteractiveWindowOleCommandTargetProvider.
        /// </summary>
        void SetLanguage(IInteractiveWindow window, Guid languageServiceGuid);
    }
}
