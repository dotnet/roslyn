// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class ForLoopBinder : LoopBinder
    {
        private readonly ForStatementSyntax syntax;

        public ForLoopBinder(MethodSymbol owner, Binder enclosing, ForStatementSyntax syntax)
            : base(owner, enclosing)
        {
            Debug.Assert(syntax != null);
            this.syntax = syntax;
        }

        override protected ImmutableArray<LocalSymbol> BuildLocals()
        {
            var declaration = this.syntax.Declaration;
            if (declaration == null)
            {
                return ImmutableArray<LocalSymbol>.Empty;
            }

            var locals = ArrayBuilder<LocalSymbol>.GetInstance();
            foreach (var variable in declaration.Variables)
            {
                var localSymbol = SourceLocalSymbol.MakeLocal(
                    this.Owner,
                    this,
                    declaration.Type,
                    variable.Identifier,
                    variable.Initializer,
                    LocalDeclarationKind.For);
                locals.Add(localSymbol);
            }
            return locals.ToImmutableAndFree();
        }
    }
}