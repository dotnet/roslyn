﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

using static SyntaxFactory;

internal abstract class AbstractCSharpForLoopSnippetProvider : AbstractForLoopSnippetProvider
{
    private static readonly string[] s_iteratorBaseNames = ["i", "j", "k"];

    protected abstract SyntaxKind ConditionKind { get; }

    protected abstract SyntaxKind IncrementorKind { get; }

    protected abstract ExpressionSyntax GenerateInitializerValue(SyntaxGenerator generator, SyntaxNode? inlineExpression);

    protected abstract ExpressionSyntax GenerateRightSideOfCondition(SyntaxGenerator generator, SyntaxNode? inlineExpression);

    protected abstract void AddSpecificPlaceholders(MultiDictionary<string, int> placeholderBuilder, ExpressionSyntax initializer, ExpressionSyntax rightOfCondition);

    protected override SyntaxNode GenerateStatement(SyntaxGenerator generator, SyntaxContext syntaxContext, InlineExpressionInfo? inlineExpressionInfo)
    {
        var semanticModel = syntaxContext.SemanticModel;
        var compilation = semanticModel.Compilation;

        var iteratorName = NameGenerator.GenerateUniqueName(s_iteratorBaseNames, n => semanticModel.LookupSymbols(syntaxContext.Position, name: n).IsEmpty);
        var iteratorVariable = generator.Identifier(iteratorName);
        var indexVariable = (ExpressionSyntax)generator.IdentifierName(iteratorName);
        var (iteratorTypeSyntax, inlineExpression) = GetLoopHeaderParts(generator, inlineExpressionInfo, compilation);

        var variableDeclaration = VariableDeclaration(
            iteratorTypeSyntax,
            variables: [VariableDeclarator(iteratorVariable,
                argumentList: null,
                EqualsValueClause(GenerateInitializerValue(generator, inlineExpression)))])
            .NormalizeWhitespace();

        return ForStatement(
            variableDeclaration,
            initializers: [],
            BinaryExpression(ConditionKind, indexVariable, GenerateRightSideOfCondition(generator, inlineExpression)),
            [PostfixUnaryExpression(IncrementorKind, indexVariable)],
            Block());

        static (TypeSyntax iteratorTypeSyntax, SyntaxNode? inlineExpression) GetLoopHeaderParts(SyntaxGenerator generator, InlineExpressionInfo? inlineExpressionInfo, Compilation compilation)
        {
            var inlineExpression = inlineExpressionInfo?.Node.WithoutLeadingTrivia();

            if (inlineExpressionInfo is null)
                return (compilation.GetSpecialType(SpecialType.System_Int32).GenerateTypeSyntax(), inlineExpression);

            var inlineExpressionType = inlineExpressionInfo.TypeInfo.Type;
            Debug.Assert(inlineExpressionType is not null);

            if (IsSuitableIntegerType(inlineExpressionType))
                return (inlineExpressionType.GenerateTypeSyntax(), inlineExpression);

            var property = FindLengthProperty(inlineExpressionType, compilation) ?? FindCountProperty(inlineExpressionType, compilation);
            Contract.ThrowIfNull(property);
            return (property.Type.GenerateTypeSyntax(), generator.MemberAccessExpression(inlineExpression, property.Name));
        }
    }

    protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<SnippetPlaceholder>.GetInstance(out var result);
        var placeholderBuilder = new MultiDictionary<string, int>();
        GetPartsOfForStatement(node, out var declaration, out var condition, out var incrementor, out var _);

        var variableDeclarator = ((VariableDeclarationSyntax)declaration!).Variables.Single();
        var declaratorIdentifier = variableDeclarator.Identifier;
        placeholderBuilder.Add(declaratorIdentifier.ValueText, declaratorIdentifier.SpanStart);

        var conditionExpression = (BinaryExpressionSyntax)condition!;
        var left = conditionExpression.Left;
        placeholderBuilder.Add(left.ToString(), left.SpanStart);

        AddSpecificPlaceholders(placeholderBuilder, variableDeclarator.Initializer!.Value, conditionExpression.Right);

        var operand = ((PostfixUnaryExpressionSyntax)incrementor!).Operand;
        placeholderBuilder.Add(operand.ToString(), operand.SpanStart);

        foreach (var (key, value) in placeholderBuilder)
            result.Add(new(key, [.. value]));

        return result.ToImmutableAndClear();
    }

    protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText)
        => CSharpSnippetHelpers.GetTargetCaretPositionInBlock<ForStatementSyntax>(
            caretTarget,
            static s => (BlockSyntax)s.Statement,
            sourceText);

    protected override Task<Document> AddIndentationToDocumentAsync(Document document, CancellationToken cancellationToken)
        => CSharpSnippetHelpers.AddBlockIndentationToDocumentAsync<ForStatementSyntax>(
            document,
            FindSnippetAnnotation,
            static s => (BlockSyntax)s.Statement,
            cancellationToken);

    private static void GetPartsOfForStatement(SyntaxNode node, out SyntaxNode? declaration, out SyntaxNode? condition, out SyntaxNode? incrementor, out SyntaxNode? statement)
    {
        var forStatement = (ForStatementSyntax)node;
        declaration = forStatement.Declaration;
        condition = forStatement.Condition;
        incrementor = forStatement.Incrementors.Single();
        statement = forStatement.Statement;
    }
}
