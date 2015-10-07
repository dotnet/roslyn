// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.InteractiveWindow
{
    public interface IInteractiveWindowOperations2 : IInteractiveWindowOperations
    {
        /// <summary>
        /// Copies the current selection to the clipboard.
        /// </summary>
        void Copy();

        /// <summary>
        /// Delete Line; Delete all selected lines, or the current line if no selection.  
        /// </summary>
        void DeleteLine();

        /// <summary>
        /// Line Cut; Cut all selected lines, or the current line if no selection, to the clipboard.   
        /// </summary>           
        void CutLine();
    }
}
