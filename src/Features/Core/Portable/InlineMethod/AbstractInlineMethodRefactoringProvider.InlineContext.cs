// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Precedence;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal abstract partial class AbstractInlineMethodRefactoringProvider<TInvocationSyntaxNode, TExpressionSyntax, TArgumentSyntax>
        where TInvocationSyntaxNode : SyntaxNode
        where TExpressionSyntax : SyntaxNode
        where TArgumentSyntax : SyntaxNode
    {
        private class InlineMethodContext
        {
            /// <summary>
            /// Statements should be inserted before the <see cref="StatementContainingCallee"/>.
            /// </summary>
            public ImmutableArray<SyntaxNode> StatementsToInsertBeforeCallee { get; }

            /// <summary>
            /// Statement invokes the callee. All the generated declarations should be put before this node.
            /// </summary>
            public SyntaxNode? StatementContainingCallee { get; }

            /// <summary>
            /// Inline content for the callee method. It should replace <see cref="SyntaxNodeToReplace"/>.
            /// It will be null if nothing need to be inlined.
            /// </summary>
            public SyntaxNode? InlineSyntaxNode { get; }

            /// <summary>
            /// SyntaxNode needs to be replaced by <see cref="InlineSyntaxNode"/>
            /// </summary>
            public SyntaxNode? SyntaxNodeToReplace { get; }

            /// <summary>
            /// Indicate is <see cref="InlineSyntaxNode"/> has Await Expression
            /// </summary>
            public bool ContainsAwaitExpression { get; }

            /// <summary>
            /// A preferred name used to generated a declaration when the
            /// inline method has a return value but is not assigned to a variable.
            /// </summary>
            private const string TemporaryName = "temp";

            private InlineMethodContext(
                ImmutableArray<SyntaxNode> statementsToInsertBeforeCallee,
                SyntaxNode? statementContainingCallee,
                SyntaxNode? inlineSyntaxNode,
                SyntaxNode? syntaxNodeToReplace,
                bool containsAwaitExpression)
            {
                StatementsToInsertBeforeCallee = statementsToInsertBeforeCallee;
                StatementContainingCallee = statementContainingCallee;
                InlineSyntaxNode = inlineSyntaxNode;
                SyntaxNodeToReplace = syntaxNodeToReplace;
                ContainsAwaitExpression = containsAwaitExpression;
            }

            public static async Task<InlineMethodContext> GetInlineContextAsync(
                AbstractInlineMethodRefactoringProvider<TInvocationSyntaxNode, TExpressionSyntax, TArgumentSyntax> inlineMethodRefactoringProvider,
                ISyntaxFacts syntaxFacts,
                IPrecedenceService precedenceService,
                Document document,
                SemanticModel semanticModel,
                SyntaxNode calleeInvocationSyntaxNode,
                IMethodSymbol calleeMethodSymbol,
                SyntaxNode calleeMethodDeclarationSyntaxNode,
                MethodParametersInfo methodParametersInfo,
                SyntaxNode root,
                CancellationToken cancellationToken)
            {
                var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
                var rawInlineSyntaxNode = inlineMethodRefactoringProvider.GetInlineStatement(calleeMethodDeclarationSyntaxNode);
                var inlineSyntaxNode = rawInlineSyntaxNode;
                // The replacement/insertion work done might cause naming conflict.
                // So first compute a mapping from symbol to newName (renameTable)
                // Cases are:
                // 1. Replace the callee's parameter withe caller's identifier might cause conflict in callee.
                // Example:
                // Before:
                // void Caller()
                // {
                //     int i, j = 10;
                //     Callee(i, j);
                // }
                // void Callee(int a, int b)
                // {
                //     DoSomething(a, b, out int i);
                // }
                // After:
                // void Caller()
                // {
                //     int i, j = 10;
                //     DoSomething(a, b, out int i1);
                // }
                // void Callee(int a, int b)
                // {
                //     DoSomething(a, b, out int i);
                // }
                // 2. Use the parameter's name to generate declarations in caller might cause conflict in the caller
                // In either case, rename the symbol in Callee.
                // Example:
                // Before:
                // void Caller(int i, int j)
                // {
                //     Callee(Foo(), Bar());
                // }
                // void Callee(int i, int j)
                // {
                //     DoSomething(i, j);
                // }
                // After:
                // void Caller(int i, int j)
                // {
                //     int i1 = Foo();
                //     int j1 = Bar();
                //     DoSomething(i1, j1)
                // }
                // void Callee(int i, int j)
                // {
                //     DoSomething(i, j);
                // }
                var parametersWithIdentifierArgumentToName = methodParametersInfo.ParametersWithIdentifierArgument
                    .Select(parameterAndArgument =>
                        (parameterAndArgument.parameterSymbol,
                            semanticModel.GetSymbolInfo(parameterAndArgument.identifierSyntaxNode, cancellationToken)
                                .GetAnySymbol()?.Name))
                    .Where(parameterAndName => parameterAndName.Name != null)
                    .ToImmutableArray();

                var parametersWithVariableDeclarationArgumentToName = methodParametersInfo.ParametersWithVariableDeclarationArgument
                    .Select(parameterAndArgument =>
                        (parameterAndArgument.parameterSymbol,
                            semanticModel
                                .GetSymbolInfo(parameterAndArgument.variableDeclarationNode, cancellationToken)
                                .GetAnySymbol()?.Name))
                    .Where(parameterAndName => parameterAndName.Name != null)
                    .ToImmutableArray();

                var allSymbolsOfCaller = semanticModel.LookupSymbols(calleeInvocationSyntaxNode.Span.End);
                var inUsedNamesOfCaller = allSymbolsOfCaller
                    .Select(symbol => symbol.Name)
                    .ToImmutableHashSet();
                var staticAndFieldNamesInClass = allSymbolsOfCaller
                    .Where(symbol => symbol.IsStatic || symbol.IsKind(SymbolKind.Field))
                    .Select(symbol => symbol.Name)
                    .ToImmutableHashSet();
                var operationVisitor = new VariableDeclaratorOperationVisitor(cancellationToken);
                var calleeOperation = semanticModel.GetOperation(calleeMethodDeclarationSyntaxNode, cancellationToken);
                var localSymbolOfCallee = operationVisitor.FindAllLocalSymbols(calleeOperation);
                var inUsedNameOfCallee = localSymbolOfCallee
                    .Select(symbol => symbol.Name)
                    .Concat(staticAndFieldNamesInClass)
                    .ToImmutableHashSet();

                var renameTable = ComputeRenameTable(
                    parametersWithIdentifierArgumentToName!,
                    methodParametersInfo.ParametersNeedGenerateDeclarations.SelectAsArray(parameterToArgument => parameterToArgument.parameterSymbol),
                    parametersWithVariableDeclarationArgumentToName!,
                    inUsedNamesOfCaller,
                    localSymbolOfCallee,
                    inUsedNameOfCallee);

                // Generate all the statements need to be put in the caller.
                var localDeclarationStatementsNeedInsert = GetLocalDeclarationStatementsNeedInsert(
                    inlineMethodRefactoringProvider,
                    semanticModel,
                    syntaxGenerator,
                    methodParametersInfo.ParametersNeedGenerateDeclarations,
                    parametersWithVariableDeclarationArgumentToName!,
                    renameTable,
                    cancellationToken);

                // The syntax node that contains the invocation syntax node.
                // Example1: var x = Callee(); Then LocalDeclarationStatement is the containing syntax node.
                // Example1: if (Callee()) {} Then IfStatement is the containing syntax node
                var statementContainingCallee = inlineMethodRefactoringProvider.GetStatementContainsCallee(calleeInvocationSyntaxNode);
                if (inlineSyntaxNode == null)
                {
                    return new InlineMethodContext(
                        localDeclarationStatementsNeedInsert,
                        statementContainingCallee,
                        null,
                        statementContainingCallee,
                        false);
                }

                // Do the replacement work within the callee. Including:
                // 1. Literal replacement
                // Example:
                // Before:
                // void Caller()
                // {
                //     Callee(20)
                // }
                // void Callee(int i, int j = 10)
                // {
                //     Bar(i, j);
                // }
                // After:
                // void Caller()
                // {
                //     Bar(20, 10);
                // }
                // void Callee(int i, int j = 10)
                // {
                //     Bar(i, j);
                // }
                // 2. Identifier rename
                // Example:
                // Before:
                // void Caller()
                // {
                //     int a, b;
                //     Callee(a, b)
                // }
                // void Callee(int i, int j)
                // {
                //     Bar(i, j);
                // }
                // After:
                // void Caller()
                // {
                //     int a, b;
                //     Bar(a, b);
                // }
                // void Callee(int i, int j = 10)
                // {
                //     Bar(i, j);
                // }
                // 3. Type arguments (generics)
                // Example:
                // Before:
                // void Caller() { Callee<int>(); }
                // void Callee<T>() => Print(typeof<T>);
                // After:
                // void Caller() { Print(typeof(int)); }
                // void Callee<T>() => Print(typeof<T>);
                var replacementTable = ComputeReplacementTable(
                    inlineMethodRefactoringProvider,
                    calleeMethodSymbol,
                    methodParametersInfo.ParametersWithLiteralArgument,
                    methodParametersInfo.ParametersWithDefaultValue,
                    syntaxGenerator,
                    renameTable);

                inlineSyntaxNode = await ReplaceAllSyntaxNodesForSymbolAsync(
                    document,
                    inlineSyntaxNode,
                    syntaxGenerator,
                    root,
                    replacementTable,
                    cancellationToken).ConfigureAwait(false);

                // Check if there is await expression. It is used later if the caller should be changed to async
                var containsAwaitExpression = inlineSyntaxNode
                    .DescendantNodesAndSelf()
                    .Any(node => node != null && syntaxFacts.IsAwaitExpression(node));

                if (syntaxFacts.IsExpressionStatement(statementContainingCallee)&& !calleeMethodSymbol.ReturnsVoid)
                {
                    // If the callee is invoked like
                    // void Caller()
                    // {
                    //     Callee();
                    // }
                    // int Callee()
                    // {
                    //     return 1;
                    // };
                    // After it should be:
                    // void Caller()
                    // {
                    //     int tmp = 1;
                    // }
                    // int Callee()
                    // {
                    //     return 1;
                    // };
                    // One variable declaration needs to be generated.
                    var unusedTemporaryName = GetUnusedName(
                        TemporaryName,
                        inUsedNamesOfCaller
                            .Concat(inUsedNameOfCallee)
                            .Concat(renameTable.Values)
                            .ToImmutableHashSet());

                    return new InlineMethodContext(
                        localDeclarationStatementsNeedInsert,
                        statementContainingCallee,
                        syntaxGenerator
                            .LocalDeclarationStatement(calleeMethodSymbol.ReturnType, unusedTemporaryName, inlineSyntaxNode),
                        statementContainingCallee,
                        containsAwaitExpression);
                }

                // Check if parenthesis is needed by comparing the Precedence.
                // Example:
                // Before:
                // void Caller() { var x = Callee() * 3; }
                // int Callee() => 1 + 2;
                // After:
                // void Caller() { var x = (1 + 2) * 3; }
                // int Callee() => 1 + 2;
                var parent = calleeInvocationSyntaxNode.Parent;
                if (parent != null
                    && !syntaxFacts.IsParenthesizedExpression(inlineSyntaxNode)
                    && inlineMethodRefactoringProvider.ShouldCheckTheExpressionPrecedenceInCallee(parent)
                    && inlineMethodRefactoringProvider.ShouldCheckTheExpressionPrecedenceInCallee(inlineSyntaxNode))
                {
                    var shouldWrapInParenthesis = false;
                    var precedenceOfInlineExpression = precedenceService.GetOperatorPrecedence(inlineSyntaxNode);
                    var precedenceOfInvocation = precedenceService.GetOperatorPrecedence(parent);
                    if (precedenceOfInlineExpression != 0 && precedenceOfInvocation != 0)
                    {
                        if (precedenceOfInlineExpression < precedenceOfInvocation)
                        {
                            shouldWrapInParenthesis = true;
                        }
                        else if (precedenceOfInlineExpression == precedenceOfInvocation)
                        {
                            // If precedences are equal, do a more carefully check based on the associativity of the expression.
                            shouldWrapInParenthesis = inlineMethodRefactoringProvider.NeedWrapInParenthesisWhenPrecedenceAreEqual(calleeInvocationSyntaxNode);
                        }
                    }

                    if (shouldWrapInParenthesis)
                    {
                        inlineSyntaxNode = syntaxGenerator.AddParentheses(inlineSyntaxNode);
                    }
                }

                return new InlineMethodContext(
                    localDeclarationStatementsNeedInsert,
                    statementContainingCallee,
                    // add the trivia of the calleeInvocationSyntaxNode so that the format is correct
                    inlineSyntaxNode
                        .WithLeadingTrivia(calleeInvocationSyntaxNode.GetLeadingTrivia())
                        .WithTrailingTrivia(calleeInvocationSyntaxNode.GetTrailingTrivia()),
                    calleeInvocationSyntaxNode,
                    containsAwaitExpression);
            }

            private static ImmutableArray<SyntaxNode> GetLocalDeclarationStatementsNeedInsert(
                AbstractInlineMethodRefactoringProvider<TInvocationSyntaxNode, TExpressionSyntax, TArgumentSyntax> inlineMethodRefactoringProvider,
                SemanticModel semanticModel,
                SyntaxGenerator syntaxGenerator,
                ImmutableArray<(IParameterSymbol parameterSymbol, ImmutableArray<SyntaxNode> arguments)> parametersNeedGenerateDeclarations,
                ImmutableArray<(IParameterSymbol parameterSymbol, string name)> parametersWithVariableDeclarationArgument,
                ImmutableDictionary<ISymbol, string> renameTable,
                CancellationToken cancellationToken)
                => parametersNeedGenerateDeclarations
                    .Select(parameterAndArguments => CreateLocalDeclarationStatement(inlineMethodRefactoringProvider, semanticModel, syntaxGenerator, renameTable, parameterAndArguments, cancellationToken))
                    .Concat(parametersWithVariableDeclarationArgument
                        .Select(parameterAndName => syntaxGenerator.LocalDeclarationStatement(parameterAndName.parameterSymbol.Type, parameterAndName.name)))
                    .ToImmutableArray();

            private static SyntaxNode CreateLocalDeclarationStatement(
                AbstractInlineMethodRefactoringProvider<TInvocationSyntaxNode, TExpressionSyntax, TArgumentSyntax> inlineMethodRefactoringProvider,
                SemanticModel semanticModel,
                SyntaxGenerator syntaxGenerator,
                ImmutableDictionary<ISymbol, string> renameTable,
                (IParameterSymbol parameterSymbol, ImmutableArray<SyntaxNode> arguments) parameterAndArguments,
                CancellationToken cancellationToken)
            {
                var (parameterSymbol, arguments) = parameterAndArguments;
                var name = renameTable.ContainsKey(parameterSymbol) ? renameTable[parameterSymbol] : parameterSymbol.Name;
                var generateArrayDeclarationStatement = parameterSymbol.IsParams
                // If arguments number is not one, it means this paramsArray maps to multiple or zero arguments. An array declaration is needed.
                    && (arguments.Length != 1
                // If the arguments number is one, it could be the single element of the array (an array declaration is needed)
                // Example: void Callee(params int[] x) {}
                // void Caller() { Callee(1, 2, 3) }
                // or it could an array (like array creation, and array declaration is not need)
                // Example: void Callee(params int[] x) {}
                // void Caller() { Callee(new int[] { 1, 2, 3}) }
                        || (arguments.Length == 1 && !parameterSymbol.Type.Equals(semanticModel.GetTypeInfo(arguments[0], cancellationToken).Type)));

                if (generateArrayDeclarationStatement)
                {
                    return syntaxGenerator.LocalDeclarationStatement(
                        parameterSymbol.Type,
                        name,
                        inlineMethodRefactoringProvider.GenerateArrayInitializerExpression(arguments));
                }
                else
                {
                    // In all the other cases, one parameter should only maps to one arguments.
                    return syntaxGenerator.LocalDeclarationStatement(parameterSymbol.Type, name, arguments[0]);
                }
            }

            private static async Task<SyntaxNode> ReplaceAllSyntaxNodesForSymbolAsync(
                Document document,
                SyntaxNode inlineSyntaxNode,
                SyntaxGenerator syntaxGenerator,
                SyntaxNode root,
                ImmutableDictionary<ISymbol, SyntaxNode> replacementTable,
                CancellationToken cancellationToken)
            {
                var editor = new SyntaxEditor(inlineSyntaxNode, syntaxGenerator);

                foreach (var (symbol, syntaxNode) in replacementTable)
                {
                    var allReferences = await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);
                    var allSyntaxNodesToReplace = allReferences
                        .SelectMany(reference => reference.Locations
                            .Select(location => root.FindNode(location.Location.SourceSpan)))
                        .ToImmutableArray();

                    foreach (var nodeToReplace in allSyntaxNodesToReplace)
                    {
                        if (editor.OriginalRoot.Contains(nodeToReplace))
                        {
                            var replacementNodeWithTrivia = syntaxNode
                                .WithLeadingTrivia(nodeToReplace.GetLeadingTrivia())
                                .WithTrailingTrivia(nodeToReplace.GetTrailingTrivia());
                            editor.ReplaceNode(nodeToReplace, replacementNodeWithTrivia);
                        }
                    }
                }

                return editor.GetChangedRoot();
            }

            /// <summary>
            /// Generate a dictionary which key is the symbol in Callee (include local variable and parameter), value
            /// is the replacement syntax node for all its occurence.
            /// </summary>
            private static ImmutableDictionary<ISymbol, SyntaxNode> ComputeReplacementTable(
                AbstractInlineMethodRefactoringProvider<TInvocationSyntaxNode, TExpressionSyntax, TArgumentSyntax> inlineMethodRefactoringProvider,
                IMethodSymbol calleeMethodSymbol,
                ImmutableArray<(IParameterSymbol parameterSymbol , SyntaxNode literalExpression)> parametersWithLiteralArgument,
                ImmutableArray<IParameterSymbol> parametersWithDefaultValue,
                SyntaxGenerator syntaxGenerator,
                ImmutableDictionary<ISymbol, string> renameTable)
            {
                var typeParametersReplacementQuery = calleeMethodSymbol.TypeParameters
                    .Zip(calleeMethodSymbol.TypeArguments,
                        (parameter, argument) => (parameter: (ISymbol)parameter, syntaxNode: inlineMethodRefactoringProvider.GenerateTypeSyntax(argument)));
                var defaultValueReplacementQuery = parametersWithDefaultValue
                    .Select(symbol => (parameter: (ISymbol)symbol, syntaxNode: syntaxGenerator.LiteralExpression(symbol.ExplicitDefaultValue)));
                var literalArgumentReplacementQuery = parametersWithLiteralArgument
                    .Select(parameterAndArgument => (parameter: (ISymbol)parameterAndArgument.parameterSymbol, syntaxNode: parameterAndArgument.literalExpression));

                return renameTable
                    .Select(kvp => (parameter: kvp.Key,
                        syntaxNode: syntaxGenerator.IdentifierName(kvp.Value)))
                    .Concat(typeParametersReplacementQuery)
                    .Concat(defaultValueReplacementQuery)
                    .Concat(literalArgumentReplacementQuery)
                    .ToImmutableDictionary(tuple => tuple.parameter, tuple => tuple.syntaxNode);
            }

            private static string GetUnusedName(string preferredNamePrefix, ImmutableHashSet<string> inUsedNames)
            {
                var newName = preferredNamePrefix;
                while (inUsedNames.Contains(preferredNamePrefix))
                {
                    newName = GenerateNewName(preferredNamePrefix);
                }

                return newName;
            }

            /// <summary>
            /// Get a map which key is the symbol is the identifier in <param name="calleeDeclarationSyntaxNode"/>, and value is
            /// its new name after inlining.
            /// </summary>
            private static ImmutableDictionary<ISymbol, string> ComputeRenameTable(
                ImmutableArray<(IParameterSymbol parameterSymbol, string variableName)> identifierArguments,
                ImmutableArray<IParameterSymbol> parametersNeedGenerateDeclaration,
                ImmutableArray<(IParameterSymbol parameterSymbol, string variableName)> variableDeclarationArguments,
                ImmutableHashSet<string> inUsedNamesOfCaller,
                ImmutableArray<ILocalSymbol> localSymbolsOfCallee,
                ImmutableHashSet<string> inUsedNamesOfCallee)
            {
                // After inlining, there might be naming conflict because
                // case 1: caller's identifier is introduced to callee.
                // Example:
                // Before:
                // void Caller()
                // {
                //     int i = 10;
                //     Callee(i)
                // }
                // void Callee(int j)
                // {
                //     int i = 100;
                // }
                // After inline it should be:
                // void Caller()
                // {
                //     int i = 10;
                //     int i1 = 100;
                // }
                // void Callee(int j)
                // {
                //     int i = 100;
                // }
                //
                // case 2: callee's parameter is introduced to caller
                // Before:
                // void Caller()
                // {
                //     int i = 0;
                //     Callee(Foo())
                // }
                // void Callee(int i)
                // {
                //     Bar(i);
                // }
                // After inline it should be:
                // void Caller()
                // {
                //     int i = 10;
                //     int i1 = Foo();
                //     Bar(i1);
                // }
                // void Callee(int i)
                // {
                //     Bar(i);
                // }
                inUsedNamesOfCallee = inUsedNamesOfCallee
                    .Concat(identifierArguments.Select(parameterAndName => parameterAndName.variableName))
                    .Concat(variableDeclarationArguments.Select(parameterAndName => parameterAndName.variableName))
                    .ToImmutableHashSet();

                var renameTable = identifierArguments.Concat(variableDeclarationArguments)
                    .ToDictionary(parameterAndName => (ISymbol)parameterAndName.parameterSymbol,
                        parameterAndName => parameterAndName.variableName);

                // Handle case 1 discussed above
                foreach (var localSymbol in localSymbolsOfCallee)
                {
                    var localSymbolName = localSymbol.Name;
                    while (inUsedNamesOfCallee.Contains(localSymbolName)
                        || renameTable.ContainsValue(localSymbolName))
                    {
                        localSymbolName = GenerateNewName(localSymbolName);
                    }

                    if (!localSymbolName.Equals(localSymbol.Name))
                    {
                        renameTable[localSymbol] = localSymbolName;
                    }
                }

                // Handle case 2 discussed above
                foreach (var parameterSymbol in parametersNeedGenerateDeclaration)
                {
                    var parameterName = parameterSymbol.Name;
                    while (inUsedNamesOfCaller.Contains(parameterName)
                        || inUsedNamesOfCallee.Contains(parameterName)
                        || renameTable.ContainsValue(parameterName))
                    {
                        parameterName = GenerateNewName(parameterName);
                    }

                    if (!parameterName.Equals(parameterSymbol.Name))
                    {
                        renameTable[parameterSymbol] = parameterName;
                    }
                }

                return renameTable.ToImmutableDictionary();
            }

            /// <summary>
            /// Generate a new identifier name. If <param name="preferredName"/> has a number suffix,
            /// increase it by 1. Otherwise, append 1 to it.
            /// </summary>
            private static string GenerateNewName(string preferredName)
            {
                var stack = new Stack<char>();
                for (var i = preferredName.Length - 1; i >= 0; i--)
                {
                    var currentCharacter = preferredName[i];
                    if (char.IsNumber(currentCharacter))
                    {
                        stack.Push(currentCharacter);
                    }
                    else
                    {
                        break;
                    }
                }

                var suffixNumber = stack.IsEmpty() ? 1 : int.Parse(new string(stack.ToArray())) + 1;
                return preferredName.Substring(0, preferredName.Length - stack.Count) + suffixNumber;
            }
        }

        private class VariableDeclaratorOperationVisitor : OperationWalker
        {
            private readonly CancellationToken _cancellationToken;
            private readonly HashSet<ILocalSymbol> _localSymbols;

            public VariableDeclaratorOperationVisitor(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
                _localSymbols = new HashSet<ILocalSymbol>();
            }

            public ImmutableArray<ILocalSymbol> FindAllLocalSymbols(IOperation? operation)
            {
                if (operation != null)
                {
                    Visit(operation);
                }

                return _localSymbols.ToImmutableArray();
            }

            public override void Visit(IOperation operation)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                if (operation is IVariableDeclaratorOperation variableDeclaratorOperation)
                {
                    _localSymbols.Add(variableDeclaratorOperation.Symbol);
                }

                base.Visit(operation);
            }
        }
    }
}
