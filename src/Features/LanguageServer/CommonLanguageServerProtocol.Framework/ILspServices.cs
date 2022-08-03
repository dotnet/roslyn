// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CommonLanguageServerProtocol.Framework;

#nullable enable

public interface ILspServices : IDisposable
{
    T GetRequiredService<T>() where T : notnull;

    bool TryGetService(Type @type, out object? service);

    IEnumerable<T> GetRequiredServices<T>();

    ImmutableArray<Type> GetRegisteredServices();

    bool SupportsGetRegisteredServices();

    bool SupportsGetRequiredServices();
}
