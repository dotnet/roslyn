// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

internal class TestLspServices : ILspServices
{
    private readonly bool _supportsGetRegisteredServices;
    private readonly IEnumerable<(Type, object)> _services;

    public TestLspServices(IEnumerable<(Type, object)> services, bool supportsGetRegisteredServices)
    {
        _services = services;
        _supportsGetRegisteredServices = supportsGetRegisteredServices;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public ImmutableArray<Type> GetRegisteredServices()
    {
        var types = new List<Type>();
        foreach (var service in _services)
        {
            types.Add(service.Item2.GetType());
        }

        return types.ToImmutableArray();
    }

    public T GetRequiredService<T>() where T : notnull
    {
        var service = (T?)TryGetService(typeof(T));
        if (service is null)
            throw new InvalidOperationException($"{typeof(T).Name} did not have a service");

        return service;
    }

    public IEnumerable<T> GetRequiredServices<T>()
    {
        var services = _services.Where(s => !_supportsGetRegisteredServices && s.Item2 is IMethodHandler).Select(s => (T)s.Item2);
        return services;
    }

    public bool SupportsGetRegisteredServices()
    {
        return _supportsGetRegisteredServices;
    }

    public object? TryGetService(Type type)
    {
        var service = _services.FirstOrDefault(s => (_supportsGetRegisteredServices ? s.Item2.GetType() : s.Item1) == type);

        return service.Item2;
    }
}
