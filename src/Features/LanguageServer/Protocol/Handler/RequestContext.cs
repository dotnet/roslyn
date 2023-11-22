﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

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
    private readonly ImmutableDictionary<Uri, (SourceText Text, string LanguageId)> _trackedDocuments;

    private readonly ILspServices _lspServices;

    /// <summary>
    /// Provides backing storage for the LSP workspace used by this RequestContext instance, allowing it to be cleared
    /// on demand from all copies that may exist of this value type.
    /// </summary>
    /// <remarks>
    /// This field is only initialized for handlers that request solution context.
    /// </remarks>
    private readonly StrongBox<(Workspace Workspace, Solution Solution, Document? Document)>? _lspSolution;

    /// <summary>
    /// The workspace this request is for, if applicable.  This will be present if <see cref="Document"/> is
    /// present.  It will be <see langword="null"/> if <c>requiresLSPSolution</c> is false.
    /// </summary>
    public Workspace? Workspace
    {
        get
        {
            if (_lspSolution is null)
            {
                // This request context never had a workspace instance
                return null;
            }

            // The workspace is available unless it has been cleared by a call to ClearSolutionContext. Explicitly throw
            // for attempts to access this property after it has been manually cleared.
            return _lspSolution.Value.Workspace ?? throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// The solution state that the request should operate on, if the handler requires an LSP solution, or <see langword="null"/> otherwise
    /// </summary>
    public Solution? Solution
    {
        get
        {
            if (_lspSolution is null)
            {
                // This request context never had a solution instance
                return null;
            }

            // The solution is available unless it has been cleared by a call to ClearSolutionContext. Explicitly throw
            // for attempts to access this property after it has been manually cleared.
            return _lspSolution.Value.Solution ?? throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// The document that the request is for, if applicable. This comes from the <see cref="TextDocumentIdentifier"/> returned from the handler itself via a call to 
    /// <see cref="ITextDocumentIdentifierHandler{RequestType, TextDocumentIdentifierType}.GetTextDocumentIdentifier(RequestType)"/>.
    /// </summary>
    public Document? Document
    {
        get
        {
            if (_lspSolution is null)
            {
                // This request context never had a solution instance
                return null;
            }

            // The solution is available unless it has been cleared by a call to ClearSolutionContext. Explicitly throw
            // for attempts to access this property after it has been manually cleared. Note that we can't rely on
            // Document being null for this check, because it is not always provided as part of the solution context.
            if (_lspSolution.Value.Workspace is null)
            {
                throw new InvalidOperationException();
            }

            return _lspSolution.Value.Document;
        }
    }

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

    /// <summary>
    /// Tracing object that can be used to log information about the status of requests.
    /// </summary>
    private readonly ILspLogger _logger;

    public RequestContext(
        Workspace? workspace,
        Solution? solution,
        ILspLogger logger,
        string method,
        ClientCapabilities? clientCapabilities,
        WellKnownLspServerKinds serverKind,
        Document? document,
        IDocumentChangeTracker documentChangeTracker,
        ImmutableDictionary<Uri, (SourceText Text, string LanguageId)> trackedDocuments,
        ImmutableArray<string> supportedLanguages,
        ILspServices lspServices,
        CancellationToken queueCancellationToken)
    {
        if (workspace is not null)
        {
            RoslynDebug.Assert(solution is not null);
            _lspSolution = new StrongBox<(Workspace Workspace, Solution Solution, Document? Document)>((workspace, solution, document));
        }
        else
        {
            RoslynDebug.Assert(solution is null);
            RoslynDebug.Assert(document is null);
            _lspSolution = null;
        }

        _clientCapabilities = clientCapabilities;
        ServerKind = serverKind;
        SupportedLanguages = supportedLanguages;
        _documentChangeTracker = documentChangeTracker;
        _logger = logger;
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

    public Document GetRequiredDocument()
    {
        return Document is null
            ? throw new ArgumentNullException($"{nameof(Document)} is null when it was required for {Method}")
            : Document;
    }

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
        var documentChangeTracker = mutatesSolutionState ? (IDocumentChangeTracker)lspWorkspaceManager : new NonMutatingDocumentChangeTracker();

        // Retrieve the current LSP tracked text as of this request.
        // This is safe as all creation of request contexts cannot happen concurrently.
        var trackedDocuments = lspWorkspaceManager.GetTrackedLspText();

        // If the handler doesn't need an LSP solution we do two important things:
        // 1. We don't bother building the LSP solution for perf reasons
        // 2. We explicitly don't give the handler a solution or document, even if we could
        //    so they're not accidentally operating on stale solution state.
        RequestContext context;
        if (!requiresLSPSolution)
        {
            context = new RequestContext(
                workspace: null, solution: null, logger: logger, method: method, clientCapabilities: clientCapabilities, serverKind: serverKind, document: null,
                documentChangeTracker: documentChangeTracker, trackedDocuments: trackedDocuments, supportedLanguages: supportedLanguages, lspServices: lspServices,
                queueCancellationToken: cancellationToken);
        }
        else
        {
            Workspace? workspace = null;
            Solution? solution = null;
            Document? document = null;
            if (textDocument is not null)
            {
                // we were given a request associated with a document.  Find the corresponding roslyn document for this.
                // There are certain cases where we may be asked for a document that does not exist (for example a
                // document is removed) For example, document pull diagnostics can ask us after removal to clear
                // diagnostics for a document.
                (workspace, solution, document) = await lspWorkspaceManager.GetLspDocumentInfoAsync(textDocument, cancellationToken).ConfigureAwait(false);
            }

            if (workspace is null)
            {
                (workspace, solution) = await lspWorkspaceManager.GetLspSolutionInfoAsync(cancellationToken).ConfigureAwait(false);
            }

            if (workspace is null)
            {
                logger.LogError($"Could not find appropriate workspace for operation {workspace} on {method}");
            }

            context = new RequestContext(
                workspace,
                solution,
                logger,
                method,
                clientCapabilities,
                serverKind,
                document,
                documentChangeTracker,
                trackedDocuments,
                supportedLanguages,
                lspServices,
                cancellationToken);
        }

        return context;
    }

    /// <summary>
    /// Allows a mutating request to open a document and start it being tracked.
    /// Mutating requests are serialized by the execution queue in order to prevent concurrent access.
    /// </summary>
    public ValueTask StartTrackingAsync(Uri uri, SourceText initialText, string languageId, CancellationToken cancellationToken)
        => _documentChangeTracker.StartTrackingAsync(uri, initialText, languageId, cancellationToken);

    /// <summary>
    /// Allows a mutating request to update the contents of a tracked document.
    /// Mutating requests are serialized by the execution queue in order to prevent concurrent access.
    /// </summary>
    public void UpdateTrackedDocument(Uri uri, SourceText changedText)
        => _documentChangeTracker.UpdateTrackedDocument(uri, changedText);

    public SourceText GetTrackedDocumentSourceText(Uri documentUri)
    {
        Contract.ThrowIfFalse(_trackedDocuments.ContainsKey(documentUri), $"Attempted to get text for {documentUri} which is not open.");
        return _trackedDocuments[documentUri].Text;
    }

    /// <summary>
    /// Allows a mutating request to close a document and stop it being tracked.
    /// Mutating requests are serialized by the execution queue in order to prevent concurrent access.
    /// </summary>
    public ValueTask StopTrackingAsync(Uri uri, CancellationToken cancellationToken)
        => _documentChangeTracker.StopTrackingAsync(uri, cancellationToken);

    public bool IsTracking(Uri documentUri)
        => _trackedDocuments.ContainsKey(documentUri);

    public void ClearSolutionContext()
    {
        if (_lspSolution is null)
            return;

        _lspSolution.Value = default;
    }

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    public void TraceInformation(string message)
        => _logger.LogInformation(message);

    public void TraceWarning(string message)
        => _logger.LogWarning(message);

    public void TraceError(string message)
        => _logger.LogError(message);

    public void TraceException(Exception exception)
        => _logger.LogException(exception);

    public T GetRequiredLspService<T>() where T : class, ILspService
    {
        return _lspServices.GetRequiredService<T>();
    }

    public T GetRequiredService<T>() where T : class
    {
        return _lspServices.GetRequiredService<T>();
    }

    public IEnumerable<T> GetRequiredServices<T>() where T : class
    {
        return _lspServices.GetRequiredServices<T>();
    }
}
