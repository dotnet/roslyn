// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace CommonLanguageServerProtocol.Framework;

public interface IRequestDispatcher<RequestContextType> where RequestContextType : struct
{
    ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods();

    Task<TResponseType?> ExecuteRequestAsync<TRequestType, TResponseType>(
        string methodName,
        TRequestType request,
        ClientCapabilities clientCapabilities,
        IRequestExecutionQueue<RequestContextType> queue,
        CancellationToken cancellationToken);
}
