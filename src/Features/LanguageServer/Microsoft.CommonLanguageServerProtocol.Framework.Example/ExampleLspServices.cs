// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

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

    public T? GetService<T>() where T : notnull
    {
        return TryGetService(typeof(T), out var service)
            ? (T)service
            : default;
    }

    public T GetRequiredService<T>() where T : notnull
    {
        var service = _serviceProvider.GetRequiredService<T>();

        return service;
    }

    public bool TryGetService(Type type, [NotNullWhen(true)] out object? service)
    {
        service = _serviceProvider.GetService(type);

        return service is not null;
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
}
