// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGeneration;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class IParameterSymbolExtensions
{
    public static IParameterSymbol RenameParameter(this IParameterSymbol parameter, string parameterName)
    {
        return parameter.Name == parameterName
            ? parameter
            : CodeGenerationSymbolFactory.CreateParameterSymbol(
                    parameter.GetAttributes(),
                    parameter.RefKind,
                    parameter.IsParams,
                    parameter.Type,
                    parameterName,
                    parameter.IsOptional,
                    parameter.HasExplicitDefaultValue,
                    parameter.HasExplicitDefaultValue ? parameter.ExplicitDefaultValue : null);
    }

    public static IParameterSymbol WithAttributes(this IParameterSymbol parameter, ImmutableArray<AttributeData> attributes)
    {
        return parameter.GetAttributes() == attributes
            ? parameter
            : CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes,
                    parameter.RefKind,
                    parameter.IsParams,
                    parameter.Type,
                    parameter.Name,
                    parameter.IsOptional,
                    parameter.HasExplicitDefaultValue,
                    parameter.HasExplicitDefaultValue ? parameter.ExplicitDefaultValue : null);
    }

    public static ImmutableArray<IParameterSymbol> RenameParameters(this IList<IParameterSymbol> parameters, ImmutableArray<string> parameterNames)
    {
        var result = new FixedSizeArrayBuilder<IParameterSymbol>(parameters.Count);
        for (var i = 0; i < parameterNames.Length; i++)
            result.Add(parameters[i].RenameParameter(parameterNames[i]));

        return result.MoveToImmutable();
    }
}
