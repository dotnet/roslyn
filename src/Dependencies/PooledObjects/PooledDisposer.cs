// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.PooledObjects;

internal readonly partial struct PooledDisposer<TPoolable>(TPoolable instance) : IDisposable
    where TPoolable : class, IPooled
{
    void IDisposable.Dispose()
        => instance?.Free();
}
