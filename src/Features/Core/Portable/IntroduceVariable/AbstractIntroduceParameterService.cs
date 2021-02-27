// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddParameter;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal abstract partial class AbstractIntroduceParameterService<
        TService,
        TExpressionSyntax,
        TInvocationExpressionSyntax,
        TIdentifierNameSyntax> : CodeRefactoringProvider
        where TService : AbstractIntroduceParameterService<TService, TExpressionSyntax, TInvocationExpressionSyntax, TIdentifierNameSyntax>
        where TExpressionSyntax : SyntaxNode
        where TInvocationExpressionSyntax : SyntaxNode
        where TIdentifierNameSyntax : SyntaxNode
    {
        protected const string ExpressionAnnotationKind = nameof(ExpressionAnnotationKind);
        protected abstract Task<Solution> IntroduceParameterAsync(SemanticDocument document, TExpressionSyntax expression, bool allOccurrences, bool trampoline, CancellationToken cancellationToken);
        protected abstract SeparatedSyntaxList<SyntaxNode> AddArgumentToArgumentList(SeparatedSyntaxList<SyntaxNode> invocationArguments, SyntaxNode newArgumentExpression);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var action = await IntroduceParameterAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            if (action != null)
            {
                context.RegisterRefactoring(action, textSpan);
            }
        }

        public async Task<CodeAction?> IntroduceParameterAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var expression = await document.TryGetRelevantNodeAsync<TExpressionSyntax>(textSpan, cancellationToken).ConfigureAwait(false);
            if (expression == null || CodeRefactoringHelpers.IsNodeUnderselected(expression, textSpan))
            {
                return null;
            }

            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var expressionType = semanticDocument.SemanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (expressionType is null or IErrorTypeSymbol)
            {
                return null;
            }

            if (expression != null)
            {
                var (title, actions) = AddActions(semanticDocument, expression, cancellationToken);

                if (actions.Length > 0)
                {
                    return new CodeActionWithNestedActions(title, actions, isInlinable: true);
                }
            }

            return null;
        }

        public async Task<Solution> IntroduceParameterForRefactoringAsync(SemanticDocument document, TExpressionSyntax expression, bool allOccurrences, CancellationToken cancellationToken)
        {
            var parameterName = GetNewParameterName(document, expression, cancellationToken);

            var annotatedExpression = new SyntaxAnnotation(ExpressionAnnotationKind);

            var annotatedSemanticDocument = await GetAnnotatedSemanticDocumentAsync(document, annotatedExpression, expression, cancellationToken).ConfigureAwait(false);
            var annotatedExpressionWithinDocument = (TExpressionSyntax)annotatedSemanticDocument.Root.GetAnnotatedNodesAndTokens(annotatedExpression).Single().AsNode()!;
            var methodSymbolInfo = GetMethodSymbolFromExpression(annotatedSemanticDocument, annotatedExpressionWithinDocument, cancellationToken);
            var methodCallSites = await FindCallSitesAsync(annotatedSemanticDocument, methodSymbolInfo, cancellationToken).ConfigureAwait(false);

            var updatedCallSitesSolution = await RewriteCallSitesAsync(annotatedSemanticDocument, annotatedExpressionWithinDocument, methodCallSites, cancellationToken).ConfigureAwait(false);

            if (updatedCallSitesSolution == null)
            {
                updatedCallSitesSolution = annotatedSemanticDocument.Document.Project.Solution;
            }

            var updatedCallSitesDocument = await SemanticDocument.CreateAsync(updatedCallSitesSolution.GetDocument(document.Document.Id), cancellationToken).ConfigureAwait(false);

            annotatedExpressionWithinDocument = (TExpressionSyntax)updatedCallSitesDocument.Root.GetAnnotatedNodesAndTokens(annotatedExpression).Single().AsNode()!;

            var updatedSolutionWithParameter = await AddParameterToMethodHeaderAsync(updatedCallSitesDocument, annotatedExpressionWithinDocument, parameterName, cancellationToken).ConfigureAwait(false);
            var updatedSemanticDocument = await SemanticDocument.CreateAsync(updatedSolutionWithParameter.GetDocument(document.Document.Id), cancellationToken).ConfigureAwait(false);

            var newExpression = (TExpressionSyntax)updatedSemanticDocument.Root.GetAnnotatedNodesAndTokens(annotatedExpression).Single().AsNode()!;
            var documentWithUpdatedMethodBody = ConvertExpressionWithNewParameter(updatedSemanticDocument, newExpression, parameterName, allOccurrences, cancellationToken);
            return documentWithUpdatedMethodBody.Project.Solution;
        }

        /// <summary>
        /// Will be the method in which a new function is introduced that calls upon the updated method so that all refactorings are coalesced to one location
        /// </summary>
        /// <param name="document"></param>
        /// <param name="expression"></param>
        /// <param name="allOccurrences"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Solution> IntroduceParameterForTrampolineAsync(SemanticDocument document, TExpressionSyntax expression, bool allOccurrences, CancellationToken cancellationToken)
        {
            var parameterName = GetNewParameterName(document, expression, cancellationToken);

            var annotatedExpression = new SyntaxAnnotation(ExpressionAnnotationKind);

            var annotatedSemanticDocument = await GetAnnotatedSemanticDocumentAsync(document, annotatedExpression, expression, cancellationToken).ConfigureAwait(false);
            var annotatedExpressionWithinDocument = (TExpressionSyntax)annotatedSemanticDocument.Root.GetAnnotatedNodesAndTokens(annotatedExpression).Single().AsNode()!;
            // var methodSymbolInfo = GetMethodSymbolFromExpression(annotatedSemanticDocument, annotatedExpressionWithinDocument, cancellationToken);

            var updatedSolutionWithParameter = await AddParameterToMethodHeaderAsync(annotatedSemanticDocument, annotatedExpressionWithinDocument, parameterName, cancellationToken).ConfigureAwait(false);
            var updatedSemanticDocument = await SemanticDocument.CreateAsync(updatedSolutionWithParameter.GetDocument(document.Document.Id), cancellationToken).ConfigureAwait(false);

            var newExpression = (TExpressionSyntax)updatedSemanticDocument.Root.GetAnnotatedNodesAndTokens(annotatedExpression).Single().AsNode()!;
            var documentWithUpdatedMethodBody = ConvertExpressionWithNewParameter(updatedSemanticDocument, newExpression, parameterName, allOccurrences, cancellationToken);
            return documentWithUpdatedMethodBody.Project.Solution;
        }

        protected static Task<Solution> AddParameterToMethodHeaderAsync(SemanticDocument document, TExpressionSyntax expression, string parameterName, CancellationToken cancellationToken)
        {
            var invocationDocument = document.Document;
            var syntaxFacts = invocationDocument.GetRequiredLanguageService<ISyntaxFactsService>();
            var methodExpression = expression.FirstAncestorOrSelf<SyntaxNode>(node => syntaxFacts.isMethodDeclaration(node), ascendOutOfTrivia: true)!;
            var semanticModel = document.SemanticModel;
            var symbolInfo = (IMethodSymbol)semanticModel.GetRequiredDeclaredSymbol(methodExpression, cancellationToken);
            var parameterType = semanticModel.GetTypeInfo(expression, cancellationToken).Type ?? document.SemanticModel.Compilation.ObjectType;
            var refKind = syntaxFacts.GetRefKindOfArgument(expression);

            return AddParameterService.Instance.AddParameterAsync(
                invocationDocument,
                symbolInfo,
                parameterType,
                refKind,
                parameterName,
                newParameterIndex: null,
                fixAllReferences: false,
                cancellationToken);
        }

        protected static string GetNewParameterName(SemanticDocument document, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var invocationDocument = document.Document;
            var semanticModel = document.SemanticModel;
            var semanticFacts = invocationDocument.GetRequiredLanguageService<ISemanticFactsService>();
            return semanticFacts.GenerateNameForExpression(
                    semanticModel, expression, capitalize: false, cancellationToken: cancellationToken);
        }

        protected static IMethodSymbol GetMethodSymbolFromExpression(SemanticDocument annotatedSemanticDocument, TExpressionSyntax annotatedExpression, CancellationToken cancellationToken)
        {
            var syntaxFacts = annotatedSemanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            var methodExpression = annotatedExpression.FirstAncestorOrSelf<SyntaxNode>(node => syntaxFacts.isMethodDeclaration(node), true)!;
            return (IMethodSymbol)annotatedSemanticDocument.SemanticModel.GetDeclaredSymbol(methodExpression, cancellationToken)!;
        }

        protected static async Task<SemanticDocument> GetAnnotatedSemanticDocumentAsync(SemanticDocument document, SyntaxAnnotation annotatedExpression,
            TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var expressionWithAnnotation = expression.WithAdditionalAnnotations(annotatedExpression);
            var newDocument = document.Document.WithSyntaxRoot(document.Root.ReplaceNode(expression, expressionWithAnnotation));
            return await SemanticDocument.CreateAsync(newDocument, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Rewrites the expression with the new parameter
        /// </summary>
        /// <param name="document"> The document after changing the method header </param>
        /// <param name="newExpression"> The annotated expression </param>
        /// <param name="parameterName"> The parameter name that was added to the method header </param>
        /// <param name="allOccurrences"> Checks if we want to change all occurrences of the expression or just the original expression </param>
        /// <returns></returns>
        protected Document ConvertExpressionWithNewParameter(
            SemanticDocument document,
            TExpressionSyntax newExpression,
            string parameterName,
            bool allOccurrences,
            CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document.Document);
            var syntaxFacts = document.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            var parameterNameSyntax = (TIdentifierNameSyntax)generator.IdentifierName(parameterName);
            var scope = newExpression.Ancestors().FirstOrDefault(s => syntaxFacts.isMethodDeclaration(s));

            var matches = FindMatches(document, newExpression, document, scope, allOccurrences, cancellationToken);
            SyntaxNode? innermostCommonBlock = null;

            if (matches.Count > 1)
            {
                innermostCommonBlock = matches.FindInnermostCommonNode();
            }
            else
            {
                innermostCommonBlock = matches.Single().Parent;
            }

            RoslynDebug.AssertNotNull(innermostCommonBlock);

            var newExpressionCopy = newExpression;

            var newInnerMostBlock = Rewrite(
                document, newExpression, parameterNameSyntax, document, innermostCommonBlock, allOccurrences, cancellationToken);
            var newRoot = document.Root.ReplaceNode(innermostCommonBlock, newInnerMostBlock);
            return document.Document.WithSyntaxRoot(newRoot);
        }

        protected static async Task<ImmutableDictionary<Document, List<TInvocationExpressionSyntax>>> FindCallSitesAsync(
            SemanticDocument document,
            IMethodSymbol methodSymbol,
            CancellationToken cancellationToken)
        {
            var methodCallSites = new Dictionary<Document, List<TInvocationExpressionSyntax>>();
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
                    list = new List<TInvocationExpressionSyntax>();
                    methodCallSites.Add(refLocation.Document, list);
                }
                list.Add((TInvocationExpressionSyntax)refLocation.Location.FindNode(cancellationToken).Parent!);
            }

            return methodCallSites.ToImmutableDictionary();
        }

        /*
        private async Task<Solution> IntroduceMethodOverloadAsync(SemanticDocument semanticDocument, TExpressionSyntax expression, IMethodSymbol method, CancellationToken cancellationToken)
        {
            var document = semanticDocument.Document;
            var generator = SyntaxGenerator.GetGenerator(document);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, generator);
            generator.MethodDeclaration(method);
        }
        */

        private async Task<Solution?> RewriteCallSitesAsync(SemanticDocument semanticDocument, TExpressionSyntax expression, ImmutableDictionary<Document, List<TInvocationExpressionSyntax>> callSites,
            CancellationToken cancellationToken)
        {
            if (!callSites.Keys.Any())
            {
                return null;
            }

            var mappingDictionary = MapExpressionToParameters(semanticDocument, expression, cancellationToken);
            expression = expression.TrackNodes(mappingDictionary.Values);
            var identifiers = expression.DescendantNodes().Where(node => node is TIdentifierNameSyntax);

            var firstCallSite = callSites.Keys.First();

            var currentSolution = firstCallSite.Project.Solution;
            var modifiedSolution = currentSolution;

            foreach (var grouping in callSites.GroupBy(kvp => kvp.Key.Project))
            {
                var project = grouping.Key;
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                foreach (var keyValuePair in grouping)
                {
                    var document = keyValuePair.Key;
                    var invocationExpressionList = keyValuePair.Value;
                    var generator = SyntaxGenerator.GetGenerator(document);
                    var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
                    var invocationSemanticModel = compilation.GetSemanticModel(await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));

                    var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var editor = new SyntaxEditor(root, generator);
                    var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

                    foreach (var invocationExpression in invocationExpressionList)
                    {
                        var newArgumentExpression = expression;
                        var invocationArguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);
                        foreach (var argument in invocationArguments)
                        {
                            var associatedParameter = semanticFacts.FindParameterForArgument(invocationSemanticModel, argument, cancellationToken);
                            if (!mappingDictionary.TryGetValue(associatedParameter, out var value))
                            {
                                continue;
                            }

                            var argumentExpression = syntaxFacts.GetExpressionOfArgument(argument);
                            var parenthesizedArgumentExpression = generator.AddParentheses(argumentExpression, includeElasticTrivia: false);
                            newArgumentExpression = newArgumentExpression.ReplaceNode(newArgumentExpression.GetCurrentNode(value), parenthesizedArgumentExpression);
                        }
                        var expressionFromInvocation = syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression);
                        var allArguments = AddArgumentToArgumentList(invocationArguments, newArgumentExpression);
                        editor.ReplaceNode(invocationExpression, editor.Generator.InvocationExpression(expressionFromInvocation, allArguments));
                    }

                    var newRoot = editor.GetChangedRoot();
                    modifiedSolution = modifiedSolution.WithDocumentSyntaxRoot(firstCallSite.Id, newRoot);
                }
            }

            return modifiedSolution;
        }

        /// <summary>
        /// Ties the identifiers within the expression back to their associated parameter
        /// </summary>
        /// <param name="expression"> The expression containing identifiers we are tying back to the parameters </param>
        /// <returns></returns>
        public static Dictionary<IParameterSymbol, TIdentifierNameSyntax> MapExpressionToParameters(SemanticDocument document, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var nameToParameterDict = new Dictionary<IParameterSymbol, TIdentifierNameSyntax>();
            var variablesInExpression = expression.DescendantNodes().OfType<TIdentifierNameSyntax>();
            var semanticModel = document.SemanticModel;

            foreach (var variable in variablesInExpression)
            {
                var symbol = semanticModel.GetSymbolInfo(variable, cancellationToken).Symbol;
                if (symbol != null && symbol is IParameterSymbol parameterSymbol)
                {
                    nameToParameterDict.Add(parameterSymbol, variable);
                }
            }
            return nameToParameterDict;
        }

        private static bool ExpressionWithinParameterizedMethod(SemanticDocument semanticDocument, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var methodSymbol = GetMethodSymbolFromExpression(semanticDocument, expression, cancellationToken);
            var variablesInExpression = expression.DescendantNodes().OfType<TIdentifierNameSyntax>();
            var semanticModel = semanticDocument.SemanticModel;

            foreach (var variable in variablesInExpression)
            {
                var parameterSymbol = semanticModel.GetSymbolInfo(variable, cancellationToken).Symbol;
                if (parameterSymbol == null || parameterSymbol is not IParameterSymbol)
                {
                    return false;
                }
            }
            return methodSymbol.GetParameters().Any();
        }

        private (string title, ImmutableArray<CodeAction> actions) AddActions(SemanticDocument semanticDocument, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var actionsBuilder = new ArrayBuilder<CodeAction>();
            if (ExpressionWithinParameterizedMethod(semanticDocument, expression, cancellationToken))
            {
                actionsBuilder.Add(new IntroduceParameterCodeAction(semanticDocument, (TService)this, expression, false, false));
                actionsBuilder.Add(new IntroduceParameterCodeAction(semanticDocument, (TService)this, expression, false, true));

                actionsBuilder.Add(new IntroduceParameterCodeAction(semanticDocument, (TService)this, expression, true, false));
                actionsBuilder.Add(new IntroduceParameterCodeAction(semanticDocument, (TService)this, expression, true, true));
            }

            return (FeaturesResources.Introduce_parameter, actionsBuilder.ToImmutable());
        }

        protected static ISet<TExpressionSyntax> FindMatches(
           SemanticDocument originalDocument,
           TExpressionSyntax expressionInOriginal,
           SemanticDocument currentDocument,
           SyntaxNode withinNodeInCurrent,
           bool allOccurrences,
           CancellationToken cancellationToken)
        {
            var syntaxFacts = currentDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            var originalSemanticModel = originalDocument.SemanticModel;
            var currentSemanticModel = currentDocument.SemanticModel;

            var result = new HashSet<TExpressionSyntax>();
            var matches = from nodeInCurrent in withinNodeInCurrent.DescendantNodesAndSelf().OfType<TExpressionSyntax>()
                          where NodeMatchesExpression(originalSemanticModel, currentSemanticModel, expressionInOriginal, nodeInCurrent, allOccurrences, cancellationToken)
                          select nodeInCurrent;
            result.AddRange(matches.OfType<TExpressionSyntax>());

            return result;
        }

        private static bool NodeMatchesExpression(
            SemanticModel originalSemanticModel,
            SemanticModel currentSemanticModel,
            TExpressionSyntax expressionInOriginal,
            TExpressionSyntax nodeInCurrent,
            bool allOccurrences,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (nodeInCurrent == expressionInOriginal)
            {
                return true;
            }

            if (allOccurrences)
            {
                // Original expression and current node being semantically equivalent isn't enough when the original expression 
                // is a member access via instance reference (either implicit or explicit), the check only ensures that the expression
                // and current node are both backed by the same member symbol. So in this case, in addition to SemanticEquivalence check, 
                // we also check if expression and current node are both instance member access.
                //
                // For example, even though the first `c` binds to a field and we are introducing a local for it,
                // we don't want other references to that field to be replaced as well (i.e. the second `c` in the expression).
                //
                //  class C
                //  {
                //      C c;
                //      void Test()
                //      {
                //          var x = [|c|].c;
                //      }
                //  }

                if (SemanticEquivalence.AreEquivalent(
                    originalSemanticModel, currentSemanticModel, expressionInOriginal, nodeInCurrent))
                {
                    var originalOperation = originalSemanticModel.GetOperation(expressionInOriginal, cancellationToken);
                    if (originalOperation != null && IsInstanceMemberReference(originalOperation))
                    {
                        var currentOperation = currentSemanticModel.GetOperation(nodeInCurrent, cancellationToken);
                        return currentOperation != null && IsInstanceMemberReference(currentOperation);
                    }

                    return true;
                }
            }

            return false;
            static bool IsInstanceMemberReference(IOperation operation)
                => operation is IMemberReferenceOperation memberReferenceOperation &&
                    memberReferenceOperation.Instance?.Kind == OperationKind.InstanceReference;
        }

        protected TNode Rewrite<TNode>(
            SemanticDocument originalDocument,
            TExpressionSyntax expressionInOriginal,
            TIdentifierNameSyntax variableName,
            SemanticDocument currentDocument,
            TNode withinNodeInCurrent,
            bool allOccurrences,
            CancellationToken cancellationToken)
            where TNode : SyntaxNode
        {
            var generator = SyntaxGenerator.GetGenerator(originalDocument.Document);
            var matches = FindMatches(originalDocument, expressionInOriginal, currentDocument, withinNodeInCurrent, allOccurrences, cancellationToken);

            // Parenthesize the variable, and go and replace anything we find with it.
            // NOTE: we do not want elastic trivia as we want to just replace the existing code 
            // as is, while preserving the trivia there.  We do not want to update it.
            var syntaxFacts = originalDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            var expressionToken = syntaxFacts.GetIdentifierOfIdentifierName(variableName);
            var updatedExpressionToken = expressionToken.WithAdditionalAnnotations(RenameAnnotation.Create());
            variableName = variableName.ReplaceToken(expressionToken, updatedExpressionToken);

            var replacement = generator.AddParentheses(variableName, includeElasticTrivia: false)
                                         .WithAdditionalAnnotations(Formatter.Annotation);

            return RewriteCore(withinNodeInCurrent, replacement, matches);
        }

        protected abstract TNode RewriteCore<TNode>(
            TNode node,
            SyntaxNode replacementNode,
            ISet<TExpressionSyntax> matches)
            where TNode : SyntaxNode;

        private class IntroduceParameterCodeAction : AbstractIntroduceParameterCodeAction
        {
            internal IntroduceParameterCodeAction(
                SemanticDocument document,
                TService service,
                TExpressionSyntax expression,
                bool allOccurrences,
                bool trampoline)
                : base(document, service, expression, allOccurrences, trampoline)
            { }
        }
    }
}
