// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class CatchClauseBinder : LocalScopeBinder
    {
        private readonly CatchClauseSyntax syntax;

        public CatchClauseBinder(Binder enclosing, CatchClauseSyntax syntax)
            : base(enclosing, enclosing.Flags | BinderFlags.InCatchBlock)
        {
            Debug.Assert(syntax != null);
            this.syntax = syntax;
        }

        override protected ImmutableArray<LocalSymbol> BuildLocals()
        {
            SourceLocalSymbol local = null;

            var declarationOpt = syntax.Declaration;
            if ((declarationOpt != null) && (declarationOpt.Identifier.CSharpKind() != SyntaxKind.None))
            {
                local = SourceLocalSymbol.MakeLocal(this.ContainingMemberOrLambda, this, declarationOpt.Type, declarationOpt.Identifier, LocalDeclarationKind.CatchVariable);
            }

            if ((object)local != null)
            {
                return ImmutableArray.Create<LocalSymbol>(local);
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(CSharpSyntaxNode node)
        {
            if (node == syntax)
            {
                return this.Locals;
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}