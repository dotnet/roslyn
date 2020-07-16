// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public static class SolutionExtensions
    {
        public static Solution WithChangedOptionsFrom(this Solution solution, OptionSet optionSet)
        {
            var newOptions = solution.Options;
            foreach (var option in optionSet.GetChangedOptions(solution.Options))
            {
                newOptions = newOptions.WithChangedOption(option, optionSet.GetOption(option));
            }

            return solution.WithOptions(newOptions);
        }
    }
}
