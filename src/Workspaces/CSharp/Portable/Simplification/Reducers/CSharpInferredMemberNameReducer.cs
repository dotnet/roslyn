// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    /// <summary>
    /// Complexify makes inferred names explicit for tuple elements and anonymous type members. This
    /// class considers which ones of those can be simplified (after the refactoring was done).
    /// If the inferred name of the member matches, the explicit name (from Complexify) can be removed.
    /// </summary>
    internal partial class CSharpInferredMemberNameReducer : AbstractCSharpReducer
    {
        private static readonly ObjectPool<IReductionRewriter> s_pool = new(
            () => new Rewriter(s_pool));

        public CSharpInferredMemberNameReducer() : base(s_pool)
        {
        }

        protected override bool IsApplicable(CSharpSimplifierOptions options)
            => true;
    }
}
