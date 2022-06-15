// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace CommonLanguageServerProtocol.Framework;

public interface ILanguageServer : IAsyncDisposable
{
    event EventHandler<bool>? Shutdown;

    event EventHandler? Exit;

    InitializeParams ClientSettings { get; }

    /// <summary>
    /// Handle the LSP initialize request by storing the client capabilities and responding with the server
    /// capabilities.  The specification assures that the initialize request is sent only once.
    /// </summary>
    [JsonRpcMethod(Methods.InitializeName, UseSingleObjectParameterDeserialization = true)]
    Task<InitializeResult> InitializeAsync(InitializeParams initializeParams, CancellationToken cancellationToken);

    object GetService(Type type);
}
