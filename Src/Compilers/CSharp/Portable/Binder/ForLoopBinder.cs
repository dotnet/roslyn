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
            var walker = new BuildLocalsFromDeclarationsWalker(this, null);

            walker.ScopeSegmentRoot = syntax.Condition;
            walker.Visit(syntax.Condition);

            foreach (var incrementor in syntax.Incrementors)
            {
                walker.ScopeSegmentRoot = incrementor;
                walker.Visit(incrementor);
            }

            walker.ScopeSegmentRoot = null;

            if (walker.Locals != null)
            {
                return walker.Locals.ToImmutableAndFree();
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }

        internal override BoundForStatement BindForParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            BoundForStatement result = BindForParts(syntax, originalBinder, diagnostics);

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

        private BoundForStatement BindForParts(ForStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            BoundStatement initializer;
            if (node.Declaration != null)
            {
                Debug.Assert(node.Initializers.Count == 0);
                ImmutableArray<BoundLocalDeclaration> unused;
                initializer = this.Next.BindForOrUsingOrFixedDeclarations(node.Declaration, LocalDeclarationKind.ForInitializerVariable, diagnostics, out unused);
            }
            else
            {
                initializer = this.Next.BindStatementExpressionList(node.Initializers, diagnostics);
            }

            var condition = (node.Condition != null) ? BindBooleanExpression(node.Condition, diagnostics) : null;
            var increment = BindStatementExpressionList(node.Incrementors, diagnostics);
            var body = originalBinder.BindPossibleEmbeddedStatement(node.Statement, diagnostics);

            return new BoundForStatement(node,
                                         ImmutableArray<LocalSymbol>.Empty,
                                         initializer,
                                         this.Locals,
                                         condition,
                                         increment,
                                         body,
                                         this.BreakLabel,
                                         this.ContinueLabel);
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
            var walker = new BuildLocalsFromDeclarationsWalker(this, null);

            walker.ScopeSegmentRoot = syntax.Declaration;
            walker.Visit(syntax.Declaration);

            foreach (var initializer in syntax.Initializers)
            {
                walker.ScopeSegmentRoot = initializer;
                walker.Visit(initializer);
            }

            walker.ScopeSegmentRoot = null;
            if (walker.Locals != null)
            {
                return walker.Locals.ToImmutableAndFree();
            }

            return ImmutableArray<LocalSymbol>.Empty;
        }
    }
}