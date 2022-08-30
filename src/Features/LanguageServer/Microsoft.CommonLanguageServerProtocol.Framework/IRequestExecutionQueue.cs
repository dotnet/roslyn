// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Queue's requests to be Executed in the proper order.
/// </summary>
/// <typeparam name="RequestContextType">The type of the RequestContext to be used by the handler.</typeparam>
public interface IRequestExecutionQueue<RequestContextType> : IAsyncDisposable
{
    ///// <summary>
    ///// Queue a request for a notifcation with a parameter.
    ///// </summary>
    ///// <typeparam name="TRequestType"></typeparam>
    ///// <returns>A task that completes when the handler execution is done.</returns>
    //Task ExecuteAsync<TRequestType>(TRequestType? request, string methodName, ILspServices lspServices, CancellationToken cancellationToken);

    ///// <summary>
    ///// Queue a request for a parameterless notification.
    ///// </summary>
    ///// <returns>A task that completes when the handler execution is done.</returns>
    //Task ExecuteAsync(string methodName, ILspServices lspServices, CancellationToken cancellationToken);

    /// <summary>
    /// Queue a request.
    /// </summary>
    /// <returns>A task that completes when the handler execution is done.</returns>
    Task<TResponseType> ExecuteAsync<TRequestType, TResponseType>(TRequestType request, string methodName, ILspServices lspServices, CancellationToken cancellationToken);

    /// <summary>
    /// Start the queue accepting requests once any event handlers have been attached.
    /// </summary>
    void Start();

    /// <summary>
    /// Raised when the execution queue has failed, or the solution state its tracking is in an unknown state
    /// and so the only course of action is to shutdown the server so that the client re-connects and we can
    /// start over again.
    /// </summary>
    event EventHandler<RequestShutdownEventArgs>? RequestServerShutdown;
}
