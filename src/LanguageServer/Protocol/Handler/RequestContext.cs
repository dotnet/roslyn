// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Threading;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Context for requests handled by <see cref="IMethodHandler"/>
/// </summary>
internal readonly struct RequestContext
{
    /// <summary>
    /// This will be the <see cref="NonMutatingDocumentChangeTracker"/> for non-mutating requests because they're not allowed to change documents
    /// </summary>
    private readonly IDocumentChangeTracker _documentChangeTracker;

    /// <summary>
    /// The client capabilities for the request.
    /// </summary>
    /// <remarks>
    /// Should only be null on the "initialize" request.
    /// </remarks>
    private readonly ClientCapabilities? _clientCapabilities;

    /// <summary>
    /// Contains the LSP text for all opened LSP documents from when this request was processed in the queue.
    /// </summary>
    /// <remarks>
    /// This is a snapshot of the source text that reflects the LSP text based on the order of this request in the queue.
    /// It contains text that is consistent with all prior LSP text sync notifications, but LSP text sync requests
    /// which are ordered after this one in the queue are not reflected here.
    /// </remarks>
    private readonly ImmutableDictionary<DocumentUri, TrackedDocumentInfo> _trackedDocuments;

    private readonly ILspServices _lspServices;

    /// <summary>
    /// Shared by all copies of this value type so clearing the solution context releases their captured workspace state.
    /// </summary>
    /// <remarks>
    /// This field is only initialized for handlers that request solution context.
    /// </remarks>
    private readonly CapturedLspWorkspaceContext? _capturedWorkspaceContext;

    public ILspLogger Logger { get; }

    /// <summary>
    /// The LSP server handling the request.
    /// </summary>
    public readonly WellKnownLspServerKinds ServerKind;

    /// <summary>
    /// The method this request is targeting.
    /// </summary>
    public readonly string Method;

    /// <summary>
    /// The languages supported by the server making the request.
    /// </summary>
    public readonly ImmutableArray<string> SupportedLanguages;

    public readonly CancellationToken QueueCancellationToken;

    public RequestContext(
        CapturedLspWorkspaceContext? capturedWorkspaceContext,
        ILspLogger logger,
        string method,
        ClientCapabilities? clientCapabilities,
        WellKnownLspServerKinds serverKind,
        IDocumentChangeTracker documentChangeTracker,
        ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
        ImmutableArray<string> supportedLanguages,
        ILspServices lspServices,
        CancellationToken queueCancellationToken)
    {
        _capturedWorkspaceContext = capturedWorkspaceContext;

        _clientCapabilities = clientCapabilities;
        ServerKind = serverKind;
        SupportedLanguages = supportedLanguages;
        _documentChangeTracker = documentChangeTracker;
        Logger = logger;
        _trackedDocuments = trackedDocuments;
        _lspServices = lspServices;
        QueueCancellationToken = queueCancellationToken;
        Method = method;
    }

    public ClientCapabilities GetRequiredClientCapabilities()
    {
        return _clientCapabilities is null
            ? throw new ArgumentNullException($"{nameof(ClientCapabilities)} is null when it was required for {Method}")
            : _clientCapabilities;
    }

    public async ValueTask<Workspace?> GetWorkspaceAsync(CancellationToken cancellationToken)
        => _capturedWorkspaceContext is null
            ? null
            : (await GetRequiredWorkspaceContextAsync().WithCancellation(cancellationToken).ConfigureAwait(false)).Workspace;

    public async ValueTask<Workspace> GetRequiredWorkspaceAsync(CancellationToken cancellationToken)
        => await GetWorkspaceAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Workspace is null when it was required for {Method}");

    public async ValueTask<Solution?> GetSolutionAsync(CancellationToken cancellationToken)
        => _capturedWorkspaceContext is null
            ? null
            : (await GetRequiredWorkspaceContextAsync().WithCancellation(cancellationToken).ConfigureAwait(false)).Solution;

    public async ValueTask<Solution> GetRequiredSolutionAsync(CancellationToken cancellationToken)
        => await GetSolutionAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Solution is null when it was required for {Method}");

    public async ValueTask<TextDocument?> GetTextDocumentAsync(CancellationToken cancellationToken)
        => _capturedWorkspaceContext is null
            ? null
            : (await GetRequiredWorkspaceContextAsync().WithCancellation(cancellationToken).ConfigureAwait(false)).Document;

    private Task<LspWorkspaceContext> GetRequiredWorkspaceContextAsync()
        => _capturedWorkspaceContext?.ResolveAsync() ?? throw new InvalidOperationException("Workspace context has been cleared.");

    public void ClearSolutionContext()
    {
        _capturedWorkspaceContext?.Clear();
    }

    public async ValueTask<TextDocument> GetRequiredTextDocumentAsync(CancellationToken cancellationToken)
        => await GetTextDocumentAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"TextDocument is null when it was required for {Method}");

    public async ValueTask<Document?> GetDocumentAsync(CancellationToken cancellationToken)
    {
        return (await GetTextDocumentAsync(cancellationToken).ConfigureAwait(false)) switch
        {
            null => null,
            Document document => document,
            _ => throw new InvalidOperationException("Attempted to retrieve a Document but a TextDocument was found instead."),
        };
    }

    public async ValueTask<Document> GetRequiredDocumentAsync(CancellationToken cancellationToken)
        => await GetDocumentAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Document is null when it was required for {Method}");

    public static async Task<RequestContext> CreateAsync(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        TextDocumentIdentifier? textDocument,
        WellKnownLspServerKinds serverKind,
        ClientCapabilities? clientCapabilities,
        ImmutableArray<string> supportedLanguages,
        ILspServices lspServices,
        ILspLogger logger,
        string method,
        CancellationToken cancellationToken)
    {
        var lspWorkspaceManager = lspServices.GetRequiredService<LspWorkspaceManager>();
        var documentChangeTracker = mutatesSolutionState ? (IDocumentChangeTracker)lspWorkspaceManager : NonMutatingDocumentChangeTracker.Instance;

        // Retrieve the current LSP tracked text as of this request.
        // This is safe as all creation of request contexts cannot happen concurrently.
        var trackedDocuments = lspWorkspaceManager.GetTrackedLspText();

        // If the handler doesn't need an LSP solution we do two important things:
        // 1. We don't bother building the LSP solution for perf reasons
        // 2. We explicitly don't give the handler a solution or document, even if we could
        //    so they're not accidentally operating on stale solution state.
        CapturedLspWorkspaceContext? capturedWorkspaceContext = null;

        if (requiresLSPSolution)
        {
            var allowProjectLoading = !mutatesSolutionState;
            capturedWorkspaceContext = textDocument is null
                ? await lspWorkspaceManager.GetLspWorkspaceContextAsync(
                    trackedDocuments, allowProjectLoading, cancellationToken).ConfigureAwait(false)
                : await lspWorkspaceManager.GetLspDocumentContextAsync(
                    textDocument, trackedDocuments, allowProjectLoading, cancellationToken).ConfigureAwait(false);

            // A client may request previous results after a document is removed and closed. The document is no
            // longer tracked or available in a workspace, but the handler still needs a solution to clear its results.
            // SpellCheckHandler for instance may need to clear diagnostics for a document that has been closed.
            if (textDocument is not null && capturedWorkspaceContext is null)
            {
                capturedWorkspaceContext = await lspWorkspaceManager.GetLspWorkspaceContextAsync(
                    trackedDocuments, allowProjectLoading, cancellationToken).ConfigureAwait(false);
            }

            if (capturedWorkspaceContext is null)
            {
                logger.LogError($"Could not find appropriate workspace or solution on {method}");
                FatalError.ReportWithDumpAndCatch(new Exception(
                    $"Could not find appropriate workspace or solution on {method}"), ErrorSeverity.Critical);
            }
        }

        return new RequestContext(
            capturedWorkspaceContext,
            logger,
            method,
            clientCapabilities,
            serverKind,
            documentChangeTracker,
            trackedDocuments,
            supportedLanguages,
            lspServices,
            cancellationToken);
    }

    /// <summary>
    /// Allows a mutating request to open a document and start it being tracked.
    /// Mutating requests are serialized by the execution queue in order to prevent concurrent access.
    /// </summary>
    public ValueTask StartTrackingAsync(DocumentUri uri, SourceText initialText, string languageId, int lspVersion, CancellationToken cancellationToken)
        => _documentChangeTracker.StartTrackingAsync(uri, initialText, languageId, lspVersion, cancellationToken);

    /// <summary>
    /// Allows a mutating request to update the contents of a tracked document.
    /// Mutating requests are serialized by the execution queue in order to prevent concurrent access.
    /// </summary>
    public void UpdateTrackedDocument(DocumentUri uri, SourceText changedText, int lspVersion)
        => _documentChangeTracker.UpdateTrackedDocument(uri, changedText, lspVersion);

    public TrackedDocumentInfo GetTrackedDocumentInfo(DocumentUri documentUri)
    {
        Contract.ThrowIfFalse(_trackedDocuments.ContainsKey(documentUri), $"Attempted to get text for {documentUri} which is not open.");
        return _trackedDocuments[documentUri];
    }

    /// <summary>
    /// Allows a mutating request to close a document and stop it being tracked.
    /// Mutating requests are serialized by the execution queue in order to prevent concurrent access.
    /// </summary>
    public ValueTask StopTrackingAsync(DocumentUri uri, CancellationToken cancellationToken)
        => _documentChangeTracker.StopTrackingAsync(uri, cancellationToken);

    public bool IsTracking(DocumentUri documentUri)
        => _trackedDocuments.ContainsKey(documentUri);

    public void TraceDebug(string message)
        => Logger.LogDebug(message);

    public void TraceInformation(string message)
        => Logger.LogInformation(message);

    public void TraceWarning(string message)
        => Logger.LogWarning(message);

    public void TraceError(string message)
        => Logger.LogError(message);

    public void TraceException(Exception exception)
        => Logger.LogException(exception);

    public T GetRequiredLspService<T>() where T : class, ILspService
        => _lspServices.GetRequiredService<T>();

    public T GetRequiredService<T>() where T : class
        => _lspServices.GetRequiredService<T>();

    public IEnumerable<T> GetRequiredServices<T>() where T : class
        => _lspServices.GetRequiredServices<T>();

    public T? GetService<T>() where T : class, ILspService
        => _lspServices.GetService<T>();
}
