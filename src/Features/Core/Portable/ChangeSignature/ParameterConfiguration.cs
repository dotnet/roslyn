// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ParameterConfiguration
    {
        public readonly IParameterSymbol ThisParameter;
        public readonly List<IParameterSymbol> ParametersWithoutDefaultValues;
        public readonly List<IParameterSymbol> RemainingEditableParameters;
        public readonly IParameterSymbol ParamsParameter;
        public readonly int SelectedIndex;

        public ParameterConfiguration(IParameterSymbol thisParameter, List<IParameterSymbol> parametersWithoutDefaultValues, List<IParameterSymbol> remainingEditableParameters, IParameterSymbol paramsParameter, int selectedIndex)
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
            var parametersWithoutDefaultValues = new List<IParameterSymbol>();
            var remainingReorderableParameters = new List<IParameterSymbol>();
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

                (seenDefaultValues ? remainingReorderableParameters : parametersWithoutDefaultValues).Add(param);
            }

            return new ParameterConfiguration(thisParameter, parametersWithoutDefaultValues, remainingReorderableParameters, paramsParameter, selectedIndex);
        }

        public List<IParameterSymbol> ToListOfParameters()
        {
            var list = new List<IParameterSymbol>();

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

        public bool IsChangeable()
        {
            return ParametersWithoutDefaultValues.Count > 0 || RemainingEditableParameters.Count > 0 || ParamsParameter != null;
        }
    }
}
