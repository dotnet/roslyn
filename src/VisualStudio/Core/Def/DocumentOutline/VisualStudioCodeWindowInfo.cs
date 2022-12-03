// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline
{
    /// <summary>
    /// All the information the document outline feature needs from Visual Studio.
    /// </summary>
    internal record VisualStudioCodeWindowInfo(ITextBuffer TextBuffer, string FilePath, SnapshotPoint? CaretPoint);
}
