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

    public IMethodHandler GetMethodHandler(string method, Type? requestType, Type? responseType, string? language = null)
        => _providers.Single(p => p.metadata.MethodName == method && p.metadata.Language == language).provider;

    public ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods()
        => _providers.Select(p => p.metadata).ToImmutableArray();
}
