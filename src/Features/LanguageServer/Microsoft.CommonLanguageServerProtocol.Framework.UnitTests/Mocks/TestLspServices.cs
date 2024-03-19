// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

internal class TestLspServices : ILspServices
{
    private readonly bool _supportsGetRegisteredServices;
    private readonly IEnumerable<(Type type, object instance)> _services;

    public TestLspServices(IEnumerable<(Type type, object instance)> services, bool supportsGetRegisteredServices)
    {
        _services = services;
        _supportsGetRegisteredServices = supportsGetRegisteredServices;
    }

    public void Dispose()
    {
    }

    public ImmutableArray<Type> GetRegisteredServices()
        => _services.Select(s => s.instance.GetType()).ToImmutableArray();

    public T GetRequiredService<T>() where T : notnull
        => (T?)TryGetService(typeof(T)) ?? throw new InvalidOperationException($"{typeof(T).Name} did not have a service");

    public IEnumerable<T> GetRequiredServices<T>()
        => _supportsGetRegisteredServices ? Array.Empty<T>() : _services.Where(s => s.instance is T).Select(s => (T)s.instance);

    public bool SupportsGetRegisteredServices()
        => _supportsGetRegisteredServices;

    public object? TryGetService(Type type)
        => _services.FirstOrDefault(s => (_supportsGetRegisteredServices ? s.instance.GetType() : s.type) == type).instance;
}
