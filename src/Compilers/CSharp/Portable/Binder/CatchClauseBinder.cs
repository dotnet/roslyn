// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class CatchClauseBinder : LocalScopeBinder
    {
        private readonly CatchClauseSyntax _syntax;

        public CatchClauseBinder(Binder enclosing, CatchClauseSyntax syntax)
            : base(enclosing, (enclosing.Flags | BinderFlags.InCatchBlock) & ~BinderFlags.InNestedFinallyBlock)
        {
            Debug.Assert(syntax != null);
            _syntax = syntax;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            var locals = ArrayBuilder<LocalSymbol>.GetInstance();

            var declarationOpt = _syntax.Declaration;
            if ((declarationOpt != null) && (declarationOpt.Identifier.Kind() != SyntaxKind.None))
            {
                locals.Add(SourceLocalSymbol.MakeLocal(this.ContainingMemberOrLambda, this, allowRefKind: false, allowScoped: false, declarationOpt.Type, declarationOpt.Identifier, LocalDeclarationKind.CatchVariable, initializer: null));
            }

            if (_syntax.Filter != null)
            {
                ExpressionVariableFinder.FindExpressionVariables(this, locals, _syntax.Filter.FilterExpression);
            }

            return locals.ToImmutableAndFree();
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(SyntaxNode scopeDesignator)
        {
            if (_syntax == scopeDesignator)
            {
                return this.Locals;
            }

            throw ExceptionUtilities.Unreachable();
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope(CSharpSyntaxNode scopeDesignator)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override SyntaxNode ScopeDesignator
        {
            get
            {
                return _syntax;
            }
        }
    }
}
