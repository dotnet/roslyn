// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace CommonLanguageServerProtocol.Framework;

public abstract class LanguageServerTarget<RequestContextType> : ILanguageServer where RequestContextType : IRequestContext
{
    private readonly JsonRpc _jsonRpc;
    private IRequestDispatcher<RequestContextType>? _requestDispatcher;
    protected readonly ILspLogger _logger;
    private RequestExecutionQueue<RequestContextType>? _queue;

    protected readonly string _serverKind;

    public bool IsInitialized { get; private set; } = false;

    // Fields used during shutdown.
    private bool _shuttingDown;

    public bool HasShutdownStarted => _shuttingDown;

    public event EventHandler<bool>? Shutdown;

    public event EventHandler? Exit;

    protected LanguageServerTarget(
        JsonRpc jsonRpc,
        ILspLogger logger,
        string serverKind)
    {
        _serverKind = serverKind;
        _logger = logger;

        _jsonRpc = jsonRpc;
        _jsonRpc.AddLocalRpcTarget(this);
        _jsonRpc.Disconnected += JsonRpc_Disconnected;
    }

    public abstract ILspServices GetLspServices();

    public virtual IRequestDispatcher<RequestContextType> GetRequestDispatcher()
    {
        if (_requestDispatcher is null)
        {
            var lspServices = GetLspServices();
            _requestDispatcher = new RequestDispatcher<RequestContextType>(lspServices);

            var entryPointMethod = typeof(DelegatingEntryPoint).GetMethod(nameof(DelegatingEntryPoint.EntryPointAsync));
            if (entryPointMethod is null)
            {
                throw new InvalidOperationException($"{typeof(DelegatingEntryPoint).FullName} is missing method {nameof(DelegatingEntryPoint.EntryPointAsync)}");
            }

            foreach (var metadata in _requestDispatcher.GetRegisteredMethods())
            {
                // Instead of concretely defining methods for each LSP method, we instead dynamically construct the
                // generic method info from the exported handler types.  This allows us to define multiple handlers for
                // the same method but different type parameters.  This is a key functionality to support TS external
                // access as we do not want to couple our LSP protocol version dll to theirs.
                //
                // We also do not use the StreamJsonRpc support for JToken as the rpc method parameters because we want
                // StreamJsonRpc to do the deserialization to handle streaming requests using IProgress<T>.
                var delegatingEntryPoint = new DelegatingEntryPoint(metadata.MethodName, this);

                var genericEntryPointMethod = entryPointMethod.MakeGenericMethod(metadata.RequestType, metadata.ResponseType);

                _jsonRpc.AddLocalRpcMethod(genericEntryPointMethod, delegatingEntryPoint, new JsonRpcMethodAttribute(metadata.MethodName) { UseSingleObjectParameterDeserialization = true });
            }
        }

        return _requestDispatcher;
    }

    public virtual void OnInitialized()
    {
        IsInitialized = true;
    }

    public RequestExecutionQueue<RequestContextType> GetRequestExecutionQueue()
    {
        if (_queue is null)
        {
            _queue = new RequestExecutionQueue<RequestContextType>(_serverKind, GetLspServices(), _logger);
            //  _queue.RequestServerShutdown += RequestExecutionQueue_Errored;
        }

        return _queue;
    }

    /// <summary>
    /// Wrapper class to hold the method and properties from the <see cref="LanguageServerTarget{RequestContextType}"/>
    /// that the method info passed to streamjsonrpc is created from.
    /// </summary>
    private class DelegatingEntryPoint
    {
        private readonly string _method;
        private readonly LanguageServerTarget<RequestContextType> _target;

        public DelegatingEntryPoint(string method, LanguageServerTarget<RequestContextType> target)
        {
            _method = method;
            _target = target;
        }

        public async Task<TResponseType?> EntryPointAsync<TRequestType, TResponseType>(TRequestType requestType, CancellationToken cancellationToken) where TRequestType : class
        {
            if (_target.IsInitialized)
            {
                throw new InvalidOperationException($"'initialize' has not been called.");
            }
            var queue = _target.GetRequestExecutionQueue();

            var requestDispatcher = _target.GetRequestDispatcher();
            var result = await requestDispatcher.ExecuteRequestAsync<TRequestType, TResponseType>(
                _method,
                requestType,
                queue,
                cancellationToken).ConfigureAwait(false);
            return result;
        }
    }

    /// <summary>
    /// Handle the LSP initialize request by storing the client capabilities and responding with the server
    /// capabilities.  The specification assures that the initialize request is sent only once.
    /// </summary>
    //[JsonRpcMethod(Methods.InitializeName, UseSingleObjectParameterDeserialization = true)]
    //public virtual Task<InitializeResult> InitializeAsync(InitializeParams initializeParams, CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        _logger?.TraceStart("Initialize");

    //        _clientSettings = initializeParams;

