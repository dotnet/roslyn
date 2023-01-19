// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// An item to be queued for execution.
/// </summary>
/// <typeparam name="TRequestContext">The type of the request context to be passed along to the handler.</typeparam>
public interface IQueueItem<TRequestContext>
{
    /// <summary>
    /// Executes the work specified by this queue item.
    /// </summary>
    /// <param name="requestContext">the context created by <see cref="CreateRequestContextAsync(CancellationToken)"/></param>
    /// <param name="cancellationToken" />
    /// <returns>A <see cref="Task "/> which completes when the request has finished.</returns>
    Task StartRequestAsync(TRequestContext? requestContext, CancellationToken cancellationToken);

    /// <summary>
    /// Creates the context that is sent to the handler for this queue item.
    /// Note - this method is always called serially inside the queue before
    /// running the actual request in <see cref="StartRequestAsync(TRequestContext?, CancellationToken)"/>
    /// Throwing in this method will cause the server to shutdown.
    /// </summary>
    Task<TRequestContext?> CreateRequestContextAsync(CancellationToken cancellationToken);

    ILspServices LspServices { get; }

    /// <summary>
    /// Indicates that this request may mutate the server state, so that the queue may handle its execution appropriatly.
    /// </summary>
    bool MutatesServerState { get; }

    /// <summary>
    /// The method being executed.
    /// </summary>
    string MethodName { get; }

    /// <summary>
    /// The handler which will run this operation.
    /// </summary>
    IMethodHandler MethodHandler { get; }
}
