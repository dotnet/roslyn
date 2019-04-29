// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class BoundExpressionWithNullability : BoundExpression
    {
        public BoundExpressionWithNullability(SyntaxNode syntax, BoundExpression expression, NullableAnnotation nullableAnnotation, TypeSymbol type)
            : this(syntax, expression, nullableAnnotation, type, hasErrors: false)
        {
            IsSuppressed = expression.IsSuppressed;
        }
    }
}

