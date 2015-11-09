// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    public static class InteractiveWindowExtensions
    {
        /// <summary>
        /// Gets the interactive window associated with the text buffer if the text
        /// buffer is being hosted in the interactive window.
        /// 
        /// Returns null if the text buffer is not hosted in the interactive window.
        /// </summary>
        public static IInteractiveWindow GetInteractiveWindow(this ITextBuffer buffer)
        {
            return InteractiveWindow.FromBuffer(buffer);
        }
    }
}
