// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

internal abstract class TestLspServices(IEnumerable<(Type type, object instance)> services) : ILspServices
{
    protected readonly IEnumerable<(Type type, object instance)> Services = services;

    public static ILspServices Create(IEnumerable<(Type type, object instance)> services, bool supportsMethodHandlerProvider)
    {
        return supportsMethodHandlerProvider
            ? new WithMethodHandlerProvider(services)
            : new Default(services);
    }

    public void Dispose()
    {
    }

    public T? GetService<T>() where T : notnull
        => (T?)TryGetService(typeof(T));

    public T GetRequiredService<T>() where T : notnull
        => (T?)TryGetService(typeof(T)) ?? throw new InvalidOperationException($"{typeof(T).Name} did not have a service");

    public virtual IEnumerable<T> GetRequiredServices<T>()
        => Services.Where(s => s.instance is T).Select(s => (T)s.instance);

    public virtual object? TryGetService(Type type)
        => Services.FirstOrDefault(s => s.type == type).instance;

    private sealed class Default(IEnumerable<(Type type, object instance)> services) : TestLspServices(services)
    {
    }

    private sealed class WithMethodHandlerProvider(IEnumerable<(Type type, object instance)> services)
        : TestLspServices(services), IMethodHandlerProvider
    {
        public ImmutableArray<(TypeRef HandlerTypeRef, ImmutableArray<MethodHandlerDescriptor> Descriptors)> GetMethodHandlers()
            => Services.Select(s => s.instance.GetType())
                       .Where(t => typeof(IMethodHandler).IsAssignableFrom(t))
                       .Select(t => (TypeRef.From(t.AssemblyQualifiedName), MethodHandlerDescriptor.From(t)))
                       .ToImmutableArray();

        public override IEnumerable<T> GetRequiredServices<T>() => [];

        public override object? TryGetService(Type type)
            => Services.FirstOrDefault(s => s.instance.GetType() == type).instance;
    }
}
