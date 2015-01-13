// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LockBinder : LockOrUsingBinder 
    {
        private readonly LockStatementSyntax syntax;

        public LockBinder(Binder enclosing, LockStatementSyntax syntax)
            : base(enclosing)
        {
            this.syntax = syntax;
        }

        protected override ExpressionSyntax TargetExpressionSyntax
        {
            get
            {
                return syntax.Expression;
            }
        }

        internal override BoundStatement BindLockStatementParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            // Allow method groups during binding and then rule them out when we check that the expression has
            // a reference type.
            ExpressionSyntax exprSyntax = TargetExpressionSyntax;
            BoundExpression expr = BindTargetExpression(diagnostics);
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

            BoundStatement stmt = originalBinder.BindPossibleEmbeddedStatement(syntax.Statement, diagnostics);
            Debug.Assert(this.Locals.IsDefaultOrEmpty);
            return new BoundLockStatement(syntax, expr, stmt, hasErrors);
        }
    }
}