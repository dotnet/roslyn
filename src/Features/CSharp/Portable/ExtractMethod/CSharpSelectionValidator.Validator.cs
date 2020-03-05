﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpSelectionValidator
    {
        public bool Check(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            switch (node)
            {
                case ExpressionSyntax expression: return CheckExpression(semanticModel, expression, cancellationToken);
                case BlockSyntax block: return CheckBlock(semanticModel, block, cancellationToken);
                case StatementSyntax statement: return CheckStatement(semanticModel, statement, cancellationToken);
                case GlobalStatementSyntax globalStatement: return CheckGlobalStatement(semanticModel, globalStatement, cancellationToken);
                default: return false;
            }
        }

        private bool CheckGlobalStatement(SemanticModel semanticModel, GlobalStatementSyntax globalStatement, CancellationToken cancellationToken)
        {
            return true;
        }

        private bool CheckBlock(SemanticModel semanticModel, BlockSyntax block, CancellationToken cancellationToken)
        {
            // TODO(cyrusn): Is it intentional that fixed statement is not in this list?
            if (block.Parent is BlockSyntax ||
                block.Parent is DoStatementSyntax ||
                block.Parent is ElseClauseSyntax ||
                block.Parent is CommonForEachStatementSyntax ||
                block.Parent is ForStatementSyntax ||
                block.Parent is IfStatementSyntax ||
                block.Parent is LockStatementSyntax ||
                block.Parent is UsingStatementSyntax ||
                block.Parent is WhileStatementSyntax)
            {
                return true;
            }

            return false;
        }

        private bool CheckExpression(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // TODO(cyrusn): This is probably unnecessary.  What we should be doing is binding
            // the type of the expression and seeing if it contains an anonymous type.
            if (expression is AnonymousObjectCreationExpressionSyntax)
            {
                return false;
            }

            return expression.CanReplaceWithRValue(semanticModel, cancellationToken);
        }

        private bool CheckStatement(SemanticModel semanticModel, StatementSyntax statement, CancellationToken cancellationToken)
        {
            if (statement is CheckedStatementSyntax ||
                statement is DoStatementSyntax ||
                statement is EmptyStatementSyntax ||
                statement is ExpressionStatementSyntax ||
                statement is FixedStatementSyntax ||
                statement is CommonForEachStatementSyntax ||
                statement is ForStatementSyntax ||
                statement is IfStatementSyntax ||
                statement is LocalDeclarationStatementSyntax ||
                statement is LockStatementSyntax ||
                statement is ReturnStatementSyntax ||
                statement is SwitchStatementSyntax ||
                statement is ThrowStatementSyntax ||
                statement is TryStatementSyntax ||
                statement is UnsafeStatementSyntax ||
                statement is UsingStatementSyntax ||
                statement is WhileStatementSyntax)
            {
                return true;
            }

            return false;
        }
    }
}
