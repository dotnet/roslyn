// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
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

        public virtual bool IsApplicable(OptionSet optionSet) => true;
    }
}
