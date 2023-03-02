// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal sealed partial class DocumentOutlineViewModel
    {
        /// <summary>
        /// Represents the state machine of possible changes that we can encounter in our queue.
        /// </summary>
        private sealed record ViewModelStateDataChange(
            string? SearchText, // symbol user is searching for, null if no search is active
            CaretPosition? CaretPositionOfNodeToSelect, // position of a symbol we want to select, null if no selection is required.
            bool? ShouldExpand, // asks to set the expand or collapse state for all items
            bool DataUpdated); // indicates that data has been updated so we don't just filter/sort existing items.
    }
}
