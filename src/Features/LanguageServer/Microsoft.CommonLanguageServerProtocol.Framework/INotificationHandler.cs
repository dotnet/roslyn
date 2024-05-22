// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// An interface for handlers of methods which do not return a response and receive no parameters.
/// </summary>
/// <typeparam name="TRequestContext">The type of the RequestContext to be used by this handler.</typeparam>
#if BINARY_COMPAT // TODO - Remove with https://github.com/dotnet/roslyn/issues/72251
public interface INotificationHandler<TRequestContext> : IMethodHandler
#else
internal interface INotificationHandler<TRequestContext> : IMethodHandler
#endif
{
    Task HandleNotificationAsync(TRequestContext requestContext, CancellationToken cancellationToken);
}

/// <summary>
/// An interface for handlers of methods which do not return a response 
/// </summary>
/// <typeparam name="TRequest">The type of the Request parameter to be received.</typeparam>
/// <typeparam name="TRequestContext">The type of the RequestContext to be used by this handler.</typeparam>
#if BINARY_COMPAT // TODO - Remove with https://github.com/dotnet/roslyn/issues/72251
public interface INotificationHandler<TRequest, TRequestContext> : IMethodHandler
#else
internal interface INotificationHandler<TRequest, TRequestContext> : IMethodHandler
#endif
{
    Task HandleNotificationAsync(TRequest request, TRequestContext requestContext, CancellationToken cancellationToken);
}
