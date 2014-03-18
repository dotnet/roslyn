// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LockBinder : Binder // NOTE: not LocalScopeBinder
    {
        private readonly LockStatementSyntax syntax;
        private readonly LockOrUsingStatementExpressionHandler expressionHandler;

        public LockBinder(Binder enclosing, LockStatementSyntax syntax)
            : base(enclosing)
        {
            this.syntax = syntax;
            this.expressionHandler = new LockOrUsingStatementExpressionHandler(syntax.Expression, this);
        }

        internal override ImmutableHashSet<Microsoft.CodeAnalysis.CSharp.Symbol> LockedOrDisposedVariables
        {
            get
            {
                return expressionHandler.LockedOrDisposedVariables;
            }
        }

        internal override BoundStatement BindLockStatementParts(DiagnosticBag diagnostics)
        {
            // Allow method groups during binding and then rule them out when we check that the expression has
            // a reference type.
            ExpressionSyntax exprSyntax = syntax.Expression;
            BoundExpression expr = expressionHandler.GetExpression(diagnostics);
            TypeSymbol exprType = expr.Type;

            bool hasErrors = false;

            if ((object)exprType == null)
            {
                if (expr.ConstantValue != ConstantValue.Null) // Dev10 allows the null literal.
                {
                    Error(diagnostics, ErrorCode.ERR_LockNeedsReference, exprSyntax, expr.Display);
                    hasErrors = true;
                }
            }
            else if (!exprType.IsReferenceType)
            {
                Error(diagnostics, ErrorCode.ERR_LockNeedsReference, exprSyntax, exprType);
                hasErrors = true;
            }

            BoundStatement stmt = BindPossibleEmbeddedStatement(syntax.Statement, diagnostics);
            return new BoundLockStatement(syntax, expr, stmt, hasErrors);
        }
    }
}