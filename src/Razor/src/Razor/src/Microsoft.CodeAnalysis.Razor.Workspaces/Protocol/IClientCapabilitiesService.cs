// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Protocol;

internal interface IClientCapabilitiesService
{
    /// <summary>
    /// Indicates whether capabilities have been sent by the client, and therefore where a call to ClientCapabilities would succeed
    /// </summary>
    bool CanGetClientCapabilities { get; }

    VSInternalClientCapabilities ClientCapabilities { get; }
}
