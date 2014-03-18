// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class CatchClauseBinder : LocalScopeBinder
    {
        private readonly CatchClauseSyntax syntax;

        public CatchClauseBinder(MethodSymbol owner, Binder enclosing, CatchClauseSyntax syntax)
            : base(owner, enclosing, enclosing.Flags | BinderFlags.InCatchBlock)
        {
            Debug.Assert(syntax != null);
            this.syntax = syntax;
        }

        override protected ImmutableArray<LocalSymbol> BuildLocals()
        {
            var declarationOpt = syntax.Declaration;
            if ((declarationOpt != null) && (declarationOpt.Identifier.CSharpKind() != SyntaxKind.None))
            {
                var local = SourceLocalSymbol.MakeLocal(this.Owner, this, declarationOpt.Type, declarationOpt.Identifier, null, LocalDeclarationKind.Catch);
                return ImmutableArray.Create<LocalSymbol>(local);
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }
    }
}