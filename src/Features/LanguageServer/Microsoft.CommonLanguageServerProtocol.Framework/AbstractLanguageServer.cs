// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

#if BINARY_COMPAT // TODO - Remove with https://github.com/dotnet/roslyn/issues/72251
public abstract class AbstractLanguageServer<TRequestContext>
#else
internal abstract class AbstractLanguageServer<TRequestContext>
#endif
{
    private readonly JsonRpc _jsonRpc;
#pragma warning disable IDE1006 // Naming Styles - Required for API compat, TODO - https://github.com/dotnet/roslyn/issues/72251
    protected readonly ILspLogger _logger;
#pragma warning restore IDE1006 // Naming Styles

    protected readonly JsonSerializer _jsonSerializer;

    /// <summary>
    /// These are lazy to allow implementations to define custom variables that are used by
    /// <see cref="ConstructRequestExecutionQueue"/> or <see cref="ConstructLspServices"/>
    /// </summary>
    private readonly Lazy<IRequestExecutionQueue<TRequestContext>> _queue;
    private readonly Lazy<ILspServices> _lspServices;

    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Ensures that we only run shutdown and exit code once in order.
    /// Guards access to <see cref="_shutdownRequestTask"/> and <see cref="_exitNotificationTask"/>
    /// </summary>
    private readonly object _lifeCycleLock = new();

    /// <summary>
    /// Task representing the work done on LSP server shutdown.
    /// </summary>
    private Task? _shutdownRequestTask;

    /// <summary>
    /// Task representing the work down on LSP exit.
    /// </summary>
    private Task? _exitNotificationTask;

    /// <summary>
    /// Task completion source that is started when the server starts and completes when the server exits.
    /// Used when callers need to wait for the server to cleanup.
    /// </summary>
    private readonly TaskCompletionSource<object?> _serverExitedSource = new();

    protected AbstractLanguageServer(
        JsonRpc jsonRpc,
        JsonSerializer jsonSerializer,
        ILspLogger logger)
    {
        _logger = logger;
        _jsonRpc = jsonRpc;
        _jsonSerializer = jsonSerializer;

        _jsonRpc.AddLocalRpcTarget(this);
        _jsonRpc.Disconnected += JsonRpc_Disconnected;
        _lspServices = new Lazy<ILspServices>(() => ConstructLspServices());
        _queue = new Lazy<IRequestExecutionQueue<TRequestContext>>(() => ConstructRequestExecutionQueue());
    }

    /// <summary>
    /// Initializes the LanguageServer.
    /// </summary>
    /// <remarks>Should be called at the bottom of the implementing constructor or immediately after construction.</remarks>
    public void Initialize()
    {
        GetRequestExecutionQueue();
    }

    /// <summary>
    /// Extension point to allow creation of <see cref="ILspServices"/> since that can't always be handled in the constructor.
    /// </summary>
    /// <returns>An <see cref="ILspServices"/> instance for this server.</returns>
    /// <remarks>This should only be called once, and then cached.</remarks>
    protected abstract ILspServices ConstructLspServices();

    [Obsolete($"Use {nameof(HandlerProvider)} property instead.", error: false)]
    protected virtual IHandlerProvider GetHandlerProvider()
    {
        var lspServices = _lspServices.Value;
        var handlerProvider = new HandlerProvider(lspServices);
        SetupRequestDispatcher(handlerProvider);

        return handlerProvider;
    }

    protected virtual AbstractHandlerProvider HandlerProvider
    {
        get
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var handlerProvider = GetHandlerProvider();
#pragma warning restore CS0618 // Type or member is obsolete
            if (handlerProvider is AbstractHandlerProvider abstractHandlerProvider)
            {
                return abstractHandlerProvider;
            }

            return new WrappedHandlerProvider(handlerProvider);
        }
    }

    public ILspServices GetLspServices() => _lspServices.Value;

    protected virtual void SetupRequestDispatcher(IHandlerProvider handlerProvider)
    {
        var entryPointMethodInfo = typeof(DelegatingEntryPoint).GetMethod(nameof(DelegatingEntryPoint.ExecuteRequestAsync))!;
        // Get unique set of methods from the handler provider for the default language.
        foreach (var methodGroup in handlerProvider
            .GetRegisteredMethods()
            .GroupBy(m => m.MethodName))
        {
            // Instead of concretely defining methods for each LSP method, we instead dynamically construct the
            // generic method info from the exported handler types.  This allows us to define multiple handlers for
            // the same method but different type parameters.  This is a key functionality to support LSP extensibility
            // in cases like XAML, TS to allow them to use different LSP type definitions

            // Verify that we are not mixing different numbers of request parameters and responses between different language handlers
            // e.g. it is not allowed to have a method have both a parameterless and regular parameter handler.
            var requestTypes = methodGroup.Select(m => m.RequestType);
            var responseTypes = methodGroup.Select(m => m.ResponseType);
            if (!AllTypesMatch(requestTypes))
            {
                throw new InvalidOperationException($"Language specific handlers for {methodGroup.Key} have mis-matched number of parameters:{Environment.NewLine}{string.Join(Environment.NewLine, methodGroup)}");
            }

            if (!AllTypesMatch(responseTypes))
            {
                throw new InvalidOperationException($"Language specific handlers for {methodGroup.Key} have mis-matched number of returns:{Environment.NewLine}{string.Join(Environment.NewLine, methodGroup)}");
            }

            var delegatingEntryPoint = new DelegatingEntryPoint(methodGroup.Key, this, methodGroup);
            var methodAttribute = new JsonRpcMethodAttribute(methodGroup.Key)
            {
                UseSingleObjectParameterDeserialization = true,
            };

            _jsonRpc.AddLocalRpcMethod(entryPointMethodInfo, delegatingEntryPoint, methodAttribute);
        }

        static bool AllTypesMatch(IEnumerable<Type?> types)
        {
            if (types.All(r => r is null))
            {
                return true;
            }

            if (types.All(r => r is not null))
            {
                return true;
            }

            return false;
        }
    }

    [JsonRpcMethod("shutdown")]
    public Task HandleShutdownRequestAsync(CancellationToken _) => ShutdownAsync();

    [JsonRpcMethod("exit")]
    public Task HandleExitNotificationAsync(CancellationToken _) => ExitAsync();

    public virtual void OnInitialized()
    {
        IsInitialized = true;
    }

    protected virtual IRequestExecutionQueue<TRequestContext> ConstructRequestExecutionQueue()
    {
        var handlerProvider = HandlerProvider;
        var queue = new RequestExecutionQueue<TRequestContext>(this, _logger, handlerProvider);

        queue.Start();

        return queue;
    }

    protected IRequestExecutionQueue<TRequestContext> GetRequestExecutionQueue()
    {
        return _queue.Value;
    }

    protected virtual string GetLanguageForRequest(string methodName, JToken? parameters)
    {
        _logger.LogInformation($"Using default language handler for {methodName}");
        return LanguageServerConstants.DefaultLanguageName;
    }

    private sealed class DelegatingEntryPoint
    {
        private readonly string _method;
        private readonly Lazy<FrozenDictionary<string, (MethodInfo MethodInfo, RequestHandlerMetadata Metadata)>> _languageEntryPoint;
        private readonly AbstractLanguageServer<TRequestContext> _target;

        private static readonly MethodInfo s_queueExecuteAsyncMethod = typeof(RequestExecutionQueue<TRequestContext>).GetMethod(nameof(RequestExecutionQueue<TRequestContext>.ExecuteAsync))!;

        public DelegatingEntryPoint(string method, AbstractLanguageServer<TRequestContext> target, IGrouping<string, RequestHandlerMetadata> handlersForMethod)
        {
            _method = method;
            _target = target;
            _languageEntryPoint = new Lazy<FrozenDictionary<string, (MethodInfo, RequestHandlerMetadata)>>(() =>
            {
                var handlerEntryPoints = new Dictionary<string, (MethodInfo, RequestHandlerMetadata)>();
                foreach (var metadata in handlersForMethod)
                {
                    var requestType = metadata.RequestType ?? NoValue.Instance.GetType();
                    var responseType = metadata.ResponseType ?? NoValue.Instance.GetType();
                    var methodInfo = s_queueExecuteAsyncMethod.MakeGenericMethod(requestType, responseType);
                    handlerEntryPoints[metadata.Language] = (methodInfo, metadata);
                }

                return handlerEntryPoints.ToFrozenDictionary();
            });
        }

        /// <summary>
        /// StreamJsonRpc entry point for all handler methods.
        /// The optional parameters allow StreamJsonRpc to call into the same method for any kind of request / notification (with any number of params or response).
        /// </summary>
        public async Task<JToken?> ExecuteRequestAsync(JToken? request = null, CancellationToken cancellationToken = default)
        {
            var queue = _target.GetRequestExecutionQueue();
            var lspServices = _target.GetLspServices();

            // Retrieve the language of the request so we know how to deserialize it.
            var language = _target.GetLanguageForRequest(_method, request);

            // Find the correct request and response types for the given request and language.
            if (!_languageEntryPoint.Value.TryGetValue(language, out var requestInfo)
                && !_languageEntryPoint.Value.TryGetValue(LanguageServerConstants.DefaultLanguageName, out requestInfo))
            {
                throw new InvalidOperationException($"No default or language specific handler was found for {_method} and document with language {language}");
            }

            // Deserialize the request parameters (if any).
            var requestObject = DeserializeRequest(request, requestInfo.Metadata, _target._jsonSerializer);

            var task = requestInfo.MethodInfo.Invoke(queue, [requestObject, _method, language, lspServices, cancellationToken]) as Task
                ?? throw new InvalidOperationException($"Queue result task cannot be null");
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result") ?? throw new InvalidOperationException("Result property on task cannot be null");
            var result = resultProperty.GetValue(task);
            if (result is null || result == NoValue.Instance)
            {
                return null;
            }

            return JToken.FromObject(result, _target._jsonSerializer);
        }

        private static object DeserializeRequest(JToken? request, RequestHandlerMetadata metadata, JsonSerializer jsonSerializer)
        {
            if (request is null && metadata.RequestType is not null)
            {
                throw new InvalidOperationException($"Handler {metadata.HandlerDescription} requires request parameters but received none");
            }

            if (request is not null && metadata.RequestType is null)
            {
                throw new InvalidOperationException($"Handler {metadata.HandlerDescription} does not accept parameters, but received some.");
            }

            object requestObject = NoValue.Instance;
            if (request is not null)
            {
                requestObject = request.ToObject(metadata.RequestType, jsonSerializer)
                    ?? throw new InvalidOperationException($"Unable to deserialize {request} into {metadata.RequestType} for {metadata.HandlerDescription}");
            }

            return requestObject;
        }
    }

    public Task WaitForExitAsync()
    {
        lock (_lifeCycleLock)
        {
            // Ensure we've actually been asked to shutdown before waiting.
            if (_shutdownRequestTask == null)
            {
                throw new InvalidOperationException("The language server has not yet been asked to shutdown.");
            }
        }

        // Note - we return the _serverExitedSource task here instead of the _exitNotification task as we may not have
        // finished processing the exit notification before a client calls into us asking to restart.
        // This is because unlike shutdown, exit is a notification where clients do not need to wait for a response.
        return _serverExitedSource.Task;
    }

    /// <summary>
    /// Tells the LSP server to stop handling any more incoming messages (other than exit).
    /// Typically called from an LSP shutdown request.
    /// </summary>
    public Task ShutdownAsync(string message = "Shutting down")
    {
        Task shutdownTask;
        lock (_lifeCycleLock)
        {
            // Run shutdown or return the already running shutdown request.
            _shutdownRequestTask ??= Shutdown_NoLockAsync(message);
            shutdownTask = _shutdownRequestTask;
            return shutdownTask;
        }

        // Runs the actual shutdown outside of the lock - guaranteed to be only called once by the above code.
        async Task Shutdown_NoLockAsync(string message)
        {
            // Immediately yield so that this does not run under the lock.
            await Task.Yield();

            _logger.LogInformation(message);

            // Allow implementations to do any additional cleanup on shutdown.
            var lifeCycleManager = GetLspServices().GetRequiredService<ILifeCycleManager>();
            await lifeCycleManager.ShutdownAsync(message).ConfigureAwait(false);

            await ShutdownRequestExecutionQueueAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Tells the LSP server to exit.  Requires that <see cref="ShutdownAsync(string)"/> was called first.
    /// Typically called from an LSP exit notification.
    /// </summary>
    public Task ExitAsync()
    {
        Task exitTask;
        lock (_lifeCycleLock)
        {
            if (_shutdownRequestTask?.IsCompleted != true)
            {
                throw new InvalidOperationException("The language server has not yet been asked to shutdown or has not finished shutting down.");
            }

            // Run exit or return the already running exit request.
            _exitNotificationTask ??= Exit_NoLockAsync();
            exitTask = _exitNotificationTask;
            return exitTask;
        }

        // Runs the actual exit outside of the lock - guaranteed to be only called once by the above code.
        async Task Exit_NoLockAsync()
        {
            // Immediately yield so that this does not run under the lock.
            await Task.Yield();

            try
            {
                var lspServices = GetLspServices();

                // Allow implementations to do any additional cleanup on exit.
                var lifeCycleManager = lspServices.GetRequiredService<ILifeCycleManager>();
                await lifeCycleManager.ExitAsync().ConfigureAwait(false);

                await ShutdownRequestExecutionQueueAsync().ConfigureAwait(false);

                lspServices.Dispose();

                _jsonRpc.Disconnected -= JsonRpc_Disconnected;
                _jsonRpc.Dispose();
            }
            catch (Exception)
            {
                // Swallow exceptions thrown by disposing our JsonRpc object. Disconnected events can potentially throw their own exceptions so
                // we purposefully ignore all of those exceptions in an effort to shutdown gracefully.
            }
            finally
            {
                _logger.LogInformation("Exiting server");
                _serverExitedSource.TrySetResult(null);
            }
        }
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
        // It is possible this gets called during normal shutdown and exit.
        // ShutdownAsync and ExitAsync will no-op if shutdown was already triggered by something else.
        await ShutdownAsync(message: "Shutdown triggered by JsonRpc disconnect").ConfigureAwait(false);
        await ExitAsync().ConfigureAwait(false);
    }
#pragma warning disable VSTHRD100

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
            if (_server._queue.Value is RequestExecutionQueue<TRequestContext> requestExecution)
                return requestExecution.GetTestAccessor();

            return null;
        }

        internal Task<TResponse> ExecuteRequestAsync<TRequest, TResponse>(string methodName, string languageName, TRequest request, CancellationToken cancellationToken)
        {
            return _server._queue.Value.ExecuteAsync<TRequest, TResponse>(request, methodName, languageName, _server._lspServices.Value, cancellationToken);
        }

        internal JsonRpc GetServerRpc() => _server._jsonRpc;

        internal bool HasShutdownStarted()
        {
            lock (_server._lifeCycleLock)
            {
                return _server._shutdownRequestTask != null;
            }
        }
    }
}
