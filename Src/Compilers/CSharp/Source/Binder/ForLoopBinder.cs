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

        public ForLoopBinder(Binder enclosing, ForStatementSyntax syntax)
            : base(enclosing)
        {
            Debug.Assert(syntax != null);
            this.syntax = syntax;
        }

        override protected ImmutableArray<LocalSymbol> BuildLocals()
        {
            var walker = new BuildLocalsFromDeclarationsWalker(this);

            walker.Visit(syntax.Condition);

            foreach (var incrementor in syntax.Incrementors)
            {
                walker.Visit(incrementor);
            }

            if (walker.Locals != null)
            {
                return walker.Locals.ToImmutableAndFree();
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }

        internal override BoundForStatement BindForParts(DiagnosticBag diagnostics)
        {
            BoundForStatement result = BindForParts(syntax, diagnostics);

            var initializationBinder = (ForLoopInitializationBinder)this.Next;
            if (!initializationBinder.Locals.IsDefaultOrEmpty)
            {
                result = result.Update(initializationBinder.Locals, 
                                       result.Initializer,
                                       result.InnerLocals,
                                       result.Condition, 
                                       result.Increment,
                                       result.Body, 
                                       result.BreakLabel, 
                                       result.ContinueLabel);
            }

            return result;
        }
    }

    internal sealed class ForLoopInitializationBinder : LocalScopeBinder
    {
        private readonly ForStatementSyntax syntax;

        public ForLoopInitializationBinder(Binder enclosing, ForStatementSyntax syntax)
            : base(enclosing)
        {
            Debug.Assert(syntax != null);
            this.syntax = syntax;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            var walker = new BuildLocalsFromDeclarationsWalker(this);

            walker.Visit(syntax.Declaration);

            foreach (var initializer in syntax.Initializers)
            {
                walker.Visit(initializer);
            }

            if (walker.Locals != null)
            {
                return walker.Locals.ToImmutableAndFree();
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }
    }
}