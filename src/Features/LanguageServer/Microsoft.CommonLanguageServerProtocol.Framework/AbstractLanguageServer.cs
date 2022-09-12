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

namespace Microsoft.CommonLanguageServerProtocol.Framework;

public abstract class AbstractLanguageServer<TRequestContext> : ILifeCycleManager, IAsyncDisposable
{
    private readonly JsonRpc _jsonRpc;
    private IRequestExecutionQueue<TRequestContext>? _queue;
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
    /// Initializes the LanguageServer.
    /// </summary>
    /// <remarks>Should be called at the bottom of the implementing constructor or immedietly after construction.</remarks>
    public void Initialize()
    {
        GetRequestExecutionQueue();
    }

    protected abstract ILspServices ConstructLspServices();

    protected ILspServices GetLspServices()
    {
        if (_lspServices is null)
            _lspServices = ConstructLspServices();

        return _lspServices;
    }

    protected virtual IHandlerProvider GetHandlerProvider()
    {
        var lspServices = GetLspServices();
        var handlerProvider = new HandlerProvider(lspServices);
        SetupRequestDispatcher(handlerProvider);

        return handlerProvider;
    }

    protected virtual void SetupRequestDispatcher(IHandlerProvider handlerProvider)
    {
        var entryPointMethod = typeof(DelegatingEntryPoint).GetMethod(nameof(DelegatingEntryPoint.EntryPointAsync));
        if (entryPointMethod is null)
            throw new InvalidOperationException($"{typeof(DelegatingEntryPoint).FullName} is missing method {nameof(DelegatingEntryPoint.EntryPointAsync)}");
        var notificationMethod = typeof(DelegatingEntryPoint).GetMethod(nameof(DelegatingEntryPoint.NotificationEntryPointAsync));
        if (notificationMethod is null)
            throw new InvalidOperationException($"{typeof(DelegatingEntryPoint).FullName} is missing method {nameof(DelegatingEntryPoint.NotificationEntryPointAsync)}");

        var parameterlessNotificationMethod = typeof(DelegatingEntryPoint).GetMethod(nameof(DelegatingEntryPoint.ParameterlessNotificationEntryPointAsync));
        if (parameterlessNotificationMethod is null)
            throw new InvalidOperationException($"{typeof(DelegatingEntryPoint).FullName} is missing method {nameof(DelegatingEntryPoint.ParameterlessNotificationEntryPointAsync)}");

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
            var methodAttribute = new JsonRpcMethodAttribute(metadata.MethodName)
            {
                UseSingleObjectParameterDeserialization = true,
            };
            _jsonRpc.AddLocalRpcMethod(genericEntryPointMethod, delegatingEntryPoint, methodAttribute);
        }
    }

    public virtual void OnInitialized()
    {
        IsInitialized = true;
    }

    protected virtual IRequestExecutionQueue<TRequestContext> ConstructRequestExecutionQueue()
    {
        var handlerProvider = GetHandlerProvider();
        var queue = new RequestExecutionQueue<TRequestContext>(_logger, handlerProvider);

        queue.Start();

        return queue;
    }

    protected IRequestExecutionQueue<TRequestContext> GetRequestExecutionQueue()
    {
        if (_queue is null)
            _queue = ConstructRequestExecutionQueue();

        return _queue;
    }

    /// <summary>
    /// Wrapper class to hold the method and properties from the <see cref="AbstractLanguageServer{RequestContextType}"/>
    /// that the method info passed to streamjsonrpc is created from.
    /// </summary>
    private class DelegatingEntryPoint
    {
        private readonly string _method;
        private readonly AbstractLanguageServer<TRequestContext> _target;

        public DelegatingEntryPoint(string method, AbstractLanguageServer<TRequestContext> target)
        {
            _method = method;
            _target = target;
        }

        public async Task NotificationEntryPointAsync<TRequest>(TRequest request, CancellationToken cancellationToken) where TRequest : class
        {
            CheckServerState();
            var queue = _target.GetRequestExecutionQueue();
            var lspServices = _target.GetLspServices();

            _ = await queue.ExecuteAsync<TRequest, VoidReturn>(request, _method, lspServices, cancellationToken).ConfigureAwait(false);
        }

        public async Task ParameterlessNotificationEntryPointAsync(CancellationToken cancellationToken)
        {
            CheckServerState();
            var queue = _target.GetRequestExecutionQueue();
            var lspServices = _target.GetLspServices();

            _ = await queue.ExecuteAsync<VoidReturn, VoidReturn>(VoidReturn.Instance, _method, lspServices, cancellationToken).ConfigureAwait(false);
        }

        public async Task<TResponse?> EntryPointAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken) where TRequest : class
        {
            CheckServerState();
            var queue = _target.GetRequestExecutionQueue();
            var lspServices = _target.GetLspServices();

            var result = await queue.ExecuteAsync<TRequest, TResponse>(request, _method, lspServices, cancellationToken).ConfigureAwait(false);

            return result;
        }

        private void CheckServerState()
        {
            if (_target.IsInitialized)
                throw new InvalidOperationException($"'initialize' has not been called.");
        }
    }

    public async Task ShutdownAsync(string message = "Shutting down")
    {
        _shuttingDown = true;
        _logger.LogInformation(message);

        await ShutdownRequestExecutionQueueAsync().ConfigureAwait(false);
    }

    public async Task ExitAsync()
    {
        try
        {
            await ShutdownRequestExecutionQueueAsync().ConfigureAwait(false);

            var lspServices = GetLspServices();
            lspServices.Dispose();

            _jsonRpc.Disconnected -= JsonRpc_Disconnected;
            _jsonRpc.Dispose();
        }
        catch (Exception)
        {
            // Swallow exceptions thrown by disposing our JsonRpc object. Disconnected events can potentially throw their own exceptions so
            // we purposefully ignore all of those exceptions in an effort to shutdown gracefully.
        }
        _logger.LogInformation("Exiting server");
    }

    private ValueTask ShutdownRequestExecutionQueueAsync()
    {
        var queue = GetRequestExecutionQueue();
        return queue.DisposeAsync();
    }

