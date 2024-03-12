// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

internal enum RunSourceGeneratorsPreference
{
    AfterBuildFinishes,
    Automatically,
}

internal static class RunSourceGeneratorsPreferenceUtilities
{
    private const string after_build_finishes = "after_build_finishes";
    private const string automatically = "automatically";

    // Default to beginning_of_line if we don't know the value.
    public static string GetEditorConfigString(
        RunSourceGeneratorsPreference value)
    {
        return value switch
        {
            RunSourceGeneratorsPreference.AfterBuildFinishes => after_build_finishes,
            RunSourceGeneratorsPreference.Automatically => automatically,
            _ => throw ExceptionUtilities.UnexpectedValue(value),
        };
    }

    public static RunSourceGeneratorsPreference Parse(
        string optionString, RunSourceGeneratorsPreference defaultValue)
    {
        return optionString switch
        {
            after_build_finishes => RunSourceGeneratorsPreference.AfterBuildFinishes,
            automatically => RunSourceGeneratorsPreference.Automatically,
            _ => defaultValue,
        };
    }
}
