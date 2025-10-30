// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;

internal sealed class CSharpUpdateExpressionSyntaxHelper : IUpdateExpressionSyntaxHelper<ExpressionSyntax, StatementSyntax>
{
    public static readonly CSharpUpdateExpressionSyntaxHelper Instance = new();

    public void GetPartsOfForeachStatement(
        SemanticModel semanticModel,
        StatementSyntax statement,
        out SyntaxToken awaitKeyword,
        out SyntaxToken identifier,
        out ExpressionSyntax expression,
        out IEnumerable<StatementSyntax> statements,
        out bool needsCast)
    {
        var foreachStatement = (ForEachStatementSyntax)statement;
        awaitKeyword = foreachStatement.AwaitKeyword;
        identifier = foreachStatement.Identifier;
        expression = foreachStatement.Expression;
        statements = ExtractEmbeddedStatements(foreachStatement.Statement);

        var foreachInfo = semanticModel.GetForEachStatementInfo(foreachStatement);
        needsCast = !foreachStatement.Type.IsVar && foreachInfo.ElementConversion.IsExplicit;
    }

    public void GetPartsOfIfStatement(
        StatementSyntax statement,
        out ExpressionSyntax condition,
        out IEnumerable<StatementSyntax> whenTrueStatements,
        out IEnumerable<StatementSyntax>? whenFalseStatements)
    {
        var ifStatement = (IfStatementSyntax)statement;
        condition = ifStatement.Condition;
        whenTrueStatements = ExtractEmbeddedStatements(ifStatement.Statement);
        whenFalseStatements = ifStatement.Else != null ? ExtractEmbeddedStatements(ifStatement.Else.Statement) : null;
    }

    private static IEnumerable<StatementSyntax> ExtractEmbeddedStatements(StatementSyntax embeddedStatement)
        => embeddedStatement is BlockSyntax block ? [.. block.Statements] : [embeddedStatement];
}
