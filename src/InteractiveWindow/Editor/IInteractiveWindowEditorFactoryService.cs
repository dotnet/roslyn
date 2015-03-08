// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Implements the service that creates text views and buffers for the interactive window.
    /// 
    /// There is a single implementation of this service for each MEF composition catalog.  The
    /// service understands how the editors and buffers need to be created and sets them up
    /// so that commands are properly routed to the editor window.
    /// 
    /// This service is imported by <see cref="Microsoft.VisualStudio.InteractiveWindow.IInteractiveWindowFactoryService"/> 
    /// to use in the creation of <see cref="Microsoft.VisualStudio.InteractiveWindow.IInteractiveWindow"/>s.
    /// </summary>
    public interface IInteractiveWindowEditorFactoryService
    {
        /// <summary>
        /// Creates a new text view for an interactive window.
        /// </summary>
        /// <param name="window">The interactive window the text view is being created for.</param>
        /// <param name="buffer">The projection buffer used for displaying the interactive window</param>
        /// <param name="roles">The requested text view roles.</param>
        IWpfTextView CreateTextView(IInteractiveWindow window, ITextBuffer buffer, ITextViewRoleSet roles);

        /// <summary>
        /// Creates a new input buffer for the interactive window.
        /// </summary>
        ITextBuffer CreateAndActivateBuffer(IInteractiveWindow window);
    }
}
