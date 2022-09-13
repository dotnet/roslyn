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
    /// <param name="cancellationToken" />
    /// <returns>A <see cref="Task "/> which completes when the request has finished.</returns>
    Task StartRequestAsync(CancellationToken cancellationToken);

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
