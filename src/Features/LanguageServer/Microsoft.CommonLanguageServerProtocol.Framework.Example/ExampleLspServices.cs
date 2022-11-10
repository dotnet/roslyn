// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System;

namespace Microsoft.CommonLanguageServerProtocol.Framework.Example;

internal class ExampleLspServices : ILspServices
{
    private readonly IServiceProvider _serviceProvider;

    public ExampleLspServices(IServiceCollection serviceCollection)
    {
        _ = serviceCollection.AddSingleton<ILspServices>(this);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        _serviceProvider = serviceProvider;
    }

    public T GetRequiredService<T>() where T : notnull
    {
        var service = _serviceProvider.GetRequiredService<T>();

        return service;
    }

    public object? TryGetService(Type type)
    {
        var obj = _serviceProvider.GetService(type);

        return obj;
    }

    public IEnumerable<TService> GetServices<TService>()
    {
        return _serviceProvider.GetServices<TService>();
    }

    public void Dispose()
    {
    }

    public IEnumerable<T> GetRequiredServices<T>()
    {
        var services = _serviceProvider.GetServices<T>();

        return services;
    }

    public ImmutableArray<Type> GetRegisteredServices()
    {
        throw new NotImplementedException();
    }

    public bool SupportsGetRegisteredServices()
    {
        return false;
    }
}
