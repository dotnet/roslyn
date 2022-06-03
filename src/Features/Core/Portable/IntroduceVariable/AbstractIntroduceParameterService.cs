// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal abstract partial class AbstractIntroduceParameterService<
        TExpressionSyntax,
        TInvocationExpressionSyntax,
        TObjectCreationExpressionSyntax,
        TIdentifierNameSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TIdentifierNameSyntax : TExpressionSyntax
    {
        protected abstract SyntaxNode GenerateExpressionFromOptionalParameter(IParameterSymbol parameterSymbol);
        protected abstract SyntaxNode UpdateArgumentListSyntax(SyntaxNode argumentList, SeparatedSyntaxList<SyntaxNode> arguments);
        protected abstract SyntaxNode? GetLocalDeclarationFromDeclarator(SyntaxNode variableDecl);
        protected abstract bool IsDestructor(IMethodSymbol methodSymbol);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var expression = await document.TryGetRelevantNodeAsync<TExpressionSyntax>(textSpan, cancellationToken).ConfigureAwait(false);
            if (expression == null || CodeRefactoringHelpers.IsNodeUnderselected(expression, textSpan))
            {
                return;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var expressionType = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (expressionType is null or IErrorTypeSymbol)
            {
                return;
            }

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // Need to special case for expressions that are contained within a parameter
            // because it is technically "contained" within a method, but an expression in a parameter does not make
            // sense to introduce.
            var parameterNode = expression.FirstAncestorOrSelf<SyntaxNode>(node => syntaxFacts.IsParameter(node));
            if (parameterNode is not null)
            {
                return;
            }

            // Need to special case for highlighting of method types because they are also "contained" within a method,
            // but it does not make sense to introduce a parameter in that case.
            if (syntaxFacts.IsInNamespaceOrTypeContext(expression))
            {
                return;
            }

            // Need to special case for expressions whose direct parent is a MemberAccessExpression since they will
            // never introduce a parameter that makes sense in that case.
            if (syntaxFacts.IsNameOfAnyMemberAccessExpression(expression))
            {
                return;
            }

            var generator = SyntaxGenerator.GetGenerator(document);
            var containingMethod = expression.FirstAncestorOrSelf<SyntaxNode>(node => generator.GetParameterListNode(node) is not null);

            if (containingMethod is null)
            {
                return;
            }

            var containingSymbol = semanticModel.GetDeclaredSymbol(containingMethod, cancellationToken);
            if (containingSymbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            var expressionSymbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;
            if (expressionSymbol is IParameterSymbol parameterSymbol && parameterSymbol.ContainingSymbol.Equals(containingSymbol))
            {
                return;
            }

            // Code actions for trampoline and overloads will not be offered if the method is a constructor.
            // Code actions for overloads will not be offered if the method if the method is a local function.
            var methodKind = methodSymbol.MethodKind;
            if (methodKind is not (MethodKind.Ordinary or MethodKind.LocalFunction or MethodKind.Constructor))
            {
                return;
            }

            if (IsDestructor(methodSymbol))
            {
                return;
            }

            var actions = await GetActionsAsync(document, expression, methodSymbol, containingMethod, context.Options, cancellationToken).ConfigureAwait(false);

            if (actions is null)
            {
                return;
            }

            var singleLineExpression = syntaxFacts.ConvertToSingleLine(expression);
            var nodeString = singleLineExpression.ToString();

            if (actions.Value.actions.Length > 0)
            {
                context.RegisterRefactoring(CodeActionWithNestedActions.Create(
                    string.Format(FeaturesResources.Introduce_parameter_for_0, nodeString), actions.Value.actions, isInlinable: false, priority: CodeActionPriority.Low), textSpan);
            }

            if (actions.Value.actionsAllOccurrences.Length > 0)
            {
                context.RegisterRefactoring(CodeActionWithNestedActions.Create(
                    string.Format(FeaturesResources.Introduce_parameter_for_all_occurrences_of_0, nodeString), actions.Value.actionsAllOccurrences, isInlinable: false,
                    priority: CodeActionPriority.Low), textSpan);
            }
        }

        /// <summary>
        /// Creates new code actions for each introduce parameter possibility.
        /// Does not create actions for overloads/trampoline if there are optional parameters or if the methodSymbol
        /// is a constructor.
        /// </summary>
        private async Task<(ImmutableArray<CodeAction> actions, ImmutableArray<CodeAction> actionsAllOccurrences)?> GetActionsAsync(Document document,
            TExpressionSyntax expression, IMethodSymbol methodSymbol, SyntaxNode containingMethod, CodeGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var (shouldDisplay, containsClassExpression) = await ShouldExpressionDisplayCodeActionAsync(
                document, expression, cancellationToken).ConfigureAwait(false);
            if (!shouldDisplay)
            {
                return null;
            }

            using var actionsBuilder = TemporaryArray<CodeAction>.Empty;
            using var actionsBuilderAllOccurrences = TemporaryArray<CodeAction>.Empty;
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            if (!containsClassExpression)
            {
                actionsBuilder.Add(CreateNewCodeAction(FeaturesResources.and_update_call_sites_directly, allOccurrences: false, IntroduceParameterCodeActionKind.Refactor));
                actionsBuilderAllOccurrences.Add(CreateNewCodeAction(FeaturesResources.and_update_call_sites_directly, allOccurrences: true, IntroduceParameterCodeActionKind.Refactor));
            }

            if (methodSymbol.MethodKind is not MethodKind.Constructor)
            {
                actionsBuilder.Add(CreateNewCodeAction(
                    FeaturesResources.into_extracted_method_to_invoke_at_call_sites, allOccurrences: false, IntroduceParameterCodeActionKind.Trampoline));
                actionsBuilderAllOccurrences.Add(CreateNewCodeAction(
                    FeaturesResources.into_extracted_method_to_invoke_at_call_sites, allOccurrences: true, IntroduceParameterCodeActionKind.Trampoline));

                if (methodSymbol.MethodKind is not MethodKind.LocalFunction)
                {
                    actionsBuilder.Add(CreateNewCodeAction(
                        FeaturesResources.into_new_overload, allOccurrences: false, IntroduceParameterCodeActionKind.Overload));
                    actionsBuilderAllOccurrences.Add(CreateNewCodeAction(
                        FeaturesResources.into_new_overload, allOccurrences: true, IntroduceParameterCodeActionKind.Overload));
                }
            }

            return (actionsBuilder.ToImmutableAndClear(), actionsBuilderAllOccurrences.ToImmutableAndClear());

            // Local function to create a code action with more ease
            CodeAction CreateNewCodeAction(string actionName, bool allOccurrences, IntroduceParameterCodeActionKind selectedCodeAction)
            {
                return CodeAction.Create(
                    actionName,
                    c => IntroduceParameterAsync(document, expression, methodSymbol, containingMethod, allOccurrences, selectedCodeAction, fallbackOptions, c),
                    actionName);
            }
        }

        /// <summary>
        /// Determines if the expression is something that should have code actions displayed for it.
        /// Depends upon the identifiers in the expression mapping back to parameters.
        /// Does not handle params parameters.
        /// </summary>
        private static async Task<(bool shouldDisplay, bool containsClassExpression)> ShouldExpressionDisplayCodeActionAsync(
            Document document, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var variablesInExpression = expression.DescendantNodes();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var variable in variablesInExpression)
            {
                var symbol = semanticModel.GetSymbolInfo(variable, cancellationToken).Symbol;

                // If the expression contains locals or range variables then we do not want to offer
                // code actions since there will be errors at call sites.
                if (symbol is IRangeVariableSymbol or ILocalSymbol)
                {
                    return (false, false);
                }

                if (symbol is IParameterSymbol parameter)
                {
                    // We do not want to offer code actions if the expressions contains references
                    // to params parameters because it is difficult to know what is being referenced
                    // at the callsites.
                    if (parameter.IsParams)
                    {
                        return (false, false);
                    }
                }
            }

            // If expression contains this or base keywords, implicitly or explicitly,
            // then we do not want to refactor call sites that are not overloads/trampolines
            // because we do not know if the class specific information is available in other documents.
            var operation = semanticModel.GetOperation(expression, cancellationToken);
            var containsClassSpecificStatement = false;
            if (operation is not null)
            {
                containsClassSpecificStatement = operation.Descendants().Any(op => op.Kind == OperationKind.InstanceReference);
            }

            return (true, containsClassSpecificStatement);
        }

        /// <summary>
        /// Introduces a new parameter and refactors all the call sites based on the selected code action.
        /// </summary>
        private async Task<Solution> IntroduceParameterAsync(Document originalDocument, TExpressionSyntax expression,
            IMethodSymbol methodSymbol, SyntaxNode containingMethod, bool allOccurrences, IntroduceParameterCodeActionKind selectedCodeAction,
            CodeGenerationOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var methodCallSites = await FindCallSitesAsync(originalDocument, methodSymbol, cancellationToken).ConfigureAwait(false);

            var modifiedSolution = originalDocument.Project.Solution;
            var rewriter = new IntroduceParameterDocumentRewriter(this, originalDocument,
                expression, methodSymbol, containingMethod, selectedCodeAction, fallbackOptions, allOccurrences);

            foreach (var (project, projectCallSites) in methodCallSites.GroupBy(kvp => kvp.Key.Project))
            {
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                foreach (var (document, invocations) in projectCallSites)
                {
                    var newRoot = await rewriter.RewriteDocumentAsync(compilation, document, invocations, cancellationToken).ConfigureAwait(false);
                    modifiedSolution = modifiedSolution.WithDocumentSyntaxRoot(document.Id, newRoot);
                }
            }

            return modifiedSolution;
        }

        /// <summary>
        /// Locates all the call sites of the method that introduced the parameter
        /// </summary>
        protected static async Task<Dictionary<Document, List<SyntaxNode>>> FindCallSitesAsync(
            Document document, IMethodSymbol methodSymbol, CancellationToken cancellationToken)
        {
            var methodCallSites = new Dictionary<Document, List<SyntaxNode>>();
            var progress = new StreamingProgressCollector();
            await SymbolFinder.FindReferencesAsync(
                methodSymbol, document.Project.Solution, progress,
                documents: null, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
            var referencedSymbols = progress.GetReferencedSymbols();

            // Ordering by descending to sort invocations by starting span to account for nested invocations
            var referencedLocations = referencedSymbols.SelectMany(referencedSymbol => referencedSymbol.Locations)
                .Distinct().Where(reference => !reference.IsImplicit)
                .OrderByDescending(reference => reference.Location.SourceSpan.Start);

            // Adding the original document to ensure that it will be seen again when processing the call sites
            // in order to update the original expression and containing method.
            methodCallSites.Add(document, new List<SyntaxNode>());
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            foreach (var refLocation in referencedLocations)
            {
                // Does not support cross-language references currently
                if (refLocation.Document.Project.Language == document.Project.Language)
                {
                    var reference = refLocation.Location.FindNode(cancellationToken).GetRequiredParent();
                    if (reference is not (TObjectCreationExpressionSyntax or TInvocationExpressionSyntax))
                    {
                        reference = reference.GetRequiredParent();
                    }

                    // Only adding items that are of type InvocationExpressionSyntax or TObjectCreationExpressionSyntax
                    var invocationOrCreation = reference as TObjectCreationExpressionSyntax ?? (SyntaxNode?)(reference as TInvocationExpressionSyntax);
                    if (invocationOrCreation is null)
                    {
                        continue;
                    }

                    if (!methodCallSites.TryGetValue(refLocation.Document, out var list))
                    {
                        list = new List<SyntaxNode>();
                        methodCallSites.Add(refLocation.Document, list);
                    }

                    list.Add(invocationOrCreation);
                }
            }

            return methodCallSites;
        }

        private enum IntroduceParameterCodeActionKind
        {
            Refactor,
            Trampoline,
            Overload
        }
    }
}
