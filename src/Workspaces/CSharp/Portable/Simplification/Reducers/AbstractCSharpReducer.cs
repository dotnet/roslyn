// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal abstract partial class AbstractCSharpReducer : AbstractReducer
    {
        protected AbstractCSharpReducer(ObjectPool<IReductionRewriter> pool) : base(pool)
        {
        }
    }
}
