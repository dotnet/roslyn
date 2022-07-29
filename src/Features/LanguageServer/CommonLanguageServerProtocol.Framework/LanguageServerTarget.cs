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

public class LifeCycleManager<RequestContextType> : ILifeCycleManager where RequestContextType : IRequestContext
{
    private readonly LanguageServerTarget<RequestContextType> _target;

    public LifeCycleManager(LanguageServerTarget<RequestContextType> languageServerTarget)
    {
        _target = languageServerTarget;
    }

    public void Exit()
    {
        _target.Exit();
    }

    public void Shutdown()
    {
        _target.Shutdown();
    }
}

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

    /// <summary>
    /// This spins up the LSP and should be called at the bottom of the constructor of the non-abstract implementor.
    /// </summary>
    public virtual void Initialize()
    {
        GetRequestExecutionQueue();
        GetRequestDispatcher();
    }

    public abstract ILspServices GetLspServices();

    public virtual IRequestDispatcher<RequestContextType> GetRequestDispatcher()
    {
        if (_requestDispatcher is null)
        {
            var lspServices = GetLspServices();
            _requestDispatcher = new RequestDispatcher<RequestContextType>(lspServices);

            SetupRequestDispatcher(_requestDispatcher);
        }
        return _requestDispatcher;
    }

    protected virtual void SetupRequestDispatcher(IRequestDispatcher<RequestContextType> requestDispatcher)
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

        foreach (var metadata in requestDispatcher.GetRegisteredMethods())
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

    public virtual RequestExecutionQueue<RequestContextType> GetRequestExecutionQueue()
    {
        if (_queue is null)
        {
            _queue = new RequestExecutionQueue<RequestContextType>(_serverKind, GetLspServices(), _logger);
            _queue.RequestServerShutdown += RequestExecutionQueue_Errored;
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

    public virtual void Shutdown()
    {
        if (_shuttingDown is true)
        {
            throw new InvalidOperationException("Shutdown has already been called.");
        }

        _shuttingDown = true;

        ShutdownRequestQueue();
    }

    public virtual void Exit()
    {
        try
        {
            ShutdownRequestQueue();

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

    protected void ShutdownRequestQueue()
    {
        _queue?.Shutdown();
    }

    public abstract Task OnErroredEndAsync(object obj);

    protected virtual void RequestExecutionQueueErroredInternal(string message)
    {
    }

    private void RequestExecutionQueue_Errored(object? sender, RequestShutdownEventArgs e)
    {
        // log message and shut down
        _logger?.TraceWarning($"Request queue is requesting shutdown due to error: {e.Message}");

        RequestExecutionQueueErroredInternal(e.Message);

        Shutdown();
        Exit();
    }

    private enum MessageType
    {
        Error = 1,
        Warning = 2,
        Info = 3,
        Log = 4,
    }

#nullable disable
    [DataContract]
    private class LogMessageParams
    {
        [DataMember(Name = "type")]
        public MessageType MessageType;
        [DataMember(Name = "message")]
        public string Message;
    }
#nullable enable

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

        Shutdown();
        Exit();
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

        internal void ShutdownServer() => _server.Shutdown();

        internal void ExitServer() => throw new NotImplementedException(); // _server.ExitImpl();
    }
}
