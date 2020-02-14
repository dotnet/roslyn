// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This binder owns the scope for Simple Program top-level statements.
    /// </summary>
    internal sealed class SimpleProgramBinder : LocalScopeBinder
    {
        private readonly CompilationUnitSyntax _compilationUnit;

        public SimpleProgramBinder(Binder enclosing, CompilationUnitSyntax compilationUnit)
            : base(enclosing, enclosing.Flags)
        {
            RoslynDebug.Assert(compilationUnit is object);
            _compilationUnit = compilationUnit;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            ArrayBuilder<LocalSymbol> locals = ArrayBuilder<LocalSymbol>.GetInstance();
            foreach (var statement in _compilationUnit.Members)
            {
                if (statement is GlobalStatementSyntax topLevelStatement)
                {
                    BuildLocals(this, topLevelStatement.Statement, locals);
                }
            }

            return locals.ToImmutableAndFree();
        }

        protected override ImmutableArray<LocalFunctionSymbol> BuildLocalFunctions()
        {
            ArrayBuilder<LocalFunctionSymbol>? locals = null;
            foreach (var statement in _compilationUnit.Members)
            {
                if (statement is GlobalStatementSyntax topLevelStatement)
                {
                    BuildLocalFunctions(topLevelStatement.Statement, ref locals);
                }
            }

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
            ArrayBuilder<LabelSymbol>? labels = null;
            RoslynDebug.Assert(this.ContainingMemberOrLambda is object);
            var containingMethod = (MethodSymbol)this.ContainingMemberOrLambda;
            foreach (var statement in _compilationUnit.Members)
            {
                if (statement is GlobalStatementSyntax topLevelStatement)
                {
                    BuildLabels(containingMethod, topLevelStatement.Statement, ref labels);
                }
            }

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

            throw ExceptionUtilities.Unreachable;
        }

        internal override SyntaxNode ScopeDesignator
        {
            get
            {
                return _compilationUnit;
            }
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            if (ScopeDesignator == scopeDesignator)
            {
                return this.LocalFunctions;
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}
