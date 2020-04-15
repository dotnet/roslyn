// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ParameterConfiguration
    {
        public readonly ExistingParameter? ThisParameter;
        public readonly ImmutableArray<Parameter> ParametersWithoutDefaultValues;
        public readonly ImmutableArray<Parameter> RemainingEditableParameters;
        public readonly ExistingParameter? ParamsParameter;
        public readonly int SelectedIndex;

        public ParameterConfiguration(
            ExistingParameter? thisParameter,
            ImmutableArray<Parameter> parametersWithoutDefaultValues,
            ImmutableArray<Parameter> remainingEditableParameters,
            ExistingParameter? paramsParameter,
            int selectedIndex)
        {
            ThisParameter = thisParameter;
            ParametersWithoutDefaultValues = parametersWithoutDefaultValues;
            RemainingEditableParameters = remainingEditableParameters;
            ParamsParameter = paramsParameter;
            SelectedIndex = selectedIndex;
        }

        public static ParameterConfiguration Create(IEnumerable<Parameter?> parameters, bool isExtensionMethod, int selectedIndex)
        {
            var parametersList = parameters.ToList();
            ExistingParameter? thisParameter = null;
            var parametersWithoutDefaultValues = ImmutableArray.CreateBuilder<Parameter>();
            var remainingReorderableParameters = ImmutableArray.CreateBuilder<Parameter>();
            ExistingParameter? paramsParameter = null;

            if (parametersList.Count > 0 && isExtensionMethod)
            {
                thisParameter = parametersList[0] as ExistingParameter;
                parametersList.RemoveAt(0);
            }

            if (parametersList.Count > 0 && (parametersList[parametersList.Count - 1] as ExistingParameter)?.Symbol.IsParams == true)
            {
                paramsParameter = parametersList[parametersList.Count - 1] as ExistingParameter;
                parametersList.RemoveAt(parametersList.Count - 1);
            }

            var seenDefaultValues = false;
            foreach (var param in parametersList)
            {
                if (param != null)
                {
                    if (param.HasExplicitDefaultValue)
                    {
                        seenDefaultValues = true;
                    }

                    (seenDefaultValues ? remainingReorderableParameters : parametersWithoutDefaultValues).Add(param);
                }
            }

            return new ParameterConfiguration(thisParameter, parametersWithoutDefaultValues.ToImmutable(), remainingReorderableParameters.ToImmutable(), paramsParameter, selectedIndex);
        }

        internal ParameterConfiguration WithoutAddedParameters()
            => Create(ToListOfParameters().OfType<ExistingParameter>(), ThisParameter != null, selectedIndex: 0);

        public List<Parameter> ToListOfParameters()
        {
            var list = new List<Parameter>();

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

            return list;
        }
    }
}

