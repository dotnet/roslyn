// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Hover;

internal readonly record struct HoverDisplayOptions(MarkupKind MarkupKind, bool SupportsVisualStudioExtensions)
{
    public static HoverDisplayOptions From(ClientCapabilities clientCapabilities)
    {
        var markupKind = clientCapabilities.GetMarkupKind();
        var supportsVisualStudioExtensions = clientCapabilities.SupportsVisualStudioExtensions();

        return new(markupKind, supportsVisualStudioExtensions);
    }
}
