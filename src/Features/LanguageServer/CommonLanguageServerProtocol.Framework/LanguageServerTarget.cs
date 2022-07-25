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
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace CommonLanguageServerProtocol.Framework;

public interface ILspServiceProvider
{
    ILspServices CreateServices(string serverKind);
}

public interface ILspServices : IDisposable
{
    T GetRequiredService<T>();
}

public interface IRequestExecutionQueue<RequestContextType> where RequestContextType : struct
{
    Task<RequestContextType?> CreateRequestContextAsync(IQueueItem<RequestContextType> queueItem, CancellationToken cancellationToken);

    event EventHandler<RequestShutdownEventArgs>? RequestServerShutdown;

    Task<TResponseType> ExecuteAsync<TRequestType, TResponseType>(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        IRequestHandler<TRequestType, TResponseType, RequestContextType> handler,
        TRequestType request,
        ClientCapabilities clientCapabilities,
        string methodName,
        CancellationToken cancellationToken);

    void Shutdown();
}

public interface IQueueItem<RequestContextType> where RequestContextType : struct
{
    /// <summary>
    /// Begins executing the work specified by this queue item.
    /// </summary>
    Task CallbackAsync(RequestContextType? context, CancellationToken cancellationToken);

    /// <inheritdoc cref="IRequestHandler{RequestContextType}.RequiresLSPSolution" />
    bool RequiresLSPSolution { get; }

    /// <inheritdoc cref="IRequestHandler{RequestContextType}.MutatesSolutionState" />
    bool MutatesSolutionState { get; }

    string MethodName { get; }

    /// <summary>
    /// The document identifier that will be used to find the solution and document for this request. This comes from the <see cref="TextDocumentIdentifier"/> returned from the handler itself via a call to <see cref="IRequestHandler{RequestType, ResponseType, TResponseContextType}.GetTextDocumentIdentifier(RequestType)"/>.
    /// </summary>
    TextDocumentIdentifier? TextDocument { get; }

    /// <inheritdoc cref="RequestContext.ClientCapabilities" />
    ClientCapabilities ClientCapabilities { get; }

    /// <summary>
    /// <see cref="CorrelationManager.ActivityId"/> used to properly correlate this work with the loghub
    /// tracing/logging subsystem.
    /// </summary>
    Guid ActivityId { get; }

    IRequestMetrics Metrics { get; }
}

public interface IRequestMetrics
{
    void RecordCancellation();
    void RecordExecutionStart();
    void RecordFailure();
    void RecordSuccess();
}

public record RequestHandlerMetadata(string MethodName, Type RequestType, Type ResponseType);

public interface IRequestDispatcher<RequestContextType> where RequestContextType : struct
{
    ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods();

    Task<TResponseType?> ExecuteRequestAsync<TRequestType, TResponseType>(
        string methodName,
        TRequestType request,
        ClientCapabilities clientCapabilities,
        IRequestExecutionQueue<RequestContextType> queue,
        CancellationToken cancellationToken);
}

public abstract class LanguageServerTarget<RequestContextType> : ILanguageServer where RequestContextType : struct
{
    private readonly ICapabilitiesProvider _capabilitiesProvider;

    protected readonly JsonRpc _jsonRpc;
    private readonly IRequestDispatcher<RequestContextType> _requestDispatcher;
    private readonly IRequestExecutionQueue<RequestContextType> _queue;
    protected readonly ILspServices _lspServices;
    protected readonly ILspLogger _logger;

    // Set on first LSP initialize request.
    protected readonly IClientCapabilitiesProvider _clientCapabilitiesProvider;

    // Fields used during shutdown.
    private bool _shuttingDown;
    private Task? _errorShutdownTask;

    public bool HasShutdownStarted => _shuttingDown;

    public abstract InitializeParams ClientSettings { get; }

    public event EventHandler<bool>? Shutdown;

    public event EventHandler? Exit;

