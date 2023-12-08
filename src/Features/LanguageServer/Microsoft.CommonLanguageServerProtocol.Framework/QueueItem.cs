// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
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

internal class QueueItem<TRequest, TResponse, TRequestContext> : IQueueItem<TRequestContext>
{
    private readonly ILspLogger _logger;

    private readonly TRequest _request;
    private readonly IMethodHandler _handler;

    /// <summary>
    /// A task completion source representing the result of this queue item's work.
    /// This is the task that the client is waiting on.
    /// </summary>
    private readonly TaskCompletionSource<TResponse> _completionSource = new();

    public ILspServices LspServices { get; }

    public bool MutatesServerState { get; }

    public string MethodName { get; }

    public IMethodHandler MethodHandler { get; }

    private QueueItem(
        bool mutatesSolutionState,
        string methodName,
        IMethodHandler methodHandler,
        TRequest request,
        IMethodHandler handler,
        ILspServices lspServices,
        ILspLogger logger,
        CancellationToken cancellationToken)
    {
        // Set the tcs state to cancelled if the token gets cancelled outside of our callback (for example the server shutting down).
        cancellationToken.Register(() => _completionSource.TrySetCanceled(cancellationToken));

        _handler = handler;
        _logger = logger;
        _request = request;
        LspServices = lspServices;
        MethodHandler = methodHandler;

        MutatesServerState = mutatesSolutionState;
        MethodName = methodName;
    }

    public static (IQueueItem<TRequestContext>, Task<TResponse>) Create(
        bool mutatesSolutionState,
        string methodName,
        IMethodHandler methodHandler,
        TRequest request,
        IMethodHandler handler,
        ILspServices lspServices,
        ILspLogger logger,
        CancellationToken cancellationToken)
    {
        var queueItem = new QueueItem<TRequest, TResponse, TRequestContext>(
            mutatesSolutionState,
            methodName,
            methodHandler,
            request,
            handler,
            lspServices,
            logger,
            cancellationToken);

        return (queueItem, queueItem._completionSource.Task);
    }

    public async Task<TRequestContext?> CreateRequestContextAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var requestContextFactory = LspServices.GetRequiredService<IRequestContextFactory<TRequestContext>>();
        var context = await requestContextFactory.CreateRequestContextAsync(this, _request, cancellationToken).ConfigureAwait(false);
        return context;
    }

    /// <summary>
    /// Processes the queued request. Exceptions will be sent to the task completion source
    /// representing the task that the client is waiting for, then re-thrown so that
    /// the queue can correctly handle them depending on the type of request.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>The result of the request.</returns>
    public async Task StartRequestAsync(TRequestContext? context, CancellationToken cancellationToken)
    {
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
                _logger.LogWarning($"Could not get request context for {MethodName}");
                _completionSource.TrySetException(new InvalidOperationException($"Unable to create request context for {MethodName}"));
            }
            else if (_handler is IRequestHandler<TRequest, TResponse, TRequestContext> requestHandler)
            {
                var result = await requestHandler.HandleRequestAsync(_request, context, cancellationToken).ConfigureAwait(false);

                _completionSource.TrySetResult(result);
            }
            else if (_handler is IRequestHandler<TResponse, TRequestContext> parameterlessRequestHandler)
            {
                var result = await parameterlessRequestHandler.HandleRequestAsync(context, cancellationToken).ConfigureAwait(false);

                _completionSource.TrySetResult(result);
            }
            else if (_handler is INotificationHandler<TRequest, TRequestContext> notificationHandler)
            {
                await notificationHandler.HandleNotificationAsync(_request, context, cancellationToken).ConfigureAwait(false);

                // We know that the return type of <see cref="INotificationHandler{TRequestType, RequestContextType}"/> will always be <see cref="VoidReturn" /> even if the compiler doesn't.
                _completionSource.TrySetResult((TResponse)(object)NoValue.Instance);
            }
            else if (_handler is INotificationHandler<TRequestContext> parameterlessNotificationHandler)
            {
                await parameterlessNotificationHandler.HandleNotificationAsync(context, cancellationToken).ConfigureAwait(false);

                // We know that the return type of <see cref="INotificationHandler{TRequestType, RequestContextType}"/> will always be <see cref="VoidReturn" /> even if the compiler doesn't.
                _completionSource.TrySetResult((TResponse)(object)NoValue.Instance);
            }
            else
            {
                throw new NotImplementedException($"Unrecognized {nameof(IMethodHandler)} implementation {_handler.GetType()}. ");
            }
        }
        catch (OperationCanceledException ex)
        {
            // Record logs + metrics on cancellation.
            _logger.LogInformation($"{MethodName} - Canceled");

            _completionSource.TrySetCanceled(ex.CancellationToken);
        }
        catch (Exception ex)
        {
            // Record logs and metrics on the exception.
            // It's important that this can NEVER throw, or the queue will hang.
            _logger.LogException(ex);

            _completionSource.TrySetException(ex);
        }
        finally
        {
            _logger.LogEndContext($"{MethodName}");
        }

        // Return the result of this completion source to the caller
        // so it can decide how to handle the result / exception.
        await _completionSource.Task.ConfigureAwait(false);
    }
}
