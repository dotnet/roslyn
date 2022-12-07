// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    /// <summary>
    /// All the information the document outline feature needs from Visual Studio.
    /// </summary>
    /// <param name="TextBuffer">text buffer used by the editor to find our language-server implementation.</param>
    /// <param name="FilePath">file path used as part of the LSP request.</param>
    /// <param name="CaretPoint">Current caret position in the code window.</param>
    internal sealed record VisualStudioCodeWindowInfo(ITextBuffer TextBuffer, string FilePath, SnapshotPoint? CaretPoint);
}
