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
    internal partial class AbstractInlineMethodRefactoringProvider
    {

        private class MethodInvocationInfo
        {
            public SyntaxNode StatementContainsCalleeInvocationExpression { get; }
            public bool IsCalleeSingleInvoked { get; }
            public bool AssignedToVariable { get; }

            private MethodInvocationInfo(
                SyntaxNode statementContainsCalleeInvocationExpression,
                bool isCalleeSingleInvoked,
                bool assignedToVariable)
            {
                StatementContainsCalleeInvocationExpression = statementContainsCalleeInvocationExpression;
                IsCalleeSingleInvoked = isCalleeSingleInvoked;
                AssignedToVariable = assignedToVariable;
            }

            public static MethodInvocationInfo GetMethodInvocationInfo(
                ISyntaxFacts syntaxFacts,
                AbstractInlineMethodRefactoringProvider inlineMethodRefactoringProvider,
                SyntaxNode calleeInvocationSyntaxNode)
            {
                var statementInvokesCallee = inlineMethodRefactoringProvider.GetInvokingStatement(calleeInvocationSyntaxNode);
                var parent = calleeInvocationSyntaxNode.Parent;
                var isCalleeSingleInvoked = syntaxFacts.IsLocalDeclarationStatement(parent) || syntaxFacts.IsExpressionStatement(parent);
                var assignedToVariable = syntaxFacts.IsLocalDeclarationStatement(parent);
                return new MethodInvocationInfo(statementInvokesCallee, isCalleeSingleInvoked, assignedToVariable);
            }
        }

        private class InlineMethodContext
        {
            /// <summary>
            /// Statements should be inserted before the <see cref="StatementContainsCalleeInvocationExpression"/>.
            /// </summary>
            public ImmutableArray<SyntaxNode> DeclarationStatementsGenerated { get; }

            /// <summary>
            /// Statement invokes the callee. All the generated declarations should be put before this node.
            /// </summary>
            public SyntaxNode StatementContainsCalleeInvocationExpression { get; }

            /// <summary>
            /// Inline content for the callee method. It should replace <see cref="SyntaxNodeToReplace"/>.
            /// It will be null if nothing need to be inlined.
            /// </summary>
            public SyntaxNode? InlineSyntaxNode { get; }

            /// <summary>
            /// SyntaxNode needs to be replaced by <see cref="InlineSyntaxNode"/>
            /// </summary>
            public SyntaxNode SyntaxNodeToReplace { get; }

            private const string TemporaryName = "tmp";

            private InlineMethodContext(
                ImmutableArray<SyntaxNode> declarationStatementsGenerated,
                SyntaxNode statementContainsCalleeInvocationExpression,
                SyntaxNode? inlineSyntaxNode,
                SyntaxNode syntaxNodeToReplace)
            {
                DeclarationStatementsGenerated = declarationStatementsGenerated;
                StatementContainsCalleeInvocationExpression = statementContainsCalleeInvocationExpression;
                InlineSyntaxNode = inlineSyntaxNode;
                SyntaxNodeToReplace = syntaxNodeToReplace;
            }

            public static async Task<InlineMethodContext> GetInlineContextAsync(
                AbstractInlineMethodRefactoringProvider inlineMethodRefactoringProvider,
                ISyntaxFacts syntaxFacts,
                IPrecedenceService precedenceService,
                Document document,
                SemanticModel semanticModel,
                SyntaxNode calleeInvocationSyntaxNode,
                IMethodSymbol calleeMethodSymbol,
                SyntaxNode calleeMethodDeclarationSyntaxNode,
                SyntaxNode? rawInlineSyntaxNode,
                MethodParametersInfo methodParametersInfo,
                MethodInvocationInfo methodInvocationInfo,
                CancellationToken cancellationToken)
            {
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
                var inlineSyntaxNode = rawInlineSyntaxNode;
                // Compute the replacement syntax node for needed symbols(variables and parameters) in the callee.
                var parametersWithIdentifierArgumentToName = methodParametersInfo.ParametersWithIdentifierArgument
                    .Select(parameterAndArgument =>
                        (parameterAndArgument.parameterSymbol,
                            semanticModel
                                .GetSymbolInfo(parameterAndArgument.identifierSyntaxNode, cancellationToken)
                                .GetAnySymbol()
                                ?.Name))
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

                var localDeclarationStatementsNeedInsert = GetLocalDeclarationStatementsNeedInsert(
                    inlineMethodRefactoringProvider,
                    semanticModel,
                    syntaxGenerator,
                    methodParametersInfo.ParametersNeedGenerateDeclarations,
                    parametersWithVariableDeclarationArgumentToName!,
                    renameTable,
                    cancellationToken);

                if (inlineSyntaxNode == null)
                {
                    return new InlineMethodContext(
                        localDeclarationStatementsNeedInsert,
                        methodInvocationInfo.StatementContainsCalleeInvocationExpression,
                        null,
                        methodInvocationInfo.StatementContainsCalleeInvocationExpression);
                }

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

                var shouldWrapInParenthesis = false;
                if (methodInvocationInfo.IsCalleeSingleInvoked
                    && !calleeMethodSymbol.ReturnsVoid
                    && !methodInvocationInfo.AssignedToVariable)
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
                        methodInvocationInfo.StatementContainsCalleeInvocationExpression,
                        syntaxGenerator
                            .LocalDeclarationStatement(calleeMethodSymbol.ReturnType, unusedTemporaryName, inlineSyntaxNode),
                        methodInvocationInfo.StatementContainsCalleeInvocationExpression);
                }
                // Check if parenthesis is needed.
                else if (calleeInvocationSyntaxNode.Parent != null
                     && !syntaxFacts.IsParenthesizedExpression(inlineSyntaxNode)
                     && inlineMethodRefactoringProvider.IsExpressionSyntax(inlineSyntaxNode)
                     && inlineMethodRefactoringProvider.IsExpressionSyntax(calleeInvocationSyntaxNode.Parent)
                     && inlineMethodRefactoringProvider.ShouldCheckTheExpressionPrecedenceInCallee(inlineSyntaxNode))
                {
                    var precedenceOfInlineExpression = precedenceService.GetOperatorPrecedence(inlineSyntaxNode);
                    var precedenceOfInvocation = precedenceService.GetOperatorPrecedence(calleeInvocationSyntaxNode.Parent);
                    if (precedenceOfInlineExpression != 0 && precedenceOfInvocation != 0)
                    {
                        if (precedenceOfInlineExpression < precedenceOfInvocation)
                        {
                            // Example:
                            // void Caller() { int i = Callee() * 3; }
                            // int Callee() => 1 + 2;
                            // Here '1 + 2' has lower precedence then Callee() * 3, so parenthesis is needed.
                            shouldWrapInParenthesis = true;
                        }
                        else if (precedenceOfInlineExpression == precedenceOfInvocation)
                        {
                            // If precedences are equal, do a more carefully check based on the associativity of the expression.
                            shouldWrapInParenthesis = inlineMethodRefactoringProvider.NeedWrapInParenthesisWhenPrecedenceAreEqual(calleeInvocationSyntaxNode);
                        }
                    }
                }

                if (shouldWrapInParenthesis)
                {
                    inlineSyntaxNode = syntaxGenerator.AddParentheses(inlineSyntaxNode);
                }

                return new InlineMethodContext(
                    localDeclarationStatementsNeedInsert,
                    methodInvocationInfo.StatementContainsCalleeInvocationExpression,
                    inlineSyntaxNode,
                    calleeInvocationSyntaxNode);
            }

            private static ImmutableArray<SyntaxNode> GetLocalDeclarationStatementsNeedInsert(
                AbstractInlineMethodRefactoringProvider inlineMethodRefactoringProvider,
                SemanticModel semanticModel,
                SyntaxGenerator syntaxGenerator,
                ImmutableArray<(IParameterSymbol parameterSymbol, ImmutableArray<SyntaxNode> arguments)> parametersNeedGenerateDeclarations,
                ImmutableArray<(IParameterSymbol parameterSymbol, string name)> parametersWithVariableDeclarationArgument,
                ImmutableDictionary<ISymbol, string> renameTable,
                CancellationToken cancellationToken)
                => parametersNeedGenerateDeclarations
                    .Select(parameterAndArguments => CreateLocalDeclarationStatement(inlineMethodRefactoringProvider, semanticModel, syntaxGenerator, renameTable, parameterAndArguments, cancellationToken))
                    .Concat(parametersWithVariableDeclarationArgument
                        .Select(parameterAndName => inlineMethodRefactoringProvider.GenerateLocalDeclarationStatement(parameterAndName.name, parameterAndName.parameterSymbol.Type)))
                    .ToImmutableArray();

            private static SyntaxNode CreateLocalDeclarationStatement(
                AbstractInlineMethodRefactoringProvider inlineMethodRefactoringProvider,
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
                // If the arguments number is one, it could be the single element of the array(an array declaration is needed)
                // or it could an array (like array creation, and array declaration is not need)
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
                AbstractInlineMethodRefactoringProvider inlineMethodRefactoringProvider,
                IMethodSymbol calleeMethodSymbol,
                ImmutableArray<(IParameterSymbol parameterSymbol , SyntaxNode literalExpression)> parametersWithLiteralArgument,
                ImmutableArray<IParameterSymbol> parametersWithDefaultValue,
                SyntaxGenerator syntaxGenerator,
                ImmutableDictionary<ISymbol, string> renameTable)
            {
                // The generics types
                // Example:
                // Before:
                // void Caller() { Callee<int>(); }
                // void Callee<T>() => Print(typeof<T>);
                // After:
                // void Caller() { Print(typeof(int)); }
                // void Callee<T>() => Print(typeof<T>);
                var typeParametersReplacementQuery = calleeMethodSymbol.TypeParameters
                    .Zip(calleeMethodSymbol.TypeArguments,
                        (parameter, argument) => (parameter: (ISymbol)parameter, syntaxNode: inlineMethodRefactoringProvider.GenerateTypeSyntax(argument)));
                var defaultValueReplacementQuery = parametersWithDefaultValue
                    .Select(symbol => (parameter: (ISymbol)symbol, syntaxNode: syntaxGenerator.LiteralExpression(symbol.ExplicitDefaultValue)));
                var literalArgumentReplacementQuery = parametersWithLiteralArgument
                    .Select(parameterAndArgument => (parameter: (ISymbol)parameterAndArgument.parameterSymbol, syntaxNode: parameterAndArgument.literalExpression));

                return renameTable
                    .Select(kvp => (parameter: kvp.Key,
                        syntaxNode: inlineMethodRefactoringProvider.GenerateIdentifierNameSyntaxNode(kvp.Value)))
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