    //        return Task.FromResult(new InitializeResult
    //        {
    //            Capabilities = _capabilitiesProvider.GetCapabilities(initializeParams.Capabilities),
    //        });
    //    }
    //    finally
    //    {
    //        _logger?.TraceStop("Initialize");
    //    }
    //}

    //[JsonRpcMethod(Methods.InitializedName)]
    //public virtual Task InitializedAsync(CancellationToken cancellationToken)
    //{
    //    if (ClientSettings is null)
    //    {
    //        throw new InvalidOperationException(nameof(ClientSettings));
    //    }

    //    return Task.CompletedTask;
    //}

    //[JsonRpcMethod(Methods.ShutdownName)]
    //public Task ShutdownAsync(CancellationToken _)
    //{
    //    try
    //    {
    //        _logger?.TraceStart("Shutdown");

    //        ShutdownImpl();

    //        return Task.CompletedTask;
    //    }
    //    finally
    //    {
    //        _logger?.TraceStop("Shutdown");
    //    }
    //}

    protected virtual void ShutdownImpl()
    {
        if (_shuttingDown is true)
        {
            throw new InvalidOperationException("Shutdown has already been called.");
        }

        _shuttingDown = true;

        ShutdownRequestQueue();
    }

    //[JsonRpcMethod(Methods.ExitName)]
    //public async Task ExitAsync(CancellationToken _)
    //{
    //    try
    //    {
    //        _logger?.TraceStart("Exit");

    //        await OnExitAsync();
    //    }
    //    finally
    //    {
    //        _logger?.TraceStop("Exit");
    //    }
    //}

    protected Task OnExitAsync()
    {
        try
        {
            ShutdownRequestQueue();

            _jsonRpc.Disconnected -= JsonRpc_Disconnected;
            _jsonRpc.Dispose();
        }
        catch (Exception e)
        {
            // Swallow exceptions thrown by disposing our JsonRpc object. Disconnected events can potentially throw their own exceptions so
            // we purposefully ignore all of those exceptions in an effort to shutdown gracefully.
        }

        return Task.CompletedTask;
    }

    protected void ShutdownRequestQueue()
    {
        _queue?.Shutdown();
    }

    /// <summary>
    /// Specially handle the execute workspace command method as we have to deserialize the request
    /// to figure out which <see cref="AbstractExecuteWorkspaceCommandHandler"/> actually handles it.
    /// </summary>
    //[JsonRpcMethod(Methods.WorkspaceExecuteCommandName, UseSingleObjectParameterDeserialization = true)]
    //public async Task<object?> ExecuteWorkspaceCommandAsync(ExecuteCommandParams request, CancellationToken cancellationToken)
    //{
    //    Contract.ThrowIfNull(_clientCapabilitiesProvider, $"{nameof(InitializeAsync)} has not been called.");
    //    var requestMethod = AbstractExecuteWorkspaceCommandHandler.GetRequestNameForCommandName(request.Command);

    //    var result = await _requestDispatcher.ExecuteRequestAsync<LSP.ExecuteCommandParams, object>(
    //        requestMethod,
    //        request,
    //        _clientCapabilitiesProvider.GetClientCapabilities(),
    //        _queue,
    //        cancellationToken).ConfigureAwait(false);
    //    return result;
    //}

    public abstract Task OnErroredEndAsync(object obj);

    /// <summary>
    /// Cleanup the server if we encounter a json rpc disconnect so that we can be restarted later.
    /// </summary>
    public void JsonRpc_Disconnected(object? sender, JsonRpcDisconnectedEventArgs e)
    {
        if (_shuttingDown)
        {
            // We're already in the normal shutdown -> exit path, no need to do anything.
            return;
        }

        _logger?.TraceWarning($"Encountered unexpected jsonrpc disconnect, Reason={e.Reason}, Description={e.Description}, Exception={e.Exception}");

        ShutdownImpl();
        OnExitAsync();
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (_logger is IDisposable disposableLogger)
            disposableLogger.Dispose();
    }

    internal TestAccessor GetTestAccessor()
    {
        return new(this);
    }

    internal readonly struct TestAccessor
    {
        private readonly LanguageServerTarget<RequestContextType> _server;

        internal TestAccessor(LanguageServerTarget<RequestContextType> server)
        {
            _server = server;
        }

        public T GetRequiredLspService<T>() where T : class => _server.GetLspServices().GetRequiredService<T>();

        internal RequestExecutionQueue<RequestContextType>.TestAccessor GetQueueAccessor()
            => _server._queue!.GetTestAccessor();

        internal JsonRpc GetServerRpc() => _server._jsonRpc;

        internal bool HasShutdownStarted() => _server.HasShutdownStarted;

        internal void ShutdownServer() => _server.ShutdownImpl();

        internal void ExitServer() => throw new NotImplementedException(); // _server.ExitImpl();
    }
}
