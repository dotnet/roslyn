// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal sealed class CSharpForLoopSnippetProvider : AbstractForLoopSnippetProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpForLoopSnippetProvider()
        {
        }

        /// <summary>
        /// Creates the for loop statement syntax.
        /// Must be done in language specific file since there is no generic way to generate the syntax.
        /// </summary>
        protected override async Task<SyntaxNode> CreateForLoopStatementSyntaxAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var generator = SyntaxGenerator.GetGenerator(document);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var iteratorName = NameGenerator.GenerateUniqueName(
                new List<string> { "i", "j", "k", "a", "b", "c" },
                n => semanticModel.LookupSymbols(position, name: n).IsEmpty);
            var indexVariable = generator.Identifier(iteratorName);

            // Creating the variable declaration based on if the user has
            // 'var for built in types' set in their editorconfig.
            var variableDeclarationSyntax =
                 SyntaxFactory.VariableDeclaration(compilation.GetSpecialType(SpecialType.System_Int32).GenerateTypeSyntax(allowVar: true),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            indexVariable,
                            argumentList: null,
                            SyntaxFactory.EqualsValueClause((ExpressionSyntax)generator.LiteralExpression(0)))));

            var forLoopSyntax = SyntaxFactory.ForStatement(
                variableDeclarationSyntax,
                SyntaxFactory.SeparatedList<ExpressionSyntax>(),
                (ExpressionSyntax)generator.LessThanExpression(
                    generator.IdentifierName(indexVariable),
                    // Using a temporary identifier name for now, could later be changed
                    // to look for an iterable item in the scope of the insertion.
                    generator.IdentifierName("length")),
                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                    SyntaxFactory.PostfixUnaryExpression(
                        SyntaxKind.PostIncrementExpression, SyntaxFactory.IdentifierName(indexVariable))),
                SyntaxFactory.Block());

            return forLoopSyntax;
        }

        /// <summary>
        /// Goes through each piece of the for statement and extracts the identifiers
        /// as well as their locations to create SnippetPlaceholder's of each.
        /// </summary>
        protected override ImmutableArray<SnippetPlaceholder> GetPlaceHolderLocationsList(SyntaxNode node, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SnippetPlaceholder>.GetInstance(out var arrayBuilder);
            var placeHolderBuilder = new MultiDictionary<string, int>();
            GetPartsOfForStatement(node, out var declaration, out var condition, out var incrementor, out var _1);

            // Retrieves the placeholder present in the variable declaration as well as its location.
            // The declaration is constructed so it can't be null.
            var variableDeclarator = ((VariableDeclarationSyntax)declaration!).Variables.FirstOrDefault();
            var declaratorIdentifier = variableDeclarator!.Identifier;
            placeHolderBuilder.Add(declaratorIdentifier.ValueText, declaratorIdentifier.SpanStart);

            // Gets the placeholders present in the left and right side of the conditional.
            // The conditional is not null since it is constructed.
            var conditionExpression = (BinaryExpressionSyntax)condition!;
            var left = conditionExpression.Left;
            placeHolderBuilder.Add(left.ToString(), left.SpanStart);

            var right = conditionExpression.Right;
            placeHolderBuilder.Add(right.ToString(), right.SpanStart);

            // Gets the placeholder present in the incrementor.
            // The incrementor is constructed so it can't be null.
            var operand = ((PostfixUnaryExpressionSyntax)incrementor!).Operand;
            placeHolderBuilder.Add(operand.ToString(), operand.SpanStart);

            foreach (var (key, value) in placeHolderBuilder)
            {
                arrayBuilder.Add(new SnippetPlaceholder(key, value.ToImmutableArray()));
            }

            return arrayBuilder.ToImmutableArray();
        }

        /// <summary>
        /// Gets the start of the BlockSyntax of the for statement
        /// to be able to insert the caret position at that location.
        /// </summary>
        protected override int GetTargetCaretPosition(ISyntaxFactsService syntaxFacts, SyntaxNode caretTarget)
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
            // We can assume there will only be one incrementor since it is only constructed with one.
            incrementor = forStatement.Incrementors.First();
            statement = forStatement.Statement;
        }
    }
}
