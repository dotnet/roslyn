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
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpIntroduceParameterService : AbstractIntroduceParameterService<CSharpIntroduceParameterService, ExpressionSyntax, MethodDeclarationSyntax, InvocationExpressionSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpIntroduceParameterService()
        {
        }

        protected override async Task<Solution> IntroduceParameterAsync(SemanticDocument document, ExpressionSyntax expression, bool allOccurrences, bool trampoline, CancellationToken cancellationToken)
        {
            var parameterName = GetNewParameterName(document, expression, cancellationToken);

            var annotatedExpression = new SyntaxAnnotation(ExpressionAnnotationKind);

            var annotatedSemanticDocument = await GetAnnotatedSemanticDocumentAsync(document, annotatedExpression, expression, cancellationToken).ConfigureAwait(false);
            var annotatedExpressionWithinDocument = (ExpressionSyntax)annotatedSemanticDocument.Root.GetAnnotatedNodesAndTokens(annotatedExpression).Single().AsNode();
            var methodSymbolInfo = GetMethodSymbolFromExpression(annotatedSemanticDocument, annotatedExpressionWithinDocument, cancellationToken);
            var methodCallSites = await FindCallSitesAsync(annotatedSemanticDocument, methodSymbolInfo, cancellationToken).ConfigureAwait(false);

            var updatedCallSitesSolution = await RewriteCallSitesAsync(annotatedExpressionWithinDocument, methodCallSites, methodSymbolInfo, cancellationToken).ConfigureAwait(false);

            if (updatedCallSitesSolution == null)
            {
                updatedCallSitesSolution = annotatedSemanticDocument.Document.Project.Solution;
            }

            var updatedCallSitesDocument = await SemanticDocument.CreateAsync(updatedCallSitesSolution.GetDocument(document.Document.Id), cancellationToken).ConfigureAwait(false);

            annotatedExpressionWithinDocument = (ExpressionSyntax)updatedCallSitesDocument.Root.GetAnnotatedNodesAndTokens(annotatedExpression).Single().AsNode();

            var updatedSolutionWithParameter = await AddParameterToMethodHeaderAsync(updatedCallSitesDocument, annotatedExpressionWithinDocument, parameterName, cancellationToken).ConfigureAwait(false);
            var updatedSemanticDocument = await SemanticDocument.CreateAsync(updatedSolutionWithParameter.GetDocument(document.Document.Id), cancellationToken).ConfigureAwait(false);

            var newExpression = (ExpressionSyntax)updatedSemanticDocument.Root.GetAnnotatedNodesAndTokens(annotatedExpression).Single().AsNode();
            var documentWithUpdatedMethodBody = ConvertExpressionWithNewParameter(updatedSemanticDocument, newExpression, parameterName, allOccurrences, cancellationToken);
            return documentWithUpdatedMethodBody.Project.Solution;
        }

        private static async Task<Solution> RewriteCallSitesAsync(ExpressionSyntax expression, ImmutableDictionary<Document, List<InvocationExpressionSyntax>> callSites,
         IMethodSymbol methodSymbol, CancellationToken cancellationToken)
        {
            var mappingDictionary = TieExpressionToParameters(expression, methodSymbol);
            expression = expression.TrackNodes(mappingDictionary.Values);
            var identifiers = expression.DescendantNodes().Where(node => node is IdentifierNameSyntax);

            if (!callSites.Keys.Any())
            {
                return null;
            }

            var firstCallSite = callSites.Keys.First();

            var currentSolution = firstCallSite.Project.Solution;
            foreach (var keyValuePair in callSites)
            {
                var document = currentSolution.GetDocument(keyValuePair.Key.Id);
                var invocationExpressionList = keyValuePair.Value;
                var generator = SyntaxGenerator.GetGenerator(document);
                var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
                var invocationSemanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var editor = new SyntaxEditor(await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false), generator);

                foreach (var invocationExpression in invocationExpressionList)
                {
                    var newArgumentExpression = expression;
                    var invocationArguments = invocationExpression.ArgumentList.Arguments;
                    foreach (var argument in invocationArguments)
                    {
                        var associatedParameter = semanticFacts.FindParameterForArgument(invocationSemanticModel, argument, cancellationToken);
                        var parenthesizedArgumentExpression = generator.AddParentheses(argument.Expression, false);
                        if (!mappingDictionary.TryGetValue(associatedParameter, out var value))
                        {
                            continue;
                        }

                        newArgumentExpression = newArgumentExpression.ReplaceNode(newArgumentExpression.GetCurrentNode(value), parenthesizedArgumentExpression);
                    }
                    var allArguments = invocationExpression.ArgumentList.Arguments.Add(SyntaxFactory.Argument(newArgumentExpression.WithoutAnnotations(ExpressionAnnotationKind).WithAdditionalAnnotations(Simplifier.Annotation)));
                    editor.ReplaceNode(invocationExpression, editor.Generator.InvocationExpression(invocationExpression.Expression, allArguments));
                }

                var newRoot = editor.GetChangedRoot();
                document = document.WithSyntaxRoot(newRoot);
                currentSolution = document.Project.Solution;
            }

            return currentSolution;
        }

        /*private static async Task<ImmutableDictionary<Document, List<InvocationExpressionSyntax>>> FindCallSitesAsync(
            SemanticDocument document,
            IMethodSymbol methodSymbol,
            CancellationToken cancellationToken)
        {
            var methodCallSites = new Dictionary<Document, List<InvocationExpressionSyntax>>();
            var progress = new StreamingProgressCollector();

            await SymbolFinder.FindReferencesAsync(
                methodSymbol, document.Document.Project.Solution, progress: progress,
                documents: null, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
            var referencedSymbols = progress.GetReferencedSymbols();
            var referencedLocations = referencedSymbols.SelectMany(referencedSymbol => referencedSymbol.Locations).Distinct().ToImmutableArray();

            foreach (var refLocation in referencedLocations)
            {
                if (!methodCallSites.TryGetValue(refLocation.Document, out var list))
                {
                    list = new List<InvocationExpressionSyntax>();
                    methodCallSites.Add(refLocation.Document, list);
                }
                list.Add((InvocationExpressionSyntax)(refLocation.Location.FindNode(cancellationToken).Parent));
            }

            return methodCallSites.ToImmutableDictionary();
        }*/

        /// <summary>
        /// Ties the identifiers within the expression back to their associated parameter
        /// </summary>
        /// <param name="expression"> The expression containing identifiers we are tying back to the parameters </param>
        /// <param name="methodSymbol"> The method containing the expression </param>
        /// <returns></returns>
        private static Dictionary<IParameterSymbol, IdentifierNameSyntax> TieExpressionToParameters(ExpressionSyntax expression, IMethodSymbol methodSymbol)
        {
            var nameToParameterDict = new Dictionary<IParameterSymbol, IdentifierNameSyntax>();
            var variablesInExpression = expression.DescendantNodes().Where(node => node is IdentifierNameSyntax);

            foreach (var variable in variablesInExpression)
            {
                foreach (var parameter in methodSymbol.Parameters)
                {
                    if ((string)((IdentifierNameSyntax)variable).Identifier.Value == parameter.Name)
                    {
                        nameToParameterDict.Add(parameter, (IdentifierNameSyntax)variable);
                        break;
                    }
                }
            }
            return nameToParameterDict;
        }

        protected override bool ExpressionWithinParameterizedMethod(ExpressionSyntax expression)
        {
            var methodExpression = expression.FirstAncestorOrSelf<MethodDeclarationSyntax>(node => node is MethodDeclarationSyntax, true);
            var variablesInExpression = expression.DescendantNodes().Where(node => node is IdentifierNameSyntax);
            var variableCount = 0;
            var parameterCount = 0;

            foreach (var variable in variablesInExpression)
            {
                variableCount++;
                foreach (var parameter in methodExpression.ParameterList.Parameters)
                {
                    if ((string)((IdentifierNameSyntax)variable).Identifier.Value == (string)parameter.Identifier.Value)
                    {
                        parameterCount++;
                        break;
                    }
                }
            }
            return variablesInExpression.Any() && methodExpression.ParameterList.Parameters.Any() && variableCount == parameterCount;
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
