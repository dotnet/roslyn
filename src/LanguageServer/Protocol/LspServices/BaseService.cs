// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Simple helper class to hold a service instance or a lazily-created service.
/// </summary>
internal abstract class BaseService
{
    public abstract Type Type { get; }
    public abstract object GetInstance(ILspServices lspServices);

    public static BaseService Create<T>(T instance)
        where T : class
        => new ConcreteService<T>(instance);

    public static BaseService CreateLazily<T>(Func<ILspServices, T> creator)
        where T : class
        => new LazyService<T>(creator);

    private sealed class ConcreteService<T>(T instance) : BaseService
        where T : class
    {
        public override Type Type => typeof(T);
        public override object GetInstance(ILspServices lspServices) => instance;
    }

    private sealed class LazyService<T>(Func<ILspServices, T> creator) : BaseService
        where T : class
    {
        private readonly object _gate = new();
        private readonly Func<ILspServices, T> _creator = creator;
        private T? _instance;

        public override Type Type => typeof(T);

        public override object GetInstance(ILspServices lspServices)
        {
            lock (_gate)
            {
                return _instance ??= _creator(lspServices);
            }
        }
    }
}
