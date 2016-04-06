// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

        override protected ImmutableArray<LocalSymbol> BuildLocals()
        {
            var locals = ArrayBuilder<LocalSymbol>.GetInstance();

            var declarationOpt = _syntax.Declaration;
            if ((declarationOpt != null) && (declarationOpt.Identifier.Kind() != SyntaxKind.None))
            {
                locals.Add(SourceLocalSymbol.MakeLocal(this.ContainingMemberOrLambda, this, RefKind.None, declarationOpt.Type, declarationOpt.Identifier, LocalDeclarationKind.CatchVariable));
            }

            if (_syntax.Filter != null)
            {
                BuildAndAddPatternVariables(locals, _syntax.Filter.FilterExpression);
            }

            return locals.ToImmutableAndFree();
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(CSharpSyntaxNode scopeDesignator)
        {
            if (_syntax == scopeDesignator)
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