    protected LanguageServerTarget(
        ILspServiceProvider lspServiceProvider,
        JsonRpc jsonRpc,
        ICapabilitiesProvider capabilitiesProvider,
        ILspLogger logger,
        ImmutableArray<string> supportedLanguages,
        string serverKind,
        IClientCapabilitiesProvider clientCapabilitiesProvider)
    {
        _clientCapabilitiesProvider = clientCapabilitiesProvider;
        _capabilitiesProvider = capabilitiesProvider;
        _logger = logger;

        _jsonRpc = jsonRpc;
        _jsonRpc.AddLocalRpcTarget(this);
        _jsonRpc.Disconnected += JsonRpc_Disconnected;

        _lspServices = GetLspServiceProvider(lspServiceProvider, serverKind);

        _queue = GetRequestExecutionQueue(supportedLanguages, serverKind, _lspServices);
        _queue.RequestServerShutdown += RequestExecutionQueue_Errored;

        _requestDispatcher = GetRequestDispatcher();

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

    public abstract ILspServices GetLspServiceProvider(
        ILspServiceProvider lspServiceProvider,
        string serverKind);

    public abstract IRequestDispatcher<RequestContextType> GetRequestDispatcher();

    public abstract IRequestExecutionQueue<RequestContextType> GetRequestExecutionQueue(
        ImmutableArray<string> supportedlanguages,
        string serverKind,
        ILspServices lspServices);

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
            var clientCapabilities = _target._clientCapabilitiesProvider.GetClientCapabilities();
            if (clientCapabilities is null)
            {
                throw new InvalidOperationException($"{nameof(InitializeAsync)} has not been called.");
            }

            var result = await _target._requestDispatcher.ExecuteRequestAsync<TRequestType, TResponseType>(
                _method,
                requestType,
                clientCapabilities,
                _target._queue,
                cancellationToken).ConfigureAwait(false);
            return result;
        }
    }

    /// <summary>
    /// Handle the LSP initialize request by storing the client capabilities and responding with the server
    /// capabilities.  The specification assures that the initialize request is sent only once.
    /// </summary>
    [JsonRpcMethod(Methods.InitializeName, UseSingleObjectParameterDeserialization = true)]
    public Task<InitializeResult> InitializeAsync(InitializeParams initializeParams, CancellationToken cancellationToken)
    {
        try
        {
            _logger?.TraceStart("Initialize");

            _clientCapabilitiesProvider.SetClientCapabilities(initializeParams.Capabilities);

            return Task.FromResult(new InitializeResult
            {
                Capabilities = _capabilitiesProvider.GetCapabilities(initializeParams.Capabilities),
            });
        }
        finally
        {
            _logger?.TraceStop("Initialize");
        }
    }

    [JsonRpcMethod(Methods.InitializedName)]
    public virtual Task InitializedAsync(CancellationToken cancellationToken)
    {
        var clientCapabilities = _clientCapabilitiesProvider.GetClientCapabilities();
        if (clientCapabilities is null)
        {
            throw new InvalidOperationException(nameof(clientCapabilities));
        }

        return Task.CompletedTask;
    }

    [JsonRpcMethod(Methods.ShutdownName)]
    public Task ShutdownAsync(CancellationToken _)
    {
        try
        {
            _logger?.TraceStart("Shutdown");

            ShutdownImpl();

            return Task.CompletedTask;
        }
        finally
        {
            _logger?.TraceStop("Shutdown");
        }
    }

    private void ShutdownImpl()
    {
        if (_shuttingDown is true)
        {
            throw new InvalidOperationException("Shutdown has already been called.");
        }

        _shuttingDown = true;

        ShutdownRequestQueue();
    }

    [JsonRpcMethod(Methods.ExitName)]
    public async Task ExitAsync(CancellationToken _)
    {
        try
        {
            _logger?.TraceStart("Exit");

            await OnExitAsync();
        }
        finally
        {
            _logger?.TraceStop("Exit");
        }
    }

    private Task OnExitAsync()
    {
        try
        {
            ShutdownRequestQueue();

            _lspServices.Dispose();

            _jsonRpc.Disconnected -= JsonRpc_Disconnected;
            _jsonRpc.Dispose();
        }
        catch (Exception e)
        {
            // Swallow exceptions thrown by disposing our JsonRpc object. Disconnected events can potentially throw their own exceptions so
            // we purposefully ignore all of those exceptions in an effort to shutdown gracefully.
        }

        return OnExitInternal();
    }

    protected abstract Task OnExitInternal();

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

    protected void ShutdownRequestQueue()
    {
        _queue.RequestServerShutdown -= RequestExecutionQueue_Errored;
        // if the queue requested shutdown via its event, it will have already shut itself down, but this
        // won't cause any problems calling it again
        _queue?.Shutdown();
    }

    private void RequestExecutionQueue_Errored(object? sender, RequestShutdownEventArgs e)
    {
        // log message and shut down
        _logger?.TraceWarning($"Request queue is requesting shutdown due to error: {e.Message}");

        var message = new LogMessageParams()
        {
            MessageType = MessageType.Error,
            Message = e.Message
        };

        OnErroredStart();
        _errorShutdownTask = Task.Run(async () =>
        {
            _logger?.TraceInformation("Shutting down language server.");

            await _jsonRpc.NotifyWithParameterObjectAsync(Methods.WindowLogMessageName, message).ConfigureAwait(false);

            ShutdownImpl();
            await OnExitAsync();
        }).ContinueWith(OnErroredEndAsync);
    }

    public abstract void OnErroredStart();

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

    public async ValueTask DisposeAsync()
    {
        // if the server shut down due to error, we might not have finished cleaning up
        if (_errorShutdownTask is not null)
            await _errorShutdownTask.ConfigureAwait(false);

        if (_logger is IDisposable disposableLogger)
            disposableLogger.Dispose();
    }

    //internal TestAccessor GetTestAccessor() => new(this);

    public object GetService(Type type)
    {
        throw new NotImplementedException();
    }

    //internal readonly struct TestAccessor
    //{
    //    private readonly LanguageServerTarget<RequestContextType> _server;

    //    internal TestAccessor(LanguageServerTarget<RequestContextType> server)
    //    {
    //        _server = server;
    //    }

    //    public T GetRequiredLspService<T>() where T : class, ILspService => _server._lspServices.GetRequiredService<T>();

    //    internal RequestExecutionQueue<RequestContextType>.TestAccessor GetQueueAccessor()
    //        => _server._queue!.GetTestAccessor();

    //    internal JsonRpc GetServerRpc() => _server._jsonRpc;

    //    internal bool HasShutdownStarted() => _server.HasShutdownStarted;

    //    internal void ShutdownServer() => _server.ShutdownImpl();

    //    internal void ExitServer() => _server.ExitImpl();
    //}
}
