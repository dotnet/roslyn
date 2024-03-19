// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

internal partial class CSharpSelectionValidator
{
    public static bool Check(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        => node switch
        {
            ExpressionSyntax expression => CheckExpression(semanticModel, expression, cancellationToken),
            BlockSyntax block => CheckBlock(block),
            StatementSyntax statement => CheckStatement(statement),
            GlobalStatementSyntax _ => CheckGlobalStatement(),
            _ => false,
        };

    private static bool CheckGlobalStatement()
        => true;

    private static bool CheckBlock(BlockSyntax block)
    {
        // TODO(cyrusn): Is it intentional that fixed statement is not in this list?
        return block.Parent is BlockSyntax or
               DoStatementSyntax or
               ElseClauseSyntax or
               CommonForEachStatementSyntax or
               ForStatementSyntax or
               IfStatementSyntax or
               LockStatementSyntax or
               UsingStatementSyntax or
               WhileStatementSyntax;
    }

    private static bool CheckExpression(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
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

    private static bool CheckStatement(StatementSyntax statement)
        => statement is CheckedStatementSyntax or
           DoStatementSyntax or
           EmptyStatementSyntax or
           ExpressionStatementSyntax or
           FixedStatementSyntax or
           CommonForEachStatementSyntax or
           ForStatementSyntax or
           IfStatementSyntax or
           LocalDeclarationStatementSyntax or
           LockStatementSyntax or
           ReturnStatementSyntax or
           SwitchStatementSyntax or
           ThrowStatementSyntax or
           TryStatementSyntax or
           UnsafeStatementSyntax or
           UsingStatementSyntax or
           WhileStatementSyntax;
}
