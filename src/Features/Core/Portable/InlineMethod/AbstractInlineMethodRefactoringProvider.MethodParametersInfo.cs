// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal abstract partial class AbstractInlineMethodRefactoringProvider<TInvocationSyntaxNode, TExpressionSyntax, TArgumentSyntax, TMethodDeclarationSyntax>
        where TInvocationSyntaxNode : SyntaxNode
        where TExpressionSyntax : SyntaxNode
        where TArgumentSyntax : SyntaxNode
        where TMethodDeclarationSyntax : SyntaxNode
    {
        /// <summary>
        /// Information about the callee method parameters to compute <see cref="InlineMethodContext"/>.
        /// </summary>
        private class MethodParametersInfo
        {
            /// <summary>
            /// Parameters map to identifier argument.
            /// </summary>
            public ImmutableArray<(IParameterSymbol parameterSymbol, SyntaxNode identifierSyntaxNode)> ParametersWithIdentifierArgument { get; }

            /// <summary>
            /// Parameters map to variable declaration argument (out var declaration in C#)
            /// </summary>
            public ImmutableArray<(IParameterSymbol parameterSymbol, SyntaxNode variableDeclarationNode)> ParametersWithVariableDeclarationArgument { get; }

            /// <summary>
            /// Parameters map to other expression arguments. Note: params array could maps to multiple arguments.
            /// </summary>
            public ImmutableArray<(IParameterSymbol parameterSymbol, ImmutableArray<SyntaxNode> arguments)> ParametersNeedGenerateDeclarations { get; }

            /// <summary>
            /// Parameters has no argument input and have default value.
            /// </summary>
            public ImmutableArray<IParameterSymbol> ParametersWithDefaultValue { get; }

            /// <summary>
            /// Parameters map to literal expression argument.
            /// </summary>
            public ImmutableArray<(IParameterSymbol parameterSymbol, SyntaxNode literalExpressionSyntaxNode)> ParametersWithLiteralArgument { get; }

            private MethodParametersInfo(
                ImmutableArray<(IParameterSymbol parameterSymbol, SyntaxNode identifierSyntaxNode)> parametersWithIdentifierArgument,
                ImmutableArray<(IParameterSymbol parameterSymbol, SyntaxNode variableDeclarationNode)> parametersWithVariableDeclarationArgument,
                ImmutableArray<(IParameterSymbol parameterSymbol, ImmutableArray<SyntaxNode> arguments)> parametersNeedGenerateDeclarations,
                ImmutableArray<IParameterSymbol> parametersWithDefaultValue,
                ImmutableArray<(IParameterSymbol parameterSymbol, SyntaxNode literalExpressionSyntaxNode)> parametersWithLiteralArgument)
            {
                ParametersWithIdentifierArgument = parametersWithIdentifierArgument;
                ParametersWithVariableDeclarationArgument = parametersWithVariableDeclarationArgument;
                ParametersNeedGenerateDeclarations = parametersNeedGenerateDeclarations;
                ParametersWithDefaultValue = parametersWithDefaultValue;
                ParametersWithLiteralArgument = parametersWithLiteralArgument;
            }

            public static MethodParametersInfo GetMethodParametersInfo2(
                ISyntaxFacts syntaxFacts,
                IInvocationOperation invocationOperation)
            {
                var allArgumentOperations = invocationOperation.Arguments
                    .Select(operation => (argumentOperation: operation,
                        argumentExpressionOperation: operation.Children.FirstOrDefault()))
                    .Where(argumentAndArgumentOperation => argumentAndArgumentOperation.argumentExpressionOperation != null)
                    .ToImmutableArray();

                // 1. Find all the parameter maps to an identifier from caller. After inlining, this identifier would be used to replace the parameter in callee body.
                // For params array, it should be included here if it is accept an array identifier as argument.
                // Example:
                // Before:
                // void Caller(int i, int j, bool[] k)
                // {
                //     Callee(i, j, k);
                // }
                // void Callee(int a, int b, params bool[] c)
                // {
                //     DoSomething(a, b, c);
                // }
                // After:
                // void Caller(int i, int j, bool[] k)
                // {
                //     DoSomething(i, j, k);
                // }
                // void Callee(int a, int b, params bool[] c)
                // {
                //     DoSomething(a, b, c);
                // }
                var operationsWithIdentifier = allArgumentOperations
                    .WhereAsArray(argumentAndArgumentOperation =>
                        syntaxFacts.IsIdentifierName(argumentAndArgumentOperation.argumentExpressionOperation.Syntax));

                // 2. Find all the declaration arguments (e.g. out var declaration in C#).
                // After inlining, an declaration needs to be put before the invocation. And also use the declared identifier to replace the mapping parameter in callee.
                // Example:
                // Before:
                // void Caller()
                // {
                //     Callee(out var x);
                // }
                // void Callee(out int i) => i = 100;
                //
                // After:
                // void Caller()
                // {
                //     int x;
                //     x = 100;
                // }
                // void Callee(out int i) => i = 100;
                var operationsWithVariableDeclaration = allArgumentOperations
                    .WhereAsArray(argumentAndArgumentOperation =>
                        syntaxFacts.IsDeclarationExpression(argumentAndArgumentOperation.argumentExpressionOperation.Syntax));

                // 3. Find the literal arguments, and the mapping parameter will be replaced by that literal expression
                // Example:
                // Before:
                // void Caller(int k)
                // {
                //     Callee(1, k);
                // }
                // void Callee(int i, int j)
                // {
                //     DoSomething(i, k);
                // }
                // After:
                // void Caller(int k)
                // {
                //     DoSomething(1, k);
                // }
                // void Callee(int i, int j)
                // {
                //     DoSomething(i, j);
                // }
                var operationsWithLiteralArgument = allArgumentOperations
                    .WhereAsArray(argumentAndArgumentOperation =>
                        syntaxFacts.IsLiteralExpression(argumentAndArgumentOperation.argumentExpressionOperation.Syntax));

                // 4. Find the default value parameters. Similarly to 3, they should be replaced by the default value.
                // Example:
                // Before:
                // void Caller(int k)
                // {
                //     Callee();
                // }
                // void Callee(int i = 1, int j = 2)
                // {
                //     DoSomething(i, k);
                // }
                // After:
                // void Caller(int k)
                // {
                //     DoSomething(1, 2);
                // }
                // void Callee(int i = 1, int j = 2)
                // {
                //     DoSomething(i, j);
                // }
                var operationsWithDefaultValue = allArgumentOperations
                    .WhereAsArray(argumentAndArgumentOperation =>
                        argumentAndArgumentOperation.argumentOperation.ArgumentKind == ArgumentKind.DefaultValue);

                // 5. All the remaining arguments, which might includes method call and a lot of other expressions.
                // Generate a declaration in the caller.
                // Example:
                // Before:
                // void Caller(bool x)
                // {
                //     Callee(Foo(), x ? Foo() : Bar())
                // }
                // void Callee(int a, int b)
                // {
                //     DoSomething(a, b);
                // }
                // After:
                // void Caller(bool x)
                // {
                //     int a = Foo();
                //     int b = x ? Foo() : Bar();
                //     DoSomething(a, b);
                // }
                // void Callee(int a, int b)
                // {
                //     DoSomething(a, b);
                // }
                var parametersNeedGenerateDeclarations = allArgumentOperations
                    .RemoveRange(operationsWithIdentifier)
                    .RemoveRange(operationsWithVariableDeclaration)
                    .RemoveRange(operationsWithLiteralArgument)
                    .RemoveRange(operationsWithDefaultValue)
                    .SelectAsArray(argumentAndArgumentOperation =>
                        (argumentAndArgumentOperation.argumentOperation.Parameter,
                            GetArgumentSyntaxFromOperation(argumentAndArgumentOperation)));

                return new MethodParametersInfo(
                    operationsWithIdentifier
                        .SelectAsArray(argumentAndArgumentOperation => (
                            argumentAndArgumentOperation.argumentOperation.Parameter,
                            argumentAndArgumentOperation.argumentExpressionOperation.Syntax)),
                    operationsWithVariableDeclaration
                        .SelectAsArray(argumentAndArgumentOperation => (
                            argumentAndArgumentOperation.argumentOperation.Parameter,
                            argumentAndArgumentOperation.argumentExpressionOperation.Syntax)),
                    parametersNeedGenerateDeclarations,
                    operationsWithDefaultValue.SelectAsArray(argumentAndArgumentOperation =>
                        argumentAndArgumentOperation.argumentOperation.Parameter),
                    operationsWithLiteralArgument
                        .SelectAsArray(argumentAndArgumentOperation => (
                            argumentAndArgumentOperation.argumentOperation.Parameter,
                            argumentAndArgumentOperation.argumentExpressionOperation.Syntax)));
            }

            private static ImmutableArray<SyntaxNode> GetArgumentSyntaxFromOperation(
                (IArgumentOperation argumentOperation, IOperation argumentExpressionOperation) argumentAndArgumentOperation)
            {
                var (argumentOperation, argumentExpressionOperation) = argumentAndArgumentOperation;
                // if this argument is a param array & the array creation operation is implicitly generated,
                // it means it is in this format:
                // void caller() { Callee(1, 2, 3); }
                // void Callee(params int[] x) { }
                // Collect each of these arguments.
                // Note: it could be empty.
                if (argumentOperation.ArgumentKind == ArgumentKind.ParamArray
                    && argumentExpressionOperation is IArrayCreationOperation arrayCreationOperation
                    && argumentOperation.IsImplicit)
                {
                    return arrayCreationOperation.Initializer.ElementValues.SelectAsArray(value => value.Syntax);
                }

                return ImmutableArray.Create(argumentExpressionOperation.Syntax);
            }
        }
    }
}
