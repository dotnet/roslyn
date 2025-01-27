// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal partial class CSharpDefaultExpressionReducer : AbstractCSharpReducer
{
    private static readonly ObjectPool<IReductionRewriter> s_pool = new(static () => new Rewriter(s_pool));

    public CSharpDefaultExpressionReducer() : base(s_pool)
    {
    }

    protected override bool IsApplicable(CSharpSimplifierOptions options)
       => true;
}