#pragma warning disable VSTHRD100
    /// <summary>
    /// Cleanup the server if we encounter a json rpc disconnect so that we can be restarted later.
    /// </summary>
    private async void JsonRpc_Disconnected(object? sender, JsonRpcDisconnectedEventArgs e)
    {
        if (_shuttingDown)
        {
            // We're already in the normal shutdown -> exit path, no need to do anything.
            return;
        }

        var message = $"Encountered unexpected jsonrpc disconnect, Reason={e.Reason}, Description={e.Description}, Exception={e.Exception}";
        _logger.LogWarning(message);

        var lspServices = GetLspServices();
        var lifeCycleManager = lspServices.GetRequiredService<ILifeCycleManager>();

        await lifeCycleManager.ShutdownAsync(message).ConfigureAwait(false);
        await lifeCycleManager.ExitAsync().ConfigureAwait(false);
    }
#pragma warning disable VSTHRD100

    /// <summary>
    /// Disposes the LanguageServer, clearing and shutting down the queue and exiting.
    /// Can be called if the Server needs to be shut down outside of 'shutdown' and 'exit' requests.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_logger is IDisposable disposableLogger)
            disposableLogger.Dispose();

        var lspServices = GetLspServices();
        var lifeCycleManager = lspServices.GetRequiredService<ILifeCycleManager>();

        await lifeCycleManager.ShutdownAsync("Disposing").ConfigureAwait(false);
        await lifeCycleManager.ExitAsync().ConfigureAwait(false);
    }

    internal TestAccessor GetTestAccessor()
    {
        return new(this);
    }

    internal readonly struct TestAccessor
    {
        private readonly AbstractLanguageServer<TRequestContext> _server;

        internal TestAccessor(AbstractLanguageServer<TRequestContext> server)
        {
            _server = server;
        }

        public T GetRequiredLspService<T>() where T : class => _server.GetLspServices().GetRequiredService<T>();

        internal RequestExecutionQueue<TRequestContext>.TestAccessor? GetQueueAccessor()
        {
            if (_server._queue is RequestExecutionQueue<TRequestContext> requestExecution)
                return requestExecution.GetTestAccessor();

            return null;
        }

        internal JsonRpc GetServerRpc() => _server._jsonRpc;

        internal bool HasShutdownStarted() => _server.HasShutdownStarted;

        internal Task ShutdownServerAsync()
        {
            return _server.ShutdownAsync();
        }

        internal Task ExitServerAsync()
        {
            return _server.ExitAsync();
        }
    }
}
