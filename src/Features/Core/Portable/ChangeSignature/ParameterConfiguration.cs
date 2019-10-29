// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    internal sealed class ParameterConfiguration
    {
        public readonly ParameterBase ThisParameter;
        public readonly List<ParameterBase> ParametersWithoutDefaultValues;
        public readonly List<ParameterBase> RemainingEditableParameters;
        public readonly ParameterBase ParamsParameter;
        public readonly int SelectedIndex;

        public ParameterConfiguration(ParameterBase thisParameter, List<ParameterBase> parametersWithoutDefaultValues, List<ParameterBase> remainingEditableParameters, ParameterBase paramsParameter, int selectedIndex)
        {
            ThisParameter = thisParameter;
            ParametersWithoutDefaultValues = parametersWithoutDefaultValues;
            RemainingEditableParameters = remainingEditableParameters;
            ParamsParameter = paramsParameter;
            SelectedIndex = selectedIndex;
        }

        public static ParameterConfiguration Create(List<ParameterBase> parameters, bool isExtensionMethod, int selectedIndex)
        {
            ParameterBase thisParameter = null;
            var parametersWithoutDefaultValues = new List<ParameterBase>();
            var remainingReorderableParameters = new List<ParameterBase>();
            ParameterBase paramsParameter = null;

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
            return Create(ToListOfParameters().OfType<ExistingParameter>().ToList<ParameterBase>(), ThisParameter != null, selectedIndex: 0);
        }

        public List<ParameterBase> ToListOfParameters()
        {
            var list = new List<ParameterBase>();

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

        // TODO probably remove thiis check. It was created when we didn't support Add Parameters to parameterless methods.
        public bool IsChangeable()
        {
            return true;
        }
    }
}

