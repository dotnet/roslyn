// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;
using System.Collections.Immutable;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Wraps an <see cref="IHandlerProvider"/>.
/// </summary>
internal sealed class WrappedHandlerProvider : AbstractHandlerProvider
{
    private readonly IHandlerProvider _handlerProvider;

    public WrappedHandlerProvider(IHandlerProvider handlerProvider)
    {
        _handlerProvider = handlerProvider;
    }

    public override IMethodHandler GetMethodHandler(string method, Type? requestType, Type? responseType, string language)
        => _handlerProvider.GetMethodHandler(method, requestType, responseType);

    public override ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods()
        => _handlerProvider.GetRegisteredMethods();
}
