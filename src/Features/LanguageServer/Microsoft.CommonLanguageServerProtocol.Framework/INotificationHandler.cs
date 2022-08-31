// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// An interface for handlers of methods which do not return a response and receive no parameters.
/// </summary>
/// <typeparam name="RequestContextType">The type of the RequestContext to be used by this handler.</typeparam>
public interface INotificationHandler<RequestContextType> : IMethodHandler
{
    Task HandleNotificationAsync(RequestContextType requestContext, CancellationToken cancellationToken);
}

/// <summary>
/// An interface for handlers of methods which do not return a response 
/// </summary>
/// <typeparam name="RequestType">The type of the Request parameter to be received.</typeparam>
/// <typeparam name="RequestContextType">The type of the RequestContext to be used by this handler.</typeparam>
public interface INotificationHandler<RequestType, RequestContextType> : IMethodHandler
{
    Task HandleNotificationAsync(RequestType request, RequestContextType requestContext, CancellationToken cancellationToken);
}
