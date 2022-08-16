// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace CommonLanguageServerProtocol.Framework;

public abstract class AbstractLanguageServer<RequestContextType> : IAsyncDisposable
{
    protected readonly ILspLogger _logger;

    public bool IsInitialized { get; private set; }

    public bool HasShutdownStarted => _shuttingDown;

    protected AbstractLanguageServer(
        JsonRpc jsonRpc,
        ILspLogger logger)
    {
    }

    /// <summary>
    /// This spins up the LSP and should be called when you're ready for your server to start receiving requests.
    /// </summary>
    public virtual Task InitializeAsync()
    {
    }

    protected abstract ILspServices ConstructLspServices();

    protected ILspServices GetLspServices()
    {
    }

    protected virtual IHandlerProvider GetHandlerProvider()
    {
    }

    protected virtual void SetupRequestDispatcher(IHandlerProvider handlerProvider)
    {
    }

    public virtual void OnInitialized()
    {
    }

    protected virtual IRequestExecutionQueue<RequestContextType> ConstructRequestExecutionQueue()
    {
    }

    protected IRequestExecutionQueue<RequestContextType> GetRequestExecutionQueue()
    {
    }

    public virtual async Task ShutdownAsync()
    {
    }

    public virtual async Task ExitAsync()
    {
    }

    protected virtual void RequestExecutionQueueErroredInternal(string message)
    {
    }

    /// <summary>
    /// Disposes the LanguageServer, clearing and shutting down the queue and exiting.
    /// Can be called if the Server needs to be shut down outside of 'shutdown' and 'exit' requests.
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
    }
}
