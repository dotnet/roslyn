// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    [NonCopyable]
    internal readonly struct PooledDisposer<TPoolable> : IDisposable
        where TPoolable : class, IPooled
    {
        private readonly TPoolable _pooledObject;

        public PooledDisposer(TPoolable instance)
            => _pooledObject = instance;

        void IDisposable.Dispose()
            => _pooledObject?.Free();
    }
}
