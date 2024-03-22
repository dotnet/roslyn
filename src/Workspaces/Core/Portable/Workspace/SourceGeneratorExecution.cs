// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

internal enum SourceGeneratorExecutionPreference
{
    Automatic,
    Balanced,
}

internal static class SourceGeneratorExecutionPreferenceUtilities
{
    private const string automatic = "automatic";
    private const string balanced = "balanced";

    // Default to beginning_of_line if we don't know the value.
    public static string GetEditorConfigString(SourceGeneratorExecutionPreference? value)
    {
        return value switch
        {
            SourceGeneratorExecutionPreference.Automatic => automatic,
            SourceGeneratorExecutionPreference.Balanced => balanced,
            null => "",
            _ => throw ExceptionUtilities.UnexpectedValue(value),
        };
    }

    public static SourceGeneratorExecutionPreference? Parse(
        string optionString)
    {
        return optionString switch
        {
            automatic => SourceGeneratorExecutionPreference.Automatic,
            balanced => SourceGeneratorExecutionPreference.Balanced,
            _ => null,
        };
    }
}
