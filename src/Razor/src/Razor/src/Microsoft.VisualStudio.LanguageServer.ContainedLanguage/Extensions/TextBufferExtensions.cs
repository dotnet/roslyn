// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.Extensions;

internal static class TextBufferExtensions
{
    private const string HostDocumentVersionMarked = "__MsLsp_HostDocumentVersionMarker__";

    public static void SetHostDocumentSyncVersion(this ITextBuffer textBuffer, long hostDocumentVersion)
    {
        if (textBuffer is null)
        {
            throw new ArgumentNullException(nameof(textBuffer));
        }

        textBuffer.Properties[HostDocumentVersionMarked] = hostDocumentVersion;
    }

    public static bool TryGetHostDocumentSyncVersion(this ITextBuffer textBuffer, out long hostDocumentVersion)
    {
        if (textBuffer is null)
        {
            throw new ArgumentNullException(nameof(textBuffer));
        }

        var result = textBuffer.Properties.TryGetProperty(HostDocumentVersionMarked, out hostDocumentVersion);

        return result;
    }
}
