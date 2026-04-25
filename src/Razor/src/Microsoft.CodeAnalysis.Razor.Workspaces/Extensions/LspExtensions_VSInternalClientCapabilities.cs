// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Roslyn.LanguageServer.Protocol;

internal static partial class LspExtensions
{
    internal static VSInternalClientCapabilities ToVSInternalClientCapabilities(this ClientCapabilities clientCapabilities)
    {
        if (clientCapabilities is VSInternalClientCapabilities vsInternalClientCapabilities)
        {
            return vsInternalClientCapabilities;
        }

        return new VSInternalClientCapabilities()
        {
            TextDocument = clientCapabilities.TextDocument,
            SupportsVisualStudioExtensions = false,
            Experimental = clientCapabilities.Experimental,
            Workspace = clientCapabilities.Workspace,
        };
    }
}
