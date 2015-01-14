// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Simplification
{
    internal abstract partial class AbstractReducer
    {
        internal interface IExpressionRewriter
        {
            SyntaxNodeOrToken VisitNodeOrToken(SyntaxNodeOrToken nodeOrTokenToReduce, SemanticModel semanticModel, bool simplifyAllDescendants);

            bool HasMoreWork { get; }
        }
    }
}
