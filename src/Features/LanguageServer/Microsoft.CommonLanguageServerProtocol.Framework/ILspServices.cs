// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

public interface ILspServices : IDisposable
{
    T GetRequiredService<T>() where T : notnull;

    object? TryGetService(Type @type);

    IEnumerable<T> GetRequiredServices<T>();

    // TODO: https://github.com/dotnet/roslyn/issues/63555
    // These two methods should ideally be removed, but that would required
    // Roslyn to allow non-lazy creation of IMethodHandlers which they currently cannot
    ImmutableArray<Type> GetRegisteredServices();

    bool SupportsGetRegisteredServices();
}
