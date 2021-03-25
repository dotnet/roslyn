// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal abstract partial class AbstractIntroduceParameterService<
        TExpressionSyntax,
        TInvocationExpressionSyntax,
        TIdentifierNameSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TInvocationExpressionSyntax : TExpressionSyntax
        where TIdentifierNameSyntax : TExpressionSyntax
    {
        protected abstract bool IsContainedInParameterizedDeclaration(SyntaxNode node);
        protected abstract SyntaxNode GenerateExpressionFromOptionalParameter(IParameterSymbol parameterSymbol);

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

            var methodSymbol = (IMethodSymbol)semanticModel.GetRequiredEnclosingSymbol(expression.SpanStart, cancellationToken);
            if (methodSymbol is null)
            {
                return;
            }

            var containingMethod = expression.FirstAncestorOrSelf<SyntaxNode>(node => IsContainedInParameterizedDeclaration(node), ascendOutOfTrivia: true);

            if (containingMethod is null)
            {
                return;
            }

            var actions = await AddActionsAsync(document, expression, methodSymbol, containingMethod, cancellationToken).ConfigureAwait(false);

            if (actions.Length == 0)
            {
                return;
            }

            var action = new CodeActionWithNestedActions(FeaturesResources.Introduce_parameter, actions, isInlinable: false);
            context.RegisterRefactoring(action, textSpan);
        }

        /// <summary>
        /// Introduces a new parameter and refactors all the call sites
        /// </summary>
        public async Task<Solution> IntroduceParameterAsync(Document document, TExpressionSyntax expression,
            IMethodSymbol methodSymbol, SyntaxNode containingMethod, bool allOccurrences, bool trampoline, bool overload,
            CancellationToken cancellationToken)
        {
            var parameterName = await GetNewParameterNameAsync(document, expression, cancellationToken).ConfigureAwait(false);

            var methodCallSites = await FindCallSitesAsync(document, methodSymbol, cancellationToken).ConfigureAwait(false);

            if (trampoline || overload)
            {
                return await RewriteSolutionWithNewMethodAsync(document,
                    expression, methodSymbol, containingMethod, allOccurrences, parameterName, methodCallSites,
                    trampoline, cancellationToken).ConfigureAwait(false);
            }

            return await RewriteSolutionAsync(document,
                expression, methodSymbol, containingMethod, methodCallSites, allOccurrences, parameterName,
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task<string> GetNewParameterNameAsync(Document document, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            if (ShouldRemoveVariableDeclaratorContainingExpression(document, expression, out var varDeclName))
            {
                return varDeclName;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
            return semanticFacts.GenerateNameForExpression(semanticModel, expression, capitalize: false, cancellationToken);
        }

        protected static bool ShouldRemoveVariableDeclaratorContainingExpression(
            Document document, TExpressionSyntax expression, [NotNullWhen(true)] out string? varDeclName)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var expressionDecl = expression?.Parent?.Parent;
            if (syntaxFacts.IsVariableDeclarator(expressionDecl))
            {
                varDeclName = syntaxFacts.GetIdentifierOfVariableDeclarator(expressionDecl).ValueText;
                return true;
            }

            varDeclName = null;
            return false;
        }

        /// <summary>
        /// Locates all the call sites of the method that introduced the parameter
        /// </summary>
        /// <returns>Dictionary tying all the invocations to each corresponding document</returns>
        protected static async Task<ImmutableDictionary<Document, ImmutableArray<TInvocationExpressionSyntax>>> FindCallSitesAsync(
            Document document, IMethodSymbol methodSymbol, CancellationToken cancellationToken)
        {
            var methodCallSitesBuilder = ImmutableDictionary.CreateBuilder<Document, ImmutableArray<TInvocationExpressionSyntax>>();
            var progress = new StreamingProgressCollector();
            await SymbolFinder.FindReferencesAsync(
                methodSymbol, document.Project.Solution, progress: progress,
                documents: null, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
            var referencedSymbols = progress.GetReferencedSymbols();

            // Ordering by descending to sort invocations by starting span to account for nested invocations
            var referencedLocations = referencedSymbols.SelectMany(referencedSymbol => referencedSymbol.Locations).Distinct()
                .OrderByDescending(reference => reference.Location.SourceSpan.Start);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // Adding the original document to account for refactorings that do not have any invocations
            methodCallSitesBuilder.Add(document, new ArrayBuilder<TInvocationExpressionSyntax>().ToImmutable());

            foreach (var refLocation in referencedLocations)
            {
                // Does not support cross-language references currently
                if (refLocation.Document.Project.Language == document.Project.Language)
                {
                    var invocation = refLocation.Location.FindNode(cancellationToken).GetRequiredParent();

                    // Only adding items that are of type InvocationExpressionSyntax
                    // TODO: in future, add annotation to items that aren't InvocationExpressionSyntax
                    if (!methodCallSitesBuilder.ContainsKey(refLocation.Document))
                    {
                        var invocations = new ArrayBuilder<TInvocationExpressionSyntax>();
                        if (syntaxFacts.IsInvocationExpression(invocation))
                        {
                            invocations.Add((TInvocationExpressionSyntax)invocation);
                            methodCallSitesBuilder.Add(refLocation.Document, invocations.ToImmutable());
                        }
                    }
                    else
                    {
                        if (syntaxFacts.IsInvocationExpression(invocation))
                        {
                            var invocations = methodCallSitesBuilder[refLocation.Document];
                            invocations = invocations.Add((TInvocationExpressionSyntax)invocation);
                            methodCallSitesBuilder[refLocation.Document] = invocations;
                        }
                    }
                }
            }
            return methodCallSitesBuilder.ToImmutable();
        }

        private static SeparatedSyntaxList<SyntaxNode> AddArgumentToArgumentList(
            SeparatedSyntaxList<SyntaxNode> invocationArguments, SyntaxGenerator generator,
            SyntaxNode newArgumentExpression, int insertionIndex, string name, bool named)
        {
            var argument = named
                ? generator.Argument(name, RefKind.None,
                    newArgumentExpression.WithAdditionalAnnotations(Simplifier.Annotation))
                :
                generator.Argument(newArgumentExpression.WithAdditionalAnnotations(Simplifier.Annotation));

            return invocationArguments.Insert(insertionIndex, argument);
        }

        /// <summary>
        /// Generates a method declaration containing a return expression of the highlighted expression
        /// Example:
        /// public void M(int x, int y)
        /// {
        ///     int f = x * y; // highlight this expression
        /// }
        /// 
        /// ---------------------------------------------------->
        /// 
        /// public int M_f(int x, int y)
        /// {
        ///     return x * y;
        /// }
        /// 
        /// public void M(int x, int y)
        /// {
        ///     int f = x * y;
        /// }
        /// </summary>
        private async Task GenerateNewMethodAsync(Document document, TExpressionSyntax expression,
            IMethodSymbol methodSymbol, SyntaxNode containingMethod, string newMethodIdentifier, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            // Remove trailing trivia because it adds spaces to the beginning of the following statement
            var newStatements = SpecializedCollections.SingletonEnumerable(editor.Generator.ReturnStatement(expression.WithoutTrailingTrivia()));

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeSymbol = semanticModel.GetTypeInfo(expression, cancellationToken).ConvertedType ?? semanticModel.Compilation.ObjectType;
            var newMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol, name: newMethodIdentifier, returnType: typeSymbol);

            var newMethodDeclaration = editor.Generator.MethodDeclaration(newMethod, newStatements);
            editor.InsertBefore(containingMethod, newMethodDeclaration);
        }

        /// <summary>
        /// Generates a method declaration containing a call to the method that introduced the parameter
        /// Example:
        /// public void M(int x, int y)
        /// {
        ///     int f = x * y; // highlight this expression
        /// }
        /// 
        /// ---------------------------------------------------->
        /// 
        /// public void M(int x, int y)
        /// {
        ///     M(x, y, x * y);
        /// }
        /// 
        /// public void M(int x, int y)
        /// {
        ///     int f = x * y;
        /// }
        /// </summary>
        private static void GenerateNewMethodOverload(TExpressionSyntax expression,
            IMethodSymbol methodSymbol, SyntaxNode containingMethod, SyntaxEditor editor)
        {
            var generator = editor.Generator;
            var arguments = generator.CreateArguments(methodSymbol.Parameters);

            // Remove trailing trivia because it adds spaces to the beginning of the following statement
            arguments = arguments.Add(generator.Argument(expression.WithoutTrailingTrivia()));
            var memberName = methodSymbol.IsGenericMethod
                ? generator.GenericName(methodSymbol.Name, methodSymbol.TypeArguments)
                : generator.IdentifierName(methodSymbol.Name);

            var invocation = generator.InvocationExpression(memberName, arguments);
            var newStatements = SpecializedCollections.SingletonEnumerable(invocation);

            if (!methodSymbol.ReturnsVoid)
            {
                newStatements = SpecializedCollections.SingletonEnumerable(generator.ReturnStatement(invocation));
            }

            var newMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol);
            var newMethodDeclaration = generator.MethodDeclaration(newMethod, newStatements);
            editor.InsertBefore(containingMethod, newMethodDeclaration);
        }

        public static async Task UpdateExpressionInOriginalFunctionAsync(Document document,
            TExpressionSyntax expression, SyntaxNode scope, string parameterName, SyntaxEditor editor,
            bool allOccurrences, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var matches = await FindMatchesAsync(document, expression, scope, allOccurrences, cancellationToken).ConfigureAwait(false);
            var replacement = (TIdentifierNameSyntax)generator.IdentifierName(parameterName);

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            foreach (var match in matches)
            {
                // Special case the removal of the originating expression to either remove the local declaration
                // or to add a rename annotation
                if (!match.Equals(expression))
                {
                    editor.ReplaceNode(match, replacement);
                }
                else
                {
                    if (ShouldRemoveVariableDeclaratorContainingExpression(document, expression, out var varDeclName))
                    {
                        var localDeclaration = expression.FirstAncestorOrSelf<SyntaxNode>(node => syntaxFacts.IsLocalDeclarationStatement(node));
                        Contract.ThrowIfNull(localDeclaration);
                        editor.RemoveNode(localDeclaration);
                    }
                    else
                    {
                        // Getting the SyntaxToken and adding a rename annotation to the identifier
                        replacement = (TIdentifierNameSyntax)generator.IdentifierName(generator.Identifier(parameterName)
                            .WithAdditionalAnnotations(RenameAnnotation.Create()));
                        editor.ReplaceNode(match, replacement);
                    }
                }
            }
        }

        /// <summary>
        /// Goes through the parameters of the original method to get the location that the parameter
        /// and argument should be introduced
        /// </summary>
        private static int GetInsertionIndex(Compilation compilation, IMethodSymbol methodSymbol,
            ISyntaxFactsService syntaxFacts, SyntaxNode methodDeclaration)
        {
            var parameterList = syntaxFacts.GetParameterList(methodDeclaration);
            Contract.ThrowIfNull(parameterList);
            var insertionIndex = 0;

            foreach (var parameterSymbol in methodSymbol.Parameters)
            {
                // Want to skip optional parameters, params parameters, and CancellationToken since they should be at
                // the end of the list.
                if (!parameterSymbol.HasExplicitDefaultValue && !parameterSymbol.IsParams &&
                        !parameterSymbol.Type.Equals(compilation.GetTypeByMetadataName(typeof(CancellationToken)?.FullName!)))
                {
                    insertionIndex++;
                }
            }

            return insertionIndex;
        }

        /// <summary>
        /// For the trampoline case, it goes through the invocations and adds an argument which is a 
        /// call to the extracted method
        /// Introduces a new method overload or new trampoline method
        /// Updates the original method site with a newly introduced parameter
        /// </summary>
        private async Task<Solution> ModifyDocumentInvocationsTrampolineAndIntroduceParameterAsync(
            Compilation compilation, Solution modifiedSolution, Document currentDocument, Document originalDocument,
            ImmutableArray<TInvocationExpressionSyntax> invocations, IMethodSymbol methodSymbol, SyntaxNode containingMethod,
            bool allOccurrences, string parameterName, TExpressionSyntax expression, bool trampoline, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(currentDocument);
            var semanticFacts = currentDocument.GetRequiredLanguageService<ISemanticFactsService>();
            var invocationSemanticModel = await currentDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var root = await currentDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, generator);
            var syntaxFacts = currentDocument.GetRequiredLanguageService<ISyntaxFactsService>();
            var insertionIndex = GetInsertionIndex(compilation, methodSymbol, syntaxFacts, containingMethod);
            var newMethodIdentifier = methodSymbol.Name + "_" + parameterName;

            if (trampoline)
            {
                foreach (var invocationExpression in invocations)
                {
                    editor.ReplaceNode(invocationExpression, (currentInvocation, _) =>
                    {
                        return GenerateNewInvocationExpressionForTrampoline(syntaxFacts, editor, currentInvocation,
                            invocationExpression, newMethodIdentifier, insertionIndex);
                    });
                }
            }

            if (currentDocument.Id == originalDocument.Id)
            {
                if (trampoline)
                {
                    await GenerateNewMethodAsync(originalDocument, expression, methodSymbol, containingMethod, newMethodIdentifier,
                        editor, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    GenerateNewMethodOverload(expression, methodSymbol, containingMethod, editor);
                }

                await UpdateExpressionInOriginalFunctionAsync(originalDocument, expression, containingMethod,
                    parameterName, editor, allOccurrences, cancellationToken).ConfigureAwait(false);
                var semanticModel = await originalDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var parameterType = semanticModel.GetTypeInfo(expression, cancellationToken).ConvertedType ?? semanticModel.Compilation.ObjectType;
                var refKind = syntaxFacts.GetRefKindOfArgument(expression);
                var parameter = generator.ParameterDeclaration(name: parameterName, type: generator.TypeExpression(parameterType), refKind: refKind);
                editor.InsertParameter(containingMethod, insertionIndex, parameter);
            }

            return modifiedSolution.WithDocumentSyntaxRoot(originalDocument.Id, editor.GetChangedRoot());

            // Adds an argument which is an invocation of the newly created method to the callsites 
            // of the method invocations where a parameter was added.
            // Example:
            // public void M(int x, int y)
            // {
            //     int f = x * y; // highlight this expression
            // }
            // 
            // public void M1()
            // {
            //     M(5, 6);
            // }
            //
            // ---------------------------------------------------->
            // 
            // public int M_f(int x, int y)
            // {
            //     return x * y;
            // }
            // 
            // public void M(int x, int y)
            // {
            //     int f = x * y;
            // }
            //
            // public void M1()
            // {
            //     M(5, 6, M_f(5, 6)); // This is the generated invocation which is a new argument at the call site
            // }
            static TInvocationExpressionSyntax GenerateNewInvocationExpressionForTrampoline(ISyntaxFactsService syntaxFacts,
                SyntaxEditor editor, SyntaxNode currentInvocation, TInvocationExpressionSyntax invocationExpression,
                string newMethodIdentifier, int insertionIndex)
            {
                var invocationArguments = syntaxFacts.GetArgumentsOfInvocationExpression(currentInvocation);
                var methodName = editor.Generator.IdentifierName(newMethodIdentifier);
                var expressionFromInvocation = syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression);
                var newMethodInvocation = editor.Generator.InvocationExpression(methodName, invocationArguments);
                var allArguments = invocationArguments.Insert(insertionIndex, newMethodInvocation);
                return (TInvocationExpressionSyntax)editor.Generator.InvocationExpression(expressionFromInvocation, allArguments);
            }
        }

        /// <summary>
        /// Goes through all of the invocations and replaces the expression with identifiers from the invocation 
        /// arguments and rewrites the call site with the updated expression as a new argument
        /// </summary>
        private async Task<Solution> RewriteSolutionWithNewMethodAsync(
            Document originalDocument, TExpressionSyntax expression, IMethodSymbol methodSymbol,
            SyntaxNode containingMethod, bool allOccurrences, string parameterName,
            ImmutableDictionary<Document, ImmutableArray<TInvocationExpressionSyntax>> callSites, bool trampoline,
            CancellationToken cancellationToken)
        {
            var modifiedSolution = originalDocument.Project.Solution;

            foreach (var (project, projectCallSites) in callSites.GroupBy(kvp => kvp.Key.Project))
            {
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                foreach (var (document, invocationExpressionList) in projectCallSites)
                {
                    modifiedSolution = await ModifyDocumentInvocationsTrampolineAndIntroduceParameterAsync(compilation,
                        modifiedSolution, document, originalDocument, invocationExpressionList, methodSymbol, containingMethod,
                        allOccurrences, parameterName, expression, trampoline, cancellationToken).ConfigureAwait(false);
                }
            }

            return modifiedSolution;
        }

        /// <summary>
        /// This method iterates through the arguments at the invocation location and maps them back
        /// to the identifiers in the expression to create a new expression as an argument
        /// </summary>
        private (TExpressionSyntax, bool) CreateNewArgumentExpression(SemanticModel invocationSemanticModel,
            Document document, ISyntaxFactsService syntaxFacts, Dictionary<TIdentifierNameSyntax, IParameterSymbol> mappingDictionary,
            SyntaxGenerator generator, TExpressionSyntax expression, TIdentifierNameSyntax variable,
            SeparatedSyntaxList<SyntaxNode> invocationArguments, SeparatedSyntaxList<SyntaxNode> updatedInvocationArguments,
            bool parameterIsNamed, CancellationToken cancellationToken)
        {
            if (mappingDictionary.TryGetValue(variable, out var mappedParameter))
            {
                var parameterMapped = false;
                var oldNode = expression.GetCurrentNode(variable);
                RoslynDebug.AssertNotNull(oldNode);
                for (var i = 0; i < invocationArguments.ToArray().Length; i++)
                {
                    var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
                    var argumentParameter = semanticFacts.FindParameterForArgument(invocationSemanticModel, invocationArguments.ToArray()[i], cancellationToken);
                    if (argumentParameter.Equals(mappedParameter, SymbolEqualityComparer.Default))
                    {
                        var updatedInvocationArgument = updatedInvocationArguments.ToArray()[i];
                        var argumentExpression = syntaxFacts.GetExpressionOfArgument(updatedInvocationArgument);
                        var parenthesizedArgumentExpression = generator.AddParentheses(argumentExpression, includeElasticTrivia: false);
                        expression = expression.ReplaceNode(oldNode, parenthesizedArgumentExpression);
                        parameterMapped = true;
                        break;
                    }
                }

                // This is special cased for optional parameters: if the invocation does not have an argument 
                // corresponding to the optional parameter, then it generates an expression from the optional parameter
                if (mappedParameter.HasExplicitDefaultValue && !parameterMapped)
                {
                    parameterIsNamed = true;
                    var generatedExpression = GenerateExpressionFromOptionalParameter(mappedParameter);
                    var parenthesizedGeneratedExpression = generator.AddParentheses(generatedExpression, includeElasticTrivia: false);
                    expression = expression.ReplaceNode(oldNode, parenthesizedGeneratedExpression);
                }
            }

            return (expression, parameterIsNamed);
        }

        /// <summary>
        /// This method goes through all the invocation sites and adds a new argument with the expression to be added
        /// It also introduces a parameter at the original method site
        /// </summary>
        private async Task<Solution> ModifyDocumentInvocationsAndIntroduceParameterAsync(Compilation compilation,
            Document originalDocument, Document document, Solution modifiedSolution, IMethodSymbol methodSymbol,
            SyntaxNode containingMethod, TExpressionSyntax expressionCopy, TExpressionSyntax expression,
            bool allOccurrences, string parameterName,
            ImmutableArray<TInvocationExpressionSyntax> invocations, CancellationToken cancellationToken)
        {
            var mappingDictionary = await MapExpressionToParametersAsync(originalDocument, expressionCopy, cancellationToken).ConfigureAwait(false);
            var generator = SyntaxGenerator.GetGenerator(document);
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, generator);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var insertionIndex = GetInsertionIndex(compilation, methodSymbol, syntaxFacts, containingMethod);

            foreach (var invocationExpression in invocations)
            {
                var newArgumentExpression = expression;
                var invocationArguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);
                var variablesInExpression = expressionCopy.DescendantNodes().OfType<TIdentifierNameSyntax>();
                var invocationSemanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                editor.ReplaceNode(invocationExpression, (currentInvo, _) =>
                {
                    var updatedInvocationArguments = syntaxFacts.GetArgumentsOfInvocationExpression(currentInvo);
                    var parameterIsNamed = false;

                    foreach (var variable in variablesInExpression)
                    {
                        (newArgumentExpression, parameterIsNamed) = CreateNewArgumentExpression(
                            invocationSemanticModel, document, syntaxFacts, mappingDictionary, generator,
                            newArgumentExpression, variable, invocationArguments, updatedInvocationArguments,
                            parameterIsNamed, cancellationToken);
                    }

                    var expressionFromInvocation = syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression);
                    var allArguments = AddArgumentToArgumentList(updatedInvocationArguments, generator,
                        newArgumentExpression.WithAdditionalAnnotations(Formatter.Annotation), insertionIndex, parameterName, parameterIsNamed);
                    var newInvo = editor.Generator.InvocationExpression(expressionFromInvocation, allArguments);
                    return newInvo;
                });
            }

            if (document.Id == originalDocument.Id)
            {
                await UpdateExpressionInOriginalFunctionAsync(originalDocument, expressionCopy, containingMethod, parameterName, editor, allOccurrences, cancellationToken).ConfigureAwait(false);
                var semanticModel = await originalDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var parameterType = semanticModel.GetTypeInfo(expressionCopy, cancellationToken).Type ?? semanticModel.Compilation.ObjectType;
                var refKind = syntaxFacts.GetRefKindOfArgument(expressionCopy);
                var parameter = generator.ParameterDeclaration(name: parameterName, type: generator.TypeExpression(parameterType), refKind: refKind);
                editor.InsertParameter(containingMethod, insertionIndex, parameter);
            }

            return modifiedSolution.WithDocumentSyntaxRoot(originalDocument.Id, editor.GetChangedRoot());
        }

        /// <summary>
        /// Goes through all of the invocations and replaces the expression with identifiers from the invocation 
        /// arguments and rewrites the call site with the updated expression as a new argument
        /// </summary>
        private async Task<Solution> RewriteSolutionAsync(Document originalDocument,
            TExpressionSyntax expression, IMethodSymbol methodSymbol, SyntaxNode containingMethod,
            ImmutableDictionary<Document, ImmutableArray<TInvocationExpressionSyntax>> callSites,
            bool allOccurrences, string parameterName, CancellationToken cancellationToken)
        {
            // Need a copy of the expression to use to find the original expression
            // Because it gets modified when tracking the nodes
            var expressionCopy = expression;
            var mappingDictionary = await MapExpressionToParametersAsync(originalDocument, expression, cancellationToken).ConfigureAwait(false);

            // Need to track the nodes in the expression so that they can be found later
            expression = expression.TrackNodes(mappingDictionary.Keys);
            var identifiers = expression.DescendantNodes().Where(node => node is TIdentifierNameSyntax);

            var modifiedSolution = originalDocument.Project.Solution;

            foreach (var (project, projectCallSites) in callSites.GroupBy(kvp => kvp.Key.Project))
            {
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                foreach (var (document, invocations) in projectCallSites)
                {
                    modifiedSolution = await ModifyDocumentInvocationsAndIntroduceParameterAsync(compilation,
                        originalDocument, document, modifiedSolution, methodSymbol, containingMethod, expressionCopy,
                        expression, allOccurrences, parameterName, invocations, cancellationToken).ConfigureAwait(false);
                }
            }

            return modifiedSolution;
        }

        /// <summary>
        /// Ties the identifiers within the expression back to their associated parameter
        /// </summary>
        public static async Task<Dictionary<TIdentifierNameSyntax, IParameterSymbol>> MapExpressionToParametersAsync(
            Document document, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var nameToParameterDict = new Dictionary<TIdentifierNameSyntax, IParameterSymbol>();
            var variablesInExpression = expression.DescendantNodes().OfType<TIdentifierNameSyntax>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var variable in variablesInExpression)
            {
                var symbol = semanticModel.GetSymbolInfo(variable, cancellationToken).Symbol;
                if (symbol is IParameterSymbol parameterSymbol)
                {
                    nameToParameterDict.Add(variable, parameterSymbol);
                }
            }

            return nameToParameterDict;
        }

        /// <summary>
        /// Determines if the expression is contained within something that is "parameterized"
        /// </summary>
        private static async Task<(bool isParameterized, bool hasOptionalParameter)> ShouldExpressionDisplayCodeActionAsync(
            Document document, TExpressionSyntax expression, IMethodSymbol methodSymbol,
            CancellationToken cancellationToken)
        {
            var variablesInExpression = expression.DescendantNodes().OfType<TIdentifierNameSyntax>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var hasOptionalParameter = false;

            foreach (var parameter in methodSymbol.Parameters)
            {
                if (parameter.HasExplicitDefaultValue)
                {
                    hasOptionalParameter = true;
                }
            }

            foreach (var variable in variablesInExpression)
            {
                var parameterSymbol = semanticModel.GetSymbolInfo(variable, cancellationToken).Symbol;
                if (parameterSymbol is not IParameterSymbol parameter)
                {
                    return (false, hasOptionalParameter);
                }
                if (parameter.IsParams)
                {
                    return (false, hasOptionalParameter);
                }
            }

            return (methodSymbol != null && methodSymbol.GetParameters().Any(), hasOptionalParameter);
        }

        private async Task<ImmutableArray<CodeAction>> AddActionsAsync(Document document,
            TExpressionSyntax expression, IMethodSymbol methodSymbol, SyntaxNode containingMethod,
            CancellationToken cancellationToken)
        {
            var actionsBuilder = new ArrayBuilder<CodeAction>();
            var (isParameterized, hasOptionalParameter) = await ShouldExpressionDisplayCodeActionAsync(document, expression, methodSymbol, cancellationToken).ConfigureAwait(false);
            if (isParameterized)
            {
                actionsBuilder.Add(CreateNewCodeAction(FeaturesResources.Introduce_parameter_for_0,
                    document, expression, methodSymbol, containingMethod, allOccurrences: false, trampoline: false, overload: false, cancellationToken));
                actionsBuilder.Add(CreateNewCodeAction(FeaturesResources.Introduce_parameter_for_all_occurrences_of_0,
                    document, expression, methodSymbol, containingMethod, allOccurrences: true, trampoline: false, overload: false, cancellationToken));

                if (!hasOptionalParameter && methodSymbol.MethodKind is not MethodKind.Constructor)
                {
                    actionsBuilder.Add(CreateNewCodeAction(
                        FeaturesResources.Extract_method_to_invoke_at_all_callsites_for_0, document, expression,
                        methodSymbol, containingMethod, allOccurrences: false, trampoline: true, overload: false, cancellationToken));
                    actionsBuilder.Add(CreateNewCodeAction(
                        FeaturesResources.Extract_method_to_invoke_at_all_callsites_for_all_occurrences_of_0, document,
                        expression, methodSymbol, containingMethod, allOccurrences: true, trampoline: true, overload: false, cancellationToken));

                    actionsBuilder.Add(CreateNewCodeAction(
                        FeaturesResources.Introduce_overload_with_new_parameter_for_0, document,
                        expression, methodSymbol, containingMethod, allOccurrences: false, trampoline: false, overload: true, cancellationToken));
                    actionsBuilder.Add(CreateNewCodeAction(
                         FeaturesResources.Introduce_overload_with_new_parameter_for_all_occurrences_of_0, document,
                         expression, methodSymbol, containingMethod, allOccurrences: true, trampoline: false, overload: true, cancellationToken));
                }
            }

            return actionsBuilder.ToImmutable();

            MyCodeAction CreateNewCodeAction(string actionName, Document document,
                TExpressionSyntax expression, IMethodSymbol methodSymbol, SyntaxNode containingMethod,
                bool allOccurrences, bool trampoline,
                bool overload, CancellationToken cancellationToken)
            {
                return new MyCodeAction(actionName, c => IntroduceParameterAsync(
                    document, expression, methodSymbol, containingMethod, allOccurrences, trampoline, overload, cancellationToken));
            }
        }

        /// <summary>
        /// Finds the matches of the expression within the same block
        /// </summary>
        protected static async Task<IEnumerable<TExpressionSyntax>> FindMatchesAsync(Document originalDocument,
            TExpressionSyntax expressionInOriginal, SyntaxNode withinNodeInCurrent,
            bool allOccurrences, CancellationToken cancellationToken)
        {
            var syntaxFacts = originalDocument.GetRequiredLanguageService<ISyntaxFactsService>();
            var originalSemanticModel = await originalDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var result = new HashSet<TExpressionSyntax>();
            var matches = from nodeInCurrent in withinNodeInCurrent.DescendantNodesAndSelf().OfType<TExpressionSyntax>()
                          where NodeMatchesExpression(originalSemanticModel, expressionInOriginal, nodeInCurrent, allOccurrences, cancellationToken)
                          select nodeInCurrent;
            return matches;
        }

        private static bool NodeMatchesExpression(SemanticModel originalSemanticModel,
            TExpressionSyntax expressionInOriginal, TExpressionSyntax nodeInCurrent, bool allOccurrences,
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
                    originalSemanticModel, originalSemanticModel, expressionInOriginal, nodeInCurrent))
                {
                    var originalOperation = originalSemanticModel.GetOperation(expressionInOriginal, cancellationToken);
                    if (originalOperation != null && IsInstanceMemberReference(originalOperation))
                    {
                        var currentOperation = originalSemanticModel.GetOperation(nodeInCurrent, cancellationToken);
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

        private class MyCodeAction : SolutionChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(title, createChangedSolution)
            {
            }
        }
    }
}
