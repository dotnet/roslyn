// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.InteractiveWindow.Shell
{
    /// <summary>
    /// Extends <see cref="IVsInteractiveWindow"/> with additional functionality.
    /// </summary>
    public interface IVsInteractiveWindow2 : IVsInteractiveWindow
    {
        /// <summary>
        /// Gets the <see cref="IVsWindowFrame"/> associated with the <see cref="IVsInteractiveWindow2"/>.
        /// </summary>
        object WindowFrame { get; }

        /// <summary>
        /// Gets or sets the ImageMoniker for the icon for this tool window. This property
        /// should be used instead of BitmapResource and BitmapIndex to allow for DPI-aware
        /// icons.
        /// </summary>
        ImageMoniker BitmapImageMoniker { get; set; }
    }
}
