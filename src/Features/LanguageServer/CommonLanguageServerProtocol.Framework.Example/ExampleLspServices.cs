// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonLanguageServerProtocol.Framework;
using System;
using System.Collections;
using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace CommonLanguageServerProtocol.Framework.Example;

internal class ExampleLspServices : ILspServices
{
    private readonly IServiceProvider _serviceProvider;

    public ExampleLspServices(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public T GetRequiredService<T>()
    {
        var service = _serviceProvider.GetRequiredService<T>();

        return service;
    }

    public bool TryGetService(Type type, out object? obj)
    {
        obj = _serviceProvider.GetService(type);

        return obj is not null;
    }

    public ImmutableArray<Type> GetRegisteredServices()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
