// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;

internal sealed class CSharpUseCollectionInitializerAnalyzer : AbstractUseCollectionInitializerAnalyzer<
    ExpressionSyntax,
    StatementSyntax,
    BaseObjectCreationExpressionSyntax,
    MemberAccessExpressionSyntax,
    InvocationExpressionSyntax,
    ExpressionStatementSyntax,
    ForEachStatementSyntax,
    IfStatementSyntax,
    VariableDeclaratorSyntax,
    CSharpUseCollectionInitializerAnalyzer>
{
    protected override bool IsComplexElementInitializer(SyntaxNode expression)
        => expression.IsKind(SyntaxKind.ComplexElementInitializerExpression);

    protected override bool HasExistingInvalidInitializerForCollection(BaseObjectCreationExpressionSyntax objectCreation)
    {
        // Can't convert to a collection expression if it already has an object-initializer.  Note, we do allow
        // conversion of empty `{ }` initializer.  So we only block if the expression count is more than zero.
        return objectCreation.Initializer is InitializerExpressionSyntax
        {
            RawKind: (int)SyntaxKind.ObjectInitializerExpression,
            Expressions.Count: > 0,
        };
    }

    protected override void GetPartsOfForeachStatement(
        ForEachStatementSyntax statement,
        out SyntaxToken identifier,
        out ExpressionSyntax expression,
        out IEnumerable<StatementSyntax> statements)
    {
        identifier = statement.Identifier;
        expression = statement.Expression;
        statements = ExtractEmbeddedStatements(statement.Statement);
    }

    protected override void GetPartsOfIfStatement(
        IfStatementSyntax statement,
        out ExpressionSyntax condition,
        out IEnumerable<StatementSyntax> whenTrueStatements,
        out IEnumerable<StatementSyntax>? whenFalseStatements)
    {
        condition = statement.Condition;
        whenTrueStatements = ExtractEmbeddedStatements(statement.Statement);
        whenFalseStatements = statement.Else != null ? ExtractEmbeddedStatements(statement.Else.Statement) : null;
    }

    private static IEnumerable<StatementSyntax> ExtractEmbeddedStatements(StatementSyntax embeddedStatement)
        => embeddedStatement is BlockSyntax block ? block.Statements : SpecializedCollections.SingletonEnumerable(embeddedStatement);
}
