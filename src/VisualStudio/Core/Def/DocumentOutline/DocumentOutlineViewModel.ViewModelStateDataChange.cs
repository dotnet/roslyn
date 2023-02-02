// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    internal sealed partial class DocumentOutlineViewModel
    {
        private sealed record ViewModelStateDataChange(string? SearchText, CaretPosition? CaretPositionOfNodeToSelect, ExpansionOption? ExpansionOption, bool DataUpdated);
    }
}
