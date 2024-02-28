// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.ChangeSignature;

internal sealed class ParameterConfiguration(
    ExistingParameter? thisParameter,
    ImmutableArray<Parameter> parametersWithoutDefaultValues,
    ImmutableArray<Parameter> remainingEditableParameters,
    ExistingParameter? paramsParameter,
    int selectedIndex)
{
    public readonly ExistingParameter? ThisParameter = thisParameter;
    public readonly ImmutableArray<Parameter> ParametersWithoutDefaultValues = parametersWithoutDefaultValues;
    public readonly ImmutableArray<Parameter> RemainingEditableParameters = remainingEditableParameters;
    public readonly ExistingParameter? ParamsParameter = paramsParameter;
    public readonly int SelectedIndex = selectedIndex;

    public static ParameterConfiguration Create(ImmutableArray<Parameter> parameters, bool isExtensionMethod, int selectedIndex)
    {
        var parametersList = parameters.ToList();
        ExistingParameter? thisParameter = null;
        var parametersWithoutDefaultValues = ArrayBuilder<Parameter>.GetInstance();
        var remainingReorderableParameters = ArrayBuilder<Parameter>.GetInstance();
        ExistingParameter? paramsParameter = null;

        if (parametersList.Count > 0 && isExtensionMethod)
        {
            // Extension method `this` parameters cannot be added, so must be pre-existing.
            thisParameter = (ExistingParameter)parametersList[0];
            parametersList.RemoveAt(0);
        }

        if ((parametersList.LastOrDefault() as ExistingParameter)?.Symbol.IsParams == true)
        {
            // Params arrays cannot be added, so must be pre-existing.
            paramsParameter = (ExistingParameter)parametersList[^1];
            parametersList.RemoveAt(parametersList.Count - 1);
        }

        var seenDefaultValues = false;
        foreach (var param in parametersList)
        {
            if (param.HasDefaultValue)
            {
                seenDefaultValues = true;
            }

            (seenDefaultValues ? remainingReorderableParameters : parametersWithoutDefaultValues).Add(param);
        }

        return new ParameterConfiguration(thisParameter, parametersWithoutDefaultValues.ToImmutableAndFree(), remainingReorderableParameters.ToImmutableAndFree(), paramsParameter, selectedIndex);
    }

    internal ParameterConfiguration WithoutAddedParameters()
        => Create(ToListOfParameters().OfType<ExistingParameter>().ToImmutableArray<Parameter>(), ThisParameter != null, selectedIndex: 0);

    public ImmutableArray<Parameter> ToListOfParameters()
    {
        var list = ArrayBuilder<Parameter>.GetInstance();

        if (ThisParameter != null)
        {
            list.Add(ThisParameter);
        }

        list.AddRange(ParametersWithoutDefaultValues);
        list.AddRange(RemainingEditableParameters);

        if (ParamsParameter != null)
        {
            list.Add(ParamsParameter);
        }

        return list.ToImmutableAndFree();
    }
}

