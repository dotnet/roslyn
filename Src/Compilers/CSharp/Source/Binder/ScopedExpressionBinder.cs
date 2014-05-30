// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class ScopedExpressionBinder : LocalScopeBinder
    {
        private readonly ExpressionSyntax expression;

        public ScopedExpressionBinder(Binder enclosing, ExpressionSyntax expression)
            : base(enclosing, enclosing.Flags)
        {
            this.expression = expression;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            var walker = new BuildLocalsFromDeclarationsWalker(this, expression);

            walker.Visit(expression);

            if (walker.Locals != null)
            {
                return walker.Locals.ToImmutableAndFree();
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(CSharpSyntaxNode node)
        {
            if (node == expression)
            {
                return this.Locals;
            }

            return base.GetDeclaredLocalsForScope(node);
        }
    }
}
