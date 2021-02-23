// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    internal partial class CSharpIntroduceParameterService : AbstractIntroduceParameterService<
        CSharpIntroduceParameterService,
        ExpressionSyntax,
        MethodDeclarationSyntax,
        InvocationExpressionSyntax,
        IdentifierNameSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpIntroduceParameterService()
        {
        }

        protected override async Task<Solution> IntroduceParameterAsync(SemanticDocument document, ExpressionSyntax expression, bool allOccurrences, bool trampoline, CancellationToken cancellationToken)
        {
            if (!trampoline)
            {
                return await IntroduceParameterForRefactoringAsync(document, expression, allOccurrences, cancellationToken).ConfigureAwait(false);

            }
            else
            {
                return await IntroduceParameterForTrampolineAsync(document, expression, allOccurrences, cancellationToken).ConfigureAwait(false);
            }
        }

        protected override async Task<Solution?> RewriteCallSitesAsync(SemanticDocument semanticDocument, ExpressionSyntax expression, ImmutableDictionary<Document, List<InvocationExpressionSyntax>> callSites,
         IMethodSymbol methodSymbol, CancellationToken cancellationToken)
        {
            if (!callSites.Keys.Any())
            {
                return null;
            }

            var mappingDictionary = MapExpressionToParameters(semanticDocument, expression, cancellationToken);
            expression = expression.TrackNodes(mappingDictionary.Values);
            var identifiers = expression.DescendantNodes().Where(node => node is IdentifierNameSyntax);

            var firstCallSite = callSites.Keys.First();

            var currentSolution = firstCallSite.Project.Solution;
            foreach (var keyValuePair in callSites)
            {
                var document = currentSolution.GetDocument(keyValuePair.Key.Id);
                if (document != null)
                {
                    var invocationExpressionList = keyValuePair.Value;
                    var generator = SyntaxGenerator.GetGenerator(document);
                    var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
                    var invocationSemanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    if (root != null)
                    {
                        var editor = new SyntaxEditor(root, generator);

                        foreach (var invocationExpression in invocationExpressionList)
                        {
                            var newArgumentExpression = expression;
                            var invocationArguments = invocationExpression.ArgumentList.Arguments;
                            foreach (var argument in invocationArguments)
                            {
                                var associatedParameter = semanticFacts.FindParameterForArgument(invocationSemanticModel, argument, cancellationToken);
                                if (!mappingDictionary.TryGetValue(associatedParameter, out var value))
                                {
                                    continue;
                                }

                                var parenthesizedArgumentExpression = generator.AddParentheses(argument.Expression, includeElasticTrivia: false);
                                newArgumentExpression = newArgumentExpression.ReplaceNode(newArgumentExpression.GetCurrentNode(value), parenthesizedArgumentExpression);
                            }

                            var allArguments =
                                invocationExpression.ArgumentList.Arguments.Add(SyntaxFactory.Argument(newArgumentExpression.WithoutAnnotations(ExpressionAnnotationKind).WithAdditionalAnnotations(Simplifier.Annotation)));
                            editor.ReplaceNode(invocationExpression, editor.Generator.InvocationExpression(invocationExpression.Expression, allArguments));
                        }

                        var newRoot = editor.GetChangedRoot();
                        document = document.WithSyntaxRoot(newRoot);
                        currentSolution = document.Project.Solution;
                    }
                }
            }

            return currentSolution;
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
