// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Diagnostics;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class ExpressionListVariableBinder : LocalScopeBinder
    {
        private readonly SeparatedSyntaxList<ExpressionSyntax> _expressions;

        internal ExpressionListVariableBinder(SeparatedSyntaxList<ExpressionSyntax> expressions, Binder next) : base(next)
        {
            Debug.Assert(expressions.Count > 0);
            _expressions = expressions;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            var builder = ArrayBuilder<LocalSymbol>.GetInstance();
            ExpressionVariableFinder.FindExpressionVariables(this, builder, _expressions);
            return builder.ToImmutableAndFree();
        }

        internal override SyntaxNode ScopeDesignator
        {
            get
            {
                return _expressions[0];
            }
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            if (ScopeDesignator == scopeDesignator)
            {
                return this.Locals;
            }

            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
