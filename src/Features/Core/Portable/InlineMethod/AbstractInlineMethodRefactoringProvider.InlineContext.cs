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
        protected abstract SyntaxNode GenerateArrayInitializerExpression(ImmutableArray<SyntaxNode> arguments);
        protected abstract SyntaxNode GenerateLocalDeclarationStatementWithRightHandExpression(string identifierTokenName, ITypeSymbol type, SyntaxNode expression);
        protected abstract SyntaxNode GenerateLocalDeclarationStatement(string identifierTokenName, ITypeSymbol type);
        protected abstract SyntaxNode GenerateIdentifierNameSyntaxNode(string name);
        protected abstract bool IsEmbeddedStatementOwner(SyntaxNode syntaxNode);
        protected abstract bool IsArrayCreationExpressionOrImplicitArrayCreationExpression(SyntaxNode syntaxNode);

        private class InlineMethodContext
        {
            /// <summary>
            /// Statements should be inserted before the <see cref="StatementInvokesCallee"/>.
            /// </summary>
            public ImmutableArray<SyntaxNode> StatementsShouldBeInserted { get; }

            /// <summary>
            /// Parameters should be replaced by literal.
            /// </summary>
            public ImmutableArray<(IParameterSymbol parameterSymbol, SyntaxNode literalExpressionSyntaxNode)> LiteralArgumentsInfo { get; }

            /// <summary>
            /// Tracks all the local variables & parameters of callee needed to be replaced by another identifier.
            /// </summary>
            public ImmutableDictionary<ISymbol, SyntaxNode> ReplacementTable { get; }

            /// <summary>
            /// Should a temp variable be generated for the inline method body.
            /// </summary>
            public bool ShouldGenerateTempVariableForReturnValue { get; }

            /// <summary>
            /// Statement invokes the callee. All the generated declarations should be put before this node.
            /// </summary>
            public SyntaxNode StatementInvokesCallee { get; }

            private InlineMethodContext(
                ImmutableArray<SyntaxNode> statementsShouldBeInserted,
                ImmutableArray<(IParameterSymbol parameterSymbol,
                SyntaxNode literalExpressionSyntaxNode)> literalArgumentsInfo,
                ImmutableDictionary<ISymbol, SyntaxNode> replacementTable,
                bool shouldGenerateTempVariableForReturnValue,
                SyntaxNode statementInvokesCallee)
            {
                StatementsShouldBeInserted = statementsShouldBeInserted;
                LiteralArgumentsInfo = literalArgumentsInfo;
                ReplacementTable = replacementTable;
                this.ShouldGenerateTempVariableForReturnValue = shouldGenerateTempVariableForReturnValue;
                StatementInvokesCallee = statementInvokesCallee;
            }

            public static InlineMethodContext GetInlineContext(
                AbstractInlineMethodRefactoringProvider service,
                ISyntaxFacts syntaxFacts,
                SemanticModel semanticModel,
                SyntaxNode calleeInvocationSyntaxNode,
                IMethodSymbol calleeMethodSymbol,
                SyntaxNode calleeMethodDeclarationSyntaxNode,
                CancellationToken cancellationToken)
            {
                var allArguments = syntaxFacts.GetArgumentsOfInvocationExpression(calleeInvocationSyntaxNode);
                var argumentsInfo = allArguments
                    .Select(argument =>
                    {
                        var argumentExpression = syntaxFacts.GetExpressionOfArgument(argument);
                        return (
                            parameterSymbol: service.GetParameterSymbol(semanticModel, argument, cancellationToken),
                            argumentExpression: argumentExpression,
                            argumentSymbol: TryGetBestMatchSymbol(semanticModel, argumentExpression, cancellationToken));
                    })
                    // parameter symbol could be null if there is error in the code
                    .Where(argumentAndParameterSymbol =>
                        argumentAndParameterSymbol.parameterSymbol != null &&
                        !argumentAndParameterSymbol.parameterSymbol.IsDiscard)
                    .ToImmutableArray();

                // Check if there is param array because it is quite special
                var paramArguments = argumentsInfo
                    .Where(info => info.parameterSymbol!.IsParams)
                    .ToImmutableArray();
                // Two cases:
                // 1. If there is only one input argument array
                // 2. Arguments are passed separately and they map to one parameter.
                var isParamArrayHasOneIdentiferArgument =
                     paramArguments.Length == 1
                     && semanticModel.GetTypeInfo(paramArguments[0].argumentExpression, cancellationToken).Type.IsArrayType()
                     && syntaxFacts.IsIdentifierName(paramArguments[0].argumentExpression);
                argumentsInfo = argumentsInfo
                    .Where(info => !info.parameterSymbol!.IsParams)
                    .ToImmutableArray();

                // Find all the parameters should be replaced by literal.
                //     I. Literal argument
                var literalArguments = argumentsInfo
                    .Where(info => syntaxFacts.IsLiteralExpression(info.argumentExpression))
                    .Select(info => (info.parameterSymbol, info.argumentExpression))
                    .ToImmutableArray();

                //    II. Parameters with default value but does not any argument.
                var defaultValueParametersQuery =
                    calleeMethodSymbol.Parameters
                        .RemoveRange(argumentsInfo.Select(info => info.parameterSymbol!))
                        .Where(parameterSymbol => parameterSymbol.HasExplicitDefaultValue)
                        .Select(parameterSymbol => (parameterSymbol,
                            service.GenerateLiteralExpression(parameterSymbol.Type,
                                parameterSymbol.ExplicitDefaultValue)));
                var literalArgumentsInfo = literalArguments.Concat(defaultValueParametersQuery).ToImmutableArray();

                // Find identifier arguments(e.g. Field, local variable, property, method name..)
                var identifierArguments = argumentsInfo
                    .Where(info => syntaxFacts.IsIdentifierName(info.argumentExpression))
                    .ToImmutableArray();
                if (isParamArrayHasOneIdentiferArgument)
                {
                    // If param array has only one argument and it is an identifier, it is the same as a parameter with identifier input.
                    identifierArguments = identifierArguments.Concat(paramArguments[0]);
                }

                // Find the expression argument (Method call, object creation..)
                // TODO: Should add all the scenarios here?
                var expressionArguments = argumentsInfo
                    .Where(info => syntaxFacts.IsInvocationExpression(info.argumentExpression) || syntaxFacts.IsObjectCreationExpression(info.argumentExpression))
                    .ToImmutableArray();
                if (!paramArguments.IsEmpty && !isParamArrayHasOneIdentiferArgument)
                {
                    var arrayInitializer = paramArguments.Length == 1 && service.IsArrayCreationExpressionOrImplicitArrayCreationExpression(paramArguments[0].argumentExpression)
                        ? paramArguments[0].argumentExpression
                        : service.GenerateArrayInitializerExpression(paramArguments.Select(info => info.argumentExpression).ToImmutableArray());
                    expressionArguments = expressionArguments.Concat((paramArguments[0].parameterSymbol, arrayInitializer, null));
                }

                // Find the out variable declaration argument
                // Out variable declaration is special because after inlining a variable declaration needs to be generated.
                var variableDeclarationArguments = argumentsInfo
                    .Where(info => syntaxFacts.IsDeclarationExpression(info.argumentExpression) && info.argumentSymbol != null)
                    .ToImmutableArray();

                // If the callee method has a return value, and it is discarded in the caller. Then a temp variable should be generated
                // Example:
                // Before:
                // void Caller()
                // { Callee() }
                // int Callee() { return 1;}
                // After:
                // void Caller()
                // { int tmp = 1; }
                // int Callee() { return 1;}
                var shouldDeclareTempVariableForReturnValue = !calleeMethodSymbol.ReturnsVoid && service.IsExpressionStatement(calleeInvocationSyntaxNode);

                // Find statement invokes the callee. And it could be
                //  I. Callee is invoked lonely as a statement for declaration
                //  II. Callee is invoked as a part of the parameter list.
                // When new declaration is created, it should be put before this node.
                var statementInvokesCallee = GetInvokingStatement(syntaxFacts, service, calleeInvocationSyntaxNode);

                // Compute the rename table for each parameter & local variable in the callee. Considering the following operations
                // I. Replace parameter by the identifier of argument. Identifiers of caller are introduced to callee. It could cause naming conflict in the callee.
                // II. Generate a declaration in the caller for the expression argument. Identifiers of callee are introduced to caller. It could cause naming conflict in the caller.
                var renameTable = ComputeRenameTable(
                    calleeInvocationSyntaxNode,
                    semanticModel,
                    calleeMethodDeclarationSyntaxNode,
                    identifierArguments,
                    expressionArguments,
                    variableDeclarationArguments,
                    cancellationToken);

                var replacementTable = renameTable.ToImmutableDictionary(
                    keySelector: kvp => kvp.Key,
                    elementSelector: kvp => service.GenerateIdentifierNameSyntaxNode(kvp.Value));

                // Generate the declaration statements needed.
                var statementsShouldBeInserted = expressionArguments
                    .Select(info =>
                    {
                        var (parameterSymbol, expression, _) = info;
                        if (renameTable.TryGetValue(parameterSymbol!, out var newName))
                        {
                            return service.GenerateLocalDeclarationStatementWithRightHandExpression(newName, parameterSymbol!.Type, expression);
                        }

                        return service.GenerateLocalDeclarationStatementWithRightHandExpression(parameterSymbol!.Name, parameterSymbol!.Type, expression);
                    })
                    .Concat(variableDeclarationArguments
                        .Select(info => service.GenerateLocalDeclarationStatement(info.argumentSymbol!.Name, info.parameterSymbol!.Type)))
                    .ToImmutableArray();


                return new InlineMethodContext(
                    statementsShouldBeInserted,
                    literalArgumentsInfo,
                    replacementTable,
                    shouldDeclareTempVariableForReturnValue,
                    statementInvokesCallee);
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

            private static ImmutableDictionary<ISymbol, string> ComputeRenameTable(
                SyntaxNode calleeInvocationSyntaxNode,
                SemanticModel semanticModel,
                SyntaxNode calleeDeclarationSyntaxNode,
                ImmutableArray<(IParameterSymbol parameterSymbol, SyntaxNode identifierNameSyntaxNode, ISymbol? argumentSymbol)> identifierArguments,
                ImmutableArray<(IParameterSymbol parameterSymbol, SyntaxNode expressionSyntaxNode, ISymbol? argumentSymbol)> expressionArguments,
                ImmutableArray<(IParameterSymbol parameterSymbol, SyntaxNode declarationSyntaxNode, ISymbol? argumentSymbol)> variableDeclarationArguments,
                CancellationToken cancellationToken)
            {
                var operationVisitor = new VariableDeclaratorOperationVisitor(cancellationToken);
                var calleeOperation = semanticModel.GetOperation(calleeDeclarationSyntaxNode, cancellationToken);
                var invocationSpanEnd = calleeInvocationSyntaxNode.Span.End;
                var localSymbolNamesOfCaller = semanticModel.LookupSymbols(invocationSpanEnd)
                    .Where(symbol => !symbol.IsInaccessibleLocal(invocationSpanEnd))
                    .Select(symbol => symbol.Name)
                    .ToImmutableHashSet();

                var parametersReplacedByIdentifier = identifierArguments.Concat(variableDeclarationArguments)
                    .Where(info => info.argumentSymbol != null)
                    .Select(info => (info.parameterSymbol, identifierName: info.argumentSymbol!.Name))
                    .ToImmutableArray();

                var renameTable = parametersReplacedByIdentifier.ToDictionary(
                    keySelector: parameterAndIdentifier => (ISymbol)parameterAndIdentifier.parameterSymbol,
                    elementSelector: parameterAndIdentifier => parameterAndIdentifier.identifierName) ;

                // 1. Make sure after replacing the parameters with the identifier from Caller, there is no variable conflict in callee
                var calleeParameterNames = parametersReplacedByIdentifier
                    .Select(parameterAndArgument => parameterAndArgument.identifierName).ToSet();

                var localSymbolsOfCallee = operationVisitor.FindAllLocalSymbols(calleeOperation);

                foreach (var localSymbol in localSymbolsOfCallee)
                {
                    var localSymbolName = localSymbol.Name;
                    while (calleeParameterNames.Contains(localSymbolName))
                    {
                        localSymbolName = GenerateNewName(localSymbolName);
                    }

                    if (!localSymbolName.Equals(localSymbol.Name))
                    {
                        renameTable[localSymbol] = localSymbolName;
                        calleeParameterNames.Remove(localSymbol.Name);
                        calleeParameterNames.Add(localSymbolName);
                    }
                }

                // 2. Make sure no variable conflict after the parameter is moved to caller
                foreach (var (parameterSymbol, _, _) in expressionArguments)
                {
                    var parameterName = parameterSymbol.Name;
                    while (localSymbolNamesOfCaller.Contains(parameterName) || calleeParameterNames.Contains(parameterName))
                    {
                        parameterName = GenerateNewName(parameterName);
                    }

                    if (!parameterName.Equals(parameterSymbol.Name))
                    {
                        renameTable[parameterSymbol] = parameterName;
                        calleeParameterNames.Remove(parameterName);
                        calleeParameterNames.Add(parameterName);
                    }
                }

                return renameTable.ToImmutableDictionary();
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
