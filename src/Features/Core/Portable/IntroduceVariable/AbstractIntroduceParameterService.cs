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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
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
        protected abstract bool GetContainingParameterizedDeclaration(SyntaxNode node);
        protected abstract SyntaxNode GenerateExpressionFromOptionalParameter(IParameterSymbol parameterSymbol);

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

        public async Task<CodeAction?> IntroduceParameterAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
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

            var actions = AddActions(semanticDocument, expression, cancellationToken);

            if (actions.Length > 0)
            {
                return new CodeActionWithNestedActions(FeaturesResources.Introduce_parameter, actions, isInlinable: true);
            }

            return null;
        }

        /// <summary>
        /// Introduces a new parameter and refactors all the call sites
        /// </summary>
        public async Task<Solution> IntroduceParameterAsync(SemanticDocument document, TExpressionSyntax expression,
            bool allOccurrences, bool trampoline, bool overload, CancellationToken cancellationToken)
        {
            var parameterName = GetNewParameterName(document, expression, cancellationToken);

            // MethodSymbol not null here since we know we're contained in something containing parameters at this point
            var methodSymbolInfo = (IMethodSymbol)document.SemanticModel.GetRequiredEnclosingSymbol(expression.SpanStart, cancellationToken);
            var methodCallSites = await FindCallSitesAsync(document, methodSymbolInfo, cancellationToken).ConfigureAwait(false);

            if (trampoline || overload)
            {
                return await RewriteSolutionWithNewMethodAsync(document,
                expression, methodSymbolInfo, allOccurrences, parameterName, methodCallSites, trampoline, cancellationToken).ConfigureAwait(false);
            }

            return await RewriteSolutionAsync(document,
                expression, methodCallSites, allOccurrences, parameterName, cancellationToken).ConfigureAwait(false);
        }

        protected static string GetNewParameterName(SemanticDocument document, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            if (ShouldRemoveVariableDeclaratorContainingExpression(document, expression, out var varDecl))
            {
                return varDecl;
            }

            var semanticFacts = document.Document.GetRequiredLanguageService<ISemanticFactsService>();
            return semanticFacts.GenerateNameForExpression(document.SemanticModel, expression, capitalize: false, cancellationToken);
        }

        protected static bool ShouldRemoveVariableDeclaratorContainingExpression(
            SemanticDocument document, TExpressionSyntax expression, out string varDecl)
        {
            var syntaxFacts = document.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            var expressionDecl = expression.GetRequiredParent().GetRequiredParent();
            varDecl = "";

            if (syntaxFacts.IsVariableDeclarator(expressionDecl))
            {
                varDecl = syntaxFacts.GetIdentifierOfVariableDeclarator(expressionDecl).ValueText;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the method symbol the expression is enclosed within
        /// </summary>
        protected IMethodSymbol? GetMethodSymbolFromExpression(SemanticDocument semanticDocument,
            TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var methodSymbol = (IMethodSymbol)semanticDocument.SemanticModel.GetRequiredEnclosingSymbol(expression.SpanStart, cancellationToken);
            var syntaxFacts = semanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            var methodDeclaration = expression.FirstAncestorOrSelf<SyntaxNode>(node => GetContainingParameterizedDeclaration(node), ascendOutOfTrivia: true);
            if (methodDeclaration is null)
            {
                return null;
            }

            return (IMethodSymbol)semanticDocument.SemanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken)!;
        }

        /// <summary>
        /// Locates all the call sites of the method that introduced the parameter
        /// </summary>
        /// <returns>Dictionary tying all the invocations to each corresponding document</returns>
        protected static async Task<ImmutableDictionary<Document, List<TInvocationExpressionSyntax>>> FindCallSitesAsync(
            SemanticDocument document, IMethodSymbol methodSymbol, CancellationToken cancellationToken)
        {
            var methodCallSites = new Dictionary<Document, List<TInvocationExpressionSyntax>>();
            var progress = new StreamingProgressCollector();

            await SymbolFinder.FindReferencesAsync(
                methodSymbol, document.Document.Project.Solution, progress: progress,
                documents: null, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
            var referencedSymbols = progress.GetReferencedSymbols();
            var referencedLocations = referencedSymbols.SelectMany(referencedSymbol => referencedSymbol.Locations).Distinct().ToImmutableArray()
                .OrderByDescending(reference => reference.Location.SourceSpan.Start);

            var list = new List<TInvocationExpressionSyntax>();
            methodCallSites.Add(document.Document, list);
            foreach (var refLocation in referencedLocations)
            {
                if (!methodCallSites.TryGetValue(refLocation.Document, out var invocations))
                {
                    invocations = new List<TInvocationExpressionSyntax>();
                    methodCallSites.Add(refLocation.Document, invocations);
                }

                invocations.Add((TInvocationExpressionSyntax)refLocation.Location.FindNode(cancellationToken).GetRequiredParent());
            }

            return methodCallSites.ToImmutableDictionary();
        }

        private static SeparatedSyntaxList<SyntaxNode> AddArgumentToArgumentList(
            SeparatedSyntaxList<SyntaxNode> invocationArguments, SyntaxGenerator generator,
            SyntaxNode newArgumentExpression, int insertionIndex, string name, bool named)
        {
            SyntaxNode argument;
            if (named)
            {
                argument = generator.Argument(name, RefKind.None, newArgumentExpression.WithAdditionalAnnotations(Simplifier.Annotation));
            }
            else
            {
                argument = generator.Argument(newArgumentExpression.WithAdditionalAnnotations(Simplifier.Annotation));
            }

            return invocationArguments.Insert(insertionIndex, argument);
        }

        private static ImmutableArray<SyntaxNode> AddExpressionArgumentToArgumentList(
            ImmutableArray<SyntaxNode> arguments, SyntaxNode expression, SyntaxGenerator generator)
        {
            var newArgument = generator.Argument(expression);
            return arguments.Add(newArgument);
        }

        /// <summary>
        /// Generates a method declaration containing a return expression of the highlighted expression
        /// </summary>
        private void GenerateNewMethod(SemanticDocument semanticDocument, TExpressionSyntax expression,
            IMethodSymbol methodSymbol, string newMethodIdentifier, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var document = semanticDocument.Document;
            var syntaxFacts = semanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            var oldMethodDeclaration = expression.FirstAncestorOrSelf<SyntaxNode>(node => GetContainingParameterizedDeclaration(node), true)!;
            var returnExpression = new List<SyntaxNode>
            {
                editor.Generator.ReturnStatement(expression.WithoutTrailingTrivia())
            };

            var typeSymbol = semanticDocument.SemanticModel.GetTypeInfo(expression, cancellationToken).Type;
            var newMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol, name: newMethodIdentifier, returnType: typeSymbol);

            var newMethodDeclaration = editor.Generator.MethodDeclaration(newMethod, returnExpression);
            editor.InsertBefore(oldMethodDeclaration, newMethodDeclaration);
        }

        /// <summary>
        /// Generates a method declaration containing a call to the method that introduced the parameter
        /// </summary>
        /// 
        private void GenerateNewMethodOverload(SemanticDocument semanticDocument, TExpressionSyntax expression,
            IMethodSymbol methodSymbol, SyntaxEditor editor)
        {
            var document = semanticDocument.Document;
            var syntaxFacts = semanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            var generator = editor.Generator;
            var oldMethodDeclaration = expression.FirstAncestorOrSelf<SyntaxNode>(node => GetContainingParameterizedDeclaration(node), true)!;
            var arguments = generator.CreateArguments(methodSymbol.Parameters);
            arguments = AddExpressionArgumentToArgumentList(arguments, expression.WithoutTrailingTrivia(), generator);
            var memberName = methodSymbol.IsGenericMethod
                ? generator.GenericName(methodSymbol.Name, methodSymbol.TypeArguments)
                : generator.IdentifierName(methodSymbol.Name);

            var invocation = generator.InvocationExpression(memberName, arguments);
            List<SyntaxNode> invocationReturn;

            if (methodSymbol.ReturnsVoid)
            {
                invocationReturn = new List<SyntaxNode>
                {
                    invocation
                };
            }
            else
            {
                invocationReturn = new List<SyntaxNode>
                {
                    generator.ReturnStatement(invocation)
                };
            }

            var newMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol);
            var newMethodDeclaration = generator.MethodDeclaration(newMethod, invocationReturn);
            editor.InsertBefore(oldMethodDeclaration, newMethodDeclaration);
        }

        public static void UpdateExpressionInOriginalFunction(SemanticDocument semanticDocument,
            TExpressionSyntax expression, SyntaxNode scope, string parameterName, SyntaxEditor editor,
            bool allOccurrences, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(semanticDocument.Document);
            var matches = FindMatches(semanticDocument, expression, semanticDocument, scope, allOccurrences, cancellationToken);
            var parameterNameSyntax = (TIdentifierNameSyntax)generator.IdentifierName(parameterName);

            // Parenthesize the variable, and go and replace anything we find with it.
            // NOTE: we do not want elastic trivia as we want to just replace the existing code 
            // as is, while preserving the trivia there.
            var replacement = generator.AddParentheses(parameterNameSyntax, includeElasticTrivia: false);

            var syntaxFacts = semanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();

            foreach (var match in matches)
            {
                if (match.Equals(expression))
                {
                    if (ShouldRemoveVariableDeclaratorContainingExpression(semanticDocument, expression, out var varDecl))
                    {
                        var localDeclaration = expression.FirstAncestorOrSelf<SyntaxNode>(node => syntaxFacts.IsLocalDeclarationStatement(node));
                        if (localDeclaration is not null)
                        {
                            editor.RemoveNode(localDeclaration);
                        }
                    }
                    else
                    {
                        parameterNameSyntax = (TIdentifierNameSyntax)generator.IdentifierName(generator.Identifier(parameterName).WithAdditionalAnnotations(RenameAnnotation.Create()));
                        replacement = generator.AddParentheses(parameterNameSyntax, includeElasticTrivia: false);
                        editor.ReplaceNode(match, replacement);
                    }
                }
                else
                {
                    editor.ReplaceNode(match, replacement);
                }
            }
        }

        private static int GetInsertionIndex(SemanticDocument document, Compilation compilation,
            ISyntaxFactsService syntaxFacts, SyntaxNode methodDeclaration, CancellationToken cancellationToken)
        {
            var parameterList = syntaxFacts.GetParameterList(methodDeclaration);
            Contract.ThrowIfNull(parameterList);
            var symbol = document.SemanticModel.GetDeclaredSymbol(parameterList.GetRequiredParent(), cancellationToken);
            Contract.ThrowIfNull(symbol);
            var parameterSymbolList = ((IMethodSymbol)symbol).Parameters;
            var insertionIndex = 0;

            foreach (var parameterSymbol in parameterSymbolList)
            {
                if (!parameterSymbol.HasExplicitDefaultValue && !parameterSymbol.IsParams && !parameterSymbol.Type.Equals(compilation.GetTypeByMetadataName(typeof(CancellationToken)?.FullName!)))
                {
                    insertionIndex++;
                }
            }

            return insertionIndex;
        }

        /// <summary>
        /// Adds an argument which is an invocation of the newly created method to the callsites 
        /// of the method invocations where a parameter was added
        /// </summary>
        private static SyntaxNode GenerateNewInvocationExpressionForTrampoline(ISyntaxFactsService syntaxFacts,
            SyntaxEditor editor, SyntaxNode currentInvocation, SyntaxNode invocationExpression,
            string newMethodIdentifier, int insertionIndex)
        {
            var invocationArguments = syntaxFacts.GetArgumentsOfInvocationExpression(currentInvocation);
            var methodName = editor.Generator.IdentifierName(newMethodIdentifier);
            var expressionFromInvocation = syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression);
            var newMethodInvocation = editor.Generator.InvocationExpression(methodName, invocationArguments);
            var allArguments = invocationArguments.Insert(insertionIndex, newMethodInvocation);
            return editor.Generator.InvocationExpression(expressionFromInvocation, allArguments);
        }

        /// <summary>
        /// Goes through all of the invocations and replaces the expression with identifiers from the invocation 
        /// arguments and rewrites the call site with the updated expression as a new argument
        /// </summary>
        private async Task<Solution> RewriteSolutionWithNewMethodAsync(
            SemanticDocument semanticDocument, TExpressionSyntax expression, IMethodSymbol methodSymbol,
            bool allOccurrences, string parameterName,
            ImmutableDictionary<Document, List<TInvocationExpressionSyntax>> callSites, bool trampoline,
            CancellationToken cancellationToken)
        {
            var firstCallSite = callSites.Keys.First();
            var modifiedSolution = firstCallSite.Project.Solution;
            var newMethodIdentifier = methodSymbol.Name + "_" + parameterName;

            foreach (var grouping in callSites.GroupBy(kvp => kvp.Key.Project))
            {
                var project = grouping.Key;
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                foreach (var (document, invocationExpressionList) in grouping)
                {
                    var generator = SyntaxGenerator.GetGenerator(document);
                    var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
                    var invocationSemanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                    var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var editor = new SyntaxEditor(root, generator);
                    var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                    var oldMethodDeclaration = expression.FirstAncestorOrSelf<SyntaxNode>(node => GetContainingParameterizedDeclaration(node), ascendOutOfTrivia: true)!;
                    var insertionIndex = GetInsertionIndex(semanticDocument, compilation, syntaxFacts, oldMethodDeclaration, cancellationToken);

                    if (trampoline)
                    {
                        foreach (var invocationExpression in invocationExpressionList)
                        {
                            editor.ReplaceNode(invocationExpression, (currentInvocation, _) =>
                            {
                                return GenerateNewInvocationExpressionForTrampoline(syntaxFacts, editor, currentInvocation, invocationExpression, newMethodIdentifier, insertionIndex);
                            });
                        }
                    }

                    if (document.Id == semanticDocument.Document.Id)
                    {
                        if (trampoline)
                        {
                            GenerateNewMethod(semanticDocument, expression, methodSymbol, newMethodIdentifier, editor, cancellationToken);
                        }
                        else
                        {
                            GenerateNewMethodOverload(semanticDocument, expression, methodSymbol, editor);
                        }

                        UpdateExpressionInOriginalFunction(semanticDocument, expression, oldMethodDeclaration, parameterName, editor, allOccurrences, cancellationToken);

                        var parameterType = semanticDocument.SemanticModel.GetTypeInfo(expression, cancellationToken).Type ?? semanticDocument.SemanticModel.Compilation.ObjectType;
                        var refKind = syntaxFacts.GetRefKindOfArgument(expression);
                        var parameter = generator.ParameterDeclaration(name: parameterName, type: generator.TypeExpression(parameterType), refKind: refKind);
                        editor.InsertParameter(oldMethodDeclaration, insertionIndex, parameter);
                    }

                    var newRoot = editor.GetChangedRoot();
                    modifiedSolution = modifiedSolution.WithDocumentSyntaxRoot(firstCallSite.Id, newRoot);
                }
            }

            return modifiedSolution;
        }

        /// <summary>
        /// Goes through all of the invocations and replaces the expression with identifiers from the invocation 
        /// arguments and rewrites the call site with the updated expression as a new argument
        /// </summary>
        private async Task<Solution> RewriteSolutionAsync(SemanticDocument semanticDocument,
            TExpressionSyntax expression, ImmutableDictionary<Document, List<TInvocationExpressionSyntax>> callSites,
            bool allOccurrences, string parameterName, CancellationToken cancellationToken)
        {
            // Need a copy of the expression to use to find the original expression
            // Because it gets modified when tracking the nodes
            var expressionCopy = expression;
            var mappingDictionary = MapExpressionToParameters(semanticDocument, expression, cancellationToken);
            expression = expression.TrackNodes(mappingDictionary.Keys);
            var identifiers = expression.DescendantNodes().Where(node => node is TIdentifierNameSyntax);

            var firstCallSite = callSites.Keys.First();
            var modifiedSolution = firstCallSite.Project.Solution;

            foreach (var grouping in callSites.GroupBy(kvp => kvp.Key.Project))
            {
                var project = grouping.Key;
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                foreach (var (document, invocationExpressionList) in grouping)
                {
                    var generator = SyntaxGenerator.GetGenerator(document);
                    var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
                    var invocationSemanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                    var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var editor = new SyntaxEditor(root, generator);
                    var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                    var oldMethodDeclaration = expressionCopy.FirstAncestorOrSelf<SyntaxNode>(node => GetContainingParameterizedDeclaration(node), ascendOutOfTrivia: true)!;
                    var insertionIndex = GetInsertionIndex(semanticDocument, compilation, syntaxFacts, oldMethodDeclaration, cancellationToken);

                    foreach (var invocationExpression in invocationExpressionList)
                    {
                        var newArgumentExpression = expression;
                        var invocationArguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);
                        var variablesInExpression = expressionCopy.DescendantNodes().OfType<TIdentifierNameSyntax>();

                        editor.ReplaceNode(invocationExpression, (currentInvo, _) =>
                        {
                            var updatedInvocationArguments = syntaxFacts.GetArgumentsOfInvocationExpression(currentInvo);
                            var parameterIsNamed = false;

                            foreach (var variable in variablesInExpression)
                            {
                                if (mappingDictionary.TryGetValue(variable, out var mappedParameter))
                                {
                                    var parameterMapped = false;
                                    var oldNode = newArgumentExpression.GetCurrentNode(variable);
                                    RoslynDebug.AssertNotNull(oldNode);
                                    for (var i = 0; i < invocationArguments.ToArray().Length; i++)
                                    {
                                        var argumentParameter = semanticFacts.FindParameterForArgument(invocationSemanticModel, invocationArguments.ToArray()[i], cancellationToken);
                                        if (argumentParameter.Equals(mappedParameter, SymbolEqualityComparer.Default))
                                        {
                                            var updatedInvocationArgument = updatedInvocationArguments.ToArray()[i];
                                            var argumentExpression = syntaxFacts.GetExpressionOfArgument(updatedInvocationArgument);
                                            var parenthesizedArgumentExpression = generator.AddParentheses(argumentExpression, includeElasticTrivia: false);
                                            newArgumentExpression = newArgumentExpression.ReplaceNode(oldNode, parenthesizedArgumentExpression);
                                            parameterMapped = true;
                                            break;
                                        }
                                    }

                                    if (mappedParameter.HasExplicitDefaultValue && !parameterMapped)
                                    {
                                        parameterIsNamed = true;
                                        var generatedExpression = GenerateExpressionFromOptionalParameter(mappedParameter);
                                        var parenthesizedGeneratedExpression = generator.AddParentheses(generatedExpression, includeElasticTrivia: false);
                                        newArgumentExpression = newArgumentExpression.ReplaceNode(oldNode, parenthesizedGeneratedExpression);
                                    }
                                }
                            }

                            var expressionFromInvocation = syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression);
                            var allArguments = AddArgumentToArgumentList(updatedInvocationArguments, generator,
                                newArgumentExpression.WithAdditionalAnnotations(Formatter.Annotation), insertionIndex, parameterName, parameterIsNamed);
                            var newInvo = editor.Generator.InvocationExpression(expressionFromInvocation, allArguments);
                            return newInvo;
                        });
                    }

                    if (document.Id == semanticDocument.Document.Id)
                    {
                        UpdateExpressionInOriginalFunction(semanticDocument, expressionCopy, oldMethodDeclaration, parameterName, editor, allOccurrences, cancellationToken);

                        var parameterType = semanticDocument.SemanticModel.GetTypeInfo(expressionCopy, cancellationToken).Type ?? semanticDocument.SemanticModel.Compilation.ObjectType;
                        var refKind = syntaxFacts.GetRefKindOfArgument(expressionCopy);
                        var parameter = generator.ParameterDeclaration(name: parameterName, type: generator.TypeExpression(parameterType), refKind: refKind);
                        editor.InsertParameter(oldMethodDeclaration, insertionIndex, parameter);
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
        public static Dictionary<TIdentifierNameSyntax, IParameterSymbol> MapExpressionToParameters(
            SemanticDocument document, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var nameToParameterDict = new Dictionary<TIdentifierNameSyntax, IParameterSymbol>();
            var variablesInExpression = expression.DescendantNodes().OfType<TIdentifierNameSyntax>();
            var semanticModel = document.SemanticModel;

            foreach (var variable in variablesInExpression)
            {
                var symbol = semanticModel.GetSymbolInfo(variable, cancellationToken).Symbol;
                if (symbol is IParameterSymbol parameterSymbol)
                {
                    if (!nameToParameterDict.ContainsKey(variable))
                    {
                        nameToParameterDict.Add(variable, parameterSymbol);
                    }
                }
            }

            return nameToParameterDict;
        }

        /// <summary>
        /// Determines if the expression is contained within something that is "parameterized"
        /// </summary>
        private static (bool isParameterized, bool hasOptionalParameter) ExpressionWithinParameterizedMethod(
            SemanticDocument semanticDocument, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var methodSymbol = (IMethodSymbol)semanticDocument.SemanticModel.GetRequiredEnclosingSymbol(expression.SpanStart, cancellationToken);

            var variablesInExpression = expression.DescendantNodes().OfType<TIdentifierNameSyntax>();
            var semanticModel = semanticDocument.SemanticModel;
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

        private ImmutableArray<CodeAction> AddActions(SemanticDocument semanticDocument,
            TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var actionsBuilder = new ArrayBuilder<CodeAction>();
            var (isParameterized, hasOptionalParameter) = ExpressionWithinParameterizedMethod(semanticDocument, expression, cancellationToken);
            if (isParameterized)
            {
                actionsBuilder.Add(new MyCodeAction(CreateDisplayText(allOccurrences: false, trampoline: false, overload: false),
                    c => IntroduceParameterAsync(semanticDocument, expression, false, false, false, cancellationToken)));
                actionsBuilder.Add(new MyCodeAction(CreateDisplayText(allOccurrences: true, trampoline: false, overload: false),
                    c => IntroduceParameterAsync(semanticDocument, expression, true, false, false, cancellationToken)));

                if (!hasOptionalParameter)
                {
                    actionsBuilder.Add(new MyCodeAction(CreateDisplayText(allOccurrences: false, trampoline: true, overload: false),
                        c => IntroduceParameterAsync(semanticDocument, expression, false, true, false, cancellationToken)));
                    actionsBuilder.Add(new MyCodeAction(CreateDisplayText(allOccurrences: true, trampoline: true, overload: false),
                        c => IntroduceParameterAsync(semanticDocument, expression, true, true, false, cancellationToken)));
                    actionsBuilder.Add(new MyCodeAction(CreateDisplayText(allOccurrences: false, trampoline: false, overload: true),
                        c => IntroduceParameterAsync(semanticDocument, expression, false, false, true, cancellationToken)));
                    actionsBuilder.Add(new MyCodeAction(CreateDisplayText(allOccurrences: true, trampoline: false, overload: true),
                        c => IntroduceParameterAsync(semanticDocument, expression, true, false, true, cancellationToken)));
                }
            }

            return actionsBuilder.ToImmutable();
        }

        private static string CreateDisplayText(bool allOccurrences, bool trampoline, bool overload)
                => (allOccurrences, trampoline, overload) switch
                {
                    (true, true, false) => FeaturesResources.Introduce_parameter_and_extract_method_for_all_occurrences_of_0,
                    (true, false, false) => FeaturesResources.Introduce_parameter_for_all_occurrences_of_0,
                    (true, false, true) => FeaturesResources.Introduce_new_parameter_overload_for_all_occurrences_of_0,
                    (false, true, false) => FeaturesResources.Introduce_parameter_and_extract_method_for_0,
                    (false, false, true) => FeaturesResources.Introduce_new_parameter_overload_for_0,
                    (false, false, false) => FeaturesResources.Introduce_parameter_for_0,
                    _ => throw new System.NotImplementedException()
                };

        /// <summary>
        /// Finds the matches of the expression within the same block
        /// </summary>
        protected static ISet<TExpressionSyntax> FindMatches(SemanticDocument originalDocument,
            TExpressionSyntax expressionInOriginal, SemanticDocument currentDocument, SyntaxNode withinNodeInCurrent,
            bool allOccurrences, CancellationToken cancellationToken)
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

        private static bool NodeMatchesExpression(SemanticModel originalSemanticModel,
            SemanticModel currentSemanticModel, TExpressionSyntax expressionInOriginal,
            TExpressionSyntax nodeInCurrent, bool allOccurrences, CancellationToken cancellationToken)
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

        private class MyCodeAction : SolutionChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(title, createChangedSolution)
            {
            }
        }
    }
}
