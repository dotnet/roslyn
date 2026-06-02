// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test;

internal class TestClientCapabilitiesService(VSInternalClientCapabilities clientCapabilities) : IClientCapabilitiesService
{
    public bool CanGetClientCapabilities => true;

    public VSInternalClientCapabilities ClientCapabilities => clientCapabilities;
}
