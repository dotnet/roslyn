// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal partial class AbstractInlineMethodRefactoringProvider
    {
        protected abstract IParameterSymbol? GetParameterSymbol(SemanticModel semanticModel, SyntaxNode argumentSyntaxNode, CancellationToken cancellationToken);
        protected abstract bool IsExpressionStatement(SyntaxNode syntaxNode);
        protected abstract SyntaxNode GenerateLiteralExpression(ITypeSymbol typeSymbol, object? value);
        protected abstract bool IsExpressionSyntax(SyntaxNode syntaxNode);
        protected abstract string GetIdentifierTokenTextFromIdentifierNameSyntax(SyntaxNode syntaxNode);
        protected abstract string GetSingleVariableNameFromDeclarationExpression(SyntaxNode syntaxNode);
        protected abstract SyntaxNode GenerateArrayInitializerExpression(ImmutableArray<SyntaxNode> arguments);
        protected abstract SyntaxNode GenerateLocalDeclarationStatementWithRightHandExpression(string identifierTokenName, ITypeSymbol type, SyntaxNode expression);
        protected abstract SyntaxNode GenerateLocalDeclarationStatement(string identifierTokenName, ITypeSymbol type);
        protected abstract SyntaxNode GenerateIdentifierNameSyntaxNode(string name);
        protected abstract SyntaxNode GenerateTypeSyntax(ITypeSymbol symbol);
        protected abstract bool IsEmbeddedStatementOwner(SyntaxNode syntaxNode);

        private class InlineMethodContext
        {
            /// <summary>
            /// Statements should be inserted before the <see cref="StatementInvokesCallee"/>.
            /// </summary>
            public ImmutableArray<SyntaxNode> StatementsNeedInsert { get; }

            /// <summary>
            /// Tracks all the symbol needs to be replaced by a syntax node.
            /// </summary>
            public ImmutableDictionary<ISymbol, SyntaxNode> ReplacementTable { get; }

            /// <summary>
            /// Statement invokes the callee. All the generated declarations should be put before this node.
            /// </summary>
            public SyntaxNode StatementInvokesCallee { get; }

            private InlineMethodContext(
                ImmutableArray<SyntaxNode> statementsNeedInsert,
                ImmutableDictionary<ISymbol, SyntaxNode> replacementTable,
                SyntaxNode statementInvokesCallee)
            {
                StatementsNeedInsert = statementsNeedInsert;
                ReplacementTable = replacementTable;
                StatementInvokesCallee = statementInvokesCallee;
            }

            public static InlineMethodContext GetInlineContext2(
                AbstractInlineMethodRefactoringProvider service,
                ISyntaxFacts syntaxFacts,
                SemanticModel semanticModel,
                SyntaxNode calleeInvocationSyntaxNode,
                IMethodSymbol calleeMethodSymbol,
                SyntaxNode calleeMethodDeclarationSyntaxNode,
                CancellationToken cancellationToken)
            {
                var allArguments = syntaxFacts.GetArgumentsOfInvocationExpression(calleeInvocationSyntaxNode);
                var allParameterSymbols = calleeMethodSymbol.Parameters;

                var statementInvokesCallee = GetInvokingStatement(syntaxFacts, service, calleeInvocationSyntaxNode);
                if (allArguments.IsEmpty() && allParameterSymbols.Length == 1 && allParameterSymbols[0].IsParams)
                {
                    var renameTable = ComputeRenameTable2(
                        calleeInvocationSyntaxNode,
                        semanticModel,
                        calleeMethodDeclarationSyntaxNode,
                        ImmutableArray<(IParameterSymbol parameterSymbol, string name)>.Empty,
                        ImmutableArray.Create(allParameterSymbols[0]),
                        ImmutableArray<(IParameterSymbol parameterSymbol, string name)>.Empty,
                        cancellationToken);
                    var arrayInitializer = service.GenerateArrayInitializerExpression(ImmutableArray<SyntaxNode>.Empty);
                    return new InlineMethodContext(
                        ImmutableArray.Create(arrayInitializer),
                        ImmutableDictionary<ISymbol, SyntaxNode>.Empty,
                        statementInvokesCallee);
                }
                else
                {
                    var parameterSymbolToArgumentGroupings = allArguments
                        .Select(argument => (
                            parameterSymbol: service.GetParameterSymbol(semanticModel, argument, cancellationToken),
                            argumentExpression: syntaxFacts.GetExpressionOfArgument(argument)))
                        .Where(parameterAndArgument =>
                            parameterAndArgument.parameterSymbol != null
                            && !parameterAndArgument.parameterSymbol.IsDiscard)
                        .GroupBy(keySelector: parameterAndArgument => parameterAndArgument.parameterSymbol!,
                            elementSelector: parameterAndArgument => parameterAndArgument.argumentExpression)
                        .ToImmutableArray();

                    var allParametersWithArgumentGroupings = parameterSymbolToArgumentGroupings
                        .Select(g => g.Key).ToImmutableArray();

                    var parametersWithIdentifierArgumentGroupings = parameterSymbolToArgumentGroupings
                        .Where(grouping =>
                            ParameterWithIdentifierArgumentFilter(syntaxFacts, semanticModel, grouping, cancellationToken))
                        .ToImmutableArray();

                    var parametersWithVariableDeclarationArgumentGroupings = parameterSymbolToArgumentGroupings
                        .Where(grouping => ParameterWithVariableDeclarationArgumentFilter(syntaxFacts, grouping))
                        .ToImmutableArray();

                    var parametersWithVariableDeclarationArgumentToName = parametersWithVariableDeclarationArgumentGroupings
                        .SelectAsArray(grouping => (grouping.Key, service.GetSingleVariableNameFromDeclarationExpression(grouping.Single())));

                    var parametersWithLiteralArgumentGroupings = parameterSymbolToArgumentGroupings
                        .Where(grouping => ParameterWithLiteralArgumentFilter(syntaxFacts, grouping))
                        .ToImmutableArray();

                    var parametersNeedGenerateDeclarations = parameterSymbolToArgumentGroupings
                        .RemoveRange(parametersWithIdentifierArgumentGroupings)
                        .RemoveRange(parametersWithVariableDeclarationArgumentGroupings)
                        .RemoveRange(parametersWithLiteralArgumentGroupings)
                        .Where(grouping => ParameterNeedsGenerateDeclarationFilter(service, syntaxFacts, semanticModel, grouping, cancellationToken))
                        .ToImmutableArray();

                    var parametersWithDefaultValue = calleeMethodSymbol.Parameters
                        .RemoveRange(allParametersWithArgumentGroupings)
                        .Where(parameterSymbol => parameterSymbol.HasExplicitDefaultValue)
                        .ToImmutableArray();

                    var renameTable = ComputeRenameTable2(
                        calleeInvocationSyntaxNode,
                        semanticModel,
                        calleeMethodDeclarationSyntaxNode,
                        parametersWithIdentifierArgumentGroupings.SelectAsArray(grouping => (grouping.Key, service.GetIdentifierTokenTextFromIdentifierNameSyntax(grouping.Single()))),
                        parametersNeedGenerateDeclarations.SelectAsArray(grouping => grouping.Key),
                        parametersWithVariableDeclarationArgumentToName,
                        cancellationToken);

                    var replacementTable = ComputeReplacementTable(
                        service,
                        calleeMethodSymbol,
                        parametersWithLiteralArgumentGroupings,
                        parametersWithDefaultValue,
                        renameTable);

                    var statementsNeedInsert = ComputeStatementsNeedInsert(
                        service,
                        semanticModel,
                        parametersNeedGenerateDeclarations,
                        parametersWithVariableDeclarationArgumentToName,
                        renameTable,
                        cancellationToken);

                    return new InlineMethodContext(statementsNeedInsert, replacementTable, statementInvokesCallee);
                }
            }

            private static bool ParameterNeedsGenerateDeclarationFilter(
                AbstractInlineMethodRefactoringProvider service,
                ISyntaxFacts syntaxFacts,
                SemanticModel semanticModel,
                IGrouping<IParameterSymbol, SyntaxNode> parameterAndArguments,
                CancellationToken cancellationToken)
            {
                var (parameterSymbol, arguments) = parameterAndArguments;
                var argumentsArray = arguments.ToImmutableArray();
                if (!parameterSymbol.IsParams && argumentsArray.Length == 1)
                {
                    var argument = argumentsArray[0];
                    // Is this check too wide?
                    return service.IsExpressionSyntax(argument);
                }

                // Params array is special, if there are multiple arguments, then they needs to be put into a separate array.
                if (parameterSymbol.IsParams)
                {
                    if (argumentsArray.Length != 1)
                    {
                        // If there is no argument or multiple arguments, a array initializer expression should be
                        // put into caller
                        return true;
                    }
                    else
                    {
                        // If there is only one argument, it has 3 cases
                        // 1. This argument is an array (identifier)
                        // 2. This argument is an array (array creation expression)
                        // 3. This argument is the single element in the array. (identifier or literal)
                        // Generate the declaration for case 2 & 3
                        var argument = argumentsArray[0];
                        var typeOfElement = (IArrayTypeSymbol)parameterSymbol.Type;
                        var typeOfArgument = semanticModel.GetTypeInfo(argument, cancellationToken).Type;
                        return !(typeOfElement.Equals(typeOfArgument) && syntaxFacts.IsIdentifierName(argument));
                    }
                }

                return false;
            }

            private static bool ParameterWithLiteralArgumentFilter(
                ISyntaxFacts syntaxFacts,
                IGrouping<IParameterSymbol, SyntaxNode> parameterAndArguments)
            {
                var (parameterSymbol, arguments) = parameterAndArguments;
                var argumentsArray = arguments.ToImmutableArray();
                if (!parameterSymbol.IsParams && argumentsArray.Length == 1)
                {
                    return syntaxFacts.IsLiteralExpression(argumentsArray[0]);
                }

                return false;
            }

            private static bool ParameterWithVariableDeclarationArgumentFilter(
                ISyntaxFacts syntaxFacts,
                IGrouping<IParameterSymbol, SyntaxNode> parameterAndArguments)
            {
                var (_, arguments) = parameterAndArguments;
                var argumentsArray = arguments.ToImmutableArray();
                return argumentsArray.Length == 1 && syntaxFacts.IsDeclarationExpression(argumentsArray[0]);
            }

            private static bool ParameterWithIdentifierArgumentFilter(
                ISyntaxFacts syntaxFacts,
                SemanticModel semanticModel,
                IGrouping<IParameterSymbol, SyntaxNode> parameterAndArguments,
                CancellationToken cancellationToken)
            {
                var (parameterSymbol, arguments) = parameterAndArguments;
                var argumentsArray = arguments.ToImmutableArray();
                if (argumentsArray.Length == 1)
                {
                    var argument = argumentsArray[0];
                    var isIdentifier = syntaxFacts.IsIdentifierName(argument);
                    if (parameterSymbol.IsParams && isIdentifier)
                    {
                        // If there is only one identifier argument,
                        // it could be an identifier to an array or single element.
                        // Only treat the array case as 'identifier'.
                        var typeOfParameter = (IArrayTypeSymbol)parameterSymbol.Type;
                        var typeOfArgument = semanticModel.GetTypeInfo(argument, cancellationToken).Type;
                        return typeOfParameter.Equals(typeOfArgument);
                    }

                    return isIdentifier;
                }

                return false;
            }

            private static ImmutableArray<SyntaxNode> ComputeStatementsNeedInsert(
                AbstractInlineMethodRefactoringProvider service,
                SemanticModel semanticModel,
                ImmutableArray<IGrouping<IParameterSymbol, SyntaxNode>> parametersNeedGenerateDeclarations,
                ImmutableArray<(IParameterSymbol parameterSymbol, string name)> parametersWithVariableDeclarationArgument,
                ImmutableDictionary<ISymbol, string> renameTable,
                CancellationToken cancellationToken)
                => parametersNeedGenerateDeclarations
                    .Select(grouping => CreateLocalDeclarationStatement(service, semanticModel, renameTable, grouping, cancellationToken))
                    .Concat(parametersWithVariableDeclarationArgument
                        .Select(grouping => service.GenerateLocalDeclarationStatement(grouping.name, grouping.parameterSymbol.Type)))
                    .ToImmutableArray();

            private static SyntaxNode CreateLocalDeclarationStatement(
                AbstractInlineMethodRefactoringProvider service,
                SemanticModel semanticModel,
                ImmutableDictionary<ISymbol, string> renameTable,
                IGrouping<IParameterSymbol, SyntaxNode> parameterAndArguments,
                CancellationToken cancellationToken)
            {
                var (parameterSymbol, arguments) = parameterAndArguments;
                var name = renameTable.ContainsKey(parameterSymbol) ? renameTable[parameterSymbol] : parameterSymbol.Name;
                var argumentArray = arguments.ToImmutableArray();
                var generateArrayInitializerExpression = parameterSymbol.IsParams
                // If arguments number is not one, it means this paramsArray maps to multiple arguments
                    && (argumentArray.Length != 1
                // If the arguments number is one, it could be the single element of the array or it could an array.
                        || (argumentArray.Length == 1 && !parameterSymbol.Type.Equals(semanticModel.GetTypeInfo(argumentArray[0], cancellationToken).Type)));

                if (generateArrayInitializerExpression)
                {
                    return service.GenerateLocalDeclarationStatementWithRightHandExpression(
                        name,
                        parameterSymbol.Type,
                        service.GenerateArrayInitializerExpression(argumentArray));
                }
                else
                {
                    return service.GenerateLocalDeclarationStatementWithRightHandExpression(
                        name,
                        parameterSymbol.Type,
                        argumentArray[0]);
                }
            }

            private static ImmutableDictionary<ISymbol, SyntaxNode> ComputeReplacementTable(
                AbstractInlineMethodRefactoringProvider service,
                IMethodSymbol calleeMethodSymbol,
                ImmutableArray<IGrouping<IParameterSymbol, SyntaxNode>> parametersWithLiteralArgument,
                ImmutableArray<IParameterSymbol> parametersWithDefaultValue,
                ImmutableDictionary<ISymbol, string> renameTable)
            {
                var typeParametersReplacementQuery = calleeMethodSymbol.TypeParameters
                    .Zip(calleeMethodSymbol.TypeArguments,
                        (parameter, argument) => (parameter: (ISymbol)parameter, syntaxNode: service.GenerateTypeSyntax(argument)));
                var defaultValueReplacementQuery = parametersWithDefaultValue
                    .Select(symbol => (parameter: (ISymbol)symbol, syntaxNode: service.GenerateLiteralExpression(symbol.Type, symbol.ExplicitDefaultValue)));

                var literalArgumentReplacementQuery = parametersWithLiteralArgument
                    .Select(grouping => (parameter: (ISymbol)grouping.Key, syntaxNode: grouping.Single()));

                return renameTable
                    .Select(kvp => (parameter: kvp.Key,
                        syntaxNode: service.GenerateIdentifierNameSyntaxNode(kvp.Value)))
                    .Concat(typeParametersReplacementQuery)
                    .Concat(defaultValueReplacementQuery)
                    .Concat(literalArgumentReplacementQuery)
                    .ToImmutableDictionary(tuple => tuple.parameter, tuple => tuple.syntaxNode);
            }

            private static ImmutableDictionary<ISymbol, string> ComputeRenameTable2(
                SyntaxNode calleeInvocationSyntaxNode,
                SemanticModel semanticModel,
                SyntaxNode calleeDeclarationSyntaxNode,
                ImmutableArray<(IParameterSymbol parameterSymbol, string name)> identifierArguments,
                ImmutableArray<IParameterSymbol> parametersNeedGenerateDeclaration,
                ImmutableArray<(IParameterSymbol parameterSymbol, string name)> variableDeclarationArguments,
                CancellationToken cancellationToken)
            {
                var allSymbolsOfCaller = semanticModel.LookupSymbols(calleeInvocationSyntaxNode.Span.End);
                var inUsedNamesOfCaller = allSymbolsOfCaller
                    .Select(symbol => symbol.Name)
                    .ToImmutableHashSet();
                var staticAndFieldNamesInClass = allSymbolsOfCaller
                    .Where(symbol => symbol.IsStatic || symbol.IsKind(SymbolKind.Field))
                    .Select(symbol => symbol.Name)
                    .ToImmutableHashSet();

                var operationVisitor = new VariableDeclaratorOperationVisitor(cancellationToken);
                var calleeOperation = semanticModel.GetOperation(calleeDeclarationSyntaxNode, cancellationToken);
                var localSymbolsOfCallee = operationVisitor.FindAllLocalSymbols(calleeOperation);
                var inUsedNamesOfCallee = staticAndFieldNamesInClass
                    .Concat(localSymbolsOfCallee.Select(symbol => symbol.Name))
                    .Concat(identifierArguments.Select(parameterAndName => parameterAndName.name))
                    .Concat(variableDeclarationArguments.Select(parameterAndName => parameterAndName.name))
                    .ToImmutableHashSet();

                var renameTable = identifierArguments.Concat(variableDeclarationArguments)
                    .ToDictionary(parameterAndName => (ISymbol)parameterAndName.parameterSymbol,
                        parameterAndName => parameterAndName.name);

                // 1. Make sure after replacing the parameters with the identifier from Caller, there is no variable conflict in callee
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

                // 2. Make sure no variable conflict after the parameter is moved to caller
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

            private static SyntaxNode GetInvokingStatement(
                ISyntaxFacts syntaxFacts, AbstractInlineMethodRefactoringProvider service, SyntaxNode syntaxNode)
            {
                for (var node = syntaxNode; node != null; node = node!.Parent)
                {
                    // TODO: Is there anything missed?
                    if (node != null && (
                        syntaxFacts.IsLocalDeclarationStatement(node)
                        || service.IsEmbeddedStatementOwner(syntaxNode)
                        || syntaxFacts.IsExpressionStatement(node)))
                    {
                        return node;
                    }
                }

                return syntaxNode;
            }

            /// <summary>
            /// Generate a new identifier name. If <param name="identifierName"/> has a number suffix,
            /// increase it by 1. Otherwise, append 1 to it.
            /// </summary>
            private static string GenerateNewName(string identifierName)
            {
                var stack = new Stack<char>();
                for (var i = identifierName.Length - 1; i >= 0; i--)
                {
                    var currentCharacter = identifierName[i];
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
                return identifierName.Substring(0, identifierName.Length - stack.Count) + suffixNumber;
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
