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
    /// This binder provides a context for binding Simple Program top-level statements within a specific
    /// compilation unit. It ensures that locals and labels are in scope, however it is not responsible
    /// for creating the symbols. That task is actually owned by <see cref="SimpleProgramBinder"/> and
    /// this binder simply delegates to it when appropriate. That ensures that the same set of symbols is 
    /// shared across all compilation usints.
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
            return _scope.Labels;
        }

        internal override bool IsLabelsScopeBinder
        {
            get
            {
                return _scope.IsLabelsScopeBinder;
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

        internal override Binder? GetBinder(SyntaxNode node)
        {
            // Delegating to the _scope first allows to get to a binder for another compilation unit
            // with top level statements. That avoids adding any special cases for situations when 
            // a SemanticModel binds an entire simple program body by calling Binder.BindMethodBody method
            // on its root binder, which is specific to the compilation unit the model is associated with.
            return _scope.GetBinder(node) ?? base.GetBinder(node);
        }
    }
}
