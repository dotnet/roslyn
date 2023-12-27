// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private readonly SynthesizedSimpleProgramEntryPointSymbol _entryPoint;

        public SimpleProgramBinder(Binder enclosing, SynthesizedSimpleProgramEntryPointSymbol entryPoint)
            : base(enclosing, enclosing.Flags)
        {
            _entryPoint = entryPoint;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            ArrayBuilder<LocalSymbol> locals = ArrayBuilder<LocalSymbol>.GetInstance(DefaultLocalSymbolArrayCapacity);

            foreach (var statement in _entryPoint.CompilationUnit.Members)
            {
                if (statement is GlobalStatementSyntax topLevelStatement)
                {
                    this.BuildLocals(this, topLevelStatement.Statement, locals);
                }
            }

            return locals.ToImmutableAndFree();
        }

        protected override ImmutableArray<LocalFunctionSymbol> BuildLocalFunctions()
        {
            ArrayBuilder<LocalFunctionSymbol>? locals = null;

            foreach (var statement in _entryPoint.CompilationUnit.Members)
            {
                if (statement is GlobalStatementSyntax topLevelStatement)
                {
                    this.BuildLocalFunctions(topLevelStatement.Statement, ref locals);
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

            foreach (var statement in _entryPoint.CompilationUnit.Members)
            {
                if (statement is GlobalStatementSyntax topLevelStatement)
                {
                    BuildLabels(_entryPoint, topLevelStatement.Statement, ref labels);
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

            throw ExceptionUtilities.Unreachable();
        }

        internal override SyntaxNode ScopeDesignator
        {
            get
            {
                return _entryPoint.SyntaxNode;
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
