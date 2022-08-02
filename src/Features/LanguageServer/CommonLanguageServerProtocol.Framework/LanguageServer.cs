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

public abstract class LanguageServer<RequestContextType> : ILanguageServer
{
    protected readonly ILspLogger _logger;

    protected readonly string _serverKind;

    public bool IsInitialized { get; private set; } = false;

    public bool HasShutdownStarted => _shuttingDown;

    protected LanguageServer(
        JsonRpc jsonRpc,
        ILspLogger logger,
        string serverKind)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// This spins up the LSP and should be called at the bottom of the constructor of the non-abstract implementor.
    /// </summary>
    public virtual void Initialize()
    {
        GetRequestExecutionQueue();
        GetRequestDispatcher();
    }

    protected abstract ILspServices GetLspServices();

    protected virtual IRequestDispatcher<RequestContextType> ConstructDispatcher()
    {
        throw new NotImplementedException();
    }

    protected IRequestDispatcher<RequestContextType> GetRequestDispatcher()
    {
        throw new NotImplementedException();
    }

    protected virtual void SetupRequestDispatcher(IRequestDispatcher<RequestContextType> requestDispatcher)
    {
        throw new NotImplementedException();
    }

    public virtual void OnInitialized()
    {
        throw new NotImplementedException();
    }

    protected virtual IRequestExecutionQueue<RequestContextType> ConstructRequestExecutionQueue()
    {
        throw new NotImplementedException();
    }

    protected IRequestExecutionQueue<RequestContextType> GetRequestExecutionQueue()
    {
        throw new NotImplementedException();
    }

    public virtual void Shutdown()
    {
        throw new NotImplementedException();
    }

    public virtual void Exit()
    {
        throw new NotImplementedException();
    }

    protected void ShutdownRequestQueue()
    {
        throw new NotImplementedException();
    }

    protected virtual void RequestExecutionQueueErroredInternal(string message)
    {
    }

    public virtual async ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}
