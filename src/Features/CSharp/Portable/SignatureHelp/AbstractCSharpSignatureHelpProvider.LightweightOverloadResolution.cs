// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal abstract partial class AbstractCSharpSignatureHelpProvider
    {
        internal static class LightweightOverloadResolution
        {
            public static void RefineOverloadAndPickParameter(Document document, int position, SemanticModel semanticModel,
                ImmutableArray<IMethodSymbol> candidates, SeparatedSyntaxList<ArgumentSyntax> arguments,
                out IMethodSymbol? currentSymbol, out int parameterIndex)
            {
                var semanticFactsService = document.GetRequiredLanguageService<ISemanticFactsService>();
                if (candidates.Length == 1)
                {
                    // The compiler told us the correct overload or we only have one choice, but we need to find out the parameter to highlight given cursor position
                    currentSymbol = candidates[0];
                    _ = FindParameterIndexIfCompatibleMethod(arguments, currentSymbol, position, semanticModel, semanticFactsService, out parameterIndex);
                }
                else
                {
                    (currentSymbol, parameterIndex) = GuessCurrentSymbolAndParameter(arguments, candidates, position, semanticModel, semanticFactsService);
                }
            }

            /// <summary>
            /// If the symbol could not be bound, we could be dealing with a partial invocation, we'll try to find a possible overload.
            /// </summary>
            private static (IMethodSymbol? symbol, int parameterIndex) GuessCurrentSymbolAndParameter(
                SeparatedSyntaxList<ArgumentSyntax> arguments, ImmutableArray<IMethodSymbol> methodGroup, int position,
                SemanticModel semanticModel, ISemanticFactsService semanticFactsService)
            {
                if (arguments.Count != 0)
                {
                    foreach (var method in methodGroup)
                    {
                        if (FindParameterIndexIfCompatibleMethod(arguments, method, position, semanticModel, semanticFactsService, out var parameterIndex))
                        {
                            return (method, parameterIndex);
                        }
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
            internal static bool FindParameterIndexIfCompatibleMethod(SeparatedSyntaxList<ArgumentSyntax> arguments, IMethodSymbol method, int position,
                SemanticModel semanticModel, ISemanticFactsService semanticFactsService, out int foundParameterIndex)
            {
                // map the arguments to their corresponding parameters
                var argumentCount = arguments.Count;
                using var _ = ArrayBuilder<int>.GetInstance(argumentCount, fillWithValue: -1, out var argToParamMap);
                if (!TryPrepareArgToParamMap(arguments, method, argToParamMap))
                {
                    foundParameterIndex = -1;
                    return false;
                }

                // verify that the arguments are compatible with their corresponding parameters
                var parameters = method.Parameters;
                for (var argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++)
                {
                    var parameterIndex = argToParamMap[argumentIndex];
                    if (parameterIndex < 0)
                    {
                        continue;
                    }

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
                    if (foundParam >= 0)
                    {
                        foundParameterIndex = foundParam;
                    }
                    else
                    {
                        foundParameterIndex = FirstUnspecifiedParameter(argToParamMap, argumentCount);
                    }
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
                    using var _ = ArrayBuilder<bool>.GetInstance(argumentCount, false, out var specified);
                    for (var i = 0; i < argumentCount; i++)
                    {
                        var parameterIndex = argToParamMap[i];
                        if (parameterIndex >= 0)
                        {
                            specified[parameterIndex] = true;
                        }
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
            private static bool TryPrepareArgToParamMap(SeparatedSyntaxList<ArgumentSyntax> arguments, IMethodSymbol method, ArrayBuilder<int> argToParamMap)
            {
                var parameters = method.Parameters;
                var parameterCount = parameters.Length;
                var currentParameterIndex = 0;
                var seenOutOfPositionArgument = false;
                var inParams = false;

                for (var argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
                {
                    if (argumentIndex >= parameterCount && !inParams)
                    {
                        return false;
                    }

                    var argument = arguments[argumentIndex];
                    if (HasName(argument, out var name))
                    {
                        var namedParameterIndex = parameters.IndexOf(p => p.Name == name);
                        if (namedParameterIndex < 0)
                        {
                            return false;
                        }

                        if (namedParameterIndex != currentParameterIndex)
                        {
                            seenOutOfPositionArgument = true;
                        }

                        AddArgToParamMapping(argumentIndex, namedParameterIndex);
                        IncrementParameterIndexIfNeeded();
                    }
                    else if (IsEmptyArgument(argument.Expression))
                    {
                        if (!seenOutOfPositionArgument)
                        {
                            // We count the empty argument as a used position
                            AddArgToParamMapping(argumentIndex, currentParameterIndex);
                            IncrementParameterIndexIfNeeded();
                        }
                    }
                    else if (seenOutOfPositionArgument)
                    {
                        // Unnamed arguments are not allowed after an out-of-position argument
                        return false;
                    }
                    else
                    {
                        AddArgToParamMapping(argumentIndex, currentParameterIndex);
                        IncrementParameterIndexIfNeeded();
                    }
                }

                return true;

                void IncrementParameterIndexIfNeeded()
                {
                    if (!seenOutOfPositionArgument && !inParams)
                    {
                        currentParameterIndex++;
                    }
                }

                void AddArgToParamMapping(int argumentIndex, int parameterIndex)
                {
                    Debug.Assert(parameterIndex >= 0);
                    Debug.Assert(parameterIndex < parameterCount);

                    inParams |= parameters[parameterIndex].IsParams;
                    argToParamMap[argumentIndex] = parameterIndex;
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
                {
                    return -1;
                }

                for (var i = 0; i < arguments.Count - 1; i++)
                {
                    // `$$,` points to the argument before the separator
                    // but `,$$` points to the argument following the separator
                    if (position <= arguments.GetSeparator(i).Span.Start)
                    {
                        return i;
                    }
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
}
