// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Queues requests to be executed in the proper order.
/// </summary>
/// <typeparam name="TRequestContext">The type of the RequestContext to be used by the handler.</typeparam>
internal interface IRequestExecutionQueue<TRequestContext> : IAsyncDisposable
{
    /// <summary>
    /// Queue a request.
    /// </summary>
    /// <returns>A task that completes when the handler execution is done.</returns>
    Task<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request, string methodName, string languageName, ILspServices lspServices, CancellationToken cancellationToken);

    /// <summary>
    /// Start the queue accepting requests once any event handlers have been attached.
    /// </summary>
    void Start();
}
