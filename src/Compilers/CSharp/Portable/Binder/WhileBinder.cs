// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class WhileBinder : LoopBinder
    {
        private readonly StatementSyntax _syntax;

        public WhileBinder(Binder enclosing, StatementSyntax syntax)
            : base(enclosing)
        {
            Debug.Assert(syntax != null && (syntax.IsKind(SyntaxKind.WhileStatement) || syntax.IsKind(SyntaxKind.DoStatement)));
            _syntax = syntax;
        }

        internal override BoundWhileStatement BindWhileParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            var node = (WhileStatementSyntax)_syntax;

            var condition = originalBinder.BindBooleanExpression(node.Condition, diagnostics);
            var body = originalBinder.BindPossibleEmbeddedStatement(node.Statement, diagnostics);
            Debug.Assert(this.Locals.IsDefaultOrEmpty);
            return new BoundWhileStatement(node, condition, body, this.BreakLabel, this.ContinueLabel);
        }

        internal override BoundDoStatement BindDoParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            var node = (DoStatementSyntax)_syntax;

            var condition = originalBinder.BindBooleanExpression(node.Condition, diagnostics);
            var body = originalBinder.BindPossibleEmbeddedStatement(node.Statement, diagnostics);
            Debug.Assert(this.Locals.IsDefaultOrEmpty);
            return new BoundDoStatement(node, condition, body, this.BreakLabel, this.ContinueLabel);
        }
    }
}
