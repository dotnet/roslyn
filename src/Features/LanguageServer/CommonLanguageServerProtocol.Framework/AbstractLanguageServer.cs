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
    private readonly JsonRpc _jsonRpc;
    //private IRequestDispatcher<RequestContextType>? _requestDispatcher;
    private IRequestExecutionQueue<RequestContextType>? _queue;
    protected readonly ILspLogger _logger;
    private ILspServices? _lspServices;

    public bool IsInitialized { get; private set; }

    // Fields used during shutdown.
    private bool _shuttingDown;

    public bool HasShutdownStarted => _shuttingDown;

    protected AbstractLanguageServer(
        JsonRpc jsonRpc,
        ILspLogger logger)
    {
        _logger = logger;

        _jsonRpc = jsonRpc;
        _jsonRpc.AddLocalRpcTarget(this);
        _jsonRpc.Disconnected += JsonRpc_Disconnected;
    }

    /// <summary>
    /// This spins up the LSP and should be called when you're ready for your server to start receiving requests.
    /// </summary>
    public virtual Task InitializeAsync()
    {
        GetRequestExecutionQueue();

        return Task.CompletedTask;
    }

    protected abstract ILspServices ConstructLspServices();

    protected ILspServices GetLspServices()
    {
        if (_lspServices is null)
        {
            _lspServices = ConstructLspServices();
        }

        return _lspServices;
    }

    protected virtual IHandlerProvider GetHandlerProvider()
    {
        var handlerProvider = new HandlerProvider(lspServices);

        return handlerProvider;
    }

    //protected virtual IRequestDispatcher<RequestContextType> ConstructDispatcher()
    //{
    //    var lspServices = GetLspServices();
    //    var dispatcher = new RequestDispatcher<RequestContextType>(lspServices);
    //    SetupRequestDispatcher(dispatcher);

    //    return dispatcher;
    //}

    //protected IRequestDispatcher<RequestContextType> GetRequestDispatcher()
    //{
    //    if (_requestDispatcher is null)
    //    {
    //        _requestDispatcher = ConstructDispatcher();
    //    }

    //    return _requestDispatcher;
    //}

    protected virtual void SetupRequestDispatcher(IHandlerProvider handlerProvider)
    {
        var entryPointMethod = typeof(DelegatingEntryPoint).GetMethod(nameof(DelegatingEntryPoint.EntryPointAsync));
        if (entryPointMethod is null)
        {
            throw new InvalidOperationException($"{typeof(DelegatingEntryPoint).FullName} is missing method {nameof(DelegatingEntryPoint.EntryPointAsync)}");
        }
        var notificationMethod = typeof(DelegatingEntryPoint).GetMethod(nameof(DelegatingEntryPoint.NotificationEntryPointAsync));
        if (notificationMethod is null)
        {
            throw new InvalidOperationException($"{typeof(DelegatingEntryPoint).FullName} is missing method {nameof(DelegatingEntryPoint.NotificationEntryPointAsync)}");
        }

        var parameterlessNotificationMethod = typeof(DelegatingEntryPoint).GetMethod(nameof(DelegatingEntryPoint.ParameterlessNotificationEntryPointAsync));
        if (parameterlessNotificationMethod is null)
        {
            throw new InvalidOperationException($"{typeof(DelegatingEntryPoint).FullName} is missing method {nameof(DelegatingEntryPoint.ParameterlessNotificationEntryPointAsync)}");
        }

        foreach (var metadata in handlerProvider.GetRegisteredMethods())
        {
            // Instead of concretely defining methods for each LSP method, we instead dynamically construct the
            // generic method info from the exported handler types.  This allows us to define multiple handlers for
            // the same method but different type parameters.  This is a key functionality to support TS external
            // access as we do not want to couple our LSP protocol version dll to theirs.
            //
            // We also do not use the StreamJsonRpc support for JToken as the rpc method parameters because we want
            // StreamJsonRpc to do the deserialization to handle streaming requests using IProgress<T>.
            var delegatingEntryPoint = new DelegatingEntryPoint(metadata.MethodName, this);

            MethodInfo genericEntryPointMethod;
            if (metadata.RequestType is not null && metadata.ResponseType is not null)
            {
                genericEntryPointMethod = entryPointMethod.MakeGenericMethod(metadata.RequestType, metadata.ResponseType);
            }
            else if (metadata.RequestType is not null && metadata.ResponseType is null)
            {
                genericEntryPointMethod = notificationMethod.MakeGenericMethod(metadata.RequestType);
            }
            else if (metadata.RequestType is null && metadata.ResponseType is null)
            {
                // No need to genericize
                genericEntryPointMethod = parameterlessNotificationMethod;
            }
            else
            {
                throw new NotImplementedException($"An unrecognized {nameof(RequestHandlerMetadata)} situation has occured");
            }

            _jsonRpc.AddLocalRpcMethod(genericEntryPointMethod, delegatingEntryPoint, new JsonRpcMethodAttribute(metadata.MethodName) { UseSingleObjectParameterDeserialization = true });
        }
    }

    public virtual void OnInitialized()
    {
        IsInitialized = true;
    }

    protected virtual IRequestExecutionQueue<RequestContextType> ConstructRequestExecutionQueue()
    {
        var lspServices = GetLspServices();

        var handlerProvider = GetRequestDispatcher();
        var queue = new RequestExecutionQueue<RequestContextType>(_logger, handlerProvider);
        queue.RequestServerShutdown += RequestExecutionQueue_Errored;

        queue.Start(lspServices);

        return queue;
    }

    protected IRequestExecutionQueue<RequestContextType> GetRequestExecutionQueue()
    {
        if (_queue is null)
        {
            _queue = ConstructRequestExecutionQueue();
        }

        return _queue;
    }

    /// <summary>
    /// Wrapper class to hold the method and properties from the <see cref="AbstractLanguageServer{RequestContextType}"/>
    /// that the method info passed to streamjsonrpc is created from.
    /// </summary>
    private class DelegatingEntryPoint
    {
        private readonly string _method;
        private readonly AbstractLanguageServer<RequestContextType> _target;

        public DelegatingEntryPoint(string method, AbstractLanguageServer<RequestContextType> target)
        {
            _method = method;
            _target = target;
        }

        public async Task NotificationEntryPointAsync<TRequestType>(TRequestType requestType, CancellationToken cancellationToken) where TRequestType : class
        {
            CheckServerState();
            var queue = _target.GetRequestExecutionQueue();

            var requestDispatcher = _target.GetRequestDispatcher();
            await requestDispatcher.ExecuteNotificationAsync(
                _method,
                requestType,
                queue,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task ParameterlessNotificationEntryPointAsync(CancellationToken cancellationToken)
        {
            CheckServerState();
            var queue = _target.GetRequestExecutionQueue();

            var requestDispatcher = _target.GetRequestDispatcher();
            await requestDispatcher.ExecuteNotificationAsync(
                _method,
                queue,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<TResponseType?> EntryPointAsync<TRequestType, TResponseType>(TRequestType requestType, CancellationToken cancellationToken) where TRequestType : class
        {
            CheckServerState();
            var queue = _target.GetRequestExecutionQueue();

            var requestDispatcher = _target.GetRequestDispatcher();
            var result = await requestDispatcher.ExecuteRequestAsync<TRequestType, TResponseType>(
                _method,
                requestType,
                queue,
                cancellationToken).ConfigureAwait(false);
            return result;
        }

        private void CheckServerState()
        {
            if (_target.IsInitialized)
            {
                throw new InvalidOperationException($"'initialize' has not been called.");
            }
        }
    }

    public virtual async Task ShutdownAsync()
    {
        if (_shuttingDown is true)
        {
            throw new InvalidOperationException("Shutdown has already been called.");
        }

        _shuttingDown = true;

        await ShutdownRequestExecutionQueue();
    }

    public virtual async Task ExitAsync()
    {
        try
        {
            await ShutdownRequestExecutionQueue();

            var lspServices = GetLspServices();
            lspServices.Dispose();

            _jsonRpc.Disconnected -= JsonRpc_Disconnected;
            _jsonRpc.Dispose();
        }
        catch (Exception e)
        {
            // Swallow exceptions thrown by disposing our JsonRpc object. Disconnected events can potentially throw their own exceptions so
            // we purposefully ignore all of those exceptions in an effort to shutdown gracefully.
        }
    }

    private ValueTask ShutdownRequestExecutionQueue()
    {
        var queue = GetRequestExecutionQueue();
        return queue.DisposeAsync();
    }

    protected virtual void RequestExecutionQueueErroredInternal(string message)
    {
    }

    private void RequestExecutionQueue_Errored(object? sender, RequestShutdownEventArgs e)
    {
        // log message and shut down
        _logger?.LogWarningAsync($"Request queue is requesting shutdown due to error: {e.Message}");

        RequestExecutionQueueErroredInternal(e.Message);

        ShutdownAsync();
        ExitAsync();
    }

    /// <summary>
    /// Cleanup the server if we encounter a json rpc disconnect so that we can be restarted later.
    /// </summary>
    private void JsonRpc_Disconnected(object? sender, JsonRpcDisconnectedEventArgs e)
    {
        if (_shuttingDown)
        {
            // We're already in the normal shutdown -> exit path, no need to do anything.
            return;
        }

        _ = _logger?.LogWarningAsync($"Encountered unexpected jsonrpc disconnect, Reason={e.Reason}, Description={e.Description}, Exception={e.Exception}");

        ShutdownAsync();
        ExitAsync();
    }

    /// <summary>
    /// Disposes the LanguageServer, clearing and shutting down the queue and exiting.
    /// Can be called if the Server needs to be shut down outside of 'shutdown' and 'exit' requests.
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
        if (_logger is IDisposable disposableLogger)
        {
            disposableLogger.Dispose();
        }

        ShutdownAsync();
        ExitAsync();
    }

    internal TestAccessor GetTestAccessor()
    {
        return new(this);
    }

    internal readonly struct TestAccessor
    {
        private readonly AbstractLanguageServer<RequestContextType> _server;

        internal TestAccessor(AbstractLanguageServer<RequestContextType> server)
        {
            _server = server;
        }

        public T GetRequiredLspService<T>() where T : class => _server.GetLspServices().GetRequiredService<T>();

        internal RequestExecutionQueue<RequestContextType>.TestAccessor? GetQueueAccessor()
        {
            if (_server._queue is RequestExecutionQueue<RequestContextType> requestExecution)
            {
                return requestExecution.GetTestAccessor();
            }

            return null;
        }

        internal JsonRpc GetServerRpc() => _server._jsonRpc;

        internal bool HasShutdownStarted() => _server.HasShutdownStarted;

        internal Task ShutdownServer()
        {
            return _server.ShutdownAsync();
        }

        internal Task ExitServer()
        {
            return _server.ExitAsync();
        }
    }
}
