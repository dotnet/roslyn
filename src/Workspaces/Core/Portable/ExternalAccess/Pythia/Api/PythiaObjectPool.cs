// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal static class PythiaObjectPool
    {
        public static IDisposable GetInstance<T>(out T instance) where T : class, new()
        {
            var disposer = Default<T>().GetPooledObject<T>();
            instance = disposer.Object;
            return disposer;
        }

        private static ObjectPool<T> Default<T>() where T : class, new()
        {
            return DefaultNormalPool<T>.Instance;
        }

        private static class DefaultNormalPool<T> where T : class, new()
        {
            public static readonly ObjectPool<T> Instance = new ObjectPool<T>(() => new T(), 20);
        }
    }
}
