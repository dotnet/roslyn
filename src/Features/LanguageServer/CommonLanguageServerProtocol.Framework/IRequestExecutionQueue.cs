// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace CommonLanguageServerProtocol.Framework;

public interface IRequestExecutionQueue<RequestContextType> where RequestContextType : struct
{
    Task<RequestContextType?> CreateRequestContextAsync(IQueueItem<RequestContextType> queueItem, CancellationToken cancellationToken);

    event EventHandler<RequestShutdownEventArgs> RequestServerShutdown;

    Task<TResponseType> ExecuteAsync<TRequestType, TResponseType>(
        bool mutatesSolutionState,
        bool requiresLSPSolution,
        IRequestHandler<TRequestType, TResponseType, RequestContextType> handler,
        TRequestType request,
        ClientCapabilities clientCapabilities,
        string methodName,
        CancellationToken cancellationToken);

    void Shutdown();
}
