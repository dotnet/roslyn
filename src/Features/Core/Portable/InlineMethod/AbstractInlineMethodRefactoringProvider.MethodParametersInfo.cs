// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal abstract partial class AbstractInlineMethodRefactoringProvider<TInvocationNode, TExpression, TArgumentSyntax, TMethodDeclarationSyntax, TIdentifierName>
        where TInvocationNode : SyntaxNode
        where TExpression : SyntaxNode
        where TArgumentSyntax : SyntaxNode
        where TMethodDeclarationSyntax : SyntaxNode
        where TIdentifierName : SyntaxNode
    {
        /// <summary>
        /// Information about the callee method parameters to compute <see cref="InlineMethodContext"/>.
        /// </summary>
        private class MethodParametersInfo
        {
            /// <summary>
            /// Parameters map to identifier argument. The identifier from Caller will be used to replace
            /// the mapping callee's parameter.
            /// Example:
            /// Before:
            /// void Caller(int i, int j, bool[] k)
            /// {
            ///     Callee(i, j, k);
            /// }
            /// void Callee(int a, int b, params bool[] c)
            /// {
            ///     DoSomething(a, b, c);
            /// }
            /// After:
            /// void Caller(int i, int j, bool[] k)
            /// {
            ///     DoSomething(i, j, k);
            /// }
            /// void Callee(int a, int b, params bool[] c)
            /// {
            ///     DoSomething(a, b, c);
            /// }
            /// </summary>
            public ImmutableArray<(IParameterSymbol parameterSymbol, TIdentifierName identifierSyntaxNode)> ParametersWithIdentifierArgument { get; }

            /// <summary>
            /// Parameters map to variable declaration argument.
            /// This is only used for C# to support the 'out' variable declaration. For VB it should always be empty.
            /// Before:
            /// void Caller()
            /// {
            ///     Callee(out var x);
            /// }
            /// void Callee(out int i) => i = 100;
            ///
            /// After:
            /// void Caller()
            /// {
            ///     int x;
            ///     x = 100;
            /// }
            /// void Callee(out int i) => i = 100;
            /// </summary>
            public ImmutableArray<(IParameterSymbol parameterSymbol, SyntaxNode variableDeclarationNode)> ParametersWithVariableDeclarationArgument { get; }

            /// <summary>
            /// Operations that represent Parameter has argument but the argument is not identifier or literal.
            /// For these parameters they are considered to be put into a declaration statement after inlining.
            /// Note: params array could maps to multiple/zero arguments.
            /// Example:
            /// Before:
            /// void Caller(bool x)
            /// {
            ///     Callee(Foo(), x ? Foo() : Bar())
            /// }
            /// void Callee(int a, int b)
            /// {
            ///     DoSomething(a, b);
            /// }
            /// After:
            /// void Caller(bool x)
            /// {
            ///     int a = Foo();
            ///     int b = x ? Foo() : Bar();
            ///     DoSomething(a, b);
            /// }
            /// void Callee(int a, int b)
            /// {
            ///     DoSomething(a, b);
            /// }
            /// </summary>
            public ImmutableArray<IArgumentOperation> OperationsNeedGenerateDeclarations { get; }

            /// <summary>
            /// Parameters has no argument input and have default value. They will be replaced by their default value.
            /// </summary>
            public ImmutableArray<IParameterSymbol> ParametersWithDefaultValue { get; }

            /// <summary>
            /// Parameters map to literal expression argument. They will be replaced by the literal value.
            /// </summary>
            public ImmutableArray<(IParameterSymbol parameterSymbol, TExpression literalExpressionSyntaxNode)> ParametersWithLiteralArgument { get; }

            private MethodParametersInfo(
                ImmutableArray<(IParameterSymbol parameterSymbol, TIdentifierName identifierSyntaxNode)> parametersWithIdentifierArgument,
                ImmutableArray<(IParameterSymbol parameterSymbol, SyntaxNode variableDeclarationNode)> parametersWithVariableDeclarationArgument,
                ImmutableArray<IArgumentOperation> operationsNeedGenerateDeclarations,
                ImmutableArray<IParameterSymbol> parametersWithDefaultValue,
                ImmutableArray<(IParameterSymbol parameterSymbol, TExpression literalExpressionSyntaxNode)> parametersWithLiteralArgument)
            {
                ParametersWithIdentifierArgument = parametersWithIdentifierArgument;
                ParametersWithVariableDeclarationArgument = parametersWithVariableDeclarationArgument;
                OperationsNeedGenerateDeclarations = operationsNeedGenerateDeclarations;
                ParametersWithDefaultValue = parametersWithDefaultValue;
                ParametersWithLiteralArgument = parametersWithLiteralArgument;
            }

            public static MethodParametersInfo GetMethodParametersInfo(
                ISyntaxFacts syntaxFacts,
                IInvocationOperation invocationOperation)
            {
                var allArgumentOperations = invocationOperation.Arguments
                    .Where(operation => operation.Children.IsSingle())
                    .SelectAsArray(operation => (argumentOperation: operation,
                        argumentExpressionOperation: operation.Children.First()));

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
                var operationsWithIdentifierArgument = allArgumentOperations
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
                var operationsWithVariableDeclarationArgument = allArgumentOperations
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
                var operationsToGenerateFreshVariablesFor = allArgumentOperations
                    .RemoveRange(operationsWithIdentifierArgument)
                    .RemoveRange(operationsWithVariableDeclarationArgument)
                    .RemoveRange(operationsWithLiteralArgument)
                    .RemoveRange(operationsWithDefaultValue)
                    .SelectAsArray(argumentAndArgumentOperation => argumentAndArgumentOperation.argumentOperation);

                return new MethodParametersInfo(
                    operationsWithIdentifierArgument
                        .SelectAsArray(argumentAndArgumentOperation => (
                            argumentAndArgumentOperation.argumentOperation.Parameter,
                            (TIdentifierName)argumentAndArgumentOperation.argumentExpressionOperation.Syntax)),
                    operationsWithVariableDeclarationArgument
                        .SelectAsArray(argumentAndArgumentOperation => (
                            argumentAndArgumentOperation.argumentOperation.Parameter,
                            argumentAndArgumentOperation.argumentExpressionOperation.Syntax)),
                    operationsToGenerateFreshVariablesFor,
                    operationsWithDefaultValue.SelectAsArray(argumentAndArgumentOperation =>
                        argumentAndArgumentOperation.argumentOperation.Parameter),
                    operationsWithLiteralArgument
                        .SelectAsArray(argumentAndArgumentOperation => (
                            argumentAndArgumentOperation.argumentOperation.Parameter,
                            (TExpression)argumentAndArgumentOperation.argumentExpressionOperation.Syntax)));
            }
        }
    }
}
