// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundSpillSequence
    {
        public BoundSpillSequence(
            SyntaxNode syntax,
            ImmutableArray<LocalSymbol> locals,
            ImmutableArray<BoundExpression> sideEffects,
            BoundExpression value,
            TypeSymbol type,
            bool hasErrors = false)
            : this(syntax, locals, MakeStatements(sideEffects), value, type, hasErrors)
        {
        }

        private static ImmutableArray<BoundStatement> MakeStatements(ImmutableArray<BoundExpression> expressions)
        {
            return expressions.SelectAsArray<BoundExpression, BoundStatement>(
                expression => new BoundExpressionStatement(expression.Syntax, expression, expression.HasErrors));
        }
    }
}
