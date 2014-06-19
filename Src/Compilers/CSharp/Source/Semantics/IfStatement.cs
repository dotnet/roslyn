// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Roslyn.Compilers.CSharp
{
    using Roslyn.Compilers.Internal;
    using Symbols.Source;

    internal sealed partial class SemanticAnalyzer
    {
        private BoundIfStatement BindIfStatement(IfStatementSyntax node)
        {
            Debug.Assert(node != null);

            var condition = BindBooleanExpression(node.Condition);
            var consequence = BindStatement(node.Statement);
            if (node.ElseOpt == null)
            {
                return new BoundIfStatement(node, condition, consequence, null);
            }
            var alternative = BindStatement(node.ElseOpt.Statement);
            return new BoundIfStatement(node, condition, consequence, alternative);
        }

        private BoundExpression BindBooleanExpression(ExpressionSyntax node)
        {
            // A boolean-expression is an expression that yields a result of type bool; 
            // either directly or through application of operator true in certain 
            // contexts as specified in the following.
            //
            // The controlling conditional expression of an if-statement, while-statement, 
            // do-statement, or for-statement is a boolean-expression. The controlling 
            // conditional expression of the ?: operator follows the same rules as a 
            // boolean-expression, but for reasons of operator precedence is classified
            // as a conditional-or-expression.
            //
            // A boolean-expression is required to be implicitly convertible to bool 
            // or of a type that implements operator true. If neither requirement 
            // is satisfied, a binding-time error occurs.
            //
            // When a boolean expression cannot be implicitly converted to bool but does 
            // implement operator true, then following evaluation of the expression, 
            // the operator true implementation provided by that type is invoked 
            // to produce a bool value.

            var expr = BindExpression(node);

            if (!expr.IsOK)
            {
                // The expression could not be bound. Insert a fake conversion
                // around it to bool and keep on going.
                return BoundConversion.AsError(null, expr, ConversionKind.NoConversion, false, false, System_Boolean);
            }

            var kind = Conversions.ClassifyConversion(expr, System_Boolean);

            if (kind.IsImplicitConversion())
            {
                if (kind == ConversionKind.Identity)
                {
                    return expr;
                }
                else
                {
                    return new BoundConversion(null, expr, kind, false, false, System_Boolean);
                }
            }

            // UNDONE: check for operator true / operator false

            // UNDONE: give appropriate errors.

            return null;
        }
    }
}