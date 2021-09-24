// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public static class SolutionExtensions
    {
        public static Solution WithChangedOptionsFrom(this Solution solution, OptionSet optionSet)
        {
            var newOptions = solution.Options;
            if (newOptions is not SerializableOptionSet serializableNewOptions ||
                optionSet is not SerializableOptionSet serializableOptionSet)
            {
                return solution;
            }

            var newOptionMap = serializableNewOptions.OptionKeyToValue;
            var optionMap = serializableOptionSet.OptionKeyToValue;

            var finalMap = newOptionMap;
            foreach (var (key, value) in optionMap)
                finalMap = finalMap.SetItem(key, value);

            var finalOptionSet = serializableNewOptions.With(finalMap);
            return solution.WithOptions(finalOptionSet);
        }
    }
}
