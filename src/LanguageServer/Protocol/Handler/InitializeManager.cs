// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal class InitializeManager : IInitializeManager
{
    public InitializeManager()
    {
    }

    private InitializeParams? _initializeParams;

    public ClientCapabilities GetClientCapabilities()
    {
        if (_initializeParams?.Capabilities is null)
        {
            throw new InvalidOperationException($"Tried to get required {nameof(ClientCapabilities)} before it was set");
        }

        return _initializeParams.Capabilities;
    }

    public void SetInitializeParams(InitializeParams initializeParams)
    {
        Contract.ThrowIfFalse(_initializeParams == null);
        _initializeParams = initializeParams;
    }

    public InitializeParams? TryGetInitializeParams()
    {
        return _initializeParams;
    }

    public ClientCapabilities? TryGetClientCapabilities()
    {
        return _initializeParams?.Capabilities;
    }
}
