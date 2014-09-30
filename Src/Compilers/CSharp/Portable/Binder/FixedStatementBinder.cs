// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class FixedStatementBinder : LocalScopeBinder
    {
        private readonly FixedStatementSyntax syntax;

        public FixedStatementBinder(Binder enclosing, FixedStatementSyntax syntax)
            : base(enclosing)
        {
            Debug.Assert(syntax != null);
            this.syntax = syntax;
        }

        override protected ImmutableArray<LocalSymbol> BuildLocals()
        {
            if (syntax.Declaration != null)
            {
                var locals = new ArrayBuilder<LocalSymbol>(syntax.Declaration.Variables.Count);
                foreach (VariableDeclaratorSyntax declarator in syntax.Declaration.Variables)
                {
                    locals.Add(MakeLocal(syntax.Declaration, declarator, LocalDeclarationKind.FixedVariable));
                }

                return locals.ToImmutable();
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }

        internal override ImmutableArray<LocalSymbol> GetDeclaredLocalsForScope(CSharpSyntaxNode node)
        {
            if (syntax == node)
            {
                return this.Locals;
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}