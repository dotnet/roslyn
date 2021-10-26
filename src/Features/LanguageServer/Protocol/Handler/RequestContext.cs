// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.LanguageServer.Handler.RequestExecutionQueue;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Context for requests handled by <see cref="IRequestHandler"/>
    /// </summary>
    internal readonly struct RequestContext
    {
        /// <summary>
        /// This will be null for non-mutating requests because they're not allowed to change documents
        /// </summary>
        private readonly IDocumentChangeTracker _documentChangeTracker;

        /// <summary>
        /// Manages the workspaces registered for LSP and handles updates from both LSP text sync and workspace updates.
        /// </summary>
        private readonly LspWorkspaceManager _lspWorkspaceManager;

        /// <summary>
        /// The solution state that the request should operate on, if the handler requires an LSP solution, or <see langword="null"/> otherwise
        /// </summary>
        public readonly Solution? Solution;

        /// <summary>
        /// The client capabilities for the request.
        /// </summary>
        public readonly ClientCapabilities ClientCapabilities;

        /// <summary>
        /// The LSP client making the request
        /// </summary>
        public readonly string? ClientName;

        /// <summary>
        /// The document that the request is for, if applicable. This comes from the <see cref="TextDocumentIdentifier"/> returned from the handler itself via a call to <see cref="IRequestHandler{RequestType, ResponseType}.GetTextDocumentIdentifier(RequestType)"/>.
        /// </summary>
        public readonly Document? Document;

        /// <summary>
        /// The languages supported by the server making the request.
        /// </summary>
        public readonly ImmutableArray<string> SupportedLanguages;

        public readonly IGlobalOptionService GlobalOptions;

        /// <summary>
        /// Tracing object that can be used to log information about the status of requests.
        /// </summary>
        private readonly Action<string> _traceInformation;

        public RequestContext(
            Solution? solution,
            Action<string> traceInformation,
            ClientCapabilities clientCapabilities,
            string? clientName,
            Document? document,
            IDocumentChangeTracker documentChangeTracker,
            LspWorkspaceManager lspWorkspaceManager,
            ImmutableArray<string> supportedLanguages,
            IGlobalOptionService globalOptions)
        {
            Document = document;
            Solution = solution;
            ClientCapabilities = clientCapabilities;
            ClientName = clientName;
            SupportedLanguages = supportedLanguages;
            GlobalOptions = globalOptions;
            _documentChangeTracker = documentChangeTracker;
            _traceInformation = traceInformation;
            _lspWorkspaceManager = lspWorkspaceManager;
        }

        public static RequestContext? Create(
            bool requiresLSPSolution,
            TextDocumentIdentifier? textDocument,
            string? clientName,
            ILspLogger logger,
            ClientCapabilities clientCapabilities,
            LspWorkspaceManager lspWorkspaceManager,
            IDocumentChangeTracker documentChangeTracker,
            ImmutableArray<string> supportedLanguages,
            IGlobalOptionService globalOptions)
        {
            // If the handler doesn't need an LSP solution we do two important things:
            // 1. We don't bother building the LSP solution for perf reasons
            // 2. We explicitly don't give the handler a solution or document, even if we could
            //    so they're not accidentally operating on stale solution state.
            if (!requiresLSPSolution)
            {
                return new RequestContext(solution: null, logger.TraceInformation, clientCapabilities, clientName, document: null, documentChangeTracker, lspWorkspaceManager, supportedLanguages, globalOptions);
            }

            // Go through each registered workspace, find the solution that contains the document that
            // this request is for, and then updates it based on the state of the world as we know it, based on the
            // text content in the document change tracker.

            Document? document = null;
            var workspaceSolution = lspWorkspaceManager.TryGetHostLspSolution();
            if (textDocument is not null)
            {
                // we were given a request associated with a document.  Find the corresponding roslyn
                // document for this.  If we can't, we cannot proceed.
                document = lspWorkspaceManager.GetLspDocument(textDocument, clientName);
                if (document != null)
                    workspaceSolution = document.Project.Solution;
            }

            if (workspaceSolution == null)
            {
                logger.TraceError("Could not find appropriate solution for operation");
                return null;
            }

            var context = new RequestContext(
                workspaceSolution,
                logger.TraceInformation,
                clientCapabilities,
                clientName,
                document,
                documentChangeTracker,
                lspWorkspaceManager,
                supportedLanguages,
                globalOptions);
            return context;
        }

        /// <summary>
        /// Allows a mutating request to open a document and start it being tracked.
        /// Mutating requests are serialized by the execution queue in order to prevent concurrent access.
        /// </summary>
        public void StartTracking(Uri uri, SourceText initialText)
        {
            _documentChangeTracker.StartTracking(uri, initialText);
            _lspWorkspaceManager.TrackLspDocument(uri, initialText);
        }

        /// <summary>
        /// Allows a mutating request to update the contents of a tracked document.
        /// Mutating requests are serialized by the execution queue in order to prevent concurrent access.
        /// </summary>
        public void UpdateTrackedDocument(Uri uri, SourceText changedText)
        {
            _documentChangeTracker.UpdateTrackedDocument(uri, changedText);
            _lspWorkspaceManager.UpdateLspDocument(uri, changedText);
        }

        public SourceText GetTrackedDocumentSourceText(Uri documentUri)
            => _documentChangeTracker.GetTrackedDocumentSourceText(documentUri);

        /// <summary>
        /// Allows a mutating request to close a document and stop it being tracked.
        /// Mutating requests are serialized by the execution queue in order to prevent concurrent access.
        /// </summary>
        public void StopTracking(Uri uri)
        {
            _documentChangeTracker.StopTracking(uri);
            _lspWorkspaceManager.StopTrackingLspDocument(uri);
        }

        public bool IsTracking(Uri documentUri)
            => _documentChangeTracker.IsTracking(documentUri);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        public void TraceInformation(string message)
            => _traceInformation(message);
    }
}
