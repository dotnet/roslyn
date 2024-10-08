// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// An item to be queued for execution.
/// </summary>
/// <typeparam name="TRequestContext">The type of the request context to be passed along to the handler.</typeparam>
internal interface IQueueItem<TRequestContext>
{
    /// <summary>
    /// Executes the work specified by this queue item.
    /// </summary>
    /// <param name="request">The request parameters.</param>
    /// <param name="context">The context created by <see cref="CreateRequestContextAsync{TRequest}(IMethodHandler, RequestHandlerMetadata, AbstractLanguageServer{TRequestContext}, CancellationToken)"/>.</param>
    /// <param name="handler">The handler to use to execute the request.</param>
    /// <param name="language">The language for the request.</param>
    /// <param name="cancellationToken" />
    /// <returns>A <see cref="Task "/> which completes when the request has finished.</returns>
    Task StartRequestAsync<TRequest, TResponse>(TRequest request, TRequestContext? context, IMethodHandler handler, string language, CancellationToken cancellationToken);

    /// <summary>
    /// Creates the context that is sent to the handler for this queue item.
    /// Note - this method is always called serially inside the queue before
    /// running the actual request in <see cref="StartRequestAsync{TRequest, TResponse}(TRequest, TRequestContext?, IMethodHandler, string, CancellationToken)"/>
    /// Throwing in this method will cause the server to shutdown.
    /// 
    /// If there was a recoverable failure in creating the request, this will return null and the caller should stop processing the request.
    /// </summary>
    Task<(TRequestContext, TRequest)?> CreateRequestContextAsync<TRequest>(IMethodHandler handler, RequestHandlerMetadata requestHandlerMetadata, AbstractLanguageServer<TRequestContext> languageServer, CancellationToken cancellationToken);

    /// <summary>
    /// Provides access to LSP services.
    /// </summary>
    ILspServices LspServices { get; }

    /// <summary>
    /// The method being executed.
    /// </summary>
    string MethodName { get; }

    object? SerializedRequest { get; }
}
