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
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.IntroduceParameter;

internal abstract partial class AbstractIntroduceParameterCodeRefactoringProvider<
    TExpressionSyntax,
    TInvocationExpressionSyntax,
    TObjectCreationExpressionSyntax,
    TIdentifierNameSyntax,
    TArgumentSyntax> : CodeRefactoringProvider
    where TExpressionSyntax : SyntaxNode
    where TInvocationExpressionSyntax : TExpressionSyntax
    where TObjectCreationExpressionSyntax : TExpressionSyntax
    where TIdentifierNameSyntax : TExpressionSyntax
    where TArgumentSyntax : SyntaxNode
{
    private enum IntroduceParameterCodeActionKind
    {
        Refactor,
        Trampoline,
        Overload
    }

    protected abstract SyntaxNode GenerateExpressionFromOptionalParameter(IParameterSymbol parameterSymbol);
    protected abstract SyntaxNode UpdateArgumentListSyntax(SyntaxNode argumentList, SeparatedSyntaxList<TArgumentSyntax> arguments);
    protected abstract SyntaxNode? GetLocalDeclarationFromDeclarator(SyntaxNode variableDecl);
    protected abstract bool IsDestructor(IMethodSymbol methodSymbol);

    public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;
        if (document.Project.Solution.WorkspaceKind == WorkspaceKind.MiscellaneousFiles)
            return;

        var expression = await document.TryGetRelevantNodeAsync<TExpressionSyntax>(textSpan, cancellationToken).ConfigureAwait(false);
        if (expression == null || CodeRefactoringHelpers.IsNodeUnderselected(expression, textSpan))
            return;

        var generator = SyntaxGenerator.GetGenerator(document);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        if (!IsValidExpression(expression, syntaxFacts))
            return;

        var containingMethod = expression.FirstAncestorOrSelf<SyntaxNode>(node => generator.GetParameterListNode(node) is not null);
        if (containingMethod is null)
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var expressionType = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
        if (expressionType is null or IErrorTypeSymbol)
            return;

        var containingSymbol = semanticModel.GetDeclaredSymbol(containingMethod, cancellationToken);
        if (containingSymbol is not IMethodSymbol methodSymbol)
            return;

        var expressionSymbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;
        if (expressionSymbol is IParameterSymbol parameterSymbol && parameterSymbol.ContainingSymbol.Equals(containingSymbol))
            return;

        // Direct reference to named type or type parameter.  e.g. `$$Console.WriteLine()` or `T.Add(...)`.  These 
        // are effectively statics (not values) and cannot become parameter.
        if (expressionSymbol is INamedTypeSymbol or ITypeParameterSymbol)
            return;

        // Code actions for trampoline and overloads will not be offered if the method is a constructor.
        // Code actions for overloads will not be offered if the method if the method is a local function.
        var methodKind = methodSymbol.MethodKind;
        if (methodKind is not (MethodKind.Ordinary or MethodKind.LocalFunction or MethodKind.Constructor))
            return;

        if (IsDestructor(methodSymbol))
            return;

        var actions = await GetActionsAsync(document, expression, methodSymbol, containingMethod, cancellationToken).ConfigureAwait(false);
        if (actions is null)
            return;

        var nodeString = syntaxFacts.ConvertToSingleLine(expression).ToString();

        if (actions.Value.actions.Length > 0)
        {
            context.RegisterRefactoring(
                CodeAction.Create(
                    string.Format(FeaturesResources.Introduce_parameter_for_0, nodeString),
                    actions.Value.actions, isInlinable: false, priority: CodeActionPriority.Low),
                containingMethod.FullSpan);
        }

        if (actions.Value.actionsAllOccurrences.Length > 0)
        {
            context.RegisterRefactoring(
                CodeAction.Create(
                    string.Format(FeaturesResources.Introduce_parameter_for_all_occurrences_of_0, nodeString),
                    actions.Value.actionsAllOccurrences, isInlinable: false, priority: CodeActionPriority.Low),
                containingMethod.FullSpan);
        }
    }

    private static bool IsValidExpression(SyntaxNode expression, ISyntaxFactsService syntaxFacts)
    {
        // Need to special case for highlighting of method types because they are also "contained" within a method,
        // but it does not make sense to introduce a parameter in that case.
        if (syntaxFacts.IsInNamespaceOrTypeContext(expression))
            return false;

        // Need to special case for expressions whose direct parent is a MemberAccessExpression since they will
        // never introduce a parameter that makes sense in that case.
        if (syntaxFacts.IsNameOfAnyMemberAccessExpression(expression))
            return false;

        // Need to special case for the left-hand side of member initializers in regular objects (e.g., 'X' in 'new Foo { X = ... }')
        // because it does not make sense to introduce a parameter for the property/member name itself.
        if (syntaxFacts.IsMemberInitializerNamedAssignmentIdentifier(expression, out _))
            return false;

        // Need to special case for the left-hand side of member initializers in anonymous objects (e.g., 'a' in 'new { a = ... }').
        // This checks if the expression is the name identifier in an anonymous object member declarator.
        if (syntaxFacts.IsAnonymousObjectMemberDeclaratorNameIdentifier(expression))
            return false;

        // Need to special case for expressions that are contained within a parameter or attribute argument
        // because it is technically "contained" within a method, but does not make
        // sense to introduce.
        var invalidNode = expression.FirstAncestorOrSelf<SyntaxNode>(node => syntaxFacts.IsAttributeArgument(node) || syntaxFacts.IsParameter(node));
        return invalidNode is null;
    }

    /// <summary>
    /// Creates new code actions for each introduce parameter possibility.
    /// Does not create actions for overloads/trampoline if there are optional parameters or if the methodSymbol
    /// is a constructor.
    /// </summary>
    private async Task<(ImmutableArray<CodeAction> actions, ImmutableArray<CodeAction> actionsAllOccurrences)?> GetActionsAsync(Document document,
        TExpressionSyntax expression, IMethodSymbol methodSymbol, SyntaxNode containingMethod,
        CancellationToken cancellationToken)
    {
        var (shouldDisplay, containsClassExpression) = await ShouldExpressionDisplayCodeActionAsync(
            document, expression, cancellationToken).ConfigureAwait(false);
        if (!shouldDisplay)
            return null;

        using var actionsBuilder = TemporaryArray<CodeAction>.Empty;
        using var actionsBuilderAllOccurrences = TemporaryArray<CodeAction>.Empty;
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var methodCallSites = await FindCallSitesAsync(document, methodSymbol, cancellationToken).ConfigureAwait(false);

        if (!containsClassExpression)
        {
            actionsBuilder.Add(CreateNewCodeAction(FeaturesResources.and_update_call_sites_directly, allOccurrences: false, IntroduceParameterCodeActionKind.Refactor));
            actionsBuilderAllOccurrences.Add(CreateNewCodeAction(FeaturesResources.and_update_call_sites_directly, allOccurrences: true, IntroduceParameterCodeActionKind.Refactor));
        }

        if (methodSymbol.MethodKind is not MethodKind.Constructor)
        {
            var containsObjectCreationReferences = methodCallSites.Values.Flatten().OfType<TObjectCreationExpressionSyntax>().Any();
            if (!containsObjectCreationReferences)
            {
                actionsBuilder.Add(CreateNewCodeAction(
                    FeaturesResources.into_extracted_method_to_invoke_at_call_sites, allOccurrences: false, IntroduceParameterCodeActionKind.Trampoline));
                actionsBuilderAllOccurrences.Add(CreateNewCodeAction(
                    FeaturesResources.into_extracted_method_to_invoke_at_call_sites, allOccurrences: true, IntroduceParameterCodeActionKind.Trampoline));
            }

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
                cancellationToken => IntroduceParameterAsync(document, expression, methodSymbol, containingMethod, methodCallSites, allOccurrences, selectedCodeAction, cancellationToken),
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
                return default;

            if (symbol is IParameterSymbol parameter)
            {
                // We do not want to offer code actions if the expressions contains references
                // to params parameters because it is difficult to know what is being referenced
                // at the callsites.
                if (parameter.IsParams)
                    return default;
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
        IMethodSymbol methodSymbol, SyntaxNode containingMethod, Dictionary<Document, List<TExpressionSyntax>> methodCallSites, bool allOccurrences, IntroduceParameterCodeActionKind selectedCodeAction,
        CancellationToken cancellationToken)
    {
        var modifiedSolution = originalDocument.Project.Solution;
        var rewriter = new IntroduceParameterDocumentRewriter(this, originalDocument,
            expression, methodSymbol, containingMethod, selectedCodeAction, allOccurrences);

        var changedRoots = await ProducerConsumer<(DocumentId documentId, SyntaxNode newRoot)>.RunParallelAsync(
            source: methodCallSites.GroupBy(kvp => kvp.Key.Project),
            produceItems: static async (tuple, callback, rewriter, cancellationToken) =>
            {
                var (project, projectCallSites) = tuple;
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                await Parallel.ForEachAsync(
                    projectCallSites,
                    cancellationToken,
                    async (tuple, cancellationToken) =>
                    {
                        var (document, invocations) = tuple;
                        var newRoot = await rewriter.RewriteDocumentAsync(compilation, document, invocations, cancellationToken).ConfigureAwait(false);
                        callback((document.Id, newRoot));
                    }).ConfigureAwait(false);
            },
            args: rewriter,
            cancellationToken).ConfigureAwait(false);

        return modifiedSolution.WithDocumentSyntaxRoots(changedRoots);
    }

    /// <summary>
    /// Locates all the call sites of the method that introduced the parameter
    /// </summary>
    protected static async Task<Dictionary<Document, List<TExpressionSyntax>>> FindCallSitesAsync(
        Document document, IMethodSymbol methodSymbol, CancellationToken cancellationToken)
    {
        var methodCallSites = new Dictionary<Document, List<TExpressionSyntax>>();
        var progress = new StreamingProgressCollector();
        await SymbolFinder.FindReferencesAsync(
            methodSymbol, document.Project.Solution, progress,
            documents: null, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
        var referencedSymbols = progress.GetReferencedSymbols();

        // Ordering by descending to sort invocations by starting span to account for nested invocations
        var referencedLocations = referencedSymbols
            .SelectMany(referencedSymbol => referencedSymbol.Locations)
            .Distinct()
            .Where(reference => !reference.IsImplicit)
            .OrderByDescending(reference => reference.Location.SourceSpan.Start);

        // Adding the original document to ensure that it will be seen again when processing the call sites
        // in order to update the original expression and containing method.
        methodCallSites.Add(document, []);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        foreach (var refLocation in referencedLocations)
        {
            // Does not support cross-language references currently
            if (refLocation.Document.Project.Language == document.Project.Language)
            {
                var reference = refLocation.Location.FindNode(cancellationToken).GetRequiredParent();
                if (reference is not (TObjectCreationExpressionSyntax or TInvocationExpressionSyntax))
                    reference = reference.GetRequiredParent();

                // Only adding items that are of type InvocationExpressionSyntax or TObjectCreationExpressionSyntax
                var invocationOrCreation = reference as TObjectCreationExpressionSyntax ?? (TExpressionSyntax?)(reference as TInvocationExpressionSyntax);
                if (invocationOrCreation is null)
                    continue;

                if (!methodCallSites.TryGetValue(refLocation.Document, out var list))
                {
                    list = [];
                    methodCallSites.Add(refLocation.Document, list);
                }

                list.Add(invocationOrCreation);
            }
        }

        return methodCallSites;
    }
}
