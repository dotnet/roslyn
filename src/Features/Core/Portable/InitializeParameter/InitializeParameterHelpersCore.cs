// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.InitializeParameter;

internal static class InitializeParameterHelpersCore
{
    public static ImmutableArray<(IParameterSymbol parameter, bool before)> GetSiblingParameters(IParameterSymbol parameter)
    {
        using var _ = ArrayBuilder<(IParameterSymbol, bool before)>.GetInstance(out var siblings);

        if (parameter.ContainingSymbol is IMethodSymbol method)
        {
            var parameterIndex = method.Parameters.IndexOf(parameter);

            // look for an existing assignment for a parameter that comes before us.
            // If we find one, we'll add ourselves after that parameter check.
            for (var i = parameterIndex - 1; i >= 0; i--)
                siblings.Add((method.Parameters[i], before: true));

            // look for an existing check for a parameter that comes before us.
            // If we find one, we'll add ourselves after that parameter check.
            for (var i = parameterIndex + 1; i < method.Parameters.Length; i++)
                siblings.Add((method.Parameters[i], before: false));
        }

        return siblings.ToImmutableAndClear();
    }

    public static bool IsParameterReference(IOperation? operation, IParameterSymbol parameter)
        => operation.UnwrapImplicitConversion() is IParameterReferenceOperation parameterReference &&
           parameter.Equals(parameterReference.Parameter);

    public static bool IsParameterReferenceOrCoalesceOfParameterReference(
       IOperation? value, IParameterSymbol parameter)
    {
        if (IsParameterReference(value, parameter))
        {
            // We already have a member initialized with this parameter like:
            //      this.field = parameter
            return true;
        }

        if (value.UnwrapImplicitConversion() is ICoalesceOperation coalesceExpression &&
            IsParameterReference(coalesceExpression.Value, parameter))
        {
            // We already have a member initialized with this parameter like:
            //      this.field = parameter ?? ...
            return true;
        }

        return false;
    }

    public static string GenerateUniqueName(IParameterSymbol parameter, ImmutableArray<string> parameterNameParts, NamingRule rule)
    {
        // Determine an appropriate name to call the new field.
        var containingType = parameter.ContainingType;
        var baseName = rule.NamingStyle.CreateName(parameterNameParts);

        // Ensure that the name is unique in the containing type so we
        // don't stomp on an existing member.
        var uniqueName = NameGenerator.GenerateUniqueName(
            baseName, n => containingType.GetMembers(n).IsEmpty);
        return uniqueName;
    }
}
