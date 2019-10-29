// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ParameterConfiguration
    {
        public readonly Parameter ThisParameter;
        public readonly List<Parameter> ParametersWithoutDefaultValues;
        public readonly List<Parameter> RemainingEditableParameters;
        public readonly Parameter ParamsParameter;
        public readonly int SelectedIndex;

        public ParameterConfiguration(Parameter thisParameter, List<Parameter> parametersWithoutDefaultValues, List<Parameter> remainingEditableParameters, Parameter paramsParameter, int selectedIndex)
        {
            ThisParameter = thisParameter;
            ParametersWithoutDefaultValues = parametersWithoutDefaultValues;
            RemainingEditableParameters = remainingEditableParameters;
            ParamsParameter = paramsParameter;
            SelectedIndex = selectedIndex;
        }

        public static ParameterConfiguration Create(List<Parameter> parameters, bool isExtensionMethod, int selectedIndex)
        {
            Parameter thisParameter = null;
            var parametersWithoutDefaultValues = new List<Parameter>();
            var remainingReorderableParameters = new List<Parameter>();
            Parameter paramsParameter = null;

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
                if (param.HasExplicitDefaultValue)
                {
                    seenDefaultValues = true;
                }

                (seenDefaultValues ? remainingReorderableParameters : parametersWithoutDefaultValues).Add(param);
            }

            return new ParameterConfiguration(thisParameter, parametersWithoutDefaultValues, remainingReorderableParameters, paramsParameter, selectedIndex);
        }

        internal ParameterConfiguration WithoutAddedParameters()
        {
            return Create(ToListOfParameters().OfType<ExistingParameter>().ToList<Parameter>(), ThisParameter != null, selectedIndex: 0);
        }

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

        // TODO probably remove this check. It was created when we didn't support Add Parameters to parameterless methods.
        public bool IsChangeable()
        {
            return true;
        }
    }
}

