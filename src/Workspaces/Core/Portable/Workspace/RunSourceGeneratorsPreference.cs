// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

internal enum RunSourceGeneratorsPreference
{
    Automatically,
    WhenBuildsComplete,
}

internal static class RunSourceGeneratorsPreferenceUtilities
{
    private const string automatically = "automatically";
    private const string when_builds_complete = "when_builds_complete";

    // Default to beginning_of_line if we don't know the value.
    public static string GetEditorConfigString(RunSourceGeneratorsPreference? value)
    {
        return value switch
        {
            RunSourceGeneratorsPreference.Automatically => automatically,
            RunSourceGeneratorsPreference.WhenBuildsComplete => when_builds_complete,
            null => "",
            _ => throw ExceptionUtilities.UnexpectedValue(value),
        };
    }

    public static RunSourceGeneratorsPreference? Parse(
        string optionString)
    {
        return optionString switch
        {
            automatically => RunSourceGeneratorsPreference.Automatically,
            when_builds_complete => RunSourceGeneratorsPreference.WhenBuildsComplete,
            _ => null,
        };
    }
}
