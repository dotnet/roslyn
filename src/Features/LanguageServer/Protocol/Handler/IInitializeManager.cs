// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.LanguageServer.Protocol;

#nullable enable

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal interface IInitializeManager : ILspService
    {
        ClientCapabilities GetClientCapabilities();

        ClientCapabilities? TryGetClientCapabilities();

        InitializeParams? TryGetInitializeParams();

        void SetInitializeParams(InitializeParams initializeParams);
    }
}
