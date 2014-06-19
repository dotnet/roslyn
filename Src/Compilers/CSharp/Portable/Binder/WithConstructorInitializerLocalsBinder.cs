// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class WithConstructorInitializerLocalsBinder : LocalScopeBinder
    {
        private readonly CSharpSyntaxNode scope;
        private readonly ArgumentListSyntax initializerArgumentList;

        public WithConstructorInitializerLocalsBinder(Binder enclosing, ConstructorDeclarationSyntax declaration)
            : base(enclosing, enclosing.Flags)
        {
            Debug.Assert(declaration.Initializer != null);
            this.scope = declaration;
            this.initializerArgumentList = declaration.Initializer.ArgumentList;
        }

        public WithConstructorInitializerLocalsBinder(Binder enclosing, ArgumentListSyntax initializerArgumentList)
            : base(enclosing, enclosing.Flags)
        {
            Debug.Assert(initializerArgumentList != null);
            this.scope = initializerArgumentList;
            this.initializerArgumentList = initializerArgumentList;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            var walker = new BuildLocalsFromDeclarationsWalker(this, initializerArgumentList);

            walker.Visit(initializerArgumentList);

            if (walker.Locals != null)
            {
                return walker.Locals.ToImmutableAndFree();
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(CSharpSyntaxNode node)
        {
            if (node == scope)
            {
                return this.Locals;
            }

            return base.GetDeclaredLocalsForScope(node);
        }
    }
}
