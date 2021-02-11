// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpIntroduceParameterService : AbstractIntroduceParameterService<CSharpIntroduceParameterService, ExpressionSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
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
            var newDocument = document.Document.WithSyntaxRoot(document.Root.ReplaceNode(expression, expressionWithAnnotation));
            var newSemanticDocument = await SemanticDocument.CreateAsync(newDocument, cancellationToken).ConfigureAwait(false);
            var updatedExpression = (ExpressionSyntax)newSemanticDocument.Root.GetAnnotatedNodesAndTokens(annotatedExpression).Single().AsNode();

            var methodExpression = expression.FirstAncestorOrSelf<MethodDeclarationSyntax>(node => node is MethodDeclarationSyntax, true);
            var symbolInfo = semanticModel.GetDeclaredSymbol(methodExpression, cancellationToken);

            var updatedSolutionWithParameter = await AddParameterAsync(newSemanticDocument, updatedExpression, parameterName, cancellationToken).ConfigureAwait(false);
            var updatedSemanticDocument = await SemanticDocument.CreateAsync(updatedSolutionWithParameter.GetDocument(document.Document.Id), cancellationToken).ConfigureAwait(false);

            var newExpression = (ExpressionSyntax)updatedSemanticDocument.Root.GetAnnotatedNodesAndTokens(annotatedExpression).Single().AsNode();
            return await IntroduceParameterConvertExpressionAsync(document, updatedSemanticDocument, newExpression, symbolInfo, (NameSyntax)syntaxGenerator.IdentifierName(parameterName), allOccurrences, cancellationToken).ConfigureAwait(false);
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
            SemanticDocument originalDocument,
            SemanticDocument document,
            ExpressionSyntax newExpression,
            IMethodSymbol methodSymbol,
            NameSyntax parameterName,
            bool allOccurrences,
            CancellationToken cancellationToken)
        {
            var block = (BlockSyntax)newExpression.Ancestors().FirstOrDefault(s => s is BlockSyntax);
            SyntaxNode scope = block;

            // If we're within a non-static local function, our scope for the new local declaration is expanded to include the enclosing member.
            /*var localFunction = block.GetAncestor<LocalFunctionStatementSyntax>();
            if (localFunction != null && !localFunction.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)))
            {
                scope = block.GetAncestor<MemberDeclarationSyntax>();
            }*/

            var matches = FindMatches(document, newExpression, document, scope, allOccurrences, cancellationToken);
            Debug.Assert(matches.Contains(newExpression));

            var innermostCommonBlock = matches.Single().Parent;

            var methodCallSites = await FindCallSitesAsync(originalDocument, methodSymbol, cancellationToken).ConfigureAwait(false);
            foreach (var callSite in methodCallSites)
            {

            }

            var dict = TieExpressionToParameters(newExpression);
            var newInnerMostBlock = Rewrite(
                document, newExpression, parameterName, document, innermostCommonBlock, allOccurrences, cancellationToken);
            var newRoot = document.Root.ReplaceNode(innermostCommonBlock, newInnerMostBlock);

            return document.Document.WithSyntaxRoot(newRoot);
        }

        private static async Task<ImmutableArray<SyntaxNode>> FindCallSitesAsync(
            SemanticDocument document,
            IMethodSymbol methodSymbol,
            CancellationToken cancellationToken)
        {
            var progress = new StreamingProgressCollector();

            await SymbolFinder.FindReferencesAsync(
                methodSymbol, document.Document.Project.Solution, progress: progress,
                documents: null, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
            var referencedSymbols = progress.GetReferencedSymbols();
            var referencedLocations = referencedSymbols.SelectMany(referencedSymbol => referencedSymbol.Locations).Distinct().ToImmutableArray();
            var methodCallSites = new ArrayBuilder<SyntaxNode>();

            foreach (var refLocation in referencedLocations)
            {
                methodCallSites.Add(refLocation.Location.FindNode(cancellationToken).Parent);
            }

            return methodCallSites.ToImmutable();
        }

        private static Dictionary<IdentifierNameSyntax, ParameterSyntax> TieExpressionToParameters(ExpressionSyntax expression)
        {
            var nameToParameterDict = new Dictionary<IdentifierNameSyntax, ParameterSyntax>();
            var y = expression.DescendantNodes().Where(node => node is IdentifierNameSyntax);
            return nameToParameterDict;
        }

        protected override bool ExpressionWithinParameterizedMethod(ExpressionSyntax expression)
        {
            var outerMethod = expression.FirstAncestorOrSelf<MethodDeclarationSyntax>(node => node is MethodDeclarationSyntax, true);
            return outerMethod.ParameterList.Parameters.Count > 0;
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
