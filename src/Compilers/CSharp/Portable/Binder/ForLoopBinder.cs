// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly ForStatementSyntax _syntax;

        public ForLoopBinder(Binder enclosing, ForStatementSyntax syntax)
            : base(enclosing)
        {
            Debug.Assert(syntax != null);
            _syntax = syntax;
        }

        override protected ImmutableArray<LocalSymbol> BuildLocals()
        {
            var declaration = _syntax.Declaration;
            if (declaration == null)
            {
                return ImmutableArray<LocalSymbol>.Empty;
            }

            var refKind = _syntax.RefKeyword.Kind().GetRefKind();

            var locals = ArrayBuilder<LocalSymbol>.GetInstance();
            foreach (var variable in declaration.Variables)
            {
                var localSymbol = MakeLocal(refKind,
                                            declaration,
                                            variable,
                                            LocalDeclarationKind.ForInitializerVariable);
                locals.Add(localSymbol);
            }

            return locals.ToImmutableAndFree();
        }

        internal override BoundForStatement BindForParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            BoundForStatement result = BindForParts(_syntax, originalBinder, diagnostics);
            return result;
        }

        private BoundForStatement BindForParts(ForStatementSyntax node, Binder originalBinder, DiagnosticBag diagnostics)
        {
            BoundStatement initializer;
            if (node.Declaration != null)
            {
                Debug.Assert(node.Initializers.Count == 0);
                ImmutableArray<BoundLocalDeclaration> unused;
                initializer = this.BindForOrUsingOrFixedDeclarations(node.Declaration, LocalDeclarationKind.ForInitializerVariable, diagnostics, out unused);
            }
            else
            {
                initializer = this.BindStatementExpressionList(node.Initializers, diagnostics);
            }

            var condition = (node.Condition != null) ? BindBooleanExpression(node.Condition, diagnostics) : null;
            var increment = BindStatementExpressionList(node.Incrementors, diagnostics);
            var body = originalBinder.BindPossibleEmbeddedStatement(node.Statement, diagnostics);

            return new BoundForStatement(node,
                                         this.Locals,
                                         initializer,
                                         condition,
                                         increment,
                                         body,
                                         this.BreakLabel,
                                         this.ContinueLabel);
        }
    }
}
