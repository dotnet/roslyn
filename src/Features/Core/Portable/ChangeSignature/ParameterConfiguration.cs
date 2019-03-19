// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ParameterConfiguration
    {
        public readonly IParameterSymbol ThisParameter;
        public readonly List<CoolParameter> ParametersWithoutDefaultValues;
        public readonly List<CoolParameter> RemainingEditableParameters;
        public readonly IParameterSymbol ParamsParameter;
        public readonly int SelectedIndex;

        public ParameterConfiguration(IParameterSymbol thisParameter, List<CoolParameter> parametersWithoutDefaultValues, List<CoolParameter> remainingEditableParameters, IParameterSymbol paramsParameter, int selectedIndex)
        {
            ThisParameter = thisParameter;
            ParametersWithoutDefaultValues = parametersWithoutDefaultValues;
            RemainingEditableParameters = remainingEditableParameters;
            ParamsParameter = paramsParameter;
            SelectedIndex = selectedIndex;
        }

        public static ParameterConfiguration Create(List<IParameterSymbol> parameters, bool isExtensionMethod, int selectedIndex)
        {
            IParameterSymbol thisParameter = null;
            var parametersWithoutDefaultValues = new List<CoolParameter>();
            var remainingReorderableParameters = new List<CoolParameter>();
            IParameterSymbol paramsParameter = null;

            if (parameters.Count > 0 && isExtensionMethod)
            {
                thisParameter = parameters[0];
                parameters.RemoveAt(0);
            }

            if (parameters.Count > 0 && parameters[parameters.Count - 1].IsParams)
            {
                paramsParameter = parameters[parameters.Count - 1];
                parameters.RemoveAt(parameters.Count - 1);
            }

            var seenDefaultValues = false;
            foreach (var param in parameters)
            {
                if (param.HasExplicitDefaultValue)
                {
                    seenDefaultValues = true;
                }

                (seenDefaultValues ? remainingReorderableParameters : parametersWithoutDefaultValues).Add(new ExistingParameter(param));
            }

            return new ParameterConfiguration(thisParameter, parametersWithoutDefaultValues, remainingReorderableParameters, paramsParameter, selectedIndex);
        }

        public List<CoolParameter> ToListOfParameters()
        {
            var list = new List<CoolParameter>();

            if (ThisParameter != null)
            {
                list.Add(new ExistingParameter(ThisParameter));
            }

            list.AddRange(ParametersWithoutDefaultValues);
            list.AddRange(RemainingEditableParameters);

            if (ParamsParameter != null)
            {
                list.Add(new ExistingParameter(ParamsParameter));
            }

            return list;
        }

        public bool IsChangeable()
        {
            return ParametersWithoutDefaultValues.Count > 0 || RemainingEditableParameters.Count > 0 || ParamsParameter != null;
        }
    }
}
