// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Humanizer.In;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal class CSharpForLoopSnippetProvider : AbstractForLoopSnippetProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpForLoopSnippetProvider()
        {
        }

        protected override async Task<SyntaxNode> CreateForLoopStatementSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var generator = SyntaxGenerator.GetGenerator(document);
            var options = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            var varBuiltInType = false;

            if (options != null)
            {
                varBuiltInType = options.GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes).Value;
            }

            var indexVariable = generator.Identifier("i");
            var varIdentifier = SyntaxFactory.IdentifierName("var");

            var variableDeclarationSyntax = varBuiltInType
                ? SyntaxFactory.VariableDeclaration(varIdentifier,
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            indexVariable,
                            argumentList: null,
                            SyntaxFactory.EqualsValueClause((ExpressionSyntax)generator.LiteralExpression(0)))))
                : SyntaxFactory.VariableDeclaration(compilation.GetSpecialType(SpecialType.System_Int32).GenerateTypeSyntax(allowVar: false),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            indexVariable,
                            argumentList: null,
                            SyntaxFactory.EqualsValueClause((ExpressionSyntax)generator.LiteralExpression(0)))));

            var forLoopSyntax = SyntaxFactory.ForStatement(variableDeclarationSyntax,
                SyntaxFactory.SeparatedList<ExpressionSyntax>(),
                (ExpressionSyntax)generator.LessThanExpression(
                    generator.IdentifierName(indexVariable),
                    generator.IdentifierName("length")),
                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                    SyntaxFactory.PostfixUnaryExpression(
                        SyntaxKind.PostIncrementExpression, SyntaxFactory.IdentifierName(indexVariable))),
                SyntaxFactory.Block());

            return forLoopSyntax;
        }

        protected override ImmutableArray<SnippetPlaceholder> GetForLoopSnippetPlaceholders(SyntaxNode node, ISyntaxFacts syntaxFacts)
        {
            using var _ = ArrayBuilder<SnippetPlaceholder>.GetInstance(out var arrayBuilder);
            var placeHolderBuilder = new Dictionary<string, List<int>>();
            GetPartsOfForStatement(node, out var declaration, out var condition, out var incrementor, out var _1);

            if (declaration != null)
            {
                var variableDeclarator = ((VariableDeclarationSyntax)declaration).Variables.FirstOrDefault();
                if (variableDeclarator != null)
                {
                    var declaratorIdentifier = variableDeclarator.Identifier;
                    placeHolderBuilder.Add(declaratorIdentifier.ToString(), new List<int>() { declaratorIdentifier.SpanStart });
                }
            }

            if (condition != null)
            {
                var conditionExpression = (BinaryExpressionSyntax)condition;
                var left = conditionExpression.Left;
                if (!placeHolderBuilder.TryGetValue(left.ToString(), out var incrementorList))
                {
                    incrementorList = new List<int>();
                    placeHolderBuilder.Add(left.ToString(), incrementorList);
                }

                incrementorList.Add(left.SpanStart);

                var right = conditionExpression.Right;
                if (!placeHolderBuilder.TryGetValue(right.ToString(), out var lengthList))
                {
                    lengthList = new List<int>();
                    placeHolderBuilder.Add(right.ToString(), lengthList);
                }

                lengthList.Add(right.SpanStart);
            }

            if (incrementor != null)
            {
                var operand = ((PostfixUnaryExpressionSyntax)incrementor).Operand;
                if (!placeHolderBuilder.TryGetValue(operand.ToString(), out var incrementorList))
                {
                    incrementorList = new List<int>();
                    placeHolderBuilder.Add(operand.ToString(), incrementorList);
                }

                incrementorList.Add(operand.SpanStart);
            }

            foreach (var kvp in placeHolderBuilder)
            {
                arrayBuilder.Add(new SnippetPlaceholder(kvp.Key, kvp.Value.ToImmutableArray()));
            }

            return arrayBuilder.ToImmutableArray();

        }

        protected override int GetCaretPosition(SyntaxNode caretTarget)
        {
            GetPartsOfForStatement(caretTarget, out _, out _, out _, out var statement);
            return statement.SpanStart + 1;

        }

        private static void GetPartsOfForStatement(SyntaxNode node, out SyntaxNode? declaration, out SyntaxNode? condition,
            out SyntaxNode? incrementor, out SyntaxNode statement)
        {
            var forStatement = (ForStatementSyntax)node;
            declaration = forStatement.Declaration;
            condition = forStatement.Condition;
            incrementor = forStatement.Incrementors.FirstOrDefault();
            statement = forStatement.Statement;
        }
    }
}
