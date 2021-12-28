// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class BoundExpressionWithNullability : BoundExpression
    {
        public BoundExpressionWithNullability(SyntaxNode syntax, BoundExpression expression, NullableAnnotation nullableAnnotation, TypeSymbol? type)
            : this(syntax, expression, nullableAnnotation, type, hasErrors: false)
        {
            IsSuppressed = expression.IsSuppressed;
        }
    }
}

