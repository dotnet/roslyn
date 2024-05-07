// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

/// <summary>
/// Helper type that allows signature help to make better decisions about which overload the user is likely choosing
/// when the compiler itself bails out and gives a generic list of options.
/// </summary>
internal readonly struct LightweightOverloadResolution(
    SemanticModel semanticModel,
    int position,
    SeparatedSyntaxList<ArgumentSyntax> arguments)
{
    public (IMethodSymbol? method, int parameterIndex) RefineOverloadAndPickParameter(SymbolInfo symbolInfo, ImmutableArray<IMethodSymbol> candidates)
    {
        // If the compiler told us the correct overload or we only have one choice, but we need to find out the
        // parameter to highlight given cursor position
        return symbolInfo.Symbol is IMethodSymbol method
            ? TryFindParameterIndexIfCompatibleMethod(method)
            : GuessCurrentSymbolAndParameter(candidates);
    }

    public int FindParameterIndexIfCompatibleMethod(IMethodSymbol method)
    {
        var (match, parameterIndex) = TryFindParameterIndexIfCompatibleMethod(method);
        return match is null ? -1 : parameterIndex;
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
                var (candidateMethod, parameterIndex) = TryFindParameterIndexIfCompatibleMethod(method);
                if (candidateMethod != null)
                    return (candidateMethod, parameterIndex);
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
    private (IMethodSymbol? method, int parameterIndex) TryFindParameterIndexIfCompatibleMethod(IMethodSymbol method)
    {
        // map the arguments to their corresponding parameters
        using var argumentToParameterMap = TemporaryArray<int>.Empty;

        for (var i = 0; i < arguments.Count; i++)
            argumentToParameterMap.Add(-1);

        if (!TryPrepareArgumentToParameterMap(method, ref argumentToParameterMap.AsRef()))
            return (null, -1);

        // verify that the arguments are compatible with their corresponding parameters
        var parameters = method.Parameters;
        for (var argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
        {
            var parameterIndex = argumentToParameterMap[argumentIndex];
            if (parameterIndex < 0)
                continue;

            var parameter = parameters[parameterIndex];
            var argument = arguments[argumentIndex];

            // We found a corresponding argument for this parameter.  If it's not compatible (say, a string passed
            // to an int parameter), then this is not a suitable overload.
            if (!IsCompatibleArgument(argument, parameter))
                return (null, -1);
        }

        // find the parameter at the cursor position
        var argumentIndexToSave = GetArgumentIndex();
        var foundParameterIndex = -1;
        if (argumentIndexToSave >= 0)
        {
            foundParameterIndex = argumentToParameterMap[argumentIndexToSave];
            if (foundParameterIndex < 0)
                foundParameterIndex = FirstUnspecifiedParameter(ref argumentToParameterMap.AsRef());
        }

        Debug.Assert(foundParameterIndex < parameters.Length);

        return (method, foundParameterIndex);
    }

    /// <summary>
    /// Determines if the given argument is compatible with the given parameter
    /// </summary>
    private bool IsCompatibleArgument(ArgumentSyntax argument, IParameterSymbol parameter)
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
                && semanticModel.ClassifyConversion(argument.Expression, arrayType.ElementType).IsImplicit)
            {
                return true;
            }

            return semanticModel.ClassifyConversion(argument.Expression, type).IsImplicit;
        }

        var argumentRefKind = argument.GetRefKind();
        if (parameterRefKind == argumentRefKind)
            return true;

        // A by-value argument matches an `in` parameter
        if (parameterRefKind == RefKind.In && argumentRefKind == RefKind.None)
            return true;

        return false;
    }

    // If the cursor is pointing at an argument for which we did not find the corresponding
    // parameter, we will highlight the first unspecified parameter.
    private int FirstUnspecifiedParameter(ref TemporaryArray<int> argumentToParameterMap)
    {
        using var specified = TemporaryArray<bool>.Empty;
        for (var i = 0; i < arguments.Count; i++)
            specified.Add(false);

        for (var i = 0; i < arguments.Count; i++)
        {
            var parameterIndex = argumentToParameterMap[i];
            if (parameterIndex >= 0 && parameterIndex < arguments.Count)
                specified.AsRef()[parameterIndex] = true;
        }

        for (var i = 0; i < specified.Count; i++)
        {
            if (!specified[i])
                return i;
        }

        return 0;
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
                // If this was a named argument but the method has no parameter with that name, there's definitely
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
    private int GetArgumentIndex()
    {
        for (var i = 0; i < arguments.Count - 1; i++)
        {
            // `$$,` points to the argument before the separator
            // but `,$$` points to the argument following the separator
            if (position <= arguments.GetSeparator(i).Span.Start)
                return i;
        }

        return arguments.Count - 1;
    }
}
