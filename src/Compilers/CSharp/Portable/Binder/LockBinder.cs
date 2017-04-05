// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class LockBinder : LockOrUsingBinder
    {
        private readonly LockStatementSyntax _syntax;

        public LockBinder(Binder enclosing, LockStatementSyntax syntax)
            : base(enclosing)
        {
            _syntax = syntax;
        }

        protected override ExpressionSyntax TargetExpressionSyntax
        {
            get
            {
                return _syntax.Expression;
            }
        }

        internal override BoundStatement BindLockStatementParts(DiagnosticBag diagnostics, Binder originalBinder)
        {
            // Allow method groups during binding and then rule them out when we check that the expression has
            // a reference type.
            ExpressionSyntax exprSyntax = TargetExpressionSyntax;
            BoundExpression expr = BindTargetExpression(diagnostics, originalBinder);
            TypeSymbol exprType = expr.Type;

            bool hasErrors = false;

            if ((object)exprType == null)
            {
                if (expr.ConstantValue != ConstantValue.Null || Compilation.FeatureStrictEnabled) // Dev10 allows the null literal.
                {
                    Error(diagnostics, ErrorCode.ERR_LockNeedsReference, exprSyntax, expr.Display);
                    hasErrors = true;
                }
            }
            else if (!exprType.IsReferenceType && (exprType.IsValueType || Compilation.FeatureStrictEnabled))
            {
                Error(diagnostics, ErrorCode.ERR_LockNeedsReference, exprSyntax, exprType);
                hasErrors = true;
            }

            BoundStatement stmt = originalBinder.BindPossibleEmbeddedStatement(_syntax.Statement, diagnostics);
            Debug.Assert(this.Locals.IsDefaultOrEmpty);
            return new BoundLockStatement(_syntax, expr, stmt, hasErrors);
        }
    }
}
