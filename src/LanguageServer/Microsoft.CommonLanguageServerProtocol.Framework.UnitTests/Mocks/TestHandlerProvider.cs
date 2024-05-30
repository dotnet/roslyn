// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

internal class TestHandlerProvider : AbstractHandlerProvider
{
    private readonly IEnumerable<(RequestHandlerMetadata metadata, IMethodHandler provider)> _providers;

    public TestHandlerProvider(IEnumerable<(RequestHandlerMetadata metadata, IMethodHandler provider)> providers)
        => _providers = providers;

    public override IMethodHandler GetMethodHandler(string method, TypeRef? requestType, TypeRef? responseType, string language)
        => _providers.Single(p => p.metadata.MethodName == method && p.metadata.Language == language).provider;

    public override ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods()
        => _providers.Select(p => p.metadata).ToImmutableArray();
}
