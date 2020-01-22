// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
