// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Threading;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Context for requests handled by <see cref="IMethodHandler"/>
/// </summary>
internal readonly struct RequestContext
{
    private sealed class SolutionContext
    {
        private readonly object _gate = new();
        private readonly LspWorkspaceManager _lspWorkspaceManager;
        private readonly TextDocumentIdentifier? _textDocumentIdentifier;
        private readonly ImmutableDictionary<DocumentUri, TrackedDocumentInfo> _trackedDocuments;
        private readonly OnDemandProjectLoadOperation _loadOperation;
        private readonly ILspLogger _logger;
        private readonly string _method;
        private readonly bool _mutatesSolutionState;
        private readonly (Workspace Workspace, Solution Solution, TextDocument? Document) _initialValue;

        private (Workspace Workspace, Solution Solution, TextDocument? Document) _value;
        private Task? _resolutionTask;
        private bool _hasResolved;
        private bool _isCleared;

        public SolutionContext(
            Workspace workspace,
            Solution solution,
            TextDocument? document,
            LspWorkspaceManager lspWorkspaceManager,
            TextDocumentIdentifier? textDocumentIdentifier,
            ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
            OnDemandProjectLoadOperation loadOperation,
            ILspLogger logger,
            string method,
            bool mutatesSolutionState)
        {
            _initialValue = _value = (workspace, solution, document);
            _lspWorkspaceManager = lspWorkspaceManager;
            _textDocumentIdentifier = textDocumentIdentifier;
            _trackedDocuments = trackedDocuments;
            _loadOperation = loadOperation;
            _logger = logger;
            _method = method;
            _mutatesSolutionState = mutatesSolutionState;
        }

        public (Workspace Workspace, Solution Solution, TextDocument? Document) GetCurrentValue()
        {
            lock (_gate)
            {
                if (_isCleared)
                    throw new InvalidOperationException();

                return _value;
            }
        }

        public (Workspace Workspace, Solution Solution, TextDocument? Document) GetInitialValue()
            => _initialValue;

        public async ValueTask<(Workspace Workspace, Solution Solution, TextDocument? Document)> GetValueAsync(CancellationToken cancellationToken)
        {
            Task? resolutionTask;
            lock (_gate)
            {
                if (_isCleared)
                    throw new InvalidOperationException();

                if (_mutatesSolutionState || _hasResolved)
                    return _value;

                resolutionTask = _resolutionTask ??= ResolveAsync();
            }

            await resolutionTask.WithCancellation(cancellationToken).ConfigureAwait(false);
            return GetCurrentValue();
        }

        private async Task ResolveAsync()
        {
            var resolvedValue = _value;
            try
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await _loadOperation.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                }
                finally
                {
                    _logger.LogDebug($"Waited {stopwatch.ElapsedMilliseconds} ms for project loading on {_method}.");
                }

                Workspace? workspace = null;
                Solution? solution = null;
                TextDocument? document = null;
                if (_textDocumentIdentifier is not null)
                {
                    (workspace, solution, document) = await _lspWorkspaceManager.GetLspDocumentInfoAsync(
                        _textDocumentIdentifier, _trackedDocuments, CancellationToken.None).ConfigureAwait(false);
                }

                if (workspace is null)
                    (workspace, solution) = await _lspWorkspaceManager.GetLspSolutionInfoAsync(CancellationToken.None).ConfigureAwait(false);

                if (workspace is not null && solution is not null)
                    resolvedValue = (workspace, solution, document);
            }
            catch (Exception exception) when (FatalError.ReportAndCatch(exception))
            {
                _logger.LogException(exception);
                _logger.LogWarning($"Could not refresh solution context after project loading on {_method}.");
            }

            lock (_gate)
            {
                if (!_isCleared)
                {
                    _value = resolvedValue;
                    _hasResolved = true;
                    _resolutionTask = null;
                }
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _value = default;
                _resolutionTask = null;
                _isCleared = true;
            }
        }
    }

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
    /// Provides backing storage for the LSP workspace used by this RequestContext instance, allowing it to be cleared
    /// on demand from all copies that may exist of this value type.
    /// </summary>
    /// <remarks>
    /// This field is only initialized for handlers that request solution context.
    /// </remarks>
    private readonly SolutionContext? _solutionContext;

    public ILspLogger Logger { get; }

    /// <summary>
    /// The workspace this request is for, if applicable.  This will be present if <see cref="GetDocumentAsync(CancellationToken)"/> returns a document.
    /// present.  It will be <see langword="null"/> if <c>requiresLSPSolution</c> is false.
    /// </summary>
    public Workspace? Workspace
    {
        get
        {
            if (_solutionContext is null)
            {
                // This request context never had a workspace instance
                return null;
            }

            // The workspace is available unless it has been cleared by a call to ClearSolutionContext. Explicitly throw
            // for attempts to access this property after it has been manually cleared.
            return _solutionContext.GetCurrentValue().Workspace;
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

    public RequestContext(
        Workspace? workspace,
        Solution? solution,
        ILspLogger logger,
        string method,
        ClientCapabilities? clientCapabilities,
        WellKnownLspServerKinds serverKind,
        TextDocument? document,
        IDocumentChangeTracker documentChangeTracker,
        ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
        ImmutableArray<string> supportedLanguages,
        ILspServices lspServices,
        LspWorkspaceManager lspWorkspaceManager,
        TextDocumentIdentifier? textDocumentIdentifier,
        OnDemandProjectLoadOperation loadOperation,
        bool mutatesSolutionState,
        CancellationToken queueCancellationToken)
    {
        if (workspace is not null)
        {
            RoslynDebug.Assert(solution is not null);
            _solutionContext = new SolutionContext(
                workspace,
                solution,
                document,
                lspWorkspaceManager,
                textDocumentIdentifier,
                trackedDocuments,
                loadOperation,
                logger,
                method,
                mutatesSolutionState);
        }
        else
        {
            RoslynDebug.Assert(solution is null);
            RoslynDebug.Assert(document is null);
            _solutionContext = null;
        }

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

    public async ValueTask<Solution?> GetSolutionAsync(CancellationToken cancellationToken)
        => _solutionContext is null
            ? null
            : (await _solutionContext.GetValueAsync(cancellationToken).ConfigureAwait(false)).Solution;

    internal Solution? GetInitialSolution()
        => _solutionContext?.GetInitialValue().Solution;

    internal TextDocument? GetInitialTextDocument()
        => _solutionContext?.GetInitialValue().Document;

    public async ValueTask<Solution> GetRequiredSolutionAsync(CancellationToken cancellationToken)
        => await GetSolutionAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new ArgumentNullException($"{nameof(Solution)} is null when it was required for {Method}");

    public async ValueTask<TextDocument?> GetTextDocumentAsync(CancellationToken cancellationToken)
        => _solutionContext is null
            ? null
            : (await _solutionContext.GetValueAsync(cancellationToken).ConfigureAwait(false)).Document;

    public async ValueTask<TextDocument> GetRequiredTextDocumentAsync(CancellationToken cancellationToken)
        => await GetTextDocumentAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new ArgumentNullException($"{nameof(TextDocument)} is null when it was required for {Method}");

    public async ValueTask<Document?> GetDocumentAsync(CancellationToken cancellationToken)
    {
        var textDocument = await GetTextDocumentAsync(cancellationToken).ConfigureAwait(false);
        return textDocument switch
        {
            null => null,
            Document document => document,
            _ => throw new InvalidOperationException("Attempted to retrieve a Document but a TextDocument was found instead."),
        };
    }

    public async ValueTask<Document> GetRequiredDocumentAsync(CancellationToken cancellationToken)
        => await GetDocumentAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new ArgumentNullException($"{nameof(Document)} is null when it was required for {Method}");

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
        OnDemandProjectLoadOperation loadOperation,
        ImmutableDictionary<DocumentUri, TrackedDocumentInfo>? trackedDocuments,
        CancellationToken cancellationToken)
    {
        var lspWorkspaceManager = lspServices.GetRequiredService<LspWorkspaceManager>();
        var documentChangeTracker = mutatesSolutionState ? (IDocumentChangeTracker)lspWorkspaceManager : NonMutatingDocumentChangeTracker.Instance;

        // Retrieve the current LSP tracked text as of this request.
        // This is safe as all creation of request contexts cannot happen concurrently.
        trackedDocuments ??= lspWorkspaceManager.GetTrackedLspText();

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
                lspWorkspaceManager: lspWorkspaceManager, textDocumentIdentifier: textDocument, loadOperation: loadOperation, mutatesSolutionState: mutatesSolutionState,
                queueCancellationToken: cancellationToken);
        }
        else
        {
            Workspace? workspace = null;
            Solution? solution = null;
            TextDocument? document = null;
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

            if (workspace is null || solution is null)
            {
                logger.LogError($"Could not find appropriate workspace or solution on {method}");
                FatalError.ReportWithDumpAndCatch(new Exception(
                    $"Could not find appropriate workspace or solution on {method}"), ErrorSeverity.Critical);
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
                lspWorkspaceManager,
                textDocument,
                loadOperation,
                mutatesSolutionState,
                cancellationToken);
        }

        return context;
    }

    /// <summary>
    /// Allows a mutating request to open a document and start it being tracked.
    /// Mutating requests are serialized by the execution queue in order to prevent concurrent access.
    /// </summary>
    public async ValueTask StartTrackingAsync(DocumentUri uri, SourceText initialText, string languageId, int lspVersion, CancellationToken cancellationToken)
        => await _documentChangeTracker.StartTrackingAsync(uri, initialText, languageId, lspVersion, cancellationToken).ConfigureAwait(false);

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

    public void ClearSolutionContext()
    {
        if (_solutionContext is null)
            return;

        _solutionContext.Clear();
    }

    public void TraceDebug(string message)
        => Logger.LogDebug(message);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    public void TraceInformation(string message)
        => Logger.LogInformation(message);

    public void TraceWarning(string message)
        => Logger.LogWarning(message);

    public void TraceError(string message)
        => Logger.LogError(message);

    public void TraceException(Exception exception)
        => Logger.LogException(exception);

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

    public T? GetService<T>() where T : class, ILspService
    {
        return _lspServices.GetService<T>();
    }
}
