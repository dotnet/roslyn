// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.PooledObjects
{
    internal partial class PooledStringBuilder : IPooled
    {
        public static PooledDisposer<PooledStringBuilder> GetInstance(out PooledStringBuilder instance)
        {
            instance = GetInstance();
            return new PooledDisposer<PooledStringBuilder>(instance);
        }
    }
}
