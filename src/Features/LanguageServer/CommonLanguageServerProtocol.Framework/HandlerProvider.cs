// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CommonLanguageServerProtocol.Framework;

/// <inheritdoc/>
internal class HandlerProvider : IHandlerProvider
{
    public HandlerProvider(ILspServices lspServices)
    {
    }

    /// <summary>
    /// Get the MethodHandler for a particular request.
    /// </summary>
    /// <param name="method">The method name being made.</param>
    /// <param name="requestType">The requestType for this method.</param>
    /// <param name="responseType">The responseType for this method.</param>
    /// <returns>The handler for this request.</returns>
    public IMethodHandler GetMethodHandler(string method, Type? requestType, Type? responseType)
    {
        var requestHandlerMetadata = new RequestHandlerMetadata(method, requestType, responseType);

        var requestHandlers = GetRequestHandlers();
        var handler = requestHandlers[requestHandlerMetadata].Value;

        return handler;
    }

    public ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods()
    {
        var requestHandlers = GetRequestHandlers();
        return requestHandlers.Keys.ToImmutableArray();
    }
}
