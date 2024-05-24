// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery;

using static SyntaxFactory;

internal sealed class DefaultConverter(ForEachInfo<ForEachStatementSyntax, StatementSyntax> forEachInfo) : AbstractConverter(forEachInfo)
{
    private static readonly TypeSyntax VarNameIdentifier = IdentifierName("var");

    public override void Convert(SyntaxEditor editor, bool convertToQuery, CancellationToken cancellationToken)
    {
        // Filter out identifiers which are not used in statements.
        var variableNamesReadInside = new HashSet<string>(ForEachInfo.Statements
            .SelectMany(statement => ForEachInfo.SemanticModel.AnalyzeDataFlow(statement).ReadInside).Select(symbol => symbol.Name));
        var identifiersUsedInStatements = ForEachInfo.Identifiers
            .Where(identifier => variableNamesReadInside.Contains(identifier.ValueText));

        // If there is a single statement and it is a block, leave it as is.
        // Otherwise, wrap with a block.
        var block = WrapWithBlockIfNecessary(
            ForEachInfo.Statements.SelectAsArray(statement => statement.KeepCommentsAndAddElasticMarkers()));

        editor.ReplaceNode(
            ForEachInfo.ForEachStatement,
            CreateDefaultReplacementStatement(identifiersUsedInStatements, block, convertToQuery)
                .WithAdditionalAnnotations(Formatter.Annotation));
    }

    private StatementSyntax CreateDefaultReplacementStatement(
        IEnumerable<SyntaxToken> identifiers,
        BlockSyntax block,
        bool convertToQuery)
    {
        var identifiersCount = identifiers.Count();
        if (identifiersCount == 0)
        {
            // Generate foreach(var _ ... select new {})
            return ForEachStatement(
                VarNameIdentifier,
                Identifier("_"),
                CreateQueryExpressionOrLinqInvocation(
                    AnonymousObjectCreationExpression(),
                    [],
                    [],
                    convertToQuery),
                block);
        }
        else if (identifiersCount == 1)
        {
            // Generate foreach(var singleIdentifier from ... select singleIdentifier)
            return ForEachStatement(
                VarNameIdentifier,
                identifiers.Single(),
                CreateQueryExpressionOrLinqInvocation(
                    IdentifierName(identifiers.Single()),
                    [],
                    [],
                    convertToQuery),
                block);
        }
        else
        {
            var tupleForSelectExpression = TupleExpression(
                [.. identifiers.Select(
                    identifier => Argument(IdentifierName(identifier)))]);
            var declaration = DeclarationExpression(
                VarNameIdentifier,
                ParenthesizedVariableDesignation(
                    [.. identifiers.Select(SingleVariableDesignation)]));

            // Generate foreach(var (a,b) ... select (a, b))
            return ForEachVariableStatement(
                declaration,
                CreateQueryExpressionOrLinqInvocation(
                    tupleForSelectExpression,
                    [],
                    [],
                    convertToQuery),
                block);
        }
    }

    private static BlockSyntax WrapWithBlockIfNecessary(ImmutableArray<StatementSyntax> statements)
        => statements is [BlockSyntax block] ? block : Block(statements);
}
