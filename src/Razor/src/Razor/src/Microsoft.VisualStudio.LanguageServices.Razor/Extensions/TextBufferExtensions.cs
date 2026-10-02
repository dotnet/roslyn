// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Razor.Extensions;

internal static class TextBufferExtensions
{
    /// <summary>
    /// Indicates if a <paramref name="textBuffer"/> has the LSP Razor content type. This is represented by the LSP based ASP.NET Core Razor editor.
    /// </summary>
    /// <param name="textBuffer">The text buffer to inspect</param>
    /// <returns><c>true</c> if the text buffers content type represents an ASP.NET Core LSP based Razor editor content type.</returns>
    public static bool IsRazorLSPBuffer(this ITextBuffer textBuffer)
    {
        var matchesContentType = textBuffer.ContentType.IsOfType(RazorConstants.RazorLSPContentTypeName);
        return matchesContentType;
    }
}
