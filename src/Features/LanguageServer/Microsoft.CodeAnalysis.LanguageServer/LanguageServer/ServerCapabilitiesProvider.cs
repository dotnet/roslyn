// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.LanguageServer;

internal sealed class ServerCapabilitiesProvider : ICapabilitiesProvider
{
    private readonly ExperimentalCapabilitiesProvider _roslynCapabilities;

    public ServerCapabilitiesProvider(ExperimentalCapabilitiesProvider roslynCapabilities)
    {
        _roslynCapabilities = roslynCapabilities;
    }

    public ServerCapabilities GetCapabilities(ClientCapabilities clientCapabilities)
    {
        var roslynCapabilities = _roslynCapabilities.GetCapabilities(clientCapabilities);
        return roslynCapabilities;
    }
}
