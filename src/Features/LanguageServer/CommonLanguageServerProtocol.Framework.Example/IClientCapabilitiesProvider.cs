// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommonLanguageServerProtocol.Framework.Example
{
    using Microsoft.VisualStudio.LanguageServer.Protocol;

    internal interface IClientCapabilitiesProvider
    {
        ServerCapabilities GetServerCapabilities();
        void SetClientCapabilities(ClientCapabilities capabilities);
    }
}
