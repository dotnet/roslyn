// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Roslyn.LanguageServer.Protocol;

internal static class ClientCapabilitiesExtensions
{
    public static MarkupKind GetMarkupKind(this ClientCapabilities clientCapabilities)
    {
        // If MarkDown is supported, we'll use that.
        if (clientCapabilities.TextDocument?.Hover?.ContentFormat is MarkupKind[] contentFormat &&
            Array.IndexOf(contentFormat, MarkupKind.Markdown) >= 0)
        {
            return MarkupKind.Markdown;
        }

        return MarkupKind.PlainText;
    }

    public static bool SupportsMarkdown(this ClientCapabilities clientCapabilities)
        => clientCapabilities.GetMarkupKind() == MarkupKind.Markdown;

    public static bool SupportsVisualStudioExtensions(this ClientCapabilities clientCapabilities)
        => clientCapabilities is VSInternalClientCapabilities { SupportsVisualStudioExtensions: true };

    public static bool SupportsAnyCompletionListData(this ClientCapabilities clientCapabilities)
        => clientCapabilities.SupportsCompletionListData() ||
           clientCapabilities.SupportsCompletionListItemDefaultsData();

    public static bool SupportsCompletionListData(this ClientCapabilities clientCapabilities)
        => clientCapabilities.SupportsVisualStudioExtensions() &&
           clientCapabilities.TextDocument?.Completion is VSInternalCompletionSetting { CompletionList.Data: true };

    public static bool SupportsCompletionListItemDefaultsData(this ClientCapabilities clientCapabilities)
        => clientCapabilities.TextDocument?.Completion?.CompletionListSetting?.ItemDefaults is { } defaults &&
           Array.IndexOf(defaults, "data") >= 0;
}
