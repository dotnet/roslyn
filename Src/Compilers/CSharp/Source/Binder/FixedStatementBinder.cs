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

        public FixedStatementBinder(MethodSymbol owner, Binder enclosing, FixedStatementSyntax syntax)
            : base(owner, enclosing)
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
                    locals.Add(SourceLocalSymbol.MakeLocal(this.Owner, this, syntax.Declaration.Type, declarator.Identifier, declarator.Initializer, LocalDeclarationKind.Fixed));
                }

                return locals.ToImmutable();
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }
    }
}