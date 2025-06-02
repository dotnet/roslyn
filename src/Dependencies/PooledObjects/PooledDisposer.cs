// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

#if !MICROSOFT_CODEANALYSIS_POOLEDOBJECTS_NO_POOLED_DISPOSER
using System;

namespace Microsoft.CodeAnalysis.PooledObjects;

internal readonly partial struct PooledDisposer<TPoolable>(
    TPoolable instance,
    bool discardLargeInstances = true) : IDisposable
    where TPoolable : class, IPooled
{
    void IDisposable.Dispose()
        => instance?.Free(discardLargeInstances);
}
#endif
