// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpSelectionValidator
    {
        public bool Check(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            return node.TypeSwitch(
                (ExpressionSyntax expression) => CheckExpression(semanticModel, expression, cancellationToken),
                (BlockSyntax block) => CheckBlock(semanticModel, block, cancellationToken),
                (StatementSyntax statement) => CheckStatement(semanticModel, statement, cancellationToken),
                (GlobalStatementSyntax globalStatement) => CheckGlobalStatement(semanticModel, globalStatement, cancellationToken));
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
                block.Parent is ForEachStatementSyntax ||
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
                statement is ForEachStatementSyntax ||
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
