// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddParameter;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.IntroduceVariable;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpIntroduceParameterService : AbstractIntroduceParameterService<CSharpIntroduceParameterService, ExpressionSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpIntroduceParameterService()
        {
        }

        protected override async Task<Document> IntroduceParameterAsync(SemanticDocument document, ExpressionSyntax expression, bool allOccurrences, bool trampoline, CancellationToken cancellationToken)
        {
            var invocationDocument = document.Document;
            var semanticModel = document.SemanticModel;
            var semanticFacts = invocationDocument.GetLanguageService<ISemanticFactsService>();
            var parameterName = semanticFacts.GenerateNameForExpression(
                    semanticModel, expression, capitalize: false, cancellationToken: cancellationToken);

            var syntaxGenerator = SyntaxGenerator.GetGenerator(invocationDocument);

            var annotatedExpression = new SyntaxAnnotation();
            var expressionWithAnnotation = expression.WithAdditionalAnnotations(annotatedExpression);
            var updatedSolutionWithParameter = await AddParameterAsync(document, expressionWithAnnotation, parameterName, cancellationToken).ConfigureAwait(false);
            var updatedSemanticDocument = await SemanticDocument.CreateAsync(updatedSolutionWithParameter.GetDocument(document.Document.Id), cancellationToken).ConfigureAwait(false);

            expression = (ExpressionSyntax)updatedSemanticDocument.Root.GetAnnotatedNodesAndTokens(annotatedExpression).Single().AsNode();
            return await IntroduceParameterConvertExpressionAsync(updatedSemanticDocument, expression, (NameSyntax)syntaxGenerator.IdentifierName(parameterName), allOccurrences, cancellationToken).ConfigureAwait(false);

            //return solution.GetDocument(invocationDocument.Id);
        }

        private static Task<Solution> AddParameterAsync(SemanticDocument document, ExpressionSyntax expression, string parameterName, CancellationToken cancellationToken)
        {
            var invocationDocument = document.Document;
            var methodExpression = expression.FirstAncestorOrSelf<MethodDeclarationSyntax>(node => node is MethodDeclarationSyntax, true);
            var semanticModel = document.SemanticModel;
            var symbolInfo = semanticModel.GetDeclaredSymbol(methodExpression, cancellationToken);
            var syntaxFacts = invocationDocument.GetLanguageService<ISyntaxFactsService>();
            var parameterType = semanticModel.GetTypeInfo(expression, cancellationToken).Type ?? document.SemanticModel.Compilation.ObjectType;
            var refKind = syntaxFacts.GetRefKindOfArgument(expression);

            return AddParameterService.Instance.AddParameterAsync(
                invocationDocument,
                symbolInfo,
                parameterType,
                refKind,
                parameterName,
                null,
                false,
                cancellationToken,
                true);
        }

        private async Task<Document> IntroduceParameterConvertExpressionAsync(
            SemanticDocument document,
            ExpressionSyntax expression,
            NameSyntax parameterName,
            bool allOccurrences,
            CancellationToken cancellationToken)
        {
            var block = (BlockSyntax)expression.Ancestors().FirstOrDefault(s => s is BlockSyntax);
            SyntaxNode scope = block;

            // If we're within a non-static local function, our scope for the new local declaration is expanded to include the enclosing member.
            var localFunction = block.GetAncestor<LocalFunctionStatementSyntax>();
            if (localFunction != null && !localFunction.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)))
            {
                scope = block.GetAncestor<MemberDeclarationSyntax>();
            }

            var matches = FindMatches(document, expression, document, scope, allOccurrences, cancellationToken);
            Debug.Assert(matches.Contains(expression));

            (document, matches) = await ComplexifyParentingStatementsAsync(document, matches, cancellationToken).ConfigureAwait(false);

            // Our original expression should have been one of the matches, which were tracked as part
            // of complexification, so we can retrieve the latest version of the expression here.
            expression = document.Root.GetCurrentNode(expression);

            var root = document.Root;
            SyntaxNode innermostCommonBlock;

            if (matches.Count == 1)
            {
                // if there was only one match, or all the matches came from the same statement
                var statement = matches.Single();

                innermostCommonBlock = statement.Parent;
            }
            else
            {
                innermostCommonBlock = matches.FindInnermostCommonNode(IsBlockLike);
            }

            var newInnerMostBlock = Rewrite(
                document, expression, parameterName, document, innermostCommonBlock, allOccurrences, cancellationToken);
            var newRoot = document.Root.ReplaceNode(innermostCommonBlock, newInnerMostBlock);
            return document.Document.WithSyntaxRoot(newRoot);
        }

        /*
        private static async Task<SyntaxNode> UpdateCallSitesAsync(
            SemanticDocument document,
            BlockSyntax block,
            ExpressionSyntax expression,
            IMethodSymbol methodSymbol,
            CancellationToken cancellationToken)
        {
            var progress = new StreamingProgressCollector();

            await SymbolFinder.FindReferencesAsync(
                methodSymbol, document.Document.Project.Solution, progress: progress,
                documents: null, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
            var referencedSymbols = progress.GetReferencedSymbols();
            var methods = referencedSymbols.Select(referencedSymbol => referencedSymbol.Definition).OfType<IMethodSymbol>().Distinct().ToImmutableArray();

        }*/

        private static bool IsBlockLike(SyntaxNode node) => node is BlockSyntax || node is SwitchSectionSyntax;

        protected override IEnumerable<SyntaxNode> GetContainingExecutableBlocks(ExpressionSyntax expression)
            => expression.GetAncestorsOrThis<BlockSyntax>();

        protected override bool CanReplace(ExpressionSyntax expression)
            => true;

        protected override bool IsExpressionInStaticLocalFunction(ExpressionSyntax expression)
        {
            var localFunction = expression.GetAncestor<LocalFunctionStatementSyntax>();
            return localFunction != null && localFunction.Modifiers.Any(SyntaxKind.StaticKeyword);
        }

        protected override TNode RewriteCore<TNode>(
            TNode node,
            SyntaxNode replacementNode,
            ISet<ExpressionSyntax> matches)
        {
            return (TNode)Rewriter.Visit(node, replacementNode, matches);
        }
    }
}
