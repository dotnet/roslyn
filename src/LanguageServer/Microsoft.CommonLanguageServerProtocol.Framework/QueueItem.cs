﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// A placeholder type to help handle parameterless messages and messages with no return value.
/// </summary>
internal sealed class NoValue
{
    public static NoValue Instance = new();
}

internal class QueueItem<TRequestContext> : IQueueItem<TRequestContext>
{
    private readonly ILspLogger _logger;
    private readonly AbstractRequestScope? _requestTelemetryScope;

    /// <summary>
    /// True if this queue item has actually started handling the request
    /// by delegating to the handler.  False while the item is still being
    /// processed by the queue.
    /// </summary>
    private bool _requestHandlingStarted = false;

    /// <summary>
    /// A task completion source representing the result of this queue item's work.
    /// This is the task that the client is waiting on.
    /// </summary>
    private readonly TaskCompletionSource<object?> _completionSource = new();

    public ILspServices LspServices { get; }

    public string MethodName { get; }

    public object? SerializedRequest { get; }

    private QueueItem(
        string methodName,
        object? serializedRequest,
        ILspServices lspServices,
        ILspLogger logger,
        CancellationToken cancellationToken)
    {
        // Set the tcs state to cancelled if the token gets cancelled outside of our callback (for example the server shutting down).
        cancellationToken.Register(() => _completionSource.TrySetCanceled(cancellationToken));

        _logger = logger;
        SerializedRequest = serializedRequest;
        LspServices = lspServices;

        MethodName = methodName;

        var telemetryService = lspServices.GetService<AbstractTelemetryService>();

        _requestTelemetryScope = telemetryService?.CreateRequestScope(methodName);
    }

    public static (IQueueItem<TRequestContext>, Task<object?>) Create(
        string methodName,
        object? serializedRequest,
        ILspServices lspServices,
        ILspLogger logger,
        CancellationToken cancellationToken)
    {
        var queueItem = new QueueItem<TRequestContext>(
            methodName,
            serializedRequest,
            lspServices,
            logger,
            cancellationToken);

        return (queueItem, queueItem._completionSource.Task);
    }

    public async Task<(TRequestContext, TRequest)?> CreateRequestContextAsync<TRequest>(IMethodHandler handler, RequestHandlerMetadata requestHandlerMetadata, AbstractLanguageServer<TRequestContext> languageServer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _requestTelemetryScope?.RecordExecutionStart();

        // Report to telemetry which handler language we're using for this request.
        _requestTelemetryScope?.RecordHandlerLanguage(requestHandlerMetadata.Language);

        if (!TryDeserializeRequest<TRequest>(languageServer, requestHandlerMetadata, handler.MutatesSolutionState, out var deserializedRequest))
        {
            // Failures are already logged in TryDeserializeRequest, just need to stop processing this item now.
            return null;
        }

        var requestContextFactory = LspServices.GetRequiredService<AbstractRequestContextFactory<TRequestContext>>();
        var context = await requestContextFactory.CreateRequestContextAsync(this, handler, deserializedRequest, cancellationToken).ConfigureAwait(false);
        return (context, deserializedRequest);
    }

