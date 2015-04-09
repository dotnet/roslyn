// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    /// <summary>
    /// Provides access to an interactive window being hosted inside of Visual Studio's process using the
    /// default tool window.
    /// 
    /// These tool windows are created using ProvideInteractiveWindowAttribute which provides the normal
    /// tool window registration options.  Instances of the tool window are then created using 
    /// IVsInteractiveWindowFactory when VS calls on your packages IVsToolWindowFactory.CreateToolWindow
    /// method.
    /// </summary>
    public interface IVsInteractiveWindow
    {
        /// <summary>
        /// Gets the interactive window instance.
        /// </summary>
        IInteractiveWindow InteractiveWindow { get; }

        /// <summary>
        /// Shows the window.
        /// </summary>
        void Show(bool focus);

        /// <summary>
        /// Configures the window for the specified VS language service guid language preferences.
        /// 
        /// Also installs a language appropriate command filter if one is exported via IVsInteractiveWindowOleCommandTargetProvider.
        /// </summary>
        void SetLanguage(Guid languageServiceGuid, IContentType contentType);
    }
}
