// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal abstract partial class AbstractInlineMethodRefactoringProvider<TInvocationSyntaxNode, TExpressionSyntax, TArgumentSyntax>
        where TInvocationSyntaxNode : SyntaxNode
        where TExpressionSyntax : SyntaxNode
        where TArgumentSyntax : SyntaxNode
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
                ImmutableArray<(IParameterSymbol, SyntaxNode)> parametersWithIdentifierArgument,
                ImmutableArray<(IParameterSymbol parameterSymbol, SyntaxNode variableDeclarationNode)> parametersWithVariableDeclarationArgument,
                ImmutableArray<(IParameterSymbol, ImmutableArray<SyntaxNode>)> parametersNeedGenerateDeclarations,
                ImmutableArray<IParameterSymbol> parametersWithDefaultValue,
                ImmutableArray<(IParameterSymbol parameterSymbol, SyntaxNode literalExpressionSyntaxNode)> parametersWithLiteralArgument)
            {
                ParametersWithIdentifierArgument = parametersWithIdentifierArgument;
                ParametersWithVariableDeclarationArgument = parametersWithVariableDeclarationArgument;
                ParametersNeedGenerateDeclarations = parametersNeedGenerateDeclarations;
                ParametersWithDefaultValue = parametersWithDefaultValue;
                ParametersWithLiteralArgument = parametersWithLiteralArgument;
            }

            public static MethodParametersInfo GetMethodParametersInfo(
                AbstractInlineMethodRefactoringProvider<TInvocationSyntaxNode, TExpressionSyntax, TArgumentSyntax> inlineMethodRefactoringProvider,
                ISyntaxFacts syntaxFacts,
                SemanticModel semanticModel,
                SyntaxNode calleeInvocationSyntaxNode,
                IMethodSymbol calleeMethodSymbol,
                CancellationToken cancellationToken)
            {
                // Classify the parameters into:
                // 1. If the parameter accept an identifier from Caller, then use this identifier to replace all the occurence of the parameter.
                // (So that after inlining, the same identifier could be found in the caller)
                // 2. If the parameter accept literal/no argument but has default value, replace all the occurence of the parameter by using the
                // literal expression
                // 3. If the argument has variable declarations ('out var' in C#), then insert an additional declaration. Also replace all the
                // occurence of parameter by using this identifier.
                // 4. For the rest of the parameters (might include method invocation and different expressions), use the parameter's name to
                // generate a declaration for them and insert that into caller.
                var allArguments = syntaxFacts.GetArgumentsOfInvocationExpression(calleeInvocationSyntaxNode);
                var parameterSymbolAndArguments = allArguments
                    .Select(argument => (
                        parameterSymbol: inlineMethodRefactoringProvider.GetParameterSymbol(semanticModel, (TArgumentSyntax)argument,
                            cancellationToken),
                        argumentExpression: syntaxFacts.GetExpressionOfArgument(argument)))
                    .Where(parameterAndArgument =>
                        parameterAndArgument.parameterSymbol != null
                        && !parameterAndArgument.parameterSymbol.IsDiscard)
                    // For params array, it could map to multiple arguments so group it.
                    .GroupBy(keySelector: parameterAndArgument => parameterAndArgument.parameterSymbol!,
                        elementSelector: parameterAndArgument => parameterAndArgument.argumentExpression)
                    .SelectAsArray(grouping => (parameterSymbol: grouping.Key, arguments: grouping.ToImmutableArray()));
                var allParametersWithArgument = parameterSymbolAndArguments.SelectAsArray(g => g.parameterSymbol);

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
                var parametersWithIdentifier = parameterSymbolAndArguments
                    .Where(parameterAndArguments => ParameterWithIdentifierArgumentFilter(syntaxFacts, semanticModel,
                        parameterAndArguments, cancellationToken))
                    .ToImmutableArray();

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
                var parametersWithVariableDeclarationArgument = parameterSymbolAndArguments
                    .Where(parameterAndArguments =>
                        ParameterWithVariableDeclarationArgumentFilter(syntaxFacts, parameterAndArguments))
                    .ToImmutableArray();

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
                var parametersWithLiteralArgument = parameterSymbolAndArguments
                    .Where(parameterAndArguments =>
                        ParameterWithLiteralArgumentFilter(syntaxFacts, parameterAndArguments))
                    .ToImmutableArray();

                // 4. All the remaining arguments, which might includes method call and a lot of other expressions.
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
                var parametersNeedGenerateDeclarations = parameterSymbolAndArguments
                    .RemoveRange(parametersWithIdentifier)
                    .RemoveRange(parametersWithVariableDeclarationArgument)
                    .RemoveRange(parametersWithLiteralArgument)
                    .Where(parameterAndArguments => ParameterNeedsGenerateDeclarationFilter(
                        inlineMethodRefactoringProvider,
                        syntaxFacts, semanticModel, parameterAndArguments, cancellationToken))
                    .ToImmutableArray();

                // 5. Parameter without any argument.
                // Case 1: only parameter has default value would be left here. The parameter will be replaced by the default value.
                // Case 2: there is no arguments and the parameter is params array. An array needs to be declared in the caller. Similarly to what is done in step 4
                // Example for case 1:
                // Before:
                // void Caller()
                // {
                //     Callee();
                // }
                // void Callee(int i = 10, bool f = true)
                // {
                //     DoSomething(i, f);
                // }
                // After:
                // void Caller()
                // {
                //     DoSomething(10, true);
                // }
                // void Callee(int i = 10, bool f = true)
                // {
                //     DoSomething(i, f);
                // }
                // Example for case 2:
                // Before:
                // void Caller(bool x)
                // {
                //     Callee();
                // }
                // void Callee(params int[] a)
                // {
                //     DoSomething(a);
                // }
                // After:
                // void Caller(bool x)
                // {
                //     int[] a = { };
                //     DoSomething(a);
                // }
                // void Callee(params int[] a)
                // {
                //     DoSomething(a);
                // }
                var parametersWithoutArgument = calleeMethodSymbol.Parameters.RemoveRange(allParametersWithArgument);
                if (parametersWithoutArgument.Length == 1 && parametersWithoutArgument[0].IsParams)
                {
                    parametersNeedGenerateDeclarations = parametersNeedGenerateDeclarations.Concat((
                        parametersWithoutArgument[0], ImmutableArray<SyntaxNode>.Empty));
                }

                var parametersWithDefaultValue = parametersWithoutArgument
                    .Where(parameterSymbol => parameterSymbol.HasExplicitDefaultValue)
                    .ToImmutableArray();

                return new MethodParametersInfo(
                    parametersWithIdentifier.SelectAsArray(parameterAndArgument =>
                        (parameterAndArgument.parameterSymbol, parameterAndArgument.arguments[0])),
                    parametersWithVariableDeclarationArgument.SelectAsArray(parameterAndArgument =>
                        (parameterAndArgument.parameterSymbol, parameterAndArgument.arguments[0])),
                    parametersNeedGenerateDeclarations,
                    parametersWithDefaultValue,
                    parametersWithLiteralArgument.SelectAsArray(parameterAndArgument =>
                        (parameterAndArgument.parameterSymbol, parameterAndArgument.arguments[0])));
            }

            private static bool ParameterNeedsGenerateDeclarationFilter(
                AbstractInlineMethodRefactoringProvider<TInvocationSyntaxNode, TExpressionSyntax, TArgumentSyntax> inlineMethodRefactoringProvider,
                ISyntaxFacts syntaxFacts,
                SemanticModel semanticModel,
                (IParameterSymbol parameterSymbol, ImmutableArray<SyntaxNode> arguments) parameterAndArguments,
                CancellationToken cancellationToken)
            {
                var (parameterSymbol, arguments) = parameterAndArguments;
                if (!parameterSymbol.IsParams && arguments.Length == 1)
                {
                    var argument = arguments[0];
                    return IsExpressionSyntax(argument);
                }

                // Params array is special, if there are multiple arguments, then they needs to be put into a separate array.
                if (parameterSymbol.IsParams)
                {
                    if (arguments.Length != 1)
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
                        var argument = arguments[0];
                        var typeOfElement = (IArrayTypeSymbol)parameterSymbol.Type;
                        var typeOfArgument = semanticModel.GetTypeInfo(argument, cancellationToken).Type;
                        return !(typeOfElement.Equals(typeOfArgument) && syntaxFacts.IsIdentifierName(argument));
                    }
                }

                return false;
            }

            private static bool ParameterWithLiteralArgumentFilter(
                ISyntaxFacts syntaxFacts,
                (IParameterSymbol parameterSymbol, ImmutableArray<SyntaxNode> arguments) parameterAndArguments)
            {
                var (parameterSymbol, arguments) = parameterAndArguments;
                if (!parameterSymbol.IsParams && arguments.Length == 1)
                {
                    return syntaxFacts.IsLiteralExpression(arguments[0]);
                }

                return false;
            }

            private static bool ParameterWithVariableDeclarationArgumentFilter(
                ISyntaxFacts syntaxFacts,
                (IParameterSymbol parameterSymbol, ImmutableArray<SyntaxNode> arguments) parameterAndArguments)
            {
                var (_, arguments) = parameterAndArguments;
                return arguments.Length == 1 && syntaxFacts.IsDeclarationExpression(arguments[0]);
            }

            private static bool ParameterWithIdentifierArgumentFilter(
                ISyntaxFacts syntaxFacts,
                SemanticModel semanticModel,
                (IParameterSymbol parameterSymbol, ImmutableArray<SyntaxNode> arguments) parameterAndArguments,
                CancellationToken cancellationToken)
            {
                var (parameterSymbol, arguments) = parameterAndArguments;
                if (arguments.Length == 1)
                {
                    var argument = arguments[0];
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
        }
    }
}