    /// <summary>
    /// Deserializes the request into the concrete type.  If the deserialization fails we will fail the request and call TrySetException on the <see cref="_completionSource"/>
    /// so that the client can observe the failure.  If this is a mutating request, we will also let the exception bubble up so that the queue can handle it.
    /// 
    /// The caller is expected to return immediately and stop processing the request if this returns false.
    /// </summary>
    private bool TryDeserializeRequest<TRequest>(
        AbstractLanguageServer<TRequestContext> languageServer,
        RequestHandlerMetadata requestHandlerMetadata,
        bool isMutating,
        [MaybeNullWhen(false)] out TRequest request)
    {
        try
        {
            request = languageServer.DeserializeRequest<TRequest>(SerializedRequest, requestHandlerMetadata);
            // We successfully deserialized, but we have not yet completed the request.  Updating the _completionSource will be handled by StartRequestAsync.
            return true;
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                throw new InvalidOperationException("Cancellation exception is not expected here");
            }

            // Deserialization failed which means the request has failed.  Record the exception and update the _completionSource.

            // Report the exception to logs / telemetry
            _requestTelemetryScope?.RecordException(ex);
            _logger.LogException(ex);

            // Set the task result to the exception
            _completionSource.TrySetException(ex);

            // End the request - the caller will return immediately if it cannot deserialize.
            _requestTelemetryScope?.Dispose();
            _logger.LogEndContext($"{MethodName}");

            // If the request is mutating, bubble the exception out so the queue shuts down.
            if (isMutating)
            {
                throw;
            }
            else
            {
                request = default;
                return false;
            }
        }
    }

    /// <summary>
    /// Processes the queued request. Exceptions will be sent to the task completion source
    /// representing the task that the client is waiting for, then re-thrown so that
    /// the queue can correctly handle them depending on the type of request.
    /// </summary>
    public async Task StartRequestAsync<TRequest, TResponse>(TRequest request, TRequestContext? context, IMethodHandler handler, string language, CancellationToken cancellationToken)
    {
        _requestHandlingStarted = true;
        _logger.LogStartContext($"{MethodName}");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (context is null)
            {
                // If we weren't able to get a corresponding context for this request (for example, we
                // couldn't map a doc request to a particular Document, or we couldn't find an appropriate
                // Workspace for a global operation), then just immediately complete the request with a
                // 'null' response.  Note: the lsp spec was checked to ensure that 'null' is valid for all
                // the requests this could happen for.  However, this assumption may not hold in the future.
                // If that turns out to be the case, we could defer to the individual handler to decide
                // what to do.
                _requestTelemetryScope?.RecordWarning($"Could not get request context for {MethodName}");
                _logger.LogWarning($"Could not get request context for {MethodName}");

                _completionSource.TrySetException(new InvalidOperationException($"Unable to create request context for {MethodName}"));
            }
            else if (handler is null)
            {
                throw new InvalidOperationException($"{nameof(StartRequestAsync)} cannot be called before {nameof(CreateRequestContextAsync)} has been called.");
            }
            else if (handler is IRequestHandler<TRequest, TResponse, TRequestContext> requestHandler)
            {
                var result = await requestHandler.HandleRequestAsync(request, context, cancellationToken).ConfigureAwait(false);

                _completionSource.TrySetResult(result);
            }
            else if (handler is IRequestHandler<TResponse, TRequestContext> parameterlessRequestHandler)
            {
                var result = await parameterlessRequestHandler.HandleRequestAsync(context, cancellationToken).ConfigureAwait(false);

                _completionSource.TrySetResult(result);
            }
            else if (handler is INotificationHandler<TRequest, TRequestContext> notificationHandler)
            {
                await notificationHandler.HandleNotificationAsync(request, context, cancellationToken).ConfigureAwait(false);

                // We know that the return type of <see cref="INotificationHandler{TRequestType, RequestContextType}"/> will always be <see cref="VoidReturn" /> even if the compiler doesn't.
                _completionSource.TrySetResult((TResponse)(object)NoValue.Instance);
            }
            else if (handler is INotificationHandler<TRequestContext> parameterlessNotificationHandler)
            {
                await parameterlessNotificationHandler.HandleNotificationAsync(context, cancellationToken).ConfigureAwait(false);

                // We know that the return type of <see cref="INotificationHandler{TRequestType, RequestContextType}"/> will always be <see cref="VoidReturn" /> even if the compiler doesn't.
                _completionSource.TrySetResult((TResponse)(object)NoValue.Instance);
            }
            else
            {
                throw new NotImplementedException($"Unrecognized {nameof(IMethodHandler)} implementation {handler.GetType()}.");
            }
        }
        catch (OperationCanceledException ex)
        {
            // Record logs + metrics on cancellation.
            _requestTelemetryScope?.RecordCancellation();
            _logger.LogInformation($"{MethodName} - Canceled");

            _completionSource.TrySetCanceled(ex.CancellationToken);
        }
        catch (Exception ex)
        {
            // Record logs and metrics on the exception.
            // It's important that this can NEVER throw, or the queue will hang.
            _requestTelemetryScope?.RecordException(ex);
            _logger.LogException(ex);

            _completionSource.TrySetException(ex);
        }
        finally
        {
            _requestTelemetryScope?.Dispose();
            _logger.LogEndContext($"{MethodName}");
        }

        // Return the result of this completion source to the caller
        // so it can decide how to handle the result / exception.
        await _completionSource.Task.ConfigureAwait(false);
    }

    public void FailRequest(string message)
    {
        // This is not valid to call after StartRequestAsync starts as they both access the same state.
        // StartRequestAsync handles any failures internally once it runs.
        if (_requestHandlingStarted)
        {
            throw new InvalidOperationException("Cannot manually fail queue item after it has started");
        }
        var exception = new Exception(message);
        _requestTelemetryScope?.RecordException(exception);
        _logger.LogException(exception);

        _completionSource.TrySetException(exception);
        _requestTelemetryScope?.Dispose();
    }
}
