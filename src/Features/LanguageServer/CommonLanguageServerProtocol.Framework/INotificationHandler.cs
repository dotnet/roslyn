// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace CommonLanguageServerProtocol.Framework;

public interface INotificationHandler<RequestContextType> : IRequestHandler
{
    Task HandleNotificationAsync(RequestContextType requestContext, CancellationToken cancellationToken);
}

public interface INotificationHandler<RequestType, RequestContextType> : IRequestHandler
{
    Task HandleNotificationAsync(RequestType request, RequestContextType requestContext, CancellationToken cancellationToken);
}
