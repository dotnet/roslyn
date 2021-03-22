// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        protected abstract SeparatedSyntaxList<SyntaxNode> AddArgumentToArgumentList(SeparatedSyntaxList<SyntaxNode> invocationArguments,
            SyntaxNode newArgumentExpression, int insertionIndex, string name, bool named);
        protected abstract bool IsMethodDeclaration(SyntaxNode node);
        protected abstract ImmutableArray<SyntaxNode> AddExpressionArgumentToArgumentList(ImmutableArray<SyntaxNode> arguments, SyntaxNode expression);
        protected abstract List<IParameterSymbol> GetParameterList(SemanticDocument document, SyntaxNode parameterList, CancellationToken cancellationToken);
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

        public async Task<Solution> IntroduceParameterAsync(SemanticDocument document, TExpressionSyntax expression, bool allOccurrences, bool trampoline, bool overload, CancellationToken cancellationToken)
        {
            if (trampoline || overload)
            {
                return await IntroduceParameterForTrampolineAsync(document, expression, allOccurrences, trampoline, cancellationToken).ConfigureAwait(false);
            }
            return await IntroduceParameterForRefactoringAsync(document, expression, allOccurrences, cancellationToken).ConfigureAwait(false);
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

            var (title, actions) = AddActions(semanticDocument, expression, cancellationToken);

            if (actions.Length > 0)
            {
                return new CodeActionWithNestedActions(title, actions, isInlinable: true);
            }

            return null;
        }

        /// <summary>
        /// Introduces a new parameter and refactors all the call sites
        /// </summary>
        public async Task<Solution> IntroduceParameterForRefactoringAsync(SemanticDocument document, TExpressionSyntax expression, bool allOccurrences, CancellationToken cancellationToken)
        {
            var parameterName = GetNewParameterName(document, expression, cancellationToken);

            // MethodSymbol not null here since we know we're contained in something containing parameters at this point
            var methodSymbolInfo = GetMethodSymbolFromExpression(document, expression, cancellationToken)!;
            var methodCallSites = await FindCallSitesAsync(document, methodSymbolInfo, cancellationToken).ConfigureAwait(false);

            var updatedCallSitesSolution = await RewriteCallSitesAsync(document, 
                expression, methodCallSites, allOccurrences, parameterName, cancellationToken).ConfigureAwait(false);
            return updatedCallSitesSolution;
        }

        /// <summary>
        /// Introduces a new parameter and a new method that calls upon the updated method so that all
        /// refactorings are coalesced to one location
        /// </summary>
        public async Task<Solution> IntroduceParameterForTrampolineAsync(SemanticDocument document, TExpressionSyntax expression, bool allOccurrences, bool trampoline, CancellationToken cancellationToken)
        {
            var parameterName = GetNewParameterName(document, expression, cancellationToken);

            // MethodSymbol not null here since we know we're contained in something containing parameters at this point
            var methodSymbolInfo = GetMethodSymbolFromExpression(document, expression, cancellationToken)!;
            var methodCallSites = await FindCallSitesAsync(document, methodSymbolInfo, cancellationToken).ConfigureAwait(false);
            var updatedInvocationsSolution = await RewriteCallSitesWithNewMethodAsync(document,
                expression, methodSymbolInfo, allOccurrences, parameterName, methodCallSites, trampoline, cancellationToken).ConfigureAwait(false);
            return updatedInvocationsSolution;
        }

        protected static string GetNewParameterName(SemanticDocument document, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            // IsExpressionRemovable checks if the expression's parent and parent's parent are not null, so we can ignore any warnings here
            if (IsExpressionRemovable(document, expression))
            {
                var syntaxFacts = document.Document.GetRequiredLanguageService<ISyntaxFactsService>();
                return syntaxFacts.GetIdentifierOfVariableDeclarator(expression.Parent!.Parent!).ValueText;
            }

            var semanticFacts = document.Document.GetRequiredLanguageService<ISemanticFactsService>();
            return semanticFacts.GenerateNameForExpression(document.SemanticModel, expression, capitalize: false, cancellationToken);
        }

        protected static bool IsExpressionRemovable(SemanticDocument document, TExpressionSyntax expression)
        {
            var syntaxFacts = document.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            return expression.Parent is not null && expression.Parent.Parent is not null && syntaxFacts.IsVariableDeclarator(expression.Parent.Parent);
        }

        /// <summary>
        /// Gets the method symbol the expression is enclosed within
        /// </summary>
        protected IMethodSymbol? GetMethodSymbolFromExpression(SemanticDocument semanticDocument, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var syntaxFacts = semanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            var methodDeclaration = expression.FirstAncestorOrSelf<SyntaxNode>(node => IsMethodDeclaration(node), ascendOutOfTrivia: true);
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
        protected static async Task<ImmutableDictionary<Document, List<TInvocationExpressionSyntax>>> FindCallSitesAsync(SemanticDocument document,
                                                                                                                         IMethodSymbol methodSymbol,
                                                                                                                         CancellationToken cancellationToken)
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
                invocations.Add((TInvocationExpressionSyntax)refLocation.Location.FindNode(cancellationToken).Parent!);
            }

            return methodCallSites.ToImmutableDictionary();
        }

        /// <summary>
        /// Generates a method declaration containing a return expression of the highlighted expression
        /// </summary>
        private async Task IntroduceNewMethodAsync(SemanticDocument semanticDocument,
                                                   TExpressionSyntax expression,
                                                   IMethodSymbol methodSymbol,
                                                   string newMethodIdentifier,
                                                   SyntaxEditor editor,
                                                   CancellationToken cancellationToken)
        {
            var document = semanticDocument.Document;
            var syntaxFacts = semanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            var generator = SyntaxGenerator.GetGenerator(document);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var methodExpression = expression.FirstAncestorOrSelf<SyntaxNode>(node => IsMethodDeclaration(node), true)!;
            var returnExpression = new List<SyntaxNode>
            {
                generator.ReturnStatement(expression.WithoutTrailingTrivia())
            };

            var typeSymbol = semanticDocument.SemanticModel.GetTypeInfo(expression, cancellationToken).Type;
            var newMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol, name: newMethodIdentifier, returnType: typeSymbol);

            var methodDeclaration = generator.MethodDeclaration(newMethod, returnExpression);
            editor.InsertBefore(methodExpression, methodDeclaration);
        }

        /// <summary>
        /// Generates a method declaration containing a call to the method that introduced the parameter
        /// </summary>
        /// 
        private async Task IntroduceNewMethodOverloadAsync(SemanticDocument semanticDocument,
                                                           TExpressionSyntax expression,
                                                           IMethodSymbol methodSymbol,
                                                           SyntaxEditor editor,
                                                           CancellationToken cancellationToken)
        {
            var document = semanticDocument.Document;
            var syntaxFacts = semanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();
            var generator = SyntaxGenerator.GetGenerator(document);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var methodExpression = expression.FirstAncestorOrSelf<SyntaxNode>(node => IsMethodDeclaration(node), true)!;
            var arguments = generator.CreateArguments(methodSymbol.Parameters.As<IParameterSymbol>());
            arguments = AddExpressionArgumentToArgumentList(arguments, expression.WithoutTrailingTrivia());
            var memberName = methodSymbol.IsGenericMethod
                ? generator.GenericName(methodSymbol.Name, methodSymbol.TypeArguments)
                : generator.IdentifierName(methodSymbol.Name);

            var invocation = generator.InvocationExpression(memberName, arguments);
            List<SyntaxNode> invocationReturn;

            if (!methodSymbol.ReturnsVoid)
            {
                invocationReturn = new List<SyntaxNode>
                {
                    generator.ReturnStatement(invocation)
                };
            }
            else
            {
                invocationReturn = new List<SyntaxNode>
                {
                    invocation
                };
            }

            var newMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol);

            var methodDeclaration = generator.MethodDeclaration(newMethod, invocationReturn);

            editor.InsertBefore(methodExpression, methodDeclaration);
        }

        public static void UpdateExpressionInOriginalFunction(SemanticDocument semanticDocument,
                                                              TExpressionSyntax expression, SyntaxNode scope,
                                                              string parameterName, SyntaxEditor editor,
                                                              bool allOccurrences, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(semanticDocument.Document);
            var matches = FindMatches(semanticDocument, expression, semanticDocument, scope, allOccurrences, cancellationToken);
            var parameterNameSyntax = (TIdentifierNameSyntax)generator.IdentifierName(parameterName);
            // Parenthesize the variable, and go and replace anything we find with it.
            // NOTE: we do not want elastic trivia as we want to just replace the existing code 
            // as is, while preserving the trivia there.  We do not want to update it.

            var replacement = generator.AddParentheses(parameterNameSyntax, includeElasticTrivia: false)
                                         .WithAdditionalAnnotations(Formatter.Annotation);

            var syntaxFacts = semanticDocument.Document.GetRequiredLanguageService<ISyntaxFactsService>();

            foreach (var match in matches)
            {
                if (match.Equals(expression))
                {
                    if (IsExpressionRemovable(semanticDocument, expression))
                    {
                        var localDeclaration = expression.FirstAncestorOrSelf<SyntaxNode>(node => syntaxFacts.IsLocalDeclarationStatement(node));
                        if (localDeclaration is not null)
                        {
                            editor.RemoveNode(localDeclaration);
                        }
                    }
                    else
                    {
                        var expressionToken = syntaxFacts.GetIdentifierOfIdentifierName(parameterNameSyntax);
                        var updatedExpressionToken = expressionToken.WithAdditionalAnnotations(RenameAnnotation.Create());
                        parameterNameSyntax = parameterNameSyntax.ReplaceToken(expressionToken, updatedExpressionToken);

                        replacement = generator.AddParentheses(parameterNameSyntax, includeElasticTrivia: false)
                                                     .WithAdditionalAnnotations(Formatter.Annotation);
                        editor.ReplaceNode(match, replacement);
                    }
                }
                else
                {
                    editor.ReplaceNode(match, replacement);
                }
            }
        }

        private int GetInsertionIndex(SemanticDocument document, Compilation compilation, ISyntaxFactsService syntaxFacts, SyntaxNode methodDeclaration, CancellationToken cancellationToken)
        {
            var parameterList = syntaxFacts.GetParameterList(methodDeclaration);
            var parameterSymbolList = GetParameterList(document, parameterList!, cancellationToken);
            var insertionIndex = 0;

            foreach (var parameterSymbol in parameterSymbolList)
            {
                if (!parameterSymbol.HasExplicitDefaultValue && !parameterSymbol.Type.Equals(compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName)))
                {
                    insertionIndex++;
                }
            }

            return insertionIndex;
        }

        /// <summary>
        /// Goes through all of the invocations and replaces the expression with identifiers from the invocation 
        /// arguments and rewrites the call site with the updated expression as a new argument
        /// </summary>
        private async Task<Solution> RewriteCallSitesWithNewMethodAsync(SemanticDocument semanticDocument,
                                                                               TExpressionSyntax expression,
                                                                               IMethodSymbol methodSymbol,
                                                                               bool allOccurrences,
                                                                               string parameterName,
                                                                               ImmutableDictionary<Document, List<TInvocationExpressionSyntax>> callSites,
                                                                               bool trampoline,
                                                                               CancellationToken cancellationToken)
        {
            var firstCallSite = callSites.Keys.First();
            var currentSolution = firstCallSite.Project.Solution;
            var modifiedSolution = currentSolution;
            var newMethodIdentifier = methodSymbol.Name + "_" + parameterName;

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
                    var oldMethodDeclaration = expression.FirstAncestorOrSelf<SyntaxNode>(node => IsMethodDeclaration(node), ascendOutOfTrivia: true)!;
                    var insertionIndex = GetInsertionIndex(semanticDocument, compilation, syntaxFacts, oldMethodDeclaration, cancellationToken);

                    if (trampoline)
                    {
                        foreach (var invocationExpression in invocationExpressionList)
                        {
                            editor.ReplaceNode(invocationExpression, (currentInvocation, _) =>
                            {
                                var invocationArguments = syntaxFacts.GetArgumentsOfInvocationExpression(currentInvocation);
                                var methodName = generator.IdentifierName(newMethodIdentifier);
                                var expressionFromInvocation = syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression);
                                var newMethodInvocation = generator.InvocationExpression(methodName, invocationArguments);
                                var allArguments = invocationArguments.Insert(insertionIndex, newMethodInvocation);
                                return editor.Generator.InvocationExpression(expressionFromInvocation, allArguments);
                            });
                        }
                    }

                    if (document.Id == semanticDocument.Document.Id)
                    {
                        if (trampoline)
                        {
                            await IntroduceNewMethodAsync(semanticDocument, expression, methodSymbol, newMethodIdentifier, editor, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await IntroduceNewMethodOverloadAsync(semanticDocument, expression, methodSymbol, editor, cancellationToken).ConfigureAwait(false);
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
        private async Task<Solution> RewriteCallSitesAsync(SemanticDocument semanticDocument,
                                                           TExpressionSyntax expression,
                                                           ImmutableDictionary<Document, List<TInvocationExpressionSyntax>> callSites,
                                                           bool allOccurrences,
                                                           string parameterName,
                                                           CancellationToken cancellationToken)
        {
            var expressionCopy = expression;
            var mappingDictionary = MapExpressionToParameters(semanticDocument, expression, cancellationToken);
            expression = expression.TrackNodes(mappingDictionary.Keys);
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
                    var oldMethodDeclaration = expressionCopy.FirstAncestorOrSelf<SyntaxNode>(node => IsMethodDeclaration(node), ascendOutOfTrivia: true)!;
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
                                    for (var i = 0; i < invocationArguments.ToArray().Length; i++)
                                    {
                                        var argumentParameter = semanticFacts.FindParameterForArgument(invocationSemanticModel, invocationArguments.ToArray()[i], cancellationToken);
                                        if (argumentParameter.Equals(mappedParameter, SymbolEqualityComparer.Default))
                                        {
                                            var updatedInvocationArgument = updatedInvocationArguments.ToArray()[i];
                                            var argumentExpression = syntaxFacts.GetExpressionOfArgument(updatedInvocationArgument);
                                            var parenthesizedArgumentExpression = generator.AddParentheses(argumentExpression, includeElasticTrivia: false);
                                            var oldNode = newArgumentExpression.GetCurrentNode(variable);
                                            RoslynDebug.AssertNotNull(oldNode);
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
                                        var oldNode = newArgumentExpression.GetCurrentNode(variable);
                                        RoslynDebug.AssertNotNull(oldNode);
                                        newArgumentExpression = newArgumentExpression.ReplaceNode(oldNode, parenthesizedGeneratedExpression);
                                    }
                                }
                            }

                            var expressionFromInvocation = syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression);
                            var allArguments = AddArgumentToArgumentList(updatedInvocationArguments,
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
        public static Dictionary<TIdentifierNameSyntax, IParameterSymbol> MapExpressionToParameters(SemanticDocument document, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var nameToParameterDict = new Dictionary<TIdentifierNameSyntax, IParameterSymbol>();
            var variablesInExpression = expression.DescendantNodes().OfType<TIdentifierNameSyntax>();
            var semanticModel = document.SemanticModel;

            foreach (var variable in variablesInExpression)
            {
                var symbol = semanticModel.GetSymbolInfo(variable, cancellationToken).Symbol;
                if (symbol != null && symbol is IParameterSymbol parameterSymbol)
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
        private (bool isParameterized, bool hasOptionalParameter) ExpressionWithinParameterizedMethod(SemanticDocument semanticDocument,
                                                                                                      TExpressionSyntax expression,
                                                                                                      CancellationToken cancellationToken)
        {
            var methodSymbol = GetMethodSymbolFromExpression(semanticDocument, expression, cancellationToken);

            var variablesInExpression = expression.DescendantNodes().OfType<TIdentifierNameSyntax>();
            var semanticModel = semanticDocument.SemanticModel;
            var hasOptionalParameter = false;

            foreach (var variable in variablesInExpression)
            {
                var parameterSymbol = semanticModel.GetSymbolInfo(variable, cancellationToken).Symbol;
                if (parameterSymbol == null || parameterSymbol is not IParameterSymbol)
                {
                    return (false, hasOptionalParameter);
                }
                if (((IParameterSymbol)parameterSymbol).IsParams)
                {
                    return (false, hasOptionalParameter);
                }
                if (((IParameterSymbol)parameterSymbol).HasExplicitDefaultValue)
                {
                    hasOptionalParameter = true;
                }
            }
            return (methodSymbol != null && methodSymbol.GetParameters().Any(), hasOptionalParameter);
        }

        private (string title, ImmutableArray<CodeAction> actions) AddActions(SemanticDocument semanticDocument, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var actionsBuilder = new ArrayBuilder<CodeAction>();
            var (isParameterized, hasOptionalParameter) = ExpressionWithinParameterizedMethod(semanticDocument, expression, cancellationToken);
            if (isParameterized)
            {
                actionsBuilder.Add(new IntroduceParameterCodeAction(semanticDocument, (TService)this, expression, false, false, false));
                actionsBuilder.Add(new IntroduceParameterCodeAction(semanticDocument, (TService)this, expression, true, false, false));

                if (!hasOptionalParameter)
                {
                    actionsBuilder.Add(new IntroduceParameterCodeAction(semanticDocument, (TService)this, expression, false, true, false));
                    actionsBuilder.Add(new IntroduceParameterCodeAction(semanticDocument, (TService)this, expression, true, true, false));

                    actionsBuilder.Add(new IntroduceParameterCodeAction(semanticDocument, (TService)this, expression, false, false, true));
                    actionsBuilder.Add(new IntroduceParameterCodeAction(semanticDocument, (TService)this, expression, true, false, true));
                }
            }

            return (FeaturesResources.Introduce_parameter, actionsBuilder.ToImmutable());
        }

        protected static ISet<TExpressionSyntax> FindMatches(SemanticDocument originalDocument,
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

        private static bool NodeMatchesExpression(SemanticModel originalSemanticModel,
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
    }
}
