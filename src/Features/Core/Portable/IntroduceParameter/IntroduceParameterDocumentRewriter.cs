// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.IntroduceParameter;

internal abstract partial class AbstractIntroduceParameterCodeRefactoringProvider<TExpressionSyntax, TInvocationExpressionSyntax, TObjectCreationExpressionSyntax, TIdentifierNameSyntax, TArgumentSyntax>
{
    private class IntroduceParameterDocumentRewriter(
        AbstractIntroduceParameterCodeRefactoringProvider<TExpressionSyntax, TInvocationExpressionSyntax, TObjectCreationExpressionSyntax, TIdentifierNameSyntax, TArgumentSyntax> service,
        Document originalDocument,
        TExpressionSyntax expression,
        IMethodSymbol methodSymbol,
        SyntaxNode containingMethod,
        IntroduceParameterCodeActionKind selectedCodeAction,
        bool allOccurrences)
    {
        private readonly AbstractIntroduceParameterCodeRefactoringProvider<TExpressionSyntax, TInvocationExpressionSyntax, TObjectCreationExpressionSyntax, TIdentifierNameSyntax, TArgumentSyntax> _service = service;
        private readonly Document _originalDocument = originalDocument;
        private readonly SyntaxGenerator _generator = SyntaxGenerator.GetGenerator(originalDocument);
        private readonly ISyntaxFactsService _syntaxFacts = originalDocument.GetRequiredLanguageService<ISyntaxFactsService>();
        private readonly ISemanticFactsService _semanticFacts = originalDocument.GetRequiredLanguageService<ISemanticFactsService>();
        private readonly TExpressionSyntax _expression = expression;
        private readonly IMethodSymbol _methodSymbol = methodSymbol;
        private readonly SyntaxNode _containerMethod = containingMethod;
        private readonly IntroduceParameterCodeActionKind _actionKind = selectedCodeAction;
        private readonly bool _allOccurrences = allOccurrences;

        public async Task<SyntaxNode> RewriteDocumentAsync(Compilation compilation, Document document, List<TExpressionSyntax> invocations, CancellationToken cancellationToken)
        {
            var insertionIndex = GetInsertionIndex(compilation);

            if (_actionKind is IntroduceParameterCodeActionKind.Overload or IntroduceParameterCodeActionKind.Trampoline)
            {
                return await ModifyDocumentInvocationsTrampolineOverloadAndIntroduceParameterAsync(
                        compilation, document, invocations, insertionIndex, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await ModifyDocumentInvocationsAndIntroduceParameterAsync(
                        compilation, document, insertionIndex, invocations, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Ties the identifiers within the expression back to their associated parameter.
        /// </summary>
        private async Task<Dictionary<TIdentifierNameSyntax, IParameterSymbol>> MapExpressionToParametersAsync(CancellationToken cancellationToken)
        {
            var nameToParameterDict = new Dictionary<TIdentifierNameSyntax, IParameterSymbol>();
            var variablesInExpression = _expression.DescendantNodes().OfType<TIdentifierNameSyntax>();
            var semanticModel = await _originalDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

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
        /// Gets the parameter name, if the expression's grandparent is a variable declarator then it just gets the
        /// local declarations name. Otherwise, it generates a name based on the context of the expression.
        /// </summary>
        private async Task<string> GetNewParameterNameAsync(CancellationToken cancellationToken)
        {
            if (ShouldRemoveVariableDeclaratorContainingExpression(out var varDeclName, out _))
            {
                return varDeclName;
            }

            var semanticModel = await _originalDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var semanticFacts = _originalDocument.GetRequiredLanguageService<ISemanticFactsService>();
            return semanticFacts.GenerateNameForExpression(semanticModel, _expression, capitalize: false, cancellationToken);
        }

        /// <summary>
        /// Determines if the expression's grandparent is a variable declarator and if so,
        /// returns the name
        /// </summary>
        private bool ShouldRemoveVariableDeclaratorContainingExpression([NotNullWhen(true)] out string? varDeclName, [NotNullWhen(true)] out SyntaxNode? localDeclaration)
        {
            var declarator = _expression?.Parent?.Parent;

            localDeclaration = null;

            if (!_syntaxFacts.IsVariableDeclarator(declarator))
            {
                varDeclName = null;
                return false;
            }

            localDeclaration = _service.GetLocalDeclarationFromDeclarator(declarator);
            if (localDeclaration is null)
            {
                varDeclName = null;
                return false;
            }

            // TODO: handle in the future
            if (_syntaxFacts.GetVariablesOfLocalDeclarationStatement(localDeclaration).Count > 1)
            {
                varDeclName = null;
                localDeclaration = null;
                return false;
            }

            varDeclName = _syntaxFacts.GetIdentifierOfVariableDeclarator(declarator).ValueText;
            return true;
        }

        /// <summary>
        /// Goes through the parameters of the original method to get the location that the parameter
        /// and argument should be introduced.
        /// </summary>
        private int GetInsertionIndex(Compilation compilation)
        {
            var parameterList = _syntaxFacts.GetParameterList(_containerMethod);
            Contract.ThrowIfNull(parameterList);
            var insertionIndex = 0;

            foreach (var parameterSymbol in _methodSymbol.Parameters)
            {
                // Want to skip optional parameters, params parameters, and CancellationToken since they should be at
                // the end of the list.
                if (ShouldParameterBeSkipped(compilation, parameterSymbol))
                {
                    insertionIndex++;
                }
            }

            return insertionIndex;
        }

        /// <summary>
        /// For the trampoline case, it goes through the invocations and adds an argument which is a 
        /// call to the extracted method.
        /// Introduces a new method overload or new trampoline method.
        /// Updates the original method site with a newly introduced parameter.
        /// 
        /// ****Trampoline Example:****
        /// public void M(int x, int y)
        /// {
        ///     int f = [|x * y|];
        ///     Console.WriteLine(f);
        /// }
        /// 
        /// public void InvokeMethod()
        /// {
        ///     M(5, 6);
        /// }
        /// 
        /// ---------------------------------------------------->
        /// 
        /// public int GetF(int x, int y) // Generated method
        /// {
        ///     return x * y;
        /// }
        /// 
        /// public void M(int x, int y, int f)
        /// {
        ///     Console.WriteLine(f);
        /// }
        /// 
        /// public void InvokeMethod()
        /// {
        ///     M(5, 6, GetF(5, 6)); //Fills in with call to generated method
        /// }
        /// 
        /// -----------------------------------------------------------------------
        /// ****Overload Example:****
        /// public void M(int x, int y)
        /// {
        ///     int f = [|x * y|];
        ///     Console.WriteLine(f);
        /// }
        /// 
        /// public void InvokeMethod()
        /// {
        ///     M(5, 6);
        /// }
        /// 
        /// ---------------------------------------------------->
        /// 
        /// public void M(int x, int y) // Generated overload
        /// {
        ///     M(x, y, x * y)
        /// }
        /// 
        /// public void M(int x, int y, int f)
        /// {
        ///     Console.WriteLine(f);
        /// }
        /// 
        /// public void InvokeMethod()
        /// {
        ///     M(5, 6);
        /// }
        /// </summary>
        private async Task<SyntaxNode> ModifyDocumentInvocationsTrampolineOverloadAndIntroduceParameterAsync(Compilation compilation, Document currentDocument,
            List<TExpressionSyntax> invocations, int insertionIndex, CancellationToken cancellationToken)
        {
            var invocationSemanticModel = await currentDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await currentDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, _generator);
            var parameterName = await GetNewParameterNameAsync(cancellationToken).ConfigureAwait(false);
            var expressionParameterMap = await MapExpressionToParametersAsync(cancellationToken).ConfigureAwait(false);
            // Creating a new method name by concatenating the parameter name that has been upper-cased.
            var newMethodIdentifier = "Get" + parameterName.ToPascalCase();
            var validParameters = _methodSymbol.Parameters.Intersect(expressionParameterMap.Values).ToImmutableArray();

            if (_actionKind is IntroduceParameterCodeActionKind.Trampoline)
            {
                // Creating an empty map here to reuse so that we do not create a new dictionary for
                // every single invocation.
                var parameterToArgumentMap = new Dictionary<IParameterSymbol, int>();
                foreach (var invocation in invocations)
                {
                    // Skipping object creation expressions since they should not have the option to trampoline.
                    if (invocation is TObjectCreationExpressionSyntax)
                    {
                        continue;
                    }

                    var argumentListSyntax = _syntaxFacts.GetArgumentListOfInvocationExpression(invocation);
                    if (argumentListSyntax == null)
                        continue;

                    editor.ReplaceNode(argumentListSyntax, (currentArgumentListSyntax, _) =>
                    {
                        return GenerateNewArgumentListSyntaxForTrampoline(compilation, invocationSemanticModel,
                            parameterToArgumentMap, currentArgumentListSyntax, argumentListSyntax, invocation,
                            validParameters, parameterName, newMethodIdentifier, insertionIndex, cancellationToken);
                    });
                }
            }

            // If you are at the original document, then also introduce the new method and introduce the parameter.
            if (currentDocument.Id == _originalDocument.Id)
            {
                var newMethodNode = _actionKind is IntroduceParameterCodeActionKind.Trampoline
                    ? await ExtractMethodAsync(validParameters, newMethodIdentifier, _generator, cancellationToken).ConfigureAwait(false)
                    : await GenerateNewMethodOverloadAsync(insertionIndex, _generator, cancellationToken).ConfigureAwait(false);
                editor.InsertBefore(_containerMethod, newMethodNode);

                await UpdateExpressionInOriginalFunctionAsync(editor, cancellationToken).ConfigureAwait(false);
                var parameterType = await GetTypeOfExpressionAsync(cancellationToken).ConfigureAwait(false);
                var parameter = _generator.ParameterDeclaration(parameterName, _generator.TypeExpression(parameterType));
                editor.InsertParameter(_containerMethod, insertionIndex, parameter);
            }

            return editor.GetChangedRoot();

            // Adds an argument which is an invocation of the newly created method to the callsites
            // of the method invocations where a parameter was added.
            // Example:
            // public void M(int x, int y)
            // {
            //     int f = [|x * y|];
            //     Console.WriteLine(f);
            // }
            // 
            // public void InvokeMethod()
            // {
            //     M(5, 6);
            // }
            //
            // ---------------------------------------------------->
            // 
            // public int GetF(int x, int y)
            // {
            //     return x * y;
            // }
            // 
            // public void M(int x, int y)
            // {
            //     int f = x * y;
            //     Console.WriteLine(f);
            // }
            //
            // public void InvokeMethod()
            // {
            //     M(5, 6, GetF(5, 6)); // This is the generated invocation which is a new argument at the call site
            // }
            SyntaxNode GenerateNewArgumentListSyntaxForTrampoline(Compilation compilation, SemanticModel invocationSemanticModel,
                Dictionary<IParameterSymbol, int> parameterToArgumentMap, SyntaxNode currentArgumentListSyntax,
                SyntaxNode argumentListSyntax, SyntaxNode invocation, ImmutableArray<IParameterSymbol> validParameters,
                string parameterName, string newMethodIdentifier, int insertionIndex, CancellationToken cancellationToken)
            {
                var invocationArguments = _syntaxFacts.GetArgumentsOfArgumentList(argumentListSyntax);
                parameterToArgumentMap.Clear();
                MapParameterToArgumentsAtInvocation(parameterToArgumentMap, invocationArguments, invocationSemanticModel, cancellationToken);
                var currentInvocationArguments = (SeparatedSyntaxList<TArgumentSyntax>)_syntaxFacts.GetArgumentsOfArgumentList(currentArgumentListSyntax);
                var requiredArguments = new List<SyntaxNode>();

                foreach (var parameterSymbol in validParameters)
                {
                    if (parameterToArgumentMap.TryGetValue(parameterSymbol, out var index))
                    {
                        requiredArguments.Add(currentInvocationArguments[index]);
                    }
                }

                var conditionalRoot = _syntaxFacts.GetRootConditionalAccessExpression(invocation);
                var named = ShouldArgumentBeNamed(compilation, invocationSemanticModel, invocationArguments, insertionIndex, cancellationToken);
                var newMethodInvocation = GenerateNewMethodInvocation(invocation, requiredArguments, newMethodIdentifier);

                SeparatedSyntaxList<TArgumentSyntax> allArguments;
                if (conditionalRoot is null)
                {
                    allArguments = AddArgumentToArgumentList(currentInvocationArguments, newMethodInvocation, parameterName, insertionIndex, named);
                }
                else
                {
                    // Conditional Access expressions are parents of invocations, so it is better to just replace the
                    // invocation in place then rebuild the tree structure.
                    var expressionsWithConditionalAccessors = conditionalRoot.ReplaceNode(invocation, newMethodInvocation);
                    allArguments = AddArgumentToArgumentList(currentInvocationArguments, expressionsWithConditionalAccessors, parameterName, insertionIndex, named);
                }

                return _service.UpdateArgumentListSyntax(currentArgumentListSyntax, allArguments);
            }
        }

        private async Task<ITypeSymbol> GetTypeOfExpressionAsync(CancellationToken cancellationToken)
        {
            var semanticModel = await _originalDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeSymbol = semanticModel.GetTypeInfo(_expression, cancellationToken).ConvertedType ?? semanticModel.Compilation.ObjectType;
            return typeSymbol;
        }

        private SyntaxNode GenerateNewMethodInvocation(SyntaxNode invocation, List<SyntaxNode> arguments, string newMethodIdentifier)
        {
            var methodName = _generator.IdentifierName(newMethodIdentifier);
            var fullExpression = _syntaxFacts.GetExpressionOfInvocationExpression(invocation);
            if (_syntaxFacts.IsMemberAccessExpression(fullExpression))
            {
                var receiverExpression = _syntaxFacts.GetExpressionOfMemberAccessExpression(fullExpression);
                methodName = _generator.MemberAccessExpression(receiverExpression, newMethodIdentifier);
            }
            else if (_syntaxFacts.IsMemberBindingExpression(fullExpression))
            {
                methodName = _generator.MemberBindingExpression(_generator.IdentifierName(newMethodIdentifier));
            }

            return _generator.InvocationExpression(methodName, arguments);
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
        /// public int GetF(int x, int y)
        /// {
        ///     return x * y;
        /// }
        /// 
        /// public void M(int x, int y)
        /// {
        ///     int f = x * y;
        /// }
        /// </summary>
        private async Task<SyntaxNode> ExtractMethodAsync(ImmutableArray<IParameterSymbol> validParameters, string newMethodIdentifier, SyntaxGenerator generator, CancellationToken cancellationToken)
        {
            // Remove trivia so the expression is in a single line and does not affect the spacing of the following line
            var returnStatement = generator.ReturnStatement(_expression.WithoutTrivia());
            var typeSymbol = await GetTypeOfExpressionAsync(cancellationToken).ConfigureAwait(false);
            var newMethodDeclaration = await CreateMethodDeclarationAsync(returnStatement,
                validParameters, newMethodIdentifier, typeSymbol, isTrampoline: true, cancellationToken).ConfigureAwait(false);
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
        /// public void M(int x, int y) // Original function (which will be mutated in a later step)
        /// {
        ///     int f = x * y;
        /// }
        /// </summary>
        private async Task<SyntaxNode> GenerateNewMethodOverloadAsync(int insertionIndex, SyntaxGenerator generator, CancellationToken cancellationToken)
        {
            // Need the parameters from the original function as arguments for the invocation
            var arguments = generator.CreateArguments(_methodSymbol.Parameters);

            // Remove trivia so the expression is in a single line and does not affect the spacing of the following line
            arguments = arguments.Insert(insertionIndex, generator.Argument(_expression.WithoutTrivia()));
            var memberName = _methodSymbol.IsGenericMethod
                ? generator.GenericName(_methodSymbol.Name, _methodSymbol.TypeArguments)
                : generator.IdentifierName(_methodSymbol.Name);
            var invocation = generator.InvocationExpression(memberName, arguments);

            var newStatement = _methodSymbol.ReturnsVoid
               ? generator.ExpressionStatement(invocation)
               : generator.ReturnStatement(invocation);

            var newMethodDeclaration = await CreateMethodDeclarationAsync(newStatement,
                validParameters: null, newMethodIdentifier: null, typeSymbol: null, isTrampoline: false, cancellationToken).ConfigureAwait(false);
            return newMethodDeclaration;
        }

        private async Task<SyntaxNode> CreateMethodDeclarationAsync(SyntaxNode newStatement, ImmutableArray<IParameterSymbol>? validParameters,
            string? newMethodIdentifier, ITypeSymbol? typeSymbol, bool isTrampoline, CancellationToken cancellationToken)
        {
            var info = await _originalDocument.GetCodeGenerationInfoAsync(CodeGenerationContext.Default, cancellationToken).ConfigureAwait(false);

            var newMethod = isTrampoline
                ? CodeGenerationSymbolFactory.CreateMethodSymbol(_methodSymbol, name: newMethodIdentifier, parameters: validParameters, statements: [newStatement], returnType: typeSymbol)
                : CodeGenerationSymbolFactory.CreateMethodSymbol(_methodSymbol, statements: [newStatement], containingType: _methodSymbol.ContainingType);

            var newMethodDeclaration = info.Service.CreateMethodDeclaration(newMethod, CodeGenerationDestination.Unspecified, info, cancellationToken);
            Contract.ThrowIfNull(newMethodDeclaration);
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
        private async Task<SyntaxNode> ModifyDocumentInvocationsAndIntroduceParameterAsync(Compilation compilation, Document document, int insertionIndex,
            List<TExpressionSyntax> invocations, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, _generator);
            var invocationSemanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var parameterToArgumentMap = new Dictionary<IParameterSymbol, int>();
            var expressionToParameterMap = await MapExpressionToParametersAsync(cancellationToken).ConfigureAwait(false);
            var parameterName = await GetNewParameterNameAsync(cancellationToken).ConfigureAwait(false);
            foreach (var invocation in invocations)
            {
                var expressionEditor = new SyntaxEditor(_expression, _generator);

                var argumentListSyntax = invocation is TObjectCreationExpressionSyntax
                    ? _syntaxFacts.GetArgumentListOfBaseObjectCreationExpression(invocation)
                    : _syntaxFacts.GetArgumentListOfInvocationExpression(invocation);

                if (argumentListSyntax == null)
                    continue;

                var invocationArguments = _syntaxFacts.GetArgumentsOfArgumentList(argumentListSyntax);

                if (insertionIndex > invocationArguments.Count)
                    continue;

                parameterToArgumentMap.Clear();
                MapParameterToArgumentsAtInvocation(parameterToArgumentMap, invocationArguments, invocationSemanticModel, cancellationToken);

                if (argumentListSyntax is not null)
                {
                    editor.ReplaceNode(argumentListSyntax, (currentArgumentListSyntax, _) =>
                    {
                        var updatedInvocationArguments = (SeparatedSyntaxList<TArgumentSyntax>)_syntaxFacts.GetArgumentsOfArgumentList(currentArgumentListSyntax);
                        var updatedExpression = CreateNewArgumentExpression(expressionEditor, expressionToParameterMap, parameterToArgumentMap, updatedInvocationArguments);
                        var named = ShouldArgumentBeNamed(compilation, invocationSemanticModel, invocationArguments, insertionIndex, cancellationToken);
                        var allArguments = AddArgumentToArgumentList(updatedInvocationArguments,
                            updatedExpression.WithAdditionalAnnotations(Formatter.Annotation), parameterName, insertionIndex, named);
                        return _service.UpdateArgumentListSyntax(currentArgumentListSyntax, allArguments);
                    });
                }
            }

            // If you are at the original document, then also introduce the new method and introduce the parameter.
            if (document.Id == _originalDocument.Id)
            {
                await UpdateExpressionInOriginalFunctionAsync(editor, cancellationToken).ConfigureAwait(false);
                var parameterType = await GetTypeOfExpressionAsync(cancellationToken).ConfigureAwait(false);
                var parameter = _generator.ParameterDeclaration(name: parameterName, type:
                    _generator.TypeExpression(parameterType));
                editor.InsertParameter(_containerMethod, insertionIndex, parameter);
            }

            return editor.GetChangedRoot();
        }

        /// <summary>
        /// This method iterates through the variables in the expression and maps the variables back to the parameter
        /// it is associated with. It then maps the parameter back to the argument at the invocation site and gets the
        /// index to retrieve the updated arguments at the invocation.
        /// </summary>
        private TExpressionSyntax CreateNewArgumentExpression(SyntaxEditor editor,
            Dictionary<TIdentifierNameSyntax, IParameterSymbol> mappingDictionary,
            Dictionary<IParameterSymbol, int> parameterToArgumentMap,
            SeparatedSyntaxList<SyntaxNode> updatedInvocationArguments)
        {
            foreach (var (variable, mappedParameter) in mappingDictionary)
            {
                var parameterMapped = parameterToArgumentMap.TryGetValue(mappedParameter, out var index);
                if (parameterMapped)
                {
                    var updatedInvocationArgument = updatedInvocationArguments[index];
                    var argumentExpression = _syntaxFacts.GetExpressionOfArgument(updatedInvocationArgument);
                    var parenthesizedArgumentExpression = editor.Generator.AddParentheses(argumentExpression, includeElasticTrivia: false);
                    editor.ReplaceNode(variable, parenthesizedArgumentExpression);
                }
                else if (mappedParameter.HasExplicitDefaultValue)
                {
                    var generatedExpression = _service.GenerateExpressionFromOptionalParameter(mappedParameter);
                    var parenthesizedGeneratedExpression = editor.Generator.AddParentheses(generatedExpression, includeElasticTrivia: false);
                    editor.ReplaceNode(variable, parenthesizedGeneratedExpression);
                }
            }

            return (TExpressionSyntax)editor.GetChangedRoot();
        }

        /// <summary>
        /// If the parameter is optional and the invocation does not specify the parameter, then
        /// a named argument needs to be introduced.
        /// </summary>
        private SeparatedSyntaxList<TArgumentSyntax> AddArgumentToArgumentList(
            SeparatedSyntaxList<TArgumentSyntax> invocationArguments, SyntaxNode newArgumentExpression,
            string parameterName, int insertionIndex, bool named)
        {
            var argument = named
                ? (TArgumentSyntax)_generator.Argument(parameterName, RefKind.None, newArgumentExpression)
                : (TArgumentSyntax)_generator.Argument(newArgumentExpression);
            return invocationArguments.Insert(insertionIndex, argument);
        }

        private bool ShouldArgumentBeNamed(Compilation compilation, SemanticModel semanticModel,
            SeparatedSyntaxList<SyntaxNode> invocationArguments, int methodInsertionIndex,
            CancellationToken cancellationToken)
        {
            var invocationInsertIndex = 0;
            foreach (var invocationArgument in invocationArguments)
            {
                var argumentParameter = _semanticFacts.FindParameterForArgument(semanticModel, invocationArgument, cancellationToken);
                if (argumentParameter is not null && ShouldParameterBeSkipped(compilation, argumentParameter))
                {
                    invocationInsertIndex++;
                }
                else
                {
                    break;
                }
            }

            return invocationInsertIndex < methodInsertionIndex;
        }

        private static bool ShouldParameterBeSkipped(Compilation compilation, IParameterSymbol parameter)
            => !parameter.HasExplicitDefaultValue &&
               !parameter.IsParams &&
               !parameter.Type.Equals(compilation.GetTypeByMetadataName(typeof(CancellationToken)?.FullName!));

        private void MapParameterToArgumentsAtInvocation(
            Dictionary<IParameterSymbol, int> mapping, SeparatedSyntaxList<SyntaxNode> arguments,
            SemanticModel invocationSemanticModel, CancellationToken cancellationToken)
        {
            for (var i = 0; i < arguments.Count; i++)
            {
                var argumentParameter = _semanticFacts.FindParameterForArgument(invocationSemanticModel, arguments[i], cancellationToken);
                if (argumentParameter is not null)
                {
                    mapping[argumentParameter] = i;
                }
            }
        }

        /// <summary>
        /// Gets the matches of the expression and replaces them with the identifier.
        /// Special case for the original matching expression, if its parent is a LocalDeclarationStatement then it can
        /// be removed because assigning the local dec variable to a parameter is repetitive. Does not need a rename
        /// annotation since the user has already named the local declaration.
        /// Otherwise, it needs to have a rename annotation added to it because the new parameter gets a randomly
        /// generated name that the user can immediately change.
        /// </summary>
        private async Task UpdateExpressionInOriginalFunctionAsync(SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var generator = editor.Generator;
            var matches = await FindMatchesAsync(cancellationToken).ConfigureAwait(false);
            var parameterName = await GetNewParameterNameAsync(cancellationToken).ConfigureAwait(false);
            var replacement = (TIdentifierNameSyntax)generator.IdentifierName(parameterName);

            foreach (var match in matches)
            {
                // Special case the removal of the originating expression to either remove the local declaration
                // or to add a rename annotation.
                if (!match.Equals(_expression))
                {
                    editor.ReplaceNode(match, replacement);
                }
                else
                {
                    if (ShouldRemoveVariableDeclaratorContainingExpression(out _, out var localDeclaration))
                    {
                        editor.RemoveNode(localDeclaration);
                    }
                    else
                    {
                        // Found the initially selected expression. Replace it with the new name we choose, but also annotate
                        // that name with the RenameAnnotation so a rename session is started where the user can pick their
                        // own preferred name.
                        replacement = (TIdentifierNameSyntax)generator.IdentifierName(generator.Identifier(parameterName)
                            .WithAdditionalAnnotations(RenameAnnotation.Create()));
                        editor.ReplaceNode(match, replacement);
                    }
                }
            }
        }

        /// <summary>
        /// Finds the matches of the expression within the same block.
        /// </summary>
        private async Task<IEnumerable<TExpressionSyntax>> FindMatchesAsync(CancellationToken cancellationToken)
        {
            if (!_allOccurrences)
                return [_expression];

            var syntaxFacts = _originalDocument.GetRequiredLanguageService<ISyntaxFactsService>();
            var originalSemanticModel = await _originalDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var matches = from nodeInCurrent in _containerMethod.DescendantNodesAndSelf().OfType<TExpressionSyntax>()
                          where NodeMatchesExpression(originalSemanticModel, nodeInCurrent, cancellationToken)
                          select nodeInCurrent;
            return matches;
        }

        private bool NodeMatchesExpression(SemanticModel originalSemanticModel, TExpressionSyntax currentNode, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (currentNode == _expression)
            {
                return true;
            }

            return SemanticEquivalence.AreEquivalent(
                originalSemanticModel, originalSemanticModel, _expression, currentNode);
        }
    }
}
