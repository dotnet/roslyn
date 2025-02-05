// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Microsoft.CodeAnalysis.PooledObjects;

internal sealed partial class PooledStringBuilder : IPooled
{
    public static PooledDisposer<PooledStringBuilder> GetInstance(out StringBuilder instance)
    {
        var pooledInstance = GetInstance();
        instance = pooledInstance;
        return new PooledDisposer<PooledStringBuilder>(pooledInstance);
    }

    void IPooled.Free(bool discardLargeInstances)
    {
        // If we're discarding large instances, use the default behavior (which already does that).  Otherwise, always
        // clear and free the instance back to its originating pool.
        if (discardLargeInstances)
        {
            Free();
        }
        else
        {
            this.Builder.Clear();
            _pool.Free(this);
        }
    }
}
