// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api;

internal static class PythiaObjectPool
{
    [Obsolete("Use specific GetInstance overloads")]
    public static IDisposable GetInstance<T>(out T instance) where T : class, new()
    {
        var disposer = Default<T>().GetPooledObject<T>();
        instance = disposer.Object;
        return disposer;
    }

    public static IDisposable GetInstance<T>(out Stack<T> instance)
    {
        var disposer = Default<Stack<T>>().GetPooledObject();
        instance = disposer.Object;
        return disposer;
    }

    public static IDisposable GetInstance<T>(out HashSet<T> instance)
    {
        var disposer = Default<HashSet<T>>().GetPooledObject();
        instance = disposer.Object;
        return disposer;
    }

    private static ObjectPool<T> Default<T>() where T : class, new()
        => DefaultNormalPool<T>.Instance;

    private static class DefaultNormalPool<T> where T : class, new()
    {
        public static readonly ObjectPool<T> Instance = new(() => new T(), 20);
    }
}
