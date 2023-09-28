// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

internal class TestHandlerWithLanguageProvider : IHandlerProvider
{
    private readonly IEnumerable<(RequestHandlerMetadata metadata, IMethodHandler provider, string? language)> _providers;

    public TestHandlerWithLanguageProvider(IEnumerable<(RequestHandlerMetadata metadata, IMethodHandler provider, string? language)> providers)
        => _providers = providers;

    public ImmutableArray<Lazy<IMethodHandler, string?>> GetMethodHandlers(string method, Type? requestType, Type? responseType)
        => _providers.Where(p => p.metadata.MethodName == method).Select(p => new Lazy<IMethodHandler, string?>(() => p.provider, p.language)).ToImmutableArray();

    public ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods()
        => _providers.Select(p => p.metadata).ToImmutableArray();
}
