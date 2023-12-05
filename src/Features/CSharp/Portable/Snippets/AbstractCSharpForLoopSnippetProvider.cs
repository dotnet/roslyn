// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    internal abstract class AbstractCSharpForLoopSnippetProvider : AbstractForLoopSnippetProvider
    {
        private static readonly string[] s_iteratorBaseNames = new[] { "i", "j", "k" };

        protected abstract SyntaxKind ConditionKind { get; }

        protected abstract SyntaxKind IncrementorKind { get; }

        protected abstract ExpressionSyntax GenerateInitializerValue(SyntaxGenerator generator, SyntaxNode? inlineExpression);

        protected abstract ExpressionSyntax GenerateRightSideOfCondition(SyntaxGenerator generator, SyntaxNode? inlineExpression);

        protected abstract void AddSpecificPlaceholders(MultiDictionary<string, int> placeholderBuilder, ExpressionSyntax initializer, ExpressionSyntax rightOfCondition);

        protected override SyntaxNode GenerateStatement(SyntaxGenerator generator, SyntaxContext syntaxContext, SyntaxNode? inlineExpression)
        {
            var semanticModel = syntaxContext.SemanticModel;
            var compilation = semanticModel.Compilation;

            var iteratorName = NameGenerator.GenerateUniqueName(s_iteratorBaseNames, n => semanticModel.LookupSymbols(syntaxContext.Position, name: n).IsEmpty);
            var iteratorVariable = generator.Identifier(iteratorName);
            var indexVariable = (ExpressionSyntax)generator.IdentifierName(iteratorName);

            TypeSyntax? iteratorTypeSyntax = null;

            if (inlineExpression is null)
            {
                iteratorTypeSyntax = compilation.GetSpecialType(SpecialType.System_Int32).GenerateTypeSyntax();
            }
            else
            {
                var inlineExpressionType = semanticModel.GetTypeInfo(inlineExpression).Type;
                Debug.Assert(inlineExpressionType is not null && (inlineExpressionType.IsIntegralType() || inlineExpressionType.IsNativeIntegerType));
                iteratorTypeSyntax = inlineExpressionType.GenerateTypeSyntax();
            }

            inlineExpression = inlineExpression?.WithoutLeadingTrivia();

            var variableDeclaration = SyntaxFactory.VariableDeclaration(
                iteratorTypeSyntax,
                variables: SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(iteratorVariable,
                        argumentList: null,
                        SyntaxFactory.EqualsValueClause(GenerateInitializerValue(generator, inlineExpression)))))
                .NormalizeWhitespace();

            return SyntaxFactory.ForStatement(
                variableDeclaration,
                SyntaxFactory.SeparatedList<ExpressionSyntax>(),
                SyntaxFactory.BinaryExpression(ConditionKind, indexVariable, GenerateRightSideOfCondition(generator, inlineExpression)),
                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                    SyntaxFactory.PostfixUnaryExpression(IncrementorKind, indexVariable)),
                SyntaxFactory.Block());
        }

        protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SnippetPlaceholder>.GetInstance(out var arrayBuilder);
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
            {
                arrayBuilder.Add(new(key, value.ToImmutableArray()));
            }

            return arrayBuilder.ToImmutableArray();
        }

        protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget, SourceText sourceText)
        {
            GetPartsOfForStatement(caretTarget, out _, out _, out _, out var statement);
            var blockStatement = (BlockSyntax)statement!;

            var triviaSpan = blockStatement.CloseBraceToken.LeadingTrivia.Span;
            var line = sourceText.Lines.GetLineFromPosition(triviaSpan.Start);
            // Getting the location at the end of the line before the newline.
            return line.Span.End;
        }

        protected override Task<Document> AddIndentationToDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            return СSharpSnippetIndentationHelpers.AddBlockIndentationToDocumentAsync<ForStatementSyntax>(
                document,
                FindSnippetAnnotation,
                static s => (BlockSyntax)s.Statement,
                cancellationToken);
        }

        private static void GetPartsOfForStatement(SyntaxNode node, out SyntaxNode? declaration, out SyntaxNode? condition, out SyntaxNode? incrementor, out SyntaxNode? statement)
        {
            var forStatement = (ForStatementSyntax)node;
            declaration = forStatement.Declaration;
            condition = forStatement.Condition;
            incrementor = forStatement.Incrementors.Single();
            statement = forStatement.Statement;
        }
    }
}
