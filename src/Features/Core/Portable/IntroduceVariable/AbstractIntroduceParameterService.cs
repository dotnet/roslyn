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
        protected abstract SyntaxNode GenerateExpressionFromOptionalParameter(IParameterSymbol parameterSymbol);
        protected abstract SyntaxNode? GetLocalDeclarationFromDeclarator(SyntaxNode variableDecl);

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
            var expressionInParameter = expression.FirstAncestorOrSelf<SyntaxNode>(node => syntaxFacts.IsParameter(node));
            if (expressionInParameter is not null)
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

            var methodKind = methodSymbol.MethodKind;
            if (methodKind is not (MethodKind.Ordinary or MethodKind.Constructor or MethodKind.LambdaMethod or MethodKind.LocalFunction))
            {
                return;
            }

            var actions = await GetActionsAsync(document, expression, methodSymbol, containingMethod, cancellationToken).ConfigureAwait(false);

            if (actions is null)
            {
                return;
            }

            context.RegisterRefactoring(new CodeActionWithNestedActions(
                CreateDisplayText(expression, syntaxFacts, FeaturesResources.Introduce_parameter_for_0), actions.Value.actions, isInlinable: false), textSpan);
            context.RegisterRefactoring(new CodeActionWithNestedActions(
                CreateDisplayText(expression, syntaxFacts, FeaturesResources.Introduce_parameter_for_all_occurrences_of_0), actions.Value.actionsAllOccurrences, isInlinable: false), textSpan);
        }

        /// <summary>
        /// Introduces a new parameter and refactors all the call sites.
        /// </summary>
        public async Task<Solution> IntroduceParameterAsync(Document document, TExpressionSyntax expression,
            IMethodSymbol methodSymbol, SyntaxNode containingMethod, bool allOccurrences, bool trampoline, bool overload,
            CancellationToken cancellationToken)
        {
            var parameterName = await GetNewParameterNameAsync(document, expression, cancellationToken).ConfigureAwait(false);

            var methodCallSites = await FindCallSitesAsync(document, methodSymbol, cancellationToken).ConfigureAwait(false);

            return await RewriteSolutionAsync(document,
                expression, methodSymbol, containingMethod, allOccurrences, parameterName, methodCallSites,
                trampoline, overload, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the parameter name, if the expression's grandparent is a variable declarator then it just gets the
        /// local declarations name. Otherwise, it generates a name based on the context of the expression.
        /// </summary>
        private async Task<string> GetNewParameterNameAsync(Document document, TExpressionSyntax expression, CancellationToken cancellationToken)
        {
            if (ShouldRemoveVariableDeclaratorContainingExpression(document, expression, out var varDeclName, out _))
            {
                return varDeclName;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
            return semanticFacts.GenerateNameForExpression(semanticModel, expression, capitalize: false, cancellationToken);
        }

        /// <summary>
        /// Determines if the expression's grandparent is a variable declarator and if so,
        /// returns the name
        /// </summary>
        protected bool ShouldRemoveVariableDeclaratorContainingExpression(
            Document document, TExpressionSyntax expression, [NotNullWhen(true)] out string? varDeclName, [NotNullWhen(true)] out SyntaxNode? localDeclaration)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var expressionDecl = expression?.Parent?.Parent;

            localDeclaration = null;

            if (syntaxFacts.IsVariableDeclarator(expressionDecl))
            {
                var localDec = GetLocalDeclarationFromDeclarator(expressionDecl);
                if (syntaxFacts.IsLocalDeclarationStatement(localDec))
                {
                    localDeclaration = localDec;
                }
            }

            if (localDeclaration is null)
            {
                varDeclName = null;
                return false;
            }

            // TODO: handle in the future
            if (syntaxFacts.GetVariablesOfLocalDeclarationStatement(localDeclaration).Count > 1)
            {
                varDeclName = null;
                localDeclaration = null;
                return false;
            }

            if (syntaxFacts.IsVariableDeclarator(expressionDecl))
            {
                varDeclName = syntaxFacts.GetIdentifierOfVariableDeclarator(expressionDecl).ValueText;
                return true;
            }

            varDeclName = null;
            localDeclaration = null;
            return false;
        }

        /// <summary>
        /// Locates all the call sites of the method that introduced the parameter
        /// </summary>
        protected static async Task<ImmutableDictionary<Document, List<TInvocationExpressionSyntax>>> FindCallSitesAsync(
            Document document, IMethodSymbol methodSymbol, CancellationToken cancellationToken)
        {
            var methodCallSitesBuilder = ImmutableDictionary.CreateBuilder<Document, List<TInvocationExpressionSyntax>>();
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
            methodCallSitesBuilder.Add(document, new List<TInvocationExpressionSyntax>());

            foreach (var refLocation in referencedLocations)
            {
                // Does not support cross-language references currently
                if (refLocation.Document.Project.Language == document.Project.Language)
                {
                    var reference = refLocation.Location.FindNode(cancellationToken).GetRequiredParent();

                    // Only adding items that are of type InvocationExpressionSyntax
                    if (reference is not TInvocationExpressionSyntax invocation)
                    {
                        continue;
                    }

                    if (!methodCallSitesBuilder.TryGetValue(refLocation.Document, out var list))
                    {
                        list = new List<TInvocationExpressionSyntax>();
                        methodCallSitesBuilder.Add(refLocation.Document, list);
                    }

                    list.Add(invocation);
                }
            }

            return methodCallSitesBuilder.ToImmutable();
        }

        /// <summary>
        /// If the parameter is optional and the invocation does not specify the parameter, then
        /// a named argument needs to be introduced.
        /// </summary>
        private static SeparatedSyntaxList<SyntaxNode> AddArgumentToArgumentList(
            SeparatedSyntaxList<SyntaxNode> invocationArguments, SyntaxGenerator generator,
            SyntaxNode newArgumentExpression, int insertionIndex)
        {
            var argument = generator.Argument(newArgumentExpression);
            return invocationArguments.Insert(insertionIndex, argument.WithAdditionalAnnotations(Simplifier.Annotation));
        }

        /// <summary>
        /// Gets the matches of the expression and replaces them with the identifier.
        /// Special case for the original matching expression, if its parent is a LocalDeclarationStatement then it can
        /// be removed because assigning the local dec variable to a parameter is repetitive. Does not need a rename
        /// annotation since the user has already named the local declaration.
        /// Otherwise, it needs to have a rename annotation added to it because the new parameter gets a randomly
        /// generated name that the user can immediately change.
        /// </summary>
        public async Task UpdateExpressionInOriginalFunctionAsync(Document document,
            TExpressionSyntax expression, SyntaxNode scope, string parameterName, SyntaxEditor editor,
            bool allOccurrences, CancellationToken cancellationToken)
        {
            var generator = editor.Generator;
            var matches = await FindMatchesAsync(document, expression, scope, allOccurrences, cancellationToken).ConfigureAwait(false);
            var replacement = (TIdentifierNameSyntax)generator.IdentifierName(parameterName);

            foreach (var match in matches)
            {
                // Special case the removal of the originating expression to either remove the local declaration
                // or to add a rename annotation.
                if (!match.Equals(expression))
                {
                    editor.ReplaceNode(match, replacement);
                }
                else
                {
                    if (ShouldRemoveVariableDeclaratorContainingExpression(document, expression, out var varDeclName, out var localDeclaration))
                    {
                        editor.RemoveNode(localDeclaration);
                    }
                    else
                    {
                        // Creating a SyntaxToken from the new parameter name and adding an annotation to it
                        // and passing that syntaxtoken in to generator.IdentifierName to create a new
                        // IdentifierNameSyntax node.
                        // Need to create the RenameAnnotation on the token itself otherwise it does not show up
                        // properly.
                        replacement = (TIdentifierNameSyntax)generator.IdentifierName(generator.Identifier(parameterName)
                            .WithAdditionalAnnotations(RenameAnnotation.Create()));
                        editor.ReplaceNode(match, replacement);
                    }
                }
            }
        }

        /// <summary>
        /// Goes through the parameters of the original method to get the location that the parameter
        /// and argument should be introduced.
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
        /// Goes through all of the invocations and replaces the expression with identifiers from the invocation 
        /// arguments and rewrites the call site with the updated expression as a new argument.
        /// </summary>
        private async Task<Solution> RewriteSolutionAsync(
            Document originalDocument, TExpressionSyntax expression, IMethodSymbol methodSymbol,
            SyntaxNode containingMethod, bool allOccurrences, string parameterName,
            ImmutableDictionary<Document, List<TInvocationExpressionSyntax>> callSites, bool trampoline,
            bool overload, CancellationToken cancellationToken)
        {
            var modifiedSolution = originalDocument.Project.Solution;
            var mappingDictionary = await MapExpressionToParametersAsync(originalDocument, expression, cancellationToken).ConfigureAwait(false);

            foreach (var (project, projectCallSites) in callSites.GroupBy(kvp => kvp.Key.Project))
            {
                var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                foreach (var (document, invocationExpressionList) in projectCallSites)
                {
                    if (trampoline || overload)
                    {
                        var newRoot = await ModifyDocumentInvocationsTrampolineOverloadAndIntroduceParameterAsync(compilation,
                            document, originalDocument, invocationExpressionList, mappingDictionary, methodSymbol, containingMethod,
                            allOccurrences, parameterName, expression, trampoline, overload, cancellationToken).ConfigureAwait(false);
                        modifiedSolution = modifiedSolution.WithDocumentSyntaxRoot(originalDocument.Id, newRoot);
                    }
                    else
                    {
                        var newRoot = await ModifyDocumentInvocationsAndIntroduceParameterAsync(compilation,
                            originalDocument, document, mappingDictionary, methodSymbol, containingMethod,
                            expression, allOccurrences, parameterName, invocationExpressionList,
                            cancellationToken).ConfigureAwait(false);
                        modifiedSolution = modifiedSolution.WithDocumentSyntaxRoot(originalDocument.Id, newRoot);
                    }
                }
            }

            return modifiedSolution;
        }

        /// <summary>
        /// For the trampoline case, it goes through the invocations and adds an argument which is a 
        /// call to the extracted method.
        /// Introduces a new method overload or new trampoline method.
        /// Updates the original method site with a newly introduced parameter.
        /// </summary>
        private async Task<SyntaxNode> ModifyDocumentInvocationsTrampolineOverloadAndIntroduceParameterAsync(
            Compilation compilation, Document currentDocument, Document originalDocument,
            List<TInvocationExpressionSyntax> invocations, Dictionary<TIdentifierNameSyntax, IParameterSymbol> mappingDictionary,
            IMethodSymbol methodSymbol, SyntaxNode containingMethod,
            bool allOccurrences, string parameterName, TExpressionSyntax expression, bool trampoline, bool overload,
            CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(currentDocument);
            var semanticFacts = currentDocument.GetRequiredLanguageService<ISemanticFactsService>();
            var invocationSemanticModel = await currentDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var root = await currentDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, generator);
            var syntaxFacts = currentDocument.GetRequiredLanguageService<ISyntaxFactsService>();
            var insertionIndex = GetInsertionIndex(compilation, methodSymbol, syntaxFacts, containingMethod);

            // Creating a new method name by concatenating the parameter name that has been camelcased.
            var newMethodIdentifier = "Get" + parameterName.First().ToString().ToUpper() + parameterName[1..];

            var validParameters = methodSymbol.Parameters.AsEnumerable().SelectMany(parameter => mappingDictionary.Values.Where(param => parameter.Equals(param))).ToImmutableArray();
            if (trampoline)
            {
                foreach (var invocationExpression in invocations)
                {
                    var argumentListSyntax = syntaxFacts.GetArgumentListOfInvocationExpression(invocationExpression);
                    editor.ReplaceNode(argumentListSyntax, (currentArgumentListSyntax, _) =>
                    {
                        return GenerateNewArgumentListSyntaxForTrampoline(syntaxFacts, semanticFacts, invocationSemanticModel, generator, currentArgumentListSyntax,
                            argumentListSyntax, validParameters, newMethodIdentifier, insertionIndex, cancellationToken);
                    });
                }
            }

            // If you are at the original document, then also introduce the new method and introduce the parameter.
            if (currentDocument.Id == originalDocument.Id)
            {
                if (trampoline)
                {
                    var newMethodNode = await ExtractMethodAsync(originalDocument, expression, methodSymbol, validParameters, newMethodIdentifier,
                        generator, cancellationToken).ConfigureAwait(false);
                    editor.InsertBefore(containingMethod, newMethodNode);
                }
                else if (overload)
                {
                    var newMethodNode = await GenerateNewMethodOverloadAsync(originalDocument, expression, methodSymbol, insertionIndex,
                        generator, cancellationToken).ConfigureAwait(false);
                    editor.InsertBefore(containingMethod, newMethodNode);
                }

                await UpdateExpressionInOriginalFunctionAsync(originalDocument, expression, containingMethod,
                    parameterName, editor, allOccurrences, cancellationToken).ConfigureAwait(false);
                var parameterType = invocationSemanticModel.GetTypeInfo(expression, cancellationToken).ConvertedType ?? invocationSemanticModel.Compilation.ObjectType;
                var parameter = generator.ParameterDeclaration(parameterName, generator.TypeExpression(parameterType));
                editor.InsertParameter(containingMethod, insertionIndex, parameter);
            }

            return editor.GetChangedRoot();

            // Adds an argument which is an invocation of the newly created method to the callsites
            // of the method invocations where a parameter was added.
            // Example:
            // public void M(int x, int y)
            // {
            //     int f = x * y; // highlight this expression
            // }
            // 
            // public void InvokeMethod()
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
            // public void InvokeMethod()
            // {
            //     M(5, 6, M_f(5, 6)); // This is the generated invocation which is a new argument at the call site
            // }
            static SyntaxNode GenerateNewArgumentListSyntaxForTrampoline(ISyntaxFactsService syntaxFacts, ISemanticFactsService semanticFacts,
                SemanticModel invocationSemanticModel, SyntaxGenerator generator, SyntaxNode currentArgumentListSyntax, SyntaxNode argumentListSyntax,
                ImmutableArray<IParameterSymbol> validParameters, string newMethodIdentifier, int insertionIndex, CancellationToken cancellationToken)
            {
                var invocationArguments = syntaxFacts.GetArgumentsOfArgumentList(argumentListSyntax);
                var parameterToArgumentMap = MapParameterToArgumentsAtInvocation(semanticFacts, invocationArguments, invocationSemanticModel, cancellationToken);
                var currentInvocationArguments = syntaxFacts.GetArgumentsOfArgumentList(currentArgumentListSyntax);
                var requiredArguments = new List<SyntaxNode>();

                foreach (var parameterSymbol in validParameters)
                {
                    if (parameterToArgumentMap.TryGetValue(parameterSymbol, out var index))
                    {
                        requiredArguments.Add(currentInvocationArguments[index]);
                    }
                }

                var methodName = generator.IdentifierName(newMethodIdentifier);
                var newMethodInvocation = generator.InvocationExpression(methodName, requiredArguments);
                var allArguments = AddArgumentToArgumentList(currentInvocationArguments, generator, newMethodInvocation, insertionIndex);
                return syntaxFacts.UpdateArgumentListSyntax(currentArgumentListSyntax, allArguments);
            }
        }

        /// <summary>
        /// Generates a method declaration containing a return expression of the highlighted expression.
        /// Example:
        /// public void M(int x, int y)
        /// {
        ///     int f = [|x * y|];
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
        private static async Task<SyntaxNode> ExtractMethodAsync(Document document, TExpressionSyntax expression,
            IMethodSymbol methodSymbol, ImmutableArray<IParameterSymbol> validParameters, string newMethodIdentifier, SyntaxGenerator generator, CancellationToken cancellationToken)
        {

            // Remove trailing trivia because it adds spaces to the beginning of the following statement.
            var newStatements = ImmutableArray.CreateBuilder<SyntaxNode>();
            newStatements.Add(generator.ReturnStatement(expression.WithoutTrailingTrivia()));
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeSymbol = semanticModel.GetTypeInfo(expression, cancellationToken).ConvertedType ?? semanticModel.Compilation.ObjectType;
            var codeGenerationService = document.GetRequiredLanguageService<ICodeGenerationService>();
            var newMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol, name: newMethodIdentifier, parameters: validParameters, statements: newStatements.ToImmutable(), returnType: typeSymbol);
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var newMethodDeclaration = codeGenerationService.CreateMethodDeclaration(newMethod, options: new CodeGenerationOptions(options: options, parseOptions: expression.SyntaxTree.Options));
            return newMethodDeclaration;
        }

        /// <summary>
        /// Generates a method declaration containing a call to the method that introduced the parameter.
        /// Example:
        /// 
        /// ***This is an intermediary step in which the original function has not be updated yet
        /// public void M(int x, int y)
        /// {
        ///     int f = [|x * y|];
        /// }
        /// 
        /// ---------------------------------------------------->
        /// 
        /// public void M(int x, int y) // Generated overload
        /// {
        ///     M(x, y, x * y);
        /// }
        /// 
        /// public void M(int x, int y) // Original function
        /// {
        ///     int f = x * y;
        /// }
        /// </summary>
        private static async Task<SyntaxNode> GenerateNewMethodOverloadAsync(Document document, TExpressionSyntax expression,
            IMethodSymbol methodSymbol, int insertionIndex, SyntaxGenerator generator, CancellationToken cancellationToken)
        {
            var arguments = generator.CreateArguments(methodSymbol.Parameters);

            // Remove trivia so the expression is in a single line and does not affect the spacing of the following line
            arguments = arguments.Insert(insertionIndex, generator.Argument(expression.WithoutTrivia()));
            var memberName = methodSymbol.IsGenericMethod
                ? generator.GenericName(methodSymbol.Name, methodSymbol.TypeArguments)
                : generator.IdentifierName(methodSymbol.Name);
            var invocation = generator.InvocationExpression(memberName, arguments);

            SyntaxNode newStatement;
            if (!methodSymbol.ReturnsVoid)
            {
                newStatement = generator.ReturnStatement(invocation);
            }
            else
            {
                newStatement = generator.ExpressionStatement(invocation);
            }

            var codeGenerationService = document.GetRequiredLanguageService<ICodeGenerationService>();
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var newMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(methodSymbol, statements: SpecializedCollections.SingletonEnumerable(newStatement).ToImmutableArray(), containingType: methodSymbol.ContainingType);
            var newMethodDeclaration = codeGenerationService.CreateMethodDeclaration(newMethod, options: new CodeGenerationOptions(options: options, parseOptions: expression.SyntaxTree.Options));
            return newMethodDeclaration;
        }

        /// <summary>
        /// This method goes through all the invocation sites and adds a new argument with the expression to be added.
        /// It also introduces a parameter at the original method site.
        /// 
        /// Example:
        /// public void M(int x, int y)
        /// {
        ///     int f = [|x * y|];
        /// }
        /// 
        /// public void InvokeMethod()
        /// {
        ///     M(5, 6);
        /// }
        /// 
        /// ---------------------------------------------------->
        /// 
        /// public void M(int x, int y, int f) // parameter gets introduced
        /// {
        /// }
        /// 
        /// public void InvokeMethod()
        /// {
        ///     M(5, 6, 5 * 6); // argument gets added to callsite
        /// }
        /// </summary>
        private async Task<SyntaxNode> ModifyDocumentInvocationsAndIntroduceParameterAsync(Compilation compilation,
            Document originalDocument, Document document, Dictionary<TIdentifierNameSyntax, IParameterSymbol> mappingDictionary,
            IMethodSymbol methodSymbol, SyntaxNode containingMethod, TExpressionSyntax expression,
            bool allOccurrences, string parameterName,
            List<TInvocationExpressionSyntax> invocations, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, generator);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var insertionIndex = GetInsertionIndex(compilation, methodSymbol, syntaxFacts, containingMethod);
            var invocationSemanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var invocationExpression in invocations)
            {
                var expressionEditor = new SyntaxEditor(expression, generator);
                var invocationArguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);
                var parameterToArgumentMap = MapParameterToArgumentsAtInvocation(semanticFacts, invocationArguments, invocationSemanticModel, cancellationToken);
                var argumentListSyntax = syntaxFacts.GetArgumentListOfInvocationExpression(invocationExpression);

                editor.ReplaceNode(argumentListSyntax, (currentArgumentListSyntax, _) =>
                {
                    var updatedInvocationArguments = syntaxFacts.GetArgumentsOfArgumentList(currentArgumentListSyntax);
                    var updatedExpression = CreateNewArgumentExpression(expressionEditor,
                        syntaxFacts, mappingDictionary, parameterToArgumentMap, updatedInvocationArguments);
                    var allArguments = AddArgumentToArgumentList(updatedInvocationArguments, generator,
                        updatedExpression.WithAdditionalAnnotations(Formatter.Annotation), insertionIndex);
                    return syntaxFacts.UpdateArgumentListSyntax(currentArgumentListSyntax, allArguments);
                });
            }

            // If you are at the original document, then also introduce the new method and introduce the parameter.
            if (document.Id == originalDocument.Id)
            {
                await UpdateExpressionInOriginalFunctionAsync(originalDocument, expression, containingMethod,
                    parameterName, editor, allOccurrences, cancellationToken).ConfigureAwait(false);
                var parameterType = invocationSemanticModel.GetTypeInfo(expression, cancellationToken).ConvertedType ??
                    invocationSemanticModel.Compilation.ObjectType;
                var parameter = generator.ParameterDeclaration(name: parameterName,
                    type: generator.TypeExpression(parameterType));
                editor.InsertParameter(containingMethod, insertionIndex, parameter);
            }

            return editor.GetChangedRoot();
        }

        /// <summary>
        /// This method iterates through the variables in the expression and maps the variables back to the parameter
        /// it is associated with. It then maps the parameter back to the argument at the invocation site and gets the
        /// index to retrieve the updated arguments at the invocation.
        /// </summary>
        private TExpressionSyntax CreateNewArgumentExpression(SyntaxEditor editor, ISyntaxFactsService syntaxFacts,
            Dictionary<TIdentifierNameSyntax, IParameterSymbol> mappingDictionary,
            ImmutableDictionary<IParameterSymbol, int> parameterToArgumentMap,
            SeparatedSyntaxList<SyntaxNode> updatedInvocationArguments)
        {
            foreach (var (variable, mappedParameter) in mappingDictionary)
            {
                var parameterMapped = false;
                if (parameterToArgumentMap.TryGetValue(mappedParameter, out var index))
                {
                    var updatedInvocationArgument = updatedInvocationArguments.ToArray()[index];
                    var argumentExpression = syntaxFacts.GetExpressionOfArgument(updatedInvocationArgument);
                    var parenthesizedArgumentExpression = editor.Generator.AddParentheses(argumentExpression, includeElasticTrivia: false);
                    editor.ReplaceNode(variable, parenthesizedArgumentExpression);
                    parameterMapped = true;
                }

                if (mappedParameter.HasExplicitDefaultValue && !parameterMapped)
                {
                    var generatedExpression = GenerateExpressionFromOptionalParameter(mappedParameter);
                    var parenthesizedGeneratedExpression = editor.Generator.AddParentheses(generatedExpression, includeElasticTrivia: false);
                    editor.ReplaceNode(variable, parenthesizedGeneratedExpression);
                }
            }

            return (TExpressionSyntax)editor.GetChangedRoot();
        }

        private static ImmutableDictionary<IParameterSymbol, int> MapParameterToArgumentsAtInvocation(
            ISemanticFactsService semanticFacts, SeparatedSyntaxList<SyntaxNode> arguments,
            SemanticModel invocationSemanticModel, CancellationToken cancellationToken)
        {
            var mapping = ImmutableDictionary.CreateBuilder<IParameterSymbol, int>();
            for (var i = 0; i < arguments.Count; i++)
            {
                var argumentParameter = semanticFacts.FindParameterForArgument(invocationSemanticModel, arguments[i], cancellationToken);
                if (argumentParameter is not null)
                {
                    mapping.Add(argumentParameter, i);
                }
            }

            return mapping.ToImmutable();
        }

        /// <summary>
        /// Ties the identifiers within the expression back to their associated parameter.
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
        /// Determines if the expression is something that should have code actions displayed for it.
        /// Depends upon the identifiers in the expression mapping back to parameters.
        /// Does not handle params parameters.
        /// </summary>
        private static async Task<bool> ShouldExpressionDisplayCodeActionAsync(
            Document document, TExpressionSyntax expression,
            CancellationToken cancellationToken)
        {
            var variablesInExpression = expression.DescendantNodes().OfType<TIdentifierNameSyntax>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var variable in variablesInExpression)
            {
                var symbol = semanticModel.GetSymbolInfo(variable, cancellationToken).Symbol;
                if (symbol is IRangeVariableSymbol or ILocalSymbol)
                {
                    return false;
                }

                if (symbol is IParameterSymbol parameter)
                {
                    if (parameter.IsParams)
                    {
                        return false;
                    }
                }
            }

            return true;
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
            var isParameterized = await ShouldExpressionDisplayCodeActionAsync(document, expression, cancellationToken).ConfigureAwait(false);
            if (!isParameterized)
            {
                return null;
            }
            else
            {
                var actionsBuilder = ImmutableArray.CreateBuilder<CodeAction>();
                var actionsBuilderAllOccurrences = ImmutableArray.CreateBuilder<CodeAction>();
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

                actionsBuilder.Add(CreateNewCodeAction(FeaturesResources.and_update_call_sites_directly, allOccurrences: false, trampoline: false, overload: false));
                actionsBuilderAllOccurrences.Add(CreateNewCodeAction(FeaturesResources.and_update_call_sites_directly, allOccurrences: true, trampoline: false, overload: false));

                if (methodSymbol.MethodKind is not MethodKind.Constructor)
                {
                    actionsBuilder.Add(CreateNewCodeAction(
                        FeaturesResources.into_extracted_method_to_invoke_at_call_sites, allOccurrences: false, trampoline: true, overload: false));
                    actionsBuilderAllOccurrences.Add(CreateNewCodeAction(
                        FeaturesResources.into_extracted_method_to_invoke_at_call_sites, allOccurrences: true, trampoline: true, overload: false));

                    actionsBuilder.Add(CreateNewCodeAction(
                        CreateDisplayText(expression, syntaxFacts, FeaturesResources.into_new_overload_of_0), allOccurrences: false, trampoline: false, overload: true));
                    actionsBuilderAllOccurrences.Add(CreateNewCodeAction(
                        CreateDisplayText(expression, syntaxFacts, FeaturesResources.into_new_overload_of_0), allOccurrences: true, trampoline: false, overload: true));
                }

                return (actionsBuilder.ToImmutable(), actionsBuilderAllOccurrences.ToImmutable());
            }

            // Local function to create a code action with more ease
            MyCodeAction CreateNewCodeAction(string actionName, bool allOccurrences, bool trampoline, bool overload)
            {
                return new MyCodeAction(actionName, c => IntroduceParameterAsync(
                    document, expression, methodSymbol, containingMethod, allOccurrences, trampoline, overload, c));
            }
        }

        /// <summary>
        /// Finds the matches of the expression within the same block.
        /// </summary>
        protected static async Task<IEnumerable<TExpressionSyntax>> FindMatchesAsync(Document originalDocument,
            TExpressionSyntax expressionInOriginal, SyntaxNode withinNodeInCurrent,
            bool allOccurrences, CancellationToken cancellationToken)
        {
            var syntaxFacts = originalDocument.GetRequiredLanguageService<ISyntaxFactsService>();
            var originalSemanticModel = await originalDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

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

            if (!allOccurrences)
            {
                return false;
            }
            else
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

                var originalOperation = originalSemanticModel.GetOperation(expressionInOriginal, cancellationToken);
                if (originalOperation != null && IsInstanceMemberReference(originalOperation))
                {
                    var currentOperation = originalSemanticModel.GetOperation(nodeInCurrent, cancellationToken);
                    return currentOperation != null && IsInstanceMemberReference(currentOperation) && SemanticEquivalence.AreEquivalent(
                        originalSemanticModel, originalSemanticModel, expressionInOriginal, nodeInCurrent);
                }

                return SemanticEquivalence.AreEquivalent(
                    originalSemanticModel, originalSemanticModel, expressionInOriginal, nodeInCurrent);
            }

            static bool IsInstanceMemberReference(IOperation operation)
                => operation is IMemberReferenceOperation memberReferenceOperation &&
                    memberReferenceOperation.Instance?.Kind == OperationKind.InstanceReference;
        }

        private static string CreateDisplayText(TExpressionSyntax expression, ISyntaxFactsService syntaxFacts, string resourceString)
        {
            var singleLineExpression = syntaxFacts.ConvertToSingleLine(expression);
            var nodeString = singleLineExpression.ToString();

            return string.Format(resourceString, nodeString);
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
