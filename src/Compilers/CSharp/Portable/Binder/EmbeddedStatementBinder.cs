// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This binder owns the scope for an embedded statement.
    /// </summary>
    internal sealed class EmbeddedStatementBinder : LocalScopeBinder
    {
        private readonly StatementSyntax _statement;

        public EmbeddedStatementBinder(Binder enclosing, StatementSyntax statement)
            : base(enclosing, enclosing.Flags)
        {
            Debug.Assert(statement != null);
            _statement = statement;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            ArrayBuilder<LocalSymbol> locals = ArrayBuilder<LocalSymbol>.GetInstance(DefaultLocalSymbolArrayCapacity);
            BuildLocals(this, _statement, locals);
            return locals.ToImmutableAndFree();
        }

        protected override ImmutableArray<LocalFunctionSymbol> BuildLocalFunctions()
        {
            ArrayBuilder<LocalFunctionSymbol> locals = null;
            BuildLocalFunctions(_statement, ref locals);
            return locals?.ToImmutableAndFree() ?? ImmutableArray<LocalFunctionSymbol>.Empty;
        }

        internal override bool IsLocalFunctionsScopeBinder
        {
            get
            {
                return true;
            }
        }

        protected override ImmutableArray<LabelSymbol> BuildLabels()
        {
            ArrayBuilder<LabelSymbol> labels = null;
            var containingMethod = (MethodSymbol)this.ContainingMemberOrLambda;
            BuildLabels(containingMethod, _statement, ref labels);
            return labels?.ToImmutableAndFree() ?? ImmutableArray<LabelSymbol>.Empty;
        }

        internal override bool IsLabelsScopeBinder
        {
            get
            {
                return true;
            }
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            if (ScopeDesignator == scopeDesignator)
            {
                return this.Locals;
            }

            throw ExceptionUtilities.Unreachable();
        }

        internal override SyntaxNode ScopeDesignator
        {
            get
            {
                return _statement;
            }
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            if (ScopeDesignator == scopeDesignator)
            {
                return this.LocalFunctions;
            }

            throw ExceptionUtilities.Unreachable();
        }
    }
}
