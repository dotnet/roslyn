// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// This binder provides a context for binding within a specific compilation unit, but outside of top-level statements.
    /// It ensures that locals are in scope, however it is not responsible
    /// for creating the symbols. That task is actually owned by <see cref="SimpleProgramBinder"/> and
    /// this binder simply delegates to it when appropriate. That ensures that the same set of symbols is 
    /// shared across all compilation units.
    /// </summary>
    internal sealed class SimpleProgramUnitBinder : LocalScopeBinder
    {
        private readonly SimpleProgramBinder _scope;
        public SimpleProgramUnitBinder(Binder enclosing, SimpleProgramBinder scope)
            : base(enclosing, enclosing.Flags)
        {
            _scope = scope;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            return _scope.Locals;
        }

        protected override ImmutableArray<LocalFunctionSymbol> BuildLocalFunctions()
        {
            return _scope.LocalFunctions;
        }

        internal override bool IsLocalFunctionsScopeBinder
        {
            get
            {
                return _scope.IsLocalFunctionsScopeBinder;
            }
        }

        protected override ImmutableArray<LabelSymbol> BuildLabels()
        {
            return ImmutableArray<LabelSymbol>.Empty;
        }

        internal override bool IsLabelsScopeBinder
        {
            get
            {
                return false;
            }
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            return _scope.GetDeclaredLocalsForScope(scopeDesignator);
        }

        internal override SyntaxNode? ScopeDesignator
        {
            get
            {
                return _scope.ScopeDesignator;
            }
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            return _scope.GetDeclaredLocalFunctionsForScope(scopeDesignator);
        }
    }
}
