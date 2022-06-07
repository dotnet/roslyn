// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal abstract partial class AbstractCSharpReducer : AbstractReducer
    {
        protected AbstractCSharpReducer(ObjectPool<IReductionRewriter> pool) : base(pool)
        {
        }

        public sealed override bool IsApplicable(SimplifierOptions options)
            => IsApplicable((CSharpSimplifierOptions)options);

        protected abstract bool IsApplicable(CSharpSimplifierOptions options);
    }
}
