// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Simplification
{
    internal abstract partial class AbstractReducer
    {
        private readonly ObjectPool<IReductionRewriter> _pool;

        protected AbstractReducer(ObjectPool<IReductionRewriter> pool)
            => _pool = pool;

        public IReductionRewriter GetOrCreateRewriter()
            => _pool.Allocate();

        public abstract bool IsApplicable(SimplifierOptions options);
    }
}
