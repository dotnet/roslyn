// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace CommonLanguageServerProtocol.Framework;

public interface IRequestExecutionQueue<RequestContextType>
{
    Task<TResponseType> ExecuteAsync<TRequestType, TResponseType>(bool mutatesSolutionState, bool requiresLSPSolution, IRequestHandler<TRequestType, TResponseType, RequestContextType> handler, TRequestType? request, string methodName, ILspServices lspServices, CancellationToken cancellationToken);

    Task ExecuteAsync<TRequestType>(bool mutatesSolutionState, bool requiresLSPSolution, INotificationHandler<TRequestType, RequestContextType> handler, TRequestType? request, string methodName, ILspServices lspServices, CancellationToken cancellationToken);

    Task ExecuteAsync(bool mutatesSolutionState, bool requiresLSPSolution, INotificationHandler<RequestContextType> handler, string methodName, ILspServices lspServices, CancellationToken cancellationToken);

    void Start(ILspServices lspServices);

    void Shutdown();

    event EventHandler<RequestShutdownEventArgs>? RequestServerShutdown;
}
