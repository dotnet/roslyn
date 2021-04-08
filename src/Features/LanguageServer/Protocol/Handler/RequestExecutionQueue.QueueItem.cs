// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal partial class RequestExecutionQueue
    {
        private readonly struct QueueItem
        {
            /// <summary>
            /// Callback to call into underlying <see cref="IRequestHandler"/> to perform the actual work of this item.
            /// </summary>
            private readonly Func<RequestContext, CancellationToken, Task> _callbackAsync;

            /// <summary>
            /// <see cref="CorrelationManager.ActivityId"/> used to properly correlate this work with the loghub
            /// tracing/logging subsystem.
            /// </summary>
            public readonly Guid ActivityId;
            private readonly ILspLogger _logger;

            /// <inheritdoc cref="IRequestHandler.MutatesSolutionState" />
            public readonly bool MutatesSolutionState;

            /// <inheritdoc cref="IRequestHandler.RequiresLSPSolution" />
            public readonly bool RequiresLSPSolution;

            /// <inheritdoc cref="RequestContext.ClientName" />
            public readonly string? ClientName;
            public readonly string MethodName;

            /// <inheritdoc cref="RequestContext.ClientCapabilities" />
            public readonly ClientCapabilities ClientCapabilities;

            /// <summary>
            /// The document identifier that will be used to find the solution and document for this request. This comes from the <see cref="TextDocumentIdentifier"/> returned from the handler itself via a call to <see cref="IRequestHandler{RequestType, ResponseType}.GetTextDocumentIdentifier(RequestType)"/>.
            /// </summary>
            public readonly TextDocumentIdentifier? TextDocument;

            /// <summary>
            /// A cancellation token that will cancel the handing of this request. The request could also be cancelled by the queue shutting down.
            /// </summary>
            public readonly CancellationToken CancellationToken;

            public readonly RequestMetrics Metrics;

            public QueueItem(
                bool mutatesSolutionState,
                bool requiresLSPSolution,
                ClientCapabilities clientCapabilities,
                string? clientName,
                string methodName,
                TextDocumentIdentifier? textDocument,
                Guid activityId,
                ILspLogger logger,
                RequestTelemetryLogger telemetryLogger,
                Func<RequestContext, CancellationToken, Task> callbackAsync,
                CancellationToken cancellationToken)
            {
                Metrics = new RequestMetrics(methodName, telemetryLogger);

                _callbackAsync = callbackAsync;
                _logger = logger;

                ActivityId = activityId;
                MutatesSolutionState = mutatesSolutionState;
                RequiresLSPSolution = requiresLSPSolution;
                ClientCapabilities = clientCapabilities;
                ClientName = clientName;
                MethodName = methodName;
                TextDocument = textDocument;
                CancellationToken = cancellationToken;
            }

            /// <summary>
            /// Processes the queued request. Exceptions that occur will be sent back to the requesting client, then re-thrown
            /// </summary>
            public async Task CallbackAsync(RequestContext context, CancellationToken cancellationToken)
            {
                // Restore our activity id so that logging/tracking works.
                Trace.CorrelationManager.ActivityId = ActivityId;
                _logger.TraceStart($"{MethodName} - Roslyn");
                try
                {
                    await _callbackAsync(context, cancellationToken).ConfigureAwait(false);
                    this.Metrics.RecordSuccess();
                }
                catch (OperationCanceledException)
                {
                    _logger.TraceInformation($"{MethodName} - Canceled");
                    this.Metrics.RecordCancellation();
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.TraceException(ex);
                    this.Metrics.RecordFailure();
                    throw;
                }
                finally
                {
                    _logger.TraceStop($"{MethodName} - Roslyn");
                }
            }
        }
    }
}
