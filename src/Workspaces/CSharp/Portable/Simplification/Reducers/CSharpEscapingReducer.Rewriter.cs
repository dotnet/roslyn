// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpEscapingReducer
    {
        private class Rewriter : AbstractReductionRewriter
        {
            public Rewriter(ObjectPool<IReductionRewriter> pool)
                : base(pool)
            {
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                var newToken = base.VisitToken(token);
                return SimplifyToken(newToken, s_simplifyIdentifierToken);
            }
        }
    }
}
