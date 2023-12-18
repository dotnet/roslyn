// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

internal abstract partial class AbstractCSharpSignatureHelpProvider
{
    protected readonly struct LightweightOverloadResolution(
        ISemanticFactsService semanticFactsService,
        SemanticModel semanticModel,
        int position,
        SeparatedSyntaxList<ArgumentSyntax> arguments)
    {
        public (IMethodSymbol? method, int parameterIndex) RefineOverloadAndPickParameter(ImmutableArray<IMethodSymbol> candidates)
        {
            // If the compiler told us the correct overload or we only have one choice, but we need to find out the parameter to highlight given cursor position
            return candidates.Length == 1
                ? FindParameterIndexIfCompatibleMethod(candidates[0])
                : GuessCurrentSymbolAndParameter(candidates);
        }

        /// <summary>
        /// If the symbol could not be bound, we could be dealing with a partial invocation, we'll try to find a possible overload.
        /// </summary>
        private (IMethodSymbol? symbol, int parameterIndex) GuessCurrentSymbolAndParameter(ImmutableArray<IMethodSymbol> methodGroup)
        {
            if (arguments.Count > 0)
            {
                foreach (var method in methodGroup)
                {
                    if (TryFindParameterIndexIfCompatibleMethod(method, out var parameterIndex))
                        return (method, parameterIndex);
                }
            }

            // Note: Providing no recommendation if no arguments allows the model to keep the last implicit choice
            return (null, -1);
        }

        /// <summary>
        /// Simulates overload resolution with the arguments provided so far and determines if you might be calling this overload.
        /// Returns true if an overload is acceptable. In that case, we output the parameter that should be highlighted given the cursor's
        /// position in the partial invocation.
        /// </summary>
        public bool TryFindParameterIndexIfCompatibleMethod(IMethodSymbol method, out int parameterIndex)
        {
            // map the arguments to their corresponding parameters
            var argumentCount = arguments.Count;
            using var argToParamMap = TemporaryArray<int>.Empty;

            for (var i = 0; i < argumentCount; i++)
                argToParamMap.Add(-1);

            if (!TryPrepareArgumentToParameterMap(method, ref argToParamMap.AsRef()))
            {
                parameterIndex = -1;
                return false;
            }

            // verify that the arguments are compatible with their corresponding parameters
            var parameters = method.Parameters;
            for (var argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++)
            {
                var parameterIndex = argToParamMap[argumentIndex];
                if (parameterIndex < 0)
                    continue;

                var parameter = parameters[parameterIndex];
                var argument = arguments[argumentIndex];

                if (!IsCompatibleArgument(argument, parameter))
                {
                    foundParameterIndex = -1;
                    return false;
                }
            }

            // find the parameter at the cursor position
            var argumentIndexToSave = TryGetArgumentIndex(arguments, position);
            if (argumentIndexToSave >= 0)
            {
                var foundParam = argToParamMap[argumentIndexToSave];
                foundParameterIndex = foundParam >= 0
                    ? foundParam
                    : FirstUnspecifiedParameter(argToParamMap, argumentCount);
            }
            else
            {
                foundParameterIndex = -1;
            }

            Debug.Assert(foundParameterIndex < parameters.Length);

            return true;

            // If the cursor is pointing at an argument for which we did not find the corresponding
            // parameter, we will highlight the first unspecified parameter.
            static int FirstUnspecifiedParameter(ArrayBuilder<int> argToParamMap, int argumentCount)
            {
                using var _ = ArrayBuilder<bool>.GetInstance(argumentCount, fillWithValue: false, out var specified);
                specified.Count = argumentCount;

                for (var i = 0; i < argumentCount; i++)
                {
                    var parameterIndex = argToParamMap[i];
                    if (parameterIndex >= 0)
                        specified[parameterIndex] = true;
                }

                var first = specified.FindIndex(s => !s);
                return first <= 0 ? 0 : first;
            }

            // Determines if the given argument is compatible with the given parameter
            bool IsCompatibleArgument(ArgumentSyntax argument, IParameterSymbol parameter)
            {
                var parameterRefKind = parameter.RefKind;
                if (parameterRefKind == RefKind.None)
                {
                    if (IsEmptyArgument(argument.Expression))
                    {
                        // An argument left empty is considered to match any parameter
                        // M(1, $$)
                        // M(1, , 2$$)
                        return true;
                    }

                    var type = parameter.Type;
                    if (parameter.IsParams
                        && type is IArrayTypeSymbol arrayType
                        && HasImplicitConversion(argument.Expression, arrayType.ElementType))
                    {
                        return true;
                    }

                    return HasImplicitConversion(argument.Expression, type);
                }

                var argumentRefKind = argument.GetRefKind();
                if (parameterRefKind == RefKind.In && argumentRefKind == RefKind.None)
                {
                    // A by-value argument matches an `in` parameter
                    return true;
                }

                if (parameterRefKind == argumentRefKind)
                {
                    return true;
                }

                return false;
            }

            bool HasImplicitConversion(SyntaxNode expression, ITypeSymbol destination)
            {
                var conversion = semanticFactsService.ClassifyConversion(semanticModel, expression, destination);
                return conversion.IsImplicit;
            }
        }

        /// <summary>
        /// Find the parameter index corresponding to each argument provided
        /// </summary>
        private bool TryPrepareArgumentToParameterMap(IMethodSymbol method, ref TemporaryArray<int> argumentToParameterMap)
        {
            Contract.ThrowIfTrue(argumentToParameterMap.Count != arguments.Count);

            var currentParameterIndex = 0;
            var seenOutOfPositionArgument = false;
            var inParams = false;

            for (var argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
            {
                // Went past the number of parameters this method takes, and this is a non-params method.  There's no
                // way this could ever match.
                if (argumentIndex >= method.Parameters.Length && !inParams)
                    return false;

                var argument = arguments[argumentIndex];
                if (argument is { NameColon.Name.Identifier.ValueText: var argumentName })
                {
                    // If this was a named argument but the method has no parameter with that name, tehre's definitely
                    // no match.  Note: this is C# only, so we don't need to worry about case matching.
                    var namedParameterIndex = method.Parameters.IndexOf(p => p.Name == argumentName);
                    if (namedParameterIndex < 0)
                        return false;

                    if (namedParameterIndex != currentParameterIndex)
                        seenOutOfPositionArgument = true;

                    AddArgumentToParameterMapping(argumentIndex, namedParameterIndex, ref argumentToParameterMap);
                }
                else if (IsEmptyArgument(argument.Expression))
                {
                    // We count the empty argument as a used position
                    if (!seenOutOfPositionArgument)
                        AddArgumentToParameterMapping(argumentIndex, currentParameterIndex, ref argumentToParameterMap);
                }
                else if (seenOutOfPositionArgument)
                {
                    // Unnamed arguments are not allowed after an out-of-position argument
                    return false;
                }
                else
                {
                    // Normal argument.
                    AddArgumentToParameterMapping(argumentIndex, currentParameterIndex, ref argumentToParameterMap);
                }
            }

            return true;

            void AddArgumentToParameterMapping(int argumentIndex, int parameterIndex, ref TemporaryArray<int> argumentToParameterMap)
            {
                Debug.Assert(parameterIndex >= 0);
                Debug.Assert(parameterIndex < method.Parameters.Length);

                inParams |= method.Parameters[parameterIndex].IsParams;
                argumentToParameterMap[argumentIndex] = parameterIndex;

                // Increment our current parameter index if we're still processing parameters in sequential order.
                if (!seenOutOfPositionArgument && !inParams)
                    currentParameterIndex++;
            }
        }

        private static bool IsEmptyArgument(ExpressionSyntax expression)
            => expression.Span.IsEmpty;

        /// <summary>
        /// Given the cursor position, find which argument is active.
        /// This will be useful to later find which parameter should be highlighted.
        /// </summary>
        private static int TryGetArgumentIndex(SeparatedSyntaxList<ArgumentSyntax> arguments, int position)
        {
            if (arguments.Count == 0)
                return -1;

            for (var i = 0; i < arguments.Count - 1; i++)
            {
                // `$$,` points to the argument before the separator
                // but `,$$` points to the argument following the separator
                if (position <= arguments.GetSeparator(i).Span.Start)
                    return i;
            }

            return arguments.Count - 1;
        }

        private static bool HasName(ArgumentSyntax argument, [NotNullWhen(true)] out string? name)
        {
            name = argument.NameColon?.Name.Identifier.ValueText;
            return name != null;
        }
    }
}
