// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class WhileBinder : LoopBinder
    {
        private readonly StatementSyntax syntax;

        public WhileBinder(Binder enclosing, StatementSyntax syntax)
            : base(enclosing)
        {
            Debug.Assert(syntax != null && (syntax.CSharpKind() == SyntaxKind.WhileStatement || syntax.CSharpKind() == SyntaxKind.DoStatement));
            this.syntax = syntax;
        }

        protected override ImmutableArray<LocalSymbol> BuildLocals()
        {
            return BuildLocals(syntax.Kind == SyntaxKind.WhileStatement ? ((WhileStatementSyntax)syntax).Condition : ((DoStatementSyntax)syntax).Condition);
        }

        internal override BoundWhileStatement BindWhileParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            var node = (WhileStatementSyntax)syntax;

            var condition = BindBooleanExpression(node.Condition, diagnostics);
            var body = originalBinder.BindPossibleEmbeddedStatement(node.Statement, diagnostics);
            return new BoundWhileStatement(node, this.Locals, condition, body, this.BreakLabel, this.ContinueLabel);
        }

        internal override BoundDoStatement BindDoParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            var node = (DoStatementSyntax)syntax;

            var condition = BindBooleanExpression(node.Condition, diagnostics);
            var body = originalBinder.BindPossibleEmbeddedStatement(node.Statement, diagnostics);
            return new BoundDoStatement(node, this.Locals, condition, body, this.BreakLabel, this.ContinueLabel);
        }
    }
}