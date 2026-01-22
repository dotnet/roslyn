// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Nerdbank.Streams;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;

internal abstract partial class AbstractInProcLanguageClient(
    AbstractLspServiceProvider lspServiceProvider,
    IGlobalOptionService globalOptions,
    ILspServiceLoggerFactory lspLoggerFactory,
    ExportProvider exportProvider,
    AbstractLanguageClientMiddleLayer? middleLayer = null)
        : ILanguageClient, ILanguageServerFactory, ILanguageClientCustomMessage2, IPropertyOwner
{
    private readonly ILanguageClientMiddleLayer2<JsonElement>? _middleLayer = middleLayer;
    private readonly ILspServiceLoggerFactory _lspLoggerFactory = lspLoggerFactory;
    private readonly ExportProvider _exportProvider = exportProvider;

    protected readonly AbstractLspServiceProvider LspServiceProvider = lspServiceProvider;

    protected readonly IGlobalOptionService GlobalOptions = globalOptions;

    /// <summary>
    /// Created when <see cref="ActivateAsync"/> is called.
    /// </summary>
    private AbstractLanguageServer<RequestContext>? _languageServer;

    /// <summary>
    /// Gets the name of the language client (displayed to the user).
    /// </summary>
    public string Name => ServerKind.ToUserVisibleString();

    /// <summary>
    /// Gets the optional middle layer object that can intercept outgoing requests and responses.
    /// </summary>
    /// <remarks>
    /// Currently utilized by Razor to intercept Roslyn's workspace/semanticTokens/refresh requests.
    /// </remarks>
    public object? MiddleLayer => _middleLayer;

    /// <summary>
    /// Unused, implementing <see cref="ILanguageClientCustomMessage2"/>.
    /// Gets the optional target object for receiving custom messages not covered by the language server protocol.
    /// </summary>
    public virtual object? CustomMessageTarget => null;

    /// <summary>
    /// An enum representing this server instance.
    /// </summary>
    public abstract WellKnownLspServerKinds ServerKind { get; }

    /// <summary>
    /// The set of languages that this LSP server supports and can return results for.
    /// </summary>
    protected abstract ImmutableArray<string> SupportedLanguages { get; }

    /// <summary>
    /// Unused, implementing <see cref="ILanguageClient"/>
    /// No additional settings are provided for this server, so we do not need any configuration section names.
    /// </summary>
    public IEnumerable<string>? ConfigurationSections { get; }

    /// <summary>
    /// Gets the initialization options object the client wants to send when 'initialize' message is sent.
    /// See https://microsoft.github.io/language-server-protocol/specifications/specification-3-14/#initialize
    /// We do not provide any additional initialization options.
    /// </summary>
    public object? InitializationOptions { get; }

    /// <summary>
    /// Gets a value indicating whether a notification bubble show be shown when the language server fails to initialize.
    /// </summary>
    public abstract bool ShowNotificationOnInitializeFailed { get; }

    /// <summary>
    /// Unused, implementing <see cref="ILanguageClient"/>
    /// Files that we care about are already provided and watched by the workspace.
    /// </summary>
    public IEnumerable<string>? FilesToWatch { get; }

    /// <summary>
    /// Property collection used by the client.
    /// This is where we set the property to enable the use of client side System.Text.Json serialization.
    /// </summary>
    public PropertyCollection Properties { get; } = CreateStjPropertyCollection();

    public event AsyncEventHandler<EventArgs>? StartAsync;

    public event AsyncEventHandler<EventArgs>? StopAsync;

    /// <summary>
    /// Stops the server if it has been started.
    /// </summary>
    /// <remarks>
    /// Per the documentation on <see cref="ILanguageClient.StopAsync"/>, the event is ignored if the server has not been started.
    /// </remarks>
    public Task StopServerAsync()
        => StopAsync?.InvokeAsync(this, EventArgs.Empty) ?? Task.CompletedTask;

    public async Task<Connection?> ActivateAsync(CancellationToken cancellationToken)
    {
        if (_languageServer is not null)
        {
            await _languageServer.WaitForExitAsync().WithCancellation(cancellationToken).ConfigureAwait(false);
        }

        var (clientStream, serverStream) = FullDuplexStream.CreatePair();

        _languageServer = await CreateAsync<RequestContext>(
            this,
            serverStream,
            serverStream,
            ServerKind,
            _lspLoggerFactory,
            typeRefResolver: null,
            cancellationToken).ConfigureAwait(false);

        return new Connection(clientStream, clientStream);
    }

    /// <summary>
    /// Signals that the extension has been loaded.  The server can be started immediately, or wait for user action to start.  
    /// To start the server, invoke the <see cref="StartAsync"/> event;
    /// </summary>
    public virtual async Task OnLoadedAsync()
    {
        try
        {
            await StartAsync.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);
        }
        catch (AggregateException e)
        {
            // The VS LSP client allows an unexpected OperationCanceledException to propagate out of the StartAsync
            // callback. Avoid allowing it to propagate further.
            e.Handle(ex => ex is OperationCanceledException);
        }
    }

    /// <summary>
    /// Signals the extension that the language server has been successfully initialized.
    /// </summary>
    /// <returns>A <see cref="Task"/> which completes when actions that need to be performed when the server is ready are done.</returns>
    public async Task OnServerInitializedAsync()
    {
        // We don't have any tasks that need to be triggered after the server has successfully initialized.
    }

    internal async Task<AbstractLanguageServer<RequestContext>> CreateAsync<TRequestContext>(
        AbstractInProcLanguageClient languageClient,
        Stream inputStream,
        Stream outputStream,
        WellKnownLspServerKinds serverKind,
        ILspServiceLoggerFactory lspLoggerFactory,
        AbstractTypeRefResolver? typeRefResolver,
        CancellationToken cancellationToken)
    {
        var messageFormatter = RoslynLanguageServer.CreateJsonMessageFormatter();

        var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(outputStream, inputStream, messageFormatter))
        {
            ExceptionStrategy = ExceptionProcessing.ISerializable,
        };

        var serverTypeName = languageClient.GetType().Name;

        var logger = await lspLoggerFactory.CreateLoggerAsync(serverTypeName, jsonRpc, cancellationToken).ConfigureAwait(false);

        var hostServices = VisualStudioMefHostServices.Create(_exportProvider);
        var server = Create(
            jsonRpc,
            messageFormatter.JsonSerializerOptions,
            serverKind,
            logger,
            hostServices,
            typeRefResolver);

        jsonRpc.StartListening();
        return server;
    }

    public virtual AbstractLanguageServer<RequestContext> Create(
        JsonRpc jsonRpc,
        JsonSerializerOptions options,
        WellKnownLspServerKinds serverKind,
        AbstractLspLogger logger,
        HostServices hostServices,
        AbstractTypeRefResolver? typeRefResolver = null)
    {
        var server = new RoslynLanguageServer(
            LspServiceProvider,
            jsonRpc,
            options,
            logger,
            hostServices,
            SupportedLanguages,
            serverKind,
            typeRefResolver);

        return server;
    }

    public async Task<InitializationFailureContext?> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
    {
        var initializationFailureContext = new InitializationFailureContext();
        initializationFailureContext.FailureMessage = string.Format(EditorFeaturesResources.Language_client_initialization_failed,
            Name, initializationState.StatusMessage, initializationState.InitializationException?.ToString());
        return initializationFailureContext;
    }

    /// <summary>
    /// Unused, implementing <see cref="ILanguageClientCustomMessage2"/>.
    /// This method is called after the language server has been activated, but connection has not been established.
    /// </summary>
    public Task AttachForCustomMessageAsync(JsonRpc rpc) => Task.CompletedTask;

    private static PropertyCollection CreateStjPropertyCollection()
    {
        var collection = new PropertyCollection();
        // These are well known property names used by the LSP client to enable STJ client side serialization.
        collection.AddProperty("lsp-serialization", "stj");
        return collection;
    }

    internal TestAccessor GetTestAccessor()
    {
        return new TestAccessor(this);
    }

    internal readonly struct TestAccessor
    {
        private readonly AbstractInProcLanguageClient _instance;

        internal TestAccessor(AbstractInProcLanguageClient instance)
        {
            _instance = instance;
        }

        public AbstractLanguageServer<RequestContext>? LanguageServer => _instance._languageServer;
    }
}
