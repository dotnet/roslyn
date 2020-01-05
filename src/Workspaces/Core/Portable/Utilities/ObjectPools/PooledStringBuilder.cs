// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
