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
            SourceLocalSymbol local = null;

            var declarationOpt = _syntax.Declaration;
            if ((declarationOpt != null) && (declarationOpt.Identifier.Kind() != SyntaxKind.None))
            {
                local = SourceLocalSymbol.MakeLocal(this.ContainingMemberOrLambda, this, declarationOpt.Type, declarationOpt.Identifier, LocalDeclarationKind.CatchVariable);
            }

            if ((object)local != null)
            {
                return ImmutableArray.Create<LocalSymbol>(local);
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope()
        {
            return this.Locals;
        }

        internal override ImmutableArray<LocalFunctionSymbol> GetDeclaredLocalFunctionsForScope()
        {
            return ImmutableArray<LocalFunctionSymbol>.Empty;
        }
    }
}
