// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ParameterConfiguration
    {
        public readonly Parameter? ThisParameter;
        public readonly ImmutableArray<Parameter> ParametersWithoutDefaultValues;
        public readonly ImmutableArray<Parameter> RemainingEditableParameters;
        public readonly Parameter? ParamsParameter;
        public readonly int SelectedIndex;

        public ParameterConfiguration(
            Parameter? thisParameter,
            ImmutableArray<Parameter> parametersWithoutDefaultValues,
            ImmutableArray<Parameter> remainingEditableParameters,
            Parameter? paramsParameter,
            int selectedIndex)
        {
            ThisParameter = thisParameter;
            ParametersWithoutDefaultValues = parametersWithoutDefaultValues;
            RemainingEditableParameters = remainingEditableParameters;
            ParamsParameter = paramsParameter;
            SelectedIndex = selectedIndex;
        }

        public static ParameterConfiguration Create(List<Parameter?> parameters, bool isExtensionMethod, int selectedIndex)
        {
            Parameter? thisParameter = null;
            var parametersWithoutDefaultValues = ImmutableArray.CreateBuilder<Parameter>();
            var remainingReorderableParameters = ImmutableArray.CreateBuilder<Parameter>();
            Parameter? paramsParameter = null;

            if (parameters.Count > 0 && isExtensionMethod)
            {
                thisParameter = parameters[0];
                parameters.RemoveAt(0);
            }

            if (parameters.Count > 0 && (parameters[parameters.Count - 1] as ExistingParameter)?.Symbol.IsParams == true)
            {
                paramsParameter = parameters[parameters.Count - 1];
                parameters.RemoveAt(parameters.Count - 1);
            }

            var seenDefaultValues = false;
            foreach (var param in parameters)
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
            => Create(ToListOfParameters().OfType<ExistingParameter>().ToList<Parameter?>(), ThisParameter != null, selectedIndex: 0);

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

